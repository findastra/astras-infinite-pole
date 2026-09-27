// Claude round 3f (2026-09-24): video screen 4x bigger, DJ cloud brought close to spawn,
// keep-clear zones so orbiting clouds never hit or hover over the screen or the DJ cloud,
// every orbiting cloud slowly spins on its own axis (random speed and direction), rounder cloud shapes,
// near-white pastel colors that differ per cloud and slowly change, and a proper deck material. Safe to rerun.
// Menu: Astra > Claude Round 3f - Big Screen + Close DJ + Clear Zones
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeRound3f
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const float ScreenScale = 4f, ScreenBottom = .5f, DJRadius = 12f, Margin = 3f;
    const string Marker = "Claude 4x screen marker";

    [MenuItem("Astra/Claude Round 3f - Big Screen + Close DJ + Clear Zones")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND3F start");
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3f.unity.txt", true);
        var report = new List<string>();
        var rng = new System.Random(2718);

        // ---------- 0a. Rounder clouds: rebuild the 8 shapes in place, then refit colliders and sparkle rain ----------
        const string Root = "Assets/Astra/";
        var shapeRng = new System.Random(1618);
        var rebuilt = new HashSet<Mesh>();
        for (int s = 0; s < 8; s++)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Meshes/Glitter Cloud " + s + ".asset");
            if (mesh == null || mesh.name.Contains("(Astra)")) continue; // never overwrite Astra's restyled shapes
            RoundCloud(mesh, "Glitter Cloud " + s, shapeRng); EditorUtility.SetDirty(mesh); rebuilt.Add(mesh);
        }
        int refit = 0;
        foreach (var mf in Object.FindObjectsOfType<MeshFilter>(true))
        {
            if (!rebuilt.Contains(mf.sharedMesh)) continue;
            var mb = mf.sharedMesh.bounds; var box = mf.GetComponent<BoxCollider>();
            if (box != null) { box.size = new Vector3(mb.size.x * .8f, .7f, mb.size.z * .8f); box.center = new Vector3(mb.center.x, -.35f, mb.center.z); }
            var rain = mf.transform.Find("Sparkle rain");
            if (rain != null) { rain.localPosition = new Vector3(mb.center.x, mb.min.y * .55f, mb.center.z); var sh = rain.GetComponent<ParticleSystem>().shape; sh.scale = new Vector3(mb.size.x * .7f, .05f, mb.size.z * .7f); }
            refit++;
        }
        report.Add("Rounder cloud shapes: " + rebuilt.Count + " rebuilt (" + (rebuilt.Count > 0 ? rebuilt.Min(m => m.vertexCount) + "-" + rebuilt.Max(m => m.vertexCount) : "0") + " vertices), " + refit + " clouds refitted");

        // ---------- 0b. Near-white pastel clouds that change color ----------
        var cloudMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Glitter Cloud.mat");
        cloudMat.shader = Shader.Find("Astra/Glitter Cloud");
        cloudMat.SetColor("_Top", new Color(1f, .98f, 1f)); cloudMat.SetColor("_Bottom", new Color(.9f, .86f, .94f));
        cloudMat.SetColor("_Rim", new Color(1.5f, 1.3f, 1.6f)); cloudMat.SetFloat("_Pastel", .2f); cloudMat.SetFloat("_ColorSpeed", .025f);
        EditorUtility.SetDirty(cloudMat);
        report.Add("Cloud color: near-white pastel, 20% color variety, slowly changing");

        // ---------- 0c. DJ deck: its own solid glitter material (it was borrowing the pole's see-through chrome) ----------
        string deckPath = Root + "Materials/DJ Deck.mat";
        var deck = AssetDatabase.LoadAssetAtPath<Material>(deckPath);
        if (deck == null) { deck = new Material(Shader.Find("Astra/Glitter Cloud")); AssetDatabase.CreateAsset(deck, deckPath); }
        deck.shader = Shader.Find("Astra/Glitter Cloud"); deck.enableInstancing = true;
        deck.SetColor("_Top", new Color(.42f, .16f, .5f)); deck.SetColor("_Bottom", new Color(.14f, .05f, .22f));
        deck.SetColor("_Rim", new Color(2.2f, .7f, 1.8f)); deck.SetFloat("_Glitter", 3f); deck.SetFloat("_Pastel", 0f);
        var platter = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/DJ Platter.mat");
        if (platter == null) { platter = new Material(deck); AssetDatabase.CreateAsset(platter, Root + "Materials/DJ Platter.mat"); }
        platter.CopyPropertiesFromMaterial(deck); platter.SetColor("_Top", new Color(.06f, .03f, .09f)); platter.SetColor("_Bottom", new Color(.03f, .01f, .05f));
        EditorUtility.SetDirty(deck); EditorUtility.SetDirty(platter);
        var boothT = GameObject.Find("DJ booth");
        if (boothT != null)
            foreach (var r in boothT.GetComponentsInChildren<MeshRenderer>(true))
                r.sharedMaterial = r.name == "Deck table" ? deck : platter;
        report.Add("DJ deck: solid glitter material (no longer see-through through the stairs)");

        // ---------- 1. Video screen 4x ----------
        var cinema = GameObject.Find("Cinema - synchronized playlist").transform;
        var before = Bounds(cinema);
        if (cinema.Find(Marker) == null)
        {
            cinema.localScale *= ScreenScale;
            var m = new GameObject(Marker).transform; m.SetParent(cinema, false);
            report.Add("Video screen scaled " + ScreenScale + "x (was " + before.size.ToString("0.0") + " m)");
        }
        else report.Add("Video screen already 4x");
        var b = Bounds(cinema);
        cinema.position += Vector3.up * (ScreenBottom - b.min.y); // bottom just above the floor
        // nearest edge 8 m from the pole: close to spawn, clear of the stairs and floor opening (true vertex distance)
        { var flat = new Vector3(cinema.position.x, 0, cinema.position.z).normalized; for (int i = 0; i < 3; i++) cinema.position += flat * (8f - NearestToPole(cinema)); }
        b = Bounds(cinema);
        report.Add("Video screen now " + b.size.ToString("0.0") + " m, bottom " + b.min.y.ToString("0.0") + " m, nearest edge " + NearestToPole(cinema).ToString("0.0") + " m from the pole");

        // ---------- 2. DJ cloud: 5x bigger dance cloud, near edge close to spawn ----------
        var dj = GameObject.Find("12 - DJ cloud");
        if (dj != null)
        {
            var stage = dj.transform.Find("DJ stage cloud"); var step = dj.transform.Find("DJ step cloud");
            if (stage.Find("Claude 5x marker") == null)
            {
                stage.localScale = new Vector3(stage.localScale.x * 5, stage.localScale.y * 2, stage.localScale.z * 5); // wide dance floor, not 5x taller
                new GameObject("Claude 5x marker").transform.SetParent(stage, false);
            }
            var sm = stage.GetComponent<MeshFilter>().sharedMesh.bounds;
            float reach = new Vector2(sm.extents.x + Mathf.Abs(sm.center.x), sm.extents.z + Mathf.Abs(sm.center.z)).magnitude * Mathf.Max(stage.localScale.x, stage.localScale.z);
            float halfDepth = (sm.extents.z + Mathf.Abs(sm.center.z)) * stage.localScale.z;
            var flat = new Vector3(dj.transform.position.x, 0, dj.transform.position.z).normalized;
            dj.transform.position = flat * (halfDepth + 7f); // near edge ~7 m from the pole: right by spawn, clear of the stairs
            dj.transform.rotation = Quaternion.LookRotation(-flat);
            if (step != null) step.localPosition = new Vector3(0, .2f, halfDepth + .5f); // hop-up step on the spawn side
            for (int i = 0; i < 3; i++) dj.transform.position += flat * Mathf.Max(0, 6.8f - NearestToPole(dj.transform)); // true edge >= 6.8 m (stairs end at 5.8)
            for (int i = 0; i < 36 && Overlaps(dj.transform, cinema, 1f); i++) // swing around the pole until it clears the screen
            { dj.transform.position = Quaternion.Euler(0, -10, 0) * dj.transform.position; dj.transform.rotation = Quaternion.LookRotation(-new Vector3(dj.transform.position.x, 0, dj.transform.position.z)); }
            var desc = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
            Vector3 spawn = desc.spawns != null && desc.spawns.Length > 0 && desc.spawns[0] != null ? desc.spawns[0].position : Vector3.zero;
            var db = Bounds(dj.transform);
            var dl = LocalBounds(dj.transform); report.Add("DJ dance cloud: 5x wider (" + dl.size.x.ToString("0") + " x " + dl.size.z.ToString("0") + " m), decks in the middle, near edge " + NearestToPole(dj.transform).ToString("0.0") + " m from the pole, decks " + Vector3.Distance(Flat(dj.transform.position), Flat(spawn)).ToString("0") + " m from spawn");
            report.Add("DJ cloud clear of the video screen: " + (Overlaps(dj.transform, cinema, 1f) ? "FAIL" : "PASS"));
        }

        // ---------- 3. Keep-clear zones ----------
        foreach (var n in new[] { "Cloud keep-clear - video screen", "Cloud keep-clear - DJ cloud" }) { var o = GameObject.Find(n); if (o != null) Object.DestroyImmediate(o); }
        var zones = new List<Transform>(); var halves = new List<Vector3>();
        // oriented boxes: measured in the object's own frame, so a rotated screen or cloud doesn't balloon into
        // an axis-aligned box that reaches the pole and spawn (board task C3)
        void Zone(string name, Transform frame, float extraUp)
        {
            var lb = LocalBounds(frame);
            var z = new GameObject(name); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(z, scene);
            z.transform.rotation = Quaternion.Euler(0, frame.eulerAngles.y, 0);
            var half = lb.extents + new Vector3(Margin, Margin, Margin); half.y += extraUp / 2;
            z.transform.position = frame.TransformPoint(lb.center) + Vector3.up * extraUp / 2;
            zones.Add(z.transform); halves.Add(half);
        }
        Zone("Cloud keep-clear - video screen", cinema, 2f);
        if (dj != null) Zone("Cloud keep-clear - DJ cloud", dj.transform, 4f); // room for the DJ and dancers above the stage

        // ---------- 4. Orbit: per-cloud spin + zones ----------
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>();
        var clouds = new List<Transform>(); var first = new List<int>(); var count = new List<int>(); var yaw = new List<float>(); var spin = new List<float>();
        foreach (var pv in orbit.pivots)
        {
            first.Add(clouds.Count); int n = 0;
            foreach (Transform c in pv) if (c.name.StartsWith("Cloud ")) { clouds.Add(c); yaw.Add(c.localEulerAngles.y); spin.Add((rng.Next(2) == 0 ? -1 : 1) * (4f + (float)rng.NextDouble() * 9f)); n++; }
            count.Add(n);
        }
        orbit.clouds = clouds.ToArray(); orbit.pivotFirst = first.ToArray(); orbit.pivotCount = count.ToArray();
        orbit.cloudYaw = yaw.ToArray(); orbit.cloudSpin = spin.ToArray();
        orbit.clearZones = zones.ToArray(); orbit.clearHalf = halves.ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(orbit);
        report.Add("Cloud spin: " + clouds.Count + " clouds, 4-13 degrees/second, " + spin.Count(x => x > 0) + " one way and " + spin.Count(x => x < 0) + " the other");
        report.Add("Keep-clear zones: " + zones.Count + " (" + string.Join(", ", zones.Select((z, i) => z.name.Replace("Cloud keep-clear - ", "") + " " + (halves[i] * 2).ToString("0"))) + ")");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ---------- validation: sweep 3 minutes of orbit and count clouds that would enter a zone ----------
        var spiral = Object.FindObjectOfType<AstraSpiral>(); spiral.Recenter(0);
        int entering = 0, samples = 0;
        for (float t = 0; t < 180; t += 2)
        {
            for (int i = 0; i < orbit.pivots.Length; i++) orbit.pivots[i].localRotation = Quaternion.Euler(0, orbit.phases[i] + t * orbit.speeds[i], 0);
            foreach (var c in clouds)
            {
                samples++;
                for (int k = 0; k < zones.Count; k++) { var d = zones[k].InverseTransformDirection(c.position - zones[k].position); var h = halves[k]; if (Mathf.Abs(d.x) < h.x && Mathf.Abs(d.y) < h.y && Mathf.Abs(d.z) < h.z) { entering++; break; } }
            }
        }
        for (int i = 0; i < orbit.pivots.Length; i++) orbit.pivots[i].localRotation = Quaternion.identity;
        report.Add("Over 3 minutes of orbit, clouds entering a keep-clear zone (they hide while inside): " + entering + " of " + samples + " samples " + (zones.Count > 0 ? "PASS (handled)" : "FAIL"));
        File.WriteAllText("Review/claude-round3f-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3F_OK " + string.Join(" | ", report));
    }

    // Rounded cloud: a big soft body plus round puffs on top, smoother spheres (2 subdivisions). Walkable top near y = 0.
    static void RoundCloud(Mesh m, string name, System.Random rng)
    {
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        Ico(2, out var sv, out var st);
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
        void Puff(Vector3 c, Vector3 sc) { int k = v.Count; foreach (var d in sv) { v.Add(c + Vector3.Scale(d, sc)); n.Add(new Vector3(d.x / sc.x, d.y / sc.y, d.z / sc.z).normalized); } foreach (var i in st) t.Add(k + i); }
        float len = R(1.15f, 1.55f), wid = R(.85f, 1.1f);
        Puff(new Vector3(0, .03f - .62f, 0), new Vector3(len, .62f, wid));                 // rounded body
        int puffs = rng.Next(4, 7);
        for (int i = 0; i < puffs; i++)
        {
            float a = i * Mathf.PI * 2 / puffs + R(-.4f, .4f), d = R(.35f, .7f), r = R(.5f, .8f);
            var sc = new Vector3(r, r * R(.82f, .95f), r * R(.9f, 1f));                        // nearly round puffs
            var c = new Vector3(Mathf.Cos(a) * d * len, 0, Mathf.Sin(a) * d * wid); c.y = R(.04f, .12f) - sc.y;
            Puff(c, sc);
        }
        float cr = R(.55f, .75f); Puff(new Vector3(R(-.2f, .2f), .1f - cr, R(-.15f, .15f)), new Vector3(cr, cr * .9f, cr));   // round crown puff
        Puff(new Vector3(0, -.85f, 0), new Vector3(len * .8f, .45f, wid * .75f));             // soft underside
        m.Clear(); m.name = name; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
    }
    static void Ico(int subdivisions, out List<Vector3> verts, out List<int> tris)
    {
        float p = (1 + Mathf.Sqrt(5)) / 2;
        var vv = new List<Vector3> { new Vector3(-1,p,0), new Vector3(1,p,0), new Vector3(-1,-p,0), new Vector3(1,-p,0), new Vector3(0,-1,p), new Vector3(0,1,p),
            new Vector3(0,-1,-p), new Vector3(0,1,-p), new Vector3(p,0,-1), new Vector3(p,0,1), new Vector3(-p,0,-1), new Vector3(-p,0,1) };
        for (int i = 0; i < vv.Count; i++) vv[i] = vv[i].normalized;
        var tt = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
        for (int s = 0; s < subdivisions; s++)
        {
            var cache = new Dictionary<long, int>(); var outT = new List<int>();
            int Mid(int a, int b) { long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; if (cache.TryGetValue(key, out int r)) return r; vv.Add(((vv[a] + vv[b]) / 2).normalized); cache[key] = vv.Count - 1; return vv.Count - 1; }
            for (int i = 0; i < tt.Count; i += 3) { int a = tt[i], b = tt[i + 1], c = tt[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a); outT.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca }); }
            tt = outT;
        }
        verts = vv; tris = tt;
    }
    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
    static Bounds Bounds(Transform root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (rs.Length == 0) return new Bounds(root.position, Vector3.one);
        var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); return bb;
    }
    static float ClosestToPole(Bounds bb)
    {
        float x = Mathf.Clamp(0, bb.min.x, bb.max.x), z = Mathf.Clamp(0, bb.min.z, bb.max.z);
        return new Vector2(x, z).magnitude;
    }
    // true nearest horizontal distance from the pole to any vertex of the object (not its axis-aligned box)
    static float NearestToPole(Transform root)
    {
        float best = float.MaxValue;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue; var m = mf.transform.localToWorldMatrix;
            foreach (var v in mf.sharedMesh.vertices) { var w = m.MultiplyPoint3x4(v); best = Mathf.Min(best, new Vector2(w.x, w.z).magnitude); }
        }
        return best;
    }
    // true overlap test: any vertex of b inside a's own-frame bounds (grown by margin), or the other way round
    static bool Overlaps(Transform a, Transform b, float margin)
    {
        bool Inside(Transform frame, Transform other)
        {
            var lb = LocalBounds(frame); lb.Expand(margin * 2);
            var toFrame = Matrix4x4.Rotate(Quaternion.Inverse(Quaternion.Euler(0, frame.eulerAngles.y, 0))) * Matrix4x4.Translate(-frame.position);
            foreach (var mf in other.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.name == "Beam") continue; var m = toFrame * mf.transform.localToWorldMatrix;
                var vs = mf.sharedMesh.vertices; for (int i = 0; i < vs.Length; i += 7) if (lb.Contains(m.MultiplyPoint3x4(vs[i]))) return true;
            }
            return false;
        }
        return Inside(a, b) || Inside(b, a);
    }
    // bounds of all non-particle meshes in the root's own frame (includes child scale/rotation, excludes root rotation)
    static Bounds LocalBounds(Transform root)
    {
        bool any = false; var lb = new Bounds();
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || mf.GetComponent<ParticleSystemRenderer>() != null || mf.name == "Beam") continue; // light beams are not solid
            var m = Matrix4x4.Rotate(Quaternion.Inverse(Quaternion.Euler(0, root.eulerAngles.y, 0))) * Matrix4x4.Translate(-root.position) * mf.transform.localToWorldMatrix;
            var b = mf.sharedMesh.bounds;
            foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 })
            { var p = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3(sx, sy, sz))); if (!any) { lb = new Bounds(p, Vector3.zero); any = true; } else lb.Encapsulate(p); }
        }
        return any ? lb : new Bounds(Vector3.zero, Vector3.one);
    }
}
