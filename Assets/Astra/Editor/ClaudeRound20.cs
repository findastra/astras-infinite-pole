// Claude, 2026-10-01. Astra > Claude > 20 Trees, duck cloud, booth + platform fixes.
//  1. window glass: the heart is gone (texture redrawn)
//  2. heart clouds: the sharp point and the top notch are rounded off
//  3. cloud platforms/floors/steps: colliders are thin slabs at the same standing height, so you can walk
//     through the cloud puffs instead of hitting an invisible wall
//  4. phone line: hub layout. Lower-floor booths hear the main-floor booth, never each other
//  5. the bathtub is gone; the rubber ducky now sits on its own puffy cloud
//  6. palm and cherry blossom trees (modelled in Blender by Claude, Assets/Astra/Models/trees.json)
//     on the duck cloud, the booth clouds and a scattering of orbit clouds
// Safe to rerun. Backup: Review/Backups/BeforeRound20.unity.txt
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ClaudeRound20
{
    const string Root = "Assets/Astra/";
    static System.Random rng; static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    static List<string> report; static Mesh cloudMesh; static Material white; static Transform lounge;

    [MenuItem("Astra/Claude/20 Trees, duck cloud, booth + platform fixes")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Review/Backups"); File.Copy(scene.path, "Review/Backups/BeforeRound20.unity.txt", true);
        rng = new System.Random(1001); report = new List<string>();
        lounge = GameObject.Find("13 - Cloud lounge (Claude)") != null ? GameObject.Find("13 - Cloud lounge (Claude)").transform : null;
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>();
        var anyCloud = spiral.GetComponentsInChildren<MeshRenderer>(true).First(r => r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Glitter Cloud"));
        cloudMesh = anyCloud.GetComponent<MeshFilter>().sharedMesh; white = anyCloud.sharedMaterial;

        Step("window glass without the heart", NoHeartGlass);
        Step("rounded heart clouds", RoundHearts);
        Step("thin platform colliders", ThinPlatforms);
        Step("phone line hub", PhoneHub);
        Step("bathtub out, duck on its own cloud", DuckCloud);
        Step("palm and cherry blossom trees", Trees);

        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-round20-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND20_OK\n" + string.Join("\n", report));
    }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("ERROR " + name + ": " + e.Message); Debug.LogException(e); } }

    // ---------------------------------------------------------------- 1. no heart on the glass
    static void NoHeartGlass()
    {
        const int W = 256, H = 512; var tex = new Texture2D(W, H, TextureFormat.RGBA32, true); var px = new Color[W * H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float u = x / (float)W, v = y / (float)H;
                var c = new Color(1f, .74f, .88f, .2f);
                float frost = Mathf.SmoothStep(.28f, .1f, v); c = Color.Lerp(c, new Color(1f, .9f, .96f, .62f), frost);
                float streak = Mathf.Max(0, 1 - Mathf.Abs(((u * .8f + v) % .55f) - .2f) * 22f) * .45f + Mathf.Max(0, 1 - Mathf.Abs(((u * .8f + v) % .55f) - .27f) * 60f) * .3f;
                c = Color.Lerp(c, new Color(1, 1, 1, .7f), streak * Mathf.SmoothStep(.1f, .4f, v));
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v)); c = Color.Lerp(new Color(1f, .55f, .82f, .75f), c, Mathf.SmoothStep(0, .04f, edge));
                uint hsh = (uint)(x * 73856093 ^ y * 19349663); if (hsh % 997 == 0) c = new Color(1, 1, 1, .9f);
                px[y * W + x] = c;
            }
        tex.SetPixels(px); tex.Apply();
        string path = Root + "Textures/Phone Booth Window.png"; File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path); if (ti != null) { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
        report.Add("booth glass: heart removed from the window texture (frost, streaks and sparkle kept)");
    }

    // ---------------------------------------------------------------- 2. rounded hearts
    static List<Vector2> heartPoly;
    static float RawHeart(float th)
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
            float s = (p.x * e.y - p.y * e.x) / den, u = (p.x * d.y - p.y * d.x) / den;
            if (s > 0 && u >= 0 && u <= 1) best = Mathf.Max(best, s);
        }
        return best;
    }
    static float[] softHeart;
    static float SoftHeart(float th)
    {
        const int N = 720;
        if (softHeart == null)
        {
            var raw = new float[N]; for (int i = 0; i < N; i++) raw[i] = RawHeart(i * Mathf.PI * 2 / N);
            // three circular blur passes over a +/- 11 degree window: the bottom cusp and the top notch become curves
            for (int pass = 0; pass < 3; pass++)
            {
                var sm = new float[N]; const int K = 22;
                for (int i = 0; i < N; i++)
                {
                    float s = 0, wsum = 0;
                    for (int k = -K; k <= K; k++) { float w = 1f - Mathf.Abs(k) / (float)(K + 1); s += raw[((i + k) % N + N) % N] * w; wsum += w; }
                    sm[i] = s / wsum;
                }
                raw = sm;
            }
            softHeart = raw;
        }
        float t2 = th / (Mathf.PI * 2) * N; int i0 = ((int)Mathf.Floor(t2) % N + N) % N; float f = t2 - Mathf.Floor(t2);
        return Mathf.Lerp(softHeart[i0], softHeart[(i0 + 1) % N], f);
    }
    static void RoundHearts()
    {
        string path = Root + "Meshes/Lounge/Heart cloud.asset";
        var target = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (target == null) { report.Add("heart clouds: no Heart cloud mesh in the project, nothing to round"); return; }
        float before = SharpnessOf(RawHeart), after = SharpnessOf(SoftHeart);
        var built = ShapeMesh("Heart cloud", SoftHeart);
        target.Clear(); target.SetVertices(built.vertices.ToList()); target.SetTriangles(built.triangles, 0);
        target.RecalculateNormals(); target.RecalculateBounds(); EditorUtility.SetDirty(target);
        UnityEngine.Object.DestroyImmediate(built);
        int used = Resources.FindObjectsOfTypeAll<MeshFilter>().Count(m => m.gameObject.scene.IsValid() && m.sharedMesh == target);
        report.Add("heart clouds: sharpest corner softened from " + before.ToString("0") + " to " + after.ToString("0") + " degrees of turn per step; " + used + " heart cloud(s) updated");
    }
    // biggest direction change between neighbouring outline points, in degrees: a cusp scores high, a curve low
    static float SharpnessOf(Func<float, float> radius)
    {
        const int N = 360; var pts = new Vector2[N];
        for (int i = 0; i < N; i++) { float a = i * Mathf.PI * 2 / N, r = radius(a); pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r); }
        float worst = 0;
        for (int i = 0; i < N; i++)
        {
            var a1 = pts[(i + 1) % N] - pts[i]; var a2 = pts[(i + 2) % N] - pts[(i + 1) % N];
            if (a1.sqrMagnitude < 1e-9f || a2.sqrMagnitude < 1e-9f) continue;
            worst = Mathf.Max(worst, Vector2.Angle(a1, a2));
        }
        return worst;
    }
    static Mesh ShapeMesh(string name, Func<float, float> radius)
    {
        const int A = 144, J = 9; const float Ht = .27f, Hb = .2f; var v = new List<Vector3>(); var tri = new List<int>();
        Func<float, float, float, Vector3> P = (th, sv, hsign) =>
        {
            float r = radius(th) * sv, e = Mathf.Sqrt(Mathf.Max(0, 1 - sv * sv));
            float h = hsign > 0 ? Ht * Mathf.Pow(e, .75f) : -Hb * Mathf.Pow(e, .7f);
            return new Vector3(r * Mathf.Cos(th), h, -r * Mathf.Sin(th));
        };
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
        m.RecalculateBounds(); return m;
    }

    // ---------------------------------------------------------------- 3. thin platform colliders
    static void ThinPlatforms()
    {
        const float Slab = .06f;
        string[] words = { "platform", "floor", "step" };
        int thinned = 0, shrunk = 0; float biggest = 0;
        foreach (var bc in Resources.FindObjectsOfTypeAll<BoxCollider>())
        {
            if (!bc.gameObject.scene.IsValid() || bc.isTrigger) continue;
            string n = bc.name.ToLowerInvariant();
            bool isPlatform = words.Any(w => n.Contains(w));
            if (!isPlatform) continue;
            if (n.Contains("invisible platform")) continue;             // the main walkway stays as it is
            var s = bc.size; var c = bc.center;
            if (n.Contains("jump platform"))
            {
                // it also reached 1.5 m past the cloud on every side, which is where the invisible ledges came from
                var ls = bc.transform.lossyScale;
                float overhangX = 3f / Mathf.Max(.0001f, Mathf.Abs(ls.x)), overhangZ = 3f / Mathf.Max(.0001f, Mathf.Abs(ls.z));
                if (s.x > overhangX && s.z > overhangZ) { s.x -= overhangX * .66f; s.z -= overhangZ * .66f; shrunk++; }
            }
            if (s.y > Slab)
            {
                biggest = Mathf.Max(biggest, s.y * Mathf.Abs(bc.transform.lossyScale.y));
                float top = c.y + s.y / 2;                                // keep the surface you stand on exactly where it is
                s.y = Slab / Mathf.Max(.0001f, Mathf.Abs(bc.transform.lossyScale.y));
                c.y = top - s.y / 2; thinned++;
            }
            bc.size = s; bc.center = c; EditorUtility.SetDirty(bc);
        }
        report.Add("platform colliders: " + thinned + " thinned to a " + Slab + " m slab (the thickest was " + biggest.ToString("0.00") + " m), standing heights unchanged; " + shrunk + " jump platform(s) pulled back to the cloud's own edge. Seats, tables and sofa backs were left alone.");
    }

    // ---------------------------------------------------------------- 4. phone line hub
    static void PhoneHub()
    {
        var line = UnityEngine.Object.FindObjectOfType<AstraPhoneLine>(); if (line == null) { report.Add("phone line: not found"); return; }
        var booths = UnityEngine.Object.FindObjectsOfType<AstraPhoneBooth>(true);
        if (booths.Length == 0) { report.Add("phone line: no booths"); return; }
        // the main floor booth is the one nearest the walkway floor (the banner booth at spawn level)
        var plat = UnityEngine.Object.FindObjectsOfType<MeshCollider>(true).FirstOrDefault(c => c.name.StartsWith("Invisible platform"));
        float floorY = plat != null ? plat.bounds.max.y : 0f;
        var main = booths.OrderBy(b => Mathf.Abs(b.transform.position.y - floorY)).First();
        line.hub = main.booth; UdonSharpEditorUtility.CopyProxyToUdon(line);
        var others = booths.Where(b => b.booth != main.booth).Select(b => Name(b) + " #" + b.booth);
        report.Add("phone line: hub is " + Name(main) + " #" + main.booth + " on the main floor (y " + main.transform.position.y.ToString("0.0") + "). It hears every other booth and they hear it; the lower booths (" + string.Join(", ", others) + ") no longer hear each other.");
    }
    static string Name(AstraPhoneBooth b) { var t = b.transform; while (t != null && !t.name.StartsWith("Phone booth") && !t.name.StartsWith("Banner booth")) t = t.parent; return t != null ? t.name : b.name; }

    // ---------------------------------------------------------------- 5. bath out, duck on a cloud
    static void DuckCloud()
    {
        var bath = lounge != null ? lounge.Find("Bathtub cloud") : null;
        var duck = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.gameObject.scene.IsValid() && t.name == "Rubber ducky");
        var old = lounge != null ? lounge.Find("Duck cloud") : null;
        Vector3 at = bath != null ? bath.position : (old != null ? old.position : Vector3.zero);
        if (duck == null) { report.Add("duck: not found, nothing moved"); return; }
        if (old != null) { duck.SetParent(lounge, true); UnityEngine.Object.DestroyImmediate(old.gameObject); }

        var g = new GameObject("Duck cloud").transform; g.SetParent(lounge, true); g.position = at; g.rotation = Quaternion.Euler(0, Mathf.Atan2(-at.x, -at.z) * Mathf.Rad2Deg, 0);
        // a puffy cloud built the same way as the phone booth clouds: many puffs baked into one mesh
        float hw = cloudMesh.bounds.size.y / cloudMesh.bounds.size.x;
        var ms = new List<Matrix4x4>();
        Action<float, float, float, float, int> ring = (rr, w, top, jitter, cnt) =>
        {
            for (int i = 0; i < cnt; i++)
            {
                float a = (i + R(-.25f, .25f)) * Mathf.PI * 2 / cnt, ww = w * R(.85f, 1.15f);
                var c = at + new Vector3(Mathf.Cos(a) * rr * R(.95f, 1.05f), 0, Mathf.Sin(a) * rr * R(.95f, 1.05f));
                c.y = at.y + top + R(-jitter, jitter) - ww * hw * .5f; ms.Add(PuffAt(c, ww));
            }
        };
        ring(0f, 2.6f, -.15f, .02f, 1);
        ring(1.5f, 1.9f, -.05f, .06f, 7);
        ring(2.9f, 1.7f, -.2f, .08f, 11);
        ring(4.0f, 1.5f, -.55f, .1f, 13);
        ring(2.2f, 1.3f, -1.5f, .12f, 9);
        ring(1.0f, 1.2f, -2.3f, .12f, 5);
        CombinedPuffs(g, "Duck cloud puffs", ms, white, "Duck Cloud Puffs");
        var floor = new GameObject("Cloud duck platform collider"); floor.transform.SetParent(g, false);
        floor.transform.localPosition = new Vector3(0, .02f, 0); floor.AddComponent<BoxCollider>().size = new Vector3(5.2f, .06f, 5.2f);

        duck.SetParent(g, true);
        float wide = Width(duck); if (wide > .01f) duck.localScale *= .62f / wide;     // a hand-sized ducky
        duck.localPosition = new Vector3(.5f, .06f, .2f); duck.localRotation = Quaternion.Euler(0, 205, 0);
        var fl = duck.GetComponent<AstraDuckFloat>(); if (fl != null) { fl.bob = .035f; fl.rx = 2.2f; fl.rz = 2.0f; fl.speed = .10f; UdonSharpEditorUtility.CopyProxyToUdon(fl); }

        int gone = 0;
        if (bath != null) { gone = bath.GetComponentsInChildren<Transform>(true).Length; UnityEngine.Object.DestroyImmediate(bath.gameObject); }
        report.Add("bathtub cloud deleted (" + gone + " objects). New Duck cloud at " + at.ToString("0.0") + ": " + ms.Count + " puffs in one mesh, a 0.06 m floor you stand on, and the rubber ducky (" + .62f.ToString("0.00") + " m wide) sitting on top.");
    }
    static float Width(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (rs.Length == 0) return 0; var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return Mathf.Max(b.size.x, b.size.z);
    }
    static Matrix4x4 PuffAt(Vector3 centre, float w)
    {
        var mb = cloudMesh.bounds; float k = w / mb.size.x; var rot = Quaternion.Euler(0, R(0, 360), 0);
        return Matrix4x4.TRS(centre - rot * (mb.center * k), rot, Vector3.one * k);
    }
    static void CombinedPuffs(Transform parent, string name, List<Matrix4x4> worldMatrices, Material m, string assetName)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        var toLocal = go.transform.worldToLocalMatrix;
        var mesh = new Mesh { name = assetName, indexFormat = IndexFormat.UInt32 };
        mesh.CombineMeshes(worldMatrices.Select(w => new CombineInstance { mesh = cloudMesh, transform = toLocal * w }).ToArray(), true, true); mesh.RecalculateBounds();
        string path = Root + "Meshes/Lounge/" + assetName + ".asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; Quiet(r);
    }
    static void Quiet(Renderer r) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off; }

    // ---------------------------------------------------------------- 6. trees
    [Serializable] class Sub { public string material; public int[] triangles; }
    [Serializable] class Grp { public string name; public float[] pivot; public float[] vertices; public float[] normals; public Sub[] submeshes; }
    [Serializable] class Model { public Grp[] groups; }

    static void Trees()
    {
        string jsonPath = Root + "Models/trees.json";
        if (!File.Exists(jsonPath)) { report.Add("trees: " + jsonPath + " is missing"); return; }
        var model = JsonUtility.FromJson<Model>(File.ReadAllText(jsonPath));
        var mats = TreeMaterials();
        var meshes = new Dictionary<string, Mesh>(); var subOrder = new Dictionary<string, string[]>();
        foreach (var g in model.groups)
        {
            int n = g.vertices.Length / 3; var v = new Vector3[n]; var nr = new Vector3[n];
            for (int i = 0; i < n; i++) { v[i] = new Vector3(g.vertices[i * 3], g.vertices[i * 3 + 1], g.vertices[i * 3 + 2]); nr[i] = new Vector3(g.normals[i * 3], g.normals[i * 3 + 1], g.normals[i * 3 + 2]); }
            string mp = Root + "Meshes/Trees/" + g.name + ".asset";
            Directory.CreateDirectory(Root + "Meshes/Trees");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            bool fresh = mesh == null; if (fresh) mesh = new Mesh();
            mesh.Clear(); mesh.name = g.name + " tree"; mesh.indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = v; mesh.normals = nr; mesh.subMeshCount = g.submeshes.Length;
            for (int s = 0; s < g.submeshes.Length; s++) mesh.SetTriangles(g.submeshes[s].triangles, s);
            mesh.RecalculateBounds();
            if (fresh) AssetDatabase.CreateAsset(mesh, mp); else EditorUtility.SetDirty(mesh);
            meshes[g.name] = mesh; subOrder[g.name] = g.submeshes.Select(s => s.material).ToArray();
        }

        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid() && t.name == "Cloud tree").ToList())
            if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);

        int planted = 0; var where = new List<string>();
        Action<Transform, string, Vector3, float, float> plant = (parent, kind, localPos, scale, yaw) =>
        {
            var go = new GameObject("Cloud tree"); go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var ls = parent.lossyScale; go.transform.localScale = new Vector3(scale / Mathf.Max(.0001f, Mathf.Abs(ls.x)), scale / Mathf.Max(.0001f, Mathf.Abs(ls.y)), scale / Mathf.Max(.0001f, Mathf.Abs(ls.z)));
            go.AddComponent<MeshFilter>().sharedMesh = meshes[kind];
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = subOrder[kind].Select(m => mats[m]).ToArray(); Quiet(r);
            planted++;
        };

        // the duck cloud gets a proper little island: one palm and one cherry
        var duckCloud = lounge != null ? lounge.Find("Duck cloud") : null;
        if (duckCloud != null)
        {
            plant(duckCloud, "Palm", new Vector3(-1.7f, 0f, -1.1f), .38f, R(0, 360));
            plant(duckCloud, "Cherry", new Vector3(1.6f, 0f, -1.6f), .34f, R(0, 360));
            where.Add("duck cloud: 1 palm + 1 cherry");
        }
        // one cherry beside each phone booth
        int booths = 0;
        foreach (var b in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid() && t.name.StartsWith("Phone booth ") && t.Find("Booth body") != null))
        { plant(b, "Cherry", new Vector3(R(-1.9f, -1.3f), -.02f, R(-.6f, .6f)), .16f, R(0, 360)); booths++; }
        if (booths > 0) where.Add(booths + " booth cloud(s): 1 cherry each");
        // a scattering on the orbit clouds, skipping hearts, stars and booth clouds, and skipping anything near the stairs
        var orbit = UnityEngine.Object.FindObjectOfType<AstraCloudOrbit>();
        int orbits = 0;
        if (orbit != null && orbit.clouds != null)
            for (int j = 0; j < orbit.clouds.Length; j++)
            {
                if (j % 6 != 3) continue;
                var c = orbit.clouds[j]; if (c == null) continue;
                var mf = c.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                if (c.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Phone booth") || t.name == "Heart cloud" || t.name == "Star cloud" || t.name == "Cloud tree")) continue;
                var mb = mf.sharedMesh.bounds; var ls = c.lossyScale;
                float wide = Mathf.Min(Mathf.Abs(ls.x) * mb.size.x, Mathf.Abs(ls.z) * mb.size.z);
                if (wide < 1.6f) continue;                                       // too small to hold a tree
                float top = mb.center.y + mb.extents.y * .55f;
                plant(c, orbits % 2 == 0 ? "Palm" : "Cherry", new Vector3(mb.center.x, top, mb.center.z), Mathf.Clamp(wide * .22f, .22f, .5f), R(0, 360));
                orbits++;
            }
        if (orbits > 0) where.Add(orbits + " orbit cloud(s)");
        report.Add("trees: " + planted + " planted (" + string.Join("; ", where) + "). Palm " + meshes["Palm"].triangles.Length / 3 + " triangles, cherry " + meshes["Cherry"].triangles.Length / 3 + ", two shared meshes. No colliders, so you walk straight through them.");
    }

    static Dictionary<string, Material> TreeMaterials()
    {
        Directory.CreateDirectory(Root + "Materials/Trees");
        Func<string, Color, Color, Color, float, Material> make = (name, top, bottom, rim, glit) =>
        {
            string p = Root + "Materials/Trees/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Astra/Glitter Cloud")); AssetDatabase.CreateAsset(m, p); }
            m.shader = Shader.Find("Astra/Glitter Cloud"); m.enableInstancing = true;
            m.SetColor("_Top", top); m.SetColor("_Bottom", bottom); m.SetColor("_Rim", rim);
            m.SetFloat("_Glitter", glit); m.SetFloat("_Pastel", 0f); EditorUtility.SetDirty(m); return m;
        };
        return new Dictionary<string, Material> {
            { "Bark",      make("Tree Bark", new Color(.62f, .40f, .44f), new Color(.30f, .18f, .22f), new Color(1.1f, .75f, .85f), .5f) },
            { "Frond",     make("Palm Frond", new Color(.52f, .96f, .68f), new Color(.20f, .54f, .36f), new Color(.9f, 1.5f, 1.1f), .9f) },
            { "Coconut",   make("Coconut", new Color(.42f, .28f, .22f), new Color(.18f, .11f, .09f), new Color(.9f, .7f, .6f), .4f) },
            { "Blossom",   make("Cherry Blossom", new Color(1.25f, .72f, .92f), new Color(.95f, .42f, .70f), new Color(1.9f, 1.1f, 1.6f), 1.6f) },
            { "BlossomHi", make("Cherry Blossom Light", new Color(1.35f, 1.05f, 1.15f), new Color(1.05f, .70f, .88f), new Color(1.9f, 1.4f, 1.8f), 1.8f) } };
    }
}
