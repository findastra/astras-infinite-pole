// Claude round 3a (2026-09-24): glittery cloud platforms to jump on + sheerer stairs.
// Clouds ride on each recycled stair turn, so they repeat forever up AND down with no new Udon logic.
// Menu: Astra > Claude Round 3a - Cloud Platforms. Safe to rerun.
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

public static class ClaudeRound3a
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const string CloudName = "Cloud platform";
    const float StairOpacity = .3f; // was .6 (owner: "more see through")

    // Per turn: angle (rad, same convention as the stairs), radius (m), top height above the turn (m).
    // Tops stay between 0 and 4.2 m so a cloud never lands in the cramped gap just under the floor.
    // Stair height at angle a = (a + PI/2) / (2 PI) * 6.4, so each stair-side cloud sits 0.3 m above its step.
    static readonly Vector3[] Spots = {
        new Vector3(-0.196f, 7.6f, 1.70f),  // hop off the stairs
        new Vector3( 0.150f, 9.8f, 2.05f),  // next hop outward
        new Vector3( 1.571f, 7.6f, 3.50f),  // hop off the stairs
        new Vector3( 1.900f, 9.8f, 3.80f),  // next hop outward
    };

    [MenuItem("Astra/Claude Round 3a - Cloud Platforms")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND3A start");
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        var scene = EditorSceneManager.OpenScene(Scene);
        Directory.CreateDirectory("Review/Backups");
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3a.unity.txt", true);
        var report = new List<string>();

        // ---------- 1. Sheerer stairs ----------
        var stairMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Prismatic Stair.mat");
        stairMat.SetFloat("_Opacity", StairOpacity); EditorUtility.SetDirty(stairMat);
        report.Add("Stair sheer amount: " + StairOpacity);

        // ---------- 2. Cloud mesh + material ----------
        string meshPath = Root + "Meshes/Glitter Cloud.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, meshPath); }
        BuildCloud(mesh); EditorUtility.SetDirty(mesh);
        string matPath = Root + "Materials/Glitter Cloud.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Astra/Glitter Cloud")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.shader = Shader.Find("Astra/Glitter Cloud");
        mat.enableInstancing = true; EditorUtility.SetDirty(mat);

        // ---------- 3. Clouds on every stair turn ----------
        var spiral = Object.FindObjectOfType<AstraSpiral>();
        var clouds = new List<GameObject>();
        foreach (var turn in spiral.turns)
        {
            for (int c = turn.childCount - 1; c >= 0; c--)
                if (turn.GetChild(c).name.StartsWith(CloudName)) Object.DestroyImmediate(turn.GetChild(c).gameObject);
            for (int k = 0; k < Spots.Length; k++)
            {
                var s = Spots[k];
                var go = new GameObject(CloudName + " " + (k + 1));
                go.transform.SetParent(turn, false);
                go.transform.localPosition = new Vector3(Mathf.Cos(s.x) * s.y, s.z, Mathf.Sin(s.x) * s.y);
                // cloud's long side runs along the circle, short side points at the pole
                go.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(s.x), 0, Mathf.Sin(s.x)));
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                var box = go.AddComponent<BoxCollider>(); box.center = new Vector3(0, -.4f, 0); box.size = new Vector3(2.9f, .8f, 1.9f); // flat top at the cloud's surface
                clouds.Add(go);
            }
        }
        report.Add("Cloud platforms: " + clouds.Count + " (" + Spots.Length + " per stair turn x " + spiral.turns.Length + " turns), " + mesh.vertexCount + " vertices each, GPU instanced");

        // ---------- 4. CLOUD PLATFORMS switch on the World items panel ----------
        var items = Object.FindObjectOfType<AstraWorldItems>();
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(items);
        var panel = Find(scene, "World items").First();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var old = panel.Find("CLOUD PLATFORMS  (jump on them)");
        var footer = panel.GetComponentsInChildren<Text>(true).First(t => t.text.StartsWith("Glitter, petals"));
        var footerRt = footer.GetComponent<RectTransform>();
        float y;
        if (old != null) { y = old.GetComponent<RectTransform>().anchoredPosition.y; Object.DestroyImmediate(old.gameObject); }
        else { y = footerRt.anchoredPosition.y + 10; footerRt.anchoredPosition += new Vector2(0, -62); }
        var toggle = MakeToggle(panel, "CLOUD PLATFORMS  (jump on them)", 0, y, true, udon, font);
        items.cloudObjects = clouds.ToArray(); items.cloudToggle = toggle;
        // stairs switch covers only the stair treads, not the clouds
        items.stairRenderers = spiral.turns.Select(t => (Renderer)t.GetComponent<MeshRenderer>()).ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(items);
        report.Add("World items switch added: CLOUD PLATFORMS");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- validation ----------
        spiral.Recenter(0); Physics.SyncTransforms();
        int ok = 0;
        foreach (float turnY in new[] { 0f, 6.4f, -6.4f, 32f, -32f })
            foreach (var s in Spots)
            {
                var top = new Vector3(Mathf.Cos(s.x) * s.y, turnY + s.z, Mathf.Sin(s.x) * s.y);
                if (Physics.Raycast(top + Vector3.up * .5f, Vector3.down, out var hit, 1f) && hit.collider.name.StartsWith(CloudName) && Mathf.Abs(hit.point.y - top.y) < .02f) ok++;
                else report.Add("FAIL: no standable cloud at " + top);
            }
        report.Add("Standable cloud tops found: " + ok + "/" + (5 * Spots.Length));
        // the stair next to each stair-side cloud stays walkable (cloud doesn't block it)
        foreach (var s in new[] { Spots[0], Spots[2] })
        {
            float stepY = (s.x + Mathf.PI / 2) / (Mathf.PI * 2) * 6.4f;
            var p = new Vector3(Mathf.Cos(s.x) * 4.3f, stepY + 1f, Mathf.Sin(s.x) * 4.3f);
            bool stair = Physics.Raycast(p, Vector3.down, out var h2, 1.3f) && h2.collider.name.StartsWith("Spiral turn");
            report.Add("Stair beside cloud walkable: " + (stair ? "PASS" : "FAIL"));
        }
        report.Add("Cloud shader compiles: " + (ShaderUtil.ShaderHasError(mat.shader) ? "FAIL" : "PASS"));
        File.WriteAllText("Review/claude-round3a-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3A_OK " + string.Join(" | ", report));
    }

    // ---------- puffy cloud: overlapping low-poly spheres, flat-ish top at y = 0 ----------
    static void BuildCloud(Mesh m)
    {
        var puffs = new[] { // center, scale
            (new Vector3(0, -.45f, 0),        new Vector3(1.55f, .50f, 1.10f)),
            (new Vector3(-.85f, -.40f, .10f), new Vector3(.80f, .48f, .72f)),
            (new Vector3(.80f, -.42f, -.10f), new Vector3(.85f, .50f, .75f)),
            (new Vector3(0, -.35f, .40f),     new Vector3(.78f, .43f, .62f)),
            (new Vector3(.10f, -.38f, -.45f), new Vector3(.72f, .45f, .58f)),
            (new Vector3(-.20f, -.75f, -.10f),new Vector3(1.0f, .40f, .80f)),
        };
        Ico(out var sv, out var st);
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
        foreach (var (c, s) in puffs)
        {
            int k = v.Count;
            foreach (var d in sv) { v.Add(c + Vector3.Scale(d, s)); n.Add(new Vector3(d.x / s.x, d.y / s.y, d.z / s.z).normalized); }
            foreach (var i in st) t.Add(k + i);
        }
        m.Clear(); m.name = "Glitter cloud"; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
    }

    static void Ico(out List<Vector3> verts, out List<int> tris)
    {
        float p = (1 + Mathf.Sqrt(5)) / 2;
        verts = new List<Vector3> { new Vector3(-1,p,0), new Vector3(1,p,0), new Vector3(-1,-p,0), new Vector3(1,-p,0), new Vector3(0,-1,p), new Vector3(0,1,p),
            new Vector3(0,-1,-p), new Vector3(0,1,-p), new Vector3(p,0,-1), new Vector3(p,0,1), new Vector3(-p,0,-1), new Vector3(-p,0,1) };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
        tris = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
        // one subdivision: 42 vertices, 80 triangles per puff
        var cache = new Dictionary<long, int>(); var outT = new List<int>();
        var vv = verts;
        int Mid(int a, int b) { long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; if (cache.TryGetValue(key, out int r)) return r; vv.Add(((vv[a] + vv[b]) / 2).normalized); cache[key] = vv.Count - 1; return vv.Count - 1; }
        for (int i = 0; i < tris.Count; i += 3)
        {
            int a = tris[i], b = tris[i + 1], c = tris[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
            outT.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
        }
        tris = outT;
    }

    static IEnumerable<Transform> Find(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                if (tr.name == name) yield return tr;
    }

    // same look as the Round 1b switches
    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.anchoredPosition = pos; r.sizeDelta = size; return r; }
    static Toggle MakeToggle(Transform p, string text, float x, float y, bool value, VRC.Udon.UdonBehaviour u, Font font)
    {
        var r = Rect(text, p, new Vector2(x, y), new Vector2(520, 50));
        var im = r.gameObject.AddComponent<Image>(); im.color = new Color(.2f, .07f, .18f);
        var t = r.gameObject.AddComponent<Toggle>(); t.targetGraphic = im;
        var mark = Rect("Crystal switch", r, new Vector2(-235, 0), new Vector2(22, 22)); mark.localRotation = Quaternion.Euler(0, 0, 45);
        var check = mark.gameObject.AddComponent<Image>(); check.color = new Color(1f, .5f, .82f); t.graphic = check; t.isOn = value;
        var lr = Rect(text, r, new Vector2(20, 0), new Vector2(460, 38));
        var label = lr.gameObject.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = 18; label.color = new Color(1f, .86f, .96f); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        UnityEventTools.AddStringPersistentListener(t.onValueChanged, u.SendCustomEvent, "Apply");
        return t;
    }
}
