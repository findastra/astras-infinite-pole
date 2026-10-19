// Claude, 2026-10-01. Menu: Astra > Claude > 24 Why did the upload check fail? (report only, changes nothing)
// Writes Review/claude-upload-check.txt: what each spawn-floor raycast in AstraWorldBuilder.Verify hits, the particle cap
// total (Verify allows 12000), and the biggest particle systems.
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ClaudeUploadCheck
{
    [MenuItem("Astra/Claude/24 Why did the upload check fail? (report only)")]
    public static void Run()
    {
        var sb = new StringBuilder(); Physics.SyncTransforms();
        foreach (var p in new[] { new Vector3(0, 1, -2), new Vector3(5, 1, 0), new Vector3(-10, 1, 0) })
        {
            var hits = Physics.RaycastAll(p, Vector3.down, 2f).OrderBy(h => h.distance).ToArray();
            sb.Append("ray down from " + p + ": " + (hits.Length == 0 ? "nothing" : string.Join(" ; ", hits.Select(h => Path(h.collider.transform) + " [" + h.collider.GetType().Name + (h.collider.isTrigger ? ", trigger" : "") + "] at y " + h.point.y.ToString("0.00")))) + "\n");
        }
        var all = Object.FindObjectsOfType<ParticleSystem>();
        int cap = all.Sum(ps => ps.main.maxParticles);
        sb.Append("\nactive particle systems: " + all.Length + ", total max particles: " + cap + " (Verify limit 12000)\n");
        foreach (var ps in all.OrderByDescending(p => p.main.maxParticles).Take(15)) sb.Append("  " + ps.main.maxParticles + "  " + Path(ps.transform) + "\n");
        File.WriteAllText("Review/claude-upload-check.txt", sb.ToString());
    }
    static string Path(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
}
