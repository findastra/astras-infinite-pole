// Claude (2026-09-24): checks every button, switch and slider in the world.
// Part 1 (edit mode): wiring. Is each control hooked to a real Udon event? Is it clickable (raycast target, VRChat UI shape,
//   not on the UI layer, inside its panel's collider, not hidden behind another panel)?
// Part 2 (Play mode with ClientSim): presses every control for real and records any Udon error it causes.
// Menu: Astra > Claude - Check All Buttons. Report: Review/claude-button-check.txt. Never uploads anything.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ClaudeButtonCheck
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Flag = "Claude.ButtonCheck";
    const string ReportPath = "Review/claude-button-check.txt";
    static double ready;

    static ClaudeButtonCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Flag, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) { ready = EditorApplication.timeSinceStartup + 8; EditorApplication.update += Tick; }
            if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Flag, false);
        };
    }

    [MenuItem("Astra/Claude - Check All Buttons")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(Scene);
        var lines = new List<string> { "BUTTON CHECK " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "", "== Part 1: wiring and clickability ==" };
        int problems = 0;
        var hand = UnityEngine.Object.FindObjectOfType<AstraHandMenu>();
        bool wasActive = hand != null && hand.menuRoot.gameObject.activeSelf;
        if (hand != null) hand.menuRoot.gameObject.SetActive(true);
        Physics.SyncTransforms();

        foreach (var s in Controls(scene))
        {
            var issues = new List<string>();
            string name = Label(s);
            // wiring
            var calls = Calls(s);
            if (calls.Count == 0 && !UsedByScript(s)) issues.Add("does nothing (no event hooked up)");
            foreach (var (target, method, arg) in calls)
            {
                if (target == null) { issues.Add("hooked to a missing object"); continue; }
                if (target is VRC.Udon.UdonBehaviour ub && method == "SendCustomEvent")
                {
                    var proxy = UdonSharpEditorUtility.GetProxyBehaviour(ub);
                    if (proxy != null)
                    {
                        var m = proxy.GetType().GetMethod(arg, BindingFlags.Public | BindingFlags.Instance);
                        if (m == null || m.GetParameters().Length > 0) issues.Add("calls \"" + arg + "\", which " + proxy.GetType().Name + " doesn't have");
                    }
                    if (!ub.enabled) issues.Add("its script is disabled");
                }
            }
            // clickability (only for controls that are showing)
            if (s.gameObject.activeInHierarchy)
            {
                if (!s.interactable) issues.Add("set to not clickable");
                if (s.targetGraphic == null || !s.targetGraphic.raycastTarget) issues.Add("has no clickable graphic");
                var canvas = s.GetComponentInParent<Canvas>();
                if (canvas == null) issues.Add("not on a canvas");
                else
                {
                    var root = canvas.rootCanvas;
                    if (root.GetComponent("VRCUiShape") == null) issues.Add("panel lacks VRC UI Shape, so VRChat lasers can't click it");
                    if (root.GetComponent<GraphicRaycaster>() == null) issues.Add("panel lacks a Graphic Raycaster");
                    if (root.gameObject.layer == 5) issues.Add("panel is on the UI layer (VRChat only allows clicks there while its own menu is open)");
                    var box = root.GetComponent<BoxCollider>();
                    var rt = (RectTransform)s.transform; var center = rt.TransformPoint(rt.rect.center);
                    if (box == null) issues.Add("panel has no collider for the laser");
                    else
                    {
                        var lp = box.transform.InverseTransformPoint(center) - box.center;
                        if (Mathf.Abs(lp.x) > box.size.x / 2 || Mathf.Abs(lp.y) > box.size.y / 2) issues.Add("sits outside its panel's clickable area");
                        var fwd = root.transform.forward;
                        var hits = Physics.RaycastAll(center - fwd * .5f, fwd, .52f, ~0, QueryTriggerInteraction.Collide).OrderBy(h => h.distance).ToArray();
                        if (hits.Length > 0 && hits[0].collider != box) issues.Add("blocked by \"" + hits[0].collider.name + "\" in front of it (control at " + center.ToString("0.000") + ", hit at " + hits[0].point.ToString("0.000") + ", blocker bounds " + hits[0].collider.bounds.min.ToString("0.000") + " to " + hits[0].collider.bounds.max.ToString("0.000") + ", blocker path " + Path(hits[0].collider.transform) + ")");
                    }
                }
            }
            if (issues.Count > 0) { problems++; lines.Add("PROBLEM  " + name + ": " + string.Join("; ", issues)); }
        }
        if (hand != null) hand.menuRoot.gameObject.SetActive(wasActive);
        lines.Add(problems == 0 ? "No wiring or clickability problems." : problems + " control(s) with problems.");
        File.WriteAllText(ReportPath, string.Join("\n", lines));
        Debug.Log("CLAUDE_BUTTONCHECK part 1 done: " + problems + " problem(s). Entering Play mode for part 2.");
        SessionState.SetBool(Flag, true);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < ready) return;
        EditorApplication.update -= Tick;
        var lines = new List<string> { "", "== Part 2: pressing every control in Play mode (ClientSim) ==" };
        var errors = new List<string>();
        Application.LogCallback grab = (msg, st, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(msg.Split('\n')[0]); };
        Application.logMessageReceived += grab;
        int pressed = 0, failed = 0;
        try
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var hand = UnityEngine.Object.FindObjectOfType<AstraHandMenu>();
            if (hand != null) UdonSharpEditorUtility.GetBackingUdonBehaviour(hand).SendCustomEvent("ToggleMenu");
            if (hand != null && !hand.menuRoot.gameObject.activeSelf) lines.Add("PROBLEM  Menu did not open with ToggleMenu (desktop: M key)");
            var all = Controls(scene).Where(c => c.gameObject.activeInHierarchy).ToList();
            var closers = all.Where(c => Label(c).ToUpper().Contains("CLOSE")).ToList();
            foreach (var s in all.Except(closers).Concat(closers))
            {
                if (!s.gameObject.activeInHierarchy) continue; // hidden by an earlier press (e.g. a close button)
                errors.Clear();
                try
                {
                    if (s is Button b) b.onClick.Invoke();
                    else if (s is Toggle t) { t.isOn = !t.isOn; t.isOn = !t.isOn; }
                    else if (s is Slider sl) { float v = sl.value; sl.value = Mathf.Lerp(sl.minValue, sl.maxValue, v > .5f ? .2f : .8f); sl.value = v; }
                    pressed++;
                }
                catch (Exception e) { errors.Add(e.GetType().Name + ": " + e.Message); }
                if (errors.Count > 0) { failed++; lines.Add("ERROR  " + Label(s) + ": " + string.Join(" | ", errors.Distinct().Take(3))); }
            }
            lines.Add("Pressed " + pressed + " controls; " + failed + " caused errors.");
        }
        catch (Exception e) { lines.Add("CHECK CRASHED: " + e); }
        Application.logMessageReceived -= grab;
        File.AppendAllText(ReportPath, "\n" + string.Join("\n", lines));
        Debug.Log("CLAUDE_BUTTONCHECK_DONE pressed " + pressed + ", errors " + failed + ". See " + ReportPath);
        EditorApplication.isPlaying = false;
    }

    static IEnumerable<Selectable> Controls(UnityEngine.SceneManagement.Scene scene) =>
        Resources.FindObjectsOfTypeAll<Selectable>().Where(s => s.gameObject.scene == scene && (s is Button || s is Toggle || s is Slider))
            .OrderBy(s => Path(s.transform));

    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    static string Label(Selectable s)
    {
        var txt = s.GetComponentInChildren<Text>(true);
        var panel = s.GetComponentInParent<Canvas>(true);
        return (panel != null ? "[" + panel.rootCanvas.name + "] " : "") + (txt != null && txt.text.Length > 0 ? txt.text.Replace("\n", " ") : s.name);
    }

    // controls that scripts read directly (e.g. glitter effect switches read by ApplyDensity) count as used
    static bool UsedByScript(Selectable s)
    {
        foreach (var mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (!(mb is UdonSharp.UdonSharpBehaviour)) continue;
            foreach (var f in mb.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var v = f.GetValue(mb);
                if (ReferenceEquals(v, s)) return true;
                if (v is Array arr) foreach (var o in arr) if (ReferenceEquals(o, s)) return true;
            }
        }
        return false;
    }

    static List<(UnityEngine.Object, string, string)> Calls(Selectable s)
    {
        var list = new List<(UnityEngine.Object, string, string)>();
        var so = new SerializedObject(s);
        var ev = so.FindProperty("m_OnClick") ?? so.FindProperty("onValueChanged") ?? so.FindProperty("m_OnValueChanged");
        if (ev == null) return list;
        var arr = ev.FindPropertyRelative("m_PersistentCalls.m_Calls");
        for (int i = 0; i < arr.arraySize; i++)
        {
            var c = arr.GetArrayElementAtIndex(i);
            list.Add((c.FindPropertyRelative("m_Target").objectReferenceValue, c.FindPropertyRelative("m_MethodName").stringValue,
                      c.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue));
        }
        return list;
    }
}
