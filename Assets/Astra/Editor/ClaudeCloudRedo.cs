// Claude, 2026-10-06. Owner's cloud redo (Review/cloud-redo-handoff.md):
//   "interesting texture, more connected, not just random round spheres oriented the same way;
//    all colliders thin and only below the cloud, nothing outside it (bird's-eye view)."
//
// Menu Astra/Claude/36...:
//   36c photos (before)   36d collider audit, dry run   36e APPLY   36f photos (after) + collider overlays   36g validate
//
// APPLY does exactly this, nothing else:
//  1. Imports the Blender meshes (Tools/Blender/make_cloud_redo.py -> Tools/Blender/out/cloud_redo.json) into
//     Assets/Astra/Meshes/CloudRedo/Orbit Cloud 0-7 and Outer Cloud 0-5 (one connected body each, <= 1500 triangles).
//  2. Spiral orbit clouds that show a Glitter Cloud k mesh now show Orbit Cloud k (same transform, pivot, height,
//     orbit/spin scripts untouched). Heart, star and phone-booth clouds keep their own shapes.
//     Glitter Cloud 0-7 assets are NOT changed: the lounge furniture is built from Glitter Cloud 4 puffs.
//  3. Each outer big cloud gets one fused mesh; its old round puffs are switched off (kept in the scene, inactive).
//  4. New material Glitter Cloud Fused (GPU instanced, opaque, same look + texture + baked folds) on those meshes;
//     the cloud materials (Glitter Cloud, Lounge Pink/Lilac/Peach) get the soft texture. The cloud sea is left alone.
//  5. Every solid cloud SUPPORT collider becomes thin horizontal slabs (0.06 m) under the visible top surface and
//     inside the cloud's outline seen from above: footprint-aware, up to a few boxes on the SAME GameObject (the
//     first BoxCollider component is reused, so references and the orbit's "Cloud " name check keep working).
//     Seats, backs, tables, swing seats, glow orbs, deck console, triggers and stations are not touched.
// To redo: run 36h (restores the pre-redo scene) first, then 36e; 36e on an already-applied scene would re-slab the slabs.
// Backup: Review/Backups/BeforeCloudRedo/.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeCloudRedo
{
    const string Json = "Tools/Blender/out/cloud_redo.json";
    const string MeshDir = "Assets/Astra/Meshes/CloudRedo";
    const string FusedMat = "Assets/Astra/Materials/Glitter Cloud Fused.mat";
    const string OuterMat = "Assets/Astra/Materials/Glitter Cloud Fused Outer.mat";
    const string Out = "Review/cloud-redo";
    const float Thick = .06f;

    [Serializable] class Item { public string name, kind; public int seed, tris; public float[] vertices, normals, colors; public int[] triangles; }
    [Serializable] class Wrap { public Item[] items; }

    // ------------------------------------------------------------------ menu
    [MenuItem("Astra/Claude/36c Cloud redo photos - before")] public static void PhotosBefore() { Photos("before"); }
    [MenuItem("Astra/Claude/36f Cloud redo photos - after")] public static void PhotosAfter() { Photos("after"); }
    [MenuItem("Astra/Claude/36d Cloud collider audit (dry run)")]
    public static void AuditDry() { var r = new List<string>(); Colliders(false, r); Write("collider-audit-before.txt", r); }
    [MenuItem("Astra/Claude/36g Cloud collider validate")]
    public static void Validate() { var r = new List<string>(); Colliders(false, r, validateOnly: true); Write("collider-validate.txt", r); }

    [MenuItem("Astra/Claude/36e Cloud redo - APPLY")]
    public static void Apply()
    {
        var report = new List<string> { "Cloud redo apply " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Review/Backups/BeforeCloudRedo");
        File.Copy(scene.path, "Review/Backups/BeforeCloudRedo/scene-at-apply.unity.txt", true);
        try
        {
            var meshes = ImportMeshes(report);
            var fused = Materials(report);
            Spiral(meshes, fused, report);
            Outer(meshes, fused, report);
            AssetDatabase.SaveAssets();
            Colliders(true, report);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            report.Add("CLOUD_REDO_OK");
        }
        catch (Exception e) { report.Add("ERROR " + e); Debug.LogException(e); }
        Write("apply-report.txt", report);
    }

    // Puts the scene back exactly as it was before the redo (Review/Backups/BeforeCloudRedo/AstrasInfinitePole.unity.txt)
    // and reopens it. Mesh/material assets are left as they are (the scene simply stops using the new ones).
    [MenuItem("Astra/Claude/36h Cloud redo - restore scene from before")]
    public static void Restore()
    {
        const string bak = "Review/Backups/BeforeCloudRedo/AstrasInfinitePole.unity.txt";
        var path = EditorSceneManager.GetActiveScene().path;
        File.Copy(bak, path, true); AssetDatabase.ImportAsset(path);
        EditorSceneManager.OpenScene(path);
        Write("restore.txt", new List<string> { "Restored " + path + " from " + bak + " at " + DateTime.Now.ToString("HH:mm:ss") });
    }

    // Physics checks on the live colliders (Default layer, triggers ignored, like the orbit's own carry ray):
    //  stand: a ray down onto each slab's centre hits that slab first   side: horizontal rays through the cloud
    //  30 cm above and 30 cm below the slab hit none of its colliders (no invisible walls)   under: a ray up from
    //  1 m below hits the slab's thin underside first   orbit: every spiral support box sits on the "Cloud N" transform
    [MenuItem("Astra/Claude/36i Cloud collider physics check")]
    public static void PhysicsCheck()
    {
        Physics.SyncTransforms();
        var r = new List<string> { "physics check " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
        var groups = Resources.FindObjectsOfTypeAll<BoxCollider>().Where(b => b.gameObject.scene.IsValid() && b.gameObject.activeInHierarchy && b.enabled && Classify(b) != null).GroupBy(b => b.gameObject).ToList();
        int stand = 0, standN = 0, side = 0, sideN = 0, under = 0, underN = 0, orbitOk = 0, orbitN = 0;
        var fails = new List<string>();
        foreach (var gr in groups)
        {
            var mine = new HashSet<Collider>(gr);
            foreach (var b in gr)
            {
                var wc = b.transform.TransformPoint(b.center); var ls = b.transform.lossyScale;
                float topY = wc.y + Mathf.Abs(b.size.y * ls.y) / 2, botY = wc.y - Mathf.Abs(b.size.y * ls.y) / 2;
                standN++; RaycastHit h;
                if (Physics.Raycast(new Vector3(wc.x, topY + 1f, wc.z), Vector3.down, out h, 1.2f, 1, QueryTriggerInteraction.Ignore) && mine.Contains(h.collider)) stand++;
                else fails.Add("stand " + ClaudeCloudInspect.Path(b.transform) + (h.collider != null ? " (first hit " + ClaudeCloudInspect.Path(h.collider.transform) + ")" : " (no hit)"));
                underN++;
                if (Physics.Raycast(new Vector3(wc.x, botY - 1f, wc.z), Vector3.up, out h, 1.2f, 1, QueryTriggerInteraction.Ignore) && mine.Contains(h.collider) && Mathf.Abs(h.point.y - botY) < .02f) under++;
                else fails.Add("under " + ClaudeCloudInspect.Path(b.transform) + (h.collider != null ? " (first hit " + ClaudeCloudInspect.Path(h.collider.transform) + " at " + F(h.point.y - botY) + ")" : " (no hit)"));
                if (IsSpiralCloud(b.transform)) { orbitN++; if (b.transform.name.StartsWith("Cloud ")) orbitOk++; }
            }
            // side rays across the whole group, along both horizontal axes of the object
            var t = gr.Key.transform; var bb = gr.First().bounds; foreach (var b in gr) bb.Encapsulate(b.bounds);
            float top = bb.max.y, bot = bb.min.y;
            foreach (var y in new[] { top + .3f, bot - .3f })
                foreach (var dir in new[] { t.right, t.forward, -t.right, -t.forward })
                {
                    var d = new Vector3(dir.x, 0, dir.z).normalized; float reach = bb.extents.magnitude + 2f;
                    var o = new Vector3(bb.center.x, y, bb.center.z) - d * reach;
                    sideN++;
                    var hits = Physics.RaycastAll(o, d, reach * 2, 1, QueryTriggerInteraction.Ignore);
                    if (!hits.Any(x => mine.Contains(x.collider))) side++;
                    else fails.Add("side wall " + ClaudeCloudInspect.Path(t) + " at " + (y > top ? "+0.3" : "-0.3") + " m");
                }
        }
        r.Add("stand-on: " + stand + "/" + standN + " slabs are the first thing a downward ray meets");
        r.Add("underside: " + under + "/" + underN + " slabs show only their thin underside from below");
        r.Add("no side walls: " + side + "/" + sideN + " horizontal rays 0.3 m above/below the slabs pass through the cloud");
        r.Add("orbit carry: " + orbitOk + "/" + orbitN + " spiral support boxes are on their \"Cloud N\" transform");
        r.Add(""); r.Add("details (" + fails.Count + "):"); r.AddRange(fails);
        Write("physics-check.txt", r);
    }

    static void Write(string file, List<string> lines) { Directory.CreateDirectory(Out); File.WriteAllText(Path.Combine(Out, file), string.Join("\n", lines) + "\n"); }
    static string F(float v) { return v.ToString("0.00"); }
    static string V(Vector3 v) { return F(v.x) + "," + F(v.y) + "," + F(v.z); }

    // ------------------------------------------------------------------ 1. meshes
    static Dictionary<string, Mesh> ImportMeshes(List<string> report)
    {
        var w = JsonUtility.FromJson<Wrap>("{\"items\":" + File.ReadAllText(Json) + "}");
        Directory.CreateDirectory(MeshDir);
        var d = new Dictionary<string, Mesh>();
        foreach (var it in w.items)
        {
            int n = it.vertices.Length / 3;
            var v = new Vector3[n]; var nn = new Vector3[n]; var c = new Color[n];
            for (int i = 0; i < n; i++)
            {
                v[i] = new Vector3(it.vertices[i * 3], it.vertices[i * 3 + 1], it.vertices[i * 3 + 2]);
                nn[i] = new Vector3(it.normals[i * 3], it.normals[i * 3 + 1], it.normals[i * 3 + 2]).normalized;
                c[i] = new Color(it.colors[i * 4], it.colors[i * 4 + 1], it.colors[i * 4 + 2], it.colors[i * 4 + 3]);
            }
            string path = MeshDir + "/" + it.name + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = m == null; if (fresh) m = new Mesh();
            m.Clear(); m.name = it.name + " (Claude redo)";
            m.SetVertices(v); m.SetNormals(nn); m.SetColors(c); m.SetTriangles(it.triangles, 0); m.RecalculateBounds();
            if (fresh) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            d[it.name] = m;
            report.Add("mesh " + it.name + ": " + it.triangles.Length / 3 + " triangles, " + n + " vertices, bounds " + V(m.bounds.min) + " .. " + V(m.bounds.max));
        }
        return d;
    }

    // ------------------------------------------------------------------ 4. materials
    static Material Materials(List<string> report)
    {
        var baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Astra/Materials/Glitter Cloud.mat");
        var fused = AssetDatabase.LoadAssetAtPath<Material>(FusedMat);
        if (fused == null) { fused = new Material(baseMat) { name = "Glitter Cloud Fused" }; AssetDatabase.CreateAsset(fused, FusedMat); }
        else fused.CopyPropertiesFromMaterial(baseMat);
        fused.enableInstancing = true;
        fused.SetFloat("_Texture", 1f); fused.SetFloat("_CloudTex", 1); fused.EnableKeyword("_CLOUDTEX"); fused.SetFloat("_VColor", 1); fused.SetFloat("_TextureScale", 1);
        EditorUtility.SetDirty(fused);
        var textured = new[] { ("Assets/Astra/Materials/Glitter Cloud.mat", .8f), ("Assets/Astra/Materials/Lounge/Lounge Pink.mat", .7f), ("Assets/Astra/Materials/Lounge/Lounge Lilac.mat", .7f), ("Assets/Astra/Materials/Lounge/Lounge Peach.mat", .7f) };
        foreach (var (p, t) in textured)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(p); if (m == null) { report.Add("material missing " + p); continue; }
            m.SetFloat("_Texture", t); m.SetFloat("_CloudTex", 1); m.EnableKeyword("_CLOUDTEX"); EditorUtility.SetDirty(m);
            report.Add("material " + m.name + ": cloud texture " + t + " (instancing " + m.enableInstancing + ")");
        }
        report.Add("material Glitter Cloud Fused: new, instancing on, texture 1, baked folds on. Pink Sea Puff (cloud sea, banner mound) unchanged.");
        return fused;
    }

    // ------------------------------------------------------------------ 2. spiral orbit clouds
    static void Spiral(Dictionary<string, Mesh> meshes, Material fused, List<string> report)
    {
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>(true);
        int swapped = 0, kept = 0; var per = new int[8];
        foreach (var t in orbit.clouds)
        {
            if (t == null) continue;
            var mf = t.GetComponent<MeshFilter>(); var mr = t.GetComponent<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null) { kept++; continue; }
            string mn = mf.sharedMesh.name; int k = -1;
            if (mn.StartsWith("Glitter Cloud ")) int.TryParse(mn.Substring(14, 1), out k);
            else if (mn.StartsWith("Orbit Cloud ")) int.TryParse(mn.Substring(12, 1), out k);
            if (k < 0 || !mr.enabled) { kept++; continue; }   // heart / star / phone-booth clouds draw their own child mesh
            Undo.RecordObject(mf, "cloud redo"); Undo.RecordObject(mr, "cloud redo");
            mf.sharedMesh = meshes["Orbit Cloud " + k]; mr.sharedMaterial = fused;
            per[k]++; swapped++;
        }
        report.Add("spiral: " + swapped + " orbit clouds now use the fused meshes (" + string.Join(", ", per.Select((c, i) => "Orbit " + i + " x" + c)) + "); " + kept + " heart/star/booth clouds keep their shapes.");
    }

    // ------------------------------------------------------------------ 3. outer big clouds
    static void Outer(Dictionary<string, Mesh> meshes, Material fused, List<string> report)
    {
        var root = GameObject.Find("14 - Outer big clouds (Claude)"); if (root == null) { report.Add("outer clouds: not found"); return; }
        int n = 0, puffsOff = 0;
        foreach (Transform c in root.transform)
        {
            if (!c.name.StartsWith("Outer cloud ")) continue;
            int idx; int.TryParse(c.name.Substring(12), out idx);
            var f = c.Find("Fused cloud (redo)");
            if (f == null) { f = new GameObject("Fused cloud (redo)").transform; f.SetParent(c, false); }
            f.localPosition = Vector3.zero; f.localRotation = Quaternion.identity; f.localScale = Vector3.one;
            var mf = f.GetComponent<MeshFilter>(); if (mf == null) mf = f.gameObject.AddComponent<MeshFilter>();
            var mr = f.GetComponent<MeshRenderer>(); if (mr == null) mr = f.gameObject.AddComponent<MeshRenderer>();
            mf.sharedMesh = meshes["Outer Cloud " + (idx % 6)]; mr.sharedMaterial = OuterMaterial(fused);
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            foreach (Transform p in c)
            {
                if (p == f) continue;
                var pm = p.GetComponent<MeshFilter>();
                if (p.name == "Puff" && pm != null && pm.sharedMesh != null && pm.sharedMesh.name.StartsWith("Glitter Cloud") && p.gameObject.activeSelf) { p.gameObject.SetActive(false); puffsOff++; }
            }
            n++;
        }
        report.Add("outer clouds: " + n + " get one fused mesh each (6 shapes, varied by their own yaw); " + puffsOff + " old round puffs switched off (inactive, not deleted).");
    }

    // 2026-10-07 relook: on the 10 m outer clouds the 1 m texture read as small dark mottling; they get the same look at 2.4x the size
    static Material OuterMaterial(Material fused)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(OuterMat);
        if (m == null) { m = new Material(fused) { name = "Glitter Cloud Fused Outer" }; AssetDatabase.CreateAsset(m, OuterMat); }
        else m.CopyPropertiesFromMaterial(fused);
        m.enableInstancing = true; m.EnableKeyword("_CLOUDTEX"); m.SetFloat("_TextureScale", 2.4f);
        EditorUtility.SetDirty(m); return m;
    }

    [MenuItem("Astra/Claude/36k Outer clouds - larger texture (material only)")]
    public static void OuterTexture()
    {
        var m = OuterMaterial(AssetDatabase.LoadAssetAtPath<Material>(FusedMat)); int n = 0;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>(true).Where(r => r.name == "Fused cloud (redo)")) { Undo.RecordObject(r, "outer texture"); r.sharedMaterial = m; n++; }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Write("outer-texture.txt", new List<string> { "outer fused clouds on Glitter Cloud Fused Outer (texture size 2.4): " + n });
    }

    // ------------------------------------------------------------------ 5. colliders
    // "Cloud booth floor" (inside the phone booths) is booth function: reported by the validator, never changed here.
    static readonly string[] SupportNames = { "Jump platform", "Cloud walk collider", "Cloud box floor", "Screen top cloud floor", "Cloud duck platform collider", "DJ stage cloud", "DJ step cloud", "Cloud swing anchor" };
    static readonly string[] NotCloudMat = { "Phone", "Payphone", "DJ", "Bubble Chair", "Lounge Dark", "Lounge Glow", "Porcelain", "Duck", "Beak", "Egg", "Crystal" };
    static readonly string[] NotCloudMesh = { "Cube", "Cylinder", "Sphere", "Quad", "VRC_chair1", "Cloud sea billows", "Pink cloud sea", "Capsule" };

    enum Kind { Spiral, Support }

    static bool IsSpiralCloud(Transform t) { return t.name.StartsWith("Cloud ") && t.parent != null && t.parent.name.StartsWith("Cloud ring"); }

    static Kind? Classify(BoxCollider b)
    {
        if (b.isTrigger) return null;
        var t = b.transform;
        if (t.GetComponent("VRCStation") != null) return null;
        if (IsSpiralCloud(t)) return Kind.Spiral;
        if (SupportNames.Contains(t.name) || t.name.StartsWith("Outer cloud ")) return Kind.Support;
        if (t.name == "Cloud" && t.parent != null && t.parent.name.StartsWith("Crystal vine cloud")) return Kind.Support;
        return null;
    }

    static bool CloudRenderer(Renderer r)
    {
        if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) return false;
        var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) return false;
        var m = r.sharedMaterial; if (m == null || m.shader == null) return false;
        bool cloudShader = m.shader.name == "Astra/Glitter Cloud" || m.name == "Pink Sea Puff";
        if (!cloudShader) return false;
        if (NotCloudMat.Any(s => m.name.Contains(s))) return false;
        if (NotCloudMesh.Contains(mf.sharedMesh.name)) return false;
        return true;
    }

    static List<Renderer> Visual(Transform owner, Kind kind)
    {
        return owner.GetComponentsInChildren<Renderer>(false).Where(r =>
        {
            if (!CloudRenderer(r)) return false;
            for (var p = r.transform; p != null && p != owner; p = p.parent)
                if (p.name.StartsWith("Phone booth") || p.name == "Banner booth" || p.name == "Swing pivot" || p.name == "DJ booth" || (kind == Kind.Support && IsSpiralCloud(p))) return false;
            return true;
        }).ToList();
    }

    class Grid
    {
        public float x0, z0, cell; public int W, H;
        public bool[] occ; public float[] surf, top;   // surf = highest cloud surface at or below the cap (NaN = none); top = highest cloud surface at all
        public bool Occ(int x, int z) { return x >= 0 && z >= 0 && x < W && z < H && occ[x + z * W]; }
        public int CX(float x) { return Mathf.FloorToInt((x - x0) / cell); }
        public int CZ(float z) { return Mathf.FloorToInt((z - z0) / cell); }
    }

    // frame: collider position + yaw, metres (no scale)
    static Grid Raster(List<Renderer> rs, Matrix4x4 toFrame, float refTop, Bounds footprint)
    {
        var tris = new List<Vector3>();
        var vb = new Bounds(footprint.center, footprint.size); bool any = false;
        foreach (var r in rs)
        {
            var mesh = r.GetComponent<MeshFilter>().sharedMesh; var M = toFrame * r.localToWorldMatrix;
            var vs = mesh.vertices; var ts = mesh.triangles;
            var p = new Vector3[vs.Length]; for (int i = 0; i < vs.Length; i++) p[i] = M.MultiplyPoint3x4(vs[i]);
            for (int i = 0; i < ts.Length; i += 3)
            {
                Vector3 a = p[ts[i]], b = p[ts[i + 1]], c = p[ts[i + 2]];
                float mn = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), mx = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                if (mx < refTop - 4f || mn > refTop + 2.5f) continue;
                tris.Add(a); tris.Add(b); tris.Add(c);
                if (!any) { vb = new Bounds(a, Vector3.zero); vb.Encapsulate(footprint); any = true; }
                vb.Encapsulate(a); vb.Encapsulate(b); vb.Encapsulate(c);
            }
        }
        float ext = Mathf.Max(vb.size.x, vb.size.z);
        var g = new Grid { cell = Mathf.Clamp(ext / 120f, .04f, .25f) };
        g.x0 = vb.min.x - g.cell * 2; g.z0 = vb.min.z - g.cell * 2;
        g.W = Mathf.CeilToInt(vb.size.x / g.cell) + 5; g.H = Mathf.CeilToInt(vb.size.z / g.cell) + 5;
        g.occ = new bool[g.W * g.H]; g.surf = Enumerable.Repeat(float.NaN, g.W * g.H).ToArray(); g.top = Enumerable.Repeat(float.NegativeInfinity, g.W * g.H).ToArray();
        float cap = refTop + .6f;
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = tris[i], b = tris[i + 1], c = tris[i + 2];
            float d = (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);
            if (Mathf.Abs(d) < 1e-9f) continue;
            int xa = Mathf.Max(0, g.CX(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), xb = Mathf.Min(g.W - 1, g.CX(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int za = Mathf.Max(0, g.CZ(Mathf.Min(a.z, Mathf.Min(b.z, c.z)))), zb = Mathf.Min(g.H - 1, g.CZ(Mathf.Max(a.z, Mathf.Max(b.z, c.z))));
            for (int z = za; z <= zb; z++) for (int x = xa; x <= xb; x++)
                {
                    float px = g.x0 + (x + .5f) * g.cell, pz = g.z0 + (z + .5f) * g.cell;
                    float w1 = ((b.x - px) * (c.z - pz) - (c.x - px) * (b.z - pz)) / d;
                    float w2 = ((c.x - px) * (a.z - pz) - (a.x - px) * (c.z - pz)) / d;
                    float w3 = 1 - w1 - w2;
                    if (w1 < -1e-4f || w2 < -1e-4f || w3 < -1e-4f) continue;
                    float y = w1 * a.y + w2 * b.y + w3 * c.y;
                    int k = x + z * g.W; g.occ[k] = true; if (y > g.top[k]) g.top[k] = y;
                    if (y <= cap && (float.IsNaN(g.surf[k]) || y > g.surf[k])) g.surf[k] = y;
                }
        }
        return g;
    }

    struct R { public int x, z, w, h; }

    static R MaxRect(bool[] m, int W, int H)
    {
        var h = new int[W]; var best = new R(); int bestA = 0; var st = new Stack<int>();
        for (int z = 0; z < H; z++)
        {
            for (int x = 0; x < W; x++) h[x] = m[x + z * W] ? h[x] + 1 : 0;
            st.Clear();
            for (int x = 0; x <= W; x++)
            {
                int cur = x == W ? 0 : h[x];
                while (st.Count > 0 && h[st.Peek()] >= cur)
                {
                    int top = st.Pop(); int height = h[top]; int left = st.Count == 0 ? 0 : st.Peek() + 1; int width = x - left;
                    if (height * width > bestA && height >= 2 && width >= 2) { bestA = height * width; best = new R { x = left, z = z - height + 1, w = width, h = height }; }
                }
                st.Push(x);
            }
        }
        return best;
    }

    class Slab { public Vector3 c; public Vector3 size; public float minSurf; }

    // 2026-10-10 (round 38): re-slab only the support objects a later change touched (e.g. a cloud whose mesh was swapped)
    public static void Reslab(System.Func<GameObject, bool> only, List<string> report) { onlyGroups = only; try { Colliders(true, report); } finally { onlyGroups = null; } }
    public static void ValidateInto(List<string> report) { Colliders(false, report, validateOnly: true); }
    static System.Func<GameObject, bool> onlyGroups;

    static void Colliders(bool apply, List<string> report, bool validateOnly = false)
    {
        Physics.SyncTransforms();
        var all = Resources.FindObjectsOfTypeAll<BoxCollider>().Where(b => b.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(b)).ToList();
        var groups = all.Where(b => b.enabled && Classify(b) != null).GroupBy(b => b.gameObject).Where(g => onlyGroups == null || onlyGroups(g.Key)).ToList();   // switched-off slabs are not support
        report.Add("");
        report.Add("COLLIDERS (" + (validateOnly ? "validate" : apply ? "apply" : "dry run") + "): " + groups.Count + " cloud support objects, " + groups.Sum(g => g.Count()) + " box colliders");
        int changed = 0, untouched = 0, problems = 0, boxesBefore = 0, boxesAfter = 0;
        var kinds = new Dictionary<string, int>();
        foreach (var grp in groups.OrderBy(g => ClaudeCloudInspect.Path(g.Key.transform)))
        {
            var t = grp.Key.transform; var kind = Classify(grp.First()).Value;
            var boxes = grp.Where(b => !b.isTrigger).ToList(); boxesBefore += boxes.Count;
            string label = kind == Kind.Spiral ? "spiral cloud" : t.name.StartsWith("Outer cloud") ? "outer cloud" : t.name;
            kinds[label] = kinds.TryGetValue(label, out var kc) ? kc + 1 : 1;
            string path = ClaudeCloudInspect.Path(t);
            var e = t.eulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(e.x, 0)) > .5f || Mathf.Abs(Mathf.DeltaAngle(e.z, 0)) > .5f) { report.Add("SKIP tilted " + path); untouched++; continue; }
            var frame = Matrix4x4.TRS(t.position, Quaternion.Euler(0, e.y, 0), Vector3.one); var toFrame = frame.inverse;
            // current boxes in the frame
            float refTop = float.MinValue, thickBefore = 0; var fp = new Bounds(); bool fpSet = false;
            foreach (var b in boxes)
            {
                var ls = t.lossyScale; var c = toFrame.MultiplyPoint3x4(t.TransformPoint(b.center));
                var s = new Vector3(Mathf.Abs(b.size.x * ls.x), Mathf.Abs(b.size.y * ls.y), Mathf.Abs(b.size.z * ls.z));
                refTop = Mathf.Max(refTop, c.y + s.y / 2); thickBefore = Mathf.Max(thickBefore, s.y);
                var bb = new Bounds(c, s); if (!fpSet) { fp = bb; fpSet = true; } else fp.Encapsulate(bb);
            }
            // the cloud this collider belongs to
            Transform owner = t; List<Renderer> vis = Visual(t, kind);
            for (int up = 0; up < 2 && vis.Count == 0 && kind != Kind.Spiral && owner.parent != null; up++) { owner = owner.parent; vis = Visual(owner, kind); }
            if (vis == null || vis.Count == 0) { report.Add("NO VISIBLE CLOUD (left unchanged, flagged) " + path + " boxes " + boxes.Count + " top " + F(refTop) + " footprint " + V(fp.size)); untouched++; problems++; continue; }
            var g = Raster(vis, toFrame, refTop, fp);

            // how the current boxes sit: footprint outside the outline, thickness, top vs surface
            // samples every quarter cell, 2 cm in from the box edges; "above" = the slab top is more than 2 cm over the
            // rasterised visible surface there (the grid can't resolve finer than that)
            bool strict = validateOnly;
            Func<IEnumerable<Bounds>, (float outside, float above)> Measure = bs =>
            {
                int n = 0, outN = 0, aboveN = 0; float st = g.cell * .25f;
                foreach (var bb in bs)
                    for (float x = bb.min.x + .02f; x <= bb.max.x - .02f; x += st)
                        for (float z = bb.min.z + .02f; z <= bb.max.z - .02f; z += st)
                        {
                            n++; int cx = g.CX(x), cz = g.CZ(z);
                            if (!g.Occ(cx, cz)) { outN++; continue; }
                            float sv = strict ? g.top[cx + cz * g.W] : g.surf[cx + cz * g.W]; if (float.IsNaN(sv) || sv < bb.max.y - .02f) aboveN++;
                        }
                return (n == 0 ? 0 : outN / (float)n, n == 0 ? 0 : aboveN / (float)n);
            };
            var beforeBs = boxes.Select(b =>
            {
                var ls = t.lossyScale; var c = toFrame.MultiplyPoint3x4(t.TransformPoint(b.center));
                return new Bounds(c, new Vector3(Mathf.Abs(b.size.x * ls.x), Mathf.Abs(b.size.y * ls.y), Mathf.Abs(b.size.z * ls.z)));
            }).ToList();
            var before = Measure(beforeBs);
            string head = path + " [" + label + "] visual " + vis.Count + " renderer(s) under " + owner.name;
            string was = "before: " + boxes.Count + " box(es), thickest " + F(thickBefore) + " m, footprint " + F(fp.size.x) + "x" + F(fp.size.z) + " m, " + (before.outside * 100).ToString("0.0") + "% outside the outline, " + (before.above * 100).ToString("0.0") + "% above the visible top";
            if (validateOnly)
            {
                bool ok = thickBefore <= Thick + .011f && before.outside < .001f && before.above < .001f;
                if (!ok) problems++;
                report.Add((ok ? "OK   " : "FAIL ") + head + " | " + was.Replace("before: ", ""));
                boxesAfter += boxes.Count; continue;
            }

            // standing cells: inside the collider's own footprint (slabs only ever shrink; spiral +0.3 m because the mesh is new),
            // cloud surface at/above the standing height, and at least ~0.1 m in from the outline
            int W = g.W, H = g.H; var stand = new bool[W * H];
            int margin = Mathf.Max(1, Mathf.CeilToInt(.1f / g.cell));
            float grow = kind == Kind.Spiral ? .3f : 0f;
            Func<int, int, bool> inFoot = (x, z) =>
            {
                float px = g.x0 + (x + .5f) * g.cell, pz = g.z0 + (z + .5f) * g.cell;
                foreach (var bb in beforeBs) if (px > bb.min.x - grow && px < bb.max.x + grow && pz > bb.min.z - grow && pz < bb.max.z + grow) return true;
                return false;
            };
            var inside = new bool[W * H]; var footSurf = new List<float>();
            for (int z = 0; z < H; z++) for (int x = 0; x < W; x++)
                {
                    if (!inFoot(x, z)) continue;
                    bool ok = true;
                    for (int dz = -margin; dz <= margin && ok; dz++) for (int dx = -margin; dx <= margin; dx++) if (!g.Occ(x + dx, z + dz)) { ok = false; break; }
                    inside[x + z * W] = ok;
                    if (ok && !float.IsNaN(g.surf[x + z * W])) footSurf.Add(g.surf[x + z * W]);
                }
            float standAt = refTop; string lowered = "";
            Action fill = () => { for (int k = 0; k < W * H; k++) stand[k] = inside[k] && !float.IsNaN(g.surf[k]) && g.surf[k] >= standAt - .14f; };
            fill();
            if (stand.Count(v => v) < footSurf.Count * .3f && footSurf.Count > 0)
            {
                // the collider floats above the visible top here: lower it to just under the top most of its footprint shows
                footSurf.Sort(); float p75 = footSurf[Mathf.Clamp((int)(footSurf.Count * .75f), 0, footSurf.Count - 1)];
                if (p75 < refTop && refTop - p75 <= .6f) { standAt = p75; fill(); lowered = " (was floating " + F(refTop - p75) + " m above the visible top; lowered)"; }
            }
            float cellA = g.cell * g.cell; int standN = 0, covered = 0;
            int maxBoxes = kind == Kind.Spiral ? 3 : t.name == "Cloud box floor" ? 2 : 24;
            List<Slab> slabs = null;
            Func<float, float, List<Slab>> search = (minArea, minSide) =>
            {
                standN = stand.Count(v => v); covered = 0;
                var res = new List<Slab>(); var work = (bool[])stand.Clone();
                while (res.Count < maxBoxes)
                {
                    var r = MaxRect(work, W, H); float area = r.w * r.h * cellA;
                    if (r.w == 0 || area < minArea || (res.Count > 0 && area < standN * cellA * (kind == Kind.Spiral ? .1f : .01f))) break;
                    if (r.w * g.cell < minSide || r.h * g.cell < minSide) break;
                    float minS = float.MaxValue;
                    for (int z = r.z; z < r.z + r.h; z++) for (int x = r.x; x < r.x + r.w; x++) work[x + z * W] = false;
                    // height: lowest visible top over the rectangle AND the cells around it (the rasteriser can't see folds finer than a cell)
                    for (int z = Mathf.Max(0, r.z - 1); z < Mathf.Min(H, r.z + r.h + 1); z++) for (int x = Mathf.Max(0, r.x - 1); x < Mathf.Min(W, r.x + r.w + 1); x++) { float sv = g.surf[x + z * W]; if (!float.IsNaN(sv)) minS = Mathf.Min(minS, sv); }
                    covered += r.w * r.h;
                    float top = Mathf.Min(standAt, minS - .02f);
                    res.Add(new Slab { c = new Vector3(g.x0 + (r.x + r.w * .5f) * g.cell, top - Thick / 2, g.z0 + (r.z + r.h * .5f) * g.cell), size = new Vector3(r.w * g.cell, Thick, r.h * g.cell), minSurf = minS });
                    if (covered >= standN * (kind == Kind.Spiral ? .93f : .95f)) break;
                }
                return res;
            };
            slabs = search(kind == Kind.Spiral ? .25f : .2f, .3f);
            if (slabs.Count == 0 && footSurf.Count > 0)
            {
                // small or very domed cloud: stand at the middle of what its top shows, with a smaller minimum slab
                footSurf.Sort(); float p50 = footSurf[footSurf.Count / 2];
                if (p50 < standAt && refTop - p50 <= .35f) { standAt = p50; fill(); lowered = " (small cloud: standing at the middle of its top)"; }
                slabs = search(.08f, .2f);
            }
            if (slabs.Count == 0) { report.Add("NO STANDING AREA (left unchanged, flagged) " + head + " | " + was); untouched++; problems++; continue; }
            var after = Measure(slabs.Select(s => new Bounds(s.c, s.size)));
            float drop = refTop - slabs.Max(s => s.c.y + Thick / 2);
            string will = "after: " + slabs.Count + " slab(s) " + F(Thick) + " m, covering " + (covered * 100f / Mathf.Max(1, standN)).ToString("0") + "% of the standable top (" + F(standN * cellA) + " m2), " + (after.outside * 100).ToString("0") + "% outside, " + (after.above * 100).ToString("0") + "% above top, standing height " + (drop < .005f ? "unchanged" : "-" + F(drop) + " m") + lowered + "; boxes " + string.Join(" ", slabs.Select(s => F(s.size.x) + "x" + F(s.size.z)));
            report.Add(head + "\n    " + was + "\n    " + will);
            boxesAfter += slabs.Count;
            if (!apply) continue;

            // write: reuse the first BoxCollider (keeps references), add or remove the rest on the same GameObject
            var ls2 = t.lossyScale;
            for (int i = 0; i < slabs.Count; i++)
            {
                BoxCollider b = i < boxes.Count ? boxes[i] : Undo.AddComponent<BoxCollider>(t.gameObject);
                Undo.RecordObject(b, "cloud redo collider");
                b.isTrigger = false; if (i > 0) b.sharedMaterial = boxes[0].sharedMaterial;
                var wc = frame.MultiplyPoint3x4(slabs[i].c);
                b.center = t.InverseTransformPoint(wc);
                b.size = new Vector3(slabs[i].size.x / Mathf.Abs(ls2.x), slabs[i].size.y / Mathf.Abs(ls2.y), slabs[i].size.z / Mathf.Abs(ls2.z));
                EditorUtility.SetDirty(b);
            }
            for (int i = slabs.Count; i < boxes.Count; i++) Undo.DestroyObjectImmediate(boxes[i]);
            changed++;
        }
        report.Add("");
        report.Add("summary: " + (apply ? changed + " objects rebuilt" : "dry run") + ", " + untouched + " left unchanged, " + problems + " flagged; box colliders " + boxesBefore + " -> " + boxesAfter);
        report.Add("by kind: " + string.Join(", ", kinds.Select(k => k.Key + " " + k.Value)));
        var excluded = all.Where(b => b != null && Classify(b) == null && !b.isTrigger).Select(b => b.transform).Where(x => { for (var p = x; p != null; p = p.parent) if (p.name.ToLowerInvariant().Contains("cloud")) return true; return false; })
            .GroupBy(x => x.name).Select(x => x.Key + " x" + x.Count());
        report.Add("not touched on purpose (furniture function, seats, deck, booths, triggers excluded): " + string.Join(", ", excluded));
    }

    // ------------------------------------------------------------------ photos
    static void Photos(string label)
    {
        Directory.CreateDirectory(Out);
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>();
        var saved = new List<(Transform, Quaternion)>();
        if (orbit != null)
        {
            foreach (var p in orbit.pivots) saved.Add((p, p.localRotation));
            foreach (var c in orbit.clouds) if (c != null) saved.Add((c, c.localRotation));
            for (int i = 0; i < orbit.pivots.Length; i++) orbit.pivots[i].localRotation = Quaternion.Euler(0, orbit.phases[i] + 35 * orbit.speeds[i], 0);
            for (int j = 0; j < orbit.clouds.Length; j++) if (orbit.clouds[j] != null) orbit.clouds[j].localRotation = Quaternion.Euler(0, orbit.cloudYaw[j] + 35 * orbit.cloudSpin[j], 0);
        }
        var overlay = new List<GameObject>();
        try
        {
            Shot(label + " 1 spiral side", new Vector3(22, 24, 12), new Vector3(0, 20, 0), 55, 1600, 900);
            Shot(label + " 2 spiral birdseye", new Vector3(.4f, 48, .3f), new Vector3(0, 20, 0), 60, 1600, 900);
            // close-ups of orbit clouds high on the spiral (clear of the lounge), from beside and from a little above
            if (orbit != null)
            {
                var near = orbit.clouds.Where(c => c != null && c.GetComponent<MeshRenderer>() != null && c.GetComponent<MeshRenderer>().enabled)
                    .OrderBy(c => (c.position - new Vector3(10, 46, 0)).sqrMagnitude).Take(2).ToList();
                int n = 0;
                foreach (var c in near)
                {
                    var cp = c.position + Vector3.down * .4f * c.lossyScale.y; var ow = new Vector3(cp.x, 0, cp.z).normalized; float sz = Mathf.Max(c.lossyScale.x, c.lossyScale.z);
                    Shot(label + " 3" + (char)('a' + n++) + " orbit cloud close", cp + ow * 3.4f * sz + Vector3.up * 1.5f * sz + Vector3.Cross(Vector3.up, ow) * 1.2f * sz, cp, 45, 1600, 900);
                }
            }
            var outer = GameObject.Find("14 - Outer big clouds (Claude)");
            if (outer != null)
            {
                var oc = outer.transform.Find("Outer cloud 20"); if (oc == null) oc = outer.transform.GetChild(0);
                var p = oc.position; var inw = -new Vector3(p.x, 0, p.z).normalized;
                Shot(label + " 4 outer clouds", p + inw * 20 + Vector3.up * 6, p, 55, 1600, 900);
                Shot(label + " 5 outer cloud birdseye", p + Vector3.up * 16 + inw * .3f, p, 55, 1600, 900);
            }
            var fire = GameObject.Find("Cloud firepit");
            if (fire != null) Shot(label + " 6 lounge furniture texture", fire.transform.position + new Vector3(7, 4.5f, 7), fire.transform.position + Vector3.up * .5f, 55, 1600, 900);

            // collider overlays: magenta = every solid cloud support box, drawn through the cloud
            var mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0); mat.SetInt("_ZTest", (int)CompareFunction.Always); mat.SetInt("_Cull", 0); mat.color = new Color(1, 0, .75f, .5f);
            foreach (var b in Resources.FindObjectsOfTypeAll<BoxCollider>().Where(b => b.gameObject.scene.IsValid() && b.gameObject.activeInHierarchy && b.enabled && Classify(b) != null))
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(q.GetComponent<Collider>());
                q.hideFlags = HideFlags.DontSave; q.name = "overlay";
                q.transform.SetPositionAndRotation(b.transform.TransformPoint(b.center), b.transform.rotation);
                var ls = b.transform.lossyScale; q.transform.localScale = new Vector3(b.size.x * ls.x, Mathf.Max(b.size.y * ls.y, .02f), b.size.z * ls.z);
                q.GetComponent<MeshRenderer>().sharedMaterial = mat; overlay.Add(q);
            }
            var samples = new List<Transform>();
            if (orbit != null)
            {
                foreach (var nm in new[] { "Heart cloud", "Star cloud", "Booth cloud" })
                { var s = orbit.clouds.FirstOrDefault(c => c != null && c.Find(nm) != null); if (s != null) samples.Add(s); }
                foreach (var s in orbit.clouds.Where(c => c != null && c.GetComponent<MeshRenderer>() != null && c.GetComponent<MeshRenderer>().enabled).GroupBy(c => c.GetComponent<MeshFilter>().sharedMesh.name).Select(g2 => g2.First()).Take(4)) samples.Add(s);
            }
            foreach (var nm in new[] { "Outer cloud 20", "Crystal vine cloud 3", "Cloud firepit", "Cloud swing", "12 - DJ cloud", "Duck cloud", "Private box 1", "Screen top cloud" })
            { var s = Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == nm); if (s != null) samples.Add(s); }
            int k = 0;
            foreach (var s in samples)
            {
                var rs = s.GetComponentsInChildren<Renderer>().Where(r => CloudRenderer(r)).ToArray();
                if (rs.Length == 0) continue;
                var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                float ext = Mathf.Max(bb.extents.x, bb.extents.z) * 1.15f + .3f;
                string nm = label + " collider " + (++k).ToString("00") + " " + s.name;
                Ortho(nm + " top", bb.center + Vector3.up * (bb.extents.y + 2), Quaternion.Euler(90, 0, 0), ext, .05f, bb.size.y + 4);
                Ortho(nm + " side", bb.center - Vector3.forward * (ext + 2) + Vector3.zero, Quaternion.identity, Mathf.Max(bb.extents.y * 1.4f, ext * .45f), .05f, ext * 2 + 4, ext * 16f / 9f);
            }
        }
        finally
        {
            foreach (var o in overlay) Object.DestroyImmediate(o);
            foreach (var (tr, q) in saved) tr.localRotation = q;
        }
        Debug.Log("CLOUD_REDO_PHOTOS_OK " + label);
    }

    static void Shot(string name, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude photo camera") { hideFlags = HideFlags.DontSave };
        var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .05f; cam.farClipPlane = 2000;
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        Render(cam, name, w, h); Object.DestroyImmediate(go);
    }

    static void Ortho(string name, Vector3 pos, Quaternion rot, float half, float near, float far, float halfW = 0)
    {
        var go = new GameObject("Claude ortho camera") { hideFlags = HideFlags.DontSave };
        var cam = go.AddComponent<Camera>(); cam.transform.SetPositionAndRotation(pos, rot);
        cam.orthographic = true; cam.orthographicSize = halfW > 0 ? halfW * 9f / 16f : half; cam.nearClipPlane = near; cam.farClipPlane = far;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.16f, .12f, .22f); cam.allowHDR = false;
        Render(cam, name, halfW > 0 ? 1600 : 1000, halfW > 0 ? 900 : 1000); Object.DestroyImmediate(go);
    }

    static void Render(Camera cam, string name, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(tex);
    }
}
