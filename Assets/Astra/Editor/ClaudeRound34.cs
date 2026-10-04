// Claude, 2026-10-04. Menu: Astra > Claude > 34 Menu sliders you can see
// The slider tracks on the tab menu were almost invisible (dark on dark), so each slider only showed a floating white
// handle. Every slider now has a clear plum track, a pink fill and a round-ish white handle; STAIR COLOR shows a rainbow
// track. Layout and wiring are not touched. Photos: Review/Photos/Round34/. Report: Review/claude-round34-report.txt.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeRound34
{
    [MenuItem("Astra/Claude/34 Menu sliders you can see")]
    public static void Run()
    {
        var report = new List<string>();
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        var panel = menu != null ? menu.menuRoot.Find("Astra menu") as RectTransform : null;
        if (panel == null) { File.WriteAllText("Review/claude-round34-report.txt", "no Astra menu panel"); return; }
        var rainbow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Astra/Textures/Menu Rainbow.png");
        int n = 0;
        foreach (var s in panel.GetComponentsInChildren<Slider>(true))
        {
            var rt = (RectTransform)s.transform; float h = Mathf.Max(40f, rt.sizeDelta.y); rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            bool stair = s.name.StartsWith("STAIR COLOR");
            var bg = s.transform.Find("Background") as RectTransform;
            if (bg == null) { bg = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>(); bg.SetParent(s.transform, false); bg.SetAsFirstSibling(); }
            bg.anchorMin = new Vector2(0, .3f); bg.anchorMax = new Vector2(1, .7f); bg.offsetMin = bg.offsetMax = Vector2.zero;
            var bi = bg.GetComponent<Image>() ?? bg.gameObject.AddComponent<Image>();
            bi.sprite = stair ? rainbow : null; bi.type = Image.Type.Simple; bi.color = stair ? Color.white : new Color(.45f, .2f, .45f, 1f); bi.raycastTarget = true;
            if (s.fillRect != null)
            {
                var fa = s.fillRect.parent as RectTransform;
                if (fa != null && fa != rt) { fa.anchorMin = new Vector2(0, .3f); fa.anchorMax = new Vector2(1, .7f); fa.offsetMin = new Vector2(6, 0); fa.offsetMax = new Vector2(-18, 0); EditorUtility.SetDirty(fa); }
                var fi = s.fillRect.GetComponent<Image>();
                if (fi != null) { fi.sprite = null; fi.color = stair ? new Color(1, 1, 1, 0) : new Color(1f, .45f, .82f, 1f); EditorUtility.SetDirty(fi); }
                s.fillRect.offsetMin = new Vector2(s.fillRect.offsetMin.x, 0); s.fillRect.offsetMax = new Vector2(s.fillRect.offsetMax.x, 0);
            }
            if (s.handleRect != null)
            {
                var ha = s.handleRect.parent as RectTransform;
                if (ha != null && ha != rt) { ha.anchorMin = new Vector2(0, 0); ha.anchorMax = new Vector2(1, 1); ha.offsetMin = new Vector2(12, 0); ha.offsetMax = new Vector2(-12, 0); EditorUtility.SetDirty(ha); }
                s.handleRect.sizeDelta = new Vector2(30f, 0f);
                var hi = s.handleRect.GetComponent<Image>(); if (hi != null) { hi.color = Color.white; EditorUtility.SetDirty(hi); }
            }
            EditorUtility.SetDirty(bi); EditorUtility.SetDirty(rt); EditorUtility.SetDirty(s); n++;
        }
        report.Add("sliders restyled: " + n + (rainbow != null ? "" : " (CHECK: rainbow sprite missing)"));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // photos of the two slider tabs
        var tabs = panel.GetComponentInChildren<AstraMenuTabs>(true);
        string dir = "Review/Photos/Round34"; Directory.CreateDirectory(dir);
        bool was = menu.menuRoot.gameObject.activeSelf; menu.menuRoot.gameObject.SetActive(true);
        var st = tabs.pages.Select(p => p.activeSelf).ToArray();
        var c = new Vector3[4]; panel.GetWorldCorners(c); var centre = (c[0] + c[2]) * .5f; float hgt = Vector3.Distance(c[0], c[1]);
        foreach (int i in new[] { 2, 3 })
        {
            for (int k = 0; k < tabs.pages.Length; k++) tabs.pages[k].SetActive(k == i);
            float fov = 45f, d = hgt * .62f / Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
            var go = new GameObject("cam"); var cam = go.AddComponent<Camera>(); cam.transform.position = centre - panel.forward * d; cam.transform.LookAt(centre); cam.fieldOfView = fov; cam.nearClipPlane = .02f;
            var rtx = new RenderTexture(1000, 1400, 24); cam.targetTexture = rtx; cam.Render(); RenderTexture.active = rtx;
            var tex = new Texture2D(1000, 1400, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1000, 1400), 0, 0); tex.Apply();
            File.WriteAllBytes(dir + "/" + tabs.pages[i].name + ".png", tex.EncodeToPNG());
            cam.targetTexture = null; RenderTexture.active = null; rtx.Release(); Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
        }
        for (int k = 0; k < tabs.pages.Length; k++) tabs.pages[k].SetActive(st[k]);
        menu.menuRoot.gameObject.SetActive(was);
        report.Add("photos: " + dir);
        File.WriteAllText("Review/claude-round34-report.txt", string.Join("\n", report));
    }
}
