// Claude, 2026-10-04. Menu: Astra > Claude > 35 Menu columns fit the panel
// Any menu column that runs past the bottom of the panel (GLITTER's effect list and ALL ON / ALL OFF ran onto the video
// bar) is squeezed up to fit, keeping its order. Report: Review/claude-round35-report.txt.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ClaudeRound35
{
    [MenuItem("Astra/Claude/35 Menu columns fit the panel")]
    public static void Run()
    {
        var report = new List<string>();
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        var panel = menu != null ? menu.menuRoot.Find("Astra menu") as RectTransform : null;
        var tabs = panel != null ? panel.GetComponentInChildren<AstraMenuTabs>(true) : null;
        if (tabs == null) { File.WriteAllText("Review/claude-round35-report.txt", "no menu"); return; }
        float bottom = -panel.sizeDelta.y / 2f + 45f;
        foreach (var page in tabs.pages.Where(p => p != null).Select(p => (RectTransform)p.transform))
        {
            var kids = page.Cast<Transform>().OfType<RectTransform>().Where(r => r.gameObject.activeSelf).ToList();
            foreach (var col in new[] { kids.Where(r => r.anchoredPosition.x < -100f).ToList(), kids.Where(r => r.anchoredPosition.x > 100f).ToList(), kids.Where(r => Mathf.Abs(r.anchoredPosition.x) <= 100f).ToList() })
            {
                if (col.Count == 0) continue;
                float top = col.Max(r => r.anchoredPosition.y + r.sizeDelta.y / 2f), low = col.Min(r => r.anchoredPosition.y - r.sizeDelta.y / 2f);
                if (low >= bottom) continue;
                float k = (top - bottom) / (top - low);
                foreach (var r in col)
                {
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, top - (top - r.anchoredPosition.y) * k);
                    if (r.GetComponent<UnityEngine.UI.Slider>() == null) r.sizeDelta = new Vector2(r.sizeDelta.x, r.sizeDelta.y * Mathf.Max(.82f, k));
                    EditorUtility.SetDirty(r);
                }
                report.Add(page.name + ": a column of " + col.Count + " items squeezed by " + (k * 100f).ToString("0") + "% to end above the panel edge");
            }
        }
        if (report.Count == 0) report.Add("every column already fits");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        File.WriteAllText("Review/claude-round35-report.txt", string.Join("\n", report));
    }
}
