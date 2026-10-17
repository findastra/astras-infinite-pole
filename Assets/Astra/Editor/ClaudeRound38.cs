// Claude, 2026-10-10. Round 38 (owner): "fuse the cloud sea the same way; look for random ball clouds that are not
// clouds; check that all of the easter egg grabbable sparkle pac dot balls are actually grabbable; way more sparkle."
// 38a = read-only survey -> Review/round38/survey.txt
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UdonSharpEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ClaudeRound38
{
    const string Out = "Review/round38";
    static string P(Transform t) { return ClaudeCloudInspect.Path(t); }
    static string V(Vector3 v) { return v.x.ToString("0.00") + "," + v.y.ToString("0.00") + "," + v.z.ToString("0.00"); }

    // connected pieces of a mesh (shared vertex indices or identical positions)
    public static List<List<int>> Pieces(Mesh m)
    {
        var vs = m.vertices; var tr = m.triangles; int n = vs.Length;
        var parent = Enumerable.Range(0, n).ToArray();
        System.Func<int, int> find = null; find = x => { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; };
        System.Action<int, int> join = (a, b) => { a = find(a); b = find(b); if (a != b) parent[a] = b; };
        for (int i = 0; i < tr.Length; i += 3) { join(tr[i], tr[i + 1]); join(tr[i], tr[i + 2]); }
        var byPos = new Dictionary<Vector3, int>();
        for (int i = 0; i < n; i++) { if (byPos.TryGetValue(vs[i], out var j)) join(i, j); else byPos[vs[i]] = i; }
        return Enumerable.Range(0, n).GroupBy(find).Select(g => g.ToList()).ToList();
    }

    [MenuItem("Astra/Claude/38a Survey - sea, ball clouds, pickups (read-only)")]
    public static void Survey()
    {
        Directory.CreateDirectory(Out);
        var sb = new StringBuilder();
        var all = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid()).ToList();

        sb.AppendLine("== cloud sea / banner mound meshes ==");
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>(true).Where(r => r.sharedMaterial != null && (r.sharedMaterial.name.Contains("Sea") || P(r.transform).Contains("cloud sea") || r.name.Contains("mound"))))
        {
            var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null;
            sb.AppendLine(P(r.transform) + (r.gameObject.activeInHierarchy ? "" : " [inactive]") + " mat " + r.sharedMaterial.name + " shader " + r.sharedMaterial.shader.name + " mesh " + (m ? m.name + " " + AssetDatabase.GetAssetPath(m) + " v " + m.vertexCount + " t " + m.triangles.Length / 3 : "none")
                + " pos " + V(r.transform.position) + " scale " + V(r.transform.lossyScale) + " bounds " + V(r.bounds.min) + ".." + V(r.bounds.max));
            if (m != null && m.vertexCount < 400000 && r.sharedMaterial.shader.name == "Astra/Glitter Cloud")
            {
                var pcs = Pieces(m);
                sb.AppendLine("   pieces " + pcs.Count + ", verts per piece: " + string.Join(" ", pcs.Select(p => p.Count).GroupBy(c => c).OrderByDescending(g => g.Count()).Take(6).Select(g => g.Key + "x" + g.Count())));
            }
        }

        sb.AppendLine(); sb.AppendLine("== round 'Glitter Cloud k' puffs still visible, by parent ==");
        var puffs = Object.FindObjectsOfType<MeshRenderer>().Where(r => r.enabled && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Glitter Cloud")).ToList();
        sb.AppendLine("total " + puffs.Count);
        foreach (var g in puffs.GroupBy(r => r.transform.parent).OrderBy(g => g.Count()))
            sb.AppendLine("  " + g.Count() + " under " + P(g.Key) + " e.g. " + g.First().name + " scale " + V(g.First().transform.lossyScale) + " mat " + g.First().sharedMaterial.name);

        sb.AppendLine(); sb.AppendLine("== built-in Sphere / ball meshes ==");
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>(true).Where(r => { var mf = r.GetComponent<MeshFilter>(); return mf && mf.sharedMesh && (mf.sharedMesh.name == "Sphere" || mf.sharedMesh.name.ToLower().Contains("ball") || mf.sharedMesh.name.ToLower().Contains("orb")); }).GroupBy(r => r.name + " | " + (r.sharedMaterial ? r.sharedMaterial.name : "null")))
            sb.AppendLine("  " + r.Count() + " x " + r.Key + " e.g. " + P(r.First().transform) + (r.First().gameObject.activeInHierarchy ? "" : " [inactive]"));

        sb.AppendLine(); sb.AppendLine("== pickups ==");
        var pickupType = System.Type.GetType("VRC.SDK3.Components.VRCPickup, VRC.SDK3");
        var pickups = pickupType == null ? new Component[0] : Object.FindObjectsOfType(pickupType, true).Cast<Component>().ToArray();
        sb.AppendLine("VRCPickup components: " + pickups.Length + (pickupType == null ? " (type not found)" : ""));
        foreach (var p in pickups)
        {
            var go = p.gameObject; var rb = go.GetComponent<Rigidbody>(); var cols = go.GetComponents<Collider>();
            var so = new SerializedObject(p); bool pickupable = so.FindProperty("pickupable") == null || so.FindProperty("pickupable").boolValue;
            var ps = go.GetComponentsInChildren<ParticleSystem>(true);
            sb.AppendLine(P(go.transform) + (go.activeInHierarchy ? "" : " [INACTIVE]") + ((Behaviour)p).enabled.ToString().Replace("True", "").Replace("False", " [PICKUP DISABLED]")
                + " layer " + LayerMask.LayerToName(go.layer) + " pickupable " + pickupable
                + " rb " + (rb ? (rb.isKinematic ? "kinematic" : "dynamic") + (rb.useGravity ? "+gravity" : "") : "NONE")
                + " colliders " + (cols.Length == 0 ? "NONE" : string.Join("/", cols.Select(c => c.GetType().Name + (c.enabled ? "" : "(off)") + (c.isTrigger ? "(trigger)" : "") + " " + V(c.bounds.size))))
                + " udon " + go.GetComponents<MonoBehaviour>().Where(m => m != null && m.GetType().Name != pickupType.Name).Select(m => m.GetType().Name).Aggregate("", (a, b) => a + b + ",")
                + " particles " + ps.Length + (ps.Length > 0 ? " (max " + string.Join("/", ps.Select(x => x.main.maxParticles)) + ", rate " + string.Join("/", ps.Select(x => x.emission.rateOverTime.constant.ToString("0"))) + ")" : "")
                + " y " + go.transform.position.y.ToString("0.00"));
        }
        sb.AppendLine(); sb.AppendLine("== things named like eggs / orbs / dots that are NOT pickups ==");
        foreach (var t in all.Where(t => (t.name.Contains("Easter egg") || t.name.Contains("Glow orb") || t.name.ToLower().Contains("dot")) && (pickupType == null || t.GetComponent(pickupType) == null)).GroupBy(t => t.name))
            sb.AppendLine("  " + t.Count() + " x " + t.Key + " e.g. " + P(t.First()) + " comps " + string.Join(",", t.First().GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name)));

        sb.AppendLine(); sb.AppendLine("== particle budget ==");
        var allPs = Object.FindObjectsOfType<ParticleSystem>(true);
        sb.AppendLine("systems " + allPs.Length + ", summed maxParticles " + allPs.Sum(x => x.main.maxParticles));
        File.WriteAllText(Path.Combine(Out, "survey.txt"), sb.ToString());
    }

    public static System.Type T(string name) { return System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null); }
    static System.Type PickupType => T("VRC.SDK3.Components.VRCPickup");

    // 38b: sphere list of the sea billows + banner mound for Blender, and a real pickup audit
    [MenuItem("Astra/Claude/38b Export sea spheres + pickup audit (read-only)")]
    public static void ExportAndAudit()
    {
        Directory.CreateDirectory("Tools/Blender/out"); Directory.CreateDirectory(Out);
        var sb = new StringBuilder("[");
        var targets = new[] { ("Sea billows", "15 - Pink cloud sea (Claude)/Cloud sea billows"), ("Banner mound", "16 - Banner phone booth (Claude)/Cloud mound") };
        bool first = true;
        foreach (var (name, path) in targets)
        {
            var go = GameObject.Find(path); var m = go.GetComponent<MeshFilter>().sharedMesh; var vs = m.vertices;
            var spheres = Pieces(m).Select(pc =>
            {
                var b = new Bounds(vs[pc[0]], Vector3.zero); foreach (var i in pc) b.Encapsulate(vs[i]);
                return b;
            }).ToList();
            if (!first) sb.Append(","); first = false;
            sb.Append("{\"name\":\"" + name + "\",\"spheres\":[" + string.Join(",", spheres.Select(b => "[" + string.Join(",", new[] { b.center.x, b.center.y, b.center.z, b.extents.x, b.extents.y, b.extents.z }.Select(f => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))) + "]")) + "]}");
        }
        sb.Append("]");
        File.WriteAllText("Tools/Blender/out/sea_spheres.json", sb.ToString());

        var r = new StringBuilder();
        var pt = PickupType;
        r.AppendLine("pickup type " + (pt == null ? "NOT FOUND" : pt.FullName));
        foreach (var t in Object.FindObjectsOfType<Transform>(true).Where(t => t.name.StartsWith("Glow orb ") || t.name == "Easter egg").OrderBy(t => t.name))
        {
            var go = t.gameObject; var pk = pt == null ? null : go.GetComponent(pt) as Behaviour;
            var rb = go.GetComponent<Rigidbody>(); var cols = go.GetComponents<Collider>();
            string pick = "NO PICKUP";
            if (pk != null) { var so = new SerializedObject(pk); var pp = so.FindProperty("pickupable"); pick = "pickup " + (pk.enabled ? "on" : "OFF") + (pp != null && !pp.boolValue ? " NOT PICKUPABLE" : ""); }
            var orb = go.GetComponent("AstraGlowOrb"); string sp = "";
            if (orb != null) { var f = orb.GetType().GetField("sparkles"); var ps = f?.GetValue(orb) as ParticleSystem; sp = " sparkles " + (ps == null ? "UNASSIGNED" : ps.name + " max " + ps.main.maxParticles + " rate " + ps.emission.rateOverTime.constant); }
            // anything solid around it that would stop a hand reaching it? (only the orb's own collider allowed)
            var blockers = Physics.OverlapSphere(t.position, .05f, ~0, QueryTriggerInteraction.Ignore).Where(c => c.gameObject != go).Select(c => c.name).Distinct();
            r.AppendLine(P(t) + (go.activeInHierarchy ? "" : " [INACTIVE]") + " | " + pick + " | layer " + LayerMask.LayerToName(go.layer)
                + " | rb " + (rb ? (rb.isKinematic ? "kinematic" : "dynamic") + (rb.useGravity ? " gravity" : "") : "none")
                + " | col " + (cols.Length == 0 ? "none" : string.Join("/", cols.Select(c => c.GetType().Name + (c.enabled ? "" : " OFF") + (c.isTrigger ? " trigger" : ""))))
                + " | sync " + (go.GetComponent("VRCObjectSync") != null) + sp
                + " | world y " + t.position.y.ToString("0.00") + " size " + (go.GetComponent<Renderer>() ? go.GetComponent<Renderer>().bounds.size.x.ToString("0.00") : "?")
                + " | inside " + string.Join(",", blockers));
        }
        File.WriteAllText(Path.Combine(Out, "pickups.txt"), r.ToString());
    }
    // ======================================================================== 38c APPLY
    [System.Serializable] class FItem { public string name; public int clumps; public float[] vertices, normals, colors; public int[] triangles; }
    [System.Serializable] class FWrap { public FItem[] items; }
    const string Backup = "Review/Backups/BeforeRound38/AstrasInfinitePole.unity.txt";

    [MenuItem("Astra/Claude/38r Restore scene from before round 38")]
    public static void Restore()
    {
        var path = EditorSceneManager.GetActiveScene().path;
        File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path); EditorSceneManager.OpenScene(path);
        File.WriteAllText(Path.Combine(Out, "restore.txt"), "restored " + path + " from " + Backup + "\n");
    }

    [MenuItem("Astra/Claude/38c APPLY - fused sea, no ball clouds, grabbable sparkly eggs and orbs")]
    public static void Apply()
    {
        var rep = new List<string> { "Round 38 apply " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
        var scene = EditorSceneManager.GetActiveScene(); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(Backup)); if (!File.Exists(Backup)) File.Copy(scene.path, Backup, true);
        try
        {
            Sea(rep);
            BallClouds(rep);
            Pickups(rep);
            ClearOverlaps(); rep.AddRange(File.ReadAllLines(Path.Combine(Out, "clear-overlaps.txt")));
            // clouds whose look changed: re-slab their support colliders so they stay inside the new outline
            var v = new List<string>(); ClaudeCloudRedo.ValidateInto(v);
            var fails = v.Where(l => l.StartsWith("FAIL ")).Select(l => l.Substring(5).Split(new[] { " [" }, System.StringSplitOptions.None)[0].Trim()).ToList();
            var r2 = new List<string>();
            if (fails.Count > 0) ClaudeCloudRedo.Reslab(go => fails.Contains(P(go.transform)), r2);
            rep.Add("support colliders under changed clouds: " + fails.Count + " re-slabbed" + (fails.Count > 0 ? ": " + string.Join("; ", fails) : ""));
            rep.AddRange(r2.Where(l => l.TrimStart().StartsWith("after") || l.StartsWith("NO ")));
            var noArea = r2.Where(l => l.StartsWith("NO ")).Select(l => l.Substring(l.IndexOf(") ") + 2).Split(new[] { " [" }, System.StringSplitOptions.None)[0].Trim()).ToList();
            foreach (var b in Object.FindObjectsOfType<BoxCollider>().Where(b => noArea.Contains(P(b.transform)) && !b.isTrigger)) { Undo.RecordObject(b, "no floating floor"); b.enabled = false; }
            if (noArea.Count > 0) rep.Add("switched off (no visible cloud left above them, they would be invisible floors): " + string.Join("; ", noArea));
            var v2 = new List<string>(); ClaudeCloudRedo.ValidateInto(v2);
            rep.Add("validate after: " + v2.Count(l => l.StartsWith("OK ")) + " OK, " + v2.Count(l => l.StartsWith("FAIL ")) + " FAIL");
            rep.AddRange(v2.Where(l => l.StartsWith("FAIL ")));
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            rep.Add("particle budget (active systems, summed maxParticles): " + Object.FindObjectsOfType<ParticleSystem>().Sum(x => x.main.maxParticles));
            rep.Add("ROUND38_OK");
        }
        catch (System.Exception e) { rep.Add("ERROR " + e); Debug.LogException(e); }
        File.WriteAllText(Path.Combine(Out, "apply-report.txt"), string.Join("\n", rep) + "\n");
    }

    // ------------------------------------------------------------------ sea + banner mound
    static void Sea(List<string> rep)
    {
        var w = JsonUtility.FromJson<FWrap>("{\"items\":" + File.ReadAllText("Tools/Blender/out/sea_fused.json") + "}");
        var goPaths = new Dictionary<string, string> { { "Sea billows", "15 - Pink cloud sea (Claude)/Cloud sea billows" }, { "Banner mound", "16 - Banner phone booth (Claude)/Cloud mound" } };
        Material seaMat = null;
        foreach (var it in w.items)
        {
            int n = it.vertices.Length / 3; var vs = new Vector3[n]; var ns = new Vector3[n]; var cs = new Color[n];
            for (int i = 0; i < n; i++)
            {
                vs[i] = new Vector3(it.vertices[i * 3], it.vertices[i * 3 + 1], it.vertices[i * 3 + 2]);
                ns[i] = new Vector3(it.normals[i * 3], it.normals[i * 3 + 1], it.normals[i * 3 + 2]).normalized;
                cs[i] = new Color(it.colors[i * 4], it.colors[i * 4 + 1], it.colors[i * 4 + 2], 1);
            }
            string assetPath = "Assets/Astra/Meshes/CloudRedo/" + it.name + " fused.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath); bool fresh = m == null; if (fresh) m = new Mesh();
            m.Clear(); m.name = it.name + " fused (Claude)"; m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(vs); m.SetNormals(ns); m.SetColors(cs); m.SetTriangles(it.triangles, 0); m.RecalculateBounds();
            if (fresh) AssetDatabase.CreateAsset(m, assetPath); else EditorUtility.SetDirty(m);
            var go = GameObject.Find(goPaths[it.name]); var mf = go.GetComponent<MeshFilter>(); var old = mf.sharedMesh;
            Undo.RecordObject(mf, "fused sea"); mf.sharedMesh = m;
            seaMat = go.GetComponent<MeshRenderer>().sharedMaterial;
            rep.Add(it.name + ": " + (old ? old.name + " " + old.triangles.Length / 3 + " triangles of round balls" : "?") + " -> " + it.clumps + " fused clump(s), " + it.triangles.Length / 3 + " triangles, " + n + " vertices (" + assetPath + ")");
        }
        if (seaMat != null) { seaMat.SetFloat("_VColor", 1); EditorUtility.SetDirty(seaMat); rep.Add("material " + seaMat.name + ": baked folds on (used only by the sea and the mound)"); }
    }

    // ------------------------------------------------------------------ lone round balls posing as clouds
    static void BallClouds(List<string> rep)
    {
        var orbit = Enumerable.Range(0, 8).Select(k => AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Astra/Meshes/CloudRedo/Orbit Cloud " + k + ".asset")).ToArray();
        var done = new List<Transform>(); var kept = new List<string>();
        var puffs = Object.FindObjectsOfType<MeshRenderer>().Where(r => r.enabled && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Glitter Cloud")).ToList();
        foreach (var r in puffs)
        {
            var t = r.transform; string path = P(t);
            if (path.StartsWith("12 - DJ cloud")) { kept.Add("DJ cloud puff (carved round the booth)"); continue; }
            if (t.name != "Puff" && t.name != "Cloud") { kept.Add(t.name + " (furniture)"); continue; }
            int k = Mathf.Abs(path.GetHashCode()) % 8;
            var mf = r.GetComponent<MeshFilter>(); Undo.RecordObject(mf, "ball cloud"); mf.sharedMesh = orbit[k]; done.Add(t);
        }
        rep.Add("lone round ball clouds swapped to fused cloud shapes (same transform, material and size): " + done.Count);
        foreach (var g in done.GroupBy(t => t.parent.name == "Cloud shape" && t.parent.parent != null ? t.parent.parent.name : t.parent.name)) rep.Add("    " + g.Count() + " in " + g.Key);
        rep.Add("round puffs kept on purpose: " + kept.Count + " -> " + string.Join(", ", kept.GroupBy(x => x).Select(g => g.Count() + "x " + g.Key)));
    }

    // ------------------------------------------------------------------ eggs + glow orbs: grabbable, reachable, lots of sparkle
    static void Pickups(List<string> rep)
    {
        var pt = PickupType; var syncT = T("VRC.SDK3.Components.VRCObjectSync");
        var orbs = Object.FindObjectsOfType<Transform>().Where(t => t.name.StartsWith("Glow orb ")).OrderBy(t => t.name).ToList();
        var eggs = Object.FindObjectsOfType<Transform>().Where(t => t.name == "Easter egg").OrderBy(t => P(t)).ToList();
        var model = orbs.First().gameObject; var modelPk = model.GetComponent(pt); var modelSync = model.GetComponent(syncT); var modelRb = model.GetComponent<Rigidbody>();
        var modelGrab = model.GetComponent<AstraGlowOrb>().sparkles;
        var sparkMat = modelGrab.GetComponent<ParticleSystemRenderer>().sharedMaterial;
        int made = 0, moved = 0;
        foreach (var e in eggs)
        {
            var go = e.gameObject; Undo.RegisterFullObjectHierarchyUndo(go, "egg pickup");
            go.layer = model.layer;
            var col = go.GetComponent<SphereCollider>(); if (col == null) col = go.AddComponent<SphereCollider>(); col.radius = .5f; col.isTrigger = model.GetComponent<SphereCollider>().isTrigger;
            var rb = go.GetComponent<Rigidbody>(); if (rb == null) rb = go.AddComponent<Rigidbody>(); EditorUtility.CopySerialized(modelRb, rb);
            var pk = go.GetComponent(pt); if (pk == null) pk = go.AddComponent(pt); EditorUtility.CopySerialized(modelPk, pk);
            var so = new SerializedObject(pk); var itx = so.FindProperty("InteractionText"); if (itx != null) { itx.stringValue = "Easter egg"; so.ApplyModifiedPropertiesWithoutUndo(); }
            if (go.GetComponent(syncT) == null) { var sy = go.AddComponent(syncT); EditorUtility.CopySerialized(modelSync, sy); }
            if (go.GetComponent<AstraGlowOrb>() == null)
            {
                var grab = Object.Instantiate(modelGrab.gameObject, go.transform, false); grab.name = "Grab sparkles"; grab.transform.localPosition = Vector3.zero;
                var gm = grab.GetComponent<ParticleSystem>().main; gm.maxParticles = 80;
                var orb = go.AddUdonSharpComponent<AstraGlowOrb>(); orb.sparkles = grab.GetComponent<ParticleSystem>(); UdonSharpEditorUtility.CopyProxyToUdon(orb);
            }
            made++;
        }
        rep.Add("easter eggs made grabbable (same setup as the glow orbs: Pickup layer, kinematic body, VRCPickup, VRCObjectSync, sparkle burst while held): " + made);

        // reach: nothing solid around it, and a floor close enough below to grab it from
        var all = orbs.Concat(eggs).ToList(); Physics.SyncTransforms();
        foreach (var t in all)
        {
            float rad = t.GetComponent<Renderer>().bounds.extents.y;
            var inside = Physics.OverlapSphere(t.position, rad * .6f, ~(1 << t.gameObject.layer), QueryTriggerInteraction.Ignore).Where(c => c.transform != t).ToList();
            if (inside.Count > 0)
            {
                float top = inside.Max(c => c.bounds.max.y); Undo.RecordObject(t, "egg reach"); t.position = new Vector3(t.position.x, top + rad + .02f, t.position.z); moved++;
                rep.Add("    moved up out of " + string.Join(",", inside.Select(c => c.name).Distinct()) + ": " + P(t));
                Physics.SyncTransforms();
            }
            // nearest walkable slab at about its own height (the lounge islands have gaps, so a straight-down ray can
            // fall through to the main floor far below); only if none is near, use the floor straight below
            string floor; var p0 = t.position;
            System.Func<float, float, List<(BoxCollider c, Vector3 w, float d)>> search = (reach, depth) => Physics.OverlapBox(p0 + Vector3.down * (depth * .5f - .3f), new Vector3(reach, depth * .5f, reach), Quaternion.identity, 1, QueryTriggerInteraction.Ignore)
                .OfType<BoxCollider>().Where(c => c.transform != t && c.GetComponent("VRCStation") == null
                    && !(c.name.StartsWith("Cloud ") && c.transform.parent != null && c.transform.parent.name.StartsWith("Cloud ring")))   // orbiting clouds move away
                .Select(c =>
                {
                    var lp = c.transform.InverseTransformPoint(p0) - c.center; var hs = c.size * .5f;
                    var q = new Vector3(Mathf.Clamp(lp.x, -hs.x, hs.x), hs.y, Mathf.Clamp(lp.z, -hs.z, hs.z));
                    var w = c.transform.TransformPoint(q + c.center); return (c, w, d: new Vector2(w.x - p0.x, w.z - p0.z).magnitude);
                })
                .Where(x => x.w.y <= p0.y + .05f).OrderBy(x => x.d).ThenByDescending(x => x.w.y).ToList();
            var near = search(3f, 2.8f); if (near.Count == 0) near = search(5f, 2.8f); if (near.Count == 0) near = search(6f, 7f);
            if (near.Count > 0)
            {
                var (c, w, d) = near[0]; float gap = p0.y - w.y; var np = p0;
                if (d > .01f) { var inward = (c.bounds.center - w); inward.y = 0; np.x = w.x + inward.normalized.x * Mathf.Min(.15f, inward.magnitude); np.z = w.z + inward.normalized.z * Mathf.Min(.15f, inward.magnitude); }
                if (gap < rad + .02f || gap > 1.7f) np.y = w.y + Mathf.Max(rad + .02f, .2f);
                if ((np - p0).sqrMagnitude > 1e-6f) { Undo.RecordObject(t, "egg reach"); t.position = np; moved++; floor = "moved " + (np - p0).magnitude.ToString("0.00") + " m onto " + c.name + " (now " + (np.y - w.y).ToString("0.00") + " m above it)"; }
                else floor = gap.ToString("0.00") + " m above " + c.name;
            }
            else
            {
                RaycastHit h;
                if (Physics.Raycast(p0 + Vector3.down * (rad + .01f), Vector3.down, out h, 30f, 1, QueryTriggerInteraction.Ignore))
                {
                    float gap = h.distance + rad;
                    if (gap < 1.9f) floor = gap.ToString("0.00") + " m above " + h.collider.name;
                    else { Undo.RecordObject(t, "egg reach"); t.position = h.point + Vector3.up * 1.1f; moved++; floor = "was " + gap.ToString("0.0") + " m above " + h.collider.name + " with no cloud slab near, lowered to 1.1 m"; }
                }
                else floor = "UNREACHABLE, no slab within 3 m and no floor close below (left where it is)";
            }
            rep.Add("    reach " + P(t) + ": " + floor);
        }
        rep.Add("pickups moved so a hand can reach them: " + moved);

        // sparkle slurry: a thick, swirling, always-on cloud of tiny glitter around every orb and egg that trails when carried
        int slurries = 0;
        foreach (var t in all)
        {
            bool isOrb = t.name.StartsWith("Glow orb");
            var old = t.Find("Sparkle slurry"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var sg = new GameObject("Sparkle slurry"); sg.transform.SetParent(t, false); sg.layer = 0;
            var ps = sg.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            float r = t.GetComponent<Renderer>().bounds.extents.x;   // world radius of the ball
            var main = ps.main; main.loop = true; main.playOnAwake = true; main.prewarm = true;
            main.maxParticles = isOrb ? 300 : 150; main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f); main.startSpeed = new ParticleSystem.MinMaxCurve(.02f, .12f);
            main.startSize = new ParticleSystem.MinMaxCurve(.02f + r * .05f, .05f + r * .14f);
            var cg = new Gradient(); cg.SetKeys(new[] { new GradientColorKey(new Color(1f, .62f, .95f) * 2.4f, 0), new GradientColorKey(new Color(.7f, .8f, 1f) * 2.4f, .35f), new GradientColorKey(new Color(1f, .95f, .6f) * 2.4f, .7f), new GradientColorKey(new Color(.75f, 1f, .9f) * 2.4f, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });   // HDR: glows through the world's bloom
            main.startColor = new ParticleSystem.MinMaxGradient(cg) { mode = ParticleSystemGradientMode.RandomColor };
            var em = ps.emission; em.rateOverTime = isOrb ? 150 : 80; em.rateOverDistance = isOrb ? 60 : 35;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1f; sh.radiusThickness = .25f;
            sg.transform.localScale = Vector3.one * .62f;   // unit-sphere ball has radius .5: shell sits just outside it and scales with the ball
            var vol = ps.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.Local;
            vol.x = new ParticleSystem.MinMaxCurve(0f); vol.y = new ParticleSystem.MinMaxCurve(0f); vol.z = new ParticleSystem.MinMaxCurve(0f);
            vol.orbitalX = new ParticleSystem.MinMaxCurve(-.35f, .35f); vol.orbitalY = new ParticleSystem.MinMaxCurve(isOrb ? 1.6f : 1.2f, isOrb ? 2.2f : 1.6f); vol.orbitalZ = new ParticleSystem.MinMaxCurve(-.2f, .2f);
            vol.radial = new ParticleSystem.MinMaxCurve(-.05f, .02f);
            var nz = ps.noise; nz.enabled = true; nz.strength = .12f; nz.frequency = 1.4f; nz.scrollSpeed = .4f; nz.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime; col.enabled = true; var ag = new Gradient();
            ag.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(.8f, .7f), new GradientAlphaKey(0, 1) });
            col.color = ag;
            var sol = ps.sizeOverLifetime; sol.enabled = true;   // twinkle: each glint pulses twice
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, .3f), new Keyframe(.2f, 1f), new Keyframe(.35f, .55f), new Keyframe(.55f, 1.1f), new Keyframe(.75f, .5f), new Keyframe(1, 0)));
            var pr = sg.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = sparkMat; pr.shadowCastingMode = ShadowCastingMode.Off; pr.receiveShadows = false; pr.maxParticleSize = .08f;
            ps.Play(); slurries++;
        }
        rep.Add("sparkle slurry (always on, swirling, trails when carried): " + slurries + " systems (orbs 300 max / 150 per s, eggs 150 max / 80 per s, HDR colours), material " + (sparkMat ? sparkMat.name : "none"));
        foreach (var o in orbs) { var g = o.GetComponent<AstraGlowOrb>().sparkles; if (g == null) continue; var m = g.main; m.maxParticles = 260; var em = g.emission; em.rateOverTime = 60; em.rateOverDistance = 20; }
        rep.Add("glow orb grab bursts: 150 -> 260 particles, 28 -> 60 per second while held");
    }
    // ------------------------------------------------------------------ photos
    [MenuItem("Astra/Claude/38p Photos - before")] public static void PhotosBefore() { Photos("before"); }
    [MenuItem("Astra/Claude/38q Photos - after")] public static void PhotosAfter() { Photos("after"); }
    static void Photos(string label)
    {
        Directory.CreateDirectory(Out);
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>()) if (ps.name == "Sparkle slurry") ps.Simulate(3f, true, true);
        var mound = GameObject.Find("16 - Banner phone booth (Claude)/Cloud mound");
        Shot(label + " 1 cloud sea", new Vector3(0, 9, 0), new Vector3(60, -4, 45), 70);
        Shot(label + " 2 cloud sea low", new Vector3(-20, 2, -30), new Vector3(-80, -2, -90), 60);
        if (mound != null) { var c = mound.GetComponent<Renderer>().bounds.center; Shot(label + " 3 banner mound", c + new Vector3(9, 6, -6), c, 55); }
        var orb = Object.FindObjectsOfType<Transform>().Where(t => t.name.StartsWith("Glow orb ")).OrderBy(t => t.position.y).First();
        Shot(label + " 4 glow orb", orb.position + new Vector3(2.0f, .6f, 2.0f), orb.position, 45);
        var eggs = Object.FindObjectsOfType<Transform>().Where(t => t.name == "Easter egg").OrderBy(t => P(t)).ToList();
        foreach (var egg in eggs.Where(e => e.position.y > 1.5f).Take(2))
            Shot(label + " 5 easter egg " + egg.parent.name, egg.position + new Vector3(.9f, 1.1f, .9f), egg.position, 45);
        var vine = GameObject.Find("13 - Cloud lounge (Claude)/Crystal vine clouds/Crystal vine cloud 3");
        if (vine != null) Shot(label + " 6 crystal vine cloud", vine.transform.position + new Vector3(7, 2, 7), vine.transform.position + Vector3.down * 2, 55);
        var nook = GameObject.Find("13 - Cloud lounge (Claude)/Cloud furniture/Sleepy nook");
        if (nook != null) Shot(label + " 7 lounge", nook.transform.position + new Vector3(9, 5, 9), nook.transform.position, 60);
    }
    static void Shot(string name, Vector3 pos, Vector3 look, float fov)
    {
        var go = new GameObject("Claude photo camera") { hideFlags = HideFlags.DontSave };
        var cam = go.AddComponent<Camera>(); cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .03f; cam.farClipPlane = 2000;
        var pp = go.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform;
        var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
    }
    // 38d: last pass - no egg or orb may sit inside a solid collider (desktop hands raycast, a box in front blocks the grab)
    [MenuItem("Astra/Claude/38d Pickups - lift out of solid colliders")]
    public static void ClearOverlaps()
    {
        Physics.SyncTransforms(); var rep = new List<string>(); int n = 0;
        foreach (var t in Object.FindObjectsOfType<Transform>().Where(t => t.name.StartsWith("Glow orb ") || t.name == "Easter egg"))
            for (int pass = 0; pass < 3; pass++)
            {
                float rad = t.GetComponent<Renderer>().bounds.extents.y;
                var hit = Physics.OverlapSphere(t.position, rad * .8f, ~(1 << t.gameObject.layer), QueryTriggerInteraction.Ignore).Where(c => c.transform != t).ToList();
                if (hit.Count == 0) break;
                Undo.RecordObject(t, "pickup clear");
                var tall = hit.OfType<BoxCollider>().FirstOrDefault(c => c.bounds.size.y > .6f);
                if (tall != null)
                {   // a sofa or chair back: slide it off the back's thin side instead of perching it on top
                    var lt = tall.transform; var lp = lt.InverseTransformPoint(t.position) - tall.center; var hs = tall.size * .5f; var ls = lt.lossyScale;
                    bool alongX = Mathf.Abs(hs.x * ls.x) < Mathf.Abs(hs.z * ls.z);
                    if (alongX) lp.x = (lp.x >= 0 ? 1 : -1) * (hs.x + (rad + .03f) / Mathf.Abs(ls.x)); else lp.z = (lp.z >= 0 ? 1 : -1) * (hs.z + (rad + .03f) / Mathf.Abs(ls.z));
                    t.position = lt.TransformPoint(lp + tall.center);
                }
                else { float top = hit.Max(c => c.bounds.max.y); t.position = new Vector3(t.position.x, top + rad + .02f, t.position.z); }
                Physics.SyncTransforms(); n++;
                rep.Add((tall != null ? "slid off " : "lifted out of ") + string.Join(",", hit.Select(c => c.name).Distinct()) + " to " + V(t.position) + ": " + P(t));
            }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        rep.Insert(0, "pickups lifted out of solid colliders: " + n);
        File.WriteAllText(Path.Combine(Out, "clear-overlaps.txt"), string.Join("\n", rep) + "\n");
    }
}
