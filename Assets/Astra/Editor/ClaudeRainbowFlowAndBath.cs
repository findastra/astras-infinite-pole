// Claude (2026-10-01): two owner requests.
//  1. Rainbow flow: liquid-rainbow streams on a huge see-through cylinder around the pole that follows you up and
//     down (Shaders/RainbowFlow.shader, Scripts/AstraFollowHeight.cs). Personal switch RAINBOW FLOW on World items.
//  2. Bathtub: the whole "Bathtub cloud" at 25% of its size, with the water pulled inside the bowl (nothing pokes
//     out between the wall puffs or over the rim) and the bubbles kept inside the tub.
// Menu: Astra > Claude > 19 Rainbow flow + small bath. Safe to rerun (it never shrinks the bath twice).
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeRainbowFlowAndBath
{
    const string Root = "Assets/Astra/";
    const string FlowName = "14 - Rainbow flow (Claude)";
    const float Radius = 45f, Height = 140f, BathScale = .25f;

    [MenuItem("Astra/Claude/19 Rainbow flow + small bath")]
    public static void Run()
    {
        try { RunInner(); }
        catch (System.Exception e) { File.WriteAllText("Review/claude-rainbow-bath-validation.txt", "FAILED: " + e); Debug.LogException(e); }
    }
    static void RunInner()
    {
        Debug.Log("CLAUDE_RAINBOW start");
        EnsureProgram("AstraFollowHeight"); EnsureProgram("AstraObjectSwitch");
        var scene = EditorSceneManager.GetActiveScene();
        Directory.CreateDirectory("Review/Backups");
        File.Copy(scene.path, "Review/Backups/BeforeRainbowFlowAndBath.unity.txt", true);
        var report = new List<string>();

        // ---------- 1. rainbow flow ----------
        foreach (var old in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t != null && t.gameObject.scene == scene && (t.name == FlowName || t.name == "Rainbow flow follower" || t.name == "Rainbow flow switch")).ToList())
            if (old != null) Object.DestroyImmediate(old.gameObject);
        string matPath = Root + "Materials/Rainbow Flow.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Astra/Rainbow Flow")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.shader = Shader.Find("Astra/Rainbow Flow"); mat.enableInstancing = true;
        mat.SetFloat("_Scale", .04f); mat.SetFloat("_Glow", .9f); mat.SetFloat("_Gap", .66f); EditorUtility.SetDirty(mat); // big, calmer rivers with space between
        string meshPath = Root + "Meshes/Rainbow Flow Cylinder.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath); if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, meshPath); }
        Cylinder(mesh, Radius, Height, 160, 40); EditorUtility.SetDirty(mesh);
        var flow = new GameObject(FlowName); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(flow, scene);
        flow.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = flow.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        var follower = new GameObject("Rainbow flow follower"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(follower, scene);
        var fh = follower.AddUdonSharpComponent<AstraFollowHeight>(); fh.target = flow.transform; UdonSharpEditorUtility.CopyProxyToUdon(fh);
        report.Add("Rainbow flow: " + Radius + " m around the pole, " + Height + " m tall, follows you up and down; fades out 35-60 m above and below you");

        // personal switch
        Transform panel = null; foreach (var rt in scene.GetRootGameObjects()) foreach (var t in rt.GetComponentsInChildren<Transform>(true)) if (t.name == "World items") panel = t;
        if (panel != null)
        {
            var holder = new GameObject("Rainbow flow switch"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, scene);
            var os = holder.AddUdonSharpComponent<AstraObjectSwitch>(); os.targets = new[] { flow };
            var res = AstraAppearanceSwitches.Ensure(panel, "RAINBOW FLOW", UdonSharpEditorUtility.GetBackingUdonBehaviour(os), "Apply", true);
            os.toggle = res.Toggle; UdonSharpEditorUtility.CopyProxyToUdon(os); report.Add(res.Report);
        }
        else report.Add("CHECK: World items panel not found; no switch added");

        // ---------- 2. bathtub at 25%, water contained ----------
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        var bath = lounge != null ? lounge.transform.Find("Bathtub cloud") : null;
        if (bath == null) report.Add("CHECK: Bathtub cloud not found");
        else
        {
            var before = Bounds(bath);
            if (bath.Find("Claude 25% marker") == null)
            {
                bath.localScale = bath.localScale * BathScale;
                new GameObject("Claude 25% marker").transform.SetParent(bath, false);
                foreach (var ps in bath.GetComponentsInChildren<ParticleSystem>(true)) { var m = ps.main; m.scalingMode = ParticleSystemScalingMode.Hierarchy; } // bubbles shrink too
                foreach (var l in bath.GetComponentsInChildren<Light>(true)) l.range *= BathScale;
            }
            // sit on the same floor as before: put the lowest point back where it was
            var after = Bounds(bath); bath.position += Vector3.up * (before.min.y - after.min.y);
            var dish = bath.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Bath dish");
            var water = dish != null ? dish.Find("Bath water") : null;
            if (water != null)
            {
                // the water's edge sat on the middle of the wall puffs (99% of the wall line) and showed between them:
                // pull it in to 90% so the cloud shell and puffs fully enclose it, and lower the surface a little under the rim
                bool firstTime = water.localScale.x > .95f; // reruns don't lower the duck again
                water.localScale = new Vector3(.9f, .96f, .9f); water.localPosition = Vector3.zero;
                var duck = dish.Find("Rubber ducky"); if (duck != null && firstTime) duck.localPosition += Vector3.down * .16f;
                var bub = dish.Find("Bath bubbles");
                if (bub != null && firstTime)
                {
                    bub.localPosition += Vector3.down * .16f;
                    var ps = bub.GetComponent<ParticleSystem>();
                    var sh = ps.shape; sh.scale = Vector3.Scale(sh.scale, new Vector3(.75f, 1f, .75f));          // start well inside the rim
                    var vel = ps.velocityOverLifetime; vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    var nz = ps.noise; nz.strength = .05f;                                                          // no drifting over the edge
                    var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                }
            }
            var now = Bounds(bath);
            report.Add("Bathtub cloud: " + before.size.ToString("0.0") + " m -> " + now.size.ToString("0.0") + " m (25%), still on the floor; water pulled inside the bowl (90% of the wall line, surface lowered), bubbles kept inside");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        report.Add("Rainbow Flow shader compiles: " + (ShaderUtil.ShaderHasError(mat.shader) ? "FAIL" : "PASS"));
        File.WriteAllText("Review/claude-rainbow-bath-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_RAINBOW_OK " + string.Join(" | ", report));
    }

    // open cylinder seen from inside and outside (Cull Off), centred on the pole
    static void Cylinder(Mesh m, float radius, float height, int sides, int rings)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for (int j = 0; j <= rings; j++) for (int i = 0; i <= sides; i++) { float a = i * Mathf.PI * 2 / sides; v.Add(new Vector3(Mathf.Cos(a) * radius, -height / 2 + height * j / rings, Mathf.Sin(a) * radius)); }
        for (int j = 0; j < rings; j++) for (int i = 0; i < sides; i++) { int a = j * (sides + 1) + i, b = a + 1, c = a + sides + 1, d = c + 1; t.AddRange(new[] { a, c, b, b, c, d }); }
        m.Clear(); m.name = "Rainbow flow cylinder"; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals();
        m.bounds = new Bounds(Vector3.zero, new Vector3(radius * 2, height, radius * 2));
    }
    static Bounds Bounds(Transform root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true).Where(x => !(x is ParticleSystemRenderer)).ToArray();
        if (rs.Length == 0) return new Bounds(root.position, Vector3.zero); var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds); return b;
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
