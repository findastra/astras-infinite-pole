// Claude, 2026-10-07. Menu: Astra > Claude > 37b Lower lounge walk slabs 4 cm under the smoother clouds
// After round 37 smoothed the lounge clouds, 36g found 16 lounge "Cloud walk collider" slabs with 0.2-2.4% of their
// samples a little above the new visible top. Every lounge walk slab is lowered by 4 cm (world space), nothing else.
// Rerunning lowers them again, so it only runs once (marker: Review/claude-round37b-done.txt).
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeRound37b
{
    [MenuItem("Astra/Claude/37b Lower lounge walk slabs 4 cm")]
    public static void Run()
    {
        const string marker = "Review/claude-round37b-done.txt";
        if (File.Exists(marker)) { File.WriteAllText("Review/claude-round37b-report.txt", "already done (" + File.ReadAllText(marker).Trim() + "); nothing changed"); return; }
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        var lines = new List<string>(); int boxes = 0;
        foreach (var t in lounge.GetComponentsInChildren<Transform>(true).Where(x => x.name == "Cloud walk collider"))
        {
            foreach (var b in t.GetComponents<BoxCollider>())
            {
                if (b.isTrigger) continue;
                float sy = Mathf.Max(1e-4f, Mathf.Abs(t.lossyScale.y));
                // move the box straight down in world space, expressed in the collider's local space
                var localDown = t.InverseTransformDirection(Vector3.down) * (.04f / 1f);
                var c = b.center; c += new Vector3(localDown.x / Mathf.Max(1e-4f, Mathf.Abs(t.lossyScale.x)) * 0, localDown.y / sy * Mathf.Abs(t.lossyScale.y) * 0, 0);
                // (simple and exact for the usual unrotated / uniformly scaled slab holders:)
                c = b.center + t.InverseTransformVector(Vector3.down * .04f);
                b.center = c; EditorUtility.SetDirty(b); boxes++;
            }
            lines.Add(t.parent != null ? t.parent.parent != null ? t.parent.parent.name : t.parent.name : t.name);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        File.WriteAllText(marker, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        File.WriteAllText("Review/claude-round37b-report.txt", "lowered " + boxes + " walk slab boxes by 4 cm on: " + string.Join(", ", lines));
    }
}
