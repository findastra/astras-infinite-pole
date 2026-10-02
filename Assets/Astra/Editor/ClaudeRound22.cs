// Claude, 2026-10-01 (spacing tightened the same day). Menu: Astra > Claude > 22 Chandeliers strung like beads.
// Rebuilds the shared "Chandelier link" meshes IN PLACE (same assets, so every hanging strand and the swing chains
// update without touching the scene): gems now sit touching one after another like beads on a string, and each link
// starts and ends on the strand's centre line so links meet with no jump. Backup: Review/Backups/BeforeRound22-links/.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeRound22
{
    const string Root = "Assets/Astra/";
    static readonly float[] Lengths = { 1f, 1.5f, 2f };
    // 2026-10-01: .92 left the diamond tips just touching, which still read as gaps. .5 overlaps each crystal halfway
    // into the next so the strand reads as one continuous string of beads.
    public static float Spacing = .5f;

    [MenuItem("Astra/Claude/22 Chandeliers strung like beads")]
    public static void Run()
    {
        var report = new List<string>();
        var gem = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Meshes/Lounge/Crystal Gem.asset");
        if (gem == null) { File.WriteAllText("Review/claude-round22-report.txt", "FAIL: Crystal Gem mesh not found"); return; }
        Directory.CreateDirectory("Review/Backups/BeforeRound22-links");
        var gb = gem.bounds; float gemH = gb.size.y, gemW = Mathf.Max(gb.size.x, gb.size.z);
        report.Add("gem mesh size " + gb.size.ToString("0.00"));
        int meshes = 0, beads = 0; float worstGap = -1f;
        for (int li = 0; li < 3; li++) for (int pt = 0; pt < 4; pt++) for (int tip = 0; tip < 2; tip++)
        {
            string path = Root + "Meshes/Lounge/Chandelier link " + li + "-" + pt + "-" + tip + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (m == null) { report.Add("missing " + path); continue; }
            File.Copy(path, "Review/Backups/BeforeRound22-links/" + Path.GetFileName(path) + ".txt", true);
            var r2 = new System.Random(li * 100 + pt * 10 + tip); System.Func<float, float, float> Q = (a, b) => a + (float)r2.NextDouble() * (b - a);
            float L = Lengths[li], ph = Q(0, 6.3f), sway = Q(.025f, .06f);
            // centre line: a gentle wiggle that is zero at both ends so neighbouring links line up
            System.Func<float, Vector3> line = t => { float e = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / L)); return new Vector3(Mathf.Sin(t * 2.1f + ph) * sway * e, -t, Mathf.Cos(t * 1.7f + ph) * sway * .7f * e); };
            var list = new List<CombineInstance>(); var centres = new List<Vector3>(); var halfs = new List<float>();
            float t0 = 0f; int k = 0;
            while (true)
            {
                float s = (k % 2 == 0) ? Q(.095f, .11f) : Q(.07f, .085f);      // big / small alternating, like a bead string
                float half = gemH * s * .5f;
                float tc = t0 + half * Spacing;                                    // a hair of overlap so no daylight between beads
                if (tc + half > L + .001f) break;
                var pos = line(tc); var dir = (line(tc + .01f) - line(tc - .01f)).normalized;
                var rot = Quaternion.FromToRotation(Vector3.down, dir) * Quaternion.Euler(0, Q(0, 360), 0);
                list.Add(new CombineInstance { mesh = gem, transform = Matrix4x4.TRS(pos - rot * (gb.center * s), rot, Vector3.one * s) });
                centres.Add(pos); halfs.Add(half); t0 = tc + half * Spacing; k++;
            }
            // close the end: one more bead whose bottom sits exactly on the link's end, so the next link joins with no gap
            if (centres.Count > 0 && centres[centres.Count - 1].y * -1f + halfs[halfs.Count - 1] < L - .005f)
            {
                float s = Q(.08f, .095f), half = gemH * s * .5f, tc = L - half;
                var pos = line(tc); var dir = (line(tc + .01f) - line(tc - .01f)).normalized;
                var rot = Quaternion.FromToRotation(Vector3.down, dir) * Quaternion.Euler(0, Q(0, 360), 0);
                list.Add(new CombineInstance { mesh = gem, transform = Matrix4x4.TRS(pos - rot * (gb.center * s), rot, Vector3.one * s) });
                centres.Add(pos); halfs.Add(half); t0 = L; k++;
            }
            if (tip == 1)
            {
                float tsY = .3f, half = gemH * tsY * .5f;
                list.Add(new CombineInstance { mesh = gem, transform = Matrix4x4.TRS(new Vector3(0, -t0 - half * .9f, 0) - gb.center * tsY, Quaternion.identity, new Vector3(.2f, tsY, .2f)) });
            }
            for (int i = 1; i < centres.Count; i++) worstGap = Mathf.Max(worstGap, Vector3.Distance(centres[i], centres[i - 1]) - halfs[i] - halfs[i - 1]);
            var tmp = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; tmp.CombineMeshes(list.ToArray(), true, true);
            m.Clear(); m.indexFormat = tmp.vertexCount > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(tmp.vertices); m.SetNormals(tmp.normals); if (tmp.uv != null && tmp.uv.Length == tmp.vertexCount) m.SetUVs(0, tmp.uv);
            m.SetTriangles(tmp.triangles, 0); m.RecalculateBounds(); EditorUtility.SetDirty(m); Object.DestroyImmediate(tmp);
            meshes++; beads += centres.Count;
        }
        AssetDatabase.SaveAssets();
        report.Add("link meshes rebuilt in place: " + meshes + ", beads: " + beads);
        report.Add("largest gap between neighbouring beads: " + (worstGap * 100f).ToString("0.0") + " cm (negative = touching/overlapping)");
        // links meet end to end: each child link hangs exactly one link length below its parent
        int strands = 0, off = 0;
        foreach (var root in Object.FindObjectsOfType<Transform>(true).Where(t => t.name == "Chandelier strands"))
            foreach (Transform l0 in root) { strands++; var t = l0; while (t.childCount > 0) { var c = t.GetChild(0); if (Mathf.Abs(c.localPosition.x) > .001f || Mathf.Abs(c.localPosition.z) > .001f) off++; t = c; } }
        report.Add("hanging strands checked: " + strands + ", links not on the strand line: " + off);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes();
        File.WriteAllText("Review/claude-round22-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND22 " + string.Join(" | ", report));
    }
}
