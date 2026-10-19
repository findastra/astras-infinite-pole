// Furniture 2x (Claude, 2026-09-26). Menu: Astra > Claude > 8 Furniture twice as big.
// Rollback: Astra > Claude > Restore scene from before furniture 2x.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraClaudeBigFurniture
{
    const string Backup = "Review/Backups/BeforeFurniture2x.unity.txt";

    [MenuItem("Astra/Claude/8 Furniture twice as big")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge == null) { EditorUtility.DisplayDialog("Furniture 2x", "Cloud lounge not found.", "OK"); return; }
        if (lounge.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Furniture 2x")) { EditorUtility.DisplayDialog("Furniture 2x", "Already applied.", "OK"); return; }
        EditorSceneManager.SaveScene(scene); File.Copy(scene.path, Backup, true);
        var groups = new List<Transform>();
        var fur = lounge.transform.Find("Cloud furniture"); if (fur != null) groups.AddRange(fur.Cast<Transform>());
        var tub = lounge.transform.Find("Bathtub cloud"); if (tub != null) groups.Add(tub);
        var am = lounge.transform.Find("Cloud amenities"); if (am != null) groups.AddRange(am.Cast<Transform>().Where(t => t.name != "Cloud lamp"));
        var report = new List<string>();
        foreach (var g in groups)
        {
            var pad = g.Find("Lift pad"); var steps = g.Find("Stepping clouds");
            var items = g.Cast<Transform>().Where(t => t != pad && t != steps).ToList();
            var holder = new GameObject("Furniture 2x").transform; holder.SetParent(g, false);
            foreach (var t in items) t.SetParent(holder, false);
            holder.localScale = Vector3.one * 2f;
            if (pad != null)
            {
                var plat = pad.Find("Platform cloud"); var oldHalf = plat != null ? Mathf.Max(plat.GetComponent<Renderer>().bounds.extents.x, plat.GetComponent<Renderer>().bounds.extents.z) : 2f;
                var oldCenter = pad.position;
                var lp = pad.localPosition; pad.localPosition = new Vector3(lp.x * 2f, lp.y, lp.z * 2f);
                pad.localScale = new Vector3(pad.localScale.x * 2f, pad.localScale.y, pad.localScale.z * 2f);
                float push = oldHalf - .7f;
                if (steps != null) foreach (Transform s in steps)
                    {
                        var d = s.position - oldCenter; d.y = 0; if (d.sqrMagnitude < .01f) d = Vector3.forward;
                        s.position = new Vector3(pad.position.x, s.position.y, pad.position.z) + d + d.normalized * push;
                    }
            }
            report.Add(g.name + ": 2x" + (pad != null ? ", platform widened, stepping clouds moved out" : ""));
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-furniture2x-report.txt", string.Join("\n", report));
        Debug.Log("ASTRA_FURNITURE_2X " + groups.Count + " groups");
    }

    [MenuItem("Astra/Claude/Restore scene from before furniture 2x")]
    public static void Restore()
    {
        if (!File.Exists(Backup) || !EditorUtility.DisplayDialog("Restore", "Put the scene back to before furniture 2x?", "Restore", "Cancel")) return;
        var path = EditorSceneManager.GetActiveScene().path; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); EditorSceneManager.OpenScene(path);
    }
}
