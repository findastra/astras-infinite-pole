// Claude round 4a (2026-09-24): the real DJ decks, modeled in Blender (console with a lit "MOMMY'S" front, two
// turntables with spinning glowing platters, a mixer, speaker stacks, glowing snaking cables) plus glowing butterflies.
// Model data: Assets/Astra/Models/dj_deck.json (exported by Claude's Blender script; Unity axes, meters).
// Replaces the placeholder deck pieces under 12 - DJ cloud/DJ booth. Keeps the DJ mic zone untouched. Safe to rerun.
// Menu: Astra > Claude Round 4a - DJ Decks
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ClaudeRound4a
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";

    [Serializable] class Sub { public string material; public int[] triangles; }
    [Serializable] class Group { public string name; public float[] pivot; public float[] vertices; public float[] normals; public Sub[] submeshes; }
    [Serializable] class Model { public Group[] groups; }

    [MenuItem("Astra/Claude Round 4a - DJ Decks")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND4A start");
        EnsureProgram("AstraDeckMotion"); EnsureProgram("AstraObjectSwitch");
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound4a.unity.txt", true);
        var report = new List<string>();
        var model = JsonUtility.FromJson<Model>(File.ReadAllText(Root + "Models/dj_deck.json"));
        var mats = Materials();

        var booth = GameObject.Find("12 - DJ cloud").transform.Find("DJ booth");
        foreach (var n in new[] { "Deck table", "Turntable left", "Turntable right", "Mixer", "Deck console", "Deck cables", "Platter L", "Platter R", "Butterflies" })
        { var o = booth.Find(n); if (o != null) UnityEngine.Object.DestroyImmediate(o.gameObject); }

        var made = new Dictionary<string, GameObject>(); Mesh butterfly = null;
        foreach (var g in model.groups)
        {
            var mesh = BuildMesh(g);
            string path = Root + "Meshes/DJ Deck " + g.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path); else { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
            report.Add(g.name + ": " + (mesh.triangles.Length / 3) + " triangles, " + g.submeshes.Length + " materials");
            if (g.name == "Butterfly") { butterfly = mesh; continue; }
            string objName = g.name == "Console" ? "Deck console" : g.name == "Cables" ? "Deck cables" : g.name;
            var go = new GameObject(objName); go.transform.SetParent(booth, false);
            go.transform.localPosition = new Vector3(g.pivot[0], g.pivot[1], g.pivot[2]);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = g.submeshes.Select(s => mats[s.material]).ToArray();
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            made[g.name] = go;
        }
        // solid console (you can lean on it, not walk through it); everything else is walk-through
        var col = made["Console"].AddComponent<BoxCollider>(); col.center = new Vector3(0, .5f, .0f); col.size = new Vector3(2.6f, 1.0f, .78f);
        foreach (var sx in new[] { -1.75f, 1.75f }) { var sp = made["Console"].AddComponent<BoxCollider>(); sp.center = new Vector3(sx, .675f, .05f); sp.size = new Vector3(.62f, 1.35f, .55f); }

        // spinning platters + pulsing cables
        var motion = booth.GetComponent<AstraDeckMotion>(); if (motion == null) motion = booth.gameObject.AddUdonSharpComponent<AstraDeckMotion>();
        motion.platters = new[] { made["Platter L"].transform, made["Platter R"].transform };
        motion.spinSpeeds = new[] { 140f, 140f };
        motion.pulseRenderers = new Renderer[] { made["Cables"].GetComponent<MeshRenderer>() };
        motion.colorProperty = "_Top"; motion.pulseColor = new Color(1.3f, 2.8f, 3.4f, 1); motion.pulseRate = .5f; motion.pulseDepth = .45f;
        UdonSharpEditorUtility.CopyProxyToUdon(motion);

        // glowing butterflies fluttering over the cables
        var bgo = new GameObject("Butterflies"); bgo.transform.SetParent(booth, false); bgo.transform.localPosition = new Vector3(0, .8f, 1.6f);
        var ps = bgo.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.prewarm = true; main.playOnAwake = true; main.maxParticles = 28;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 15f); main.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .2f);
        main.startSize = new ParticleSystem.MinMaxCurve(.8f, 1.4f); main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(-.4f, .4f); main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationZ = new ParticleSystem.MinMaxCurve(-.3f, .3f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .7f, .95f), new Color(.7f, .9f, 1f));
        var em = ps.emission; em.rateOverTime = 2.4f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(5f, 1.2f, 2.4f);
        var noise = ps.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = .45f; noise.scrollSpeed = .25f; noise.quality = ParticleSystemNoiseQuality.Medium;
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.y = new ParticleSystem.MinMaxCurve(-.6f, .6f);
        var size = ps.sizeOverLifetime; size.enabled = true; size.separateAxes = true; // wings flap: squeeze the width fast
        var flap = new AnimationCurve(); for (int k = 0; k <= 90; k++) flap.AddKey(k / 90f, k % 2 == 0 ? 1f : .25f);
        size.x = new ParticleSystem.MinMaxCurve(1f, flap); size.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0, 1, 1)); size.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0, 1, 1));
        var fade = ps.colorOverLifetime; fade.enabled = true; var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .1f), new GradientAlphaKey(1, .85f), new GradientAlphaKey(0, 1) }); fade.color = gr;
        var pr = bgo.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Mesh; pr.mesh = butterfly; pr.sharedMaterial = mats["Glow"];
        pr.shadowCastingMode = ShadowCastingMode.Off; pr.receiveShadows = false; pr.alignment = ParticleSystemRenderSpace.Local;
        report.Add("Butterflies: up to 28 glowing flapping butterflies over the cables");

        // personal switch for the butterflies (the whole DJ cloud already has its own)
        // the switch script lives on its own object so it keeps running while the butterflies are hidden
        var holderT = booth.Find("Butterfly switch"); if (holderT != null) UnityEngine.Object.DestroyImmediate(holderT.gameObject);
        var holder = new GameObject("Butterfly switch"); holder.transform.SetParent(booth, false);
        var os = holder.AddUdonSharpComponent<AstraObjectSwitch>(); os.targets = new[] { bgo };
        Transform panel = null; foreach (var rr in scene.GetRootGameObjects()) foreach (var t in rr.GetComponentsInChildren<Transform>(true)) if (t.name == "World items") panel = t;
        var res = AstraAppearanceSwitches.Ensure(panel, "DJ BUTTERFLIES", UdonSharpEditorUtility.GetBackingUdonBehaviour(os), "Apply", true);
        os.toggle = res.Toggle; UdonSharpEditorUtility.CopyProxyToUdon(os); report.Add(res.Report);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);

        // validation
        Physics.SyncTransforms();
        bool mic = booth.Find("DJ mic zone (voice carries world-wide)") != null;
        var micT = booth.Find("DJ mic zone (voice carries world-wide)");
        bool djSpotFree = micT != null && !Physics.CheckBox(micT.position + Vector3.up * .2f, new Vector3(.4f, .5f, .3f), micT.rotation, ~0, QueryTriggerInteraction.Ignore);
        report.Add("DJ mic zone kept: " + (mic ? "PASS" : "FAIL") + "; DJ standing spot not blocked by the new deck: " + (djSpotFree ? "PASS" : "FAIL"));
        report.Add("Platters spin: " + (motion.platters.All(p => p != null) ? "PASS" : "FAIL"));
        File.WriteAllText("Review/claude-round4a-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND4A_OK " + string.Join(" | ", report));
    }

    static Mesh BuildMesh(Group g)
    {
        int n = g.vertices.Length / 3; var v = new Vector3[n]; var nr = new Vector3[n];
        for (int i = 0; i < n; i++) { v[i] = new Vector3(g.vertices[i * 3], g.vertices[i * 3 + 1], g.vertices[i * 3 + 2]); nr[i] = new Vector3(g.normals[i * 3], g.normals[i * 3 + 1], g.normals[i * 3 + 2]); }
        var m = new Mesh { name = "DJ Deck " + g.name, indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        m.vertices = v; m.normals = nr; m.subMeshCount = g.submeshes.Length;
        for (int s = 0; s < g.submeshes.Length; s++) m.SetTriangles(g.submeshes[s].triangles, s);
        m.RecalculateBounds(); return m;
    }

    static Dictionary<string, Material> Materials()
    {
        Material Get(string name) { var p = Root + "Materials/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(p); if (m == null) { m = new Material(Shader.Find("Astra/Glitter Cloud")); AssetDatabase.CreateAsset(m, p); } m.shader = Shader.Find("Astra/Glitter Cloud"); m.enableInstancing = true; m.SetFloat("_Pastel", 0f); EditorUtility.SetDirty(m); return m; }
        var metal = Get("DJ Metal"); metal.SetColor("_Top", new Color(.9f, .86f, .96f)); metal.SetColor("_Bottom", new Color(.42f, .38f, .5f)); metal.SetColor("_Rim", new Color(1.6f, 1.3f, 1.8f)); metal.SetFloat("_Glitter", 1.2f);
        var glow = Get("DJ Glow"); glow.SetColor("_Top", new Color(3.2f, 1.2f, 2.6f)); glow.SetColor("_Bottom", new Color(2.8f, 1.0f, 2.3f)); glow.SetColor("_Rim", new Color(1f, .5f, 1f)); glow.SetFloat("_Glitter", .3f);
        var cable = Get("DJ Cable Glow"); cable.SetColor("_Top", new Color(1.3f, 2.8f, 3.4f)); cable.SetColor("_Bottom", new Color(1.1f, 2.4f, 3f)); cable.SetColor("_Rim", new Color(.8f, 1.2f, 1.4f)); cable.SetFloat("_Glitter", .5f);
        return new Dictionary<string, Material> {
            { "Body", AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/DJ Deck.mat") },
            { "Dark", AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/DJ Platter.mat") },
            { "Metal", metal }, { "Glow", glow }, { "CableGlow", cable } };
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
