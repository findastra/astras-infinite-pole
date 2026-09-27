// Claude round 3b (2026-09-24): clouds circle the stairs in rings, alternating clockwise and counterclockwise,
// further out rings for cloud-to-cloud hops, random placement / rotation / size, 8 unique shapes.
// Replaces the fixed round 3a clouds. Menu: Astra > Claude Round 3b - Orbiting Clouds. Safe to rerun (same seed).
// LOOK is Astra's: Shaders/GlitterCloud.shader and Meshes/Glitter Cloud 0-7.asset may be restyled freely
// as long as each mesh keeps its walkable top near y = 0 and roughly the same footprint (colliders use these bounds).
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeRound3b
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const int Rings = 5, PerRing = 2, Shapes = 8, Seed = 3141;
    const float InnerRing = 7.8f, RingGap = 2.1f;   // stairs end at 5.8 m (closer rings since 2026-09-24: easier hops)
    const float MinTop = .3f, MaxTop = 4.1f;         // keeps clouds out of the cramped gap under the floor

    [MenuItem("Astra/Claude Round 3b - Orbiting Clouds")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND3B start");
        EnsureProgram("AstraCloudOrbit");
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3b.unity.txt", true);
        var rng = new System.Random(Seed);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        var report = new List<string>();

        // ---------- unique cloud shapes ----------
        var meshes = new Mesh[Shapes];
        for (int s = 0; s < Shapes; s++)
        {
            string path = Root + "Meshes/Glitter Cloud " + s + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = m == null;
            if (fresh) { m = new Mesh(); AssetDatabase.CreateAsset(m, path); }
            if (fresh) BuildCloud(m, "Glitter Cloud " + s, rng); // existing shapes are kept (round 3f makes them rounder; Astra may restyle)
            else rng.NextDouble();
            EditorUtility.SetDirty(m); meshes[s] = m;
        }
        if (AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Meshes/Glitter Cloud.asset") != null) AssetDatabase.DeleteAsset(Root + "Meshes/Glitter Cloud.asset"); // round 3a single shape
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Glitter Cloud.mat");

        // ---------- rings on every stair turn ----------
        var spiral = Object.FindObjectOfType<AstraSpiral>();
        foreach (var turn in spiral.turns)
            for (int c = turn.childCount - 1; c >= 0; c--)
            { var n = turn.GetChild(c).name; if (n.StartsWith("Cloud platform") || n.StartsWith("Cloud ring")) Object.DestroyImmediate(turn.GetChild(c).gameObject); }
        foreach (var old in Object.FindObjectsOfType<AstraCloudOrbit>()) Object.DestroyImmediate(old.gameObject);

        var pivots = new List<Transform>(); var speeds = new List<float>(); var phases = new List<float>(); var pivotTurns = new List<Transform>();
        var clouds = new List<Transform>();
        float minClear = 99;
        foreach (var turn in spiral.turns)
            for (int ring = 0; ring < Rings; ring++)
            {
                var pivot = new GameObject("Cloud ring " + (ring + 1) + (ring % 2 == 0 ? " (clockwise)" : " (counterclockwise)")).transform;
                pivot.SetParent(turn, false);
                pivots.Add(pivot); pivotTurns.Add(turn);
                speeds.Add((ring % 2 == 0 ? 1 : -1) * R(2.2f, 5.5f)); // 1 lap every 1-3 minutes
                phases.Add(R(0, 360));
                float a0 = R(0, Mathf.PI * 2);
                for (int k = 0; k < PerRing; k++)
                {
                    float scale = R(.6f, 1.6f);
                    float angle = a0 + k * Mathf.PI + R(-.9f, .9f);
                    float radius = InnerRing + ring * RingGap + R(-.2f, .2f);
                    var mesh = meshes[rng.Next(Shapes)];
                    var sx = scale * R(.9f, 1.15f); var sz = scale * R(.9f, 1.15f);
                    float reach = new Vector2(mesh.bounds.extents.x + Mathf.Abs(mesh.bounds.center.x), mesh.bounds.extents.z + Mathf.Abs(mesh.bounds.center.z)).magnitude * Mathf.Max(sx, sz);
                    if (ring == 0) { radius = Mathf.Max(radius, 5.8f + .4f + reach); minClear = Mathf.Min(minClear, radius - reach - 5.8f); } // never touches the stairs
                    var go = new GameObject("Cloud " + clouds.Count);
                    go.transform.SetParent(pivot, false);
                    // stepped heights: each ring outward sits ~0.3 m higher, so every outward hop is a small jump up (two staircases per turn)
                    float top = Mathf.Clamp((k == 0 ? .5f : 2.3f) + ring * .3f + R(-.08f, .08f), MinTop, MaxTop);
                    go.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, top, Mathf.Sin(angle) * radius);
                    go.transform.localRotation = Quaternion.Euler(0, R(0, 360), 0);
                    go.transform.localScale = new Vector3(sx, scale * R(.85f, 1.2f), sz);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    var b = mesh.bounds; var box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(b.size.x * .8f, .7f, b.size.z * .8f); box.center = new Vector3(b.center.x, -.35f, b.center.z); // flat top at y = 0
                    clouds.Add(go.transform);
                }
            }

        var orbitGo = new GameObject("Astra cloud orbits");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(orbitGo, scene);
        var orbit = orbitGo.AddUdonSharpComponent<AstraCloudOrbit>();
        orbit.pivots = pivots.ToArray(); orbit.speeds = speeds.ToArray(); orbit.phases = phases.ToArray(); orbit.pivotTurns = pivotTurns.ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(orbit);

        // World items CLOUD PLATFORMS switch now controls the rings
        var items = Object.FindObjectOfType<AstraWorldItems>();
        items.cloudObjects = pivots.Select(p => p.gameObject).ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(items);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- validation (at the start pose) ----------
        spiral.Recenter(0);
        for (int i = 0; i < pivots.Count; i++) pivots[i].localRotation = Quaternion.Euler(0, phases[i], 0);
        Physics.SyncTransforms();
        int standable = 0, tested = 0;
        foreach (var c in clouds.Where(c => Mathf.Abs(c.position.y) < 40))
        {
            tested++;
            var top = c.position;
            if (Physics.Raycast(top + Vector3.up * .6f, Vector3.down, out var hit, 1f) && hit.collider.transform == c && Mathf.Abs(hit.point.y - top.y) < .02f) standable++;
        }
        report.Add("Clouds: " + clouds.Count + " (" + Rings + " rings x " + PerRing + " per ring x " + spiral.turns.Length + " turns), " + Shapes + " unique shapes, sizes 0.6-1.6x, random rotation");
        report.Add("Rings: " + InnerRing + "-" + (InnerRing + (Rings - 1) * RingGap) + " m from the pole, alternating clockwise / counterclockwise, 1 lap per 1-3 min");
        report.Add("Standable cloud tops (within 40 m of spawn): " + standable + "/" + tested + (standable == tested ? " PASS" : " FAIL"));
        report.Add("Inner ring clearance from stairs: " + minClear.ToString("0.00") + " m " + (minClear > 0 ? "PASS" : "FAIL"));
        report.Add("Cloud material instanced: " + mat.enableInstancing + ", vertices per cloud: " + meshes.Min(m => m.vertexCount) + "-" + meshes.Max(m => m.vertexCount));
        File.WriteAllText("Review/claude-round3b-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3B_OK " + string.Join(" | ", report));
    }

    // ---------- a cloud: random overlapping puffs, walkable top near y = 0 ----------
    static void BuildCloud(Mesh m, string name, System.Random rng)
    {
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        Ico(out var sv, out var st);
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
        void Puff(Vector3 c, Vector3 s)
        {
            int k = v.Count;
            foreach (var d in sv) { v.Add(c + Vector3.Scale(d, s)); n.Add(new Vector3(d.x / s.x, d.y / s.y, d.z / s.z).normalized); }
            foreach (var i in st) t.Add(k + i);
        }
        float len = R(1.2f, 1.7f), wid = R(.8f, 1.1f);
        Puff(new Vector3(0, -.45f, 0), new Vector3(len, .5f, wid));                  // body
        int puffs = rng.Next(4, 8);
        for (int i = 0; i < puffs; i++)
        {
            float a = R(0, Mathf.PI * 2), d = R(.2f, .75f);
            var s = new Vector3(R(.5f, .9f), R(.35f, .6f), R(.45f, .8f));
            var c = new Vector3(Mathf.Cos(a) * d * len, 0, Mathf.Sin(a) * d * wid);
            c.y = R(.02f, .1f) - s.y;                                                   // tops just above the walk line
            Puff(c, s);
        }
        Puff(new Vector3(R(-.3f, .3f), -.8f, R(-.2f, .2f)), new Vector3(len * .75f, .38f, wid * .7f)); // soft underside
        m.Clear(); m.name = name; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
    }

    static void Ico(out List<Vector3> verts, out List<int> tris)
    {
        float p = (1 + Mathf.Sqrt(5)) / 2;
        var vv = new List<Vector3> { new Vector3(-1,p,0), new Vector3(1,p,0), new Vector3(-1,-p,0), new Vector3(1,-p,0), new Vector3(0,-1,p), new Vector3(0,1,p),
            new Vector3(0,-1,-p), new Vector3(0,1,-p), new Vector3(p,0,-1), new Vector3(p,0,1), new Vector3(-p,0,-1), new Vector3(-p,0,1) };
        for (int i = 0; i < vv.Count; i++) vv[i] = vv[i].normalized;
        var tt = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
        var cache = new Dictionary<long, int>(); var outT = new List<int>();
        int Mid(int a, int b) { long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; if (cache.TryGetValue(key, out int r)) return r; vv.Add(((vv[a] + vv[b]) / 2).normalized); cache[key] = vv.Count - 1; return vv.Count - 1; }
        for (int i = 0; i < tt.Count; i += 3)
        {
            int a = tt[i], b = tt[i + 1], c = tt[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
            outT.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
        }
        verts = vv; tris = outT;
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
