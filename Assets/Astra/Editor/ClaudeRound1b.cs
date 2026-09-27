// Claude round 1b (2026-09-23): star dust in the stair opening + a WORLD ITEMS toggle panel.
// Menu: Astra > Claude Round 1b - Star Dust + Item Toggles. Safe to run more than once.
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

public static class ClaudeRound1b
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    static Font font;
    static List<GameObject> objectList; static List<Toggle> objectToggles; static Transform curPanel; static VRC.Udon.UdonBehaviour curUdon; static float y;
    static void AddObj(string label, GameObject go) { if (go == null) return; objectList.Add(go); objectToggles.Add(MakeToggle(curPanel, label, 0, y, go.activeSelf, curUdon)); y -= 62; }

    [MenuItem("Astra/Claude Round 1b - Star Dust + Item Toggles")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND1B start");
        EnsureProgram("AstraWorldItems");
        var scene = EditorSceneManager.OpenScene(Scene);
        Directory.CreateDirectory("Review/Backups");
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound1b.unity.txt", true);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---------- 1. Star dust filling the 6.4 m drop under the floor opening ----------
        foreach (var old in Find(scene, "10 - Stair opening star dust")) Object.DestroyImmediate(old.gameObject);
        var dustGo = new GameObject("10 - Stair opening star dust");
        SceneManager_Move(dustGo, scene);
        dustGo.transform.position = new Vector3(0, -3.2f, 0);
        var ps = dustGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.playOnAwake = true;
        main.maxParticles = 1800;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.018f, .07f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.4f, .7f, 1.6f, 1f), new Color(2.2f, 1.7f, 2.2f, 1f)); // HDR pinks for bloom
        var emission = ps.emission; emission.rateOverTime = 260;
        var shape = ps.shape;
        shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 4.3f; shape.donutRadius = 1.4f; shape.radiusThickness = 1f;
        shape.rotation = new Vector3(90, 0, 0);
        shape.scale = new Vector3(1, 1, 2.8f); // tube stretched vertically: roughly 0 to -6.4 m
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0, 0); vel.z = new ParticleSystem.MinMaxCurve(0, 0);
        vel.y = new ParticleSystem.MinMaxCurve(.02f, .14f);   // drifts up so you rise out of it
        vel.orbitalX = 0f; vel.orbitalY = .08f; vel.orbitalZ = 0f; vel.radial = 0f;
        var noise = ps.noise; noise.enabled = true; noise.strength = .12f; noise.frequency = .6f; noise.scrollSpeed = .2f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                     new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(1, .8f), new GradientAlphaKey(0, 1) });
        col.color = grad;
        var size = ps.sizeOverLifetime; size.enabled = true;
        var twinkle = new AnimationCurve();
        for (int k = 0; k <= 12; k++) twinkle.AddKey(k / 12f, k % 2 == 0 ? .35f : 1f);
        size.size = new ParticleSystem.MinMaxCurve(1f, twinkle);
        var r = dustGo.GetComponent<ParticleSystemRenderer>();
        string matPath = Root + "Materials/Stair Star Dust.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Astra/Soft Sparkle")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetColor("_Color", Color.white);
        r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard; r.maxParticleSize = .5f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;

        // ---------- 2. WORLD ITEMS panel, mirrored beside the Celestial observatory ----------
        var anchor = Find(scene, "Summoned spellbook anchor").First();
        var celestial = anchor.Find("Celestial observatory");
        var glitter = anchor.Find("Glitter studio");
        var oldPanel = anchor.Find("World items"); if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
        var panel = Object.Instantiate(celestial.gameObject, anchor).transform;
        panel.name = "World items";
        for (int i = panel.childCount - 1; i >= 0; i--) Object.DestroyImmediate(panel.GetChild(i).gameObject);
        foreach (var mb in panel.GetComponents<MonoBehaviour>()) if (mb is UdonSharpBehaviour || mb.GetType().Name == "UdonBehaviour") Object.DestroyImmediate(mb);
        panel.localPosition = celestial.localPosition * 2 - glitter.localPosition;
        var ce = celestial.localEulerAngles; var ge = glitter.localEulerAngles;
        panel.localRotation = Quaternion.Euler(ce.x, ce.y * 2 - ge.y, ce.z);
        var prt = panel.GetComponent<RectTransform>();
        float h = prt != null ? prt.sizeDelta.y : 1200, w = prt != null ? prt.sizeDelta.x : 600;
        var backing = Rect("Opaque backing", panel, Vector2.zero, new Vector2(w, h));
        var bim = backing.gameObject.AddComponent<Image>(); bim.color = new Color(.07f, .035f, .11f, .96f); bim.raycastTarget = false;

        var holder = Find(scene, "Astra world item switches").FirstOrDefault();
        if (holder != null) Object.DestroyImmediate(holder.gameObject);
        var switchesGo = new GameObject("Astra world item switches");
        SceneManager_Move(switchesGo, scene);
        var items = switchesGo.AddUdonSharpComponent<AstraWorldItems>();
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(items);

        float top = h / 2 - 70;
        Label(panel, "WORLD ITEMS", 0, top, w - 40, 30);
        Label(panel, "Switch anything in the world on or off. Only you see the change.", 0, top - 45, w - 40, 17);
        objectList = new List<GameObject>(); objectToggles = new List<Toggle>();
        curPanel = panel; curUdon = udon; y = top - 110;
        AddObj("CHROME POLE", Find(scene, "Infinite Pole - 45mm diameter").First().gameObject);
        var stairToggle = MakeToggle(panel, "SPIRAL STAIRS  (you can still walk on them)", 0, y, true, udon); y -= 62;
        AddObj("STAR DUST UNDER THE FLOOR", dustGo);
        var cinema = Find(scene, "Cinema - synchronized playlist").First();
        var screenToggle = MakeToggle(panel, "VIDEO SCREEN", 0, y, true, udon); y -= 62;
        AddObj("VOID BACKGROUND", Find(scene, "Void cylinder - visual envelope").First().gameObject);
        AddObj("SOFT AVATAR LIGHT", Find(scene, "Soft avatar light").First().gameObject);
        Label(panel, "Glitter, petals, bloom, music and body stardust have their own switches on the other panels.", 0, y - 10, w - 40, 15);

        items.objects = objectList.ToArray(); items.objectToggles = objectToggles.ToArray();
        items.stairRenderers = Find(scene, "09 - Infinite prismatic spiral").First().GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray();
        items.stairToggle = stairToggle;
        items.screenRenderers = cinema.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray();
        items.screenToggle = screenToggle;
        UdonSharpEditorUtility.CopyProxyToUdon(items);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- validation ----------
        ps.Simulate(8f, true, true);
        var parts = new ParticleSystem.Particle[ps.particleCount]; ps.GetParticles(parts);
        float minY = float.MaxValue, maxY = float.MinValue, minR = float.MaxValue, maxR = 0;
        foreach (var p in parts) { var q = p.position; minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y); float rr = new Vector2(q.x, q.z).magnitude; minR = Mathf.Min(minR, rr); maxR = Mathf.Max(maxR, rr); }
        string report = $"Star dust: {parts.Length} particles, y {minY:0.0}..{maxY:0.0} m, radius {minR:0.0}..{maxR:0.0} m (opening is 2.6..6.0 m, drop 0..-6.4 m). World item switches: {objectList.Count} objects + stairs + video screen.";
        ps.Clear();
        File.WriteAllText("Review/claude-round1b-validation.txt", report);
        Debug.Log("CLAUDE_ROUND1B_OK " + report);
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

    static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene scene) { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene); }

    static IEnumerable<Transform> Find(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) yield return t;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.anchoredPosition = pos; r.sizeDelta = size; return r; }
    static Text Label(Transform p, string text, float x, float y, float w, int size) { var r = Rect(text, p, new Vector2(x, y), new Vector2(w, size + 20)); var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = text; t.fontSize = size; t.color = new Color(1f, .86f, .96f); t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false; return t; }
    static Toggle MakeToggle(Transform p, string text, float x, float y, bool value, VRC.Udon.UdonBehaviour u)
    {
        var r = Rect(text, p, new Vector2(x, y), new Vector2(520, 50));
        var im = r.gameObject.AddComponent<Image>(); im.color = new Color(.2f, .07f, .18f);
        var t = r.gameObject.AddComponent<Toggle>(); t.targetGraphic = im;
        var mark = Rect("Crystal switch", r, new Vector2(-235, 0), new Vector2(22, 22)); mark.localRotation = Quaternion.Euler(0, 0, 45);
        var check = mark.gameObject.AddComponent<Image>(); check.color = new Color(1f, .5f, .82f); t.graphic = check; t.isOn = value;
        Label(r, text, 20, 0, 460, 18);
        UnityEventTools.AddStringPersistentListener(t.onValueChanged, u.SendCustomEvent, "Apply");
        return t;
    }
}
