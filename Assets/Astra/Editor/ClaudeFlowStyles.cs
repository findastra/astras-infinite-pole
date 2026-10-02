// Claude, 2026-10-01: renders one photo of the pole per rainbow-flow pattern, so the look can be chosen from pictures.
// Astra > Claude > 21 Rainbow flow: photograph every pattern   -> Review/Photos/Styles/
// Astra > Claude > 21b Rainbow flow: keep pattern <n>          -> sets the live material (edit Pick below first)
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class ClaudeFlowStyles
{
    const string Root = "Assets/Astra/";
    public static int Pick = 0;    // the pattern applied by "keep pattern"
    static readonly string[] Names = { "0 rivers", "1 aurora", "2 oil swirl", "3 lava blobs", "4 rain streaks", "5 tiger marble" };

    [MenuItem("Astra/Claude/21 Rainbow flow: photograph every pattern")]
    public static void Shoot()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Rainbow Flow.mat");
        if (mat == null) { Debug.LogError("Rainbow Flow.mat not found"); return; }
        var flow = GameObject.Find("14 - Rainbow flow (Claude)");
        if (flow != null) flow.transform.position = new Vector3(0, 6, 0);     // the follower only runs in play mode
        Directory.CreateDirectory("Review/Photos/Styles");
        float keep = mat.GetFloat("_Style");
        var spiral = Object.FindObjectOfType<AstraSpiral>(); if (spiral != null) spiral.Recenter(0);
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>()) ps.Simulate(12, true, true);
        var lines = new System.Collections.Generic.List<string>();
        for (int s = 0; s < Names.Length; s++)
        {
            mat.SetFloat("_Style", s); EditorUtility.SetDirty(mat);
            Shot("Review/Photos/Styles/" + Names[s] + ".png", new Vector3(10, 7.5f, -10), new Vector3(0, 7, 0), 62, 1280, 800);
            lines.Add(Names[s] + " -> Review/Photos/Styles/" + Names[s] + ".png");
        }
        mat.SetFloat("_Style", keep); EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
        File.WriteAllText("Review/claude-flow-styles.txt", string.Join("\n", lines) + "\nlive pattern is still " + Names[Mathf.Clamp((int)keep, 0, 5)]);
        Debug.Log("CLAUDE_FLOW_STYLES_OK " + string.Join(" | ", lines));
    }

    [MenuItem("Astra/Claude/21b Rainbow flow: keep the picked pattern")]
    public static void Keep()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Rainbow Flow.mat"); if (mat == null) return;
        mat.SetFloat("_Style", Pick); EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        File.WriteAllText("Review/claude-flow-styles.txt", "live pattern set to " + Names[Mathf.Clamp(Pick, 0, 5)]);
        Debug.Log("CLAUDE_FLOW_PICK_OK " + Names[Mathf.Clamp(Pick, 0, 5)]);
    }

    static void Shot(string path, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude style camera");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .05f; cam.farClipPlane = 2000;
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf);
        cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release();
        Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
}
