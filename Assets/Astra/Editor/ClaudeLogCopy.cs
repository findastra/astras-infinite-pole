// Claude, 2026-10-04. Menu: Astra > Claude > 36 Copy the Unity log (report only) -> Review/editor-log-tail.txt
using System.IO; using System.Linq; using UnityEditor; using UnityEngine;
public static class ClaudeLogCopy
{
    [MenuItem("Astra/Claude/36 Copy the Unity log (report only)")]
    public static void Run()
    {
        string src = Application.consoleLogPath; string text;
        using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) using (var sr = new StreamReader(fs)) text = sr.ReadToEnd();
        var lines = text.Split('\n'); File.WriteAllText("Review/editor-log-tail.txt", string.Join("\n", lines.Skip(System.Math.Max(0, lines.Length - 4000))));
    }
}
