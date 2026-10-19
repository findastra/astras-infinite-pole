// Follow-up to the cloud lounge pass: screen 4x area (2x each way) instead of 4x each way, and grow the
// World items panel downward so the new switches sit inside it. Menu: Astra > Claude > 4 Lounge fixes.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraClaudeLoungeFix
{
    [MenuItem("Astra/Claude/4 Lounge fixes")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene(); var report = new List<string>();
        File.Copy(scene.path, "Review/Backups/BeforeLoungeFix.unity.txt", true);
        // --- screen back to 2x each way (4x area), then pulled in toward the pole as far as the cloud paths allow
        var vs = Object.FindObjectsOfType<Renderer>(true).First(r => r.name == "VideoScreen");
        var cinema = vs.transform.parent != null && vs.transform.parent.name.StartsWith("Cinema") ? vs.transform.parent : vs.transform;
        if (Mathf.Abs(vs.transform.lossyScale.x) > 20f) cinema.localScale *= .5f;
        Physics.SyncTransforms();
        var orbit = Object.FindObjectOfType<AstraCloudOrbit>(); float maxR = 0;
        foreach (var c in orbit.clouds) { if (c == null) continue; var rr = c.GetComponent<Renderer>(); float ext = rr != null ? new Vector2(rr.bounds.extents.x, rr.bounds.extents.z).magnitude : 3f; maxR = Mathf.Max(maxR, new Vector2(c.position.x, c.position.z).magnitude + ext); }
        var mb = vs.GetComponent<MeshFilter>().sharedMesh.bounds;
        System.Func<float> nearest = () => { float n = float.MaxValue; for (int i = 0; i <= 8; i++) for (int j = 0; j <= 2; j++) { var w = vs.transform.TransformPoint(new Vector3(Mathf.Lerp(mb.min.x, mb.max.x, i / 8f), Mathf.Lerp(mb.min.y, mb.max.y, j / 2f), mb.center.z)); n = Mathf.Min(n, new Vector2(w.x, w.z).magnitude); } return n; };
        var bearing = new Vector3(vs.bounds.center.x, 0, vs.bounds.center.z).normalized;
        int guard = 0; while (nearest() > maxR + 2f && guard++ < 200) { cinema.position -= bearing * .25f; Physics.SyncTransforms(); }
        guard = 0; while (nearest() < maxR + 1.5f && guard++ < 200) { cinema.position += bearing * .25f; Physics.SyncTransforms(); }
        float bottom = vs.bounds.min.y; if (bottom < 1.5f) cinema.position += Vector3.up * (1.5f - bottom); else if (bottom > 3f) cinema.position -= Vector3.up * (bottom - 3f);
        Physics.SyncTransforms();
        var sc = vs.transform.TransformPoint(mb.center); var sh = Vector3.Scale(mb.extents, new Vector3(Mathf.Abs(vs.transform.lossyScale.x), Mathf.Abs(vs.transform.lossyScale.y), Mathf.Abs(vs.transform.lossyScale.z)));
        for (int i = 0; i < orbit.clearZones.Length; i++) if (orbit.clearZones[i] != null && orbit.clearZones[i].name.Contains("video")) { orbit.clearZones[i].position = sc; orbit.clearZones[i].rotation = vs.transform.rotation; orbit.clearHalf[i] = sh + new Vector3(4, 4, 5); }
        UdonSharpEditorUtility.CopyProxyToUdon(orbit);
        var kill = Object.FindObjectsOfType<BoxCollider>(true).FirstOrDefault(b => b.name == "Screen cloud kill zone");
        if (kill != null) { kill.transform.position = sc; kill.transform.rotation = vs.transform.rotation; kill.transform.localScale = Vector3.one; kill.size = Vector3.Scale(sh * 2 + new Vector3(8, 8, 10), new Vector3(1 / kill.transform.lossyScale.x, 1 / kill.transform.lossyScale.y, 1 / kill.transform.lossyScale.z)); }
        report.Add("screen now " + (sh.x * 2).ToString("0.0") + " x " + (sh.y * 2).ToString("0.0") + " m, nearest edge " + nearest().ToString("0.0") + " m, cloud paths end at " + maxR.ToString("0.0") + " m");
        // --- grow the World items panel downward so every switch is inside it
        RectTransform panel = null; foreach (var r in scene.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<RectTransform>(true)) if (t.name == "World items") panel = t;
        float H = panel.sizeDelta.y, low = float.MaxValue;
        foreach (Transform c in panel) { var r = c as RectTransform; if (r == null || c.name == "Opaque backing") continue; low = Mathf.Min(low, r.anchoredPosition.y - r.sizeDelta.y / 2); }
        float extra = Mathf.Max(0, (-low - H / 2 + 40) / 2);
        if (extra > 0)
        {
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, H + 2 * extra);
            panel.localPosition -= Vector3.up * extra * panel.localScale.y;
            foreach (Transform c in panel) { var r = c as RectTransform; if (r != null && c.name != "Opaque backing") r.anchoredPosition += Vector2.up * extra; }
            var box = panel.GetComponent<BoxCollider>(); if (box != null) box.size = new Vector3(panel.sizeDelta.x, panel.sizeDelta.y, box.size.z);
            var back = panel.Find("Opaque backing"); if (back != null) back.localScale = new Vector3(back.localScale.x, panel.sizeDelta.y, back.localScale.z);
        }
        report.Add("World items panel " + H + " -> " + panel.sizeDelta.y + " tall (lowest switch bottom was " + low + ")");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Review/claude-lounge-fix-report.txt", string.Join("\n", report)); Debug.Log("ASTRA_LOUNGE_FIX " + string.Join(" | ", report));
    }
}
