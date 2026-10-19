// Claude round 3e (2026-09-24): make every world panel's laser-click area cover all of its controls.
// The button check found the video controls (play, loop, mute, volume...) outside their panel's collider,
// so VRChat lasers passed through them. Grows each panel's collider to fit; never shrinks one. Safe to rerun.
// Menu: Astra > Claude Round 3e - Fix Clickable Areas
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClaudeRound3e
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    static readonly HashSet<string> TightPanels = new HashSet<string> { "ControlsUI", "InfoCanvas", "Progress" };

    [MenuItem("Astra/Claude Round 3e - Fix Clickable Areas")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3e.unity.txt", true);
        var report = new List<string>();
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>().Where(c => c.gameObject.scene == scene && c.isRootCanvas && c.renderMode == RenderMode.WorldSpace);
        foreach (var canvas in canvases)
        {
            var sels = canvas.GetComponentsInChildren<Selectable>(true).Where(x => x.GetComponentInParent<Canvas>(true).rootCanvas == canvas).ToArray(); // own controls only, not a nested panel's
            if (sels.Length == 0) continue;
            var box = canvas.GetComponent<BoxCollider>();
            if (box == null) { box = canvas.gameObject.AddComponent<BoxCollider>(); box.size = Vector3.zero; }
            var t = canvas.transform;
            // bounds of every control, in the canvas's own space
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            foreach (var s in sels)
            {
                var rt = (RectTransform)s.transform; var corners = new Vector3[4]; rt.GetWorldCorners(corners);
                foreach (var c in corners) { var p = t.InverseTransformPoint(c); min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            }
            var oldMin = box.center - box.size / 2; var oldMax = box.center + box.size / 2;
            bool tight = TightPanels.Contains(canvas.name); // small video-player panels sit next to each other: fit exactly so they don't overlap
            bool inside = !tight && box.size != Vector3.zero && min.x >= oldMin.x && min.y >= oldMin.y && max.x <= oldMax.x && max.y <= oldMax.y;
            if (inside) continue;
            float pad = tight ? 2f : 20f; // canvas units
            var nMin = (tight || box.size == Vector3.zero ? min : Vector3.Min(oldMin, min)) - new Vector3(pad, pad, 0);
            var nMax = (tight || box.size == Vector3.zero ? max : Vector3.Max(oldMax, max)) + new Vector3(pad, pad, 0);
            float depth = Mathf.Max(box.size.z, 1f);
            box.center = new Vector3((nMin.x + nMax.x) / 2, (nMin.y + nMax.y) / 2, box.center.z);
            box.size = new Vector3(nMax.x - nMin.x, nMax.y - nMin.y, depth);
            EditorUtility.SetDirty(box);
            report.Add((tight ? "Fitted" : "Grew") + " clickable area of \"" + canvas.name + "\" to cover " + sels.Length + " controls");
        }
        // The video progress bar is its own small panel inside the video controls, and its position is driven by the
        // video player's layout (moving it doesn't stick). Where it overlaps the Play/volume row, set its click area a
        // little BEHIND that row, so the buttons get the click there and the rest of the bar stays clickable. (Board fix.)
        var progress = canvases.FirstOrDefault(c => c.name == "Progress"); var controls = canvases.FirstOrDefault(c => c.name == "ControlsUI");
        if (progress != null && controls != null)
        {
            var pb = progress.GetComponent<BoxCollider>(); var cb = controls.GetComponent<BoxCollider>();
            if (pb != null && cb != null)
            {
                // express ControlsUI's back face in Progress's local z, then sit just behind it
                float cbBack = progress.transform.InverseTransformPoint(controls.transform.TransformPoint(cb.center + Vector3.forward * cb.size.z / 2)).z;
                float want = cbBack + pb.size.z / 2 + 1f;
                if (pb.center.z < want) { pb.center = new Vector3(pb.center.x, pb.center.y, want); EditorUtility.SetDirty(pb); report.Add("Set the video progress bar's click area just behind the Play/volume row"); }
            }
        }
        if (report.Count == 0) report.Add("All panels already covered their controls.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-round3e-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3E_OK " + string.Join(" | ", report));
    }
}
