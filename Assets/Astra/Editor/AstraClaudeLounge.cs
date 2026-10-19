// Claude cloud lounge pass (2026-09-26). Menu: Astra > Claude > 3 Cloud lounge upgrade.
// Rollback: Astra > Claude > Restore scene from before cloud lounge (all new meshes/materials are new assets,
// so restoring the scene file reverts everything). Outer clouds alone: Astra > Claude > Remove outer big clouds.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraClaudeLounge
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Backup = "Review/Backups/BeforeCloudLounge.unity.txt";
    const string Root = "Assets/Astra/";
    const string LoungeName = "13 - Cloud lounge (Claude)";
    const string OuterName = "14 - Outer big clouds (Claude)";
    const float FloorR = 46f;
    static System.Random rng;
    static List<string> report;
    static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    // ---------------------------------------------------------------- menu
    [MenuItem("Astra/Claude/3 Cloud lounge upgrade")]
    public static void Run()
    {
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene(Scene);
        if (GameObject.Find(LoungeName) != null) { EditorUtility.DisplayDialog("Cloud lounge", "Already applied. Use Restore first to rebuild.", "OK"); return; }
        Directory.CreateDirectory("Review/Backups");
        File.Copy(Scene, Backup, true);
        Directory.CreateDirectory(Root + "Meshes/Lounge"); Directory.CreateDirectory(Root + "Materials/Lounge"); Directory.CreateDirectory(Root + "Textures");
        rng = new System.Random(20260926); report = new List<string>();
        placed = new List<Vector4>();
        Step("materials", Materials);
        Step("stairs +50%", WidenStairs);
        Step("DJ cloud 1/3", ShrinkDJ);
        Step("video screen 4x", MoveScreen);
        Step("phone booths", Booths);
        Step("stair sparkle flow", StairFlow);
        lounge = new GameObject(LoungeName).transform;
        Step("furniture", Furniture);
        Step("bathtub", Bathtub);
        Step("amenities", Amenities);
        Step("crystal vines", CrystalVines);
        Step("butterflies", Butterflies);
        Step("outer clouds", OuterClouds);
        Step("switches", Switches);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Step("validation", Validate);
        File.WriteAllText("Review/claude-lounge-report.txt", string.Join("\n", report));
        Debug.Log("ASTRA_LOUNGE_DONE\n" + string.Join("\n", report));
        EditorUtility.DisplayDialog("Cloud lounge", string.Join("\n", report.Where(l => l.Contains("FAIL") || l.StartsWith("ERROR") || l.StartsWith("==")).ToArray()) + "\n\nFull report: Review/claude-lounge-report.txt", "OK");
    }

    static void Step(string name, Action a)
    {
        try { a(); report.Add("== " + name + ": ok"); }
        catch (Exception e) { report.Add("ERROR " + name + ": " + e.Message); Debug.LogException(e); }
    }

    [MenuItem("Astra/Claude/Restore scene from before cloud lounge")]
    public static void Restore()
    {
        if (!File.Exists(Backup)) { EditorUtility.DisplayDialog("Restore", "No backup found.", "OK"); return; }
        if (!EditorUtility.DisplayDialog("Restore", "Replace the scene with the copy saved before the cloud lounge pass?", "Restore", "Cancel")) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        File.Copy(Backup, Scene, true); AssetDatabase.ImportAsset(Scene, ImportAssetOptions.ForceUpdate);
        EditorSceneManager.OpenScene(Scene); Debug.Log("ASTRA_LOUNGE_RESTORED");
    }

    [MenuItem("Astra/Claude/Remove outer big clouds")]
    public static void RemoveOuter()
    {
        var scene = EditorSceneManager.OpenScene(Scene);
        var o = GameObject.Find(OuterName); if (o != null) UnityEngine.Object.DestroyImmediate(o);
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene == scene && t.name == "OUTER CLOUDS").ToList()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene == scene && t.name == "Outer clouds switch").ToList()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Debug.Log("ASTRA_OUTER_REMOVED");
    }

    // ---------------------------------------------------------------- materials
    static Mesh cloudMesh, sphere, cylinder, octa;
    static Material cloudMat, pinkMat, lilacMat, peachMat, glowMat, duckMat, beakMat, darkMat, cupMat, waterMat, starMat, sparkMat, windowMat;
    static Material[] crystalMats;
    static Transform lounge;

    static Material Mat(string name, Material src, Action<Material> set)
    {
        string path = Root + "Materials/Lounge/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); } else m.CopyPropertiesFromMaterial(src);
        m.enableInstancing = true; set(m); EditorUtility.SetDirty(m); return m;
    }
    static void Cols(Material m, Color top, Color bottom, Color rim, float glitter)
    {
        m.SetColor("_Top", top); m.SetColor("_Bottom", bottom); m.SetColor("_Rim", rim); m.SetFloat("_Glitter", glitter); if (m.HasProperty("_Pastel")) m.SetFloat("_Pastel", 0);
    }

    static void Materials()
    {
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>();
        var anyCloud = spiral.GetComponentsInChildren<MeshRenderer>(true).First(r => r.name.StartsWith("Cloud "));
        cloudMesh = anyCloud.GetComponent<MeshFilter>().sharedMesh; cloudMat = anyCloud.sharedMaterial;
        sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx"); cylinder = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
        pinkMat = Mat("Lounge Pink", cloudMat, m => Cols(m, new Color(1.15f, .62f, .86f), new Color(.8f, .36f, .62f), new Color(1.5f, .9f, 1.3f), .9f));
        lilacMat = Mat("Lounge Lilac", cloudMat, m => Cols(m, new Color(.86f, .7f, 1.15f), new Color(.52f, .4f, .86f), new Color(1.2f, 1f, 1.6f), .9f));
        peachMat = Mat("Lounge Peach", cloudMat, m => Cols(m, new Color(1.2f, .82f, .7f), new Color(.9f, .5f, .5f), new Color(1.5f, 1.1f, 1f), .8f));
        glowMat = Mat("Lounge Glow", cloudMat, m => Cols(m, new Color(2.6f, 1.7f, 2.3f), new Color(2.2f, 1.1f, 2f), new Color(2.8f, 2f, 2.6f), .4f));
        duckMat = Mat("Lounge Duck", cloudMat, m => Cols(m, new Color(1.35f, 1.12f, .18f), new Color(1f, .7f, .05f), new Color(1.5f, 1.3f, .5f), .15f));
        beakMat = Mat("Lounge Beak", cloudMat, m => Cols(m, new Color(1.4f, .5f, .1f), new Color(1f, .3f, .05f), new Color(1.5f, .7f, .3f), .1f));
        darkMat = Mat("Lounge Dark", cloudMat, m => Cols(m, new Color(.12f, .05f, .12f), new Color(.05f, .02f, .06f), new Color(.35f, .15f, .3f), .2f));
        cupMat = Mat("Lounge Porcelain", cloudMat, m => Cols(m, new Color(1.2f, 1.1f, 1.15f), new Color(.95f, .8f, .9f), new Color(1.4f, 1.2f, 1.3f), .3f));
        var glass = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Phone Booth Glass.mat");
        if (glass == null) glass = new Material(Shader.Find("Astra/Prismatic Stair"));
        Action<Material, Color, float> crys = (m, c, o) => { if (m.HasProperty("_Color")) m.SetColor("_Color", c); if (m.HasProperty("_Opacity")) m.SetFloat("_Opacity", o); if (m.HasProperty("_Glitter")) m.SetFloat("_Glitter", 1f); if (m.HasProperty("_RainbowRate")) m.SetFloat("_RainbowRate", .12f); if (m.HasProperty("_Rainbow")) m.SetFloat("_Rainbow", .35f); };
        crystalMats = new[] {
            Mat("Crystal Rose", glass, m => crys(m, new Color(1f, .5f, .8f), .62f)),
            Mat("Crystal Lilac", glass, m => crys(m, new Color(.72f, .55f, 1f), .62f)),
            Mat("Crystal Aqua", glass, m => crys(m, new Color(.45f, .95f, 1f), .58f)) };
        waterMat = Mat("Bath Water", glass, m => crys(m, new Color(.62f, .82f, 1f), .5f));
        var dust = GameObject.Find("10 - Stair opening star dust");
        starMat = dust != null ? dust.GetComponent<ParticleSystemRenderer>().sharedMaterial : null;
        var gentle = UnityEngine.Object.FindObjectsOfType<ParticleSystemRenderer>(true).FirstOrDefault(r => r.sharedMaterial != null && r.sharedMaterial.name.Contains("Lilac Sparkles"));
        sparkMat = gentle != null ? gentle.sharedMaterial : starMat;
        octa = OctaMesh();
        report.Add("cloud mesh " + cloudMesh.name + " bounds " + cloudMesh.bounds.size + ", crystal shader " + crystalMats[0].shader.name + ", star mat " + (starMat ? starMat.name : "none"));
    }

    static Mesh SaveMesh(Mesh m, string name)
    {
        string path = Root + "Meshes/Lounge/" + name + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (old != null) AssetDatabase.DeleteAsset(path);
        m.name = name; AssetDatabase.CreateAsset(m, path); return m;
    }

    static Mesh OctaMesh()
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
        Vector3[] eq = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
        foreach (var pole in new[] { Vector3.up * 1.6f, Vector3.down * 1.6f })
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = eq[i] * .55f, b = eq[(i + 1) % 4] * .55f;
                var nn = Vector3.Cross(a - pole, b - pole); if (Vector3.Dot(nn, pole + a + b) < 0) { var tmp = a; a = b; b = tmp; nn = -nn; }
                nn.Normalize(); int k = v.Count; v.Add(pole); v.Add(a); v.Add(b); n.Add(nn); n.Add(nn); n.Add(nn); t.AddRange(new[] { k, k + 1, k + 2 });
            }
        var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
        return SaveMesh(m, "Crystal Gem");
    }

    // ---------------------------------------------------------------- geometry helpers
    static GameObject Puff(Transform parent, string name, Vector3 pos, Vector3 size, Material m, float yaw = 0, bool solid = false)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var b = cloudMesh.bounds.size; go.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, size.z / b.z);
        go.AddComponent<MeshFilter>().sharedMesh = cloudMesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; Quiet(r);
        if (solid) { var c = go.AddComponent<BoxCollider>(); c.center = cloudMesh.bounds.center; c.size = new Vector3(b.x * .9f, b.y * .8f, b.z * .9f); }
        return go;
    }
    static GameObject Prim(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default(Vector3))
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(euler); go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; Quiet(r); return go;
    }
    static void Quiet(Renderer r) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off; }
    static void Solid(Transform parent, Vector3 center, Vector3 size, string name = "Cloud seat collider")
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = center;
        go.AddComponent<BoxCollider>().size = size;
    }

    // ---------------------------------------------------------------- placement
    static List<Vector4> placed; // xyz + radius
    static Vector3 djCenter; static float djR = 9f;
    static Transform screenT; static Vector3 screenC, screenHalf; static Quaternion screenRot;
    static bool Blocked(Vector3 c, float rad, float minR = 10f)
    {
        float r = new Vector2(c.x, c.z).magnitude;
        if (r < minR + rad || r > FloorR - rad * .6f) return true;
        if (Vector2.Distance(new Vector2(c.x, c.z), new Vector2(djCenter.x, djCenter.z)) < djR + 3 + rad) return true;
        if (screenT != null) { var d = Quaternion.Inverse(screenRot) * (c - screenC); if (Mathf.Abs(d.x) < screenHalf.x + rad + 2 && Mathf.Abs(d.y) < screenHalf.y + rad + 2 && Mathf.Abs(d.z) < 6 + rad) return true; }
        foreach (var p in placed) if (Vector3.Distance(c, p) < (rad + p.w) * .92f) return true;
        return false;
    }
    static bool Place(ref Vector3 c, float rad, float minR = 10f)
    {
        var start = c;
        for (int i = 0; i < 120; i++)
        {
            if (!Blocked(c, rad, minR)) { placed.Add(new Vector4(c.x, c.y, c.z, rad)); return true; }
            float a = R(0, Mathf.PI * 2), d = 1.5f + i * .35f; c = start + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
        }
        report.Add("placement FAIL near " + start); return false;
    }
    static Vector3 Polar(float deg, float r, float y) { float a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
    static float FaceCenter(Vector3 p) { return Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg; }
    static Transform Group(Transform parent, string name, Vector3 pos, float yaw)
    {
        var g = new GameObject(name).transform; g.SetParent(parent, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0, yaw, 0); return g;
    }

    // ---------------------------------------------------------------- 1. stairs 50% further from the pole
    static float stairIn, stairOut, stairDelta;
    static float MapR(float r)
    {
        if (r <= stairIn) return r * (stairIn + stairDelta) / stairIn;
        if (r <= stairOut) return r + stairDelta;
        if (r < 48f) return stairOut + stairDelta + (r - stairOut) * (48f - stairOut - stairDelta) / (48f - stairOut);
        return r;
    }
    static Mesh Widen(Mesh src, string name)
    {
        var m = UnityEngine.Object.Instantiate(src); var v = m.vertices;
        for (int i = 0; i < v.Length; i++) { float r = new Vector2(v[i].x, v[i].z).magnitude; if (r < 1e-4f) continue; float k = MapR(r) / r; v[i].x *= k; v[i].z *= k; }
        m.vertices = v; m.RecalculateBounds(); return SaveMesh(m, name);
    }
    static void WidenStairs()
    {
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>();
        var t0 = spiral.turns[0]; var ramp = t0.GetComponent<MeshCollider>().sharedMesh; var steps = t0.GetComponent<MeshFilter>().sharedMesh;
        var radii = ramp.vertices.Select(v => new Vector2(v.x, v.z).magnitude).ToArray();
        stairIn = radii.Min(); stairOut = radii.Max(); stairDelta = stairIn * .5f;
        if (stairIn < .5f) throw new Exception("stair inner radius looks wrong: " + stairIn);
        var wideRamp = Widen(ramp, "Spiral Walkable Ramp Wide"); var wideSteps = Widen(steps, "Spiral Steps Wide");
        foreach (var t in spiral.turns) { t.GetComponent<MeshFilter>().sharedMesh = wideSteps; t.GetComponent<MeshCollider>().sharedMesh = wideRamp; }
        report.Add("stairs: inner " + stairIn.ToString("0.00") + " -> " + (stairIn + stairDelta).ToString("0.00") + " m, outer " + stairOut.ToString("0.00") + " -> " + (stairOut + stairDelta).ToString("0.00") + " m");
        // platform opening follows the stairs
        var plat = UnityEngine.Object.FindObjectsOfType<MeshCollider>(true).FirstOrDefault(c => c.name.StartsWith("Invisible platform"));
        if (plat != null) { plat.sharedMesh = Widen(plat.sharedMesh, "Invisible platform wide opening"); report.Add("platform opening widened (" + plat.name + ")"); }
        else report.Add("platform FAIL: not found");
        // orbiting clouds keep their clearance from the wider stairs
        var orbit = UnityEngine.Object.FindObjectOfType<AstraCloudOrbit>(); int moved = 0;
        if (orbit != null) foreach (var c in orbit.clouds) { if (c == null) continue; var p = c.localPosition; float r = new Vector2(p.x, p.z).magnitude; if (r < 1e-3f) continue; float k = MapR(r) / r; c.localPosition = new Vector3(p.x * k, p.y, p.z * k); moved++; }
        report.Add("orbit clouds pushed out with the stairs: " + moved);
        var dust = GameObject.Find("10 - Stair opening star dust");
        if (dust != null) { var sh = dust.GetComponent<ParticleSystem>().shape; sh.radius = MapR(sh.radius); var s = sh.scale; float k = (stairOut + stairDelta) / stairOut; sh.scale = new Vector3(s.x * k, s.y, s.z * k); }
    }

    // ---------------------------------------------------------------- 2. DJ cloud at a third of the size, lights on its floor
    static void ShrinkDJ()
    {
        var dj = GameObject.Find("12 - DJ cloud").transform; djCenter = dj.position;
        var stage = dj.Find("DJ stage cloud"); var s = stage.localScale; stage.localScale = new Vector3(s.x / 3f, s.y, s.z / 3f);
        foreach (var ps in stage.GetComponentsInChildren<ParticleSystem>(true)) { var sh = ps.shape; var sc = sh.scale; sh.scale = new Vector3(sc.x / 3f, sc.y, sc.z / 3f); sh.radius /= 3f; }
        Physics.SyncTransforms();
        var bounds = stage.GetComponent<Renderer>().bounds; float rad = Mathf.Max(bounds.extents.x, bounds.extents.z) * .82f; djR = rad;
        var col = stage.GetComponent<Collider>();
        var step = dj.Find("DJ step cloud");
        if (step != null) { var p = step.localPosition; var dir = new Vector3(p.x, 0, p.z).normalized; if (dir == Vector3.zero) dir = Vector3.forward; step.position = stage.position + dir * (rad + .7f) + Vector3.up * (step.position.y - stage.position.y); }
        var lights = dj.Find("Stage lights");
        if (lights != null) foreach (Transform tower in lights)
            {
                var p = tower.position - stage.position; var flat = new Vector3(p.x, 0, p.z); float k = Mathf.Min(1f, (rad - 1.2f) / Mathf.Max(.01f, flat.magnitude));
                var target = stage.position + flat * k; RaycastHit hit;
                float y = col != null && col.Raycast(new Ray(target + Vector3.up * 20, Vector3.down), out hit, 40) ? hit.point.y : bounds.max.y;
                tower.position = new Vector3(target.x, y, target.z);
                var truss = tower.Find("Truss"); if (truss != null) truss.gameObject.SetActive(false);
                var head = tower.Find("Moving head"); if (head != null) head.localPosition = new Vector3(head.localPosition.x, .32f, head.localPosition.z);
            }
        var orbit = UnityEngine.Object.FindObjectOfType<AstraCloudOrbit>();
        if (orbit != null && orbit.clearZones != null) for (int i = 0; i < orbit.clearZones.Length; i++) if (orbit.clearZones[i] != null && orbit.clearZones[i].name.Contains("DJ")) { var h = orbit.clearHalf[i]; orbit.clearHalf[i] = new Vector3(Mathf.Max(rad + 2, h.x / 3f), h.y, Mathf.Max(rad + 2, h.z / 3f)); }
        if (orbit != null) UdonSharpEditorUtility.CopyProxyToUdon(orbit);
        report.Add("DJ stage cloud now " + (bounds.size.x).ToString("0.0") + " m wide; stage lights moved onto the cloud floor");
    }

    // ---------------------------------------------------------------- 3. video screen 4x, outside every cloud path
    static void MoveScreen()
    {
        var vs = UnityEngine.Object.FindObjectsOfType<Renderer>(true).First(r => r.name == "VideoScreen");
        var cinema = vs.transform.parent != null && vs.transform.parent.name.StartsWith("Cinema") ? vs.transform.parent : vs.transform;
        cinema.localScale *= 4f; Physics.SyncTransforms();
        var orbit = UnityEngine.Object.FindObjectOfType<AstraCloudOrbit>();
        float maxR = 0;
        if (orbit != null) foreach (var c in orbit.clouds) { if (c == null) continue; var rr = c.GetComponent<Renderer>(); float ext = rr != null ? new Vector2(rr.bounds.extents.x, rr.bounds.extents.z).magnitude : 3f; float r = new Vector2(c.position.x, c.position.z).magnitude + ext; maxR = Mathf.Max(maxR, r); }
        maxR = Mathf.Max(maxR, stairOut + stairDelta + 2);
        var mf = vs.GetComponent<MeshFilter>(); var mb = mf.sharedMesh.bounds;
        Func<float> nearest = () => { float n = float.MaxValue; for (int i = 0; i <= 8; i++) for (int j = 0; j <= 2; j++) { var lp = new Vector3(Mathf.Lerp(mb.min.x, mb.max.x, i / 8f), Mathf.Lerp(mb.min.y, mb.max.y, j / 2f), mb.center.z); var w = vs.transform.TransformPoint(lp); n = Mathf.Min(n, new Vector2(w.x, w.z).magnitude); } return n; };
        var bearing = new Vector3(vs.bounds.center.x, 0, vs.bounds.center.z).normalized; if (bearing == Vector3.zero) bearing = Vector3.right;
        int guard = 0; while (nearest() < maxR + 1.5f && guard++ < 200) { cinema.position += bearing * .5f; Physics.SyncTransforms(); }
        float bottom = vs.bounds.min.y; if (bottom < 1.5f) cinema.position += Vector3.up * (1.5f - bottom);
        Physics.SyncTransforms();
        screenT = vs.transform; screenRot = vs.transform.rotation; screenC = vs.transform.TransformPoint(mb.center);
        screenHalf = Vector3.Scale(mb.extents, new Vector3(Mathf.Abs(vs.transform.lossyScale.x), Mathf.Abs(vs.transform.lossyScale.y), Mathf.Abs(vs.transform.lossyScale.z)));
        report.Add("screen 4x: " + (screenHalf.x * 2).ToString("0.0") + " x " + (screenHalf.y * 2).ToString("0.0") + " m, nearest edge " + nearest().ToString("0.0") + " m from the pole, farthest cloud path " + maxR.ToString("0.0") + " m");
        if (orbit != null && orbit.clearZones != null) for (int i = 0; i < orbit.clearZones.Length; i++) if (orbit.clearZones[i] != null && orbit.clearZones[i].name.Contains("video")) { orbit.clearZones[i].position = screenC; orbit.clearZones[i].rotation = screenRot; orbit.clearHalf[i] = screenHalf + new Vector3(4, 4, 5); }
        if (orbit != null) UdonSharpEditorUtility.CopyProxyToUdon(orbit);
        // particle clouds (sunset clouds etc.) die inside the screen volume instead of drawing over it
        var kill = new GameObject("Screen cloud kill zone"); kill.layer = 2; kill.transform.position = screenC; kill.transform.rotation = screenRot;
        var box = kill.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = screenHalf * 2 + new Vector3(8, 8, 10);
        int n2 = 0;
        foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true))
        {
            var pr = ps.GetComponent<ParticleSystemRenderer>(); if (pr == null || pr.sharedMaterial == null) continue;
            if (!pr.sharedMaterial.shader.name.Contains("Cloud") && !pr.sharedMaterial.name.Contains("Cloud")) continue;
            if (ps.main.startSize.constantMax < 1f && ps.main.startSize.constant < 1f) continue;
            var tr = ps.trigger; tr.enabled = true; tr.SetCollider(0, box); tr.inside = ParticleSystemOverlapAction.Kill; tr.enter = ParticleSystemOverlapAction.Kill; tr.outside = ParticleSystemOverlapAction.Ignore; tr.exit = ParticleSystemOverlapAction.Ignore; n2++;
        }
        kill.transform.SetParent(cinema.parent, true);
        report.Add("particle cloud systems that now vanish at the screen: " + n2);
    }

    // ---------------------------------------------------------------- 4. phone booths: pinker, window texture, detailed payphone, floor above the cloud
    static void Booths()
    {
        var booths = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid() && t.name.StartsWith("Phone booth ") && t.Find("Booth body") != null).ToList();
        if (booths.Count == 0) { report.Add("booths FAIL: none found"); return; }
        var body0 = booths[0].Find("Booth body").GetComponent<MeshRenderer>().sharedMaterial;
        var sign0 = booths[0].Find("Booth sign glow").GetComponent<MeshRenderer>().sharedMaterial;
        var phone0 = booths[0].Find("Booth payphone").GetComponent<MeshRenderer>().sharedMaterial;
        var bodyPink = Mat("Phone Booth Body Pink", body0, m => Cols(m, new Color(1.3f, .42f, .82f), new Color(.86f, .2f, .56f), new Color(1.7f, .75f, 1.35f), .85f));
        var signPink = Mat("Phone Booth Sign Pink", sign0, m => Cols(m, new Color(2.5f, .95f, 1.9f), new Color(2.3f, .7f, 1.7f), new Color(1.8f, .9f, 1.6f), .3f));
        var chrome = Mat("Payphone Pink Chrome", phone0, m => Cols(m, new Color(1.25f, .8f, 1f), new Color(.55f, .28f, .45f), new Color(1.8f, 1.2f, 1.6f), .55f));
        windowMat = WindowMaterial();
        var glassMesh = GlassUV(booths[0].Find("Booth glass").GetComponent<MeshFilter>().sharedMesh);
        var phoneParts = PayphoneMeshes();
        foreach (var b in booths)
        {
            b.Find("Booth body").GetComponent<MeshRenderer>().sharedMaterial = bodyPink;
            b.Find("Booth sign glow").GetComponent<MeshRenderer>().sharedMaterial = signPink;
            var g = b.Find("Booth glass"); g.GetComponent<MeshFilter>().sharedMesh = glassMesh; g.GetComponent<MeshRenderer>().sharedMaterial = windowMat;
            b.Find("Booth payphone").gameObject.SetActive(false);
            var old = b.Find("Payphone detail"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var pd = new GameObject("Payphone detail").transform; pd.SetParent(b, false);
            Prim(pd, "Payphone chrome", phoneParts[0], Vector3.zero, Vector3.one, chrome);
            Prim(pd, "Payphone face", phoneParts[1], Vector3.zero, Vector3.one, darkMat);
            Prim(pd, "Payphone lights", phoneParts[2], Vector3.zero, Vector3.one, signPink);
            // lift the booth so the cloud never pokes through its floor
            var cloud = b.parent; var cr = cloud.GetComponent<MeshFilter>(); float top = float.MinValue;
            foreach (var v in cr.sharedMesh.vertices) { var w = cloud.TransformPoint(v); var local = b.InverseTransformPoint(w); if (Mathf.Abs(local.x) < .9f && Mathf.Abs(local.z) < .9f) top = Mathf.Max(top, w.y); }
            float lift = top == float.MinValue ? 0 : top + .03f - b.position.y;
            if (lift > 0) b.position += Vector3.up * lift;
            var oldF = b.Find("Cloud booth floor"); if (oldF != null) UnityEngine.Object.DestroyImmediate(oldF.gameObject);
            Solid(b, new Vector3(0, .02f, 0), new Vector3(1.3f, .04f, 1.3f), "Cloud booth floor");
            if (lift > .12f) { var st = Puff(b, "Cloud booth step", new Vector3(0, -lift * .5f - .12f, 1.05f), new Vector3(1.3f, .4f, .8f), pinkMat); Solid(b, new Vector3(0, -lift * .5f - .02f, 1.05f), new Vector3(1.2f, .04f, .7f), "Cloud booth step collider"); }
            report.Add(b.name + ": lifted " + lift.ToString("0.00") + " m above its cloud, pink body, textured windows, detailed payphone");
        }
    }

    static Material WindowMaterial()
    {
        const int W = 256, H = 512; var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
        var px = new Color[W * H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float u = x / (float)W, v = y / (float)H;
                var c = new Color(1f, .74f, .88f, .2f);
                float frost = Mathf.SmoothStep(.28f, .1f, v); c = Color.Lerp(c, new Color(1f, .9f, .96f, .62f), frost);
                float streak = Mathf.Max(0, 1 - Mathf.Abs(((u * .8f + v) % .55f) - .2f) * 22f) * .45f + Mathf.Max(0, 1 - Mathf.Abs(((u * .8f + v) % .55f) - .27f) * 60f) * .3f;
                c = Color.Lerp(c, new Color(1, 1, 1, .7f), streak * Mathf.SmoothStep(.1f, .4f, v));
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v)); c = Color.Lerp(new Color(1f, .55f, .82f, .75f), c, Mathf.SmoothStep(0, .04f, edge));
                float hx = (u - .5f) * 2.4f, hy = (v - .74f) * 2.4f; float heart = Mathf.Pow(hx * hx + hy * hy - .05f, 3) - hx * hx * hy * hy * hy;
                if (heart < 0 && heart > -.00022f) c = Color.Lerp(c, new Color(1, .95f, 1, .8f), .8f);
                else if (heart < 0) c = Color.Lerp(c, new Color(1, .86f, .95f, .5f), .45f);
                uint hsh = (uint)(x * 73856093 ^ y * 19349663); if (hsh % 997 == 0) c = new Color(1, 1, 1, .9f);
                px[y * W + x] = c;
            }
        tex.SetPixels(px); tex.Apply();
        string path = Root + "Textures/Phone Booth Window.png"; File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path); var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        var shader = Shader.Find("Unlit/Transparent");
        string mp = Root + "Materials/Lounge/Phone Booth Window.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, mp); }
        m.shader = shader; m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); EditorUtility.SetDirty(m); return m;
    }

    static Mesh GlassUV(Mesh src)
    {
        var m = UnityEngine.Object.Instantiate(src); var v = m.vertices; var n = m.normals; var uv = new Vector2[v.Length];
        for (int b = 0; b + 24 <= v.Length; b += 24)
        {
            var mn = v[b]; var mx = v[b];
            for (int i = b; i < b + 24; i++) { mn = Vector3.Min(mn, v[i]); mx = Vector3.Max(mx, v[i]); }
            var size = mx - mn; int thin = size.x < size.y && size.x < size.z ? 0 : (size.z < size.y ? 2 : 1);
            for (int i = b; i < b + 24; i++)
            {
                var p = v[i] - mn; float a = thin == 0 ? p.z / Mathf.Max(size.z, 1e-4f) : p.x / Mathf.Max(size.x, 1e-4f); float c = thin == 1 ? p.z / Mathf.Max(size.z, 1e-4f) : p.y / Mathf.Max(size.y, 1e-4f);
                uv[i] = new Vector2(a, c);
            }
        }
        m.uv = uv; return SaveMesh(m, "Phone Booth Glass UV");
    }

    class Parts { public List<Vector3> v = new List<Vector3>(); public List<Vector3> n = new List<Vector3>(); public List<int> t = new List<int>(); }
    static void Box(Parts p, Vector3 c, Vector3 size, Quaternion? rotation = null)
    {
        var rot = rotation ?? Quaternion.identity;
        var h = size / 2; Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var d in dirs)
        {
            Vector3 a = Mathf.Abs(d.y) > .5f ? Vector3.right : Vector3.up, b = Vector3.Cross(d, a); int k = p.v.Count;
            foreach (var sa in new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1) })
            { var local = Vector3.Scale(d + a * sa.x + b * sa.y, h); p.v.Add(c + rot * local); p.n.Add(rot * d); }
            p.t.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
        }
    }
    static Mesh Build(Parts p, string name) { var m = new Mesh(); m.SetVertices(p.v); m.SetNormals(p.n); m.SetTriangles(p.t, 0); m.RecalculateBounds(); return SaveMesh(m, name); }

    static Mesh[] PayphoneMeshes()
    {
        const float Wd = 1.3f; var back = new Vector3(0, 0, -(Wd / 2 - .03f) + .1f);
        Parts chrome = new Parts(), face = new Parts(), glow = new Parts();
        Box(chrome, back + new Vector3(.08f, 1.38f, .05f), new Vector3(.36f, .56f, .1f));              // housing
        Box(chrome, back + new Vector3(.08f, 1.68f, .08f), new Vector3(.42f, .05f, .17f));             // hood
        Box(chrome, back + new Vector3(.08f, 1.085f, .07f), new Vector3(.4f, .04f, .14f));             // shelf
        Box(face, back + new Vector3(.08f, 1.39f, .102f), new Vector3(.3f, .47f, .006f));              // face plate
        Box(glow, back + new Vector3(.08f, 1.57f, .107f), new Vector3(.2f, .055f, .004f));             // display
        for (int row = 0; row < 4; row++) for (int col = 0; col < 3; col++)
                Box(glow, back + new Vector3(.08f + (col - 1) * .056f, 1.47f - row * .046f, .111f), new Vector3(.04f, .032f, .012f)); // keypad
        Box(chrome, back + new Vector3(.19f, 1.61f, .104f), new Vector3(.036f, .075f, .004f));         // coin plate
        Box(face, back + new Vector3(.19f, 1.61f, .107f), new Vector3(.012f, .048f, .004f));           // coin slot
        Box(chrome, back + new Vector3(.12f, 1.19f, .115f), new Vector3(.085f, .055f, .03f));          // coin return
        Box(face, back + new Vector3(.12f, 1.185f, .131f), new Vector3(.06f, .03f, .004f));
        Box(glow, back + new Vector3(.02f, 1.265f, .104f), new Vector3(.12f, .05f, .004f));            // instruction card
        Box(chrome, back + new Vector3(-.12f, 1.47f, .09f), new Vector3(.035f, .12f, .06f));          // cradle
        Box(chrome, back + new Vector3(-.12f, 1.53f, .13f), new Vector3(.02f, .02f, .07f));           // hook
        var tilt = Quaternion.Euler(0, 0, 6);
        Box(chrome, back + new Vector3(-.165f, 1.42f, .15f), new Vector3(.045f, .2f, .045f), tilt);  // handset grip
        Box(chrome, back + new Vector3(-.175f, 1.54f, .165f), new Vector3(.075f, .065f, .085f), tilt); // earpiece
        Box(chrome, back + new Vector3(-.155f, 1.30f, .165f), new Vector3(.075f, .065f, .085f), tilt); // mouthpiece
        Box(face, back + new Vector3(-.175f, 1.54f, .209f), new Vector3(.05f, .04f, .004f), tilt);
        Box(face, back + new Vector3(-.155f, 1.30f, .209f), new Vector3(.05f, .04f, .004f), tilt);
        // coiled cord from the mouthpiece down and back into the housing
        var top = back + new Vector3(-.15f, 1.26f, .16f); int steps = 18 * 12; float len = .24f, coil = .018f, wire = .004f;
        for (int s = 0; s < steps; s++)
        {
            float t = s / (float)steps, a = t * 18 * Mathf.PI * 2, t2 = (s + 1) / (float)steps, a2 = t2 * 18 * Mathf.PI * 2;
            var c1 = top + new Vector3(Mathf.Cos(a) * coil, -t * len, Mathf.Sin(a) * coil); var c2 = top + new Vector3(Mathf.Cos(a2) * coil, -t2 * len, Mathf.Sin(a2) * coil);
            Box(face, (c1 + c2) / 2, new Vector3(wire, (c2 - c1).magnitude + .002f, wire), Quaternion.FromToRotation(Vector3.up, (c2 - c1).normalized));
        }
        var end = top + Vector3.down * len; var joint = back + new Vector3(-.06f, 1.12f, .1f);
        Box(face, (end + joint) / 2, new Vector3(wire * 1.5f, (joint - end).magnitude, wire * 1.5f), Quaternion.FromToRotation(Vector3.up, (joint - end).normalized));
        return new[] { Build(chrome, "Payphone Chrome"), Build(face, "Payphone Face"), Build(glow, "Payphone Lights") };
    }

    // ---------------------------------------------------------------- 5. sparkles flowing up the whole staircase
    static void StairFlow()
    {
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>();
        var ramp = spiral.turns[0].GetComponent<MeshCollider>().sharedMesh;
        float omega = .55f, pitch = spiral.pitch, rise = pitch * omega / (Mathf.PI * 2);
        float sign = OrbitSign(stairIn + stairDelta + 1);
        int n = 0;
        foreach (var t in spiral.turns)
        {
            var old = t.Find("Stair sparkle flow"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go = new GameObject("Stair sparkle flow"); go.transform.SetParent(t, false); var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            FlowSettings(ps, ramp, sign * omega, rise); n++;
        }
        report.Add("stair sparkle flow on " + n + " turns, orbit sign " + sign + ", " + (40 * n) + " particles max");
    }
    static void FlowSettings(ParticleSystem ps, Mesh ramp, float orbitY, float rise)
    {
        var main = ps.main; main.loop = true; main.prewarm = true; main.playOnAwake = true; main.maxParticles = 40; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 7f); main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.05f, .15f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .75f, .95f), new Color(.7f, .85f, 1f));
        var em = ps.emission; em.rateOverTime = 6.5f;
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Mesh; sh.meshShapeType = ParticleSystemMeshShapeType.Triangle; sh.mesh = ramp; sh.position = new Vector3(0, .12f, 0);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(0); vel.y = new ParticleSystem.MinMaxCurve(rise); vel.z = new ParticleSystem.MinMaxCurve(0);
        vel.orbitalX = new ParticleSystem.MinMaxCurve(0); vel.orbitalY = new ParticleSystem.MinMaxCurve(orbitY); vel.orbitalZ = new ParticleSystem.MinMaxCurve(0);
        var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(1, .8f), new GradientAlphaKey(0, 1) }); col.color = gr;
        var pr = ps.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = starMat; pr.renderMode = ParticleSystemRenderMode.Billboard; Quiet(pr);
    }
    // which orbital sign carries a particle the way the stairs climb (from -z toward +x)
    static float OrbitSign(float r)
    {
        var go = new GameObject("orbit test"); var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.playOnAwake = false; main.startLifetime = 10; main.startSpeed = 0;
        var em = ps.emission; em.enabled = false; var sh = ps.shape; sh.enabled = false;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local; vel.x = 0; vel.y = 0; vel.z = 0; vel.orbitalX = 0; vel.orbitalY = .5f; vel.orbitalZ = 0;
        ps.Play(); ps.Emit(new ParticleSystem.EmitParams { position = new Vector3(0, 0, -r), velocity = Vector3.zero, startLifetime = 10, startSize = .1f }, 1);
        ps.Simulate(.6f, false, false, false); var arr = new ParticleSystem.Particle[1]; int c = ps.GetParticles(arr);
        float x = c > 0 ? arr[0].position.x : 0; UnityEngine.Object.DestroyImmediate(go);
        return x > 0 ? 1f : -1f;
    }

    // ---------------------------------------------------------------- 6. cloud furniture, many sizes
    static Material Tint() { int k = rng.Next(4); return k == 0 ? cloudMat : k == 1 ? pinkMat : k == 2 ? lilacMat : peachMat; }
    static void Chair(Transform g, float s, Material m)
    {
        Puff(g, "Cloud seat", new Vector3(0, .32f, 0) * s, new Vector3(1f, .5f, .95f) * s, m);
        Puff(g, "Cloud back", new Vector3(0, .82f, -.4f) * s, new Vector3(1f, .95f, .38f) * s, m);
        Puff(g, "Cloud arm", new Vector3(-.55f, .55f, 0) * s, new Vector3(.32f, .42f, .9f) * s, m);
        Puff(g, "Cloud arm", new Vector3(.55f, .55f, 0) * s, new Vector3(.32f, .42f, .9f) * s, m);
        Solid(g, new Vector3(0, .25f, 0) * s, new Vector3(1f, .5f, .95f) * s); Solid(g, new Vector3(0, .7f, -.42f) * s, new Vector3(1f, .9f, .3f) * s, "Cloud back collider");
    }
    static void Sofa(Transform g, float s, Material m)
    {
        Puff(g, "Cloud sofa seat", new Vector3(0, .32f, 0) * s, new Vector3(2.5f, .5f, 1f) * s, m);
        Puff(g, "Cloud sofa back", new Vector3(0, .85f, -.42f) * s, new Vector3(2.6f, .85f, .4f) * s, m);
        Puff(g, "Cloud sofa arm", new Vector3(-1.3f, .55f, 0) * s, new Vector3(.38f, .5f, 1f) * s, m);
        Puff(g, "Cloud sofa arm", new Vector3(1.3f, .55f, 0) * s, new Vector3(.38f, .5f, 1f) * s, m);
        Puff(g, "Cloud cushion", new Vector3(-.62f, .95f, -.2f) * s, new Vector3(.7f, .5f, .28f) * s, pinkMat);
        Puff(g, "Cloud cushion", new Vector3(.62f, .95f, -.2f) * s, new Vector3(.7f, .5f, .28f) * s, lilacMat);
        Solid(g, new Vector3(0, .25f, 0) * s, new Vector3(2.5f, .5f, 1f) * s); Solid(g, new Vector3(0, .75f, -.45f) * s, new Vector3(2.6f, .8f, .3f) * s, "Cloud back collider");
    }
    static void Bed(Transform g, float s, Material m)
    {
        Puff(g, "Cloud bed base", new Vector3(0, .28f, 0) * s, new Vector3(2.3f, .55f, 2.7f) * s, m);
        Puff(g, "Cloud mattress", new Vector3(0, .6f, .05f) * s, new Vector3(2.05f, .32f, 2.4f) * s, cloudMat);
        Puff(g, "Cloud pillow", new Vector3(-.5f, .86f, -.9f) * s, new Vector3(.82f, .3f, .46f) * s, pinkMat);
        Puff(g, "Cloud pillow", new Vector3(.5f, .86f, -.9f) * s, new Vector3(.82f, .3f, .46f) * s, pinkMat);
        Puff(g, "Cloud blanket", new Vector3(0, .78f, .55f) * s, new Vector3(2.1f, .2f, 1f) * s, lilacMat);
        Puff(g, "Cloud headboard", new Vector3(0, 1.15f, -1.35f) * s, new Vector3(2.4f, 1.4f, .45f) * s, m);
        Prim(g, "Headboard star", sphere, new Vector3(0, 1.75f, -1.3f) * s, Vector3.one * .28f * s, glowMat);
        Solid(g, new Vector3(0, .38f, 0) * s, new Vector3(2.2f, .75f, 2.6f) * s);
    }
    static void Beanbag(Transform g, float s, Material m)
    {
        Puff(g, "Cloud beanbag", new Vector3(0, .32f, 0) * s, new Vector3(1.15f, .62f, 1.15f) * s, m);
        Puff(g, "Cloud beanbag top", new Vector3(0, .6f, -.25f) * s, new Vector3(.9f, .45f, .55f) * s, m);
        Solid(g, new Vector3(0, .25f, 0) * s, new Vector3(1.1f, .5f, 1.1f) * s);
    }
    static void Ottoman(Transform g, float s, Material m) { Puff(g, "Cloud ottoman", new Vector3(0, .24f, 0) * s, new Vector3(.8f, .46f, .8f) * s, m); Solid(g, new Vector3(0, .2f, 0) * s, new Vector3(.75f, .4f, .75f) * s); }
    static void Chaise(Transform g, float s, Material m)
    {
        Puff(g, "Cloud chaise", new Vector3(0, .3f, .2f) * s, new Vector3(.95f, .45f, 2.1f) * s, m);
        var back = Puff(g, "Cloud chaise back", new Vector3(0, .7f, -.8f) * s, new Vector3(.95f, .85f, .36f) * s, m); back.transform.localRotation = Quaternion.Euler(-32, 0, 0);
        Puff(g, "Cloud chaise pillow", new Vector3(0, .8f, -.55f) * s, new Vector3(.6f, .25f, .3f) * s, pinkMat);
        Solid(g, new Vector3(0, .25f, .2f) * s, new Vector3(.95f, .45f, 2.1f) * s);
    }
    static void Table(Transform g, float s)
    {
        Puff(g, "Cloud table stem", new Vector3(0, .3f, 0) * s, new Vector3(.38f, .62f, .38f) * s, cloudMat);
        Puff(g, "Cloud table top", new Vector3(0, .68f, 0) * s, new Vector3(1.35f, .24f, 1.35f) * s, cloudMat);
        Solid(g, new Vector3(0, .62f, 0) * s, new Vector3(1.2f, .14f, 1.2f) * s, "Cloud table collider");
        float top = .8f * s;
        for (int i = 0; i < 4; i++)
        {
            var p = Polar(i * 90 + 45, .38f * s, top);
            Prim(g, "Saucer", cylinder, p, new Vector3(.16f, .006f, .16f) * s, cupMat);
            Prim(g, "Teacup", cylinder, p + Vector3.up * .045f * s, new Vector3(.09f, .04f, .09f) * s, i % 2 == 0 ? pinkMat : cupMat);
            Prim(g, "Tea", cylinder, p + Vector3.up * .082f * s, new Vector3(.075f, .002f, .075f) * s, peachMat);
        }
        Prim(g, "Teapot", sphere, new Vector3(0, top + .11f * s, 0), new Vector3(.24f, .2f, .24f) * s, pinkMat);
        Prim(g, "Teapot lid", sphere, new Vector3(0, top + .22f * s, 0), new Vector3(.09f, .05f, .09f) * s, cupMat);
        Prim(g, "Teapot spout", cylinder, new Vector3(.15f * s, top + .14f * s, 0), new Vector3(.03f, .07f, .03f) * s, pinkMat, new Vector3(0, 0, -55));
        Prim(g, "Macaron", sphere, new Vector3(-.12f, top + .02f * s, .2f * s), new Vector3(.07f, .035f, .07f) * s, lilacMat);
        Prim(g, "Macaron", sphere, new Vector3(-.2f, top + .02f * s, .1f * s), new Vector3(.07f, .035f, .07f) * s, peachMat);
    }
    static void Lamp(Transform g, float s)
    {
        Puff(g, "Cloud lamp base", new Vector3(0, .3f, 0) * s, new Vector3(.55f, .6f, .55f) * s, cloudMat);
        Puff(g, "Cloud lamp neck", new Vector3(0, .8f, 0) * s, new Vector3(.34f, .55f, .34f) * s, lilacMat);
        Prim(g, "Glow orb", sphere, new Vector3(0, 1.28f, 0) * s, Vector3.one * .48f * s, glowMat);
    }

    static void Furniture()
    {
        var fur = new GameObject("Cloud furniture").transform; fur.SetParent(lounge, false);
        Action<float, float, string, Action<Transform>> corner = (deg, r, name, build) =>
        {
            var p = Polar(deg, r, 0); if (!Place(ref p, 4.2f)) return;
            var g = Group(fur, name, p, FaceCenter(p)); build(g);
        };
        corner(118, 17, "Sleepy nook", g => { var a = Group(g, "Big bed", new Vector3(-1.6f, 0, 0), 0); Bed(a, 1.15f, pinkMat); var b = Group(g, "Little bed", new Vector3(1.9f, 0, .4f), -12); Bed(b, .7f, lilacMat); Lamp(Group(g, "Lamp", new Vector3(.3f, 0, -1.4f), 0), .9f); Ottoman(Group(g, "Bench", new Vector3(-1.6f, 0, 2f), 0), 1.2f, peachMat); });
        corner(168, 15, "Tea lounge", g => { Sofa(Group(g, "Sofa", new Vector3(0, 0, -1.6f), 0), 1f, lilacMat); Chair(Group(g, "Armchair", new Vector3(-2.1f, 0, .6f), 70), 1f, pinkMat); Chair(Group(g, "Armchair", new Vector3(2.1f, 0, .6f), -70), 1f, peachMat); Table(Group(g, "Tea table", new Vector3(0, 0, .2f), 0), 1f); Beanbag(Group(g, "Beanbag", new Vector3(0, 0, 2.2f), 180), .9f, cloudMat); });
        corner(-100, 22, "Reading nook", g => { Chaise(Group(g, "Chaise", new Vector3(-1.2f, 0, 0), 20), 1.1f, peachMat); Chaise(Group(g, "Chaise", new Vector3(1.2f, 0, 0), -20), 1.1f, pinkMat); Lamp(Group(g, "Lamp", new Vector3(0, 0, -1.2f), 0), 1.1f); });
        corner(88, 27, "Giant chair", g => { Chair(Group(g, "Giant cloud chair", Vector3.zero, 0), 3.2f, pinkMat); Chair(Group(g, "Tiny cloud chair", new Vector3(2.8f, 0, 1.5f), -25), .45f, lilacMat); Chair(Group(g, "Tiny cloud chair", new Vector3(3.4f, 0, .9f), -40), .35f, peachMat); });
        corner(-30, 17, "Beanbag pile", g => { for (int i = 0; i < 6; i++) { var p = Polar(i * 60 + R(-10, 10), R(1.4f, 2.2f), 0); Beanbag(Group(g, "Beanbag", p, FaceCenter(p)), R(.6f, 1.5f), Tint()); } Lamp(Group(g, "Lamp", Vector3.zero, 0), 1.3f); });
        // scattered singles in many sizes
        Action<Transform, float, Material>[] kinds = { Chair, Beanbag, Ottoman, Chaise, Sofa, Bed };
        for (int i = 0; i < 16; i++)
        {
            var p = Polar(R(0, 360), R(11, 38), 0); float s = new[] { .5f, .7f, .9f, 1f, 1.2f, 1.5f, 1.9f, 2.4f }[rng.Next(8)];
            if (!Place(ref p, 1.6f * s)) continue;
            var k = rng.Next(kinds.Length); var g = Group(fur, "Cloud " + kinds[k].Method.Name.ToLower() + " x" + s.ToString("0.0"), p, FaceCenter(p) + R(-30, 30)); kinds[k](g, s, Tint());
        }
        report.Add("furniture pieces: " + fur.GetComponentsInChildren<Transform>().Count(t => t.parent != null && (t.parent == fur || t.parent.parent == fur)));
    }

    // ---------------------------------------------------------------- 7. the bathtub cloud with a rubber ducky
    static void Bathtub()
    {
        var p = Polar(-165, 21, .45f); if (!Place(ref p, 4f)) return;
        var g = Group(lounge, "Bathtub cloud", p, FaceCenter(p));
        Puff(g, "Cloud bath platform", new Vector3(0, -.2f, 0), new Vector3(6.5f, 1.1f, 5.5f), cloudMat); Solid(g, new Vector3(0, .1f, 0), new Vector3(6f, .5f, 5f), "Cloud bath platform collider");
        Puff(g, "Cloud step", new Vector3(0, -.25f, 3.1f), new Vector3(2f, .55f, 1.2f), pinkMat); Solid(g, new Vector3(0, -.15f, 3.1f), new Vector3(1.8f, .3f, 1f), "Cloud step collider");
        var tub = Group(g, "Tub", new Vector3(0, .35f, -.3f), 0);
        Puff(tub, "Cloud tub bottom", new Vector3(0, .15f, 0), new Vector3(2.7f, .45f, 1.6f), pinkMat);
        for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2; Puff(tub, "Cloud tub rim", new Vector3(Mathf.Cos(a) * 1.3f, .55f, Mathf.Sin(a) * .75f), new Vector3(.62f, .58f, .62f), i % 2 == 0 ? pinkMat : cloudMat); }
        for (int i = 0; i < 4; i++) Prim(tub, "Tub foot", sphere, new Vector3(i < 2 ? -1f : 1f, -.05f, i % 2 == 0 ? -.5f : .5f), new Vector3(.22f, .14f, .22f), glowMat);
        Prim(tub, "Bath water", cylinder, new Vector3(0, .62f, 0), new Vector3(2.35f, .01f, 1.3f), waterMat);
        Solid(tub, new Vector3(0, .3f, 0), new Vector3(2.4f, .6f, 1.3f), "Cloud tub collider");
        // rubber ducky
        var duck = Group(tub, "Rubber ducky", new Vector3(.35f, .66f, .1f), 35);
        Prim(duck, "Duck body", sphere, new Vector3(0, .09f, 0), new Vector3(.3f, .21f, .38f), duckMat);
        Prim(duck, "Duck tail", sphere, new Vector3(0, .15f, -.17f), new Vector3(.12f, .1f, .12f), duckMat, new Vector3(-30, 0, 0));
        Prim(duck, "Duck head", sphere, new Vector3(0, .28f, .1f), new Vector3(.2f, .2f, .2f), duckMat);
        Prim(duck, "Duck beak", sphere, new Vector3(0, .26f, .21f), new Vector3(.1f, .04f, .09f), beakMat);
        Prim(duck, "Duck eye", sphere, new Vector3(-.065f, .31f, .17f), Vector3.one * .03f, darkMat);
        Prim(duck, "Duck eye", sphere, new Vector3(.065f, .31f, .17f), Vector3.one * .03f, darkMat);
        Prim(duck, "Duck wing", sphere, new Vector3(-.14f, .1f, -.02f), new Vector3(.05f, .1f, .2f), duckMat);
        Prim(duck, "Duck wing", sphere, new Vector3(.14f, .1f, -.02f), new Vector3(.05f, .1f, .2f), duckMat);
        // bubbles
        var bgo = new GameObject("Bath bubbles"); bgo.transform.SetParent(tub, false); bgo.transform.localPosition = new Vector3(0, .65f, 0);
        var ps = bgo.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.prewarm = true; main.maxParticles = 60; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3, 6); main.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .25f); main.startSize = new ParticleSystem.MinMaxCurve(.04f, .16f);
        var em = ps.emission; em.rateOverTime = 12; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.2f, .05f, 1.1f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local; vel.x = new ParticleSystem.MinMaxCurve(-.05f, .05f); vel.y = new ParticleSystem.MinMaxCurve(.15f, .35f); vel.z = new ParticleSystem.MinMaxCurve(-.05f, .05f);
        var noise = ps.noise; noise.enabled = true; noise.strength = .15f; noise.frequency = .6f;
        var pr = ps.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Mesh; pr.mesh = sphere; pr.sharedMaterial = waterMat; Quiet(pr);
        // towel stool and a little glow lamp
        Ottoman(Group(g, "Towel stool", new Vector3(2f, .3f, .9f), 0), .8f, lilacMat);
        Lamp(Group(g, "Bath lamp", new Vector3(-2.2f, .3f, -1.2f), 0), .8f);
        report.Add("bathtub cloud at " + p.ToString("0.0") + " with a rubber ducky and bubbles");
    }

    // ---------------------------------------------------------------- 8. amenities
    static void Amenities()
    {
        var am = new GameObject("Cloud amenities").transform; am.SetParent(lounge, false);
        // swing hanging from a floating cloud
        var p = Polar(-58, 19, 0);
        if (Place(ref p, 3f))
        {
            var g = Group(am, "Cloud swing", p, FaceCenter(p));
            Puff(g, "Cloud swing anchor", new Vector3(0, 5.2f, 0), new Vector3(4f, 1.4f, 3f), lilacMat, 0, true);
            foreach (var x in new[] { -.55f, .55f }) Prim(g, "Swing rope", cylinder, new Vector3(x, 2.7f, 0), new Vector3(.035f, 2.2f, .035f), glowMat);
            Puff(g, "Cloud swing seat", new Vector3(0, .5f, 0), new Vector3(1.4f, .35f, .6f), pinkMat); Solid(g, new Vector3(0, .5f, 0), new Vector3(1.3f, .2f, .55f));
        }
        // hammock between two cloud posts
        p = Polar(-78, 29, 0);
        if (Place(ref p, 3.5f))
        {
            var g = Group(am, "Cloud hammock", p, FaceCenter(p) + 90);
            foreach (var x in new[] { -2.1f, 2.1f }) for (int k = 0; k < 3; k++) Puff(g, "Cloud post", new Vector3(x, .5f + k * .75f, 0), new Vector3(.9f - k * .12f, .8f, .9f - k * .12f), cloudMat, 0, k == 0);
            for (int i = 0; i <= 8; i++) { float t = i / 8f, x = Mathf.Lerp(-1.7f, 1.7f, t), y = 1.85f - Mathf.Sin(t * Mathf.PI) * .9f; Puff(g, "Cloud hammock", new Vector3(x, y, 0), new Vector3(.62f, .28f, 1.1f), i % 2 == 0 ? pinkMat : peachMat); }
            Solid(g, new Vector3(0, 1.05f, 0), new Vector3(2.4f, .15f, 1f), "Cloud hammock collider");
        }
        // pink firepit circle with ottomans in mixed sizes
        p = Polar(-8, 25, 0);
        if (Place(ref p, 4f))
        {
            var g = Group(am, "Cloud firepit", p, 0);
            for (int i = 0; i < 10; i++) { float a = i * 36; Puff(g, "Cloud firepit ring", Polar(a, .95f, .15f), new Vector3(.55f, .35f, .55f), peachMat, a); }
            var fgo = new GameObject("Pink flames"); fgo.transform.SetParent(g, false); fgo.transform.localPosition = new Vector3(0, .25f, 0);
            var ps = fgo.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.prewarm = true; main.maxParticles = 70; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(.4f, 1.1f); main.startSize = new ParticleSystem.MinMaxCurve(.12f, .35f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .45f, .7f), new Color(1f, .7f, .45f));
            var em = ps.emission; em.rateOverTime = 45; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12; sh.radius = .35f; sh.rotation = new Vector3(-90, 0, 0);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.8f, .6f, 1f), 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(0, 1) }); col.color = gr;
            var pr = ps.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = sparkMat; Quiet(pr);
            for (int i = 0; i < 7; i++) { float a = i * 51.4f + R(-8, 8); var sp = Polar(a, R(2.6f, 3.3f), 0); var s = new[] { .6f, .8f, 1f, 1.3f, 1.7f }[rng.Next(5)]; var og = Group(g, "Firepit seat", sp, FaceCenter(sp)); if (i % 2 == 0) Ottoman(og, s, Tint()); else Beanbag(og, s, Tint()); }
        }
        // glowing lamp posts along the walk around the stairs
        for (int i = 0; i < 10; i++) { var lp = Polar(i * 36 + 18, 12.5f, 0); if (Place(ref lp, .6f)) Lamp(Group(am, "Cloud lamp", lp, 0), R(1.1f, 1.8f)); }
    }

    // ---------------------------------------------------------------- 9. clouds with hanging crystal vines
    static void CrystalVines()
    {
        var root = new GameObject("Crystal vine clouds").transform; root.SetParent(lounge, false); int made = 0;
        for (int c = 0; c < 8; c++)
        {
            var p = Polar(c * 45 + R(-12, 12), R(13, 32), R(6.5f, 13f)); if (!Place(ref p, 4.5f, 11f)) continue;
            var g = Group(root, "Crystal vine cloud " + (c + 1), p, R(0, 360)); float w = R(4.5f, 7.5f);
            Puff(g, "Cloud", Vector3.zero, new Vector3(w, w * .35f, w * .8f), Tint(), 0, true);
            Puff(g, "Cloud", new Vector3(w * .3f, .35f, .2f), new Vector3(w * .55f, w * .3f, w * .5f), cloudMat);
            var parts = new[] { new CombineList(), new CombineList(), new CombineList() };
            int vines = rng.Next(7, 13);
            for (int v = 0; v < vines; v++)
            {
                float ang = R(0, Mathf.PI * 2), rad = R(.2f, .42f); var start = new Vector3(Mathf.Cos(ang) * w * rad, -w * .12f, Mathf.Sin(ang) * w * .8f * rad);
                float len = R(2f, 6.5f), sway = R(.15f, .5f), ph = R(0, 6.3f); int mi = rng.Next(3);
                for (float t = 0; t < len; t += .22f)
                {
                    var pos = start + new Vector3(Mathf.Sin(t * 1.3f + ph) * sway * t / len, -t, Mathf.Cos(t * .9f + ph) * sway * .6f * t / len);
                    float s = Mathf.Lerp(.11f, .06f, t / len);
                    parts[mi].Add(octa, Matrix4x4.TRS(pos, Quaternion.Euler(R(-15, 15), R(0, 360), R(-15, 15)), Vector3.one * s));
                    if (rng.NextDouble() < .55) { var side = Quaternion.Euler(0, R(0, 360), 0) * Vector3.right; parts[mi].Add(octa, Matrix4x4.TRS(pos + side * .09f, Quaternion.LookRotation(side) * Quaternion.Euler(70, 0, 0), new Vector3(.9f, .35f, .5f) * s)); }
                }
                var tip = start + new Vector3(Mathf.Sin(len * 1.3f + ph) * sway, -len - .15f, Mathf.Cos(len * .9f + ph) * sway * .6f);
                parts[mi].Add(octa, Matrix4x4.TRS(tip, Quaternion.identity, new Vector3(.2f, .3f, .2f)));
            }
            for (int m = 0; m < 3; m++) if (parts[m].Count > 0)
                {
                    var mesh = parts[m].Build(); mesh = SaveMesh(mesh, "Crystal vines " + (c + 1) + "-" + m);
                    Prim(g, "Crystal vines", mesh, Vector3.zero, Vector3.one, crystalMats[m]);
                }
            var gl = new GameObject("Crystal glints"); gl.transform.SetParent(g, false); gl.transform.localPosition = new Vector3(0, -2.5f, 0);
            var ps = gl.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.maxParticles = 18; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1.1f); main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.08f, .22f); main.prewarm = true;
            var em = ps.emission; em.rateOverTime = 14; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(w * .7f, 4.5f, w * .55f);
            var pr = ps.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = starMat; Quiet(pr);
            made++;
        }
        report.Add("crystal vine clouds: " + made);
    }
    class CombineList
    {
        List<CombineInstance> list = new List<CombineInstance>(); public int Count { get { return list.Count; } }
        public void Add(Mesh m, Matrix4x4 x) { list.Add(new CombineInstance { mesh = m, transform = x }); }
        public Mesh Build() { var m = new Mesh(); m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; m.CombineMeshes(list.ToArray(), true, true); m.RecalculateBounds(); return m; }
    }

    // ---------------------------------------------------------------- 10. butterflies
    static GameObject butterflies;
    static void Butterflies()
    {
        var src = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true).FirstOrDefault(p => p.name == "Butterflies");
        if (src == null) { report.Add("butterflies FAIL: DJ butterflies not found to copy"); return; }
        butterflies = new GameObject("Butterflies"); butterflies.transform.SetParent(lounge, false);
        foreach (var spot in new[] { new Vector3(0, 2.2f, 0), new Vector3(0, 2.8f, 0) })
        {
            var b = UnityEngine.Object.Instantiate(src.gameObject, butterflies.transform); b.name = "Butterfly swarm"; b.transform.localPosition = spot; b.transform.localRotation = Quaternion.identity; b.transform.localScale = Vector3.one;
            var ps = b.GetComponent<ParticleSystem>(); var main = ps.main; main.maxParticles = 45; main.startLifetime = new ParticleSystem.MinMaxCurve(10, 18);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .6f, .9f), new Color(.6f, .8f, 1f));
            var em = ps.emission; em.rateOverTime = 3.5f; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = spot.y < 2.5f ? new Vector3(60, 3, 60) : new Vector3(26, 5, 26);
            var noise = ps.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .25f;
        }
        report.Add("butterflies: 2 swarms, 90 max");
    }

    // ---------------------------------------------------------------- 11. large clouds filling a third of the outer ring (reversible)
    static GameObject outer;
    static void OuterClouds()
    {
        outer = new GameObject(OuterName); float target = Mathf.PI * (48 * 48 - 30 * 30) / 3f, area = 0; int n = 0;
        for (int i = 0; i < 40 && area < target; i++)
        {
            float w = R(10, 17), d = w * R(.65f, .9f); var p = Polar(i * 27.7f + R(-6, 6), R(34, 44 - w * .2f), R(-1.2f, 4.5f));
            if (!Place(ref p, w * .45f, 28f)) continue;
            var g = Group(outer.transform, "Outer cloud " + (++n), p, R(0, 360)); var m = Tint();
            Puff(g, "Cloud", Vector3.zero, new Vector3(w, w * R(.28f, .4f), d), m, 0, true);
            for (int k = 0; k < rng.Next(2, 5); k++) Puff(g, "Cloud", new Vector3(R(-w * .35f, w * .35f), R(.4f, 1.8f), R(-d * .3f, d * .3f)), new Vector3(w * R(.35f, .6f), w * R(.22f, .35f), d * R(.35f, .6f)), k % 2 == 0 ? cloudMat : m);
            area += Mathf.PI * w * d / 4f;
        }
        report.Add("outer big clouds: " + n + " covering " + (area / (target * 3) * 100).ToString("0") + "% of the 30-48 m ring (goal 33%)");
    }

    // ---------------------------------------------------------------- 12. personal menu switches
    static void Switches()
    {
        Transform panel = null; foreach (var rr in EditorSceneManager.GetActiveScene().GetRootGameObjects()) foreach (var t in rr.GetComponentsInChildren<Transform>(true)) if (t.name == "World items") panel = t;
        if (panel == null) { report.Add("switches FAIL: World items panel not found"); return; }
        Action<string, string, GameObject[]> sw = (label, holderName, targets) =>
        {
            targets = targets.Where(x => x != null).ToArray(); if (targets.Length == 0) return;
            var holder = new GameObject(holderName); holder.transform.SetParent(lounge, false);
            var os = holder.AddUdonSharpComponent<AstraObjectSwitch>(); os.targets = targets;
            var res = AstraAppearanceSwitches.Ensure(panel, label, UdonSharpEditorUtility.GetBackingUdonBehaviour(os), "Apply", true);
            os.toggle = res.Toggle; UdonSharpEditorUtility.CopyProxyToUdon(os); report.Add(res.Report);
        };
        var petals = GameObject.Find("Cherry blossom petals");
        sw("CHERRY PETALS", "Cherry petals switch", new[] { petals });
        sw("BUTTERFLIES", "Butterflies switch", new[] { butterflies });
        sw("CLOUD LOUNGE", "Cloud lounge switch", lounge.Cast<Transform>().Where(t => !t.name.EndsWith("switch") && t.name != "Butterflies").Select(t => t.gameObject).ToArray());
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>();
        sw("STAIR SPARKLES", "Stair sparkles switch", spiral.turns.Select(t => t.Find("Stair sparkle flow")).Where(t => t != null).Select(t => t.gameObject).ToArray());
        var holder2 = new GameObject("Outer clouds switch"); holder2.transform.SetParent(outer.transform, false);
        var os2 = holder2.AddUdonSharpComponent<AstraObjectSwitch>(); os2.targets = outer.transform.Cast<Transform>().Where(t => t != holder2.transform).Select(t => t.gameObject).ToArray();
        var res2 = AstraAppearanceSwitches.Ensure(panel, "OUTER CLOUDS", UdonSharpEditorUtility.GetBackingUdonBehaviour(os2), "Apply", true);
        os2.toggle = res2.Toggle; UdonSharpEditorUtility.CopyProxyToUdon(os2); report.Add(res2.Report);
    }

    // ---------------------------------------------------------------- validation
    static void Validate()
    {
        var spiral = UnityEngine.Object.FindObjectOfType<AstraSpiral>(); spiral.Recenter(0); Physics.SyncTransforms();
        float mid = stairIn + stairDelta + (stairOut - stairIn) / 2; int ok = 0;
        for (int s = 0; s < 40; s++) { float a = -Mathf.PI / 2 + (s + .5f) * Mathf.PI * 2 / 40; var from = new Vector3(Mathf.Cos(a) * mid, (s + 1) * .16f + .5f, Mathf.Sin(a) * mid); RaycastHit h; if (Physics.Raycast(from, Vector3.down, out h, 1.2f, ~0, QueryTriggerInteraction.Ignore) && h.collider.name.StartsWith("Spiral turn")) ok++; }
        report.Add("walkable wide stairs: " + ok + "/40 " + (ok >= 38 ? "PASS" : "FAIL"));
        RaycastHit sp; bool spawn = Physics.Raycast(new Vector3(0, 1, -2), Vector3.down, out sp, 2f, ~0, QueryTriggerInteraction.Ignore);
        report.Add("spawn floor: " + (spawn ? "PASS (" + sp.collider.name + ")" : "FAIL"));
        bool ring = Physics.Raycast(new Vector3(0, 1, -(stairOut + stairDelta + 1.5f)), Vector3.down, out sp, 2f, ~0, QueryTriggerInteraction.Ignore);
        report.Add("floor just outside the stairs: " + (ring ? "PASS" : "FAIL"));
        int total = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true).Where(p => p.gameObject.activeInHierarchy).Sum(p => p.main.maxParticles);
        report.Add("particle capacity (active systems): " + total);
    }
}
