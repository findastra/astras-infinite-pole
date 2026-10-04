// Claude, 2026-10-03. Menu: Astra > Claude > 29 Tidy the one-panel menu
//  - The four tab buttons were plain white with invisible labels (their colours were only set at run time). They now
//    look right in the editor too: the open tab is bright pink, the others dark.
//  - The panel shrinks to fit its tallest tab, so there is no dead space under the shorter ones.
//  - Gradient swatches move onto their own slider row instead of floating between the columns.
//  - Every label is centred, fully opaque, and no two rows overlap on the MAGIC tab.
// Safe to rerun. Backup: Review/Backups/BeforeRound29.unity.txt. Report: Review/claude-round29-report.txt.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ClaudeRound29
{
    const float HeaderBand = 215f, BottomMargin = 70f;
    static List<string> report;

    [MenuItem("Astra/Claude/29 Tidy the one-panel menu")]
    public static void Run()
    {
        report = new List<string>();
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Review/Backups"); File.Copy(scene.path, "Review/Backups/BeforeRound29.unity.txt", true);
            Tidy();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        catch (Exception e) { report.Add("FAILED: " + e); }
        File.WriteAllText("Review/claude-round29-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND29 " + string.Join(" | ", report));
    }

    static void Tidy()
    {
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        var panel = menu == null ? null : menu.menuRoot.Find("Astra menu") as RectTransform;
        if (panel == null) { report.Add("no \"Astra menu\" panel found"); return; }
        var tabs = panel.GetComponentInChildren<AstraMenuTabs>(true);
        if (tabs == null) { report.Add("no AstraMenuTabs on the panel"); return; }
        var pages = tabs.pages.Select(p => (RectTransform)p.transform).ToArray();

        // ---- 1. tab colours, baked in so the panel looks right before anything runs
        for (int i = 0; i < tabs.tabBacks.Length; i++)
        {
            if (tabs.tabBacks[i] != null) { tabs.tabBacks[i].color = i == 0 ? tabs.onBack : tabs.offBack; EditorUtility.SetDirty(tabs.tabBacks[i]); }
            if (tabs.tabLabels[i] != null) { tabs.tabLabels[i].color = i == 0 ? tabs.onText : tabs.offText; EditorUtility.SetDirty(tabs.tabLabels[i]); }
        }
        report.Add("tabs: " + tabs.tabBacks.Length + " tab buttons coloured (open tab pink, the rest dark)");

        // ---- 2. swatches onto their slider rows, and MAGIC's two stacked rows pulled apart
        var glit = pages.FirstOrDefault(p => p.name.EndsWith("GLITTER"));
        if (glit != null)
            foreach (var pair in new[] { new { n = "Color A swatch", y = 1 }, new { n = "Color B swatch", y = 2 } })
            {
                var sw = glit.Find(pair.n) as RectTransform;
                if (sw == null) { report.Add("swatch missing: " + pair.n); continue; }
                sw.anchoredPosition = new Vector2(-55f, sw.anchoredPosition.y); sw.sizeDelta = new Vector2(30, 24);
                EditorUtility.SetDirty(sw);
            }
        var magic = pages.FirstOrDefault(p => p.name.EndsWith("MAGIC"));
        if (magic != null)
        {
            var sp = magic.Find("SPELL PATTERN") as RectTransform;
            var ns = magic.Cast<Transform>().FirstOrDefault(t => t.name == "NEW SPELL") as RectTransform;
            if (sp != null && ns != null && Mathf.Abs(sp.anchoredPosition.y - ns.anchoredPosition.y) < 70f)
            {
                ns.anchoredPosition = new Vector2(ns.anchoredPosition.x, sp.anchoredPosition.y - 76f);
                EditorUtility.SetDirty(ns); report.Add("magic: NEW SPELL moved clear of the SPELL PATTERN label");
            }
        }

        // ---- 3. every label readable: centred and fully opaque
        int fixedText = 0;
        foreach (var page in pages)
            foreach (var t in page.GetComponentsInChildren<Text>(true))
            {
                bool changed = false;
                if (t.color.a < .99f) { t.color = new Color(t.color.r, t.color.g, t.color.b, 1f); changed = true; }
                if (t.color.r + t.color.g + t.color.b < .5f) { t.color = new Color(1f, .86f, .96f); changed = true; }   // near-black labels on a near-black panel
                if (t.alignment != TextAnchor.MiddleCenter) { t.alignment = TextAnchor.MiddleCenter; changed = true; }
                if (changed) { EditorUtility.SetDirty(t); fixedText++; }
            }
        report.Add("labels: " + fixedText + " texts centred / made fully opaque");

        // ---- 4. shrink the panel to the tallest tab, keeping the same gap under the tabs
        float oldH = panel.sizeDelta.y, oldTop = oldH / 2f - HeaderBand;
        float lowest = float.MaxValue;
        foreach (var page in pages)
            foreach (RectTransform c in page)
                lowest = Mathf.Min(lowest, c.anchoredPosition.y - c.sizeDelta.y / 2f);
        if (lowest == float.MaxValue) { report.Add("panel: no page content found; size left alone"); return; }
        float contentH = oldTop - lowest;
        float newH = Mathf.Round(HeaderBand + contentH + BottomMargin);
        float newTop = newH / 2f - HeaderBand, shift = newTop - oldTop;

        foreach (var page in pages) { page.sizeDelta = new Vector2(panel.sizeDelta.x, newH); foreach (RectTransform c in page) { c.anchoredPosition += new Vector2(0, shift); EditorUtility.SetDirty(c); } }
        foreach (RectTransform c in panel)
        {
            if (pages.Contains(c)) continue;
            if (c.name == "Opaque backing") { c.sizeDelta = new Vector2(panel.sizeDelta.x, newH); EditorUtility.SetDirty(c); continue; }
            c.anchoredPosition += new Vector2(0, (newH - oldH) / 2f);     // title, tabs and CLOSE ride with the top edge
            EditorUtility.SetDirty(c);
        }
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, newH);
        var box = panel.GetComponent<BoxCollider>();
        if (box != null) { box.size = new Vector3(panel.sizeDelta.x, newH, 1); EditorUtility.SetDirty(box); }
        UdonSharpEditorUtility.CopyProxyToUdon(tabs);
        report.Add("panel: height " + oldH + " -> " + newH + " (" + (newH * panel.localScale.y).ToString("0.00") + " m tall); tallest tab needs "
            + contentH.ToString("0") + ", content shifted " + shift.ToString("0"));
    }
}
