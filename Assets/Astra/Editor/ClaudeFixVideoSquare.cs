// Claude (2026-10-01): fix the grey square on the video screen.
// Cause: the VIDEO SCREEN switch (AstraWorldItems.screenRenderers) included the video player's two hidden helper quads
// (Internals/UnityRenderTextureFetch and AVProRenderTextureFetch). On world start the switch turned them ON, so a grey
// 5.6 m square (Unity's default material) appeared in front of the 4x screen. This removes them from the switch list
// and keeps them off. Nothing else changes. Menu: Astra > Claude - Fix Video Square.
using System.IO;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeFixVideoSquare
{
    const string ScenePath = "Assets/Astra/Scenes/AstrasInfinitePole.unity";

    [MenuItem("Astra/Claude - Fix Video Square")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        File.Copy(ScenePath, "Review/Backups/BeforeClaudeFixVideoSquare.unity.txt", true);
        var items = Object.FindObjectOfType<AstraWorldItems>();
        int before = items.screenRenderers.Length;
        var helpers = items.screenRenderers.Where(r => r != null && r.name.Contains("RenderTextureFetch")).ToArray();
        items.screenRenderers = items.screenRenderers.Where(r => r != null && !r.name.Contains("RenderTextureFetch")).ToArray();
        foreach (var r in helpers) { r.enabled = false; EditorUtility.SetDirty(r); }
        UdonSharpEditorUtility.CopyProxyToUdon(items);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string left = string.Join(", ", items.screenRenderers.Select(r => r.name));
        string report = $"Video screen switch: {before} renderers -> {items.screenRenderers.Length} ({left}). Helper quads removed and kept off: {helpers.Length}.";
        File.WriteAllText("Review/claude-fix-video-square.txt", report);
        Debug.Log("CLAUDE_FIX_VIDEO_SQUARE_OK " + report);
    }
}
