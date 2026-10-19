// Claude, 2026-10-03. Menu: Astra > Claude > 28 Photograph the new menu and furniture (report only)
// Shoots each of the four tabs of the one-panel menu straight on, plus a few cloud lounge furniture pieces, into
// Review/Photos/Round28/. Changes nothing in the scene (the menu is put back exactly as it was).
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeRound28Photos
{
    const string Dir = "Review/Photos/Round28";

    [MenuItem("Astra/Claude/28 Photograph the new menu and furniture (report only)")]
    public static void Run()
    {
        Directory.CreateDirectory(Dir);
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        var panel = menu != null ? menu.menuRoot.Find("Astra menu") as RectTransform : null;
        if (panel == null) { Debug.LogError("CLAUDE_R28 no Astra menu panel"); return; }

        bool rootWas = menu.menuRoot.gameObject.activeSelf;
        menu.menuRoot.gameObject.SetActive(true);
        var tabs = panel.GetComponentInChildren<AstraMenuTabs>(true);
        var pages = tabs != null ? tabs.pages : panel.Cast<Transform>().Where(t => t.name.StartsWith("Page ")).Select(t => t.gameObject).ToArray();
        var wasOn = pages.Select(p => p.activeSelf).ToArray();

        // straight-on shot of the whole panel
        var corners = new Vector3[4]; panel.GetWorldCorners(corners);
        var centre = (corners[0] + corners[2]) * .5f;
        float h = Vector3.Distance(corners[0], corners[1]);
        const float fov = 36f;
        float dist = h * .56f / Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
        var eye = centre - panel.forward * dist;

        for (int i = 0; i < pages.Length; i++)
        {
            for (int k = 0; k < pages.Length; k++) pages[k].SetActive(k == i);
            Shot(Dir + "/" + (i + 1) + " Menu - " + pages[i].name.Replace("Page ", "") + ".png", eye, centre, fov, 1100, 1650);
        }
        for (int k = 0; k < pages.Length; k++) pages[k].SetActive(wasOn[k]);
        menu.menuRoot.gameObject.SetActive(rootWas);

        // a few furniture pieces, to see whether each one now reads as a single connected cloud
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge != null)
        {
            var picks = new[] { "Cloud sofa x1.9", "Cloud bed x1.5", "Giant chair", "Tea lounge" };
            int n = 0;
            foreach (var name in picks)
            {
                var t = lounge.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
                if (t == null) continue;
                var furn = t.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name.StartsWith("Furniture"));
                if (furn == null) continue;
                var rs = furn.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var outw = new Vector3(b.center.x, 0, b.center.z).normalized;
                float d = b.size.magnitude * .95f;
                Shot(Dir + "/" + (5 + n) + " " + name + ".png", b.center + outw * d + Vector3.up * d * .35f, b.center, 45f, 1600, 1000);
                n++;
            }
        }
        Debug.Log("CLAUDE_R28_OK " + Directory.GetFiles(Dir, "*.png").Length + " images in " + Dir);
    }

    static void Shot(string path, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude photo camera");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .02f;
        var refCam = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>()?.ReferenceCamera?.GetComponent<Camera>();
        cam.farClipPlane = refCam != null ? refCam.farClipPlane : 350f;
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release();
        Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
}
