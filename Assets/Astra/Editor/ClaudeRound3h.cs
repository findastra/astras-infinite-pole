// Claude round 3h (2026-09-24): stage lights on the DJ dance cloud. 8 light towers ring the decks; each has a
// moving head that sweeps a glowing color-cycling beam and blasts bursts of sparkles along it, plus a few real
// spotlights that light up avatars on the dance floor. Run after round 3f. Safe to rerun.
// Menu: Astra > Claude Round 3h - Stage Lights
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

public static class ClaudeRound3h
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const int Towers = 8, RealLights = 4;
    const float TowerRadius = 7.5f, TowerHeight = 3.2f, BeamLength = 22f;

    [MenuItem("Astra/Claude Round 3h - Stage Lights")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND3H start");
        EnsureProgram("AstraStageLights");
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3h.unity.txt", true);
        var report = new List<string>();
        var dj = GameObject.Find("12 - DJ cloud");
        var old = dj.transform.Find("Stage lights"); if (old != null) Object.DestroyImmediate(old.gameObject);
        var stage = dj.transform.Find("DJ stage cloud");
        float top = stage.localPosition.y;

        var towerMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/DJ Deck.mat");
        var lampMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Phone Booth Sign Glow.mat") ?? towerMat;
        string beamPath = Root + "Materials/Stage Light Beam.mat";
        var beamMat = AssetDatabase.LoadAssetAtPath<Material>(beamPath);
        if (beamMat == null) { beamMat = new Material(Shader.Find("Astra/Light Beam")); AssetDatabase.CreateAsset(beamMat, beamPath); }
        beamMat.shader = Shader.Find("Astra/Light Beam"); beamMat.enableInstancing = true; EditorUtility.SetDirty(beamMat);
        string sparkPath = Root + "Materials/Stage Light Sparks.mat";
        var sparkMat = AssetDatabase.LoadAssetAtPath<Material>(sparkPath);
        if (sparkMat == null) { sparkMat = new Material(Shader.Find("Astra/Soft Sparkle")); AssetDatabase.CreateAsset(sparkMat, sparkPath); }
        sparkMat.SetColor("_Color", Color.white); sparkMat.enableInstancing = true; EditorUtility.SetDirty(sparkMat);
        string conePath = Root + "Meshes/Stage Light Beam.asset";
        var cone = AssetDatabase.LoadAssetAtPath<Mesh>(conePath);
        if (cone == null) { cone = new Mesh(); AssetDatabase.CreateAsset(cone, conePath); }
        BuildCone(cone, BeamLength, .12f, 1.6f, 20); EditorUtility.SetDirty(cone);

        var rig = new GameObject("Stage lights").transform; rig.SetParent(dj.transform, false); rig.localPosition = new Vector3(0, top, -.4f);
        var lights = rig.gameObject.AddUdonSharpComponent<AstraStageLights>();
        var heads = new List<Transform>(); var yaws = new List<float>();
        Color[] palette = { new Color(2.6f, .6f, 2.0f), new Color(1.6f, .8f, 2.8f), new Color(2.8f, 1.6f, 2.4f), new Color(.9f, 1.4f, 2.8f) };
        for (int i = 0; i < Towers; i++)
        {
            float a = (i + .5f) * 360f / Towers;
            var tower = new GameObject("Light tower " + (i + 1)).transform; tower.SetParent(rig, false);
            tower.localPosition = Quaternion.Euler(0, a, 0) * new Vector3(0, 0, TowerRadius);
            Prim(PrimitiveType.Cylinder, "Truss", tower, new Vector3(0, TowerHeight / 2, 0), new Vector3(.14f, TowerHeight / 2, .14f), towerMat);
            Prim(PrimitiveType.Cylinder, "Base", tower, new Vector3(0, .05f, 0), new Vector3(.6f, .05f, .6f), towerMat);
            var head = new GameObject("Moving head").transform; head.SetParent(tower, false); head.localPosition = new Vector3(0, TowerHeight + .2f, 0);
            Prim(PrimitiveType.Cube, "Lamp body", head, new Vector3(0, 0, 0), new Vector3(.36f, .3f, .46f), towerMat);
            Prim(PrimitiveType.Cylinder, "Lens", head, new Vector3(0, 0, .24f), new Vector3(.26f, .02f, .26f), lampMat).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var beam = new GameObject("Beam"); beam.transform.SetParent(head, false); beam.transform.localPosition = new Vector3(0, 0, .25f);
            beam.AddComponent<MeshFilter>().sharedMesh = cone;
            var br = beam.AddComponent<MeshRenderer>(); br.sharedMaterial = beamMat; br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; br.receiveShadows = false;
            // sparkle blast along the beam
            var sp = new GameObject("Sparkle blast"); sp.transform.SetParent(head, false); sp.transform.localPosition = new Vector3(0, 0, .3f);
            var ps = sp.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = true; main.prewarm = true; main.duration = 2.4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(.03f, .09f); main.maxParticles = 220; main.gravityModifier = .15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(palette[i % palette.Length], new Color(3f, 2.8f, 3.2f));
            var em = ps.emission; em.rateOverTime = 14; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 45), new ParticleSystem.Burst(1.2f, 30) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 4f; sh.radius = .08f; sh.rotation = Vector3.zero;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .5f), new GradientAlphaKey(0, 1) }); col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, .2f));
            var pr = sp.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = sparkMat; pr.maxParticleSize = .03f; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // a few real spotlights to light up avatars (no shadows, limited range, kept cheap)
            if (i % (Towers / RealLights) == 0)
            {
                var lg = new GameObject("Spotlight"); lg.transform.SetParent(head, false);
                var l = lg.AddComponent<Light>(); l.type = LightType.Spot; l.range = 24; l.spotAngle = 32; l.intensity = 2.2f; l.color = new Color(1f, .55f, .9f); l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
            }
            heads.Add(head); yaws.Add(a + 180f); // face inward over the dance floor, sweeping out to the sky
        }
        lights.heads = heads.ToArray(); lights.yawBase = yaws.ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(lights);
        report.Add("Stage lights: " + Towers + " towers " + TowerRadius + " m around the decks, sweeping color-cycling beams (" + BeamLength + " m), sparkle blasts on each, " + RealLights + " real spotlights for avatars");

        // World items switch
        var items = Object.FindObjectOfType<AstraWorldItems>(); var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(items);
        Transform panel = null; foreach (var r in scene.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(true)) if (t.name == "World items") panel = t;
        const string label = "STAGE LIGHTS";
        var oldT = panel.Find(label);
        var footer = panel.GetComponentsInChildren<Text>(true).First(t => t.text.StartsWith("Glitter, petals")).GetComponent<RectTransform>();
        float y; if (oldT != null) { y = oldT.GetComponent<RectTransform>().anchoredPosition.y; Object.DestroyImmediate(oldT.gameObject); } else { y = footer.anchoredPosition.y + 10; footer.anchoredPosition += new Vector2(0, -62); }
        var toggle = MakeToggle(panel, label, 0, y, true, udon);
        var objs = new List<GameObject>(); var togs = new List<Toggle>();
        for (int i = 0; i < items.objects.Length; i++) if (items.objects[i] != null && items.objects[i].name != "Stage lights" && items.objectToggles[i] != null) { objs.Add(items.objects[i]); togs.Add(items.objectToggles[i]); }
        objs.Add(rig.gameObject); togs.Add(toggle);
        items.objects = objs.ToArray(); items.objectToggles = togs.ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(items);
        var prt = panel.GetComponent<RectTransform>();
        report.Add("World items panel fits: " + (footer.anchoredPosition.y - 30 > -(prt != null ? prt.sizeDelta.y : 1200) / 2 ? "PASS" : "FAIL"));

        // keep-clear zone for the DJ cloud must cover the towers' height too
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>();
        if (orbit != null && orbit.clearZones != null)
            for (int k = 0; k < orbit.clearZones.Length; k++)
                if (orbit.clearZones[k].name.Contains("DJ")) { var h = orbit.clearHalf[k]; float need = TowerHeight + 3f; if (orbit.clearZones[k].position.y + h.y < top + need) { h.y += need / 2; orbit.clearZones[k].position += Vector3.up * need / 2; orbit.clearHalf[k] = h; } }
        if (orbit != null) UdonSharpEditorUtility.CopyProxyToUdon(orbit);

        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        bool onStage = heads.All(hd => Physics.Raycast(hd.parent.position + Vector3.up * .5f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore));
        report.Add("Every tower stands on the dance cloud: " + (onStage ? "PASS" : "FAIL"));
        report.Add("Beam shader compiles: " + (ShaderUtil.ShaderHasError(beamMat.shader) ? "FAIL" : "PASS"));
        File.WriteAllText("Review/claude-round3h-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3H_OK " + string.Join(" | ", report));
    }

    static void BuildCone(Mesh m, float length, float r0, float r1, int sides)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            v.Add(d * r0); v.Add(d * r1 + Vector3.forward * length);
            n.Add(d); n.Add(d); uv.Add(new Vector2(i / (float)sides, 0)); uv.Add(new Vector2(i / (float)sides, 1));
        }
        for (int i = 0; i < sides; i++) { int k = i * 2; t.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3 }); }
        m.Clear(); m.name = "Stage light beam"; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
    }
    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
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
