// Claude, 2026-10-06 (cloud redo). Read-only survey of every cloud renderer and every solid collider that
// belongs to a cloud, so the redo knows exactly what it is changing. Menu Astra/Claude/36a Cloud survey.
// Writes Review/cloud-redo/inspect.txt. Changes nothing.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ClaudeCloudInspect
{
    [MenuItem("Astra/Claude/36a Cloud survey (read-only)")]
    public static void Run() { Directory.CreateDirectory("Review/cloud-redo"); File.WriteAllText("Review/cloud-redo/inspect.txt", Survey()); }

    [MenuItem("Astra/Claude/36b Cloud hierarchy samples (read-only)")]
    public static void Tree()
    {
        var sb = new StringBuilder();
        string[] names = { "Outer cloud 3", "Crystal vine cloud 3", "Cloud 57", "Cloud 30", "Cloud 19", "Cloud firepit", "Cloud swing", "Cloud bed x0.7", "DJ stage cloud", "DJ step cloud", "Duck cloud", "Private box 1", "Screen top cloud", "Banner booth" };
        var all = Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene.IsValid()).ToList();
        foreach (var n in names) { var t = all.FirstOrDefault(x => x.name == n); if (t == null) { sb.AppendLine("MISSING " + n); continue; } sb.AppendLine("== " + Path(t)); Dump(t, 0, sb); }
        File.WriteAllText("Review/cloud-redo/tree.txt", sb.ToString());
    }
    static void Dump(Transform t, int d, StringBuilder sb)
    {
        if (d > 4) return;
        var mf = t.GetComponent<MeshFilter>(); var mr = t.GetComponent<MeshRenderer>();
        string comps = string.Join(",", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
        sb.AppendLine(new string(' ', d * 2) + t.name + (t.gameObject.activeSelf ? "" : " [off]") + " lp " + V(t.localPosition) + " ls " + V(t.localScale) + " ly " + t.localEulerAngles.y.ToString("0") + " {" + comps + "}"
            + (mf && mf.sharedMesh ? " mesh " + mf.sharedMesh.name + " b " + V(mf.sharedMesh.bounds.min) + ".." + V(mf.sharedMesh.bounds.max) : "") + (mr ? (mr.enabled ? "" : " [rend off]") + " mat " + (mr.sharedMaterial ? mr.sharedMaterial.name : "null") : ""));
        int k = 0; foreach (Transform c in t) { if (k++ > 12) { sb.AppendLine(new string(' ', d * 2 + 2) + "... " + (t.childCount - 13) + " more"); break; } Dump(c, d + 1, sb); }
    }

    public static string Path(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    static string V(Vector3 v) { return v.x.ToString("0.00") + "," + v.y.ToString("0.00") + "," + v.z.ToString("0.00"); }

    public static bool IsCloudRenderer(Renderer r)
    {
        if (r == null) return false;
        foreach (var m in r.sharedMaterials) if (m != null && m.shader != null && m.shader.name == "Astra/Glitter Cloud") return true;
        return false;
    }

    public static string Survey()
    {
        var sb = new StringBuilder();
        var rends = Resources.FindObjectsOfTypeAll<MeshRenderer>().Where(r => r.gameObject.scene.IsValid() && IsCloudRenderer(r)).ToList();
        sb.AppendLine("CLOUD RENDERERS (shader Astra/Glitter Cloud): " + rends.Count);
        foreach (var g in rends.GroupBy(r => { var mf = r.GetComponent<MeshFilter>(); return mf && mf.sharedMesh ? mf.sharedMesh.name + " | " + AssetDatabase.GetAssetPath(mf.sharedMesh) : "(none)"; }).OrderByDescending(g => g.Count()))
        {
            var mf = g.First().GetComponent<MeshFilter>(); int tris = mf && mf.sharedMesh ? mf.sharedMesh.triangles.Length / 3 : 0;
            sb.AppendLine("  " + g.Count() + " x " + g.Key + " tris " + tris + "  mats " + string.Join("/", g.First().sharedMaterials.Select(m => m ? m.name : "null")) + "  e.g. " + Path(g.First().transform));
        }
        sb.AppendLine("by root:");
        foreach (var g in rends.GroupBy(r => r.transform.root.name)) sb.AppendLine("  " + g.Count() + " " + g.Key);

        sb.AppendLine();
        sb.AppendLine("SOLID COLLIDERS ON / UNDER CLOUD OBJECTS:");
        var cols = Resources.FindObjectsOfTypeAll<Collider>().Where(c => c.gameObject.scene.IsValid() && !c.isTrigger).ToList();
        int n = 0;
        foreach (var c in cols)
        {
            var t = c.transform; bool cloudy = false;
            for (var p = t; p != null; p = p.parent) if (p.name.ToLowerInvariant().Contains("cloud") || p.name.StartsWith("Jump platform")) { cloudy = true; break; }
            var rr = c.GetComponent<Renderer>(); if (IsCloudRenderer(rr)) cloudy = true;
            if (!cloudy) continue; n++;
            string extra = "";
            if (c is BoxCollider b) extra = " box size " + V(b.size) + " center " + V(b.center);
            else if (c is MeshCollider mc) extra = " mesh " + (mc.sharedMesh ? mc.sharedMesh.name + " tris " + mc.sharedMesh.triangles.Length / 3 : "null") + " convex " + mc.convex;
            else if (c is SphereCollider s) extra = " sphere r " + s.radius;
            else if (c is CapsuleCollider cc) extra = " capsule r " + cc.radius + " h " + cc.height;
            var vb = rr != null && rr.enabled ? " | rend " + V(rr.bounds.min) + " .. " + V(rr.bounds.max) : "";
            sb.AppendLine(c.GetType().Name + " " + (c.gameObject.activeInHierarchy ? "" : "[inactive] ") + (c.enabled ? "" : "[disabled] ") + Path(t) + " layer " + c.gameObject.layer
                + " scale " + V(t.lossyScale) + " rotEuler " + V(t.eulerAngles) + extra + " | world " + V(c.bounds.min) + " .. " + V(c.bounds.max) + vb);
        }
        sb.AppendLine("total " + n);
        sb.AppendLine();
        sb.AppendLine("ROOTS:");
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) sb.AppendLine("  " + go.name + " (" + go.transform.childCount + ")");
        return sb.ToString();
    }
}
