// Claude, 2026-10-04. Menu: Astra > Claude > 33 Glow orb report (report only). Review/claude-orb-report.txt
using System.IO; using System.Linq; using System.Text; using UnityEditor; using UnityEngine; using VRC.SDK3.Components;
public static class ClaudeOrbReport
{
    [MenuItem("Astra/Claude/33 Glow orb report (report only)")]
    public static void Run()
    {
        var sb = new StringBuilder(); Physics.SyncTransforms();
        foreach (var pk in Object.FindObjectsOfType<VRCPickup>(true))
        {
            var t = pk.transform; string path = t.name; var p = t.parent; while (p != null) { path = p.name + "/" + path; p = p.parent; }
            var col = pk.GetComponent<Collider>(); var c = col != null ? col.bounds.center : t.position;
            RaycastHit h; string below = "nothing";
            var hits = Physics.RaycastAll(c, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore).Where(x => x.collider != col).OrderBy(x => x.distance).ToArray();
            if (hits.Length > 0) { var x = hits[0]; string hp = x.collider.name; var q = x.collider.transform.parent; int k = 0; while (q != null && k++ < 3) { hp = q.name + "/" + hp; q = q.parent; } below = hp + " " + x.distance.ToString("0.00") + " m below"; }
            bool orbit = t.GetComponentInParent<AstraCloudOrbit>() != null;
            sb.Append(path + "\n   at " + c.ToString("0.0") + (orbit ? "  [rides an orbiting cloud]" : "") + "\n   below: " + below + "\n");
        }
        File.WriteAllText("Review/claude-orb-report.txt", sb.ToString());
    }
}
