// Claude (2026-09-24): beauty shots of the world for showing off + a VRChat thumbnail (1200x900).
// Renders from set camera spots with the world's bloom, after letting sparkles, rain and clouds run for a while.
// Menu: Astra > Claude - Take World Photos. Output: Review/Photos/. Never uploads anything.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class ClaudePhotos
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";

    [MenuItem("Astra/Claude - Take World Photos")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(Scene);
        Directory.CreateDirectory("Review/Photos");
        var spiral = Object.FindObjectOfType<AstraSpiral>(); spiral.Recenter(0);
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>();
        void Pose(float t)
        {
            if (orbit == null) return;
            for (int i = 0; i < orbit.pivots.Length; i++) orbit.pivots[i].localRotation = Quaternion.Euler(0, orbit.phases[i] + t * orbit.speeds[i], 0);
            if (orbit.clouds != null) for (int j = 0; j < orbit.clouds.Length; j++) if (orbit.clouds[j] != null) orbit.clouds[j].localRotation = Quaternion.Euler(0, orbit.cloudYaw[j] + t * orbit.cloudSpin[j], 0);
        }
        Pose(35);
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>()) ps.Simulate(18, true, true);

        var dj = GameObject.Find("12 - DJ cloud"); var screen = GameObject.Find("Cinema - synchronized playlist");
        var booth = GameObject.Find("Phone booth 1");
        var desc = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
        Vector3 spawn = desc.spawns != null && desc.spawns.Length > 0 && desc.spawns[0] != null ? desc.spawns[0].position : new Vector3(0, 0, -2);
        Vector3 eye = spawn + Vector3.up * 1.6f;

        Shot("01 Hero - the infinite pole", new Vector3(15, 7, -15), new Vector3(0, 9, 0), 60, 1920, 1080);
        Shot("02 Looking up the rainbow spiral", new Vector3(3.2f, -14, -3.2f), new Vector3(0, 24, 0), 78, 1920, 1080);
        Shot("03 The endless drop", new Vector3(1.5f, 34, -8.5f), new Vector3(0, -12, 0), 70, 1920, 1080);
        Shot("04 Orbiting cloud rings", new Vector3(22, 24, 12), new Vector3(0, 20, 0), 55, 1920, 1080);
        Shot("05 Sparkle rain among the clouds", new Vector3(9.5f, 16.5f, -4), new Vector3(12, 14, 4), 62, 1920, 1080);
        if (screen != null) { var sc = Center(screen.transform); Shot("06 Big screen from spawn", eye, sc, 62, 1920, 1080); }
        if (dj != null) { var dc = Center(dj.transform); var toward = (dc - eye); toward.y = 0; Shot("07 DJ cloud", dc - toward.normalized * 7 + Vector3.up * 2.2f, dc + Vector3.up * 1.2f, 58, 1920, 1080); }
        if (booth != null) { var bp = booth.transform.position; var outward = new Vector3(bp.x, 0, bp.z).normalized; Shot("08 Phone booth in the clouds", bp + outward * 4.5f + Vector3.up * 1.6f + Vector3.Cross(Vector3.up, outward) * 1.5f, bp + Vector3.up * 1.3f, 55, 1920, 1080); }
        Shot("09 Stairs, pole and clouds", new Vector3(-8, 5, 8), new Vector3(0, 7, 0), 70, 1920, 1080);
        // VRChat thumbnail: 4:3
        Shot("THUMBNAIL", new Vector3(13, 8, -13), new Vector3(0, 9.5f, 0), 58, 1200, 900);
        Debug.Log("CLAUDE_PHOTOS_OK Review/Photos (" + Directory.GetFiles("Review/Photos", "*.png").Length + " images)");
    }

    static Vector3 Center(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (rs.Length == 0) return t.position; var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b.center;
    }

    static void Shot(string name, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude photo camera");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .05f; cam.farClipPlane = 2000;
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); // second pass lets bloom settle
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes("Review/Photos/" + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release();
        Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
}
