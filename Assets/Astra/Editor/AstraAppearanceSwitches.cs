// Appearance switches helper (Claude, board task C2). Editor-only.
// Adds (or re-binds, on reruns) one individually labelled personal on/off switch on a menu panel, in the same style
// as the World items switches, wired to an Udon event the caller chooses. It never duplicates a switch, never
// changes other switches, and never touches AstraWorldItems or any existing builder.
//
// Usage from an Astra builder:
//   var t = AstraAppearanceSwitches.Ensure(panel, "DECK LIGHTS", deckMotionUdon, "ApplyToggle", true);
//   // then point your script at t (e.g. mist.mistToggle = t) and CopyProxyToUdon as usual.
// Placement: a new switch goes where the panel footer note ("Glitter, petals...") is, and the footer moves down one row,
// the same way rounds 3a-3h add World items switches. Without that footer it goes one row below the lowest switch.
// If the panel runs out of room, the result's Fits = false and the report line says so; nothing is resized.
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public static class AstraAppearanceSwitches
{
    public const float RowHeight = 62f;
    public const string FooterPrefix = "Glitter, petals";

    public struct Result { public Toggle Toggle; public bool Created; public bool Fits; public string Report; }

    /// <summary>Find the switch called <paramref name="label"/> on <paramref name="panel"/> or create it, then bind its
    /// On Value Changed to <paramref name="target"/>.SendCustomEvent(<paramref name="eventName"/>) exactly once.
    /// <paramref name="defaultOn"/> applies only when the switch is first created (reruns keep the saved state).</summary>
    public static Result Ensure(Transform panel, string label, VRC.Udon.UdonBehaviour target, string eventName, bool defaultOn = true)
    {
        var res = new Result();
        if (panel == null || target == null || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(eventName))
        { res.Report = "AstraAppearanceSwitches: missing panel, target, label or event; nothing changed"; return res; }

        var existing = panel.Find(label);
        Toggle t = existing != null ? existing.GetComponent<Toggle>() : null;
        if (t == null)
        {
            float y = NextRow(panel);
            t = Build(panel, label, y, defaultOn);
            res.Created = true;
        }
        // exactly one binding: remove old persistent calls on this switch only, then add ours
        while (t.onValueChanged.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(t.onValueChanged, 0);
        UnityEventTools.AddStringPersistentListener(t.onValueChanged, target.SendCustomEvent, eventName);
        EditorUtility.SetDirty(t);

        var prt = panel.GetComponent<RectTransform>();
        float bottom = LowestEdge(panel), limit = -(prt != null ? prt.sizeDelta.y : 1200f) / 2f;
        res.Fits = bottom > limit;
        res.Toggle = t;
        res.Report = (res.Created ? "Added" : "Re-bound") + " switch \"" + label + "\" -> " + target.name + "." + eventName + (res.Fits ? "" : "  (CHECK: panel is full; lowest item at " + bottom + ", edge " + limit + ")");
        return res;
    }

    // next free row: take the footer's slot and push the footer down; otherwise one row under the lowest switch
    static float NextRow(Transform panel)
    {
        var footer = panel.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.text.StartsWith(FooterPrefix));
        if (footer != null)
        {
            var fr = footer.GetComponent<RectTransform>();
            float y = fr.anchoredPosition.y + 10f;
            fr.anchoredPosition += new Vector2(0, -RowHeight);
            EditorUtility.SetDirty(fr);
            return y;
        }
        var toggles = panel.GetComponentsInChildren<Toggle>(true).Where(x => x.transform.parent == panel).ToArray();
        return toggles.Length == 0 ? 0f : toggles.Min(x => ((RectTransform)x.transform).anchoredPosition.y) - RowHeight;
    }

    static float LowestEdge(Transform panel)
    {
        float low = float.MaxValue;
        foreach (Transform c in panel) { var r = c as RectTransform; if (r == null) continue; low = Mathf.Min(low, r.anchoredPosition.y - r.sizeDelta.y / 2f); }
        return low == float.MaxValue ? 0f : low;
    }

    // same look as the World items switches (rounds 1b / 3a-3h)
    static Toggle Build(Transform p, string text, float y, bool value)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var r = Rect(text, p, new Vector2(0, y), new Vector2(520, 50));
        var im = r.gameObject.AddComponent<Image>(); im.color = new Color(.2f, .07f, .18f);
        var t = r.gameObject.AddComponent<Toggle>(); t.targetGraphic = im;
        var mark = Rect("Crystal switch", r, new Vector2(-235, 0), new Vector2(22, 22)); mark.localRotation = Quaternion.Euler(0, 0, 45);
        var check = mark.gameObject.AddComponent<Image>(); check.color = new Color(1f, .5f, .82f); t.graphic = check; t.isOn = value;
        var lr = Rect(text, r, new Vector2(20, 0), new Vector2(460, 38));
        var lab = lr.gameObject.AddComponent<Text>(); lab.font = font; lab.text = text; lab.fontSize = 18; lab.color = new Color(1f, .86f, .96f); lab.alignment = TextAnchor.MiddleCenter; lab.raycastTarget = false;
        return t;
    }
    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.anchoredPosition = pos; r.sizeDelta = size; return r; }
}
