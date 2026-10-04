// Claude, 2026-10-04. Menu: Astra > Claude > 31 Menu header + slider fix, close-up photos
//  - Puts the title, the four tab buttons and CLOSE back at the top of the panel (an interrupted run of round 30
//    shifted the title and tabs twice, so the tabs sat on top of the title).
//  - Removes the AMBIENT PREVIEW button (it is named "Ambient preview" on the panel, so round 30 missed it).
//  - Every slider gets a visible track (dark plum bar, pink fill, bigger white handle); STAIR COLOR gets its rainbow
//    bar on the track itself, so it is obvious what it does.
//  - Close-up photos of the smooth furniture (before/after), the DJ booth and the pole: Review/Photos/Round31/.
// Safe to rerun. Report: Review/claude-round31-report.txt.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeRound31
{
    static List<string> report;

    [MenuItem("Astra/Claude/31 Menu header + slider fix, close-up photos")]
    public static void Run()
    {
        report = new List<string>();
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            File.Copy(scene.path, "Review/Backups/BeforeRound31.unity.txt", true);
            Step("header", Header);
            Step("ambient", Ambient);
            Step("sliders", Sliders);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Step("photos", Photos);
        }
        catch (Exception e) { report.Add("FAILED: " + e); }
        File.WriteAllText("Review/claude-round31-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND31 " + string.Join(" | ", report));
    }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("STEP FAILED (" + name + "): " + e.Message + "\n" + e.StackTrace); } }
    static RectTransform Panel() { var m = Object.FindObjectOfType<AstraHandMenu>(true); return m == null ? null : m.menuRoot.Find("Astra menu") as RectTransform; }

    static void Header()
    {
        var panel = Panel(); if (panel == null) { report.Add("no panel"); return; }
        float H = panel.sizeDelta.y, W = panel.sizeDelta.x;
        foreach (var r in panel.Cast<Transform>().OfType<RectTransform>())
        {
            if (r.name == "Title") r.anchoredPosition = new Vector2(-90f, H / 2f - 62f);
            else if (r.name.StartsWith("Tab ")) r.anchoredPosition = new Vector2(r.anchoredPosition.x, H / 2f - 145f);
            else if (r.GetComponentInChildren<Text>(true) != null && r.GetComponentInChildren<Text>(true).text.StartsWith("CLOSE")) r.anchoredPosition = new Vector2(W / 2f - 130f, H / 2f - 62f);
            else continue;
            EditorUtility.SetDirty(r);
        }
        report.Add("header: title, tabs and CLOSE pinned to the top of the " + H + "-tall panel");
    }

    static void Ambient()
    {
        var panel = Panel(); if (panel == null) return;
        foreach (var b in panel.GetComponentsInChildren<Button>(true).Where(b => { var t = b.GetComponentInChildren<Text>(true); return t != null && t.text.StartsWith("AMBIENT PREVIEW"); }).ToList())
        { report.Add("removed button " + b.name); Object.DestroyImmediate(b.gameObject); }
    }

    static void Sliders()
    {
        var panel = Panel(); if (panel == null) return;
        var rainbow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Astra/Textures/Menu Rainbow.png");
        int n = 0; if (rainbow == null) report.Add("sliders: CHECK rainbow sprite not found");
        foreach (var s in panel.GetComponentsInChildren<Slider>(true))
        {
            var handle = s.handleRect; var fill = s.fillRect;
            var bg = s.GetComponentsInChildren<Image>(true).FirstOrDefault(i => (handle == null || !i.transform.IsChildOf(handle)) && (fill == null || !i.transform.IsChildOf(fill)) && i.transform != s.transform);
            bool isRainbow = s.name.StartsWith("STAIR COLOR");
            // these sliders had no visible track at all; give each one its own track image behind everything
            var trackT = s.transform.Find("Claude track");
            if (trackT == null)
            {
                var tg = new GameObject("Claude track", typeof(RectTransform), typeof(Image)); trackT = tg.transform; trackT.SetParent(s.transform, false); trackT.SetAsFirstSibling();
                tg.GetComponent<Image>().raycastTarget = false;
            }
            bg = trackT.GetComponent<Image>();
            if (bg != null)
            {
                var br = (RectTransform)bg.transform; br.anchorMin = new Vector2(0, .3f); br.anchorMax = new Vector2(1, .7f); br.offsetMin = br.offsetMax = Vector2.zero;
                if (isRainbow && rainbow != null) { bg.sprite = rainbow; bg.type = Image.Type.Simple; bg.color = Color.white; }
                else { bg.sprite = null; bg.color = new Color(.30f, .13f, .32f, 1f); }
                EditorUtility.SetDirty(bg); EditorUtility.SetDirty(br);
            }
            if (fill != null)
            {
                var fi = fill.GetComponent<Image>(); if (fi != null) { fi.sprite = null; fi.color = isRainbow ? new Color(1, 1, 1, 0) : new Color(1f, .45f, .82f, 1f); EditorUtility.SetDirty(fi); }
                var area = fill.parent as RectTransform; if (area != null && area != s.transform) { area.anchorMin = new Vector2(0, .3f); area.anchorMax = new Vector2(1, .7f); area.offsetMin = new Vector2(5, 0); area.offsetMax = new Vector2(-5, 0); EditorUtility.SetDirty(area); }
                fill.anchorMin = new Vector2(fill.anchorMin.x, 0); fill.anchorMax = new Vector2(fill.anchorMax.x, 1); fill.offsetMin = new Vector2(fill.offsetMin.x, 0); fill.offsetMax = new Vector2(fill.offsetMax.x, 0); EditorUtility.SetDirty(fill);
            }
            if (handle != null)
            {
                handle.sizeDelta = new Vector2(30f, 10f); var hi = handle.GetComponent<Image>(); if (hi != null) { hi.color = Color.white; EditorUtility.SetDirty(hi); }
                var area = handle.parent as RectTransform; if (area != null && area != s.transform) { area.anchorMin = new Vector2(0, 0); area.anchorMax = new Vector2(1, 1); area.offsetMin = new Vector2(15, 0); area.offsetMax = new Vector2(-15, 0); EditorUtility.SetDirty(area); }
                EditorUtility.SetDirty(handle);
            }
            n++;
        }
        report.Add("sliders: " + n + " restyled (visible plum track, pink fill, white handle; STAIR COLOR rainbow)");
    }

    // ---------------- photos
    static void Photos()
    {
        string dir = "Review/Photos/Round31"; Directory.CreateDirectory(dir);
        var panel = Panel(); var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        var tabs = panel != null ? panel.GetComponentInChildren<AstraMenuTabs>(true) : null;
        if (panel != null && menu != null && tabs != null)
        {
            bool was = menu.menuRoot.gameObject.activeSelf; menu.menuRoot.gameObject.SetActive(true);
            var c = new Vector3[4]; panel.GetWorldCorners(c); var centre = (c[0] + c[2]) * .5f; float hgt = Vector3.Distance(c[0], c[1]);
            var look = centre + Vector3.down * hgt * .1f; float d = hgt * .72f / Mathf.Tan(25f * Mathf.Deg2Rad);
            var states = tabs.pages.Select(p => p.activeSelf).ToArray();
            foreach (var i in new[] { 0, 3, 2 })
            {
                for (int k = 0; k < tabs.pages.Length; k++) tabs.pages[k].SetActive(k == i);
                for (int k = 0; k < tabs.tabBacks.Length; k++) { tabs.tabBacks[k].color = k == i ? tabs.onBack : tabs.offBack; tabs.tabLabels[k].color = k == i ? tabs.onText : tabs.offText; }
                // a dark card behind the panel so the world behind it does not show through in the photo
                Shot(dir, "Menu " + tabs.pages[i].name.Replace("Page ", ""), look - panel.forward * d, look, 50, 1100, 1500, panel);
            }
            for (int k = 0; k < tabs.pages.Length; k++) tabs.pages[k].SetActive(states[k]);
            for (int k = 0; k < tabs.tabBacks.Length; k++) { tabs.tabBacks[k].color = k == 0 ? tabs.onBack : tabs.offBack; tabs.tabLabels[k].color = k == 0 ? tabs.onText : tabs.offText; }
            menu.menuRoot.gameObject.SetActive(was);
        }
        // furniture close-ups: merged surface, then the old puffs for comparison
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge != null)
        {
            var merged = lounge.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name == "Merged cloud shape").ToList();
            report.Add("photos: " + merged.Count + " merged furniture shapes in the scene");
            foreach (var pick in merged.OrderByDescending(r => r.bounds.size.magnitude).Where((r, i) => i == 2 || i == 10 || i == 25).ToList())
            {
                var b = pick.bounds; var piece = pick.transform.parent;
                var outw = new Vector3(b.center.x, 0, b.center.z).normalized; if (outw.sqrMagnitude < .01f) outw = Vector3.forward;
                var pos = b.center + outw * b.extents.magnitude * 1.5f + Vector3.up * b.extents.magnitude * .6f;
                string nm = piece.parent != null ? piece.parent.name : piece.name;
                Shot(dir, "Furniture after - " + nm, pos, b.center, 45, 1400, 1000, null);
                var puffs = piece.GetComponentsInChildren<MeshRenderer>(true).Where(r => r != pick && !r.enabled && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Glitter Cloud")).ToList();
                pick.enabled = false; foreach (var p in puffs) p.enabled = true;
                Shot(dir, "Furniture before - " + nm, pos, b.center, 45, 1400, 1000, null);
                pick.enabled = true; foreach (var p in puffs) p.enabled = false;
            }
        }
        var booth = GameObject.Find("12 - DJ cloud") != null ? GameObject.Find("12 - DJ cloud").transform.Find("DJ booth") : null;
        if (booth != null)
        {
            var rs = booth.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); var fwd = -booth.forward; Shot(dir, "DJ booth front", b.center + booth.forward * 5f + Vector3.up * 1.5f, b.center, 50, 1400, 900, null); Shot(dir, "DJ booth back", b.center - booth.forward * 5f + Vector3.up * 1.5f, b.center, 50, 1400, 900, null); }
        }
        Shot(dir, "Pole close", new Vector3(0, 2.2f, -1.6f), new Vector3(0, 2.2f, 0), 50, 900, 1200, null);
        report.Add("photos: " + dir);
    }
    static void Shot(string dir, string name, Vector3 pos, Vector3 look, float fov, int w, int h, RectTransform card)
    {
        var go = new GameObject("Claude photo camera"); var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 350f;
        if (card != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.02f, .01f, .03f); cam.cullingMask = 1 << card.gameObject.layer; }
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
}
