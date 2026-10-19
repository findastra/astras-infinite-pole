// Claude, 2026-10-04. Menu: Astra > Claude > 30 Bigger tab menu, video skip, smooth cloud furniture
// Finishes Astra's menu mark-up and the cloud lounge revamp on top of rounds 26-27 (another Claude chat started those):
//  1. Menu: CLOUD SPARKLE RAIN (switch + effect), AMBIENT PREVIEW and the "Only you see these changes" note are removed.
//     Every tab is laid out again with bigger text and taller rows, keeping the same grouping (label over slider, pairs
//     side by side). Tab buttons get their colours baked in (they were plain white in the editor). STAIR COLOR shows a
//     rainbow bar so it is obvious what it does, NEW SPELL becomes a big NEW SPELL PATTERN button, and BODY STARDUST /
//     TOUCH MAGIC / SWIRL ENERGY get full-size rows. The panel grows to fit its tallest tab.
//  2. Video: skip back « and skip forward » buttons next to play/pause (AstraVideoSkip.cs, playlist prev / next).
//  3. Cloud furniture: the puffs of each piece of furniture are melted into ONE smooth cloud surface (a metaball /
//     surface-nets mesh built from the puffs), so a sofa looks like one soft cloud instead of a pile of dots. The old
//     puffs stay in the scene with their renderers off (rerun or the backup restores them). Colliders untouched.
//  4. Pole: the crystal dots are gone (InfinitePole.shader) and the CRYSTAL SPARKLE slider is hidden (it only drove them).
//  5. DJ booth: the cloud no longer cuts through the booth - every cloud surface inside the booth footprint is pressed
//     down under the booth (carved copies of the cloud meshes), so the booth sits in a cloud hollow.
//  6. Photos: Review/Photos/Round30/.
// Safe to rerun. Backup: Review/Backups/BeforeRound30.unity.txt. Report: Review/claude-round30-report.txt.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeRound30
{
    const string Root = "Assets/Astra/";
    static List<string> report;
    static Font font;

    [MenuItem("Astra/Claude/30 Bigger tab menu, video skip, smooth cloud furniture")]
    public static void Run()
    {
        report = new List<string>();
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Review/Backups"); File.Copy(scene.path, "Review/Backups/BeforeRound30.unity.txt", true);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Step("remove crossed-out items", RemoveCrossedOut);
            Step("pole dots", PoleDots);
            Step("menu layout", Layout);
            Step("DJ booth clipping", DJCarve);
            Step("video skip", VideoSkip);
            Step("furniture", Furniture);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Step("photos", Photos);
        }
        catch (Exception e) { report.Add("FAILED: " + e); }
        File.WriteAllText("Review/claude-round30-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND30 " + string.Join(" | ", report));
    }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("STEP FAILED (" + name + "): " + e.Message + "\n" + e.StackTrace); } }
    static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }

    static RectTransform Panel()
    {
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        return menu == null ? null : menu.menuRoot.Find("Astra menu") as RectTransform;
    }
    static AstraMenuTabs Tabs(RectTransform panel) { return panel == null ? null : panel.GetComponentInChildren<AstraMenuTabs>(true); }
    static string Label(Transform t) { var x = t.GetComponentInChildren<Text>(true); return x != null ? x.text : ""; }

    // ================================================================= 1. crossed-out items
    static void RemoveCrossedOut()
    {
        var panel = Panel(); if (panel == null) { report.Add("menu: \"Astra menu\" panel not found"); return; }
        var all = panel.GetComponentsInChildren<RectTransform>(true);

        // CLOUD SPARKLE RAIN: the switch and whatever it switched
        foreach (var tg in all.Select(r => r.GetComponent<Toggle>()).Where(t => t != null && (t.name == "CLOUD SPARKLE RAIN" || Label(t.transform) == "CLOUD SPARKLE RAIN")).ToList())
        {
            var removed = new List<string>();
            foreach (var wi in Object.FindObjectsOfType<AstraWorldItems>(true))
            {
                int idx = Array.IndexOf(wi.objectToggles, tg); if (idx < 0) continue;
                var target = wi.objects[idx];
                wi.objects = wi.objects.Where((o, i) => i != idx).ToArray();
                wi.objectToggles = wi.objectToggles.Where((o, i) => i != idx).ToArray();
                if (target != null) { removed.Add(PathOf(target.transform)); Object.DestroyImmediate(target); }
                UdonSharpEditorUtility.CopyProxyToUdon(wi);
            }
            foreach (var os in Object.FindObjectsOfType<AstraObjectSwitch>(true).Where(o => o.toggle == tg).ToList())
            {
                if (os.targets != null) foreach (var g in os.targets) if (g != null) { removed.Add(PathOf(g.transform)); Object.DestroyImmediate(g); }
                Object.DestroyImmediate(os.gameObject);
            }
            Object.DestroyImmediate(tg.gameObject);
            report.Add("removed CLOUD SPARKLE RAIN switch" + (removed.Count > 0 ? " and its effect: " + string.Join(", ", removed) : " (no effect object was linked to it)"));
        }
        // AMBIENT PREVIEW button and the "only you see" note (most switches are shared now, so it was wrong too)
        foreach (var r in panel.GetComponentsInChildren<RectTransform>(true).Where(r => r != null && (r.name.StartsWith("AMBIENT PREVIEW") || r.name == "World note" || Label(r).StartsWith("Only you see"))).ToList())
        {
            if (r == null) continue;
            report.Add("removed " + r.name); Object.DestroyImmediate(r.gameObject);
        }
    }

    // ================================================================= 2. bigger layout
    const float RowGap = 14f, ToggleH = 66f, ButtonH = 72f, LabelH = 40f, SliderH = 46f;
    const int ToggleFont = 28, ButtonFont = 28, LabelFont = 27, NoteFont = 22;
    enum Kind { Toggle, Button, Slider, Text, Other }
    static Kind KindOf(RectTransform r)
    {
        if (r.GetComponent<Toggle>() != null) return Kind.Toggle;
        if (r.GetComponent<Button>() != null) return Kind.Button;
        if (r.GetComponent<Slider>() != null) return Kind.Slider;
        if (r.GetComponent<Text>() != null) return Kind.Text;
        return Kind.Other;
    }
    static void Fonts(RectTransform r, int size, float w)
    {
        foreach (var t in r.GetComponentsInChildren<Text>(true))
        {
            t.font = font; t.fontSize = size; t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = size;
            t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            if (t.color.a < .99f) t.color = new Color(t.color.r, t.color.g, t.color.b, 1f);
            var trt = t.GetComponent<RectTransform>();
            if (trt != r) { trt.anchorMin = trt.anchorMax = new Vector2(.5f, .5f); trt.anchoredPosition = new Vector2(KindOf(r) == Kind.Toggle ? 18f : 0f, 0f); trt.sizeDelta = new Vector2(w - (KindOf(r) == Kind.Toggle ? 90f : 30f), Mathf.Max(size + 12f, r.sizeDelta.y - 8f)); }
            EditorUtility.SetDirty(t);
        }
    }
    static void Place(RectTransform r, float x, float yTop, float w, float h, int fontSize)
    {
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = new Vector2(x, yTop - h / 2f); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one;
        var k = KindOf(r);
        if (k == Kind.Slider)
        {
            var s = r.GetComponent<Slider>();
            if (s.handleRect != null) { s.handleRect.sizeDelta = new Vector2(34f, s.handleRect.sizeDelta.y); EditorUtility.SetDirty(s.handleRect); }
        }
        else Fonts(r, fontSize, w);
        var mark = r.Find("Crystal switch") as RectTransform;
        if (mark != null) { mark.anchorMin = mark.anchorMax = new Vector2(.5f, .5f); mark.anchoredPosition = new Vector2(-w / 2f + 34f, 0); mark.sizeDelta = new Vector2(28, 28); EditorUtility.SetDirty(mark); }
        EditorUtility.SetDirty(r);
    }
    // one column: elements top to bottom; a Text directly followed by a Slider is a label/slider pair;
    // elements sharing a y (within 12 units) in a full-width column sit side by side
    static float LayColumn(List<RectTransform> col, float x, float w, float top)
    {
        float y = top;
        var rows = new List<List<RectTransform>>();
        foreach (var r in col.OrderByDescending(r => r.anchoredPosition.y).ThenBy(r => r.anchoredPosition.x))
        {
            if (rows.Count > 0 && Mathf.Abs(rows[rows.Count - 1][0].anchoredPosition.y - r.anchoredPosition.y) < 12f) rows[rows.Count - 1].Add(r);
            else rows.Add(new List<RectTransform> { r });
        }
        foreach (var row in rows)
        {
            float rowH = 0f; int n = row.Count; float cw = (w - (n - 1) * 20f) / n;
            for (int i = 0; i < n; i++)
            {
                var r = row[i]; var k = KindOf(r); float cx = x - w / 2f + cw / 2f + i * (cw + 20f);
                float h = k == Kind.Toggle ? ToggleH : k == Kind.Button ? ButtonH : k == Kind.Slider ? SliderH : k == Kind.Text ? TextH(r) : r.sizeDelta.y;
                int f = k == Kind.Toggle ? ToggleFont : k == Kind.Button ? ButtonFont : k == Kind.Text ? TextFont(r) : 24;
                if (k == Kind.Other) { r.anchoredPosition = new Vector2(cx, y - h / 2f); EditorUtility.SetDirty(r); }
                else Place(r, cx, y, cw, h, f);
                rowH = Mathf.Max(rowH, h);
            }
            bool labelOverSlider = n == 1 && KindOf(row[0]) == Kind.Text && IsSliderLabel(row, rows);
            y -= rowH + (labelOverSlider ? 2f : RowGap);
        }
        return y;
    }
    static bool IsSliderLabel(List<RectTransform> row, List<List<RectTransform>> rows)
    {
        int i = rows.IndexOf(row); return i >= 0 && i + 1 < rows.Count && rows[i + 1].Count == 1 && KindOf(rows[i + 1][0]) == Kind.Slider;
    }
    static float TextH(RectTransform r) { var t = r.GetComponent<Text>(); int lines = t.text.Split('\n').Length; return lines > 1 ? 34f * lines + 8f : (t.text.Length > 50 ? 34f : LabelH); }
    static int TextFont(RectTransform r) { var t = r.GetComponent<Text>(); return t.text.Contains("\n") || t.text.Length > 40 ? NoteFont : LabelFont; }

    static Sprite RainbowSprite()
    {
        string path = Root + "Textures/Menu Rainbow.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(256, 4);
            for (int x = 0; x < 256; x++) { var c = Color.HSVToRGB(Mathf.Repeat(.92f + x / 255f, 1f), .75f, 1f); for (int y = 0; y < 4; y++) tex.SetPixel(x, y, c); }
            Directory.CreateDirectory(Root + "Textures"); File.WriteAllBytes(path, tex.EncodeToPNG()); AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.textureType = TextureImporterType.Sprite; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = false; ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Layout()
    {
        var panel = Panel(); var tabs = Tabs(panel);
        if (panel == null || tabs == null) { report.Add("menu: panel or tabs not found"); return; }
        float W = panel.sizeDelta.x, header = 215f;
        // tab colours baked in, bigger labels
        for (int i = 0; i < tabs.tabBacks.Length; i++)
        {
            if (tabs.tabBacks[i] != null) { tabs.tabBacks[i].color = i == 0 ? tabs.onBack : tabs.offBack; EditorUtility.SetDirty(tabs.tabBacks[i]); }
            if (tabs.tabLabels[i] != null) { tabs.tabLabels[i].color = i == 0 ? tabs.onText : tabs.offText; tabs.tabLabels[i].fontSize = 32; EditorUtility.SetDirty(tabs.tabLabels[i]); }
        }
        var pages = tabs.pages.Where(p => p != null).Select(p => (RectTransform)p.transform).ToArray();

        // clearer controls on MAGIC
        var magic = pages.FirstOrDefault(p => p.name.EndsWith("MAGIC"));
        if (magic != null)
        {
            var ns = magic.Cast<Transform>().FirstOrDefault(t => t.name == "NEW SPELL" && t.GetComponent<Button>() != null);
            if (ns != null) { var t = ns.GetComponentInChildren<Text>(true); if (t != null) { t.text = "NEW SPELL PATTERN"; EditorUtility.SetDirty(t); } var img = ns.GetComponent<Image>(); if (img != null) { img.color = new Color(.42f, .12f, .36f); EditorUtility.SetDirty(img); } }
            var stair = magic.Cast<Transform>().Select(t => t.GetComponent<Slider>()).FirstOrDefault(s => s != null && s.name.StartsWith("STAIR COLOR"));
            var lab = magic.Cast<Transform>().FirstOrDefault(t => t.GetComponent<Text>() != null && t.GetComponent<Slider>() == null && t.GetComponent<Text>().text.StartsWith("STAIR COLOR"));
            if (lab != null) { var t = lab.GetComponent<Text>(); t.text = "STAIR COLOR  (slide for any color)"; EditorUtility.SetDirty(t); }
            if (stair != null)
            {
                var bg = stair.transform.Find("Background"); var sp = RainbowSprite();
                if (bg != null && sp != null) { var img = bg.GetComponent<Image>(); img.sprite = sp; img.type = Image.Type.Simple; img.color = Color.white; var br = (RectTransform)bg; br.anchorMin = new Vector2(0, .15f); br.anchorMax = new Vector2(1, .85f); EditorUtility.SetDirty(img); }
                if (stair.fillRect != null) { var fi = stair.fillRect.GetComponent<Image>(); if (fi != null) { fi.color = new Color(1, 1, 1, 0); EditorUtility.SetDirty(fi); } }
                report.Add("magic: STAIR COLOR slider shows a rainbow bar");
            }
        }

        float lowest = 0f;
        foreach (var page in pages)
        {
            var all = page.Cast<Transform>().OfType<RectTransform>().Where(r => r.gameObject != null && r.gameObject.activeSelf).ToList();
            var swatches = all.Where(r => r.name.EndsWith("swatch")).ToList();
            var kids = all.Except(swatches).ToList();
            var left = kids.Where(r => r.anchoredPosition.x < -100f).ToList();
            var right = kids.Where(r => r.anchoredPosition.x > 100f).ToList();
            var centre = kids.Except(left).Except(right).ToList();
            float top = page.sizeDelta.y / 2f - header;
            float endY;
            if (left.Count + right.Count > 0 && centre.Count <= 2)
            {
                float colW = (W - 100f) / 2f;
                float yl = LayColumn(left, -W / 4f + 12f, colW, top), yr = LayColumn(right, W / 4f - 12f, colW, top);
                endY = Mathf.Min(yl, yr);
                if (centre.Count > 0) endY = LayColumn(centre, 0, W - 120f, endY - 10f);
            }
            else
            {
                // single column page (SKY): pairs that sat side by side stay side by side
                foreach (var r in left.Concat(right)) centre.Add(r);
                endY = LayColumn(centre, 0, W - 140f, top);
            }
            // colour swatches ride at the right end of their slider's label
            foreach (var sw in swatches)
            {
                string key = sw.name.Contains("A") && !sw.name.Contains("B") ? "COLOR A" : "COLOR B";
                var lab = kids.FirstOrDefault(r => r.GetComponent<Text>() != null && r.GetComponent<Slider>() == null && r.GetComponent<Text>().text.Contains(key));
                if (lab == null) continue;
                sw.anchorMin = sw.anchorMax = new Vector2(.5f, .5f); sw.sizeDelta = new Vector2(40, 30);
                sw.anchoredPosition = new Vector2(lab.anchoredPosition.x + lab.sizeDelta.x / 2f - 30f, lab.anchoredPosition.y); EditorUtility.SetDirty(sw);
            }
            lowest = Mathf.Min(lowest, endY - top);
            report.Add("menu: " + page.name + " laid out, " + kids.Count + " items, " + (top - endY).ToString("0") + " tall");
        }

        // panel height = header + tallest page + margin; keep the header pinned to the top edge
        float oldH = panel.sizeDelta.y, newH = Mathf.Round(header + (-lowest) + 60f);
        float dyTop = (newH - oldH) / 2f;
        foreach (var page in pages) { page.sizeDelta = new Vector2(W, newH); foreach (var c in page.Cast<Transform>().OfType<RectTransform>()) { c.anchoredPosition += new Vector2(0, dyTop); EditorUtility.SetDirty(c); } }
        foreach (var c in panel.Cast<Transform>().OfType<RectTransform>())
        {
            if (pages.Contains(c)) continue;
            if (c.name == "Opaque backing") { c.sizeDelta = new Vector2(W, newH); EditorUtility.SetDirty(c); continue; }
            c.anchoredPosition += new Vector2(0, dyTop); EditorUtility.SetDirty(c);
            foreach (var t in c.GetComponentsInChildren<Text>(true)) { if (t.text.StartsWith("ASTRA")) t.fontSize = 44; else if (t.text.StartsWith("CLOSE")) t.fontSize = 30; EditorUtility.SetDirty(t); }
        }
        panel.sizeDelta = new Vector2(W, newH);
        var box = panel.GetComponent<BoxCollider>(); if (box != null) { box.size = new Vector3(W, newH, 1); EditorUtility.SetDirty(box); }
        // move the panel up by the growth so its bottom edge stays above the video bar
        panel.localPosition += new Vector3(0, dyTop * panel.localScale.y, 0);
        UdonSharpEditorUtility.CopyProxyToUdon(tabs);
        report.Add("menu: panel " + oldH + " -> " + newH + " tall (" + (newH * panel.localScale.y).ToString("0.00") + " m), fonts 27-32");
    }

    // ================================================================= 3. video skip buttons
    static void VideoSkip()
    {
        var vp = Object.FindObjectOfType<UdonSharp.Video.USharpVideoPlayer>(true);
        var play = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b => b.gameObject.scene.IsValid() && b.name == "PlayButton");
        if (vp == null || play == null) { report.Add("video: player or PlayButton not found"); return; }
        EnsureProgram("AstraVideoSkip");
        var holder = vp.transform.Find("Video skip (Claude)");
        if (holder == null) { holder = new GameObject("Video skip (Claude)").transform; holder.SetParent(vp.transform, false); }
        var skip = holder.GetComponent<AstraVideoSkip>(); if (skip == null) skip = holder.gameObject.AddUdonSharpComponent<AstraVideoSkip>();
        skip.player = vp; UdonSharpEditorUtility.CopyProxyToUdon(skip);
        var u = UdonSharpEditorUtility.GetBackingUdonBehaviour(skip);
        var bar = (RectTransform)play.transform.parent;
        var pr = (RectTransform)play.transform; float bw = pr.sizeDelta.x, gap = 6f;
        var slider = bar.GetComponentsInChildren<Slider>(true).FirstOrDefault(s => s.transform.parent == bar);
        bool fresh = bar.Find("Skip back") == null;
        foreach (var spec in new[] { new { n = "Skip back", g = "«", e = "Previous", i = 1 }, new { n = "Skip forward", g = "»", e = "Next", i = 2 } })
        {
            var old = bar.Find(spec.n); if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(spec.n, typeof(RectTransform), typeof(Image), typeof(Button)); var r = (RectTransform)go.transform; r.SetParent(bar, false);
            r.anchorMin = pr.anchorMin; r.anchorMax = pr.anchorMax; r.pivot = pr.pivot; r.sizeDelta = pr.sizeDelta; r.anchoredPosition = pr.anchoredPosition + new Vector2(spec.i * (bw + gap), 0);
            var img = go.GetComponent<Image>(); var pimg = play.GetComponent<Image>(); img.sprite = pimg != null ? pimg.sprite : null; img.type = pimg != null ? pimg.type : Image.Type.Simple; img.color = pimg != null ? pimg.color : new Color(.2f, .2f, .25f);
            var b = go.GetComponent<Button>(); b.targetGraphic = img; b.colors = play.colors;
            var lr = new GameObject("Label", typeof(RectTransform), typeof(Text)); var ltr = (RectTransform)lr.transform; ltr.SetParent(r, false); ltr.anchorMin = Vector2.zero; ltr.anchorMax = Vector2.one; ltr.sizeDelta = Vector2.zero;
            var t = lr.GetComponent<Text>(); t.font = font; t.text = spec.g; t.fontSize = Mathf.RoundToInt(bw * .8f); t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            UnityEventTools.AddStringPersistentListener(b.onClick, u.SendCustomEvent, spec.e);
        }
        if (fresh && slider != null)       // make room: the seek bar starts after the two new buttons
        {
            var sr = (RectTransform)slider.transform; float shift = 2 * (bw + gap);
            sr.anchoredPosition += new Vector2(shift / 2f, 0); sr.sizeDelta -= new Vector2(shift, 0); EditorUtility.SetDirty(sr);
        }
        report.Add("video: skip back « / skip forward » added next to play/pause (playlist of " + vp.playlist.Length + ")");
    }

    // ================================================================= 4. smooth cloud furniture (surface nets over metaballs)
    struct Puff { public Matrix4x4 toPuff; public Vector3 c, e; }
    const float R = 1.6f, Iso = .45f;
    static bool IsPuff(Renderer r) { var mf = r.GetComponent<MeshFilter>(); return mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Glitter Cloud") && !r.name.StartsWith("Merged"); }
    static float Field(List<Puff> ps, Vector3 p)
    {
        float s = 0f;
        for (int i = 0; i < ps.Count; i++)
        {
            Vector3 q = ps[i].toPuff.MultiplyPoint3x4(p) - ps[i].c;
            float x = q.x / ps[i].e.x, y = q.y / ps[i].e.y, z = q.z / ps[i].e.z;
            float d = (x * x + y * y + z * z) / (R * R);
            if (d < 1f) { float k = 1f - d; s += k * k; }
        }
        return s;
    }
    static void Furniture()
    {
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge == null) { report.Add("furniture: cloud lounge not found"); return; }
        Directory.CreateDirectory(Root + "Meshes/Lounge/Merged");
        int groups = 0, srcPuffs = 0; long verts = 0, srcVerts = 0;
        foreach (var furn in lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Furniture")).ToList())
        {
            foreach (var g in furn.GetComponentsInChildren<Renderer>(true).Where(IsPuff).GroupBy(r => r.transform.parent).ToList())
            {
                var rs = g.ToList(); if (rs.Count < 2) continue;
                var parent = g.Key;
                var old = parent.Find("Merged cloud shape"); if (old != null) Object.DestroyImmediate(old.gameObject);
                var ps = new List<Puff>(); var bmin = Vector3.one * float.MaxValue; var bmax = -bmin; float minExt = float.MaxValue;
                foreach (var r in rs)
                {
                    var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds;
                    var toParent = parent.worldToLocalMatrix * r.transform.localToWorldMatrix;
                    ps.Add(new Puff { toPuff = toParent.inverse, c = mb.center, e = new Vector3(Mathf.Max(mb.extents.x, 1e-3f), Mathf.Max(mb.extents.y, 1e-3f), Mathf.Max(mb.extents.z, 1e-3f)) });
                    for (int k = 0; k < 8; k++)
                    {
                        var corner = mb.center + Vector3.Scale(mb.extents * R, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                        var pp = toParent.MultiplyPoint3x4(corner); bmin = Vector3.Min(bmin, pp); bmax = Vector3.Max(bmax, pp);
                    }
                    var ws = toParent.lossyScaleApprox(mb.extents); minExt = Mathf.Min(minExt, ws);
                    srcVerts += r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                }
                var size = bmax - bmin; float h = Mathf.Max(Mathf.Max(size.x, Mathf.Max(size.y, size.z)) / 64f, minExt / 2.2f, .02f);
                var mesh = Build(ps, bmin - Vector3.one * h, size + Vector3.one * 2 * h, h);
                if (mesh == null || mesh.vertexCount == 0) { report.Add("furniture: CHECK " + PathOf(parent) + " made no surface; left as it was"); continue; }
                string safe = string.Join("_", PathOf(parent).Replace("13 - Cloud lounge (Claude)/", "").Split(Path.GetInvalidFileNameChars())).Replace('/', '-');
                string path = Root + "Meshes/Lounge/Merged/" + safe + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                mesh.name = "Merged cloud " + safe; AssetDatabase.CreateAsset(mesh, path);
                var mat = rs.GroupBy(r => r.sharedMaterial).OrderByDescending(x => x.Count()).First().Key;
                var go = new GameObject("Merged cloud shape"); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                foreach (var r in rs) { r.enabled = false; EditorUtility.SetDirty(r); }
                groups++; srcPuffs += rs.Count; verts += mesh.vertexCount;
            }
        }
        report.Add("furniture: " + groups + " pieces melted into smooth clouds from " + srcPuffs + " puffs; vertices " + srcVerts + " (puffs) -> " + verts + " (smooth)");
    }
    static float lossyScaleApprox(this Matrix4x4 m, Vector3 ext)
    {
        return Mathf.Min(m.MultiplyVector(new Vector3(ext.x, 0, 0)).magnitude, Mathf.Min(m.MultiplyVector(new Vector3(0, ext.y, 0)).magnitude, m.MultiplyVector(new Vector3(0, 0, ext.z)).magnitude));
    }
    static Mesh Build(List<Puff> ps, Vector3 o, Vector3 size, float h)
    {
        int nx = Mathf.CeilToInt(size.x / h), ny = Mathf.CeilToInt(size.y / h), nz = Mathf.CeilToInt(size.z / h);
        if ((long)(nx + 1) * (ny + 1) * (nz + 1) > 6000000) { float k = Mathf.Pow((nx + 1f) * (ny + 1f) * (nz + 1f) / 6000000f, 1f / 3f); h *= k; nx = Mathf.CeilToInt(size.x / h); ny = Mathf.CeilToInt(size.y / h); nz = Mathf.CeilToInt(size.z / h); }
        int sx = nx + 1, sy = ny + 1, sz = nz + 1;
        var f = new float[sx * sy * sz];
        // splat each puff into the corners inside its own box only
        foreach (var p in ps)
        {
            var inv = p.toPuff.inverse; var lo = Vector3.one * float.MaxValue; var hi = -lo;
            for (int k = 0; k < 8; k++) { var c = p.c + Vector3.Scale(p.e * R, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)); var w = inv.MultiplyPoint3x4(c); lo = Vector3.Min(lo, w); hi = Vector3.Max(hi, w); }
            int x0 = Mathf.Clamp(Mathf.FloorToInt((lo.x - o.x) / h), 0, nx), x1 = Mathf.Clamp(Mathf.CeilToInt((hi.x - o.x) / h), 0, nx);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((lo.y - o.y) / h), 0, ny), y1 = Mathf.Clamp(Mathf.CeilToInt((hi.y - o.y) / h), 0, ny);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((lo.z - o.z) / h), 0, nz), z1 = Mathf.Clamp(Mathf.CeilToInt((hi.z - o.z) / h), 0, nz);
            for (int z = z0; z <= z1; z++) for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                    {
                        var q = p.toPuff.MultiplyPoint3x4(o + new Vector3(x * h, y * h, z * h)) - p.c;
                        float a = q.x / p.e.x, b = q.y / p.e.y, c = q.z / p.e.z, d = (a * a + b * b + c * c) / (R * R);
                        if (d < 1f) { float k = 1f - d; f[(z * sy + y) * sx + x] += k * k; }
                    }
        }
        Func<int, int, int, float> F = (x, y, z) => f[(z * sy + y) * sx + x];
        var cellVert = new int[nx * ny * nz]; for (int i = 0; i < cellVert.Length; i++) cellVert[i] = -1;
        var vs = new List<Vector3>();
        int[,] edges = { { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 }, { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        var cv = new float[8]; var cp = new Vector3[8];
        for (int z = 0; z < nz; z++) for (int y = 0; y < ny; y++) for (int x = 0; x < nx; x++)
                {
                    int mask = 0;
                    for (int k = 0; k < 8; k++) { int dx = k & 1, dy = (k >> 1) & 1, dz = (k >> 2) & 1; cv[k] = F(x + dx, y + dy, z + dz); cp[k] = new Vector3(x + dx, y + dy, z + dz); if (cv[k] > Iso) mask |= 1 << k; }
                    if (mask == 0 || mask == 255) continue;
                    var sum = Vector3.zero; int cnt = 0;
                    for (int e = 0; e < 12; e++)
                    {
                        int a = edges[e, 0], b = edges[e, 1];
                        if ((cv[a] > Iso) == (cv[b] > Iso)) continue;
                        float t = (Iso - cv[a]) / (cv[b] - cv[a]); sum += Vector3.Lerp(cp[a], cp[b], t); cnt++;
                    }
                    cellVert[(z * ny + y) * nx + x] = vs.Count; vs.Add(o + (sum / cnt) * h);
                }
        var tris = new List<int>();
        Func<int, int, int, int> CV = (x, y, z) => (x < 0 || y < 0 || z < 0 || x >= nx || y >= ny || z >= nz) ? -1 : cellVert[(z * ny + y) * nx + x];
        Action<int, int, int, int, Vector3> Quad = (a, b, c, d, outward) =>
        {
            if (a < 0 || b < 0 || c < 0 || d < 0) return;
            var n = Vector3.Cross(vs[b] - vs[a], vs[c] - vs[a]);
            if (Vector3.Dot(n, outward) >= 0) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(a); tris.Add(c); tris.Add(d); }
            else { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(a); tris.Add(d); tris.Add(c); }
        };
        for (int z = 0; z <= nz; z++) for (int y = 0; y <= ny; y++) for (int x = 0; x <= nx; x++)
                {
                    bool inside = F(x, y, z) > Iso;
                    if (x < nx && inside != (F(x + 1, y, z) > Iso)) Quad(CV(x, y - 1, z - 1), CV(x, y, z - 1), CV(x, y, z), CV(x, y - 1, z), inside ? Vector3.right : Vector3.left);
                    if (y < ny && inside != (F(x, y + 1, z) > Iso)) Quad(CV(x - 1, y, z - 1), CV(x, y, z - 1), CV(x, y, z), CV(x - 1, y, z), inside ? Vector3.up : Vector3.down);
                    if (z < nz && inside != (F(x, y, z + 1) > Iso)) Quad(CV(x - 1, y - 1, z), CV(x, y - 1, z), CV(x, y, z), CV(x - 1, y, z), inside ? Vector3.forward : Vector3.back);
                }
        // smooth normals from the field gradient (the field grows inward, so the normal is minus the gradient)
        var ns = new Vector3[vs.Count]; float e2 = h * .5f;
        for (int i = 0; i < vs.Count; i++)
        {
            var p = vs[i];
            var gr = new Vector3(Field(ps, p + new Vector3(e2, 0, 0)) - Field(ps, p - new Vector3(e2, 0, 0)), Field(ps, p + new Vector3(0, e2, 0)) - Field(ps, p - new Vector3(0, e2, 0)), Field(ps, p + new Vector3(0, 0, e2)) - Field(ps, p - new Vector3(0, 0, e2)));
            ns[i] = gr.sqrMagnitude > 1e-12f ? -gr.normalized : Vector3.up;
        }
        var m = new Mesh { indexFormat = vs.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        m.SetVertices(vs); m.SetNormals(ns); m.SetTriangles(tris, 0); m.RecalculateBounds();
        return m;
    }

    // ================================================================= 5. photos (nothing saved)
    static void Photos()
    {
        string dir = "Review/Photos/Round30"; Directory.CreateDirectory(dir);
        var panel = Panel(); var tabs = Tabs(panel);
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        if (panel != null && tabs != null && menu != null)
        {
            bool was = menu.menuRoot.gameObject.activeSelf; menu.menuRoot.gameObject.SetActive(true);
            var states = tabs.pages.Select(p => p.activeSelf).ToArray();
            var c = new Vector3[4]; panel.GetWorldCorners(c); var centre = (c[0] + c[2]) * .5f; float hgt = Vector3.Distance(c[0], c[1]);
            // include the video bar under the panel in the frame
            var lookAt = centre + Vector3.down * hgt * .12f; float fov = 50f; float d = hgt * .78f / Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
            for (int i = 0; i < tabs.pages.Length; i++)
            {
                for (int k = 0; k < tabs.pages.Length; k++) tabs.pages[k].SetActive(k == i);
                for (int k = 0; k < tabs.tabBacks.Length; k++) { tabs.tabBacks[k].color = k == i ? tabs.onBack : tabs.offBack; tabs.tabLabels[k].color = k == i ? tabs.onText : tabs.offText; }
                Shot(dir, (i + 1) + " Menu - " + tabs.pages[i].name.Replace("Page ", ""), lookAt - panel.forward * d, lookAt, fov, 1100, 1500);
            }
            for (int k = 0; k < tabs.pages.Length; k++) tabs.pages[k].SetActive(states[k]);
            for (int k = 0; k < tabs.tabBacks.Length; k++) { tabs.tabBacks[k].color = k == 0 ? tabs.onBack : tabs.offBack; tabs.tabLabels[k].color = k == 0 ? tabs.onText : tabs.offText; }
            menu.menuRoot.gameObject.SetActive(was);
        }
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge != null)
        {
            int n = 0;
            foreach (var name in new[] { "Cloud sofa x1.9", "Cloud bed x1.5", "Giant chair", "Tea lounge", "Cloud chaise x1.0" })
            {
                var t = lounge.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name); if (t == null) continue;
                var rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var outw = new Vector3(b.center.x, 0, b.center.z).normalized; if (outw.sqrMagnitude < .01f) outw = Vector3.forward;
                float dist = b.extents.magnitude * 1.6f;
                Shot(dir, (5 + n++) + " " + name, b.center + outw * dist + Vector3.up * b.extents.y * 1.2f, b.center, 50, 1600, 1000);
            }
        }
        report.Add("photos: " + dir);
    }
    static void Shot(string dir, string name, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude photo camera"); var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 350f;
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
    // ================================================================= pole dots
    static void PoleDots()
    {
        var mg = Object.FindObjectOfType<AstraMagic>(true);
        if (mg == null || mg.poleSparkle == null) { report.Add("pole: CRYSTAL SPARKLE slider not found (shader change still removes the dots)"); return; }
        mg.poleSparkle.gameObject.SetActive(false);
        var page = mg.poleSparkle.transform.parent;
        foreach (Transform t in page) if (t.GetComponent<Text>() != null && t.GetComponent<Slider>() == null && t.GetComponent<Text>().text.StartsWith("CRYSTAL SPARKLE")) t.gameObject.SetActive(false);
        report.Add("pole: crystal dots removed from the pole shader; CRYSTAL SPARKLE slider hidden (kept inactive so the magic script still finds it)");
    }

    // ================================================================= DJ booth clipping
    static void DJCarve()
    {
        var dj = GameObject.Find("12 - DJ cloud"); var booth = dj != null ? dj.transform.Find("DJ booth") : null;
        if (booth == null) { report.Add("DJ: booth not found"); return; }
        // the booth's box in its own space, from every renderer under it (speakers, console, decks)
        var rs = booth.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer) && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null).ToList();
        var lo = Vector3.one * float.MaxValue; var hi = -lo;
        foreach (var r in rs)
        {
            var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds; var m = booth.worldToLocalMatrix * r.transform.localToWorldMatrix;
            for (int k = 0; k < 8; k++) { var c = mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)); var p = m.MultiplyPoint3x4(c); lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
        }
        float margin = .14f / Mathf.Max(.01f, booth.lossyScale.x);
        var footLo = lo - new Vector3(margin, 0, margin); var footHi = hi + new Vector3(margin, 0, margin);
        float floorY = lo.y - .02f / Mathf.Max(.01f, booth.lossyScale.y);
        Directory.CreateDirectory(Root + "Meshes/Lounge/DJ carved");
        int meshes = 0, moved = 0;
        foreach (var mf in dj.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.transform.IsChildOf(booth) || mf.sharedMesh == null) continue;
            var mr = mf.GetComponent<MeshRenderer>(); if (mr == null || mr.sharedMaterial == null || mr.sharedMaterial.shader == null || mr.sharedMaterial.shader.name != "Astra/Glitter Cloud") continue;
            var toB = booth.worldToLocalMatrix * mf.transform.localToWorldMatrix; var toM = toB.inverse;
            var v = mf.sharedMesh.vertices; int hit = 0;
            for (int i = 0; i < v.Length; i++)
            {
                var p = toB.MultiplyPoint3x4(v[i]);
                if (p.x > footLo.x && p.x < footHi.x && p.z > footLo.z && p.z < footHi.z && p.y > floorY) { p.y = floorY; v[i] = toM.MultiplyPoint3x4(p); hit++; }
            }
            if (hit == 0) continue;
            string src = AssetDatabase.GetAssetPath(mf.sharedMesh);
            var copy = Object.Instantiate(mf.sharedMesh); copy.vertices = v; copy.RecalculateBounds();
            string path = Root + "Meshes/Lounge/DJ carved/" + string.Join("_", (mf.name + " " + mf.GetInstanceID()).Split(Path.GetInvalidFileNameChars())) + ".asset";
            if (mf.sharedMesh.name.StartsWith("DJ carved")) path = src;                                  // rerun: update the carved copy in place
            copy.name = "DJ carved " + mf.name;
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(copy, path); mf.sharedMesh = copy; EditorUtility.SetDirty(mf);
            var mc = mf.GetComponent<MeshCollider>(); if (mc != null) { mc.sharedMesh = copy; EditorUtility.SetDirty(mc); }
            meshes++; moved += hit;
        }
        report.Add("DJ: " + moved + " cloud vertices inside the booth footprint pressed under the booth, in " + meshes + " cloud meshes (booth box " + (hi - lo).ToString("0.00") + " local)");
    }

    static void EnsureProgram(string name)
    {
        string path = Root + "Scripts/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) == null)
        {
            var a = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            a.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "Scripts/" + name + ".cs");
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets();
        }
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
    }
}
