// Cozy clouds pass (Claude, 2026-09-26). Menu: Astra > Claude > 11 Platforms, sparkles, easter eggs (+ chandeliers).
// Discards any unsaved half-run, runs the swaying chandeliers, removes stepping clouds, prunes overlapping puffs,
// adds an invisible jump platform under every floating cloud (1.5 m bigger all round), fills every lounge cloud with
// sparkles and hides simple easter eggs (stickers / patterned eggs) inside. Rollback: Restore scene from before cozy clouds.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraClaudeCozy
{
    const string Backup = "Review/Backups/BeforeCozyClouds.unity.txt";
    const string Root = "Assets/Astra/";
    static System.Random rng; static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    static Mesh cloudMesh, quad, sphere; static Material star; static Material[] stickerMats, eggMats;

    [MenuItem("Astra/Claude/11 Platforms, sparkles, easter eggs (+ chandeliers)")]
    public static void Run()
    {
        var path = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single); // throw away any unsaved half-finished run
        File.Copy(path, Backup, true);
        AstraClaudeChandeliers.Run();
        var scene = EditorSceneManager.GetActiveScene();
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)").transform;
        rng = new System.Random(3141); var report = new List<string>();
        cloudMesh = lounge.GetComponentsInChildren<MeshFilter>(true).First(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("Glitter Cloud")).sharedMesh;
        quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx"); sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        star = GameObject.Find("10 - Stair opening star dust")?.GetComponent<ParticleSystemRenderer>()?.sharedMaterial;
        MakeEasterMaterials();
        int puffsBefore = Puffs(lounge);

        // 1. stepping clouds out
        int stones = 0; foreach (var s in lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Stepping clouds").ToList()) { if (s == null) continue; stones += s.childCount; UnityEngine.Object.DestroyImmediate(s.gameObject); }
        report.Add("stepping clouds removed: " + stones);

        // 2. prune puffs that mostly sit inside a bigger neighbour
        int pruned = 0;
        foreach (var shape in lounge.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Cloud shape").ToList())
        {
            var ps = shape.Cast<Transform>().Where(p => p.GetComponent<MeshFilter>() != null).OrderByDescending(p => p.localScale.x).ToList(); var kept = new List<Transform>();
            foreach (var p in ps)
            {
                var c = p.GetComponent<Renderer>().bounds.center;
                if (kept.Any(k => Vector3.Distance(k.GetComponent<Renderer>().bounds.center, c) < .45f * k.GetComponent<Renderer>().bounds.size.x)) { UnityEngine.Object.DestroyImmediate(p.gameObject); pruned++; }
                else kept.Add(p);
            }
        }
        report.Add("overlapping puffs pruned: " + pruned + " (lounge cloud pieces " + puffsBefore + " -> " + Puffs(lounge) + ")");

        // targets: every furniture / amenity / bath group and every chandelier cloud
        var groups = new List<Transform>();
        foreach (var n in new[] { "Cloud furniture", "Cloud amenities", "Crystal vine clouds" }) { var t = lounge.Find(n); if (t != null) groups.AddRange(t.Cast<Transform>()); }
        var bath = lounge.Find("Bathtub cloud"); if (bath != null) groups.Add(bath);
        int plats = 0, sparkles = 0, eggs = 0;
        foreach (var g in groups)
        {
            var clouds = g.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<MeshFilter>()?.sharedMesh == cloudMesh).ToArray();
            if (clouds.Length == 0) continue;
            var b = clouds[0].bounds; foreach (var r in clouds) b.Encapsulate(r.bounds);
            bool floating = g.Find("Lift pad") != null || g.parent.name == "Crystal vine clouds";
            // 3. invisible jump platform under the cloud, a little bigger than it
            if (floating && b.min.y - .55f > .6f)
            {
                var old = g.Find("Jump platform"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var pl = new GameObject("Jump platform"); pl.transform.SetParent(g, true); pl.transform.position = new Vector3(b.center.x, b.min.y - .55f, b.center.z); pl.transform.rotation = Quaternion.identity; pl.transform.localScale = Vector3.one;
                var bc = pl.AddComponent<BoxCollider>(); var ls = pl.transform.lossyScale; bc.size = new Vector3((b.size.x + 3f) / ls.x, .3f / ls.y, (b.size.z + 3f) / ls.z);
                plats++;
            }
            // 4. sparkles filling the inside of the cloud
            var sg = new GameObject("Cloud sparkles"); sg.transform.SetParent(g, true); sg.transform.position = b.center; sg.transform.rotation = Quaternion.identity;
            var ps = sg.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            float vol = b.size.x * b.size.y * b.size.z;
            var main = ps.main; main.loop = true; main.prewarm = true; main.maxParticles = 70; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2.6f); main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.04f, .13f); main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, .95f, 1f));
            var em = ps.emission; em.rateOverTime = Mathf.Clamp(vol * .6f, 10f, 30f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = b.size * .8f;
            var noise = ps.noise; noise.enabled = true; noise.strength = .15f; noise.frequency = .5f;
            var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(1, .7f), new GradientAlphaKey(0, 1) }); col.color = gr;
            var pr = ps.GetComponent<ParticleSystemRenderer>(); if (star != null) pr.sharedMaterial = star; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sparkles++;
            // 5. easter eggs tucked inside the puffs
            int n = rng.Next(1, 4);
            for (int i = 0; i < n; i++)
            {
                var host = clouds[rng.Next(clouds.Length)].bounds; var e = host.extents;
                var pos = host.center + new Vector3(R(-.3f, .3f) * e.x, R(-.15f, .25f) * e.y, R(-.3f, .3f) * e.z);
                var egg = rng.NextDouble() < .55 ? Sticker(g) : Egg(g);
                egg.position = pos; egg.rotation = Quaternion.Euler(R(-25, 25), R(0, 360), R(-25, 25)); eggs++;
            }
        }
        report.Add("jump platforms: " + plats + ", sparkle-filled clouds: " + sparkles + ", easter eggs hidden: " + eggs);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-cozy-report.txt", string.Join("\n", report));
        Debug.Log("ASTRA_COZY " + string.Join(" | ", report));
    }

    static int Puffs(Transform t) { return t.GetComponentsInChildren<MeshFilter>(true).Count(f => f.sharedMesh == cloudMesh); }

    static Transform Sticker(Transform parent)
    {
        var go = new GameObject("Easter egg sticker"); go.transform.SetParent(parent, false); float s = R(.28f, .45f);
        var m = stickerMats[rng.Next(stickerMats.Length)];
        for (int side = 0; side < 2; side++)
        {
            var q = new GameObject(side == 0 ? "Front" : "Back"); q.transform.SetParent(go.transform, false); q.transform.localRotation = Quaternion.Euler(0, side * 180, 0);
            q.transform.localScale = Vector3.one * s / Mathf.Max(.001f, go.transform.lossyScale.x);
            q.AddComponent<MeshFilter>().sharedMesh = quad; var r = q.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return go.transform;
    }
    static Transform Egg(Transform parent)
    {
        var go = new GameObject("Easter egg"); go.transform.SetParent(parent, false); float s = R(.18f, .28f) / Mathf.Max(.001f, go.transform.lossyScale.x);
        go.transform.localScale = new Vector3(s, s * 1.3f, s);
        go.AddComponent<MeshFilter>().sharedMesh = sphere; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = eggMats[rng.Next(eggMats.Length)]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    // ---------------- procedural sticker + egg textures
    static readonly Color[] Palette = { new Color(1f, .45f, .7f), new Color(.55f, .75f, 1f), new Color(1f, .85f, .3f), new Color(.6f, .95f, .7f), new Color(.8f, .6f, 1f), new Color(1f, .6f, .45f) };
    static void MakeEasterMaterials()
    {
        Directory.CreateDirectory(Root + "Textures/Easter"); Directory.CreateDirectory(Root + "Materials/Easter");
        Func<float, float, bool>[] shapes = {
            (x, y) => { x *= 1.3f; y = y * 1.3f + .15f; float a = x * x + y * y - .3f; return a * a * a - x * x * y * y * y < 0; },          // heart
            (x, y) => { float r = Mathf.Sqrt(x * x + y * y), t = Mathf.Atan2(y, x); return r < .28f + .2f * Mathf.Pow(Mathf.Abs(Mathf.Cos(2.5f * t)), 3); }, // star
            (x, y) => x * x + y * y < .2f,                                                                                                   // smiley base
            (x, y) => x * x + y * y < .2f && (x - .18f) * (x - .18f) + (y - .1f) * (y - .1f) > .14f,                                        // moon
            (x, y) => (x * x + (y + .12f) * (y + .12f) < .06f) || Toe(x, y, -.26f, .12f) || Toe(x, y, -.1f, .27f) || Toe(x, y, .1f, .27f) || Toe(x, y, .26f, .12f), // paw
            (x, y) => { float r = Mathf.Sqrt(x * x + (y + .2f) * (y + .2f)); return y > -.2f && r < .45f && r > .18f; },                    // rainbow
            (x, y) => Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y)) < .7f };                                                         // sparkle
        string[] names = { "Heart", "Star", "Smiley", "Moon", "Paw", "Rainbow", "Sparkle" };
        stickerMats = new Material[shapes.Length];
        for (int i = 0; i < shapes.Length; i++)
        {
            var fill = Palette[i % Palette.Length]; const int N = 256; var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N * 2 - 1, v = y / (float)N * 2 - 1; Color c = new Color(0, 0, 0, 0);
                    if (shapes[i](u / .88f, v / .88f)) c = Color.white;                    // white sticker border
                    if (shapes[i](u, v))
                    {
                        c = fill;
                        if (names[i] == "Rainbow") { float r = Mathf.Sqrt(u * u + (v + .2f) * (v + .2f)); c = Color.HSVToRGB(Mathf.Repeat((r - .18f) / .27f * .8f, 1), .55f, 1); }
                        if (names[i] == "Smiley" && (((u + .15f) * (u + .15f) + (v - .1f) * (v - .1f) < .004f) || ((u - .15f) * (u - .15f) + (v - .1f) * (v - .1f) < .004f) || (Mathf.Abs(Mathf.Sqrt(u * u + (v + .02f) * (v + .02f)) - .22f) < .025f && v < -.08f))) c = new Color(.25f, .12f, .2f);
                        if (u * .7f + v > .35f && u * .7f + v < .42f) c = Color.Lerp(c, Color.white, .6f); // glossy shine line
                    }
                    px[y * N + x] = c;
                }
            stickerMats[i] = SaveMat("Sticker " + names[i], px, N, N, "Unlit/Transparent");
        }
        string[] patterns = { "Stripes", "Dots", "Zigzag", "Checker", "Rainbow", "Confetti" };
        eggMats = new Material[patterns.Length];
        for (int i = 0; i < patterns.Length; i++)
        {
            Color a = Palette[i % Palette.Length], b2 = Palette[(i + 2) % Palette.Length], w = new Color(1f, .97f, .95f); const int W = 256, H = 128; var px = new Color[W * H];
            var r2 = new System.Random(i * 7 + 1);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)H; Color c = w;
                    switch (patterns[i])
                    {
                        case "Stripes": c = ((int)(v * 10)) % 2 == 0 ? a : w; break;
                        case "Dots": { float fu = Mathf.Repeat(u * 12, 1) - .5f, fv = Mathf.Repeat(v * 6, 1) - .5f; c = fu * fu + fv * fv < .08f ? a : b2 * .4f + w * .6f; break; }
                        case "Zigzag": c = Mathf.Abs(Mathf.Repeat(v * 5 + Mathf.Abs(Mathf.Repeat(u * 16, 2) - 1) * .5f, 1) - .5f) < .18f ? a : w; break;
                        case "Checker": c = (((int)(u * 12) + (int)(v * 6)) % 2 == 0) ? a : b2; break;
                        case "Rainbow": c = Color.HSVToRGB(v * .85f, .5f, 1); break;
                        case "Confetti": c = w; break;
                    }
                    px[y * W + x] = c;
                }
            if (patterns[i] == "Confetti") for (int k = 0; k < 140; k++) { int cx = r2.Next(W), cy = r2.Next(H); var cc = Palette[r2.Next(Palette.Length)]; for (int dy = -2; dy <= 2; dy++) for (int dx = -3; dx <= 3; dx++) px[Mathf.Clamp(cy + dy, 0, H - 1) * W + (cx + dx + W) % W] = cc; }
            eggMats[i] = SaveMat("Egg " + patterns[i], px, W, H, "Unlit/Texture");
        }
    }
    static bool Toe(float x, float y, float cx, float cy) { return (x - cx) * (x - cx) + (y - cy) * (y - cy) < .012f; }
    static Material SaveMat(string name, Color[] px, int w, int h, string shader)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true); tex.SetPixels(px); tex.Apply();
        string tp = Root + "Textures/Easter/" + name + ".png"; File.WriteAllBytes(tp, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(tp); var ti = (TextureImporter)AssetImporter.GetAtPath(tp); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        string mp = Root + "Materials/Easter/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, mp); }
        m.shader = Shader.Find(shader); m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(tp); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }

    [MenuItem("Astra/Claude/Restore scene from before cozy clouds")]
    public static void Restore()
    {
        if (!File.Exists(Backup) || !EditorUtility.DisplayDialog("Restore", "Put the scene back to before the platforms / sparkles / easter eggs pass?", "Restore", "Cancel")) return;
        var path = EditorSceneManager.GetActiveScene().path; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); EditorSceneManager.OpenScene(path);
    }
}
