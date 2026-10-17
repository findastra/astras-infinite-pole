// Claude, 2026-10-07. Menu: Astra > Claude > 37a Cloud leftovers survey (report only, saves nothing)
// For the owner's notes "still slightly jagged / some clouds didn't get done / pink clouds don't match".
// Writes Review/claude-round37-survey.txt and lounge photos to Review/Photos/Round37/survey/.
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeRound37Survey
{
    static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }

    [MenuItem("Astra/Claude/37a Cloud leftovers survey (report only)")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var all = Object.FindObjectsOfType<MeshRenderer>(false).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();

        // 1. meshes on visible cloud-shader renderers: are they smooth-shaded? (split vertices = faceted)
        sb.AppendLine("== cloud meshes in view (Glitter Cloud shader) ==");
        var cloudRs = all.Where(r => r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == "Astra/Glitter Cloud").ToList();
        foreach (var g in cloudRs.Select(r => r.GetComponent<MeshFilter>()).Where(f => f != null && f.sharedMesh != null).GroupBy(f => f.sharedMesh).OrderByDescending(g => g.Count()).Take(60))
        {
            var m = g.Key; int tris = m.triangles.Length / 3; var v = m.vertices; var n = m.normals;
            // how many vertices share a position with another vertex (split normals), and the mean angle between a
            // vertex normal and the faces around it (flat shading -> ~0, smooth -> some degrees)
            var pos = new HashSet<Vector3Int>(); int dup = 0;
            foreach (var p in v) if (!pos.Add(Vector3Int.RoundToInt(p * 2000f))) dup++;
            sb.AppendLine(string.Format("{0,5} x  {1}  ({2}) verts {3} tris {4} split {5} path {6}", g.Count(), m.name, AssetDatabase.GetAssetPath(m), v.Length, tris, dup, PathOf(g.First().transform)));
        }

        // 2. visible ROUND puffs (Glitter Cloud k meshes) by parent: the clouds that were not fused yet
        sb.AppendLine("\n== visible round puffs (mesh 'Glitter Cloud k'), grouped by parent ==");
        var puffs = all.Where(r => { var f = r.GetComponent<MeshFilter>(); return f != null && f.sharedMesh != null && f.sharedMesh.name.StartsWith("Glitter Cloud"); }).ToList();
        sb.AppendLine("total visible round puffs: " + puffs.Count);
        foreach (var g in puffs.GroupBy(r => r.transform.parent).OrderByDescending(g => g.Count()))
            sb.AppendLine(string.Format("{0,4} puffs  mats [{1}]  comps-on-puffs [{2}]  parent {3}", g.Count(),
                string.Join(", ", g.Select(r => r.sharedMaterial != null ? r.sharedMaterial.name : "none").Distinct()),
                string.Join(", ", g.SelectMany(r => r.GetComponents<Component>()).Select(c => c.GetType().Name).Where(x => x != "Transform" && x != "MeshFilter" && x != "MeshRenderer").Distinct()),
                g.Key != null ? PathOf(g.Key) : "(root)"));

        // 3. every material on the cloud shader: colours and where it is used
        sb.AppendLine("\n== Glitter Cloud materials ==");
        foreach (var g in cloudRs.GroupBy(r => r.sharedMaterial).OrderByDescending(g => g.Count()))
        {
            var m = g.Key; Color t = m.GetColor("_Top"), b = m.GetColor("_Bottom"); float h, s, val, h2, s2, v2;
            Color.RGBToHSV(t, out h, out s, out val); Color.RGBToHSV(b, out h2, out s2, out v2);
            sb.AppendLine(string.Format("{0,5} x  {1}  ({2})  top {3} (sat {4:0.00})  bottom {5} (sat {6:0.00})  rim {7}  pastel {8:0.00}  tex {9}  e.g. {10}",
                g.Count(), m.name, AssetDatabase.GetAssetPath(m), t, s, b, s2, m.GetColor("_Rim"), m.GetFloat("_Pastel"), m.IsKeywordEnabled("_CLOUDTEX") ? m.GetFloat("_Texture").ToString("0.0") : "off",
                string.Join(" | ", g.Take(3).Select(r => PathOf(r.transform)))));
        }
        // other cloud-looking shaders (sea, sparkle clouds) for the pink question
        sb.AppendLine("\n== other shaders on objects named like clouds ==");
        foreach (var g in all.Where(r => r.sharedMaterial != null && r.sharedMaterial.shader.name != "Astra/Glitter Cloud" && r.name.ToLower().Contains("cloud")).GroupBy(r => r.sharedMaterial).OrderByDescending(g => g.Count()).Take(30))
            sb.AppendLine(string.Format("{0,5} x  {1}  shader {2}  e.g. {3}", g.Count(), g.Key.name, g.Key.shader.name, PathOf(g.First().transform)));

        // 4. theatre
        var th = GameObject.Find("17 - Cloud theatre (Claude)");
        sb.AppendLine("\n== theatre == " + (th == null ? "not found" : th.GetComponentsInChildren<Transform>(true).Length + " transforms, " + th.GetComponentsInChildren<VRC.SDK3.Components.VRCStation>(true).Length + " stations"));

        File.WriteAllText("Review/claude-round37-survey.txt", sb.ToString());

        // 5. photos of the lounge from four sides and above
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge != null)
        {
            string dir = "Review/Photos/Round37/survey"; Directory.CreateDirectory(dir);
            var rs = lounge.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            for (int i = 0; i < 4; i++)
            {
                var dirv = Quaternion.Euler(0, 45 + 90 * i, 0) * Vector3.forward;
                Shot(dir, "lounge " + (i + 1), bb.center + dirv * bb.extents.magnitude * .9f + Vector3.up * bb.extents.y * .8f, bb.center, 55, 1600, 1000);
            }
            Shot(dir, "lounge above", bb.center + Vector3.up * bb.extents.magnitude * 1.1f + Vector3.forward * .5f, bb.center, 60, 1400, 1400);
        }
    }

    static void Shot(string dir, string name, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude photo camera"); var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .05f; cam.farClipPlane = 600f;
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
