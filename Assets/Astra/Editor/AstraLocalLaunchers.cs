// Claude, 2026-09-30. Editor-only helper so agents can start the owner's local companion apps
// through the agent queue (menu Astra/Local/...) without closing Unity. Not part of the world build.
using System.Diagnostics;
using System.IO;
using UnityEditor;

public static class AstraLocalLaunchers
{
    [MenuItem("Astra/Local/Launch Claude Presence")]
    public static void LaunchClaudePresence()
    {
        var dir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "claude-discord-presence");
        var cmd = Path.Combine(dir, "Start Claude Presence.cmd");
        if (!File.Exists(cmd)) throw new FileNotFoundException("Claude Presence not found", cmd);
        Process.Start(new ProcessStartInfo { FileName = cmd, WorkingDirectory = dir, UseShellExecute = true });
        File.WriteAllText("Review/agent-queue/_claude-presence-launch.txt", "Launched " + cmd + " at " + System.DateTime.Now.ToString("HH:mm:ss") + "\n");
    }
}
