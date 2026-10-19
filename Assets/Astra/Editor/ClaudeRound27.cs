// Claude, 2026-10-03. Menu: Astra > Claude > 27 One menu with tabs + connected furniture clouds
//  1. The three floating panels (World items, Celestial observatory, Glitter studio) become ONE panel with four tabs:
//     WORLD / SKY / GLITTER / MAGIC. Every control is MOVED, never rebuilt, so all the existing wiring survives.
//     Bigger fonts and bigger rows throughout; every slider gets a visible name; the rows Astra crossed out are deleted.
//     New: a PINK CLOUD SEA button (the start sky had no way back) and clearer petal names.
//  2. Cloud furniture: puffs inside each "Furniture" group are pulled together and grown until each piece of furniture
//     is one connected shape instead of a scatter of separate blobs. Seats, colliders and platforms are not touched.
//  3. Reports any leftover tree objects or renderers whose mesh went missing.
// Safe to rerun (step 1 is skipped once the new panel exists). Backup: Review/Backups/BeforeRound27.unity.txt.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ClaudeRound27
{
    const string Root = "Assets/Astra/";
    const string MenuName = "Astra menu";
    const float W = 1020f, H = 1560f;                 // panel size in canvas units (x 0.00065 world scale = 0.66 x 1.01 m)
    const float Scale = 0.00065f;
    static List<string> report;
    static readonly List<RectTransform> pool = new List<RectTransform>();   // everything that lived on the old panels
    static Font font;

    [MenuItem("Astra/Claude/27 One menu with tabs + connected furniture clouds")]
    public static void Run()
    {
        report = new List<string>();
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Review/Backups"); File.Copy(scene.path, "Review/Backups/BeforeRound27.unity.txt", true);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Step("menu", Menu);
            Step("furniture", Furniture);
            Step("leftovers", Leftovers);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        catch (Exception e) { report.Add("FAILED: " + e); }
        File.WriteAllText("Review/claude-round27-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND27 " + string.Join(" | ", report));
    }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("STEP FAILED (" + name + "): " + e.Message + "\n" + e.StackTrace); } }
    static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }

    // ================================================================= 1. one menu
    // kind: "text" = a plain label, "ctrl" = Toggle/Button/Slider, "any". Names collide (a Text and a Slider are both
    // called "SWIRL ENERGY"), so the kind decides which one is meant rather than sibling order.
    static bool Is(RectTransform r, string kind)
    {
        bool ctrl = r.GetComponent<Toggle>() != null || r.GetComponent<Button>() != null || r.GetComponent<Slider>() != null;
        return kind == "any" || (kind == "ctrl" ? ctrl : !ctrl);
    }
    static RectTransform Grab(string label, string kind = "any")
    {
        var ok = pool.Where(r => r != null && Is(r, kind)).ToList();
        var hit = ok.FirstOrDefault(r => r.name == label)
               ?? ok.FirstOrDefault(r => { var t = r.GetComponentInChildren<Text>(true); return t != null && t.text == label; });
        if (hit == null) { report.Add("  MISSING (" + kind + "): \"" + label + "\""); return null; }
        pool.Remove(hit); return hit;
    }
    static void SetFont(RectTransform rt, int size, float width)
    {
        foreach (var t in rt.GetComponentsInChildren<Text>(true))
        {
            t.fontSize = size; t.font = font; t.resizeTextForBestFit = false;
            var trt = t.GetComponent<RectTransform>();
            if (trt != rt) { trt.sizeDelta = new Vector2(width - 40f, trt.sizeDelta.y < 10 ? 40f : Mathf.Max(trt.sizeDelta.y, size + 14f)); }
            EditorUtility.SetDirty(t);
        }
    }
    static RectTransform Put(RectTransform rt, Transform page, float x, float y, float w, float h, int fontSize, string newLabel = null)
    {
        if (rt == null) return null;
        rt.SetParent(page, false);
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h); rt.localScale = Vector3.one;
        if (newLabel != null) { var t = rt.GetComponentInChildren<Text>(true); if (t != null) { t.text = newLabel; EditorUtility.SetDirty(t); } }
        SetFont(rt, fontSize, w);
        var mark = rt.Find("Crystal switch") as RectTransform;                 // the diamond tick sits just inside the left edge
        if (mark != null) { mark.anchoredPosition = new Vector2(-w / 2f + 30f, 0); mark.sizeDelta = new Vector2(26, 26); }
        EditorUtility.SetDirty(rt);
        return rt;
    }
    static void PutPair(string labelName, string sliderName, Transform page, float x, float y, float w, int fontSize, string newLabel = null)
    {
        var lab = Grab(labelName, "text"); var sl = Grab(sliderName, "ctrl");
        if (lab != null) Put(lab, page, x, y + 24f, w, 40f, fontSize, newLabel);
        if (sl != null) Put(sl, page, x, y - 20f, w, 26f, fontSize);
    }
    static RectTransform MakeRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = pos; r.sizeDelta = size; return r;
    }
    static Text MakeText(string name, Transform parent, Vector2 pos, Vector2 size, string text, int size2, Color col)
    {
        var r = MakeRect(name, parent, pos, size);
        var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = text; t.fontSize = size2; t.color = col;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    static void Menu()
    {
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        if (menu == null) { report.Add("menu: AstraHandMenu not found"); return; }
        var rootT = menu.menuRoot;
        var oldNames = new[] { "World items", "Celestial observatory", "Glitter studio" };
        var olds = rootT.GetComponentsInChildren<Canvas>(true).Where(c => oldNames.Contains(c.name)).Select(c => c.GetComponent<RectTransform>()).ToList();
        if (olds.Count == 0) { report.Add("menu: the old panels are already gone, so the one-menu rebuild has been done; skipped"); return; }
        if (olds.Count != 3) { report.Add("menu: expected the 3 old panels, found " + olds.Count + " (" + string.Join(", ", olds.Select(o => o.name)) + "); nothing changed"); return; }
        // the old panels still being here means a previous attempt stopped part way; clear whatever it left and start over
        var partial = rootT.Find(MenuName);
        if (partial != null) { Object.DestroyImmediate(partial.gameObject); report.Add("menu: cleared a half-built panel left by an earlier attempt"); }
        pool.Clear();
        // not every child of a Canvas is a RectTransform, so filter rather than cast
        foreach (var o in olds) foreach (Transform c in o) { var r = c as RectTransform; if (r != null) pool.Add(r); else report.Add("menu: left alone (not a UI element): " + c.name); }
        report.Add("menu: " + pool.Count + " elements collected from the three old panels");

        // ---- the new panel
        var src = olds[0];
        var canvasGO = new GameObject(MenuName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var panel = canvasGO.GetComponent<RectTransform>();
        panel.SetParent(rootT, false);
        panel.localPosition = Vector3.zero; panel.localRotation = Quaternion.identity; panel.localScale = Vector3.one * Scale;
        panel.sizeDelta = new Vector2(W, H);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var shapeSrc = src.GetComponent("VRCUiShape");
        if (shapeSrc != null) canvasGO.AddComponent(shapeSrc.GetType()); else report.Add("menu: CHECK - no VRCUiShape on the old panel to copy");
        canvasGO.AddComponent<BoxCollider>().size = new Vector3(W, H, 1);

        var backing = MakeRect("Opaque backing", panel, Vector2.zero, new Vector2(W, H));
        var bimg = backing.gameObject.AddComponent<Image>(); bimg.color = new Color(.05f, .02f, .07f, .97f); bimg.raycastTarget = false;
        backing.SetAsFirstSibling();

        MakeText("Title", panel, new Vector2(-60, H / 2f - 60f), new Vector2(760, 60), "ASTRA'S  INFINITE  POLE", 40, new Color(1f, .45f, .82f));

        // ---- tabs
        EnsureProgram("AstraMenuTabs");
        var tabsGO = new GameObject("Menu tabs"); tabsGO.transform.SetParent(panel, false);
        var tabs = tabsGO.AddUdonSharpComponent<AstraMenuTabs>();
        string[] tabNames = { "WORLD", "SKY", "GLITTER", "MAGIC" };
        var pages = new GameObject[4]; var backs = new Image[4]; var labels = new Text[4];
        float tabW = 232f, tabGap = 246f;
        for (int i = 0; i < 4; i++)
        {
            var r = MakeRect("Tab " + tabNames[i], panel, new Vector2((i - 1.5f) * tabGap, H / 2f - 140f), new Vector2(tabW, 72f));
            backs[i] = r.gameObject.AddComponent<Image>();
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = backs[i];
            labels[i] = MakeText("Label", r, Vector2.zero, new Vector2(tabW, 50), tabNames[i], 28, Color.white);
            var page = new GameObject("Page " + tabNames[i], typeof(RectTransform));
            var prt = page.GetComponent<RectTransform>(); prt.SetParent(panel, false);
            prt.anchorMin = prt.anchorMax = new Vector2(.5f, .5f); prt.pivot = new Vector2(.5f, .5f);
            prt.anchoredPosition = Vector2.zero; prt.sizeDelta = new Vector2(W, H);
            pages[i] = page;
        }
        tabs.pages = pages; tabs.tabBacks = backs; tabs.tabLabels = labels;
        UdonSharpEditorUtility.CopyProxyToUdon(tabs);
        var tabsU = UdonSharpEditorUtility.GetBackingUdonBehaviour(tabs);
        for (int i = 0; i < 4; i++)
        {
            var b = panel.Find("Tab " + tabNames[i]).GetComponent<Button>();
            UnityEventTools.AddStringPersistentListener(b.onClick, tabsU.SendCustomEvent, "Tab" + i);
            EditorUtility.SetDirty(b);
        }

        Transform world = pages[0].transform, sky = pages[1].transform, glit = pages[2].transform, magic = pages[3].transform;
        float top = H / 2f - 215f;

        // ---- WORLD: 16 switches in two columns
        string[] worldRows = { "CHROME POLE", "SPIRAL STAIRS  (you can still walk on them)", "STAR DUST UNDER THE FLOOR", "VIDEO SCREEN",
            "VOID BACKGROUND", "SOFT AVATAR LIGHT", "CLOUD PLATFORMS  (jump on them)", "DJ CLOUD",
            "CLOUD SPARKLE RAIN", "PHONE BOOTHS", "STAGE LIGHTS", "CHERRY PETALS",
            "CLOUD LOUNGE", "STAIR SPARKLES", "OUTER CLOUDS", "RAINBOW FLOW" };
        string[] worldLabels = { "CHROME POLE", "SPIRAL STAIRS", "STAR DUST UNDER THE FLOOR", "VIDEO SCREEN",
            "VOID BACKGROUND", "SOFT AVATAR LIGHT", "CLOUD PLATFORMS", "DJ CLOUD",
            "CLOUD SPARKLE RAIN", "PHONE BOOTHS", "STAGE LIGHTS", "FALLING CHERRY PETALS",
            "CLOUD LOUNGE", "STAIR SPARKLES", "OUTER CLOUDS", "RAINBOW FLOW" };
        for (int i = 0; i < worldRows.Length; i++)
            Put(Grab(worldRows[i], "ctrl"), world, i < 8 ? -250f : 250f, top - (i % 8) * 78f, 470f, 64f, 26, worldLabels[i]);
        Put(Grab("PETALS  /  ON", "ctrl"), world, 0f, top - 8 * 78f - 30f, 470f, 64f, 26, "PETALS AROUND YOU  /  ON");
        MakeText("World note", world, new Vector2(0, top - 8 * 78f - 110f), new Vector2(900, 40),
            "Only you see these changes.", 20, new Color(.70f, .55f, .70f));

        // ---- SKY
        Put(Grab("VELVET NEBULA", "text"), sky, 0, top, 760f, 54f, 30);                      // current-sky label (Text)
        string[] skyBtns = { "PINK CLOUD SEA", "VELVET NEBULA", "ARCTIC AURORA", "QUIET VOID", "MOONLIT SKY" };
        var atm = Object.FindObjectOfType<AstraAtmosphere>(true);
        for (int i = 0; i < skyBtns.Length; i++)
        {
            float y = top - 80f - i * 76f;
            if (i == 0)
            {
                var r = MakeRect("PINK CLOUD SEA", sky, new Vector2(0, y), new Vector2(700f, 64f));
                var img = r.gameObject.AddComponent<Image>(); img.color = new Color(.12f, .035f, .13f);
                var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                MakeText("Label", r, Vector2.zero, new Vector2(700, 50), "PINK CLOUD SEA", 26, new Color(1f, .8f, .93f));
                if (atm != null) UnityEventTools.AddStringPersistentListener(b.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(atm).SendCustomEvent, "PinkCloudSea");
                report.Add("menu: added a PINK CLOUD SEA button (the start sky had no way back)");
                continue;
            }
            Put(Grab(skyBtns[i], "ctrl"), sky, 0, y, 700f, 64f, 26);
        }
        PutPair("SKY BRIGHTNESS", "Sky brightness", sky, 0, top - 500f, 700f, 24);
        Put(Grab("SLOW EVOLUTION  /  ON", "ctrl"), sky, 0, top - 590f, 700f, 64f, 26);
        Put(Grab("HOVER  /  OFF", "ctrl"), sky, -180f, top - 680f, 340f, 64f, 26);
        Put(Grab("PINKSCAPE", "ctrl"), sky, 180f, top - 680f, 340f, 64f, 26);
        Put(Grab("BLOOM  /  ON", "ctrl"), sky, -180f, top - 760f, 340f, 64f, 26);
        Put(Grab("STORMSCAPE  /  OFF", "ctrl"), sky, 180f, top - 760f, 340f, 64f, 26);
        Put(Grab("HANDS: bring together, pause, then pull apart\nRepeat to close  /  Desktop: M", "text"), sky, 0, top - 860f, 900f, 70f, 20);
        Put(Grab("Sunset & moonlit panoramas: Poly Haven / CC0", "text"), sky, 0, top - 930f, 900f, 40f, 17,
            "Moonlit panorama: Poly Haven / CC0");

        // ---- GLITTER
        string[,] glitSliders = { { "DENSITY", "DENSITY slider" }, { "GRADIENT - COLOR A", "GRADIENT - COLOR A slider" },
            { "GRADIENT - COLOR B", "GRADIENT - COLOR B slider" }, { "COLOR SATURATION", "COLOR SATURATION slider" },
            { "GLITTER BRIGHTNESS", "GLITTER BRIGHTNESS slider" }, { "TWINKLE", "TWINKLE slider" } };
        for (int i = 0; i < glitSliders.GetLength(0); i++) PutPair(glitSliders[i, 0], glitSliders[i, 1], glit, -255f, top - i * 96f, 460f, 24);
        Put(Grab("Color A swatch", "text"), glit, 10f, top - 1 * 96f + 24f, 34f, 26f, 20);
        Put(Grab("Color B swatch", "text"), glit, 10f, top - 2 * 96f + 24f, 34f, 26f, 20);
        string[] fx = { "Fine glitter dust", "Starbursts", "Tiny stars", "Diamond shards", "Soft bokeh", "Fairy lights", "Falling shimmer",
            "Rising sparks", "Confetti flakes", "Orbiting glints", "Floating hearts", "Snow crystals", "Magic wisps" };
        for (int i = 0; i < fx.Length; i++) Put(Grab(fx[i] + " toggle", "ctrl"), glit, 255f, top - i * 56f, 450f, 48f, 24);
        Put(Grab("ALL ON", "ctrl"), glit, 140f, top - 13 * 56f - 20f, 210f, 62f, 24);
        Put(Grab("ALL OFF", "ctrl"), glit, 370f, top - 13 * 56f - 20f, 210f, 62f, 24);
        Put(Grab("LUSH", "ctrl"), glit, -255f, top - 6 * 96f - 10f, 460f, 62f, 24, "RESET DENSITY");

        // ---- MAGIC
        string[,] magicSliders = { { "BACKGROUND", "BACKGROUND slider" }, { "MUSIC VOLUME", "MUSIC VOLUME slider" },
            { "SWIRL ENERGY", "SWIRL ENERGY" }, { "POLE OPACITY", "POLE OPACITY" },
            { "CRYSTAL SPARKLE", "CRYSTAL SPARKLE" }, { "STAIR COLOR  (pink  →  any color)", "STAIR COLOR" } };
        string[] magicSliderLabels = { "BACKGROUND BRIGHTNESS", "MUSIC VOLUME", "SWIRL ENERGY", "POLE OPACITY", "CRYSTAL SPARKLE", "STAIR COLOR" };
        for (int i = 0; i < magicSliders.GetLength(0); i++)
            PutPair(magicSliders[i, 0], magicSliders[i, 1], magic, -255f, top - i * 96f, 460f, 24, magicSliderLabels[i]);
        string[] magicToggles = { "BODY STARDUST", "TOUCH MAGIC", "SPARKLE CLOUDS", "TRANSLUCENT POLE" };
        for (int i = 0; i < magicToggles.Length; i++) Put(Grab(magicToggles[i], "ctrl"), magic, 255f, top - i * 76f, 450f, 64f, 24);
        Put(Grab("AMBIENT PREVIEW / OFF", "ctrl"), magic, 255f, top - 4 * 76f - 20f, 450f, 62f, 24);
        Put(Grab("SPELL PATTERN", "text"), magic, 255f, top - 5 * 76f - 20f, 450f, 44f, 22);
        Put(Grab("NEW SPELL", "ctrl"), magic, 255f, top - 5 * 76f - 80f, 450f, 62f, 24);
        Put(Grab("PEACH CLOUDS", "ctrl"), magic, -255f, top - 6 * 96f - 10f, 460f, 62f, 24);
        Put(Grab("ROSE CLOUDS", "ctrl"), magic, -255f, top - 6 * 96f - 80f, 460f, 62f, 24);
        Put(Grab("TWILIGHT CLOUDS", "ctrl"), magic, -255f, top - 6 * 96f - 150f, 460f, 62f, 24);

        // ---- close button lives outside the pages
        Put(Grab("CLOSE  X", "ctrl"), panel, W / 2f - 150f, H / 2f - 60f, 240f, 56f, 26);

        // ---- whatever is left on the old panels was crossed out, or is a heading the new panel replaces
        var dropped = pool.Where(r => r != null).Select(r => r.name).ToList();
        foreach (var r in pool.ToList()) if (r != null) Object.DestroyImmediate(r.gameObject);
        report.Add("menu: " + dropped.Count + " leftover elements deleted: " + string.Join(", ", dropped));
        foreach (var o in olds) if (o != null) Object.DestroyImmediate(o.gameObject);
        report.Add("menu: the three old panels are gone; one panel " + W + "x" + H + " at scale " + Scale + " (" + (W * Scale).ToString("0.00") + " x " + (H * Scale).ToString("0.00") + " m), four tabs");
        pool.Clear();
    }

    static void EnsureProgram(string name)
    {
        string path = Root + "Scripts/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<UdonSharp.UdonSharpProgramAsset>(path) == null)
        {
            var a = ScriptableObject.CreateInstance<UdonSharp.UdonSharpProgramAsset>();
            a.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "Scripts/" + name + ".cs");
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets();
        }
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
    }

    // ================================================================= 2. connected furniture clouds
    static bool IsPuff(Renderer r)
    {
        var mf = r.GetComponent<MeshFilter>();
        return mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Glitter Cloud");
    }
    static void Furniture()
    {
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge == null) { report.Add("furniture: cloud lounge not found"); return; }
        int groups = 0, before = 0, after = 0, puffs = 0;
        foreach (var furn in lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Furniture")).ToList())
        {
            // puffs grouped by the transform that holds them - one group is one piece of the furniture
            foreach (var g in furn.GetComponentsInChildren<Renderer>(true).Where(IsPuff).GroupBy(r => r.transform.parent))
            {
                var list = g.ToList(); if (list.Count < 3) continue;
                groups++; puffs += list.Count;
                before += Components(list, 1f, 1f);
                float shrink = 1f, grow = 1f; int comp = Components(list, shrink, grow);
                for (int step = 0; step < 14 && comp > 1; step++) { shrink = Mathf.Max(.80f, shrink - .02f); grow = Mathf.Min(1.5f, grow + .035f); comp = Components(list, shrink, grow); }
                Apply(list, shrink, grow);
                after += Components(list, 1f, 1f);
            }
        }
        report.Add("furniture: " + groups + " puff groups across the lounge furniture, " + puffs + " puffs; separate blobs before: "
            + before + ", after: " + after + " (one per group is the goal, so " + groups + ")");
    }
    static Vector3[] Centres(List<Renderer> rs) { return rs.Select(r => r.bounds.center).ToArray(); }
    static float[] Radii(List<Renderer> rs) { return rs.Select(r => (r.bounds.extents.x + r.bounds.extents.z) * .5f).ToArray(); }
    static int Components(List<Renderer> rs, float shrink, float grow)
    {
        var c = Centres(rs); var rad = Radii(rs); int n = rs.Count;
        var m = Vector3.zero; foreach (var p in c) m += p; m /= n;
        for (int i = 0; i < n; i++) { c[i] = m + (c[i] - m) * shrink; rad[i] *= grow; }
        var seen = new bool[n]; int comps = 0;
        for (int i = 0; i < n; i++)
        {
            if (seen[i]) continue;
            comps++; var stack = new Stack<int>(); stack.Push(i); seen[i] = true;
            while (stack.Count > 0)
            {
                int a = stack.Pop();
                for (int b = 0; b < n; b++)
                    if (!seen[b] && Vector3.Distance(c[a], c[b]) <= (rad[a] + rad[b]) * .88f) { seen[b] = true; stack.Push(b); }
            }
        }
        return comps;
    }
    static void Apply(List<Renderer> rs, float shrink, float grow)
    {
        var m = Vector3.zero; foreach (var r in rs) m += r.bounds.center; m /= rs.Count;
        foreach (var r in rs)
        {
            var t = r.transform;
            var offset = t.position - r.bounds.center;              // the puff mesh is not centred on its transform
            var target = m + (r.bounds.center - m) * shrink;
            t.position = target + offset * grow;
            t.localScale *= grow;
            EditorUtility.SetDirty(t);
        }
    }

    // ================================================================= 3. leftovers
    static void Leftovers()
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t != null && t.gameObject.scene.IsValid()).ToList();
        var trees = all.Where(t => t.name == "Cloud tree").ToList();
        var broken = all.Select(t => t.GetComponent<MeshFilter>()).Where(f => f != null && f.sharedMesh == null && f.GetComponent<MeshRenderer>() != null).ToList();
        foreach (var f in broken) if (f != null) Object.DestroyImmediate(f.gameObject);
        report.Add("leftovers: cloud trees still in the scene: " + trees.Count + (trees.Count > 0 ? " (" + string.Join(", ", trees.Take(5).Select(PathOf)) + ")" : "")
            + "; objects whose mesh had gone missing, deleted: " + broken.Count);
        foreach (var t in trees) if (t != null) Object.DestroyImmediate(t.gameObject);
    }
}
