// Claude, 2026-09-24. Lets Claude and Astra run jobs inside the Unity editor that's already open,
// instead of closing Unity and relaunching it with -executeMethod / -batchmode.
// Every relaunch makes the VRChat SDK restore its sign-in from scratch, which is why the owner
// kept being asked to sign in. With this, she signs in once per time SHE opens Unity.
//
// How agents use it: write a text file into Review/agent-queue/ named <anything>.cmd whose first line is one of:
//   status                      -> writes signed-in account, scene, compile/play state
//   refresh                     -> AssetDatabase.Refresh (compiles new scripts)
//   save                        -> saves open scenes
//   menu Astra/<menu path>      -> runs one of our own Astra/ menu items (nothing else is allowed)
//   desktop-test                -> saves, then SDK Build & Test in desktop mode (no upload)
//   build                       -> saves, then builds the Windows bundle only (no upload)
//   upload                      -> saves, then uploads to the existing world. Needs the owner's OK in chat first.
// The result goes to Review/agent-queue/<same name>.result.txt and the .cmd file is deleted.
// Jobs wait while Unity is compiling, updating or in Play mode.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;

[InitializeOnLoad]
public static class AstraAgentQueue
{
    const string Dir = "Review/agent-queue";
    static double nextCheck;

    static AstraAgentQueue()
    {
        Directory.CreateDirectory(Dir);
        EditorApplication.update += Poll;
        File.WriteAllText(Path.Combine(Dir, "_listener.txt"),
            "Listening since " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (Unity " + Application.unityVersion + ")\n");
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 2;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string file;
        try { file = Directory.GetFiles(Dir, "*.cmd").OrderBy(File.GetCreationTimeUtc).FirstOrDefault(); }
        catch { return; }
        if (file == null) return;

        string command, result;
        try { command = (File.ReadLines(file).FirstOrDefault() ?? "").Trim(); File.Delete(file); }
        catch (Exception e) { Write(file, "Could not read command: " + e.Message); return; }
        try { result = Run(command); }
        catch (Exception e) { result = "FAILED: " + e.Message; Debug.LogException(e); }
        Write(file, DateTime.Now.ToString("HH:mm:ss") + " " + command + "\n" + result);
    }

    static void Write(string cmdFile, string text) =>
        File.WriteAllText(Path.ChangeExtension(cmdFile, null) + ".result.txt", text + "\n");

    static string Run(string command)
    {
        var parts = command.Split(new[] { ' ' }, 2);
        var verb = parts[0].ToLowerInvariant();
        var arg = parts.Length > 1 ? parts[1].Trim() : "";
        switch (verb)
        {
            case "status": return Status();
            case "refresh": AssetDatabase.Refresh(); return "Refreshed. Check status again after compiling finishes.";
            case "save": EditorSceneManager.SaveOpenScenes(); return "Saved open scenes.";
            case "menu":
                if (!arg.StartsWith("Astra/")) return "REFUSED: only Astra/ menu items can be run from the queue.";
                EditorSceneManager.SaveOpenScenes();
                return EditorApplication.ExecuteMenuItem(arg) ? "Ran " + arg + ". See its own Review/ report." : "FAILED: no menu item " + arg;
            case "desktop-test": EditorSceneManager.SaveOpenScenes(); AstraReleaseCheck.DesktopTest(); return "Started. See Review/desktop-test-status.txt.";
            case "build": EditorSceneManager.SaveOpenScenes(); AstraReleaseCheck.BuildOnly(); return "Started. See Review/windows-build-status.txt.";
            case "upload":
                if (APIUser.CurrentUser == null) return "NOT SIGNED IN: the owner needs to sign in to the VRChat SDK panel once.";
                EditorSceneManager.SaveOpenScenes(); AstraReleaseCheck.UploadNow(); return "Started. See Review/reupload-status.txt.";
            default: return "Unknown command. Use: status, refresh, save, menu Astra/..., desktop-test, build, upload.";
        }
    }

    static string Status()
    {
        var user = APIUser.CurrentUser;
        var scene = EditorSceneManager.GetActiveScene();
        return "SDK account: " + (user == null ? "NOT SIGNED IN" : user.displayName) +
               "\nScene: " + scene.path + (scene.isDirty ? " (unsaved changes)" : "") +
               "\nCompiling: " + EditorApplication.isCompiling + ", Play mode: " + EditorApplication.isPlaying;
    }
}
