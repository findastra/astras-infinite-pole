// Claude (2026-10-01): wider stairs ONLY. Owner asked for "a little wider".
// Menu: Astra > Claude - Wider Stairs. Changes just three things and nothing else:
//   1. stair tread + walkable-ramp meshes: 2.8-5.1 m -> 2.8-5.8 m from the pole (treads 2.3 m -> 3.0 m wide)
//   2. invisible floor opening: 2.6-5.3 m -> 2.6-6.0 m so the wider stairs still pass through
//   3. star dust under the opening re-centred to match
// Works on the scene that's open (keeps your other edits), saves it, and checks walking height.
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeWiderStairs
{
    const string ScenePath = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const float Inner = 2.8f, Outer = 5.8f, HoleIn = 2.6f, HoleOut = 6.0f, FloorR = 48f;

    [MenuItem("Astra/Claude - Wider Stairs")]
    public static void Run()
    {
        Debug.Log("CLAUDE_WIDER_STAIRS start");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory("Review/Backups");
        File.Copy(ScenePath, "Review/Backups/BeforeClaudeWiderStairs.unity.txt", true);

        Rebuild(AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Materials/Spiral Steps.asset"), false);
        Rebuild(AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Materials/Spiral Walkable Ramp.asset"), true);

        var floorMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Meshes/Platform With Stair Opening.asset");
        if (floorMesh != null) BuildFloor(floorMesh);

        var dust = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ParticleSystem>(true))
                        .FirstOrDefault(p => p.name == "10 - Stair opening star dust");
        if (dust != null)
        {
            var shape = dust.shape; shape.radius = (Inner + Outer) / 2f; shape.donutRadius = 1.4f; shape.scale = new Vector3(1, 1, 2.3f);
            EditorUtility.SetDirty(dust);
        }

        // Re-assign meshes so colliders pick up the new shape
        foreach (var mc in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshCollider>(true)))
            if (mc.sharedMesh != null && (mc.sharedMesh.name.Contains("spiral") || mc.sharedMesh.name.Contains("platform")))
            { var m = mc.sharedMesh; mc.sharedMesh = null; mc.sharedMesh = m; }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Checks: you can stand on the new outer edge of a step, and the floor opening is open there
        Physics.SyncTransforms();
        var report = new List<string>();
        var spiral = Object.FindObjectOfType<AstraSpiral>();
        if (spiral != null) { spiral.Recenter(0); Physics.SyncTransforms(); }
        float r = 5.5f; // inside the new strip, outside the old one
        int pass = 0;
        for (int s = 0; s < 40; s++)
        {
            float a = -Mathf.PI / 2 + (s + .5f) * Mathf.PI * 2 / 40;
            var p = new Vector3(Mathf.Cos(a) * r, (s + .5f) * .16f + 1f, Mathf.Sin(a) * r);
            if (Physics.Raycast(p, Vector3.down, out var hit, 1.05f) && hit.collider.name.StartsWith("Spiral turn")) pass++;
        }
        report.Add($"Walkable at 5.5 m (new outer strip): {pass}/40 steps");
        bool open = true;
        if (Physics.Raycast(new Vector3(0, .5f, 5.7f), Vector3.down, out var h2, 1f) && h2.collider.name.StartsWith("Invisible platform")) open = false;
        report.Add("Floor opening reaches 6.0 m: " + (open ? "PASS" : "FAIL"));
        report.Add("Star dust re-centred: " + (dust != null ? "yes" : "not found (skipped)"));
        File.WriteAllText("Review/claude-wider-stairs-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_WIDER_STAIRS_OK " + string.Join(" | ", report));
    }

    static List<Vector3> v; static List<int> t; static List<Vector2> uv;
    static void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int k = v.Count; v.AddRange(new[] { a, b, c, d }); uv.AddRange(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }); t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 }); }
    static Vector3 P(float r, float a, float y) { return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
    static void Rebuild(Mesh m, bool ramp)
    {
        v = new List<Vector3>(); t = new List<int>(); uv = new List<Vector2>();
        for (int step = 0; step < 40; step++) for (int j = 0; j < 3; j++)
        {
            float a = -Mathf.PI / 2 + (step + j / 3f) * Mathf.PI * 2 / 40, b = -Mathf.PI / 2 + (step + (j + 1) / 3f) * Mathf.PI * 2 / 40;
            float y = (step + 1) * .16f, ya = ramp ? (step + j / 3f) * .16f : y, yb = ramp ? (step + (j + 1) / 3f) * .16f : y;
            var ia = P(Inner, a, ya); var ib = P(Inner, b, yb); var ob = P(Outer, b, yb); var oa = P(Outer, a, ya); Quad(ia, ib, ob, oa);
            if (!ramp) { var down = Vector3.down * .065f; Quad(oa + down, ob + down, ib + down, ia + down); Quad(ia + down, ib + down, ib, ia); Quad(oa, ob, ob + down, oa + down); if (j == 0) Quad(ia, oa, oa + down, ia + down); if (j == 2) Quad(ob, ib, ib + down, ob + down); }
        }
        string keepName = m.name;
        m.Clear(); m.SetVertices(v); m.SetTriangles(t, 0); m.SetUVs(0, uv); m.RecalculateNormals(); m.RecalculateBounds(); m.name = keepName;
        EditorUtility.SetDirty(m);
    }
    static void BuildFloor(Mesh m)
    {
        const int seg = 96;
        var verts = new List<Vector3> { Vector3.zero }; var tris = new List<int>();
        for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2 / seg; verts.Add(new Vector3(Mathf.Sin(a) * HoleIn, 0, Mathf.Cos(a) * HoleIn)); }
        for (int i = 0; i < seg; i++) { tris.Add(0); tris.Add(i + 1); tris.Add((i + 1) % seg + 1); }
        int ring = verts.Count;
        for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2 / seg; verts.Add(new Vector3(Mathf.Sin(a) * HoleOut, 0, Mathf.Cos(a) * HoleOut)); verts.Add(new Vector3(Mathf.Sin(a) * FloorR, 0, Mathf.Cos(a) * FloorR)); }
        for (int i = 0; i < seg; i++) { int ii = ring + i * 2, io = ii + 1, ni = ring + ((i + 1) % seg) * 2, no = ni + 1; tris.AddRange(new[] { ii, io, no, ii, no, ni }); }
        string keepName = m.name;
        m.Clear(); m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds(); m.name = keepName;
        EditorUtility.SetDirty(m);
    }
}
