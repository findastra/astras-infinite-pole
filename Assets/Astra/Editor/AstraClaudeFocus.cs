using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraClaudeFocus
{
    static string V(Vector3 v) { return v.x.ToString("0.##") + "," + v.y.ToString("0.##") + "," + v.z.ToString("0.##"); }

    static void Line(Transform t, StringBuilder sb, int d)
    {
        var r = t.GetComponent<Renderer>();
        sb.Append(new string(' ', d)).Append(t.name).Append(t.gameObject.activeSelf ? "" : "[off]").Append(" p").Append(V(t.localPosition)).Append(" s").Append(V(t.localScale));
        var comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform) && !(c is MeshFilter)).Select(c => c.GetType().Name.Replace("Renderer", "R").Replace("Collider", "C").Replace("ParticleSystem", "PS"));
        sb.Append(" {").Append(string.Join(",", comps)).Append("}");
        if (r != null) sb.Append(" W").Append(V(r.bounds.center)).Append(" B").Append(V(r.bounds.size)).Append(" m=").Append(string.Join("|", r.sharedMaterials.Where(m => m != null).Select(m => m.name)));
        sb.Append('\n');
    }

    static void Walk(Transform t, StringBuilder sb, int d, int maxD)
    {
        Line(t, sb, d);
        if (d >= maxD) { if (t.childCount > 0) sb.Append(new string(' ', d + 1)).Append("(" + t.childCount + " children)\n"); return; }
        int shown = 0; string last = null; int same = 0;
        foreach (Transform c in t)
        {
            string stem = new string(c.name.Where(ch => !char.IsDigit(ch)).ToArray());
            if (stem == last && ++same > 2) continue;
            if (stem != last) { same = 0; last = stem; }
            if (++shown > 14) { sb.Append(new string(' ', d + 1)).Append("...(" + t.childCount + " total)\n"); break; }
            Walk(c, sb, d + 1, maxD);
        }
    }

    static void Fields(Component c, StringBuilder sb)
    {
        var so = new SerializedObject(c); var it = so.GetIterator(); bool enter = true;
        sb.Append("  [").Append(c.GetType().Name).Append(" on ").Append(c.name).Append("]\n");
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.name == "m_Script" || it.name.StartsWith("serialized")) continue;
            string val;
            if (it.isArray && it.propertyType == SerializedPropertyType.Generic)
            {
                var names = new System.Collections.Generic.List<string>();
                for (int i = 0; i < Mathf.Min(it.arraySize, 12); i++) { var e = it.GetArrayElementAtIndex(i); names.Add(e.propertyType == SerializedPropertyType.ObjectReference ? (e.objectReferenceValue ? e.objectReferenceValue.name : "null") : e.propertyType == SerializedPropertyType.String ? e.stringValue : e.propertyType.ToString()); }
                val = "[" + it.arraySize + "] " + string.Join(";", names);
            }
            else if (it.propertyType == SerializedPropertyType.ObjectReference) val = it.objectReferenceValue ? it.objectReferenceValue.name : "null";
            else if (it.propertyType == SerializedPropertyType.Float) val = it.floatValue.ToString("0.###");
            else if (it.propertyType == SerializedPropertyType.Boolean) val = it.boolValue.ToString();
            else if (it.propertyType == SerializedPropertyType.Integer) val = it.intValue.ToString();
            else if (it.propertyType == SerializedPropertyType.String) val = it.stringValue;
            else if (it.propertyType == SerializedPropertyType.Vector3) val = V(it.vector3Value);
            else val = it.propertyType.ToString();
            sb.Append("    ").Append(it.name).Append('=').Append(val).Append('\n');
        }
    }

    [MenuItem("Astra/Claude/2 Focus report")]
    public static void Run()
    {
        var sb = new StringBuilder();
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        string[] deep = { "12 - DJ cloud", "Phone line", "10 - Stair opening star dust", "Cloud keep-clear - video screen", "Cloud keep-clear - DJ cloud", "Astra world item switches", "Astra cloud orbits", "09 - Infinite prismatic spiral" };
        foreach (var root in roots)
        {
            if (deep.Contains(root.name)) Walk(root.transform, sb, 0, 3);
            else Walk(root.transform, sb, 0, 1);
        }
        sb.Append("\n== Udon fields ==\n");
        foreach (var n in new[] { "Astra world item switches", "12 - DJ cloud", "Phone line", "10 - Stair opening star dust", "08 - Pinkscape and gentle hover", "Cloud keep-clear - video screen", "Astra cloud orbits" })
        {
            var go = roots.FirstOrDefault(r => r.name == n); if (go == null) continue;
            foreach (var c in go.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null && c.GetType().Namespace != "VRC.Udon" && c.GetType().Name != "UdonBehaviour").Take(4)) Fields(c, sb);
        }
        var vp = Object.FindObjectsOfType<Renderer>(true).Where(r => r.name.ToLower().Contains("screen")).Take(8);
        sb.Append("\n== screens ==\n"); foreach (var r in vp) { sb.Append(r.transform.parent ? r.transform.parent.name + "/" : ""); Line(r.transform, sb, 0); }
        sb.Append("\n== editor scripts ==\n" + string.Join(", ", Directory.GetFiles("Assets/Astra/Editor", "*.cs").Select(Path.GetFileNameWithoutExtension)) + "\n");
        sb.Append("== scripts ==\n" + string.Join(", ", Directory.GetFiles("Assets/Astra/Scripts", "*.cs").Select(Path.GetFileNameWithoutExtension)) + "\n");
        File.WriteAllText("docs/dev/focus.txt", sb.ToString());
        Debug.Log("ASTRA_FOCUS " + sb.Length);
    }
}
