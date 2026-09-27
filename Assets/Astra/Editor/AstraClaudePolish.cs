// Polish + banner backdrop (Claude, 2026-09-26).
// Astra > Claude > 12 Polish pass: smaller duck, deeper bath, booths nestled in bigger clouds, 4 small DJ lights on the cloud,
//   seamless vines, more small puffs, outer cloud track facing the pole, orbit clouds move together facing the pole,
//   sit-on swing with crystal-vine chains, breakfast cloud (Astra <3 Mc Waffles).
// Astra > Claude > 13 Banner backdrop: pink sunset sky + endless pink cloud sea, a banner-style booth sunk in the clouds,
//   and pink flapping butterflies (sprite-sheet flaps; the old 91-key flap curve was beyond Unity's 8-key particle limit).
// Rollback: Astra > Claude > Restore scene from before polish / before banner backdrop.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Components;

[InitializeOnLoad]
public static class AstraClaudePolish
{
    const string Root = "Assets/Astra/";
    const string BackupPolish = "Review/Backups/BeforePolish.unity.txt";
    const string BackupBanner = "Review/Backups/BeforeBannerBackdrop.unity.txt";
    static System.Random rng; static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    static Mesh cloudMesh, sphere, cylinder, quad; static Material white; static Transform lounge; static List<string> report;
    static readonly Dictionary<Mesh, Vector3[]> vertCache = new Dictionary<Mesh, Vector3[]>();

    // make sure new Udon programs exist (and get compiled) long before a menu item needs them
    static AstraClaudePolish() { EditorApplication.delayCall += () => { foreach (var n in new[] { "AstraSwing" }) EnsureProgram(n, false); }; }
    static void EnsureProgram(string name, bool compile)
    {
        string path = Root + "Scripts/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) == null)
        {
            var src = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "Scripts/" + name + ".cs"); if (src == null) return;
            var a = ScriptableObject.CreateInstance<UdonSharpProgramAsset>(); a.sourceCsScript = src;
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets(); compile = true;
        }
        if (compile) UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
    }

    static void Begin(string backup)
    {
        var path = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        File.Copy(path, backup, true);
        rng = new System.Random(926 + backup.Length); report = new List<string>();
        lounge = GameObject.Find("13 - Cloud lounge (Claude)").transform;
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>();
        var anyCloud = spiral.GetComponentsInChildren<MeshRenderer>(true).First(r => r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Glitter Cloud"));
        cloudMesh = anyCloud.GetComponent<MeshFilter>().sharedMesh; white = anyCloud.sharedMaterial;
        sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx"); cylinder = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx"); quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
    }
    static void End(string file)
    {
        var scene = EditorSceneManager.GetActiveScene(); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/" + file, string.Join("\n", report)); Debug.Log("ASTRA_" + file + "\n" + string.Join("\n", report));
    }
    static void Step(string name, Action a) { try { a(); report.Add("== " + name + ": ok"); } catch (Exception e) { report.Add("ERROR " + name + ": " + e.Message); Debug.LogException(e); } }

    // ------------------------------------------------------------------ helpers
    static Transform Puff(Transform parent, Vector3 localPos, float width, Material m, string name = "Puff")
    {
        float k = width / cloudMesh.bounds.size.x / Mathf.Max(.0001f, Mathf.Abs(parent.lossyScale.x));
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(0, R(0, 360), 0); go.transform.localScale = Vector3.one * k;
        go.transform.localPosition = localPos - go.transform.localRotation * (cloudMesh.bounds.center * k);
        go.AddComponent<MeshFilter>().sharedMesh = cloudMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; Quiet(r);
        return go.transform;
    }
    static GameObject Prim(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default(Vector3))
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(euler); go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; Quiet(r); return go;
    }
    static void Quiet(Renderer r) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off; }
    static Vector3[] Verts(Mesh m) { Vector3[] v; if (!vertCache.TryGetValue(m, out v)) { v = m.vertices; vertCache[m] = v; } return v; }
    // highest cloud surface point within `radius` (horizontally) of `at`
    static float Top(IEnumerable<Renderer> rs, Vector3 at, float radius, float fallback)
    {
        float top = float.MinValue;
        foreach (var r in rs)
        {
            if (r == null) continue; var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var b = r.bounds; if (Mathf.Abs(b.center.x - at.x) > b.extents.x + radius || Mathf.Abs(b.center.z - at.z) > b.extents.z + radius) continue;
            var tr = r.transform;
            foreach (var v in Verts(mf.sharedMesh)) { var w = tr.TransformPoint(v); if (new Vector2(w.x - at.x, w.z - at.z).sqrMagnitude < radius * radius && w.y > top) top = w.y; }
        }
        return top == float.MinValue ? fallback : top;
    }
    static Bounds BoundsOf(IEnumerable<Renderer> rs) { var a = rs.Where(r => r != null).ToArray(); if (a.Length == 0) return new Bounds(); var b = a[0].bounds; foreach (var r in a) b.Encapsulate(r.bounds); return b; }
    static Material GlitterMat(string name, Color top, Color bottom, Color rim, float glitter)
    {
        string path = Root + "Materials/Lounge/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Astra/Glitter Cloud")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Astra/Glitter Cloud"); m.SetColor("_Top", top); m.SetColor("_Bottom", bottom); m.SetColor("_Rim", rim); m.SetFloat("_Glitter", glitter); m.SetFloat("_Pastel", 0); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }
    static bool Uniform(Vector3 s) { float a = Mathf.Abs(s.x), b = Mathf.Abs(s.y), c = Mathf.Abs(s.z); return Mathf.Max(a, Mathf.Max(b, c)) / Mathf.Min(a, Mathf.Min(b, c)) < 1.02f; }
    // replace one stretched cloud with naturally proportioned (smaller) puffs; returns the new container
    static Transform Naturalize(MeshFilter mf, float smaller = .8f, int maxN = 40)
    {
        var t = mf.transform; var mb = cloudMesh.bounds; var ls = t.lossyScale;
        var B = new Vector3(Mathf.Abs(ls.x) * mb.size.x, Mathf.Abs(ls.y) * mb.size.y, Mathf.Abs(ls.z) * mb.size.z);
        var center = t.TransformPoint(mb.center); var rot = t.rotation; var mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
        float k = Mathf.Min(B.x / mb.size.x, Mathf.Min(B.y / mb.size.y, B.z / mb.size.z)) * smaller; int nx, ny, nz; Vector3 p;
        while (true) { p = mb.size * k; nx = N(B.x, p.x); ny = N(B.y, p.y); nz = N(B.z, p.z); if (nx * ny * nz <= maxN) break; k *= 1.05f; }
        Transform anc = t.parent; while (anc != null && !Uniform(anc.lossyScale)) anc = anc.parent;
        var shape = new GameObject("Cloud shape").transform; shape.SetParent(anc, true);
        shape.position = center + rot * new Vector3(0, -Mathf.Max(0, p.y - B.y) * .5f, 0); shape.rotation = rot;
        shape.localScale = anc != null ? Vector3.one / Mathf.Abs(anc.lossyScale.x) : Vector3.one;
        for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++) for (int q = 0; q < nz; q++)
                {
                    float fx = nx == 1 ? 0 : i / (float)(nx - 1) - .5f, fy = ny == 1 ? 0 : j / (float)(ny - 1) - .5f, fz = nz == 1 ? 0 : q / (float)(nz - 1) - .5f;
                    var off = new Vector3(fx * Mathf.Max(0, B.x - p.x), fy * Mathf.Max(0, B.y - p.y), fz * Mathf.Max(0, B.z - p.z)) + new Vector3(R(-.12f, .12f) * p.x, R(-.06f, .1f) * p.y, R(-.12f, .12f) * p.z);
                    float s = k * R(.85f, 1.15f);
                    var go = new GameObject("Puff"); go.transform.SetParent(shape, false);
                    go.transform.localRotation = Quaternion.Euler(0, (rng.Next(2) == 0 ? 0 : 180) + R(-20, 20), 0); go.transform.localScale = Vector3.one * s;
                    go.transform.localPosition = off - go.transform.localRotation * (mb.center * s);
                    go.AddComponent<MeshFilter>().sharedMesh = cloudMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; Quiet(r);
                }
        UnityEngine.Object.DestroyImmediate(mf.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(mf);
        return shape;
    }
    static int N(float size, float puff) { return size <= puff ? 1 : Mathf.CeilToInt((size - puff) / (puff * .6f)) + 1; }
    static IEnumerable<Renderer> CloudRenderers(Transform t) { return t.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh == cloudMesh).Cast<Renderer>(); }
    static void Sparkles(Transform parent, Bounds b, string name)
    {
        var star = GameObject.Find("10 - Stair opening star dust")?.GetComponent<ParticleSystemRenderer>()?.sharedMaterial;
        var sg = new GameObject(name); sg.transform.SetParent(parent, true); sg.transform.position = b.center; sg.transform.rotation = Quaternion.identity;
        var ps = sg.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.prewarm = true; main.maxParticles = 60; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2.6f); main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.04f, .12f); main.startColor = Color.white;
        var em = ps.emission; em.rateOverTime = Mathf.Clamp(b.size.x * b.size.y * b.size.z * .6f, 8f, 26f);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = b.size * .8f;
        var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(1, .7f), new GradientAlphaKey(0, 1) }); col.color = gr;
        var pr = ps.GetComponent<ParticleSystemRenderer>(); if (star != null) pr.sharedMaterial = star; Quiet(pr);
    }
    static void JumpPlatform(Transform parent, Bounds b)
    {
        if (b.min.y - .55f < .6f) return;
        var pl = new GameObject("Jump platform"); pl.transform.SetParent(parent, true); pl.transform.position = new Vector3(b.center.x, b.min.y - .55f, b.center.z); pl.transform.rotation = Quaternion.identity;
        var ls = pl.transform.lossyScale; pl.AddComponent<BoxCollider>().size = new Vector3((b.size.x + 3f) / ls.x, .3f / ls.y, (b.size.z + 3f) / ls.z);
    }
    static float FaceCenter(Vector3 p) { return Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg; }

    // ---- free-space search: every visible thing in the scene (not particles, not the sky-sized backdrop) plus the video screen as an oriented box
    static List<Bounds> taken; static Transform screenT; static Vector3 screenC, screenHalf;
    static void Survey()
    {
        taken = new List<Bounds>();
        foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || !r.gameObject.scene.IsValid() || !r.enabled || !r.gameObject.activeInHierarchy) continue; var b = r.bounds;
            if (b.size.x > 90 || b.size.z > 90 || r.name == "VideoScreen") continue; taken.Add(b);
        }
        var vs = UnityEngine.Object.FindObjectsOfType<Renderer>(true).FirstOrDefault(r => r.name == "VideoScreen"); screenT = null;
        if (vs != null)
        {
            var mf = vs.GetComponent<MeshFilter>(); var mb = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
            screenT = vs.transform; screenC = vs.transform.TransformPoint(mb.center);
            screenHalf = Vector3.Scale(mb.extents, new Vector3(Mathf.Abs(vs.transform.lossyScale.x), Mathf.Abs(vs.transform.lossyScale.y), Mathf.Abs(vs.transform.lossyScale.z)));
        }
    }
    static bool NearScreen(Vector3 p, float margin)
    {
        if (screenT == null) return false; var d = Quaternion.Inverse(screenT.rotation) * (p - screenC);
        return Mathf.Abs(d.x) < screenHalf.x + margin && Mathf.Abs(d.y) < screenHalf.y + margin && Mathf.Abs(d.z) < screenHalf.z + margin + 3f;
    }
    static bool Free(Vector3 center, Vector3 size, float margin, float ignoreAbove = 90f)
    {
        var test = new Bounds(center, size + Vector3.one * margin * 2);
        if (taken.Any(b => b.size.x < ignoreAbove && b.size.z < ignoreAbove && b.Intersects(test))) return false;
        for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) if (NearScreen(center + new Vector3(i * size.x * .5f, 0, j * size.z * .5f), margin + size.y * .5f)) return false;
        return true;
    }
    static void Claim(IEnumerable<Renderer> rs) { foreach (var r in rs) if (r != null && !(r is ParticleSystemRenderer)) taken.Add(r.bounds); }
    static float Overlap(Bounds a, Bounds b)
    {
        float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x), y = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y), z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
        return x > 0 && y > 0 && z > 0 ? x * y * z : 0f;
    }
    // the emptiest spot among many random candidates (overlap volume with everything visible, plus a small preference term)
    static Vector3 BestSpot(Func<Vector3> sample, Vector3 size, float margin, Func<Vector3, float> preference, int tries, out float overlap)
    {
        Vector3 best = Vector3.zero; float bestScore = float.MaxValue; overlap = 0;
        for (int i = 0; i < tries; i++)
        {
            var p = sample(); var test = new Bounds(p, size + Vector3.one * margin * 2); float ov = 0;
            foreach (var b in taken) if (b.size.x < 60 && b.size.z < 60) ov += Overlap(b, test);
            if (NearScreen(p, margin + size.y * .5f)) ov += 1000f;
            float score = ov * 100f + preference(p); if (score < bestScore) { bestScore = score; best = p; overlap = ov; }
        }
        return best;
    }
    // exact cloud surface under a point: temporary mesh colliders on the puffs, ray straight down
    static readonly List<MeshCollider> temp = new List<MeshCollider>();
    static void TempColliders(IEnumerable<Renderer> rs)
    {
        foreach (var r in rs) { var mf = r != null ? r.GetComponent<MeshFilter>() : null; if (mf == null || mf.sharedMesh == null) continue; var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); }
        Physics.SyncTransforms();
    }
    static void ClearTemp() { foreach (var c in temp) if (c != null) UnityEngine.Object.DestroyImmediate(c); temp.Clear(); }
    static float Surface(Vector3 at, float fromY)
    {
        var ray = new Ray(new Vector3(at.x, fromY, at.z), Vector3.down); float best = float.NaN; RaycastHit hit;
        foreach (var c in temp) if (c != null && c.Raycast(ray, out hit, 400f) && (float.IsNaN(best) || hit.point.y > best)) best = hit.point.y;
        return best;
    }

    // ================================================================== 12. polish
    [MenuItem("Astra/Claude/12 Polish pass")]
    public static void Polish()
    {
        EnsureProgram("AstraSwing", false);
        Begin(BackupPolish);
        Step("duck + deeper bath", Bath);
        Step("phone booths nestled", Booths);
        Step("DJ lights", DJ);
        Step("seamless vines", Vines);
        Step("outer cloud track", OuterTrack);
        Step("orbit clouds together, facing the pole", Orbits);
        Step("sit-on swing", Swing);
        Step("breakfast cloud", Breakfast);
        Step("more small puffs", SmallPuffs);
        End("claude-polish-report.txt");
    }

    static void Bath()
    {
        var dish = lounge.GetComponentsInChildren<Transform>(true).First(t => t.name == "Bath dish");
        var water = dish.Find("Bath water"); float rx = water.localScale.x / 2f / .98f, rz = water.localScale.z / 2f / .98f;
        foreach (Transform c in dish.Cast<Transform>().ToList())
        {
            var pr = c.GetComponent<MeshRenderer>(); bool wall = c.name == "Puff" && pr != null && dish.InverseTransformPoint(pr.bounds.center).y > .3f;
            if (wall || c.name == "Bath water depth" || c.name == "Bath steps") UnityEngine.Object.DestroyImmediate(c.gameObject);
        }
        // walls: a deep cloud bowl 4.2 m tall (the old dish was ~1.1 m): big soft puffs low down, smaller ones toward the rim,
        // flaring out as it rises, with jitter so it reads as one fluffy cloud rather than stacked rings
        float depth = 4.2f, wy = depth - .3f; int puffs = 0;
        for (int k = 0; k < 6; k++)
        {
            float t = k / 5f, y = .45f + t * (depth - .55f), f = .84f + .2f * Mathf.Sqrt(t), w = Mathf.Lerp(2.5f, 1.4f, t);
            int n = Mathf.CeilToInt(Mathf.PI * (rx + rz) * f / (w * .62f));
            for (int i = 0; i < n; i++)
            {
                float a = (i + (k % 2) * .5f + R(-.2f, .2f)) * Mathf.PI * 2 / n, j = R(-.35f, .35f);
                Puff(dish, new Vector3(Mathf.Cos(a) * (rx * f + j), y + R(-.12f, .15f), Mathf.Sin(a) * (rz * f + j)), w * R(.75f, 1.3f), white); puffs++;
            }
        }
        // a few big billows on the outside lower half so the silhouette is a cloud, not a tub
        { int n = Mathf.CeilToInt(Mathf.PI * (rx + rz) / 3.4f); for (int i = 0; i < n; i++) { float a = (i + R(-.3f, .3f)) * Mathf.PI * 2 / n; Puff(dish, new Vector3(Mathf.Cos(a) * (rx * .98f + 1.1f), R(.6f, 1.9f), Mathf.Sin(a) * (rz * .98f + 1.1f)), R(2.6f, 3.6f), white); puffs++; } }
        { int n = Mathf.CeilToInt(Mathf.PI * (rx + rz) * 1.05f / .8f); for (int i = 0; i < n; i++) { float a = (i + R(-.25f, .25f)) * Mathf.PI * 2 / n; Puff(dish, new Vector3(Mathf.Cos(a) * rx * 1.05f, depth + R(.1f, .3f), Mathf.Sin(a) * rz * 1.05f), R(.9f, 1.5f), white); puffs++; } }
        // water fills the dish right up under the rim; the column shows the depth through the surface
        water.localPosition = new Vector3(0, wy, 0); water.localScale = new Vector3(rx * 2f * 1.02f, .015f, rz * 2f * 1.02f);
        Prim(dish, "Bath water depth", cylinder, new Vector3(0, (.4f + wy) * .5f, 0), new Vector3(rx * 2f * .8f, (wy - .4f) * .5f, rz * 2f * .8f), water.GetComponent<MeshRenderer>().sharedMaterial);
        // you stand chest-deep instead of 4 m under water
        var floor = dish.Find("Bath floor collider"); if (floor != null) floor.localPosition = new Vector3(0, wy - 1.15f, 0);
        // cloud steps up the outside wall on the pole side, so you can climb in
        var steps = new GameObject("Bath steps").transform; steps.SetParent(dish, false);
        var toPole = dish.InverseTransformDirection(-new Vector3(dish.position.x, 0, dish.position.z).normalized); float a0 = Mathf.Atan2(toPole.z / rz, toPole.x / rx);
        for (int s = 0; s < 9; s++)
        {
            float a = a0 + (s - 4f) * .09f, sy = .55f + s * .45f, off = Mathf.Lerp(3.4f, 1.5f, s / 8f); var at = new Vector3(Mathf.Cos(a) * (rx * 1.02f + off), sy, Mathf.Sin(a) * (rz * 1.02f + off));
            var st = Puff(steps, at, 1.7f, white, "Bath step"); var sc = new GameObject("Step collider"); sc.transform.SetParent(steps, false); sc.transform.localPosition = at + Vector3.up * .25f;
            sc.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0); sc.AddComponent<BoxCollider>().size = new Vector3(1.3f, .2f, 1.3f); puffs++;
        }
        var duck = dish.Find("Rubber ducky");
        if (duck != null)
        {
            float ds = Mathf.Abs(duck.lossyScale.x) / 4f; duck.localScale = Vector3.one * ds / Mathf.Abs(dish.lossyScale.x);
            duck.localPosition = new Vector3(0, wy - .09f * ds, 0);
            var f = duck.GetComponent<AstraDuckFloat>(); if (f != null) { f.bob = .05f * ds; UdonSharpEditorUtility.CopyProxyToUdon(f); }
        }
        var bub = dish.Find("Bath bubbles"); if (bub != null) bub.localPosition = new Vector3(0, wy, 0);
        report.Add("bath now " + depth + " m deep (" + puffs + " small puffs incl. 9 cloud steps up the outside), water filled to " + wy.ToString("0.0") + " m, you stand chest-deep; ducky 4x smaller");
    }

    static void Booths()
    {
        var booths = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid() && (t.name == "Phone booth 1" || t.name == "Phone booth 2") && t.Find("Booth body") != null).ToList();
        foreach (var b in booths)
        {
            var cloud = b.parent; var rot = b.rotation; var cr = cloud.GetComponent<MeshRenderer>();
            if (cr == null) { report.Add(b.name + ": SKIPPED, its cloud has no renderer"); continue; }
            var bl = b.lossyScale; float bw = (Mathf.Abs(bl.x) + Mathf.Abs(bl.y) + Mathf.Abs(bl.z)) / 3f;   // the booth's real size stays the same
            float m = Mathf.Max(Mathf.Abs(cloud.localScale.x), Mathf.Max(Mathf.Abs(cloud.localScale.y), Mathf.Abs(cloud.localScale.z)));
            cloud.localScale = Vector3.one * m * 1.15f;                      // a little bigger, and no longer stretched
            b.localScale = Vector3.one * bw / Mathf.Abs(cloud.lossyScale.x); b.rotation = rot;
            Physics.SyncTransforms();
            var at = new Vector3(cloud.position.x, 0, cloud.position.z);
            float top = Top(new Renderer[] { cr }, at, .5f, cr.bounds.max.y);
            b.position = new Vector3(at.x, top - .03f, at.z);
            foreach (var n in new[] { "Cloud booth step", "Cloud booth step collider", "Booth cloud skirt" }) { var o = b.Find(n); if (o != null) UnityEngine.Object.DestroyImmediate(o.gameObject); }
            // two rings of small puffs hugging the outside of the booth so its base sits IN the cloud (like the banner), while the floor inside stays clean
            var body = b.Find("Booth body"); var hb = body != null ? BoundsOf(body.GetComponentsInChildren<Renderer>()) : new Bounds(b.position, Vector3.one * 1.2f);
            float half = Mathf.Min(hb.extents.x, hb.extents.z) / bw;                                      // booth half width in booth units
            var skirt = new GameObject("Booth cloud skirt").transform; skirt.SetParent(b, false); int sp = 0;
            foreach (var ring in new[] { new Vector3(.34f, .03f, .75f), new Vector3(.75f, .12f, 1.05f) })   // (gap from wall, height, puff width)
            {
                float rr = half + ring.x + ring.z * .25f; int n = Mathf.CeilToInt(2 * Mathf.PI * rr / (ring.z * .5f));
                for (int i = 0; i < n; i++) { float a = (i + R(-.2f, .2f)) * Mathf.PI * 2 / n; Puff(skirt, new Vector3(Mathf.Cos(a) * rr, ring.y + R(-.05f, .08f), Mathf.Sin(a) * rr), ring.z * R(.85f, 1.15f) * bw, cr.sharedMaterial); sp++; }
            }
            report.Add(b.name + ": cloud 15% bigger and even, booth floor on the cloud top, " + sp + " skirt puffs sink its base into the cloud");
        }
    }

    static void DJ()
    {
        var dj = GameObject.Find("12 - DJ cloud").transform; var shapes = new List<Transform>();
        foreach (var n in new[] { "DJ stage cloud", "DJ step cloud" }) { var t = dj.Find(n); var mf = t != null ? t.GetComponent<MeshFilter>() : null; if (mf != null && mf.GetComponent<MeshRenderer>() != null && !Uniform(t.lossyScale)) shapes.Add(Naturalize(mf, .7f, 70)); }
        var stageRs = dj.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh == cloudMesh && !r.transform.IsChildOf(dj.Find("Stage lights"))).Cast<Renderer>().ToList();
        var stageShape = shapes.Count > 0 ? shapes[0] : null;
        var sb = BoundsOf(stageShape != null ? stageShape.GetComponentsInChildren<Renderer>() : stageRs.ToArray());
        var lights = dj.Find("Stage lights"); var sl = lights.GetComponent<AstraStageLights>();
        var towers = lights.Cast<Transform>().Where(t => t.name.StartsWith("Light tower")).OrderBy(t => t.name.Length).ThenBy(t => t.name).ToList();
        TempColliders(stageRs);
        var keep = towers.Where((t, i) => i % 2 == 0).ToList();
        var heads = new List<Transform>(); var yaws = new List<float>();
        float rad = Mathf.Min(sb.extents.x, sb.extents.z) * .72f;
        for (int i = 0; i < keep.Count; i++)
        {
            var tw = keep[i]; var head = tw.Find("Moving head");
            int idx = Array.IndexOf(sl.heads, head); heads.Add(head); yaws.Add(idx >= 0 && idx < sl.yawBase.Length ? sl.yawBase[idx] : i * 90f);
            // walk inward from the rim until the ray lands on the cloud top (not a gap or the cloud's side)
            float a = (45 + 90 * i) * Mathf.Deg2Rad; Vector3 at = sb.center; float top = float.NaN;
            for (float f = 1f; f >= .35f; f -= .05f)
            {
                at = sb.center + new Vector3(Mathf.Cos(a) * rad * f, 0, Mathf.Sin(a) * rad * f);
                float c0 = Surface(at, sb.max.y + 5); if (float.IsNaN(c0) || c0 < sb.max.y - .9f) continue;
                bool flat = true; foreach (var d in new[] { new Vector3(.25f, 0, 0), new Vector3(-.25f, 0, 0), new Vector3(0, 0, .25f), new Vector3(0, 0, -.25f) }) { float cd = Surface(at + d, sb.max.y + 5); if (float.IsNaN(cd) || Mathf.Abs(cd - c0) > .3f) flat = false; }
                if (flat) { top = c0; break; }
            }
            if (float.IsNaN(top)) { at = sb.center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rad * .4f; top = Surface(at, sb.max.y + 5); if (float.IsNaN(top)) top = sb.max.y - .3f; }
            tw.localScale = tw.localScale * .5f;
            if (head != null) head.localPosition = new Vector3(head.localPosition.x, .25f, head.localPosition.z);   // lamp sits right on its base
            tw.position = new Vector3(at.x, top - .04f, at.z);                                                     // base pressed into the cloud top
        }
        ClearTemp();
        foreach (var t in towers.Except(keep)) UnityEngine.Object.DestroyImmediate(t.gameObject);
        sl.heads = heads.ToArray(); sl.yawBase = yaws.ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(sl);
        report.Add("DJ: " + keep.Count + " lights at half size standing in the cloud top (was " + towers.Count + "); DJ clouds rebuilt from even puffs");
    }

    static void Vines()
    {
        int moved = 0;
        foreach (var root in lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Chandelier strands").ToList())
        {
            var puffs = CloudRenderers(root.parent).ToList(); if (puffs.Count == 0) continue;
            foreach (Transform link in root) if (link.name == "Link 0")
                {
                    var w = link.position; Renderer host = null; float best = float.MaxValue;
                    foreach (var p in puffs) { var b = p.bounds; float d = new Vector2(b.center.x - w.x, b.center.z - w.z).magnitude / Mathf.Max(.1f, Mathf.Min(b.extents.x, b.extents.z)); if (d < best) { best = d; host = p; } }
                    var hb = host.bounds; var off = new Vector2(w.x - hb.center.x, w.z - hb.center.z); float lim = Mathf.Min(hb.extents.x, hb.extents.z) * .45f; if (off.magnitude > lim) off = off.normalized * lim;
                    link.position = new Vector3(hb.center.x + off.x, hb.center.y - hb.extents.y * .1f, hb.center.z + off.y); moved++;   // the top of each strand starts inside a puff
                }
        }
        report.Add("chandelier strands anchored inside the cloud (seamless): " + moved);
    }

    static void SmallPuffs()
    {
        int added = 0;
        var shapes = lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Cloud shape").ToList();
        var djShapes = GameObject.Find("12 - DJ cloud").GetComponentsInChildren<Transform>(true).Where(t => t.name == "Cloud shape");
        foreach (var shape in shapes.Concat(djShapes))
            foreach (var p in shape.Cast<Transform>().Where(c => c.name == "Puff" && c.GetComponent<MeshRenderer>() != null).ToList())
            {
                if (rng.NextDouble() > .55) continue;
                var r = p.GetComponent<MeshRenderer>(); var b = r.bounds; float a = R(0, Mathf.PI * 2);
                var w = b.center + new Vector3(Mathf.Cos(a) * b.extents.x * .7f, b.extents.y * R(.2f, .5f), Mathf.Sin(a) * b.extents.z * .7f);
                var go = Puff(shape, shape.InverseTransformPoint(w), b.size.x * R(.3f, .48f), r.sharedMaterial); added++;
            }
        report.Add("small detail puffs added: " + added);
    }

    static void OuterTrack()
    {
        var outer = GameObject.Find("14 - Outer big clouds (Claude)"); if (outer == null) { outer = new GameObject("14 - Outer big clouds (Claude)"); }
        foreach (Transform c in outer.transform.Cast<Transform>().ToList()) if (c.name.StartsWith("Outer cloud")) UnityEngine.Object.DestroyImmediate(c.gameObject);
        Survey();
        var avoid = new List<Bounds>();
        foreach (var n in new[] { "Cloud furniture", "Cloud amenities", "Crystal vine clouds" }) { var t = lounge.Find(n); if (t != null) foreach (Transform g in t) { var gb = BoundsOf(g.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer))); if (gb.size != Vector3.zero) { gb.Expand(2); avoid.Add(gb); } } }
        var bath = lounge.Find("Bathtub cloud"); if (bath != null) { var bb = BoundsOf(bath.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer))); bb.Expand(2); avoid.Add(bb); }
        var djc = GameObject.Find("12 - DJ cloud"); if (djc != null) { var db = BoundsOf(djc.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer) && r.bounds.size.x < 30)); db.Expand(3); avoid.Add(db); }
        int segs = 26, made = 0, skipped = 0, swerved = 0; float Rc = 40.5f;
        var switchHolder = outer.transform.Find("Outer clouds switch");
        for (int i = 0; i < segs; i++)
        {
            // one continuous, gently rolling ring: neighbours differ by less than a jump
            float ang = i * Mathf.PI * 2 / segs; float rr = Rc + R(-.6f, .6f);
            float baseY = 1.6f + 1.1f * Mathf.Sin(ang * 2 + .7f) + .5f * Mathf.Sin(ang * 5 + 2.1f);
            float L = 2 * Mathf.PI * Rc / segs * 1.1f, D = R(6.5f, 8f);
            // where a lounge cloud or the screen is in the way, the track swerves out / in / over it instead of breaking
            Vector3 pos = Vector3.zero; Quaternion rot = Quaternion.identity; bool blocked = true; int alt = 0;
            foreach (var o in new[] { new Vector2(0, 0), new Vector2(3.5f, 0), new Vector2(-3.5f, 0), new Vector2(6.5f, 0), new Vector2(0, 4f), new Vector2(3.5f, 4f), new Vector2(-3.5f, 4f), new Vector2(0, 7.5f) })
            {
                pos = new Vector3(Mathf.Cos(ang) * (rr + o.x), baseY + o.y, Mathf.Sin(ang) * (rr + o.x)); rot = Quaternion.Euler(0, FaceCenter(pos), 0);
                var test = new Bounds(pos, new Vector3(L * .9f, 3.2f, L * .9f));
                blocked = avoid.Any(a => a.Intersects(test));
                for (int k = -2; k <= 2 && !blocked; k++) blocked = NearScreen(pos + rot * new Vector3(k * L / 4f, 0, 0), D * .5f + 1f);
                if (!blocked) break; alt++;
            }
            if (blocked) { skipped++; continue; }
            if (alt > 0) swerved++;
            var g = new GameObject("Outer cloud " + (i + 1)).transform; g.SetParent(outer.transform, false); g.position = pos; g.rotation = rot;   // long side runs around the ring, front faces the pole
            for (float x = -L / 2 + 1f; x <= L / 2 - .6f; x += R(1.1f, 1.5f))
            {
                float bulge = 1 - Mathf.Pow(Mathf.Abs(x) / (L / 2), 2) * .3f;
                Puff(g, new Vector3(x, R(-.25f, .15f), R(-D * .22f, D * .22f)), R(2.2f, 2.8f) * bulge, white);
                Puff(g, new Vector3(x + R(-.5f, .5f), R(-.35f, 0f), (rng.Next(2) == 0 ? -1 : 1) * R(D * .25f, D * .34f)), R(1.6f, 2.1f), white);
                if (rng.NextDouble() < .6) Puff(g, new Vector3(x + R(-.5f, .5f), R(.45f, .85f), R(-D * .15f, D * .15f)), R(1.1f, 1.6f), white);
                if (rng.NextDouble() < .45) Puff(g, new Vector3(x + R(-.6f, .6f), R(-.8f, -.45f), R(-D * .28f, D * .28f)), R(1.4f, 2f), white);
            }
            var col = g.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, .42f, 0); col.size = new Vector3(L * .98f, .3f, D * .7f);
            made++;
        }
        if (switchHolder != null) { var os = switchHolder.GetComponent<AstraObjectSwitch>(); if (os != null) { os.targets = outer.transform.Cast<Transform>().Where(t => t != switchHolder).Select(t => t.gameObject).ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(os); } }
        report.Add("outer track: " + made + " walkable cloud segments in a ring at ~" + Rc + " m, each facing the pole (" + swerved + " swerve around lounge clouds, " + skipped + " gaps where nothing fits)");
    }

    static void Orbits()
    {
        var orbit = UnityEngine.Object.FindObjectOfType<AstraCloudOrbit>(); if (orbit == null) return;
        float common = orbit.speeds.Where(s => s != 0).Select(s => Mathf.Abs(s)).DefaultIfEmpty(2f).Average();
        for (int i = 0; i < orbit.speeds.Length; i++) orbit.speeds[i] = common;
        for (int j = 0; j < orbit.clouds.Length; j++)
        {
            var c = orbit.clouds[j]; if (c == null) continue;
            var lp = c.localPosition; float yaw = Mathf.Atan2(-lp.x, -lp.z) * Mathf.Rad2Deg;
            orbit.cloudYaw[j] = yaw; orbit.cloudSpin[j] = 0; c.localRotation = Quaternion.Euler(0, yaw, 0);
        }
        UdonSharpEditorUtility.CopyProxyToUdon(orbit);
        report.Add("orbit clouds: all " + orbit.speeds.Length + " rings turn together at " + common.ToString("0.0") + " deg/s, no self-spin, every cloud faces the pole");
    }

    static void Swing()
    {
        var sw = lounge.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Cloud swing"); if (sw == null) { report.Add("swing FAIL: not found"); return; }
        var holder = sw.Find("Furniture 2x") ?? sw;
        var anchor = holder.GetComponentsInChildren<Transform>(true).First(t => t.name == "Cloud swing anchor");
        var seatT = holder.GetComponentsInChildren<Transform>(true).First(t => t.name == "Cloud swing seat");
        var shapes = holder.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Cloud shape").ToList();
        if (shapes.Count < 2) { report.Add("swing FAIL: expected the anchor and seat clouds as puff shapes, found " + shapes.Count); return; }
        Func<Vector3, Transform> nearest = p => shapes.OrderBy(s => Vector3.Distance(BoundsOf(s.GetComponentsInChildren<Renderer>()).center, p)).First();
        var anchorShape = nearest(anchor.position); var seatShape = nearest(seatT.position);
        var ab = BoundsOf(anchorShape.GetComponentsInChildren<Renderer>()); var sbd = BoundsOf(seatShape.GetComponentsInChildren<Renderer>());
        var seatCol = holder.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name == "Cloud seat collider").OrderBy(c => Vector3.Distance(c.transform.position, seatT.position)).First();
        var pivot = new GameObject("Swing pivot").transform; pivot.SetParent(sw, false); pivot.position = new Vector3(sbd.center.x, ab.min.y + .3f, sbd.center.z); pivot.rotation = holder.rotation;
        seatShape.SetParent(pivot, true); seatCol.transform.SetParent(pivot, true);
        // crystal-vine chains instead of ropes (no jewel at the end)
        foreach (var rope in holder.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Swing rope").ToList()) UnityEngine.Object.DestroyImmediate(rope.gameObject);
        var links = new Mesh[3]; var crystal = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Lounge/Crystal Lilac.mat");
        float[] lens = { 1f, 1.5f, 2f }; for (int i = 0; i < 3; i++) links[i] = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Meshes/Lounge/Chandelier link " + i + "-" + (i % 4) + "-0.asset");
        float chainLen = pivot.position.y - sbd.max.y + .15f; float half = .55f * Mathf.Abs(holder.lossyScale.x);   // where the old ropes hung
        foreach (var side in new[] { -1f, 1f })
        {
            Transform parent = pivot; var at = pivot.InverseTransformPoint(pivot.position + pivot.right * side * half + Vector3.down * .05f); float left = chainLen;
            while (left > .3f)
            {
                int li = left >= 2f ? 2 : left >= 1.5f ? 1 : 0; float scaleY = Mathf.Min(1f, left / lens[li]);
                var go = new GameObject("Swing chain link"); go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = new Vector3(1, scaleY, 1) / Mathf.Abs(parent.lossyScale.x);
                go.AddComponent<MeshFilter>().sharedMesh = links[li]; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = crystal; Quiet(r);
                left -= lens[li] * scaleY; parent = pivot; at = pivot.InverseTransformPoint(go.transform.position + Vector3.down * lens[li] * scaleY);
            }
        }
        // sit on it
        var st = seatCol.gameObject.GetComponent<VRCStation>(); if (st == null) st = seatCol.gameObject.AddComponent<VRCStation>();
        var sitPoint = new GameObject("Sit point").transform; sitPoint.SetParent(seatCol.transform, false); sitPoint.position = new Vector3(sbd.center.x, sbd.max.y - .05f, sbd.center.z); sitPoint.rotation = holder.rotation;
        st.stationEnterPlayerLocation = sitPoint; st.stationExitPlayerLocation = sitPoint; st.PlayerMobility = VRC.SDKBase.VRCStation.Mobility.Immobilize; st.seated = true; st.disableStationExit = false;
        var swing = seatCol.gameObject.AddUdonSharpComponent<AstraSwing>(); swing.pivot = pivot; UdonSharpEditorUtility.CopyProxyToUdon(swing);
        report.Add("swing: sit by clicking/grabbing the seat; crystal-vine chains; sways, and swings higher with a rider");
    }

    // ---------------- breakfast cloud: small real-size glittery waffles + orange juice for two
    static void Breakfast()
    {
        var fur = lounge.Find("Cloud furniture"); Survey(); float ov;
        // the emptiest spot outside every orbit path (<= ~22 m, at every height because the rings ride the endless stairs)
        var pos = BestSpot(() => { float a = R(0, Mathf.PI * 2), r = R(26f, 46f); return new Vector3(Mathf.Cos(a) * r, R(3f, 9f), Mathf.Sin(a) * r); },
            new Vector3(6f, 3.5f, 6f), 1.5f, p => Mathf.Abs(new Vector2(p.x, p.z).magnitude - 33f) * .02f, 2500, out ov);
        report.Add("breakfast spot overlap with other things: " + ov.ToString("0.0") + " m3");
        var g = new GameObject("Breakfast cloud").transform; g.SetParent(fur, false); g.position = pos; g.rotation = Quaternion.Euler(0, FaceCenter(pos) + 90, 0);
        // the cloud: small, even puffs with a flat walkable top at local y = 0
        for (float x = -2.4f; x <= 2.4f; x += 1.1f) for (float z = -2f; z <= 2f; z += 1.1f)
            {
                if ((x * x) / 7f + (z * z) / 5f > 1.05f) continue;
                Puff(g, new Vector3(x + R(-.2f, .2f), -.45f + R(-.1f, .05f), z + R(-.2f, .2f)), R(1.3f, 1.8f), white);
                if (rng.NextDouble() < .5) Puff(g, new Vector3(x + R(-.3f, .3f), -.9f, z + R(-.3f, .3f)), R(1.0f, 1.5f), white);
            }
        var walk = new GameObject("Cloud walk collider"); walk.transform.SetParent(g, false); walk.transform.localPosition = new Vector3(0, -.12f, 0); walk.AddComponent<BoxCollider>().size = new Vector3(5.2f, .24f, 4.4f);
        // table: cloud stem + white top
        var porcelain = GlitterMat("Breakfast Porcelain", new Color(1.1f, 1.08f, 1.1f), new Color(.9f, .88f, .92f), new Color(1.2f, 1.2f, 1.25f), .6f);
        var table = new GameObject("Breakfast table").transform; table.SetParent(g, false);
        Puff(table, new Vector3(0, .2f, 0), .55f, white); Puff(table, new Vector3(0, .45f, 0), .45f, white);
        for (int i = 0; i < 12; i++) { float a = i * Mathf.PI * 2 / 12; Puff(table, new Vector3(Mathf.Cos(a) * .52f, .66f, Mathf.Sin(a) * .52f), .34f, white); }
        Prim(table, "Tabletop", cylinder, new Vector3(0, .735f, 0), new Vector3(1.05f, .015f, 1.05f), porcelain);
        var tc = new GameObject("Table collider"); tc.transform.SetParent(table, false); tc.transform.localPosition = new Vector3(0, .6f, 0); tc.AddComponent<BoxCollider>().size = new Vector3(1.1f, .3f, 1.1f);
        // two place settings: his (blue) on -z, hers (pink) on +z, each built facing the table
        var waffleMat = GlitterMat("Glitter Waffle", new Color(1.35f, .86f, .36f), new Color(.85f, .48f, .14f), new Color(1.5f, 1.1f, .55f), 3f);
        var butter = GlitterMat("Glitter Butter", new Color(1.4f, 1.3f, .7f), new Color(1.1f, .95f, .45f), new Color(1.5f, 1.4f, .9f), 1.5f);
        var oj = GlitterMat("Glitter Orange Juice", new Color(1.5f, .72f, .12f), new Color(1.15f, .42f, .04f), new Color(1.6f, 1f, .4f), 2.6f);
        var silver = GlitterMat("Glitter Silver", new Color(1.1f, 1.1f, 1.15f), new Color(.6f, .6f, .68f), new Color(1.4f, 1.4f, 1.5f), 2f);
        var glass = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Lounge/Bath Water.mat");
        var text = WaffleText();
        var sides = new[] { new { name = "His", z = -1f, plate = GlitterMat("Plate Blue", new Color(.8f, .9f, 1.25f), new Color(.55f, .65f, 1f), new Color(1.1f, 1.2f, 1.5f), 1f), straw = GlitterMat("Straw Blue", new Color(.45f, .75f, 1.4f), new Color(.3f, .5f, 1.1f), new Color(.8f, 1f, 1.5f), .5f) },
                            new { name = "Hers", z = 1f, plate = GlitterMat("Plate Pink", new Color(1.3f, .82f, .98f), new Color(1f, .55f, .8f), new Color(1.5f, 1.1f, 1.3f), 1f), straw = GlitterMat("Straw Pink", new Color(1.4f, .5f, .85f), new Color(1.1f, .3f, .65f), new Color(1.5f, .9f, 1.2f), .5f) } };
        foreach (var s in sides)
        {
            var set = new GameObject(s.name + " breakfast").transform; set.SetParent(table, false); set.localPosition = new Vector3(0, .745f, s.z * .27f); set.localRotation = Quaternion.Euler(0, s.z < 0 ? 0 : 180, 0);
            Prim(set, "Plate", cylinder, new Vector3(0, .006f, 0), new Vector3(.25f, .006f, .25f), s.plate);
            var waffle = new GameObject("Glitter waffle").transform; waffle.SetParent(set, false); waffle.localPosition = new Vector3(0, .014f, 0); waffle.localRotation = Quaternion.Euler(0, R(-8, 8), 0);
            Prim(waffle, "Waffle base", cylinder, new Vector3(0, .008f, 0), new Vector3(.17f, .008f, .17f), waffleMat);
            for (int k = 0; k <= 5; k++)
            {
                float o = -.07f + k * .028f, len = 2 * Mathf.Sqrt(Mathf.Max(0, .078f * .078f - o * o));
                Prim(waffle, "Ridge", cylinder, new Vector3(o, .02f, 0), new Vector3(.009f, len * .5f, .009f), waffleMat, new Vector3(90, 0, 0));
                Prim(waffle, "Ridge", cylinder, new Vector3(0, .02f, o), new Vector3(.009f, len * .5f, .009f), waffleMat, new Vector3(0, 0, 90));
            }
            Prim(waffle, "Butter", sphere, new Vector3(.052f, .031f, .045f), new Vector3(.026f, .011f, .022f), butter);
            if (text != null) Prim(waffle, "Astra <3 Mc Waffles", quad, new Vector3(0, .0305f, 0), new Vector3(.135f, .135f * text.mainTexture.height / text.mainTexture.width, 1f), text, new Vector3(90, 0, 0));
            var cup = new GameObject("Orange juice").transform; cup.SetParent(set, false); cup.localPosition = new Vector3(.17f, 0, .06f);
            Prim(cup, "Juice", cylinder, new Vector3(0, .052f, 0), new Vector3(.058f, .05f, .058f), oj);
            Prim(cup, "Glass", cylinder, new Vector3(0, .065f, 0), new Vector3(.068f, .065f, .068f), glass);
            Prim(cup, "Straw", cylinder, new Vector3(.012f, .12f, 0), new Vector3(.006f, .06f, .006f), s.straw, new Vector3(0, 0, -12));
            Prim(set, "Fork", cylinder, new Vector3(-.16f, .004f, 0), new Vector3(.008f, .08f, .004f), silver, new Vector3(90, 0, 0));
            Prim(set, "Knife", cylinder, new Vector3(.155f, .004f, -.06f), new Vector3(.01f, .085f, .003f), silver, new Vector3(90, 0, 0));
            ChairCloud(g, new Vector3(0, 0, s.z * .95f), s.z < 0 ? 0 : 180);
        }
        var bb = BoundsOf(CloudRenderers(g)); Sparkles(g, bb, "Cloud sparkles"); JumpPlatform(g, bb);
        report.Add("breakfast cloud at " + pos.ToString("0") + ": his & hers glitter waffles (" + (text != null ? "\"Astra <3 Mc Waffles\"" : "text FAIL") + "), orange juice, plates, cutlery, two cloud chairs");
    }
    static void ChairCloud(Transform parent, Vector3 pos, float yaw)
    {
        var c = new GameObject("Cloud chair").transform; c.SetParent(parent, false); c.localPosition = pos; c.localRotation = Quaternion.Euler(0, yaw, 0);
        for (int i = -1; i <= 1; i++) Puff(c, new Vector3(i * .22f, .3f, R(-.05f, .05f)), .5f, white);
        for (int i = -1; i <= 1; i++) Puff(c, new Vector3(i * .22f, .72f, -.28f), .45f, white);
        Puff(c, new Vector3(0, .95f, -.3f), .38f, white);
        var col = new GameObject("Cloud seat collider"); col.transform.SetParent(c, false); col.transform.localPosition = new Vector3(0, .3f, 0); col.AddComponent<BoxCollider>().size = new Vector3(.7f, .4f, .55f);
    }

    // "Astra <3 Mc Waffles" rendered from the built-in font into a transparent texture, with a drawn heart
    static Material WaffleText()
    {
        // two lines on the round waffle:  "Astra <heart>"  over  "Mc Waffles"  (hot pink letters with a thin white icing outline)
        var a = Word("Astra"); var b = Word("Mc Waffles"); if (a == null || b == null) return null;
        int h = a.height, heartW = (int)(h * .95f), gap = h / 6, pad = 10, lead = h / 8;
        int line1 = a.width + gap + heartW, W = Mathf.Max(line1, b.width) + pad * 2, H = h * 2 + lead + pad * 2;
        var mask = new float[W * H]; var isHeart = new bool[W * H];
        Action<Texture2D, int, int> blit = (t, x0, y0) => { var src = t.GetPixels(); for (int y = 0; y < t.height; y++) for (int x = 0; x < t.width; x++) { float m = src[y * t.width + x].r; int i = (y0 + y) * W + x0 + x; mask[i] = Mathf.Max(mask[i], Mathf.Clamp01(m * 1.4f)); } };
        int x1 = (W - line1) / 2, y1 = pad + h + lead;                  // top line (texture y grows upward)
        blit(a, x1, y1); blit(b, (W - b.width) / 2, pad);
        int hx0 = x1 + a.width + gap;
        for (int y = 0; y < h; y++) for (int x = 0; x < heartW; x++)
            {
                float u = (x / (float)heartW * 2 - 1) * 1.25f, v = (y / (float)h * 2 - 1) * 1.3f + .15f; float q = u * u + v * v - 1f;
                if (q * q * q - u * u * v * v * v < 0) { int i = (y1 + y) * W + hx0 + x; mask[i] = 1; isHeart[i] = true; }
            }
        UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
        var px = new Color[W * H]; var letter = new Color(.95f, .16f, .52f, 1); var heart = new Color(1f, .3f, .6f, 1); const int R0 = 5;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                int i = y * W + x; float o = 0;
                for (int dy = -R0; dy <= R0 && o < 1; dy++) for (int dx = -R0; dx <= R0; dx++)
                    {
                        if (dx * dx + dy * dy > R0 * R0) continue; int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue;
                        o = Mathf.Max(o, mask[yy * W + xx]);
                    }
                var c = Color.Lerp(new Color(1, .97f, .99f, o), isHeart[i] ? heart : letter, mask[i]); c.a = Mathf.Max(o, mask[i]); px[i] = c;
            }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true); tex.SetPixels(px); tex.Apply();
        Directory.CreateDirectory(Root + "Textures/Breakfast"); string tp = Root + "Textures/Breakfast/Astra Heart Mc Waffles.png"; File.WriteAllBytes(tp, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(tp); var ti = (TextureImporter)AssetImporter.GetAtPath(tp); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        string mp = Root + "Materials/Lounge/Waffle Text.mat"; var m2 = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (m2 == null) { m2 = new Material(Shader.Find("Unlit/Transparent")); AssetDatabase.CreateAsset(m2, mp); }
        m2.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(tp); EditorUtility.SetDirty(m2); return m2;
    }
    static Texture2D Word(string s)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); font.RequestCharactersInTexture(s, 160, FontStyle.Bold);
        var go = new GameObject("tmp text"); go.layer = 31; go.transform.position = new Vector3(0, -900, 0);
        var tm = go.AddComponent<TextMesh>(); tm.font = font; tm.fontSize = 160; tm.fontStyle = FontStyle.Bold; tm.characterSize = .01f; tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white; tm.text = s;
        var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = font.material;
        var cg = new GameObject("tmp cam"); var cam = cg.AddComponent<Camera>(); cam.enabled = false;
        try
        {
            var bnd = mr.bounds; if (bnd.size.x < .001f) return null;
            int H = 128, W = Mathf.CeilToInt(H * bnd.size.x / bnd.size.y * 1.04f);
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0); cam.cullingMask = 1 << 31;
            cam.transform.position = bnd.center + Vector3.back * 5; cam.transform.rotation = Quaternion.identity; cam.orthographicSize = bnd.extents.y * 1.08f; cam.aspect = W / (float)H; cam.nearClipPlane = .1f; cam.farClipPlane = 20;
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt; var t = new Texture2D(W, H, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply(); RenderTexture.active = prev;
            cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt); return t;
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(cg); }
    }

    // ================================================================== 13. banner backdrop
    [MenuItem("Astra/Claude/13 Banner backdrop (pink cloud sea + butterflies)")]
    public static void Banner()
    {
        Begin(BackupBanner);
        var plat = UnityEngine.Object.FindObjectsOfType<MeshCollider>(true).FirstOrDefault(c => c.name.StartsWith("Invisible platform"));
        FloorY = plat != null ? plat.bounds.max.y : 0f; SeaY = FloorY - 1.4f; report.Add("walkway floor at y " + FloorY.ToString("0.00"));
        Material sky = null, sea = null, fly = null;
        Step("sky + sea materials", () =>
        {
            Directory.CreateDirectory(Root + "Materials/Backdrop");
            sky = MatFor("Pink Cloud Sea Sky", "Astra/Cloud Sea Sky"); sea = MatFor("Pink Cloud Sea", "Astra/Cloud Sea");
            fly = MatFor("Pink Butterfly", "Astra/Butterfly"); fly.mainTexture = ButterflySheet(); EditorUtility.SetDirty(fly);
        });
        Step("sunset sky", () =>
        {
            RenderSettings.skybox = sky;
            var atm = UnityEngine.Object.FindObjectOfType<AstraAtmosphere>(); if (atm != null) { atm.startSky = sky; UdonSharpEditorUtility.CopyProxyToUdon(atm); }
            var pink = UnityEngine.Object.FindObjectOfType<AstraPinkscape>(); if (pink != null) { pink.pinkSky = sky; UdonSharpEditorUtility.CopyProxyToUdon(pink); }
            report.Add("sky: pink-to-gold sunset is the start sky; the PINKSCAPE button brings it back");
        });
        Step("endless cloud sea", BuildSea);
        Step("banner phone booth", () => HeroBooth(fly));
        Step("pink butterflies", () => Butterflies(fly));
        End("claude-banner-report.txt");
    }
    static float SeaY = -1.4f, FloorY = 0f;
    static void BuildSea()
    {
        var sea = MatFor("Pink Cloud Sea", "Astra/Cloud Sea");
        var old = GameObject.Find("15 - Pink cloud sea (Claude)"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var root = new GameObject("15 - Pink cloud sea (Claude)"); root.transform.position = new Vector3(0, SeaY, 0);
        var go = new GameObject("Cloud sea surface"); go.transform.SetParent(root.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = SeaMesh(); var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = sea; Quiet(r);
        // real 3D billows rising out of the sea: low lumps under the walkway (you wade over them) and big ones past the edge
        var puffs = new List<Matrix4x4>(); float hw = cloudMesh.bounds.size.y / cloudMesh.bounds.size.x;
        for (int i = 0; i < 70; i++) { float a = R(0, Mathf.PI * 2), d = Mathf.Sqrt(R(11f * 11f, 46f * 46f)), w = R(3f, 7f); puffs.Add(PuffMatrix(new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d), w, FloorY + R(-.8f, -.3f) - w * hw * .5f - SeaY)); }   // tops stay under the floor
        for (int i = 0; i < 120; i++) { float a = R(0, Mathf.PI * 2), d = R(50f, 115f), w = R(5f, 13f) * (d / 70f); puffs.Add(PuffMatrix(new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d), w, R(-.2f, .9f) * w * .2f)); }
        var billows = CombinedPuffs(root.transform, "Cloud sea billows", puffs, SeaPuffMat(), "Cloud sea billows");
        report.Add("cloud sea: 1.2 km pink cloud-top sea at y " + SeaY + " under the walkway, open over the stairs, with " + puffs.Count + " billows (one combined mesh, " + billows.vertexCount + " verts)");
    }
    // world matrix for one puff of width w whose bottom-centre sits at (p.x, SeaY + lift, p.z)
    static Matrix4x4 PuffMatrix(Vector3 p, float w, float lift)
    {
        var mb = cloudMesh.bounds; float k = w / mb.size.x; var rot = Quaternion.Euler(0, R(0, 360), 0);
        var centre = new Vector3(p.x, SeaY + lift, p.z); return Matrix4x4.TRS(centre - rot * (mb.center * k), rot, Vector3.one * k);
    }
    // many puffs baked into ONE mesh and one draw call (same look: the cloud shader only uses world position and normals)
    static Mesh CombinedPuffs(Transform parent, string name, List<Matrix4x4> worldMatrices, Material m, string assetName)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        var toLocal = go.transform.worldToLocalMatrix;
        var mesh = new Mesh { name = assetName }; mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.CombineMeshes(worldMatrices.Select(w => new CombineInstance { mesh = cloudMesh, transform = toLocal * w }).ToArray(), true, true); mesh.RecalculateBounds();
        string path = Root + "Meshes/Lounge/" + assetName + ".asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; Quiet(r); return mesh;
    }
    static Material SeaPuffMat()
    {
        var m = MatFor("Pink Sea Puff", "Astra/Glitter Cloud");
        m.SetColor("_Top", new Color(1f, .8f, .9f)); m.SetColor("_Bottom", new Color(.94f, .5f, .78f)); m.SetColor("_Rim", new Color(1.35f, 1f, .9f));
        m.SetFloat("_Glitter", 1.1f); m.SetFloat("_Pastel", 0); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }
    static Material MatFor(string name, string shader)
    {
        string path = Root + "Materials/Backdrop/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find(shader); if (m.shader == null) throw new Exception("shader missing: " + shader); EditorUtility.SetDirty(m); return m;
    }
    static Mesh SeaMesh()
    {
        const int seg = 160, rings = 48; float r0 = 9f, r1 = 1200f; var v = new List<Vector3>(); var t = new List<int>();
        for (int j = 0; j <= rings; j++) { float f = j / (float)rings; float r = r0 * Mathf.Pow(r1 / r0, f); for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2 / seg; v.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)); } }
        for (int j = 0; j < rings; j++) for (int i = 0; i < seg; i++) { int a = j * seg + i, b = j * seg + (i + 1) % seg, c = a + seg, d = b + seg; t.AddRange(new[] { a, c, b, b, c, d }); }
        var m = new Mesh { name = "Pink cloud sea" }; m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        string path = Root + "Meshes/Lounge/Pink cloud sea.asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path); return m;
    }
    static void AddSwitch(Transform holderParent, string label, GameObject[] targets)
    {
        Transform panel = null; foreach (var rr in EditorSceneManager.GetActiveScene().GetRootGameObjects()) foreach (var tt in rr.GetComponentsInChildren<Transform>(true)) if (tt.name == "World items") panel = tt;
        if (panel == null) return;
        var holder = new GameObject(label.ToLower() + " switch"); holder.transform.SetParent(holderParent, false);
        var os = holder.AddUdonSharpComponent<AstraObjectSwitch>(); os.targets = targets;
        var res = AstraAppearanceSwitches.Ensure(panel, label, UdonSharpEditorUtility.GetBackingUdonBehaviour(os), "Apply", true);
        os.toggle = res.Toggle; UdonSharpEditorUtility.CopyProxyToUdon(os); report.Add(res.Report);
    }

    // a copy of the booth, sunk into pink cloud exactly like the banner, door facing the pole with the sunset glow behind it
    static void HeroBooth(Material fly)
    {
        var src = Resources.FindObjectsOfTypeAll<Transform>().First(t => t.gameObject.scene.IsValid() && t.name == "Phone booth 1" && t.Find("Booth body") != null);
        var old = GameObject.Find("16 - Banner phone booth (Claude)"); Vector3 pos; float ov = 0;
        if (old != null) { pos = old.transform.position; UnityEngine.Object.DestroyImmediate(old); report.Add("banner booth rebuilt in the same spot"); }
        else
        {
            Survey();
            // the emptiest floor spot, preferring the sunset side (+z) so the view from spawn looks like the banner
            pos = BestSpot(() => { float a = R(0, Mathf.PI * 2), r = R(27f, 35f); return new Vector3(Mathf.Cos(a) * r, FloorY + 1.4f, Mathf.Sin(a) * r); },
                new Vector3(7f, 2.8f, 7f), .5f, p => Vector3.Angle(new Vector3(p.x, 0, p.z), Vector3.forward) * .02f, 2500, out ov);
            pos.y = FloorY; report.Add("banner booth spot overlap with other things: " + ov.ToString("0.0") + " m3");
        }
        var root = new GameObject("16 - Banner phone booth (Claude)").transform; root.position = pos; root.rotation = Quaternion.Euler(0, FaceCenter(pos), 0);
        var booth = UnityEngine.Object.Instantiate(src.gameObject, root).transform; booth.name = "Banner booth";
        booth.localPosition = Vector3.zero; booth.localRotation = Quaternion.identity; booth.localScale = Vector3.one * 1.1f;
        foreach (var t in booth.GetComponentsInChildren<Transform>(true).Where(x => x.name == "Phone booth voice zone" || x.name == "Booth cloud skirt" || x.name == "Pink butterflies").ToList()) if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
        // decorative copy: keep only meshes, lights, particles and colliders
        for (int pass = 0; pass < 4; pass++)
            foreach (var c in booth.GetComponentsInChildren<Component>(true).ToList())
            {
                if (c == null || c is Transform || c is MeshFilter || c is Renderer || c is Collider || c is Light || c is ParticleSystem) continue;
                bool udon = c is UdonSharpBehaviour || c.GetType().Name == "UdonBehaviour"; bool vrc = c.GetType().Namespace != null && c.GetType().Namespace.StartsWith("VRC");
                if (pass == 0 && !udon) continue; if (pass == 1 && !vrc) continue; if (pass == 2 && c is Rigidbody) continue;
                UnityEngine.Object.DestroyImmediate(c);
            }
        // pink cloud rising out of the sea and swallowing the booth's base (no floor inside the booth, you stand on the walkway)
        var pink = SeaPuffMat(); var mats = new List<Matrix4x4>(); float hw = cloudMesh.bounds.size.y / cloudMesh.bounds.size.x, half = .65f * 1.1f;
        Action<float, float, float, float, int> ring = (rr, w, top, jitter, n) =>
        {
            for (int i = 0; i < n; i++)
            {
                float a = (i + R(-.25f, .25f)) * Mathf.PI * 2 / n, ww = w * R(.85f, 1.15f); var lp = new Vector3(Mathf.Cos(a) * rr * R(.95f, 1.05f), 0, Mathf.Sin(a) * rr * R(.95f, 1.05f));
                var wp = root.TransformPoint(lp); mats.Add(PuffMatrix(new Vector3(wp.x, 0, wp.z), ww, (FloorY + top + R(-jitter, jitter)) - ww * hw * .5f - SeaY));
            }
        };
        ring(.3f, 1.1f, -.03f, .02f, 4);                  // cloud filling the booth floor, just under your feet
        ring(half + .6f, 1f, .38f, .06f, 18);              // hugging the walls (the banner look)
        ring(half + 1.4f, 1.5f, .42f, .1f, 20);
        ring(half + 2.5f, 2.1f, .25f, .12f, 22);
        ring(half + 3.8f, 2.8f, -.1f, .15f, 24);
        ring(half + 5.3f, 3.5f, -.55f, .2f, 26);           // melting into the sea
        var mound = CombinedPuffs(root, "Cloud mound", mats, pink, "Banner booth cloud mound");
        var outer = GameObject.Find("14 - Outer big clouds (Claude)"); int carved = 0;
        if (outer != null)
        {
            float ba = Mathf.Atan2(pos.z, pos.x);
            foreach (var seg in outer.transform.Cast<Transform>().Where(x => x.name.StartsWith("Outer cloud")).ToList())
            {
                float sa = Mathf.Atan2(seg.position.z, seg.position.x); if (Mathf.Abs(Mathf.DeltaAngle(sa * Mathf.Rad2Deg, ba * Mathf.Rad2Deg)) < 15f) { UnityEngine.Object.DestroyImmediate(seg.gameObject); carved++; }
            }
            var sw = outer.transform.Find("Outer clouds switch"); var os = sw != null ? sw.GetComponent<AstraObjectSwitch>() : null;
            if (os != null) { os.targets = outer.transform.Cast<Transform>().Where(x => x != sw).Select(x => x.gameObject).ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(os); }
        }
        if (carved > 0) report.Add("opened the outer track behind the banner booth (" + carved + " segments) so the endless sea shows behind it");
        var mb = root.Find("Cloud mound").GetComponent<MeshRenderer>().bounds; mb.size = new Vector3(mb.size.x * .5f, 1.2f, mb.size.z * .5f); mb.center = new Vector3(mb.center.x, FloorY + .3f, mb.center.z);
        Sparkles(root, mb, "Cloud sparkles");
        report.Add("banner booth: a copy of the booth sunk in pink cloud at " + pos.ToString("0") + " (" + mats.Count + " puffs, one mesh), door facing the pole, sunset glow behind it");
    }

    // pink sprite-sheet butterflies: 8 flap frames, played many times per life so they really flap
    static void ConfigureButterflies(ParticleSystem ps, Material fly, int max, float rate, bool local)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.prewarm = true; main.playOnAwake = true; main.maxParticles = max; main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 15f); main.startSpeed = new ParticleSystem.MinMaxCurve(.15f, .45f); main.startSize = new ParticleSystem.MinMaxCurve(.16f, .3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-.5f, .5f); main.startRotation3D = false; main.gravityModifier = 0; main.scalingMode = ParticleSystemScalingMode.Shape;
        var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(new Color(1f, .45f, .75f), 0), new GradientColorKey(new Color(1f, .7f, .9f), .5f), new GradientColorKey(new Color(.95f, .55f, 1f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        var em = ps.emission; em.enabled = true; em.rateOverTime = rate;
        var sol = ps.sizeOverLifetime; sol.enabled = false; var rol = ps.rotationOverLifetime; rol.enabled = true; rol.separateAxes = false; rol.z = new ParticleSystem.MinMaxCurve(-.4f, .4f);
        var noise = ps.noise; noise.enabled = true; noise.strength = .7f; noise.frequency = .35f; noise.scrollSpeed = .25f; noise.damping = true; noise.quality = ParticleSystemNoiseQuality.Medium;
        var col = ps.colorOverLifetime; col.enabled = true; var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .08f), new GradientAlphaKey(1, .9f), new GradientAlphaKey(0, 1) }); col.color = fade;
        var ts = ps.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = 4; ts.numTilesY = 2; ts.animation = ParticleSystemAnimationType.WholeSheet;
        ts.frameOverTime = new ParticleSystem.MinMaxCurve(.9999f, AnimationCurve.Linear(0, 0, 1, 1)); ts.cycleCount = 50;
        var pr = ps.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Billboard; pr.mesh = null; pr.sharedMaterial = fly; pr.alignment = ParticleSystemRenderSpace.View; Quiet(pr);
        ps.Play();
    }
    static ParticleSystem NewFlock(Transform parent, Vector3 localPos, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localRotation = Quaternion.identity; return go.AddComponent<ParticleSystem>();
    }
    static void Butterflies(Material fly)
    {
        int fixedN = 0, newN = 0, moved = 0;
        foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true).Where(p => p.name == "Butterflies" || p.name == "Butterfly swarm").ToList())
        {
            // the old flap squeezed the particle width with a 91-key curve (Unity keeps 8), and the DJ flock sat under a squashed cloud,
            // so they came out as stretched, barely-moving shapes. Give every flock an even-scaled parent and real flap frames.
            var t = ps.transform;
            if (!Uniform(t.lossyScale) || Mathf.Abs(Mathf.Abs(t.lossyScale.x) - 1f) > .05f)
            {
                Transform anc = t.parent; while (anc != null && !Uniform(anc.lossyScale)) anc = anc.parent;
                t.SetParent(anc, true); t.localScale = anc != null ? Vector3.one / Mathf.Abs(anc.lossyScale.x) : Vector3.one; t.rotation = Quaternion.identity; moved++;
            }
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box;
            bool lounge = ps.name == "Butterfly swarm";
            sh.scale = lounge ? new Vector3(16f, 5f, 16f) : new Vector3(6f, 1.6f, 4f);
            ConfigureButterflies(ps, fly, lounge ? 36 : 20, lounge ? 3f : 2f, false); fixedN++;
        }
        var hero = GameObject.Find("16 - Banner phone booth (Claude)");
        if (hero != null) { var ps = NewFlock(hero.transform, new Vector3(0, FloorY - hero.transform.position.y + 2f, 0), "Pink butterflies"); var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 4.5f; ConfigureButterflies(ps, fly, 34, 3f, false); newN++; }
        foreach (var b in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid() && (t.name == "Phone booth 1" || t.name == "Phone booth 2") && t.Find("Booth body") != null).ToList())
        {
            var old = b.Find("Pink butterflies"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            // these booths ride orbiting clouds, so their butterflies fly in the booth's own space and travel with it
            var ps = NewFlock(b, new Vector3(0, 1.8f, 0), "Pink butterflies"); var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 2.4f; ConfigureButterflies(ps, fly, 14, 1.4f, true); newN++;
        }
        report.Add("butterflies: " + fixedN + " existing flocks rebuilt as pink flapping butterflies (" + moved + " moved off squashed parents), " + newN + " new flocks (banner booth + both phone booths)");
    }
    static Texture2D ButterflySheet()
    {
        const int F = 128, W = F * 4, H = F * 2; var px = new Color[W * H];
        for (int k = 0; k < 8; k++)
        {
            float open = .18f + .82f * Mathf.Abs(Mathf.Cos(Mathf.PI * k / 8f));
            int fx = (k % 4) * F, fy = (1 - k / 4) * F;
            for (int y = 0; y < F; y++) for (int x = 0; x < F; x++)
                {
                    float u = x / (float)F * 2 - 1, v = y / (float)F * 2 - 1; Color c = new Color(0, 0, 0, 0);
                    float wx = Mathf.Abs(u) / open;
                    float up = Sq((wx - .46f) / .44f) + Sq((v - .2f) / .36f), lo = Sq((wx - .34f) / .32f) + Sq((v + .28f) / .3f);
                    float q = Mathf.Min(up, lo);
                    if (q < 1 && wx > .03f)
                    {
                        float edge = Mathf.SmoothStep(.62f, 1f, q);
                        c = Color.Lerp(new Color(1, 1, 1, 1), new Color(.78f, .5f, .74f, 1), edge * .85f);
                        float spot = Sq((wx - .72f) / .07f) + Sq((v - .34f) / .07f); if (spot < 1) c = Color.white;
                        c.a = Mathf.Clamp01((1 - q) * 14f);
                    }
                    if (Mathf.Abs(u) < .045f && Mathf.Abs(v + .02f) < .42f) c = new Color(.4f, .2f, .35f, 1);                       // body
                    for (int s = -1; s <= 1; s += 2) { float t = Mathf.InverseLerp(.38f, .7f, v); if (t > 0 && t < 1 && Mathf.Abs(u - s * (.04f + .14f * t)) < .014f) c = new Color(.4f, .2f, .35f, 1); } // antennae
                    px[(fy + y) * W + fx + x] = c;
                }
        }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, true); tex.SetPixels(px); tex.Apply();
        Directory.CreateDirectory(Root + "Textures/Backdrop"); string tp = Root + "Textures/Backdrop/Butterfly flap sheet.png"; File.WriteAllBytes(tp, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(tp); var ti = (TextureImporter)AssetImporter.GetAtPath(tp); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
    }
    static float Sq(float a) { return a * a; }

    // ================================================================== 15. round 2
    // Bath water fills the whole bowl (no see-through column), DJ stage lights + cables gone and the console back closed,
    // each phone booth sits in ONE puffy cloud, some orbit clouds become hearts and stars, more swings (a set of three) with sparkle rain.
    const string BackupRound2 = "Review/Backups/BeforeRound2.unity.txt";
    [MenuItem("Astra/Claude/15 Round 2 (bath water, DJ, booth clouds, hearts + stars, swings)")]
    public static void Round2()
    {
        EnsureProgram("AstraSwing", true);
        Begin(BackupRound2);
        var plat = UnityEngine.Object.FindObjectsOfType<MeshCollider>(true).FirstOrDefault(c => c.name.StartsWith("Invisible platform"));
        FloorY = plat != null ? plat.bounds.max.y : 0f; SeaY = FloorY - 1.4f;
        Step("bath water fills the whole bowl", BathWater);
        Step("DJ: no stage lights, no cables, closed console back", DJTidy);
        Step("phone booths in one cloud each", BoothClouds);
        Step("heart and star clouds", HeartStarClouds);
        Step("more swings + sparkle rain", Swings);
        End("claude-round2-report.txt");
    }
    [MenuItem("Astra/Claude/Restore scene from before round 2")] public static void RestoreRound2() { RestoreFrom(BackupRound2, "round 2 pass"); }

    static Matrix4x4 PuffAt(Vector3 centre, float w)
    {
        var mb = cloudMesh.bounds; float k = w / mb.size.x; var rot = Quaternion.Euler(0, R(0, 360), 0);
        return Matrix4x4.TRS(centre - rot * (mb.center * k), rot, Vector3.one * k);
    }
    static Mesh SaveMesh(Mesh m, string name)
    {
        string path = Root + "Meshes/Lounge/" + name + ".asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path); return m;
    }

    static void BathWater()
    {
        var dish = lounge.GetComponentsInChildren<Transform>(true).First(t => t.name == "Bath dish");
        var water = dish.Find("Bath water"); var wf = water.GetComponent<MeshFilter>();
        if (wf.sharedMesh != null && wf.sharedMesh.name == "Bath water volume") { report.Add("bath water already fills the bowl"); return; }
        float rx = water.localScale.x / 2f / 1.02f, rz = water.localScale.z / 2f / 1.02f;
        const float depth = 4.2f; float wy = depth - .3f, y0 = .35f;
        // same profile as the cloud walls (pass 12): the water's edge runs through the middle of the wall puffs, so there is no gap anywhere
        Func<float, float> F = y => (.84f + .2f * Mathf.Sqrt(Mathf.Clamp01((y - .45f) / (depth - .55f)))) * .99f;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); const int A = 96, Rn = 10;
        for (int j = 0; j <= Rn; j++) { float y = Mathf.Lerp(y0, wy, j / (float)Rn), f = F(y); for (int i = 0; i <= A; i++) { float a = i * Mathf.PI * 2 / A; v.Add(new Vector3(Mathf.Cos(a) * rx * f, y, Mathf.Sin(a) * rz * f)); uv.Add(new Vector2(i / (float)A, (y - y0) / (wy - y0))); } }
        for (int j = 0; j < Rn; j++) for (int i = 0; i < A; i++) { int a = j * (A + 1) + i, b = a + 1, c = a + A + 1, d = c + 1; tri.AddRange(new[] { a, c, b, b, c, d }); }
        foreach (var cap in new[] { new Vector2(wy, 1), new Vector2(y0, -1) })
        {
            int centre = v.Count; v.Add(new Vector3(0, cap.x, 0)); uv.Add(new Vector2(.5f, .5f)); float f = F(cap.x);
            for (int i = 0; i <= A; i++) { float a = i * Mathf.PI * 2 / A; v.Add(new Vector3(Mathf.Cos(a) * rx * f, cap.x, Mathf.Sin(a) * rz * f)); uv.Add(new Vector2(.5f + .5f * Mathf.Cos(a), .5f + .5f * Mathf.Sin(a))); }
            for (int i = 0; i < A; i++) { if (cap.y > 0) tri.AddRange(new[] { centre, centre + 2 + i, centre + 1 + i }); else tri.AddRange(new[] { centre, centre + 1 + i, centre + 2 + i }); }
        }
        var m = new Mesh { name = "Bath water volume" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        wf.sharedMesh = SaveMesh(m, "Bath water volume"); water.localPosition = Vector3.zero; water.localRotation = Quaternion.identity; water.localScale = Vector3.one;
        var deep = dish.Find("Bath water depth"); if (deep != null) UnityEngine.Object.DestroyImmediate(deep.gameObject);
        report.Add("bath: one water body shaped like the bowl, surface at " + wy.ToString("0.0") + " m, edges hidden inside the wall puffs (the separate depth column is gone)");
    }

    static void DJTidy()
    {
        var dj = GameObject.Find("12 - DJ cloud").transform;
        var lights = dj.Find("Stage lights"); if (lights != null) { UnityEngine.Object.DestroyImmediate(lights.gameObject); report.Add("DJ stage lights deleted"); }
        var boothT = dj.Find("DJ booth");
        var cables = boothT != null ? boothT.Find("Deck cables") : null;
        if (cables != null) { cables.gameObject.SetActive(false); report.Add("DJ cables hidden (turned off, so the platter/pulse script keeps working)"); }
        var console = boothT != null ? boothT.Find("Deck console") : null; if (console == null) { report.Add("DJ console not found"); return; }
        var oldPanel = console.Find("Console back panel"); if (oldPanel != null) UnityEngine.Object.DestroyImmediate(oldPanel.gameObject);
        var mesh = console.GetComponent<MeshFilter>().sharedMesh; var mb = mesh.bounds; var verts = mesh.vertices; var mr = console.GetComponent<MeshRenderer>();
        // the lit MOMMY'S letters sit on the front face: the submesh furthest out along z tells us which side is the front
        float bestOff = 0; int bodySub = 0, most = -1;
        for (int sm = 0; sm < mesh.subMeshCount; sm++)
        {
            var tris = mesh.GetTriangles(sm); if (tris.Length == 0) continue; if (tris.Length > most) { most = tris.Length; bodySub = sm; }
            float z = 0; foreach (var i in tris) z += verts[i].z; z /= tris.Length; float off = (z - mb.center.z) / Mathf.Max(.001f, mb.extents.z);
            if (Mathf.Abs(off) > Mathf.Abs(bestOff)) bestOff = off;
        }
        float front = Mathf.Sign(bestOff); string how = "letters";
        if (Mathf.Abs(bestOff) < .3f) { var step = dj.Find("DJ step cloud"); front = step != null ? Mathf.Sign(console.InverseTransformPoint(step.position).z - mb.center.z) : 1f; how = "crowd side"; }
        var box = console.GetComponents<BoxCollider>().FirstOrDefault(); var bc = box != null ? box.center : new Vector3(0, .5f, 0); var bs = box != null ? box.size : new Vector3(2.6f, 1f, .78f);
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.name = "Console back panel"; panel.transform.SetParent(console, false);
        panel.transform.localPosition = new Vector3(bc.x, bc.y - bs.y * .04f, bc.z - front * (bs.z * .5f - .015f)); panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(bs.x * .99f, bs.y * .9f, .02f);
        var pr = panel.GetComponent<MeshRenderer>(); var mats = mr.sharedMaterials.Where(x => x != null).ToArray();
        var dark = mats.OrderBy(x => x.HasProperty("_Top") ? x.GetColor("_Top").grayscale : (x.HasProperty("_Color") ? x.color.grayscale : 1f)).First();
        pr.sharedMaterial = dark; Quiet(pr);
        report.Add("DJ console: back closed with a panel in the console's own finish (front found from the " + how + "), so the lettering no longer shows through from the DJ side");
    }

    static void BoothClouds()
    {
        var booths = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid() && (t.name == "Phone booth 1" || t.name == "Phone booth 2") && t.Find("Booth body") != null).ToList();
        float hw = cloudMesh.bounds.size.y / cloudMesh.bounds.size.x;
        foreach (var b in booths)
        {
            var cloud = b.parent; var cr = cloud.GetComponent<MeshRenderer>(); if (cr == null) { report.Add(b.name + ": no cloud renderer, skipped"); continue; }
            var skirt = b.Find("Booth cloud skirt"); if (skirt != null) UnityEngine.Object.DestroyImmediate(skirt.gameObject);
            var old = cloud.Find("Booth cloud"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var mat = cr.sharedMaterial; cr.enabled = false;   // the old single cloud stays only as the invisible floor you stand on (it still carries you round)
            var bp = b.position; var bl = b.lossyScale; float bw = (Mathf.Abs(bl.x) + Mathf.Abs(bl.y) + Mathf.Abs(bl.z)) / 3f, half = .65f * bw; var rot = b.rotation;
            var ms = new List<Matrix4x4>();
            Action<float, float, float, float, int> ring = (rr, w, top, jitter, n) =>
            {
                for (int i = 0; i < n; i++)
                {
                    float a = (i + R(-.25f, .25f)) * Mathf.PI * 2 / n, ww = w * R(.85f, 1.15f);
                    var c = bp + rot * new Vector3(Mathf.Cos(a) * rr * R(.95f, 1.05f), 0, Mathf.Sin(a) * rr * R(.95f, 1.05f)); c.y = bp.y + top + R(-jitter, jitter) - ww * hw * .5f; ms.Add(PuffAt(c, ww));
                }
            };
            ring(.3f, 1.1f, -.03f, .02f, 4);          // soft floor inside the booth, just under your feet
            ring(half + .55f, 1f, .36f, .06f, 16);     // hugging the walls, like the banner
            ring(half + 1.3f, 1.4f, .3f, .08f, 18);
            ring(half + 2.0f, 1.6f, .05f, .1f, 18);
            foreach (var layer in new[] { new Vector3(-.75f, 2.2f, 2.1f), new Vector3(-1.45f, 1.5f, 1.8f), new Vector3(-2.05f, .8f, 1.4f) })   // rounded underside: (depth, radius, puff width)
            {
                int n = Mathf.Max(3, Mathf.RoundToInt(2 * Mathf.PI * layer.y / (layer.z * .7f)));
                for (int i = 0; i < n; i++) { float a = (i + R(-.3f, .3f)) * Mathf.PI * 2 / n; ms.Add(PuffAt(bp + new Vector3(Mathf.Cos(a) * layer.y, layer.x, Mathf.Sin(a) * layer.y), layer.z * R(.85f, 1.15f))); }
                ms.Add(PuffAt(bp + Vector3.up * layer.x, layer.z));
            }
            CombinedPuffs(cloud, "Booth cloud", ms, mat, "Booth cloud " + b.name.Substring(b.name.Length - 1));
            report.Add(b.name + ": sits in one puffy cloud (" + ms.Count + " puffs, one mesh), the old cloud and the separate skirt are gone");
        }
    }

    // ---- hearts and stars you see from above: puffy pillow shapes, one shared mesh each (cheap, GPU-instanced)
    static List<Vector2> heartPoly;
    static float HeartRadius(float th)
    {
        if (heartPoly == null)
        {
            heartPoly = new List<Vector2>();
            for (int i = 0; i < 720; i++) { float t = i * Mathf.PI * 2 / 720, sn = Mathf.Sin(t); heartPoly.Add(new Vector2(16 * sn * sn * sn, 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t) + 2f) / 32f); }
        }
        var d = new Vector2(Mathf.Cos(th), Mathf.Sin(th)); float best = 0;
        for (int i = 0; i < heartPoly.Count; i++)
        {
            var p = heartPoly[i]; var e = heartPoly[(i + 1) % heartPoly.Count] - p; float den = d.x * e.y - d.y * e.x; if (Mathf.Abs(den) < 1e-7f) continue;
            float t = (p.x * e.y - p.y * e.x) / den, u = (p.x * d.y - p.y * d.x) / den; if (t > best && u >= 0 && u <= 1) best = t;
        }
        return best;
    }
    static float StarRadius(float th) { return .5f * (.54f + .46f * Mathf.Pow(.5f + .5f * Mathf.Cos(5 * (th - Mathf.PI / 2)), 1.5f)); }
    static Mesh ShapeMesh(string name, Func<float, float> radius)
    {
        const int A = 144, J = 9; const float Ht = .27f, Hb = .2f; var v = new List<Vector3>(); var tri = new List<int>();
        Func<float, float, float, Vector3> P = (th, sv, hsign) =>
        {
            float r = radius(th) * sv, e = Mathf.Sqrt(Mathf.Max(0, 1 - sv * sv));
            float h = hsign > 0 ? Ht * Mathf.Pow(e, .75f) : -Hb * Mathf.Pow(e, .7f);
            return new Vector3(r * Mathf.Cos(th), h, -r * Mathf.Sin(th));   // 2D "up" points away from the pole (-z), so it reads upright from the stairs
        };
        // top: centre + J rings (ring J is the shared rim); bottom: centre + J-1 rings, joined to the same rim
        int top0 = v.Count; v.Add(new Vector3(0, Ht, 0));
        for (int j = 1; j <= J; j++) for (int i = 0; i < A; i++) v.Add(P(i * Mathf.PI * 2 / A, j / (float)J, 1));
        int bot0 = v.Count; v.Add(new Vector3(0, -Hb, 0));
        for (int j = 1; j < J; j++) for (int i = 0; i < A; i++) v.Add(P(i * Mathf.PI * 2 / A, j / (float)J, -1));
        Func<int, int, int> T = (j, i) => j == 0 ? top0 : top0 + 1 + (j - 1) * A + (i % A);
        Func<int, int, int> B = (j, i) => j == 0 ? bot0 : j == J ? T(J, i) : bot0 + 1 + (j - 1) * A + (i % A);
        for (int i = 0; i < A; i++) { tri.AddRange(new[] { T(0, 0), T(1, i), T(1, i + 1) }); tri.AddRange(new[] { B(0, 0), B(1, i + 1), B(1, i) }); }
        for (int j = 1; j < J; j++) for (int i = 0; i < A; i++)
            {
                tri.AddRange(new[] { T(j, i), T(j + 1, i), T(j + 1, i + 1), T(j, i), T(j + 1, i + 1), T(j, i + 1) });
                tri.AddRange(new[] { B(j, i), B(j + 1, i + 1), B(j + 1, i), B(j, i), B(j, i + 1), B(j + 1, i + 1) });
            }
        var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals();
        if (m.normals[top0].y < 0) { for (int k = 0; k < tri.Count; k += 3) { int t0 = tri[k]; tri[k] = tri[k + 1]; tri[k + 1] = t0; } m.SetTriangles(tri, 0); m.RecalculateNormals(); }
        m.RecalculateBounds(); return SaveMesh(m, name);
    }
    static void HeartStarClouds()
    {
        var orbit = UnityEngine.Object.FindObjectOfType<AstraCloudOrbit>(); if (orbit == null) return;
        var heart = ShapeMesh("Heart cloud", HeartRadius); var star = ShapeMesh("Star cloud", StarRadius); int hearts = 0, stars = 0;
        for (int j = 0; j < orbit.clouds.Length; j++)
        {
            var c = orbit.clouds[j]; if (c == null) continue; var cr = c.GetComponent<MeshRenderer>(); var cf = c.GetComponent<MeshFilter>();
            if (cr == null || cf == null || cf.sharedMesh == null) continue;
            if (c.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Phone booth"))) continue;
            foreach (var n in new[] { "Heart cloud", "Star cloud" }) { var o = c.Find(n); if (o != null) { UnityEngine.Object.DestroyImmediate(o.gameObject); cr.enabled = true; } }
            int k = j % 8; if (k != 1 && k != 5) continue;
            bool isHeart = k == 1; var mb = cf.sharedMesh.bounds; var ls = c.lossyScale;
            float W = Mathf.Max(Mathf.Abs(ls.x) * mb.size.x, Mathf.Abs(ls.z) * mb.size.z) * 1.05f;
            var go = new GameObject(isHeart ? "Heart cloud" : "Star cloud"); go.transform.SetParent(c, false);
            go.transform.localPosition = mb.center; go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(W / Mathf.Abs(ls.x), W / Mathf.Abs(ls.y), W / Mathf.Abs(ls.z));   // cancels the cloud's stretch: true proportions
            go.AddComponent<MeshFilter>().sharedMesh = isHeart ? heart : star; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = cr.sharedMaterial; Quiet(r);
            cr.enabled = false; if (isHeart) hearts++; else stars++;
        }
        report.Add("orbit clouds: " + hearts + " hearts and " + stars + " stars (a quarter of them), puffy from the side, clear shapes from above; they still carry you round");
    }

    // ---- more swings: a set of three side by side under one long cloud, two singles, sparkle rain falling below every seat
    static void Swings()
    {
        var src = lounge.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Cloud swing"); if (src == null) throw new Exception("Cloud swing not found");
        if (src.GetComponentsInChildren<Transform>(true).All(t => t.name != "Swing pivot")) throw new Exception("run 12 Polish first: the swing has no seat yet");
        var parent = src.parent;
        foreach (var o in parent.Cast<Transform>().Where(t => t.name.StartsWith("Cloud swing ") || t.name == "Cloud swing trio").ToList()) UnityEngine.Object.DestroyImmediate(o.gameObject);
        Survey(); float ov; int swings = 0;
        var trioPos = SwingSpot(new[] { -3.6f, 0f, 3.6f }, out ov);
        var trio = new GameObject("Cloud swing trio").transform; trio.SetParent(parent, true); trio.position = trioPos; trio.rotation = Quaternion.Euler(0, FaceCenter(trioPos), 0);
        for (int i = -1; i <= 1; i++) { MakeSwing(src, trio, new Vector3(i * 3.6f, 0, 0), "Cloud swing " + (i + 3)); swings++; }
        Rain(trio, SeatLocal(trio) + Vector3.down * .3f, new Vector3(12f, .3f, 7f)); Claim(trio.GetComponentsInChildren<Renderer>());
        report.Add("swing trio (3 side by side) at " + trio.position.ToString("0") + ", overlap " + ov.ToString("0.0") + " m3");
        for (int k = 0; k < 2; k++)
        {
            var p = SwingSpot(new[] { 0f }, out ov);
            var g = new GameObject("Cloud swing single " + (k + 1)).transform; g.SetParent(parent, true); g.position = p; g.rotation = Quaternion.Euler(0, FaceCenter(p), 0);
            MakeSwing(src, g, Vector3.zero, "Cloud swing " + (5 + k)); swings++;
            Rain(g, SeatLocal(g) + Vector3.down * .3f, new Vector3(4f, .3f, 7f)); Claim(g.GetComponentsInChildren<Renderer>());
            report.Add("single swing at " + g.position.ToString("0") + ", overlap " + ov.ToString("0.0") + " m3");
        }
        Rain(src, SeatLocal(src) + Vector3.down * .3f, new Vector3(4f, .3f, 7f));
        report.Add("swings: " + (swings + 1) + " in all (each with its own seat, swinging out of step), sparkle rain falling below every seat");
    }
    static Vector3 SwingSpot(float[] offsets, out float overlap)
    {
        Vector3 best = Vector3.zero; float bestScore = float.MaxValue; overlap = 0;
        for (int t = 0; t < 4000; t++)
        {
            float a = R(0, Mathf.PI * 2), r = R(27f, 34f); var p = new Vector3(Mathf.Cos(a) * r, FloorY - .1f, Mathf.Sin(a) * r);
            var fwd = -new Vector3(p.x, 0, p.z).normalized; var right = Vector3.Cross(Vector3.up, fwd); float ov = 0;
            foreach (var o in offsets)
            {
                var c = p + right * o + Vector3.up * 6.5f; var box = new Bounds(c, new Vector3(8.5f, 12.5f, 8.5f));
                foreach (var b in taken) if (b.size.x < 60 && b.size.z < 60) ov += Overlap(b, box);
                if (NearScreen(c, 5f)) ov += 1000f;
            }
            if (ov < bestScore) { bestScore = ov; best = p; overlap = ov; }
        }
        return best;
    }
    static Vector3 SeatLocal(Transform g)
    {
        var seats = g.GetComponentsInChildren<VRCStation>(true); if (seats.Length == 0) return new Vector3(0, 1f, 0);
        var c = Vector3.zero; foreach (var s in seats) c += s.transform.position; return g.InverseTransformPoint(c / seats.Length);
    }
    static Transform MakeSwing(Transform src, Transform parent, Vector3 localPos, string name)
    {
        var copy = UnityEngine.Object.Instantiate(src.gameObject, parent).transform; copy.name = name;
        copy.localPosition = localPos; copy.localRotation = Quaternion.identity; copy.localScale = src.lossyScale;
        foreach (var old in copy.Cast<Transform>().Where(t => t.name != "Furniture 2x" && t.name != "Swing pivot" && t.name != "Cloud sparkles").ToList()) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var pivot = copy.GetComponentsInChildren<Transform>(true).First(t => t.name == "Swing pivot");
        var sw = copy.GetComponentInChildren<AstraSwing>(true);
        if (sw != null) { sw.pivot = pivot; sw.phase = R(0, 6.28f); UdonSharpEditorUtility.CopyProxyToUdon(sw); }
        return copy;
    }
    static void Rain(Transform parent, Vector3 localCentre, Vector3 size)
    {
        var old = parent.Find("Sparkle rain"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var star = GameObject.Find("10 - Stair opening star dust")?.GetComponent<ParticleSystemRenderer>()?.sharedMaterial;
        var go = new GameObject("Sparkle rain"); go.transform.SetParent(parent, false); go.transform.localPosition = localCentre; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.prewarm = true; main.maxParticles = 420; main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.6f); main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.04f, .11f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, .8f, .95f));
        var em = ps.emission; em.rateOverTime = Mathf.Min(115f, size.x * size.z * 1.3f);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-.05f, .05f); vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -.9f); vel.z = new ParticleSystem.MinMaxCurve(-.05f, .05f);
        var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(1, .75f), new GradientAlphaKey(0, 1) }); col.color = gr;
        var pr = ps.GetComponent<ParticleSystemRenderer>(); if (star != null) pr.sharedMaterial = star; Quiet(pr);
        ps.Play();
    }

    // ================================================================== 16. what blocks the stairs
    // Walks a player-sized capsule up every step of every stair turn, with the orbiting clouds swept through a full turn,
    // and lists every collider that gets in the way. Changes nothing.
    [MenuItem("Astra/Claude/16 Find what blocks the stairs")]
    public static void StairBlockers()
    {
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>(); spiral.Recenter(0);
        var orbit = UnityEngine.Object.FindObjectOfType<AstraCloudOrbit>();
        var saved = orbit != null ? orbit.pivots.Select(x => x.localRotation).ToArray() : new Quaternion[0];
        var own = new HashSet<Collider>(spiral.turns.SelectMany(t => t.GetComponentsInChildren<Collider>(true)));
        var centre = spiral.pole != null ? spiral.pole.position : Vector3.zero;
        var hits = new Dictionary<string, int>(); var poses = new Dictionary<string, HashSet<int>>(); var first = new Dictionary<string, string>(); int samples = 0, blocked = 0;
        try
        {
            for (int pose = 0; pose < 24; pose++)
            {
                if (orbit != null) for (int i = 0; i < orbit.pivots.Length; i++) orbit.pivots[i].localRotation = Quaternion.Euler(0, pose * 15f, 0) * saved[i];
                Physics.SyncTransforms();
                foreach (var t in spiral.turns)
                {
                    var mc = t.GetComponent<MeshCollider>(); if (mc == null) continue;
                    for (float a = 0; a < 360; a += 5) foreach (var rr in new[] { 4.6f, 5.35f, 6.1f })
                        {
                            var at = centre + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * rr, 0, Mathf.Sin(a * Mathf.Deg2Rad) * rr); RaycastHit h;
                            if (!mc.Raycast(new Ray(new Vector3(at.x, t.position.y + 30, at.z), Vector3.down), out h, 80)) continue;
                            if (pose == 0) samples++; bool any = false;
                            foreach (var c in Physics.OverlapCapsule(h.point + Vector3.up * .45f, h.point + Vector3.up * 1.55f, .26f, ~0, QueryTriggerInteraction.Ignore))
                            {
                                if (own.Contains(c)) continue; any = true; string k = PathOf(c.transform) + " [" + c.GetType().Name + "]";
                                hits[k] = (hits.ContainsKey(k) ? hits[k] : 0) + 1; if (!poses.ContainsKey(k)) { poses[k] = new HashSet<int>(); first[k] = "turn " + t.name + " at " + a + " deg, r " + rr + ", y " + h.point.y.ToString("0.0"); }
                                poses[k].Add(pose);
                            }
                            if (any && pose == 0) blocked++;
                        }
                }
            }
        }
        finally { if (orbit != null) for (int i = 0; i < orbit.pivots.Length; i++) orbit.pivots[i].localRotation = saved[i]; Physics.SyncTransforms(); }
        var lines = new List<string> { "stair samples: " + samples + ", blocked right now: " + blocked, "colliders in the way (hits, in how many of 24 orbit poses, first place):" };
        lines.AddRange(hits.OrderByDescending(kv => kv.Value).Take(60).Select(kv => kv.Value + "  " + poses[kv.Key].Count + "/24  " + kv.Key + "  @ " + first[kv.Key]));
        File.WriteAllText("Review/claude-stairs-report.txt", string.Join("\n", lines)); Debug.Log("ASTRA_STAIRS\n" + string.Join("\n", lines.Take(20)));
    }
    // any jump platform reaching into the stair column (r < 7.2 m around the pole, at any height: the stairs recycle upward forever) is removed
    static void ClearStairs()
    {
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>(); var c0 = spiral.pole != null ? spiral.pole.position : Vector3.zero;
        var own = new HashSet<Collider>(spiral.turns.SelectMany(t => t.GetComponentsInChildren<Collider>(true)));
        int removed = 0; var flagged = new List<string>();
        foreach (var bc in UnityEngine.Object.FindObjectsOfType<BoxCollider>(true).ToList())
        {
            if (bc == null || bc.isTrigger || own.Contains(bc)) continue; var t = bc.transform; float best = float.MaxValue;
            for (int i = 0; i <= 6; i++) for (int j = 0; j <= 6; j++)
                {
                    var lp = bc.center + Vector3.Scale(bc.size, new Vector3(i / 6f - .5f, 0, j / 6f - .5f)); var w = t.TransformPoint(lp);
                    best = Mathf.Min(best, new Vector2(w.x - c0.x, w.z - c0.z).magnitude);
                }
            if (best >= 7.2f) continue;
            if (t.name == "Jump platform") { flagged.Add(PathOf(t) + " (removed)"); UnityEngine.Object.DestroyImmediate(bc.gameObject); removed++; }
            else flagged.Add(PathOf(t) + " (" + best.ToString("0.0") + " m from the pole, left as is)");
        }
        report.Add("stairs: " + removed + " jump platform(s) that reached over the stairs removed" + (flagged.Count > 0 ? ": " + string.Join("; ", flagged) : ""));
    }
    static string PathOf(Transform t) { var s = t.name; for (int i = 0; i < 5 && t.parent != null; i++) { t = t.parent; s = t.name + "/" + s; } return s; }

    // ================================================================== 17. cute clouds
    // Every cloud puff mesh becomes a cartoon cloud: a flat base with round bumps on top (a big one in the middle), same size and
    // footprint as before, so everything built from puffs keeps its place. The combined clouds (sea billows, banner booth, phone
    // booth clouds) are rebuilt from the new shape. Rollback: Astra > Claude > Restore ball clouds (puts the old meshes back).
    const string BackupCute = "Review/Backups/BeforeCuteClouds.unity.txt", MeshBackup = "Review/Backups/CloudMeshes/";
    [MenuItem("Astra/Claude/17 Cute clouds (flat-bottom cartoon puffs)")]
    public static void CuteClouds()
    {
        Begin(BackupCute);
        var plat = UnityEngine.Object.FindObjectsOfType<MeshCollider>(true).FirstOrDefault(c => c.name.StartsWith("Invisible platform"));
        FloorY = plat != null ? plat.bounds.max.y : 0f; SeaY = FloorY - 1.4f;
        Step("cartoon cloud shape for every puff mesh", () =>
        {
            Directory.CreateDirectory(MeshBackup); int n = 0; var skipped = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/Astra" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Where(x => x.name.StartsWith("Glitter Cloud")).ToList())
                {
                    if (!path.EndsWith(".asset")) { skipped.Add(path); continue; }
                    var bk = MeshBackup + Path.GetFileName(path); if (!File.Exists(bk)) File.Copy(path, bk);
                    var target = m.bounds; var cute = CuteCloudMesh(target, Math.Abs(m.name.GetHashCode()));
                    m.Clear(); m.SetVertices(cute.vertices); m.SetTriangles(cute.triangles, 0); m.SetNormals(cute.normals); m.RecalculateBounds(); m.RecalculateTangents();
                    EditorUtility.SetDirty(m); n++;
                }
            }
            AssetDatabase.SaveAssets();
            report.Add("cartoon cloud shape written into " + n + " cloud meshes (old ones kept in " + MeshBackup + ")" + (skipped.Count > 0 ? "; not editable: " + string.Join(", ", skipped) : ""));
        });
        var fly = MatFor("Pink Butterfly", "Astra/Butterfly");
        Step("rebuild the pink cloud sea", BuildSea);
        Step("rebuild the banner booth cloud", () => HeroBooth(fly));
        Step("rebuild the phone booth clouds", BoothClouds);
        Step("butterflies", () => Butterflies(fly));
        Step("keep the stairs clear", ClearStairs);
        End("claude-cute-report.txt");
    }
    [MenuItem("Astra/Claude/Restore ball clouds (undo 17)")]
    public static void RestoreBallClouds()
    {
        if (!Directory.Exists(MeshBackup) || !EditorUtility.DisplayDialog("Restore", "Put the old round cloud puffs back and the scene to before 17?", "Restore", "Cancel")) return;
        foreach (var bk in Directory.GetFiles(MeshBackup, "*.asset"))
        {
            var target = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(bk) + " t:Mesh", new[] { "Assets/Astra" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(pth => Path.GetFileName(pth) == Path.GetFileName(bk));
            if (target != null) { File.Copy(bk, target, true); AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate); }
        }
        RestoreFrom(BackupCute, "cute clouds");
    }
    // flat-bottomed cartoon cloud: union of round bumps sitting on one base line, soft valleys, gently rounded underside
    static Mesh CuteCloudMesh(Bounds target, int seed)
    {
        var rnd = new System.Random(seed); Func<float, float, float> J = (a, b) => a + (float)rnd.NextDouble() * (b - a);
        var bumps = new List<Vector3> {                                     // (x, z, radius)
            new Vector3(J(-.06f, .06f), J(-.05f, .05f), J(.95f, 1.05f)),
            new Vector3(J(-.86f, -.74f), J(-.12f, .12f), J(.66f, .74f)),
            new Vector3(J(.72f, .84f), J(-.12f, .12f), J(.68f, .76f)),
            new Vector3(J(-1.2f, -1.1f), J(-.1f, .1f), J(.4f, .46f)),
            new Vector3(J(1.1f, 1.2f), J(-.1f, .1f), J(.42f, .48f)),
            new Vector3(J(.1f, .35f), J(-.5f, -.38f), J(.52f, .6f)),
            new Vector3(J(-.35f, -.1f), J(.38f, .5f), J(.5f, .58f)) };
        Func<float, float> Rim = th =>
        {
            var d = new Vector2(Mathf.Cos(th), Mathf.Sin(th)); float best = 0;
            foreach (var b in bumps) { var c = new Vector2(b.x, b.y); float dc = Vector2.Dot(d, c), disc = dc * dc - c.sqrMagnitude + b.z * b.z; if (disc >= 0) best = Mathf.Max(best, dc + Mathf.Sqrt(disc)); }
            return best;
        };
        Func<Vector2, float> Top = q =>
        {
            float h1 = 0, h2 = 0;
            foreach (var b in bumps) { float d2 = (q - new Vector2(b.x, b.y)).sqrMagnitude, h = b.z * b.z > d2 ? Mathf.Sqrt(b.z * b.z - d2) : 0; if (h > h1) { h2 = h1; h1 = h; } else if (h > h2) h2 = h; }
            const float k = .16f; float gap = h1 - h2; return h1 + (gap < k ? (k - gap) * (k - gap) / (4 * k) : 0);   // soft valley between neighbouring bumps
        };
        const int A = 72, Jr = 14; const float thick = .12f; var v = new List<Vector3>(); var tri = new List<int>();
        int top0 = v.Count; v.Add(new Vector3(0, Top(Vector2.zero), 0));
        for (int j = 1; j <= Jr; j++) { float s = Mathf.Sin(Mathf.PI * .5f * j / Jr); for (int i = 0; i < A; i++) { float th = i * Mathf.PI * 2 / A; var q = new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * Rim(th) * s; v.Add(new Vector3(q.x, j == Jr ? 0 : Top(q), q.y)); } }
        int bot0 = v.Count; v.Add(new Vector3(0, -thick, 0));
        const int Jb = 4; for (int j = 1; j < Jb; j++) { float s = j / (float)Jb; for (int i = 0; i < A; i++) { float th = i * Mathf.PI * 2 / A; var q = new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * Rim(th) * s; v.Add(new Vector3(q.x, -thick * (1 - Mathf.Pow(s, 4)), q.y)); } }
        Func<int, int, int> T = (j, i) => j == 0 ? top0 : top0 + 1 + (j - 1) * A + (i % A);
        Func<int, int, int> B = (j, i) => j == 0 ? bot0 : j == Jb ? T(Jr, i) : bot0 + 1 + (j - 1) * A + (i % A);
        for (int i = 0; i < A; i++) { tri.AddRange(new[] { T(0, 0), T(1, i), T(1, i + 1) }); tri.AddRange(new[] { B(0, 0), B(1, i + 1), B(1, i) }); }
        for (int j = 1; j < Jr; j++) for (int i = 0; i < A; i++) tri.AddRange(new[] { T(j, i), T(j + 1, i), T(j + 1, i + 1), T(j, i), T(j + 1, i + 1), T(j, i + 1) });
        for (int j = 1; j < Jb; j++) for (int i = 0; i < A; i++) tri.AddRange(new[] { B(j, i), B(j + 1, i + 1), B(j + 1, i), B(j, i), B(j, i + 1), B(j + 1, i + 1) });
        // fit exactly into the old puff's box so every placement stays valid
        var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var hi = -lo; foreach (var p in v) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
        var size = hi - lo; var sc = new Vector3(target.size.x / size.x, target.size.y / size.y, target.size.z / size.z);
        for (int k = 0; k < v.Count; k++) v[k] = target.min + Vector3.Scale(v[k] - lo, sc);
        var m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals();
        if (m.normals[top0].y < 0) { for (int k = 0; k < tri.Count; k += 3) { int t0 = tri[k]; tri[k] = tri[k + 1]; tri[k + 1] = t0; } m.SetTriangles(tri, 0); m.RecalculateNormals(); }
        return m;
    }

    static void RestoreFrom(string backup, string what)
    {
        if (!File.Exists(backup) || !EditorUtility.DisplayDialog("Restore", "Put the scene back to before the " + what + "?", "Restore", "Cancel")) return;
        var path = EditorSceneManager.GetActiveScene().path; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        File.Copy(backup, path, true); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); EditorSceneManager.OpenScene(path);
    }
    [MenuItem("Astra/Claude/Restore scene from before polish")] public static void RestorePolish() { RestoreFrom(BackupPolish, "polish pass"); }
    [MenuItem("Astra/Claude/Restore scene from before banner backdrop")] public static void RestoreBanner() { RestoreFrom(BackupBanner, "banner backdrop"); }
}
