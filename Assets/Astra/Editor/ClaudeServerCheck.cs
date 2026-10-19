// Claude, 2026-10-03. Menu: Astra > Claude > 25 What version is live on VRChat? (report only)
// Asks the VRChat API about the live world and writes Review/server-status.txt (version, release, packages, updated time).
using UnityEditor;
public static class ClaudeServerCheck
{
    [MenuItem("Astra/Claude/25 What version is live on VRChat? (report only)")]
    public static void Run() { AstraReleaseCheck.Inspect(); }
}
