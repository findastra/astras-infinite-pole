// Claude, 2026-10-03. Menu: Astra > Claude > 26 Trees and butterflies out, fewer skies, menu survey
//  1. Every palm and cherry blossom tree deleted (scene objects + the Trees meshes/materials/model).
//  2. Every butterfly deleted, and the BUTTERFLIES / DJ BUTTERFLIES switches with them (also out of AstraWorldItems).
//  3. Sky list cut to the four Astra kept: Velvet Nebula, Arctic Aurora, Quiet Void, Moonlit Sky (+ the Pink Cloud Sea
//     start sky). Rose Dusk, Midnight Stars and Belfast Sunset and their buttons are gone.
//  4. Writes Review/claude-menu-survey.txt: every menu panel element (type, size, font, bound Udon event) and the cloud
//     lounge furniture layout, so the one-menu rebuild and the connected-cloud furniture pass can be designed from facts.
// The look-up/down spin fix lives in Scripts/AstraCloudOrbit.cs, not here.
// Safe to rerun. Backup: Review/Backups/BeforeRound26.unity.txt. Report: Review/claude-round26-report.txt.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ClaudeRound26
{
    const string Root = "Assets/Astra/";
    static List<string> report;

    [MenuItem("Astra/Claude/26 Trees and butterflies out, fewer skies, menu survey")]
    public static void Run()
    {
        report = new List<string>();
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Review/Backups"); File.Copy(scene.path, "Review/Backups/BeforeRound26.unity.txt", true);
            Step("trees", Trees);
            Step("butterflies", Butterflies);
            Step("skies", Skies);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Step("survey", Survey);
        }
        catch (Exception e) { report.Add("FAILED: " + e); }
        File.WriteAllText("Review/claude-round26-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND26 " + string.Join(" | ", report));
    }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("STEP FAILED (" + name + "): " + e.Message + "\n" + e.StackTrace); } }

    static IEnumerable<Transform> SceneTransforms()
    {
        return Resources.FindObjectsOfTypeAll<Transform>().Where(t => t != null && t.gameObject.scene.IsValid());
    }
    static string Path(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }

    // ---------------- 1. trees
    static void Trees()
    {
        var trees = SceneTransforms().Where(t => t.name == "Cloud tree").ToList();
        foreach (var t in trees) if (t != null) Object.DestroyImmediate(t.gameObject);
        report.Add("trees: " + trees.Count + " cloud trees deleted");
        int assets = 0;
        foreach (var p in new[] { Root + "Meshes/Trees", Root + "Materials/Trees", Root + "Models/trees.json" })
            if (AssetDatabase.LoadAssetAtPath<Object>(p) != null || AssetDatabase.IsValidFolder(p)) { AssetDatabase.DeleteAsset(p); assets++; }
        report.Add("trees: " + assets + " tree asset folders/files deleted");
    }

    // ---------------- 2. butterflies
    static void Butterflies()
    {
        bool IsButterfly(string n) { return n.IndexOf("utterfl", StringComparison.OrdinalIgnoreCase) >= 0; }

        // the menu switches first, so the arrays that reference them can be cleaned before the objects go
        var toggles = Object.FindObjectsOfType<Toggle>(true).Where(t => IsButterfly(t.name) || (t.GetComponentInChildren<Text>(true) != null && IsButterfly(t.GetComponentInChildren<Text>(true).text))).ToList();
        var toggleSet = new HashSet<Toggle>(toggles);
        report.Add("butterflies: menu switches found: " + (toggles.Count == 0 ? "none" : string.Join(", ", toggles.Select(t => t.name))));

        foreach (var wi in Object.FindObjectsOfType<AstraWorldItems>(true))
        {
            var keepObj = new List<GameObject>(); var keepTog = new List<Toggle>(); int dropped = 0;
            for (int i = 0; i < wi.objects.Length; i++)
            {
                var tog = wi.objectToggles != null && i < wi.objectToggles.Length ? wi.objectToggles[i] : null;
                bool drop = (tog != null && toggleSet.Contains(tog)) || (wi.objects[i] != null && IsButterfly(wi.objects[i].name));
                if (drop) { dropped++; continue; }
                keepObj.Add(wi.objects[i]); keepTog.Add(tog);
            }
            if (dropped == 0) continue;
            wi.objects = keepObj.ToArray(); wi.objectToggles = keepTog.ToArray();
            UdonSharpEditorUtility.CopyProxyToUdon(wi);
            report.Add("butterflies: " + dropped + " entries removed from AstraWorldItems (" + wi.objects.Length + " world items left)");
        }
        foreach (var os in Object.FindObjectsOfType<AstraObjectSwitch>(true))
            if (os.toggle != null && toggleSet.Contains(os.toggle)) { report.Add("butterflies: switch holder " + os.name + " deleted"); Object.DestroyImmediate(os.gameObject); }

        foreach (var t in toggles) if (t != null) Object.DestroyImmediate(t.gameObject);

        // then everything in the world itself
        var objs = SceneTransforms().Where(t => IsButterfly(t.name) && t.GetComponentInParent<Canvas>() == null).ToList();
        var roots = objs.Where(t => !objs.Any(o => o != t && t.IsChildOf(o))).ToList();
        report.Add("butterflies: world objects deleted: " + (roots.Count == 0 ? "none" : string.Join(", ", roots.Select(Path))));
        foreach (var t in roots) if (t != null) Object.DestroyImmediate(t.gameObject);

        // the DJ butterfly particle reference on the deck script, if it has one
        foreach (var d in Object.FindObjectsOfType<AstraDeckMotion>(true)) UdonSharpEditorUtility.CopyProxyToUdon(d);
    }

    // ---------------- 3. skies
    static readonly string[] KeepNames = { "VELVET NEBULA", "ARCTIC AURORA", "QUIET VOID", "MOONLIT SKY" };
    static readonly int[] KeepIndex = { 0, 1, 4, 6 };          // old order: Nebula, Aurora, RoseDusk, Midnight, QuietVoid, Sunset, Moonlit
    static void Skies()
    {
        var atm = Object.FindObjectOfType<AstraAtmosphere>(true);
        if (atm == null) { report.Add("skies: AstraAtmosphere not found"); return; }
        report.Add("skies: before = " + atm.skies.Length + " [" + string.Join(", ", atm.skies.Select(m => m == null ? "null" : m.name)) + "]");
        if (atm.skies.Length == 7)
        {
            atm.skies = KeepIndex.Select(i => atm.skies[i]).ToArray();
            atm.skyNames = (string[])KeepNames.Clone();
            UdonSharpEditorUtility.CopyProxyToUdon(atm);
            report.Add("skies: after = " + string.Join(", ", atm.skies.Select((m, i) => KeepNames[i] + " (" + (m == null ? "null" : m.name) + ")")));
        }
        else if (atm.skyNames == null || atm.skyNames.Length != atm.skies.Length)
        {
            atm.skyNames = KeepNames.Take(atm.skies.Length).ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(atm);
            report.Add("skies: already reduced; names re-synced");
        }
        else report.Add("skies: already reduced, nothing to do");

        int gone = 0;
        foreach (var name in new[] { "ROSE DUSK", "MIDNIGHT STARS", "BELFAST SUNSET" })
            foreach (var b in Object.FindObjectsOfType<Button>(true).Where(x => x.name == name || (x.GetComponentInChildren<Text>(true) != null && x.GetComponentInChildren<Text>(true).text == name)).ToList())
                if (b != null) { Object.DestroyImmediate(b.gameObject); gone++; }
        report.Add("skies: " + gone + " sky buttons removed from the menu");
    }

    // ---------------- 4. survey
    static string Bound(Component c)
    {
        var so = new SerializedObject(c);
        foreach (var prop in new[] { "m_OnClick", "m_OnValueChanged" })
        {
            var calls = so.FindProperty(prop + ".m_PersistentCalls.m_Calls");
            if (calls == null || calls.arraySize == 0) continue;
            var parts = new List<string>();
            for (int i = 0; i < calls.arraySize; i++)
            {
                var e = calls.GetArrayElementAtIndex(i);
                var target = e.FindPropertyRelative("m_Target").objectReferenceValue;
                string m = e.FindPropertyRelative("m_MethodName").stringValue;
                string arg = e.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue;
                parts.Add((target == null ? "?" : target.name) + "." + m + (string.IsNullOrEmpty(arg) ? "" : "(\"" + arg + "\")"));
            }
            return string.Join(" + ", parts);
        }
        return "";
    }
    static void Survey()
    {
        var sb = new StringBuilder();
        sb.Append("=== MENU PANELS ===\n");
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        if (menu == null) sb.Append("AstraHandMenu not found\n");
        else
        {
            sb.Append("menu root: " + Path(menu.menuRoot) + "\n");
            foreach (var canvas in menu.menuRoot.GetComponentsInChildren<Canvas>(true))
            {
                var crt = canvas.GetComponent<RectTransform>();
                sb.Append("\n-- canvas \"" + canvas.name + "\"  size " + crt.sizeDelta + "  pos " + crt.anchoredPosition + "  worldScale " + crt.lossyScale.x.ToString("0.0000") + "\n");
                foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt == crt) continue;
                    if (rt.parent != crt) continue;                       // direct rows only; their labels are folded in
                    var txt = rt.GetComponentInChildren<Text>(true);
                    string kind = rt.GetComponent<Toggle>() != null ? "Toggle" : rt.GetComponent<Button>() != null ? "Button"
                                : rt.GetComponent<Slider>() != null ? "Slider" : rt.GetComponent<Text>() != null ? "Text"
                                : rt.GetComponent<Image>() != null ? "Image" : "Group";
                    var comp = (Component)rt.GetComponent<Toggle>() ?? (Component)rt.GetComponent<Button>() ?? (Component)rt.GetComponent<Slider>();
                    sb.Append("   " + kind.PadRight(7) + " name=\"" + rt.name + "\" pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta
                        + (txt != null ? " font=" + txt.fontSize + " text=\"" + txt.text.Replace("\n", "\\n") + "\"" : "")
                        + (comp != null ? "  -> " + Bound(comp) : "") + "\n");
                }
            }
        }

        sb.Append("\n=== CLOUD LOUNGE FURNITURE ===\n");
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        if (lounge == null) sb.Append("cloud lounge not found\n");
        else foreach (Transform group in lounge.transform)
            {
                sb.Append("\n-- " + group.name + " (" + group.childCount + " children)\n");
                foreach (Transform item in group)
                {
                    var rs = item.GetComponentsInChildren<MeshRenderer>(true);
                    if (rs.Length == 0) { sb.Append("   " + item.name + ": no renderers\n"); continue; }
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    var meshes = rs.Select(r => { var mf = r.GetComponent<MeshFilter>(); return mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "?"; }).Distinct().Take(4);
                    var kids = item.Cast<Transform>().Select(c => c.name).Distinct().Take(6);
                    sb.Append("   " + item.name + ": " + rs.Length + " renderers, size " + b.size.ToString("0.00")
                        + ", meshes [" + string.Join(", ", meshes) + "], children [" + string.Join(", ", kids) + "]\n");
                }
            }
        Directory.CreateDirectory("Review");
        File.WriteAllText("Review/claude-menu-survey.txt", sb.ToString());
        report.Add("survey: Review/claude-menu-survey.txt (" + sb.Length + " chars)");
    }
}
