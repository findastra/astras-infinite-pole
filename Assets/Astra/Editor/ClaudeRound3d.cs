// Claude round 3d (2026-09-24): every cloud rains bright white sparkles that drift and sway as they fall.
// One small particle system per cloud, simulated in the cloud's own space (so the rain rides with the orbiting cloud)
// and built so Unity can skip simulating any cloud that's off screen (procedural culling). Safe to rerun.
// Menu: Astra > Claude Round 3d - Cloud Sparkle Rain
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClaudeRound3d
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const string RainName = "Sparkle rain";
    const string SwitchLabel = "CLOUD SPARKLE RAIN";

    [MenuItem("Astra/Claude Round 3d - Cloud Sparkle Rain")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND3D start");
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3d.unity.txt", true);
        var report = new List<string>();

        string matPath = Root + "Materials/Cloud Sparkle Rain.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Astra/Soft Sparkle")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetColor("_Color", Color.white); mat.enableInstancing = true; EditorUtility.SetDirty(mat);

        // every orbiting cloud + the DJ clouds
        var clouds = new List<Transform>();
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>();
        if (orbit != null) foreach (var pv in orbit.pivots) foreach (Transform c in pv) clouds.Add(c);
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "DJ stage cloud" || t.name == "DJ step cloud") clouds.Add(t);

        var rains = new List<GameObject>(); int culled = 0;
        foreach (var c in clouds)
        {
            var old = c.Find(RainName); if (old != null) Object.DestroyImmediate(old.gameObject);
            var mf = c.GetComponent<MeshFilter>(); if (mf == null) continue;
            var b = mf.sharedMesh.bounds;
            bool big = c.name.StartsWith("DJ stage");
            float area = b.size.x * b.size.z * c.localScale.x * c.localScale.z;

            var go = new GameObject(RainName); go.transform.SetParent(c, false);
            go.transform.localPosition = new Vector3(b.center.x, b.min.y * .55f, b.center.z); // just inside the cloud's underside
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.6f, 4.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(.016f, .055f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.2f, 2.2f, 2.4f, 1), new Color(3.2f, 2.9f, 3.3f, 1)); // HDR white, blooms
            main.maxParticles = big ? 160 : 40;
            var em = ps.emission; em.rateOverTime = big ? 34f : Mathf.Clamp(area * 1.1f, 5f, 11f);
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(b.size.x * .7f, .05f, b.size.z * .7f);

            // fall + drift: steady descent, plus a slow sway that bends each flake's path (curves keep culling support)
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0, 1, -.5f), AnimationCurve.Constant(0, 1, -.22f)); // all three axes must share the same curve mode
            vel.x = new ParticleSystem.MinMaxCurve(.22f, Sway(0f), Sway(1.9f));
            vel.z = new ParticleSystem.MinMaxCurve(.22f, Sway(3.4f), Sway(5.1f));

            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                         new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(.85f, .7f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var size = ps.sizeOverLifetime; size.enabled = true;
            var twinkle = new AnimationCurve();
            for (int k = 0; k <= 16; k++) twinkle.AddKey(k / 16f, (k % 2 == 0 ? .45f : 1f) * (1 - k / 16f * .35f));
            size.size = new ParticleSystem.MinMaxCurve(1f, twinkle);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard; r.maxParticleSize = .025f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            if (ps.proceduralSimulationSupported) culled++;
            rains.Add(go);
        }
        report.Add("Clouds raining sparkles: " + rains.Count + " (" + clouds.Count + " clouds found)");
        report.Add("Off-screen skipping (procedural culling) supported: " + culled + "/" + rains.Count + (culled == rains.Count ? " PASS" : " CHECK"));
        report.Add("Max particles if every cloud were on screen: " + rains.Sum(g => g.GetComponent<ParticleSystem>().main.maxParticles));

        // World items switch: one CLOUD SPARKLE RAIN toggle drives every rain object
        var items = Object.FindObjectOfType<AstraWorldItems>();
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(items);
        Transform panel = null;
        foreach (var root in scene.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == "World items") panel = t;
        var oldT = panel.Find(SwitchLabel);
        var footer = panel.GetComponentsInChildren<Text>(true).First(t => t.text.StartsWith("Glitter, petals")).GetComponent<RectTransform>();
        float y;
        if (oldT != null) { y = oldT.GetComponent<RectTransform>().anchoredPosition.y; Object.DestroyImmediate(oldT.gameObject); }
        else { y = footer.anchoredPosition.y + 10; footer.anchoredPosition += new Vector2(0, -62); }
        var toggle = MakeToggle(panel, SwitchLabel, 0, y, true, udon);
        var objs = new List<GameObject>(); var togs = new List<Toggle>();
        for (int i = 0; i < items.objects.Length; i++)
            if (items.objects[i] != null && items.objects[i].name != RainName && items.objectToggles[i] != null) { objs.Add(items.objects[i]); togs.Add(items.objectToggles[i]); }
        foreach (var g in rains) { objs.Add(g); togs.Add(toggle); }
        items.objects = objs.ToArray(); items.objectToggles = togs.ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(items);
        report.Add("World items switch added: " + SwitchLabel);

        // panel still fits: lowest switch/footer stays inside the panel
        var prt = panel.GetComponent<RectTransform>();
        float bottom = footer.anchoredPosition.y - 30, limit = -(prt != null ? prt.sizeDelta.y : 1200) / 2;
        report.Add("World items panel fits: " + (bottom > limit ? "PASS" : "FAIL (footer at " + bottom + ", panel edge " + limit + ")"));

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // quick simulation check on one cloud: flakes fall below it and drift sideways
        var test = rains.FirstOrDefault(g => g.transform.parent.name.StartsWith("Cloud "));
        if (test != null)
        {
            var ps = test.GetComponent<ParticleSystem>(); ps.Simulate(6f, true, true);
            var parts = new ParticleSystem.Particle[ps.particleCount]; ps.GetParticles(parts);
            float minY = parts.Length > 0 ? parts.Min(p => p.position.y) : 0, spread = parts.Length > 0 ? parts.Max(p => new Vector2(p.position.x, p.position.z).magnitude) : 0;
            report.Add("Sample cloud: " + parts.Length + " flakes, falling to " + minY.ToString("0.0") + " m below, drifting up to " + spread.ToString("0.0") + " m sideways " + (parts.Length > 10 && minY < -.8f ? "PASS" : "CHECK"));
            ps.Clear(); ps.Play();
        }
        File.WriteAllText("Review/claude-round3d-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3D_OK " + string.Join(" | ", report));
    }

    static AnimationCurve Sway(float phase)
    {
        var c = new AnimationCurve();
        for (int k = 0; k <= 8; k++) { float t = k / 8f; c.AddKey(t, Mathf.Sin(t * Mathf.PI * 2.4f + phase)); }
        return c;
    }
    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.anchoredPosition = pos; r.sizeDelta = size; return r; }
    static Toggle MakeToggle(Transform p, string text, float x, float y, bool value, VRC.Udon.UdonBehaviour u)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var r = Rect(text, p, new Vector2(x, y), new Vector2(520, 50));
        var im = r.gameObject.AddComponent<Image>(); im.color = new Color(.2f, .07f, .18f);
        var t = r.gameObject.AddComponent<Toggle>(); t.targetGraphic = im;
        var mark = Rect("Crystal switch", r, new Vector2(-235, 0), new Vector2(22, 22)); mark.localRotation = Quaternion.Euler(0, 0, 45);
        var check = mark.gameObject.AddComponent<Image>(); check.color = new Color(1f, .5f, .82f); t.graphic = check; t.isOn = value;
        var lr = Rect(text, r, new Vector2(20, 0), new Vector2(460, 38));
        var lab = lr.gameObject.AddComponent<Text>(); lab.font = font; lab.text = text; lab.fontSize = 18; lab.color = new Color(1f, .86f, .96f); lab.alignment = TextAnchor.MiddleCenter; lab.raycastTarget = false;
        UnityEventTools.AddStringPersistentListener(t.onValueChanged, u.SendCustomEvent, "Apply");
        return t;
    }
}
