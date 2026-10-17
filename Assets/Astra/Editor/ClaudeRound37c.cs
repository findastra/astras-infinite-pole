// Claude, 2026-10-07. Menu: Astra > Claude > 37c Remove every leftover theatre chair
// Owner: "get rid of all the theatre seats, weird chairs you just added". Round 37 removed the "17 - Cloud theatre"
// root; this sweeps the whole scene (active and inactive) for anything left of it: objects named "Theatre ..." and
// the SDK sample chairs (VRC_chair meshes / VRCChair prefab copies). Lounge cloud seats are not touched.
// Report (also lists every remaining VRCStation): Review/claude-round37c-report.txt.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeRound37c
{
    static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }

    [MenuItem("Astra/Claude/37c Remove every leftover theatre chair")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
        var kill = new HashSet<Transform>();
        foreach (var t in all)
        {
            if (t.name.StartsWith("Theatre") || t.name.StartsWith("17 - Cloud theatre") || t.name.StartsWith("Private cloud box") || t.name == "Screen top cloud") { kill.Add(t); continue; }
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("VRC_chair"))
            {
                // remove the whole chair (the copied prefab root), not just its mesh
                var root = t; while (root.parent != null && (root.parent.name.StartsWith("VRCChair") || root.parent.name.StartsWith("VRC_chair") || root.parent.name.StartsWith("Theatre chair"))) root = root.parent;
                kill.Add(root);
            }
            if (t.name.StartsWith("VRCChair")) kill.Add(t);
        }
        // only top-most of the set
        var top = kill.Where(k => !kill.Any(o => o != k && k.IsChildOf(o))).ToList();
        var lines = new List<string>();
        foreach (var k in top) { lines.Add("removed: " + PathOf(k)); }
        foreach (var k in top) Object.DestroyImmediate(k.gameObject);
        lines.Add(top.Count == 0 ? "no theatre chairs left in the scene" : "removed " + top.Count + " object trees");
        lines.Add("");
        lines.Add("seats still in the scene (VRCStation):");
        foreach (var st in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<VRC.SDK3.Components.VRCStation>(true)))
            lines.Add("  " + PathOf(st.transform));
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-round37c-report.txt", string.Join("\n", lines));
    }
}
