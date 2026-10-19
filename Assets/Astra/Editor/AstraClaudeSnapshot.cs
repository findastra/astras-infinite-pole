 using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Rollback snapshot: saves scene, dumps structure, commits, tags, pushes a WIP branch.
public static class AstraClaudeSnapshot
{
    static string Git(string args)
    {
        string exe = FindGit();
        if (exe == null) return "$ git " + args + " -> git.exe not found\n";
        var p = new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Directory.GetCurrentDirectory() };
        p.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
        using (var proc = Process.Start(p)) { string o = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd(); proc.WaitForExit(120000); return "$ git " + args + " (exit " + proc.ExitCode + ")\n" + o + "\n"; }
    }

    static string FindGit()
    {
        string la = Environment.GetEnvironmentVariable("LOCALAPPDATA"), up = Environment.GetEnvironmentVariable("USERPROFILE");
        var c = new System.Collections.Generic.List<string> { @"C:\Program Files\Git\cmd\git.exe", @"C:\Program Files (x86)\Git\cmd\git.exe", Path.Combine(la, @"Programs\Git\cmd\git.exe"), Path.Combine(up, @"scoop\shims\git.exe") };
        foreach (var root in new[] { Path.Combine(la, "GitHubDesktop"), Path.Combine(la, "Programs") })
            if (Directory.Exists(root)) try { c.AddRange(Directory.GetFiles(root, "git.exe", SearchOption.AllDirectories)); } catch { }
        foreach (var d in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')) try { c.Add(Path.Combine(d.Trim().Trim('"'), "git.exe")); } catch { }
        return c.FirstOrDefault(File.Exists);
    }

    static void CopyDir(string from, string to)
    {
        foreach (var f in Directory.GetFiles(from, "*", SearchOption.AllDirectories)) { var t = Path.Combine(to, f.Substring(from.Length + 1)); Directory.CreateDirectory(Path.GetDirectoryName(t)); File.Copy(f, t, true); }
    }

    static void Dump(Transform t, StringBuilder sb, int depth)
    {
        var comps = string.Join(",", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name));
        sb.Append(new string(' ', depth * 2)).Append(t.name).Append(t.gameObject.activeSelf ? "" : " [off]")
          .Append(" p=").Append(t.localPosition.ToString("F2")).Append(" r=").Append(t.localEulerAngles.ToString("F0")).Append(" s=").Append(t.localScale.ToString("F2"))
          .Append(" {").Append(comps).Append("}");
        var r = t.GetComponent<Renderer>();
        if (r != null) sb.Append(" mats=").Append(string.Join("|", r.sharedMaterials.Where(m => m != null).Select(m => m.name + "(" + m.shader.name + ")")));
        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) sb.Append(" mesh=").Append(mf.sharedMesh.name).Append(" b=").Append(r != null ? r.bounds.size.ToString("F1") : "");
        var ps = t.GetComponent<ParticleSystem>();
        if (ps != null) sb.Append(" ps[max=").Append(ps.main.maxParticles).Append(" shape=").Append(ps.shape.shapeType).Append(" rate=").Append(ps.emission.rateOverTime.constant).Append("]");
        sb.Append('\n');
        if (depth < 5) foreach (Transform c in t) Dump(c, sb, depth + 1);
        else if (t.childCount > 0) sb.Append(new string(' ', depth * 2 + 2)).Append("... ").Append(t.childCount).Append(" children\n");
    }

    [MenuItem("Astra/Claude/1 Snapshot and push WIP")]
    public static void Run()
    {
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("docs/dev");
        var sb = new StringBuilder();
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects()) Dump(root.transform, sb, 0);
        File.WriteAllText("docs/dev/scene-dump.txt", sb.ToString());
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        var log = new StringBuilder();
        string backup = "Review/Backups/pre-cloud-furniture-" + stamp;
        CopyDir("Assets/Astra", backup + "/Assets/Astra");
        log.Append("Folder backup: " + backup + "\ngit: " + (FindGit() ?? "NOT FOUND") + "\n.git present: " + Directory.Exists(".git") + "\n");
        log.Append(Git("status --short --branch"));
        log.Append(Git("remote -v"));
        log.Append(Git("add -A"));
        log.Append(Git("commit -m \"WIP snapshot before cloud furniture pass (spiral, stair dust, DJ cloud, phone booth)\""));
        log.Append(Git("tag -f pre-cloud-furniture-" + stamp));
        log.Append(Git("push origin HEAD:refs/heads/wip/pre-cloud-furniture refs/tags/pre-cloud-furniture-" + stamp));
        log.Append(Git("log --oneline -3"));
        File.WriteAllText("docs/dev/snapshot-log.txt", log.ToString());
        UnityEngine.Debug.Log("ASTRA_SNAPSHOT\n" + log);
        string s = log.ToString();
        EditorUtility.DisplayDialog("Astra snapshot", s.Length > 3000 ? s.Substring(s.Length - 3000) : s, "OK");
    }
}
