// Height variance for the cloud lounge (Claude, 2026-09-26). Menu: Astra > Claude > 5 Height variance.
// Lifts each furniture group / amenity / the bathtub onto its own floating cloud platform at a varied height,
// with stepping-stone clouds down to the floor. Outer big clouds get a wider height spread.
// Rollback: Astra > Claude > Restore scene from before height variance.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraClaudeHeights
{
    const string Backup = "Review/Backups/BeforeHeightVariance.unity.txt";
    static System.Random rng; static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    static Mesh cloudMesh; static Material[] mats;

    [MenuItem("Astra/Claude/5 Height variance")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge == null) { EditorUtility.DisplayDialog("Heights", "Cloud lounge not found.", "OK"); return; }
        if (lounge.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Lift pad")) { EditorUtility.DisplayDialog("Heights", "Already applied. Restore first to redo.", "OK"); return; }
        EditorSceneManager.SaveScene(scene); File.Copy(scene.path, Backup, true);
        rng = new System.Random(926);
        var sample = lounge.GetComponentsInChildren<MeshRenderer>(true).First(r => r.name.StartsWith("Cloud") && r.GetComponent<MeshFilter>().sharedMesh.name.Contains("Cloud"));
        cloudMesh = sample.GetComponent<MeshFilter>().sharedMesh;
        mats = new[] { "Lounge Pink", "Lounge Lilac", "Lounge Peach" }.Select(n => AssetDatabase.LoadAssetAtPath<Material>("Assets/Astra/Materials/Lounge/" + n + ".mat")).Where(m => m != null).ToArray();
        var baseMat = sample.sharedMaterial;
        var report = new List<string>();

        // groups to lift: furniture sets, bathtub, swing / hammock / firepit (lamp posts stay on the walkway)
        var groups = new List<Transform>();
        var fur = lounge.transform.Find("Cloud furniture"); if (fur != null) groups.AddRange(fur.Cast<Transform>());
        var tub = lounge.transform.Find("Bathtub cloud"); if (tub != null) groups.Add(tub);
        var am = lounge.transform.Find("Cloud amenities"); if (am != null) groups.AddRange(am.Cast<Transform>().Where(t => t.name != "Cloud lamp"));
        // shuffle heights across three bands so neighbours differ
        float[] bands = { 0f, 1.2f, 2.4f, 3.6f, 5f, 6.5f, 8f, 10f };
        int i = 0;
        foreach (var g in groups.OrderBy(x => rng.Next()))
        {
            float lift = bands[i++ % bands.Length] + R(0f, .9f);
            if (lift < .5f) { report.Add(g.name + ": stays on the floor"); continue; }
            var bounds = Bounds(g); g.position += Vector3.up * lift; Physics.SyncTransforms();
            float w = bounds.size.x + 1.4f, d = bounds.size.z + 1.4f, top = g.position.y;
            var padRoot = new GameObject("Lift pad").transform; padRoot.SetParent(g, true); padRoot.position = new Vector3(bounds.center.x, top, bounds.center.z); padRoot.rotation = Quaternion.identity;
            var m = mats.Length > 0 ? mats[rng.Next(mats.Length)] : baseMat;
            Puff(padRoot, "Platform cloud", new Vector3(0, -.5f, 0), new Vector3(w, 1.1f, d), baseMat);
            Puff(padRoot, "Platform fluff", new Vector3(w * .28f, -.75f, -d * .2f), new Vector3(w * .55f, .9f, d * .55f), m);
            Puff(padRoot, "Platform fluff", new Vector3(-w * .3f, -.8f, d * .22f), new Vector3(w * .5f, .8f, d * .5f), m);
            Solid(padRoot, new Vector3(0, -.08f, 0), new Vector3(w * .92f, .16f, d * .92f));
            // stepping stones spiralling down beside the platform
            var c = new Vector3(bounds.center.x, 0, bounds.center.z); var radial = c.normalized; var tangent = Vector3.Cross(Vector3.up, radial) * (rng.Next(2) == 0 ? 1 : -1);
            float reach = Mathf.Max(w, d) * .5f + .9f; int steps = Mathf.CeilToInt(lift / .5f);
            var stones = new GameObject("Stepping clouds").transform; stones.SetParent(g, true);
            for (int s = 1; s <= steps; s++)
            {
                float y = top - s * (lift / (steps + 1)); float ang = s * .42f;
                var dir = (Mathf.Cos(ang) * tangent + Mathf.Sin(ang) * -radial).normalized;
                var p = c + dir * (reach + s * .18f) + Vector3.up * y;
                var st = new GameObject("Step " + s).transform; st.SetParent(stones, true); st.position = p;
                Puff(st, "Step cloud", new Vector3(0, -.22f, 0), new Vector3(1.35f, .5f, 1.35f), s % 2 == 0 ? baseMat : m);
                Solid(st, new Vector3(0, -.06f, 0), new Vector3(1.15f, .12f, 1.15f));
            }
            report.Add(g.name + ": lifted " + lift.ToString("0.0") + " m, " + steps + " stepping clouds");
        }
        // outer big clouds: much wider height spread
        var outer = GameObject.Find("14 - Outer big clouds (Claude)");
        if (outer != null) foreach (Transform t in outer.transform) if (t.name.StartsWith("Outer cloud")) { var p = t.position; t.position = new Vector3(p.x, R(-3f, 14f), p.z); report.Add(t.name + " at " + t.position.y.ToString("0.0") + " m"); }
        // crystal vine clouds: spread them over a taller band too
        var vines = lounge.transform.Find("Crystal vine clouds");
        if (vines != null) foreach (Transform t in vines) { var p = t.position; t.position = new Vector3(p.x, R(7f, 18f), p.z); }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-heights-report.txt", string.Join("\n", report));
        Debug.Log("ASTRA_HEIGHTS_DONE " + report.Count);
    }

    [MenuItem("Astra/Claude/Restore scene from before height variance")]
    public static void Restore()
    {
        if (!File.Exists(Backup) || !EditorUtility.DisplayDialog("Restore", "Put the scene back to before the height pass?", "Restore", "Cancel")) return;
        var path = EditorSceneManager.GetActiveScene().path; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); EditorSceneManager.OpenScene(path);
    }

    static Bounds Bounds(Transform g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (rs.Length == 0) return new Bounds(g.position, Vector3.one * 2);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    static void Puff(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, R(0, 360), 0);
        var b = cloudMesh.bounds.size; go.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, size.z / b.z);
        go.AddComponent<MeshFilter>().sharedMesh = cloudMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }
    static void Solid(Transform parent, Vector3 c, Vector3 size) { var go = new GameObject("Cloud walk collider"); go.transform.SetParent(parent, false); go.transform.localPosition = c; go.AddComponent<BoxCollider>().size = size; }
}
