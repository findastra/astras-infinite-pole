// Natural cloud shapes, no hammock, grabbable glow orbs (Claude, 2026-09-26).
// Menu: Astra > Claude > 9 Natural cloud shapes + glow orbs. Rollback: Astra > Claude > Restore scene from before cloud shapes.
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

public static class AstraClaudeCloudShapes
{
    const string Backup = "Review/Backups/BeforeCloudShapes.unity.txt";
    static System.Random rng; static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    static Mesh cloudMesh;

    [MenuItem("Astra/Claude/9 Natural cloud shapes + glow orbs")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge == null) { EditorUtility.DisplayDialog("Cloud shapes", "Cloud lounge not found.", "OK"); return; }
        EditorSceneManager.SaveScene(scene); File.Copy(scene.path, Backup, true);
        rng = new System.Random(2609); var report = new List<string>();
        var outer = GameObject.Find("14 - Outer big clouds (Claude)");
        var roots = new List<Transform> { lounge.transform }; if (outer != null) roots.Add(outer.transform);
        int before = roots.Sum(r => r.GetComponentsInChildren<MeshRenderer>(true).Length);

        // 1. hammock out
        foreach (var h in lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Cloud hammock").ToList()) { if (h == null) continue; UnityEngine.Object.DestroyImmediate(h.gameObject); report.Add("hammock removed"); }

        // 2. lamps -> free-floating grabbable glow orbs
        EnsureProgram();
        var starMat = GameObject.Find("10 - Stair opening star dust")?.GetComponent<ParticleSystemRenderer>()?.sharedMaterial;
        var orbRoot = new GameObject("Glow orbs").transform; orbRoot.SetParent(lounge.transform, false);
        int orbs = 0;
        foreach (var orb in lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Glow orb" && t.parent != null && t.parent.Find("Cloud lamp base") != null).ToList())
        {
            var lamp = orb.parent; var pos = orb.position; float dia = Mathf.Clamp(orb.lossyScale.x, .3f, .8f);
            var mat = orb.GetComponent<MeshRenderer>().sharedMaterial; var mesh = orb.GetComponent<MeshFilter>().sharedMesh;
            var go = new GameObject("Glow orb " + (++orbs)); go.transform.SetParent(orbRoot, false); go.transform.position = pos; go.transform.localScale = Vector3.one * dia; go.layer = 13;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<SphereCollider>().radius = .5f;
            var rb = go.AddComponent<Rigidbody>(); rb.useGravity = false; rb.isKinematic = false; rb.drag = 2f; rb.angularDrag = 2f;
            var pu = go.AddComponent<VRCPickup>(); pu.InteractionText = "Glow orb"; pu.UseText = "";
            go.AddComponent<VRCObjectSync>();
            var sp = new GameObject("Grab sparkles"); sp.transform.SetParent(go.transform, false);
            var ps = sp.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = true; main.maxParticles = 150; main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .9f); main.startSize = new ParticleSystem.MinMaxCurve(.12f, .3f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .7f, .95f), new Color(.75f, .85f, 1f));
            var em = ps.emission; em.rateOverTime = 28; em.rateOverDistance = 8;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .55f;
            var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, .8f, 1f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) }); col.color = gr;
            var noise = ps.noise; noise.enabled = true; noise.strength = .3f; noise.frequency = .8f;
            var pr = ps.GetComponent<ParticleSystemRenderer>(); if (starMat != null) pr.sharedMaterial = starMat; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var ob = go.AddUdonSharpComponent<AstraGlowOrb>(); ob.sparkles = ps; UdonSharpEditorUtility.CopyProxyToUdon(ob);
            UnityEngine.Object.DestroyImmediate(lamp.gameObject);
        }
        report.Add("lamps removed, glow orbs made grabbable: " + orbs);
        var lsw = lounge.transform.Find("Cloud lounge switch")?.GetComponent<AstraObjectSwitch>();
        if (lsw != null) { lsw.targets = lsw.targets.Where(t => t != null).Concat(new[] { orbRoot.gameObject }).ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(lsw); }

        // bath dish needs the cloud mesh too
        cloudMesh = roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)).First(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("Glitter Cloud")).sharedMesh;
        RebuildBath(lounge, report);
        // back to white / almost-white clouds
        Cols("Lounge Pink", new Color(1.02f, .98f, 1f), new Color(.84f, .8f, .84f), new Color(1.25f, 1.18f, 1.22f));
        Cols("Lounge Lilac", new Color(.98f, .98f, 1.03f), new Color(.8f, .82f, .88f), new Color(1.2f, 1.2f, 1.28f));
        Cols("Lounge Peach", new Color(1.03f, 1f, .97f), new Color(.86f, .83f, .8f), new Color(1.26f, 1.22f, 1.16f));
        AssetDatabase.SaveAssets(); report.Add("pink / lilac / peach cloud colors set to white and near-white");

        // 3. every stretched cloud puff -> cluster of naturally proportioned puffs
        var any = roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)).FirstOrDefault(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("Glitter Cloud"));
        if (any == null) { report.Add("FAIL: cloud mesh not found"); Finish(scene, report); return; }
        cloudMesh = any.sharedMesh;
        int fixedN = 0, made = 0;
        foreach (var mf in roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.sharedMesh == cloudMesh).ToList())
        {
            var ls = mf.transform.lossyScale; float a = Mathf.Abs(ls.x), b = Mathf.Abs(ls.y), c = Mathf.Abs(ls.z);
            if (Mathf.Max(a, Mathf.Max(b, c)) / Mathf.Min(a, Mathf.Min(b, c)) <= 1.3f) continue;
            made += Naturalize(mf); fixedN++;
        }
        int after = roots.Sum(r => r.GetComponentsInChildren<MeshRenderer>(true).Length);
        int left = roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.sharedMesh == cloudMesh).Count(f => { var s = f.transform.lossyScale; float x = Mathf.Abs(s.x), y = Mathf.Abs(s.y), z = Mathf.Abs(s.z); return Mathf.Max(x, Mathf.Max(y, z)) / Mathf.Min(x, Mathf.Min(y, z)) > 1.35f; });
        report.Add("stretched pieces rebuilt: " + fixedN + " -> " + made + " natural puffs; still stretched: " + left + (left == 0 ? " PASS" : " FAIL"));
        report.Add("renderers in lounge + outer clouds: " + before + " -> " + after);
        Finish(scene, report);
    }

    static void Finish(UnityEngine.SceneManagement.Scene scene, List<string> report)
    {
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-cloud-shapes-report.txt", string.Join("\n", report));
        Debug.Log("ASTRA_CLOUD_SHAPES " + string.Join(" | ", report));
    }

    static bool Uniform(Vector3 s) { float a = Mathf.Abs(s.x), b = Mathf.Abs(s.y), c = Mathf.Abs(s.z); return Mathf.Max(a, Mathf.Max(b, c)) / Mathf.Min(a, Mathf.Min(b, c)) < 1.02f; }

    static int Naturalize(MeshFilter mf)
    {
        var t = mf.transform; var mb = cloudMesh.bounds; var ls = t.lossyScale;
        var B = new Vector3(Mathf.Abs(ls.x) * mb.size.x, Mathf.Abs(ls.y) * mb.size.y, Mathf.Abs(ls.z) * mb.size.z);
        var center = t.TransformPoint(mb.center); var rot = t.rotation;
        var mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
        // uniform puff size that fits the thinnest side, grown until the cluster stays small
        float k = Mathf.Min(B.x / mb.size.x, Mathf.Min(B.y / mb.size.y, B.z / mb.size.z)) * 1.15f;
        int nx, ny, nz; Vector3 p;
        while (true)
        {
            p = mb.size * k;
            nx = N(B.x, p.x); ny = N(B.y, p.y); nz = N(B.z, p.z);
            if (nx * ny * nz <= 30) break; k *= 1.15f;
        }
        Transform anc = t.parent; while (anc != null && !Uniform(anc.lossyScale)) anc = anc.parent;
        var shape = new GameObject("Cloud shape").transform; shape.SetParent(anc, true);
        float lift = -(Mathf.Max(0, p.y - B.y)) * .5f; // keep the top surface where it was
        shape.position = center + rot * new Vector3(0, lift, 0); shape.rotation = rot;
        shape.localScale = anc != null ? Vector3.one / Mathf.Abs(anc.lossyScale.x) : Vector3.one;
        int n = 0;
        for (int i = 0; i < nx; i++) for (int j = 0; j < ny; j++) for (int q = 0; q < nz; q++)
                {
                    float fx = nx == 1 ? 0 : i / (float)(nx - 1) - .5f, fy = ny == 1 ? 0 : j / (float)(ny - 1) - .5f, fz = nz == 1 ? 0 : q / (float)(nz - 1) - .5f;
                    var off = new Vector3(fx * Mathf.Max(0, B.x - p.x), fy * Mathf.Max(0, B.y - p.y), fz * Mathf.Max(0, B.z - p.z));
                    off += new Vector3(R(-.12f, .12f) * p.x, R(-.06f, .1f) * p.y, R(-.12f, .12f) * p.z);
                    float s = k * R(.85f, 1.15f);
                    var go = new GameObject("Puff"); go.transform.SetParent(shape, false);
                    go.transform.localRotation = Quaternion.Euler(0, (rng.Next(2) == 0 ? 0 : 180) + R(-20, 20), 0);
                    go.transform.localPosition = off - go.transform.localRotation * (mb.center * s);
                    go.transform.localScale = Vector3.one * s;
                    go.AddComponent<MeshFilter>().sharedMesh = cloudMesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    n++;
                }
        // original keeps its collider (if any) so walking and sitting surfaces don't change
        UnityEngine.Object.DestroyImmediate(mf.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(mf);
        return n;
    }
    static int N(float size, float puff) { return size <= puff ? 1 : Mathf.CeilToInt((size - puff) / (puff * .6f)) + 1; }

    static void EnsureProgram()
    {
        foreach (var name in new[] { "AstraGlowOrb", "AstraDuckFloat" })
        {
            string path = "Assets/Astra/Scripts/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) != null) continue;
            var a = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            a.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Astra/Scripts/" + name + ".cs");
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets();
        }
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
    }

    static void Cols(string name, Color top, Color bottom, Color rim)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Astra/Materials/Lounge/" + name + ".mat"); if (m == null) return;
        m.SetColor("_Top", top); m.SetColor("_Bottom", bottom); m.SetColor("_Rim", rim); EditorUtility.SetDirty(m);
    }

    static Transform Puff(Transform parent, Vector3 pos, float width, Material m)
    {
        float k = width / cloudMesh.bounds.size.x;
        var go = new GameObject("Puff"); go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(0, R(0, 360), 0); go.transform.localScale = Vector3.one * k;
        go.transform.localPosition = pos - go.transform.localRotation * (cloudMesh.bounds.center * k);
        go.AddComponent<MeshFilter>().sharedMesh = cloudMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return go.transform;
    }

    // bathtub -> a white cloud dish 4x the size, water filling it, ducky drifting around
    static void RebuildBath(GameObject lounge, List<string> report)
    {
        var bath = lounge.transform.Find("Bathtub cloud"); var tub = bath == null ? null : bath.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Tub");
        if (tub == null) { report.Add("bath FAIL: tub not found"); return; }
        var plat = bath.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "Cloud bath platform");
        var white = plat != null ? plat.sharedMaterial : null;
        var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/Astra/Materials/Lounge/Bath Water.mat");
        var cyl = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
        float oldRx = 1.3f * Mathf.Abs(tub.lossyScale.x), oldRz = .75f * Mathf.Abs(tub.lossyScale.x);
        float rx = oldRx * 4f, rz = oldRz * 4f;
        var dish = new GameObject("Bath dish").transform; dish.SetParent(bath, false); dish.position = tub.position; dish.rotation = tub.rotation;
        // shallow bowl: a floor of puffs, then two rising rings that flare outward
        float floorW = Mathf.Max(1.4f, rx * .32f);
        for (float x = -rx * .8f; x <= rx * .8f; x += floorW * .7f) for (float z = -rz * .8f; z <= rz * .8f; z += floorW * .7f)
                if ((x * x) / (rx * rx) + (z * z) / (rz * rz) < .7f) Puff(dish, new Vector3(x + R(-.2f, .2f), R(-.05f, .1f), z + R(-.2f, .2f)), floorW * R(.9f, 1.15f), white);
        foreach (var ring in new[] { new Vector3(.92f, .45f, 1f), new Vector3(1.06f, 1.05f, .9f) })
        {
            float w = floorW * ring.z, circ = Mathf.PI * (rx + rz) * ring.x; int n = Mathf.CeilToInt(circ / (w * .6f));
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2 / n; Puff(dish, new Vector3(Mathf.Cos(a) * rx * ring.x, ring.y + R(-.08f, .12f), Mathf.Sin(a) * rz * ring.x), w * R(.9f, 1.15f), white); }
        }
        // water filling the dish up to just under the rim
        float waterY = 1.15f;
        var wgo = new GameObject("Bath water"); wgo.transform.SetParent(dish, false); wgo.transform.localPosition = new Vector3(0, waterY, 0);
        wgo.transform.localScale = new Vector3(rx * 2f * .98f, .015f, rz * 2f * .98f);
        wgo.AddComponent<MeshFilter>().sharedMesh = cyl; var wr = wgo.AddComponent<MeshRenderer>(); wr.sharedMaterial = water; wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var deep = new GameObject("Bath water depth"); deep.transform.SetParent(dish, false); deep.transform.localPosition = new Vector3(0, waterY * .5f + .1f, 0);
        deep.transform.localScale = new Vector3(rx * 2f * .9f, waterY * .5f - .05f, rz * 2f * .9f);
        deep.AddComponent<MeshFilter>().sharedMesh = cyl; var dr = deep.AddComponent<MeshRenderer>(); dr.sharedMaterial = water; dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // you can wade in: a floor just above the cloud bottom
        var floor = new GameObject("Bath floor collider"); floor.transform.SetParent(dish, false); floor.transform.localPosition = new Vector3(0, .35f, 0);
        floor.AddComponent<BoxCollider>().size = new Vector3(rx * 1.6f, .1f, rz * 1.6f);
        // ducky: 4x, floating at the water line and drifting around
        var duck = tub.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Rubber ducky");
        if (duck != null)
        {
            float ds = Mathf.Abs(duck.lossyScale.x) * 4f; duck.SetParent(dish, true);
            duck.localScale = Vector3.one * ds; duck.localRotation = Quaternion.identity; duck.localPosition = new Vector3(0, waterY - .09f * ds, 0);
            var f = duck.gameObject.AddUdonSharpComponent<AstraDuckFloat>(); f.rx = rx * .75f; f.rz = rz * .75f; f.speed = .08f; f.bob = .05f * ds; UdonSharpEditorUtility.CopyProxyToUdon(f);
        }
        var bubbles = tub.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(p => p.name == "Bath bubbles");
        if (bubbles != null)
        {
            bubbles.transform.SetParent(dish, true); bubbles.transform.localScale = Vector3.one; bubbles.transform.localPosition = new Vector3(0, waterY, 0);
            var sh = bubbles.shape; sh.scale = new Vector3(rx * 1.7f, .05f, rz * 1.7f); var main = bubbles.main; main.maxParticles = 160; var em = bubbles.emission; em.rateOverTime = 30;
        }
        UnityEngine.Object.DestroyImmediate(tub.gameObject);
        report.Add("bath: white cloud dish " + (rx * 2).ToString("0.0") + " x " + (rz * 2).ToString("0.0") + " m (4x), filled with water, ducky drifting" + (duck == null ? " (duck FAIL)" : ""));
    }

    [MenuItem("Astra/Claude/Restore scene from before cloud shapes")]
    public static void Restore()
    {
        if (!File.Exists(Backup) || !EditorUtility.DisplayDialog("Restore", "Put the scene back to before the cloud shapes pass?", "Restore", "Cancel")) return;
        var path = EditorSceneManager.GetActiveScene().path; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); EditorSceneManager.OpenScene(path);
    }
}
