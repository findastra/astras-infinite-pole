// Git setup + release push helpers (Claude, 2026-09-26). Menu: Astra > Claude > 6 Install Git / 7 Commit, tag and push.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AstraClaudeGit
{
    const string Log = "Review/git-log.txt";
    static void Append(string s) { Directory.CreateDirectory("Review"); File.AppendAllText(Log, DateTime.Now.ToString("HH:mm:ss ") + s + "\n"); }

    [MenuItem("Astra/Claude/6 Install Git (winget, official package)")]
    public static void Install()
    {
        if (FindGit() != null) { Append("git already installed: " + FindGit()); EditorUtility.DisplayDialog("Git", "Git is already installed:\n" + FindGit(), "OK"); return; }
        var psi = new ProcessStartInfo("winget", "install --id Git.Git -e --source winget --silent --accept-package-agreements --accept-source-agreements")
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        Append("starting: winget " + psi.Arguments);
        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        p.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Append(e.Data); };
        p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Append("ERR " + e.Data); };
        p.Exited += (s, e) => Append("winget finished, exit " + p.ExitCode + ", git now: " + (FindGit() ?? "not found"));
        p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
        EditorUtility.DisplayDialog("Installing Git", "Git for Windows is installing in the background (official winget package).\nIf Windows asks for permission, click Yes.\nProgress: Review/git-log.txt", "OK");
    }

    [MenuItem("Astra/Claude/7 Commit, tag and push")]
    public static void Push()
    {
        var git = FindGit(); if (git == null) { EditorUtility.DisplayDialog("Git", "Git isn't installed yet.", "OK"); return; }
        EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        var sb = new StringBuilder();
        Func<string, string> run = a => { var r = Run(git, a); sb.Append("$ git " + a + "\n" + r + "\n"); Append("git " + a + " -> " + r.Replace("\n", " | ")); return r; };
        if (!Directory.Exists(".git"))
        {
            run("init -b main");
            run("remote add origin https://github.com/findastra/astras-infinite-pole.git");
            run("fetch origin main");
            run("reset --soft origin/main"); // keep every local file; history continues from GitHub main
        }
        if (File.Exists(".git/index.lock") && (DateTime.Now - File.GetLastWriteTime(".git/index.lock")).TotalMinutes > 2) { File.Delete(".git/index.lock"); Append("removed stale .git/index.lock left by the earlier stuck run"); }
        Directory.CreateDirectory(".git/info");
        var ex = File.Exists(".git/info/exclude") ? File.ReadAllText(".git/info/exclude") : "";
        foreach (var line in new[] { "/Review/", "/ClientSimStorage/", "/docs/dev/" }) if (!ex.Contains(line)) File.AppendAllText(".git/info/exclude", "\n" + line);
        run("status --short --branch");
        var st = Run(git, "status --porcelain"); if (!st.StartsWith("(exit 0)")) { EditorUtility.DisplayDialog("Git push stopped", st, "OK"); return; }
        var changed = st.Split('\n').Count(l => l.Trim().Length > 3);
        if (changed > 600) { EditorUtility.DisplayDialog("Git push stopped", changed + " changed paths is more than expected, so nothing was committed. See Review/git-log.txt.", "OK"); return; }
        run("config user.name \"Astra\"");
        run("config user.email \"findastra@users.noreply.github.com\"");
        run("add -A");
        run("commit -m \"Pink cloud sea: banner backdrop and butterflies, cloud lounge polish, heart and star clouds, swings, breakfast cloud\"");
        run("tag -a v0.8.0-cloud-sea -m \"Pink cloud sea (includes the v0.7.0 cloud lounge work)\"");
        run("push -u origin HEAD:refs/heads/release/v0.8.0-cloud-sea");
        run("push origin v0.8.0-cloud-sea");
        run("log --oneline -3");
        var s = sb.ToString(); EditorUtility.DisplayDialog("Git push", s.Length > 2500 ? s.Substring(s.Length - 2500) : s, "OK");
    }

    static string Run(string exe, string args)
    {
        if (exe.EndsWith("git.exe")) args = "-c core.safecrlf=false -c \"safe.directory=" + Directory.GetCurrentDirectory().Replace('\\', '/') + "\" " + args;
        var psi = new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Directory.GetCurrentDirectory() };
        psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
        using (var p = new Process { StartInfo = psi })
        {
            var outB = new StringBuilder(); var errB = new StringBuilder();
            p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (outB) outB.AppendLine(e.Data); };
            p.ErrorDataReceived += (s, e) => { if (e.Data != null && !e.Data.Contains("CRLF will be replaced") && !e.Data.Contains("LF will be replaced")) lock (errB) errB.AppendLine(e.Data); };
            p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
            if (!p.WaitForExit(600000)) { try { p.Kill(); } catch { } return "(timeout) " + outB + errB; }
            p.WaitForExit(); return "(exit " + p.ExitCode + ") " + (outB.ToString() + errB.ToString()).Trim();
        }
    }

    public static string FindGit()
    {
        string la = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", pf = Environment.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files";
        var c = new[] { Path.Combine(pf, @"Git\cmd\git.exe"), @"C:\Program Files (x86)\Git\cmd\git.exe", Path.Combine(la, @"Programs\Git\cmd\git.exe") }.ToList();
        foreach (var d in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')) try { c.Add(Path.Combine(d.Trim().Trim('"'), "git.exe")); } catch { }
        return c.FirstOrDefault(File.Exists);
    }
}
