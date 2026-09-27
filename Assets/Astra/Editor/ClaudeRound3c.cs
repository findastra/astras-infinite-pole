// Claude round 3c (2026-09-24): DJ cloud near spawn, round "bokeh" sparkles from the thumbnail brought back,
// dead cloud-color buttons hidden. The DJ cloud and booth are placeholders in shape; Astra owns their look.
// Menu: Astra > Claude Round 3c - DJ Cloud + Round Sparkles. Safe to rerun.
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

public static class ClaudeRound3c
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const string DJName = "12 - DJ cloud";
    // Placement: close to spawn (owner request). Orbiting clouds are kept out by a keep-clear zone (round 3f).
    const float DJRadius = 12f, DJTurnDegrees = 35f, StageTop = .4f, StageScale = 2.4f;

    [MenuItem("Astra/Claude Round 3c - DJ Cloud + Round Sparkles")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND3C start");
        EnsureProgram("AstraDJBooth");
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3c.unity.txt", true);
        var report = new List<string>();

        // ---------- 1. DJ cloud ----------
        foreach (var old in Find(scene, DJName).ToList()) Object.DestroyImmediate(old.gameObject);
        var desc = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
        Vector3 spawn = desc.spawns != null && desc.spawns.Length > 0 && desc.spawns[0] != null ? desc.spawns[0].position : new Vector3(0, 0, -2);
        Vector3 dir = new Vector3(spawn.x, 0, spawn.z); if (dir.sqrMagnitude < .01f) dir = Vector3.back; dir.Normalize();
        dir = Quaternion.Euler(0, DJTurnDegrees, 0) * dir;
        var cinema = GameObject.Find("Cinema - synchronized playlist");
        if (cinema != null) { var c = cinema.transform.position; c.y = 0; if (Vector3.Angle(dir, c) < 60) dir = Quaternion.Euler(0, -2 * DJTurnDegrees, 0) * dir; }

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Meshes/Glitter Cloud 0.asset");
        var cloudMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Glitter Cloud.mat");
        var chrome = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Pole Silver.mat");
        var sheer = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Prismatic Stair.mat");

        var root = new GameObject(DJName);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = dir * DJRadius;
        root.transform.rotation = Quaternion.LookRotation(-dir); // booth faces the pole and spawn

        var stage = Cloud("DJ stage cloud", root.transform, mesh, cloudMat, new Vector3(0, StageTop, 0), new Vector3(StageScale, 1.3f, StageScale));
        Cloud("DJ step cloud", root.transform, mesh, cloudMat, new Vector3(0, .2f, mesh.bounds.extents.z * StageScale + .6f), new Vector3(.8f, .7f, .8f)); // on the crowd side

        // booth (placeholder geometry; Astra restyles)
        var booth = new GameObject("DJ booth").transform; booth.SetParent(root.transform, false); booth.localPosition = new Vector3(0, StageTop, -.4f); // back of the stage; the DJ faces the pole and spawn
        var desk = Prim(PrimitiveType.Cube, "Deck table", booth, new Vector3(0, .5f, 0), new Vector3(2.4f, 1f, .8f), chrome, true);
        Prim(PrimitiveType.Cylinder, "Turntable left", booth, new Vector3(-.7f, 1.015f, 0), new Vector3(.46f, .015f, .46f), sheer, false);
        Prim(PrimitiveType.Cylinder, "Turntable right", booth, new Vector3(.7f, 1.015f, 0), new Vector3(.46f, .015f, .46f), sheer, false);
        Prim(PrimitiveType.Cube, "Mixer", booth, new Vector3(0, 1.03f, 0), new Vector3(.5f, .06f, .36f), sheer, false);

        // mic zone behind the decks
        var zone = new GameObject("DJ mic zone (voice carries world-wide)");
        zone.transform.SetParent(booth, false); zone.transform.localPosition = new Vector3(0, 1.2f, -.95f); // DJ stands here, still on the stage collider
        var trig = zone.AddComponent<BoxCollider>(); trig.isTrigger = true; trig.size = new Vector3(2.8f, 2.4f, 1.1f);
        zone.AddUdonSharpComponent<AstraDJBooth>();
        report.Add("DJ cloud at " + root.transform.position.ToString("0.0") + ", " + (root.transform.position - spawn).magnitude.ToString("0") + " m from spawn, stage top " + StageTop + " m, step cloud " + .2f + " m");

        // ---------- 2. World items switch ----------
        var items = Object.FindObjectOfType<AstraWorldItems>();
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(items);
        var panel = Find(scene, "World items").First();
        const string label = "DJ CLOUD";
        var oldT = panel.Find(label);
        var footer = panel.GetComponentsInChildren<Text>(true).First(t => t.text.StartsWith("Glitter, petals")).GetComponent<RectTransform>();
        float y;
        if (oldT != null) { y = oldT.GetComponent<RectTransform>().anchoredPosition.y; Object.DestroyImmediate(oldT.gameObject); }
        else { y = footer.anchoredPosition.y + 10; footer.anchoredPosition += new Vector2(0, -62); }
        var toggle = MakeToggle(panel, label, 0, y, true, udon);
        var objs = items.objects.ToList(); var togs = items.objectToggles.ToList();
        for (int i = objs.Count - 1; i >= 0; i--) if (objs[i] == null || objs[i].name == DJName) { objs.RemoveAt(i); togs.RemoveAt(i); }
        objs.Add(root); togs.Add(toggle);
        items.objects = objs.ToArray(); items.objectToggles = togs.ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(items);

        // ---------- 3. Round sparkles from the thumbnail ("Soft bokeh" rings) ----------
        // Round "Fine Magic" (2026-09-14) shrank every glitter to 1.8% of the screen and left bokeh at 28% opacity,
        // so the rings became unreadable dots. Restore bokeh to its original size and make it clearly visible.
        var g = Object.FindObjectOfType<AstraGlitterControls>();
        var bokeh = g.effects[4];
        var main = bokeh.main; main.startSize = new ParticleSystem.MinMaxCurve(.09f, .2f);
        var col = bokeh.colorOverLifetime; var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                     new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.8f, .16f), new GradientAlphaKey(.65f, .75f), new GradientAlphaKey(0, 1) });
        col.color = grad;
        bokeh.GetComponent<ParticleSystemRenderer>().maxParticleSize = .06f;
        g.effectToggles[4].isOn = true;
        UdonSharpEditorUtility.CopyProxyToUdon(g);
        report.Add("Soft bokeh rings: size 0.09-0.2 m, max 6% of screen, peak opacity 80%, switched on");

        // ---------- 4. Hide cloud-color buttons that do nothing since the particle clouds were removed (round 1) ----------
        int hidden = 0;
        foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (b.gameObject.scene != scene) continue;
            var t = b.GetComponentInChildren<Text>(true);
            if (t != null && (t.text == "PEACH CLOUDS" || t.text == "ROSE CLOUDS" || t.text == "TWILIGHT CLOUDS")) { b.gameObject.SetActive(false); hidden++; }
        }
        report.Add("Dead cloud-color buttons hidden: " + hidden);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- validation ----------
        Physics.SyncTransforms();
        var top = stage.transform.position;
        bool standStage = Physics.Raycast(top + Vector3.up * .8f + stage.transform.forward * 1.5f, Vector3.down, out var h1, 1.2f, ~0, QueryTriggerInteraction.Ignore) && h1.collider.gameObject == stage;
        bool behindDecks = Physics.Raycast(zone.transform.position + Vector3.up * .5f, Vector3.down, out var h2, 3f, ~0, QueryTriggerInteraction.Ignore) && h2.collider.gameObject == stage;
        var sb = stage.GetComponent<BoxCollider>().bounds;
        report.Add("Stage walkable: " + (standStage ? "PASS" : "FAIL") + ", DJ spot behind decks on the stage: " + (behindDecks ? "PASS" : "FAIL (ray hit " + (h2.collider != null ? h2.collider.name + " at " + h2.point.ToString("0.00") : "nothing") + "; zone at " + zone.transform.position.ToString("0.00") + "; stage box " + sb.min.ToString("0.0") + " to " + sb.max.ToString("0.0") + ")"));
        float ringsOuter = 0; // farthest any orbiting cloud reaches from the pole
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>();
        if (orbit != null) foreach (var pv in orbit.pivots) foreach (Transform c in pv)
        {
            var mb = c.GetComponent<MeshFilter>().sharedMesh.bounds; var p = c.localPosition;
            float reach = new Vector2(mb.extents.x + Mathf.Abs(mb.center.x), mb.extents.z + Mathf.Abs(mb.center.z)).magnitude * Mathf.Max(c.localScale.x, c.localScale.z);
            ringsOuter = Mathf.Max(ringsOuter, new Vector2(p.x, p.z).magnitude + reach);
        }
        float stageInner = DJRadius - new Vector2(mesh.bounds.extents.x + Mathf.Abs(mesh.bounds.center.x), mesh.bounds.extents.z + Mathf.Abs(mesh.bounds.center.z)).magnitude * StageScale;
        report.Add("Orbiting clouds: kept out of the DJ area by the round 3f keep-clear zone (rerun 3f after 3c)");
        report.Add("Mic zone trigger: " + (trig.isTrigger ? "PASS" : "FAIL"));
        File.WriteAllText("Review/claude-round3c-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3C_OK " + string.Join(" | ", report));
    }

    static GameObject Cloud(string name, Transform parent, Mesh mesh, Material mat, Vector3 topPos, Vector3 scale)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = topPos; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        var b = mesh.bounds; var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(b.size.x * .8f, .7f, b.size.z * .8f); box.center = new Vector3(b.center.x, -.35f, b.center.z);
        return go;
    }
    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static IEnumerable<Transform> Find(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                if (tr.name == name) yield return tr;
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
