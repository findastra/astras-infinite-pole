// Swaying crystal chandeliers (Claude, 2026-09-26). Menu: Astra > Claude > 10 Swaying crystal chandeliers (run after 9).
// Rebuilds every hanging crystal vine as 3-link strands that sway like hair, adds more strands per cloud (no new clouds) and
// white falling sparkles. Rollback: Astra > Claude > Restore scene from before chandeliers.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraClaudeChandeliers
{
    const string Backup = "Review/Backups/BeforeChandeliers.unity.txt";
    const string Root = "Assets/Astra/";
    static System.Random rng; static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    static Mesh cloudMesh, gem; static Material white, star; static Material[] crystal;
    static readonly float[] Lengths = { 1f, 1.5f, 2f };
    static Mesh[,,] variants; // length, pattern, tip

    [MenuItem("Astra/Claude/10 Swaying crystal chandeliers")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var vines = GameObject.Find("13 - Cloud lounge (Claude)")?.transform.Find("Crystal vine clouds");
        if (vines == null) { EditorUtility.DisplayDialog("Chandeliers", "Crystal vine clouds not found.", "OK"); return; }
        EditorSceneManager.SaveScene(scene); File.Copy(scene.path, Backup, true);
        rng = new System.Random(1501); var report = new List<string>();
        EnsureProgram();
        gem = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Meshes/Lounge/Crystal Gem.asset");
        crystal = new[] { "Crystal Rose", "Crystal Lilac", "Crystal Aqua" }.Select(n => AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Lounge/" + n + ".mat")).ToArray();
        star = GameObject.Find("10 - Stair opening star dust")?.GetComponent<ParticleSystemRenderer>()?.sharedMaterial;
        var anyCloud = vines.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("Glitter Cloud"));
        cloudMesh = anyCloud.sharedMesh; white = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Lounge/Lounge Lilac.mat") ?? anyCloud.GetComponent<MeshRenderer>().sharedMaterial;
        BuildVariants();

        // more chandelier clouds, placed where nothing else is
        int added = 0;
        for (int tries = 0; tries < 400 && added < 0; tries++)
        {
            float ang = R(0, Mathf.PI * 2), rad = R(13, 40); var p = new Vector3(Mathf.Cos(ang) * rad, R(7f, 18f), Mathf.Sin(ang) * rad);
            if (Physics.CheckSphere(p, 5.5f, ~0, QueryTriggerInteraction.Collide) || Physics.CheckSphere(p + Vector3.down * 4, 3f, ~0, QueryTriggerInteraction.Collide)) continue;
            if (vines.Cast<Transform>().Any(c => Vector3.Distance(c.position, p) < 9f)) continue;
            var g = new GameObject("Crystal vine cloud " + (vines.childCount + 1)).transform; g.SetParent(vines, false); g.position = p; g.rotation = Quaternion.Euler(0, R(0, 360), 0);
            float w = R(4.5f, 7.5f);
            for (int i = 0; i < 5; i++) Puff(g, new Vector3(R(-w * .3f, w * .3f), R(-.2f, .5f), R(-w * .25f, w * .25f)), w * R(.45f, .65f));
            var col = new GameObject("Cloud walk collider"); col.transform.SetParent(g, false); col.transform.localPosition = new Vector3(0, .5f, 0); col.AddComponent<BoxCollider>().size = new Vector3(w * .8f, .3f, w * .6f);
            Physics.SyncTransforms(); added++;
        }
        report.Add("new chandelier clouds: " + added);

        // every chandelier: old baked vines out, swaying 3-link strands in, white sparkles
        int strands = 0, links = 0;
        foreach (Transform c in vines)
        {
            foreach (var old in c.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Crystal vines" || t.name == "Crystal glints" || t.name == "Chandelier strands" || t.name == "Chandelier sparkles").ToList()) if (old != null) Object.DestroyImmediate(old.gameObject);
            var rs = c.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(c.position, Vector3.one * 5); foreach (var r in rs) b.Encapsulate(r.bounds);
            var root = new GameObject("Chandelier strands").transform; root.SetParent(c, false); root.position = new Vector3(b.center.x, b.min.y + .25f, b.center.z); root.rotation = Quaternion.identity;
            var segs = new List<Transform>(); var ph = new List<float>(); var dep = new List<int>();
            int n = rng.Next(18, 27); float hx = b.extents.x * .75f, hz = b.extents.z * .75f;
            for (int s = 0; s < n; s++)
            {
                float a = R(0, Mathf.PI * 2), rr = Mathf.Sqrt(R(.05f, 1f));
                Transform parent = root; var at = new Vector3(Mathf.Cos(a) * hx * rr, 0, Mathf.Sin(a) * hz * rr);
                int mi = rng.Next(3), pattern = rng.Next(4); float phase = R(0, 6.3f);
                for (int d = 0; d < 3; d++)
                {
                    int li = rng.Next(3); bool tip = d == 2;
                    var seg = new GameObject("Link " + d).transform; seg.SetParent(parent, false); seg.localPosition = at;
                    seg.gameObject.AddComponent<MeshFilter>().sharedMesh = variants[li, (pattern + d) % 4, tip ? 1 : 0];
                    var mr = seg.gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = crystal[mi]; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                    segs.Add(seg); ph.Add(phase); dep.Add(d); links++;
                    parent = seg; at = new Vector3(0, -Lengths[li], 0);
                }
                strands++;
            }
            var sway = root.gameObject.AddUdonSharpComponent<AstraVineSway>(); sway.segs = segs.ToArray(); sway.phase = ph.ToArray(); sway.depth = dep.ToArray();
            sway.amp = R(5.5f, 8.5f); sway.freq = R(.55f, .85f); UdonSharpEditorUtility.CopyProxyToUdon(sway);
            // white sparkles drifting down out of the crystals
            var sp = new GameObject("Chandelier sparkles"); sp.transform.SetParent(c, false); sp.transform.position = root.position + Vector3.down * 2.8f;
            var ps = sp.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.prewarm = true; main.maxParticles = 70; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.2f); main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.05f, .15f); main.startColor = new Color(1f, 1f, 1f, .95f);
            var em = ps.emission; em.rateOverTime = 22; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(hx * 2f, 5f, hz * 2f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = new ParticleSystem.MinMaxCurve(-.05f, .05f); vel.y = new ParticleSystem.MinMaxCurve(-.35f, -.12f); vel.z = new ParticleSystem.MinMaxCurve(-.05f, .05f);
            var colr = ps.colorOverLifetime; colr.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(1, .7f), new GradientAlphaKey(0, 1) }); colr.color = gr;
            var pr = ps.GetComponent<ParticleSystemRenderer>(); if (star != null) pr.sharedMaterial = star; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        report.Add("chandeliers: " + vines.childCount + " clouds, " + strands + " swaying strands, " + links + " links, white sparkles on each");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-chandeliers-report.txt", string.Join("\n", report));
        Debug.Log("ASTRA_CHANDELIERS " + string.Join(" | ", report));
    }

    static void Puff(Transform parent, Vector3 pos, float width)
    {
        float k = width / cloudMesh.bounds.size.x;
        var go = new GameObject("Puff"); go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(0, R(0, 360), 0); go.transform.localScale = Vector3.one * k;
        go.transform.localPosition = pos - go.transform.localRotation * (cloudMesh.bounds.center * k);
        go.AddComponent<MeshFilter>().sharedMesh = cloudMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = white;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
    }

    // a few shared link meshes: 3 lengths x 4 gem patterns x (with / without a teardrop tip)
    static void BuildVariants()
    {
        variants = new Mesh[3, 4, 2];
        for (int li = 0; li < 3; li++) for (int pt = 0; pt < 4; pt++) for (int tip = 0; tip < 2; tip++)
                {
                    var r2 = new System.Random(li * 100 + pt * 10 + tip); System.Func<float, float, float> Q = (a, b) => a + (float)r2.NextDouble() * (b - a);
                    var list = new List<CombineInstance>(); float L = Lengths[li], ph = Q(0, 6.3f), sway = Q(.03f, .09f);
                    for (float t = .05f; t < L; t += .2f)
                    {
                        var pos = new Vector3(Mathf.Sin(t * 2.1f + ph) * sway, -t, Mathf.Cos(t * 1.7f + ph) * sway * .7f); float s = Q(.07f, .11f);
                        list.Add(new CombineInstance { mesh = gem, transform = Matrix4x4.TRS(pos, Quaternion.Euler(Q(-15, 15), Q(0, 360), Q(-15, 15)), Vector3.one * s) });
                        if (r2.NextDouble() < .55) { var side = Quaternion.Euler(0, Q(0, 360), 0) * Vector3.right; list.Add(new CombineInstance { mesh = gem, transform = Matrix4x4.TRS(pos + side * .08f, Quaternion.LookRotation(side) * Quaternion.Euler(70, 0, 0), new Vector3(.9f, .35f, .5f) * s) }); }
                    }
                    if (tip == 1) list.Add(new CombineInstance { mesh = gem, transform = Matrix4x4.TRS(new Vector3(0, -L - .12f, 0), Quaternion.identity, new Vector3(.2f, .3f, .2f)) });
                    var m = new Mesh(); m.CombineMeshes(list.ToArray(), true, true); m.RecalculateBounds();
                    string path = Root + "Meshes/Lounge/Chandelier link " + li + "-" + pt + "-" + tip + ".asset";
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                    m.name = Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(m, path); variants[li, pt, tip] = m;
                }
    }

    static void EnsureProgram()
    {
        const string path = Root + "Scripts/AstraVineSway.asset";
        if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) == null)
        {
            var a = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            a.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "Scripts/AstraVineSway.cs");
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets();
        }
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
    }

    [MenuItem("Astra/Claude/Restore scene from before chandeliers")]
    public static void Restore()
    {
        if (!File.Exists(Backup) || !EditorUtility.DisplayDialog("Restore", "Put the scene back to before the chandeliers pass?", "Restore", "Cancel")) return;
        var path = EditorSceneManager.GetActiveScene().path; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); EditorSceneManager.OpenScene(path);
    }
}
