// Claude, 2026-10-07. Menu: Astra > Claude > 37 Theatre out, sea puffs match, comfy smooth clouds
// Owner: "the new cloud texture is alright, still slightly jagged. needs to be comfy. some of the clouds didnt get done.
//         these pink clouds, fix the coloring to match the rest. get rid of the entire theatre seating."
//  1. Deletes "17 - Cloud theatre (Claude)" (chairs, private boxes, screen-top cloud) and its meshes/material.
//  2. Pink Sea Puff (the bright pink round billows of the cloud sea and the banner mound) gets the same white-pastel
//     colours and soft texture as the other clouds. The sea surface itself is not touched.
//  3. Lounge clouds: EVERY group of round puffs in the cloud lounge (889 were still separate puffs, only the
//     "Furniture 2x" parts had been melted) is melted into one soft shape, and the 107 older melted shapes are rebuilt:
//     finer grid, iso chosen so a lone puff keeps its exact size (joins only fill in, so the thin support slabs stay
//     under the visible top), then Taubin smoothing (no shrinking) and normals from the smooth field. Old puffs are
//     switched off, not deleted. The DJ cloud is left alone (its puffs are carved round the booth).
//  5. Fuzzy texture on every cloud material (owner: "dented tin foil"): bump relief off, cotton fibres + velvet sheen.
//  4. Orbit Cloud 0-7 / Outer Cloud 0-5 (the redo's fused clouds) get a smoother copy: one Loop subdivision step
//     (rounder outline, no facets), vertex colours carried over; renderers swap to "... smooth" meshes.
// Photos: Review/Photos/Round37/. Report: Review/claude-round37-report.txt.
// Backup: Review/Backups/BeforeRound37/AstrasInfinitePole.unity.txt; menu 37r puts that scene back.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeRound37
{
    const string Root = "Assets/Astra/";
    const string Bak = "Review/Backups/BeforeRound37/AstrasInfinitePole.unity.txt";
    static List<string> report;
    static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("STEP FAILED (" + name + "): " + e.Message + "\n" + e.StackTrace); } }

    [MenuItem("Astra/Claude/37 Theatre out, sea puffs match, comfy smooth clouds")]
    public static void Run()
    {
        report = new List<string>();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(Bak));
        if (!File.Exists(Bak)) File.Copy(scene.path, Bak, true);
        Step("photos before", () => Photos("before"));
        Step("theatre", Theatre);
        Step("sea puffs", SeaPuffs);
        Step("lounge", Lounge);
        Step("smooth fused", SmoothFused);
        Step("fuzzy", Fuzzy);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Step("photos after", () => Photos("after"));
        File.WriteAllText("Review/claude-round37-report.txt", string.Join("\n", report));
    }

    [MenuItem("Astra/Claude/37r Restore scene from before round 37")]
    public static void Restore()
    {
        var path = EditorSceneManager.GetActiveScene().path;
        File.Copy(Bak, path, true); AssetDatabase.ImportAsset(path); EditorSceneManager.OpenScene(path);
        File.WriteAllText("Review/claude-round37-report.txt", "restored " + path + " from " + Bak + " at " + DateTime.Now.ToString("HH:mm:ss"));
    }

    // ================================================================= 1. theatre
    static void Theatre()
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.parent == null && t.gameObject.scene.IsValid() && t.name == "17 - Cloud theatre (Claude)").ToList();
        foreach (var t in all) Object.DestroyImmediate(t.gameObject);
        bool m = AssetDatabase.DeleteAsset(Root + "Meshes/Theatre");
        bool mat = AssetDatabase.DeleteAsset(Root + "Materials/Theatre Bubble Chair.mat");
        report.Add("theatre: removed " + all.Count + " root(s); meshes folder " + (m ? "deleted" : "not found") + ", chair material " + (mat ? "deleted" : "not found"));
    }

    // ================================================================= 2. sea puffs
    static void SeaPuffs()
    {
        var sea = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Backdrop/Pink Sea Puff.mat");
        var refm = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Glitter Cloud.mat");
        if (sea == null || refm == null) { report.Add("sea puffs: CHECK material missing"); return; }
        foreach (var p in new[] { "_Top", "_Bottom", "_Rim", "_Fold" }) if (refm.HasProperty(p)) sea.SetColor(p, refm.GetColor(p));
        foreach (var p in new[] { "_Pastel", "_ColorSpeed", "_TextureScale" }) if (refm.HasProperty(p)) sea.SetFloat(p, refm.GetFloat(p));
        sea.SetFloat("_CloudTex", 1f); sea.EnableKeyword("_CLOUDTEX"); sea.SetFloat("_Texture", .7f); sea.SetFloat("_VColor", 0f);
        EditorUtility.SetDirty(sea);
        report.Add("sea puffs: Pink Sea Puff now matches Glitter Cloud (top " + sea.GetColor("_Top") + ", bottom " + sea.GetColor("_Bottom") + ", pastel " + sea.GetFloat("_Pastel") + ", soft texture .7)");
    }

    // ================================================================= 2b. fuzzy texture on every cloud material
    // (owner: "the clouds look like dented tin foil. give them all a fuzzy texture"). Not the DJ deck, booths, payphones,
    // duck, glow orbs or porcelain, which share the shader but are not clouds.
    static void Fuzzy()
    {
        var wanted = new HashSet<string> { "Lounge Pink", "Lounge Lilac", "Lounge Peach", "Pink Sea Puff" };
        var done = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "Materials" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.shader == null || m.shader.name != "Astra/Glitter Cloud") continue;
            if (!(m.name.StartsWith("Glitter Cloud") || wanted.Contains(m.name))) continue;
            m.SetFloat("_CloudTex", 1f); m.EnableKeyword("_CLOUDTEX");
            if (m.GetFloat("_Texture") < .5f) m.SetFloat("_Texture", .7f);
            m.SetFloat("_Fuzz", 1f); m.SetFloat("_Relief", 0f);
            EditorUtility.SetDirty(m); done.Add(m.name);
        }
        report.Add("fuzzy texture on (bump relief off): " + string.Join(", ", done));
    }

    // ================================================================= 3. lounge: melt every puff group, smoother
    struct Puff { public Matrix4x4 toPuff; public Vector3 c, e; }
    const float R = 1.6f;
    static readonly float Iso = (1f - 1f / (R * R)) * (1f - 1f / (R * R));   // a lone puff keeps its own size
    static bool IsPuff(Renderer r) { var mf = r.GetComponent<MeshFilter>(); return mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Glitter Cloud") && r.gameObject.activeInHierarchy; }
    static string Key(Transform t) { var s = t.name + "~" + t.GetSiblingIndex(); while (t.parent != null) { t = t.parent; s = t.name + "~" + t.GetSiblingIndex() + "/" + s; } return s; }

    static void Lounge()
    {
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge == null) { report.Add("lounge: not found"); return; }
        string dir = Root + "Meshes/Lounge/Merged";
        Directory.CreateDirectory(dir);
        int groups = 0, rebuilt = 0, puffsN = 0, singles = 0; long verts = 0, srcVerts = 0;
        var used = new HashSet<string>();
        var groupsList = lounge.GetComponentsInChildren<MeshRenderer>(true).Where(IsPuff).GroupBy(r => r.transform.parent).ToList();
        foreach (var g in groupsList)
        {
            var parent = g.Key;
            var oldMerged = parent.Cast<Transform>().Where(c => c.name == "Merged cloud shape").ToList();
            // puffs that show now, or that an older melted shape stands in for
            var rs = g.Where(r => r.enabled || oldMerged.Count > 0).ToList();
            if (rs.Count < 2) { if (rs.Count == 1) singles++; continue; }
            var ps = new List<Puff>(); var bmin = Vector3.one * float.MaxValue; var bmax = -bmin; float minExt = float.MaxValue;
            foreach (var r in rs)
            {
                var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds;
                var toParent = parent.worldToLocalMatrix * r.transform.localToWorldMatrix;
                ps.Add(new Puff { toPuff = toParent.inverse, c = mb.center, e = new Vector3(Mathf.Max(mb.extents.x, 1e-3f), Mathf.Max(mb.extents.y, 1e-3f), Mathf.Max(mb.extents.z, 1e-3f)) });
                for (int k = 0; k < 8; k++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents * R, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                    var pp = toParent.MultiplyPoint3x4(corner); bmin = Vector3.Min(bmin, pp); bmax = Vector3.Max(bmax, pp);
                }
                minExt = Mathf.Min(minExt, MinAxis(toParent, mb.extents));
                if (r.enabled) srcVerts += r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            }
            var size = bmax - bmin;
            // grid step in the parent's space; minExt is in parent units too
            float h = Mathf.Max(Mathf.Max(size.x, Mathf.Max(size.y, size.z)) / 64f, minExt / 2.1f, 1e-3f);   // coarse grid is fine: Taubin removes the steps (finer was 3.7M vertices)
            var mesh = Build(ps, bmin - Vector3.one * 2 * h, size + Vector3.one * 4 * h, h);
            if (mesh == null || mesh.vertexCount == 0) { report.Add("lounge: CHECK " + PathOf(parent) + " made no surface; left as it was"); continue; }
            string safe = string.Join("_", PathOf(parent).Replace("13 - Cloud lounge (Claude)/", "").Split(Path.GetInvalidFileNameChars())).Replace('/', '-');
            string name = safe; int n = 2; while (used.Contains(name)) name = safe + " " + n++; used.Add(name);
            string path = dir + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            mesh.name = "Merged cloud " + name; AssetDatabase.CreateAsset(mesh, path);
            var mat = rs.GroupBy(r => r.sharedMaterial).OrderByDescending(x => x.Count()).First().Key;
            foreach (var o in oldMerged) Object.DestroyImmediate(o.gameObject);
            var go = new GameObject("Merged cloud shape"); go.transform.SetParent(parent, false);
            go.layer = rs[0].gameObject.layer; GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(rs[0].gameObject));
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            foreach (var r in rs) { r.enabled = false; EditorUtility.SetDirty(r); }
            groups++; if (oldMerged.Count > 0) rebuilt++; puffsN += rs.Count; verts += mesh.vertexCount;
        }
        // tidy: melted-shape assets no scene object uses any more (old names from round 30)
        int removed = 0;
        var inUse = new HashSet<Mesh>(Object.FindObjectsOfType<MeshFilter>(true).Select(f => f.sharedMesh).Where(m => m != null));
        foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { dir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid); var m = AssetDatabase.LoadAssetAtPath<Mesh>(p);
            if (m != null && !inUse.Contains(m)) { AssetDatabase.DeleteAsset(p); removed++; }
        }
        report.Add("lounge: " + groups + " soft cloud shapes (" + rebuilt + " were older melted shapes, rebuilt smoother; " + (groups - rebuilt) + " were still separate round puffs) from " + puffsN + " puffs; " + singles + " single puffs left as they are; visible puff vertices " + srcVerts + " -> " + verts + "; unused old melted meshes removed: " + removed);
    }
    static float MinAxis(Matrix4x4 m, Vector3 ext)
    {
        return Mathf.Min(m.MultiplyVector(new Vector3(ext.x, 0, 0)).magnitude, Mathf.Min(m.MultiplyVector(new Vector3(0, ext.y, 0)).magnitude, m.MultiplyVector(new Vector3(0, 0, ext.z)).magnitude));
    }
    static float Field(List<Puff> ps, Vector3 p)
    {
        float s = 0f;
        for (int i = 0; i < ps.Count; i++)
        {
            Vector3 q = ps[i].toPuff.MultiplyPoint3x4(p) - ps[i].c;
            float x = q.x / ps[i].e.x, y = q.y / ps[i].e.y, z = q.z / ps[i].e.z;
            float d = (x * x + y * y + z * z) / (R * R);
            if (d < 1f) { float k = 1f - d; s += k * k; }
        }
        return s;
    }
    static Mesh Build(List<Puff> ps, Vector3 o, Vector3 size, float h)
    {
        int nx = Mathf.CeilToInt(size.x / h), ny = Mathf.CeilToInt(size.y / h), nz = Mathf.CeilToInt(size.z / h);
        const long Cap = 9000000;
        if ((long)(nx + 1) * (ny + 1) * (nz + 1) > Cap) { float k = Mathf.Pow((nx + 1f) * (ny + 1f) * (nz + 1f) / Cap, 1f / 3f); h *= k; nx = Mathf.CeilToInt(size.x / h); ny = Mathf.CeilToInt(size.y / h); nz = Mathf.CeilToInt(size.z / h); }
        int sx = nx + 1, sy = ny + 1, sz = nz + 1;
        var f = new float[(long)sx * sy * sz];
        foreach (var p in ps)
        {
            var inv = p.toPuff.inverse; var lo = Vector3.one * float.MaxValue; var hi = -lo;
            for (int k = 0; k < 8; k++) { var c = p.c + Vector3.Scale(p.e * R, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)); var w = inv.MultiplyPoint3x4(c); lo = Vector3.Min(lo, w); hi = Vector3.Max(hi, w); }
            int x0 = Mathf.Clamp(Mathf.FloorToInt((lo.x - o.x) / h), 0, nx), x1 = Mathf.Clamp(Mathf.CeilToInt((hi.x - o.x) / h), 0, nx);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((lo.y - o.y) / h), 0, ny), y1 = Mathf.Clamp(Mathf.CeilToInt((hi.y - o.y) / h), 0, ny);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((lo.z - o.z) / h), 0, nz), z1 = Mathf.Clamp(Mathf.CeilToInt((hi.z - o.z) / h), 0, nz);
            for (int z = z0; z <= z1; z++) for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                    {
                        var q = p.toPuff.MultiplyPoint3x4(o + new Vector3(x * h, y * h, z * h)) - p.c;
                        float a = q.x / p.e.x, b = q.y / p.e.y, c = q.z / p.e.z, d = (a * a + b * b + c * c) / (R * R);
                        if (d < 1f) { float k = 1f - d; f[((long)z * sy + y) * sx + x] += k * k; }
                    }
        }
        Func<int, int, int, float> F = (x, y, z) => f[((long)z * sy + y) * sx + x];
        var cellVert = new Dictionary<long, int>();
        var vs = new List<Vector3>();
        int[,] edges = { { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 }, { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        var cv = new float[8]; var cp = new Vector3[8];
        for (int z = 0; z < nz; z++) for (int y = 0; y < ny; y++) for (int x = 0; x < nx; x++)
                {
                    int mask = 0;
                    for (int k = 0; k < 8; k++) { int dx = k & 1, dy = (k >> 1) & 1, dz = (k >> 2) & 1; cv[k] = F(x + dx, y + dy, z + dz); cp[k] = new Vector3(x + dx, y + dy, z + dz); if (cv[k] > Iso) mask |= 1 << k; }
                    if (mask == 0 || mask == 255) continue;
                    var sum = Vector3.zero; int cnt = 0;
                    for (int e = 0; e < 12; e++)
                    {
                        int a = edges[e, 0], b = edges[e, 1];
                        if ((cv[a] > Iso) == (cv[b] > Iso)) continue;
                        float t = (Iso - cv[a]) / (cv[b] - cv[a]); sum += Vector3.Lerp(cp[a], cp[b], t); cnt++;
                    }
                    cellVert[((long)z * ny + y) * nx + x] = vs.Count; vs.Add(o + (sum / cnt) * h);
                }
        var tris = new List<int>();
        Func<int, int, int, int> CV = (x, y, z) => { if (x < 0 || y < 0 || z < 0 || x >= nx || y >= ny || z >= nz) return -1; int i; return cellVert.TryGetValue(((long)z * ny + y) * nx + x, out i) ? i : -1; };
        Action<int, int, int, int, Vector3> Quad = (a, b, c, d, outward) =>
        {
            if (a < 0 || b < 0 || c < 0 || d < 0) return;
            if (Vector3.Dot(Vector3.Cross(vs[b] - vs[a], vs[c] - vs[a]), outward) < 0) { var tmp = b; b = d; d = tmp; }
            // split along the shorter diagonal: fewer slivers, smoother silhouette
            if ((vs[a] - vs[c]).sqrMagnitude <= (vs[b] - vs[d]).sqrMagnitude) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(a); tris.Add(c); tris.Add(d); }
            else { tris.Add(a); tris.Add(b); tris.Add(d); tris.Add(b); tris.Add(c); tris.Add(d); }
        };
        for (int z = 0; z <= nz; z++) for (int y = 0; y <= ny; y++) for (int x = 0; x <= nx; x++)
                {
                    bool inside = F(x, y, z) > Iso;
                    if (x < nx && inside != (F(x + 1, y, z) > Iso)) Quad(CV(x, y - 1, z - 1), CV(x, y, z - 1), CV(x, y, z), CV(x, y - 1, z), inside ? Vector3.right : Vector3.left);
                    if (y < ny && inside != (F(x, y + 1, z) > Iso)) Quad(CV(x - 1, y, z - 1), CV(x, y, z - 1), CV(x, y, z), CV(x - 1, y, z), inside ? Vector3.up : Vector3.down);
                    if (z < nz && inside != (F(x, y, z + 1) > Iso)) Quad(CV(x - 1, y - 1, z), CV(x, y - 1, z), CV(x, y, z), CV(x - 1, y, z), inside ? Vector3.forward : Vector3.back);
                }
        f = null;
        var arr = vs.ToArray();
        Taubin(arr, tris, 10);
        var ns = new Vector3[arr.Length]; float e2 = h * .5f;
        for (int i = 0; i < arr.Length; i++)
        {
            var p = arr[i];
            var gr = new Vector3(Field(ps, p + new Vector3(e2, 0, 0)) - Field(ps, p - new Vector3(e2, 0, 0)), Field(ps, p + new Vector3(0, e2, 0)) - Field(ps, p - new Vector3(0, e2, 0)), Field(ps, p + new Vector3(0, 0, e2)) - Field(ps, p - new Vector3(0, 0, e2)));
            ns[i] = gr.sqrMagnitude > 1e-14f ? -gr.normalized : Vector3.up;
        }
        var m = new Mesh { indexFormat = arr.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        m.vertices = arr; m.normals = ns; m.SetTriangles(tris, 0); m.RecalculateBounds();
        return m;
    }
    // Taubin lambda|mu smoothing: removes the voxel stair-steps without shrinking the shape
    static void Taubin(Vector3[] v, List<int> tris, int iters)
    {
        var nb = new List<int>[v.Length]; for (int i = 0; i < v.Length; i++) nb[i] = new List<int>(8);
        for (int t = 0; t < tris.Count; t += 3)
            for (int k = 0; k < 3; k++) { int a = tris[t + k], b = tris[t + (k + 1) % 3]; if (!nb[a].Contains(b)) nb[a].Add(b); if (!nb[b].Contains(a)) nb[b].Add(a); }
        var tmp = new Vector3[v.Length];
        for (int it = 0; it < iters * 2; it++)
        {
            float w = (it & 1) == 0 ? .5f : -.53f;
            for (int i = 0; i < v.Length; i++)
            {
                if (nb[i].Count == 0) { tmp[i] = v[i]; continue; }
                var s = Vector3.zero; foreach (var j in nb[i]) s += v[j];
                tmp[i] = v[i] + w * (s / nb[i].Count - v[i]);
            }
            Array.Copy(tmp, v, v.Length);
        }
    }

    // ================================================================= 4. smoother fused clouds (Loop subdivision once)
    static void SmoothFused()
    {
        string dir = Root + "Meshes/CloudRedo";
        var map = new Dictionary<Mesh, Mesh>();
        foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { dir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid); var src = AssetDatabase.LoadAssetAtPath<Mesh>(p);
            if (src == null || src.name.EndsWith(" smooth") || p.EndsWith(" smooth.asset")) continue;
            var outPath = p.Replace(".asset", " smooth.asset");
            var m = Loop(src); m.name = src.name + " smooth";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(outPath);
            if (existing != null) { EditorUtility.CopySerialized(m, existing); EditorUtility.SetDirty(existing); map[src] = existing; }   // keep references on a rerun
            else { AssetDatabase.CreateAsset(m, outPath); map[src] = m; }
        }
        int swapped = 0; long before = 0, after = 0;
        foreach (var f in Object.FindObjectsOfType<MeshFilter>(true))
        {
            Mesh nm; if (f.sharedMesh == null || !map.TryGetValue(f.sharedMesh, out nm)) continue;
            before += f.sharedMesh.triangles.Length / 3; after += nm.triangles.Length / 3;
            f.sharedMesh = nm; EditorUtility.SetDirty(f); swapped++;
        }
        report.Add("fused clouds: " + map.Count + " smooth meshes (Loop x1), " + swapped + " renderers swapped; triangles " + before + " -> " + after);
    }
    static Mesh Loop(Mesh src)
    {
        var v = src.vertices; var col = src.colors; bool hasCol = col != null && col.Length == v.Length;
        var uv = src.uv; bool hasUv = uv != null && uv.Length == v.Length;
        var t = src.triangles; int n = v.Length;
        var nb = new List<int>[n]; for (int i = 0; i < n; i++) nb[i] = new List<int>(8);
        var edgeOpp = new Dictionary<long, List<int>>();
        Func<int, int, long> EK = (a, b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        for (int i = 0; i < t.Length; i += 3)
            for (int k = 0; k < 3; k++)
            {
                int a = t[i + k], b = t[i + (k + 1) % 3], c = t[i + (k + 2) % 3];
                if (!nb[a].Contains(b)) nb[a].Add(b); if (!nb[b].Contains(a)) nb[b].Add(a);
                List<int> l; long key = EK(a, b); if (!edgeOpp.TryGetValue(key, out l)) edgeOpp[key] = l = new List<int>(2); l.Add(c);
            }
        var nv = new List<Vector3>(); var nc = new List<Color>(); var nu = new List<Vector2>();
        // old vertices
        for (int i = 0; i < n; i++)
        {
            int k = nb[i].Count; bool boundary = nb[i].Any(j => edgeOpp[EK(i, j)].Count != 2);
            if (boundary || k < 3) { nv.Add(v[i]); if (hasCol) nc.Add(col[i]); if (hasUv) nu.Add(uv[i]); continue; }
            float beta = k == 3 ? 3f / 16f : 3f / (8f * k);
            var s = Vector3.zero; var sc = Color.clear; var su = Vector2.zero;
            foreach (var j in nb[i]) { s += v[j]; if (hasCol) sc += col[j]; if (hasUv) su += uv[j]; }
            nv.Add(v[i] * (1 - k * beta) + s * beta); if (hasCol) nc.Add(col[i] * (1 - k * beta) + sc * beta); if (hasUv) nu.Add(uv[i] * (1 - k * beta) + su * beta);
        }
        // edge vertices
        var edgeVert = new Dictionary<long, int>();
        foreach (var kv in edgeOpp)
        {
            int a = (int)(kv.Key >> 32), b = (int)(kv.Key & 0xffffffff);
            Vector3 p; Color c = Color.clear; Vector2 u = Vector2.zero;
            if (kv.Value.Count == 2)
            {
                int c0 = kv.Value[0], c1 = kv.Value[1];
                p = (v[a] + v[b]) * .375f + (v[c0] + v[c1]) * .125f;
                if (hasCol) c = (col[a] + col[b]) * .375f + (col[c0] + col[c1]) * .125f;
                if (hasUv) u = (uv[a] + uv[b]) * .375f + (uv[c0] + uv[c1]) * .125f;
            }
            else { p = (v[a] + v[b]) * .5f; if (hasCol) c = (col[a] + col[b]) * .5f; if (hasUv) u = (uv[a] + uv[b]) * .5f; }
            edgeVert[kv.Key] = nv.Count; nv.Add(p); if (hasCol) nc.Add(c); if (hasUv) nu.Add(u);
        }
        var nt = new List<int>(t.Length * 4);
        for (int i = 0; i < t.Length; i += 3)
        {
            int a = t[i], b = t[i + 1], c = t[i + 2];
            int ab = edgeVert[EK(a, b)], bc = edgeVert[EK(b, c)], ca = edgeVert[EK(c, a)];
            nt.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
        }
        var m = new Mesh { indexFormat = nv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        m.SetVertices(nv); if (hasCol) m.SetColors(nc); if (hasUv) m.SetUVs(0, nu); m.SetTriangles(nt, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // ================================================================= photos
    static void Photos(string tag)
    {
        string dir = "Review/Photos/Round37/" + tag; Directory.CreateDirectory(dir);
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge != null)
        {
            foreach (var name in new[] { "Cloud sofa x1.9", "Cloud bed x1.5", "Giant chair", "Reading nook", "Sleepy nook", "Beanbag pile", "Cloud firepit", "Cloud swing" })
            {
                var t = lounge.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name); if (t == null) continue;
                var rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var outw = new Vector3(b.center.x, 0, b.center.z).normalized; if (outw.sqrMagnitude < .01f) outw = Vector3.forward;
                Shot(dir, name, b.center + outw * b.extents.magnitude * 1.5f + Vector3.up * b.extents.y * 1.1f, b.center, 50, 1400, 900);
            }
        }
        var orbit = Object.FindObjectsOfType<MeshFilter>().FirstOrDefault(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("Orbit Cloud"));
        if (orbit != null) { var b = orbit.GetComponent<Renderer>().bounds; Shot(dir, "orbit cloud close", b.center + new Vector3(1, .5f, 1).normalized * b.extents.magnitude * 2.2f, b.center, 45, 1400, 900); }
        var sea = GameObject.Find("15 - Pink cloud sea (Claude)");
        var cin = GameObject.Find("Cinema - synchronized playlist");
        if (cin != null) Shot(dir, "big screen", cin.transform.position + new Vector3(-26, -4, 0), cin.transform.position + new Vector3(0, -5, 0), 60, 1400, 900);
        if (sea != null && lounge != null) Shot(dir, "lounge and sea", lounge.transform.position + new Vector3(30, 14, 30), lounge.transform.position + Vector3.down * 4, 60, 1600, 1000);
    }
    static void Shot(string dir, string name, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude photo camera"); var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 600f;
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
}
