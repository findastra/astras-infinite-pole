// Claude round 1c (2026-09-24): more stair floors (21 recycled turns, about 64 m visible each way)
// plus verified optimizations. Menu: Astra > Claude Round 1c - More Stairs + Optimizations. Safe to rerun.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClaudeRound1c
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const int Turns = 21;

    [MenuItem("Astra/Claude Round 1c - More Stairs + Optimizations")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND1C start");
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound1c.unity.txt", true);
        var report = new List<string>();

        // 1. Stair pool 11 -> 21 turns
        var spiral = Object.FindObjectOfType<AstraSpiral>();
        var list = spiral.turns.Where(t => t != null).ToList();
        var template = list[0].gameObject;
        while (list.Count < Turns)
        {
            var copy = Object.Instantiate(template, template.transform.parent);
            copy.name = "Spiral turn " + list.Count;
            list.Add(copy.transform);
        }
        spiral.turns = list.ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(spiral);
        report.Add("Stair turns: " + list.Count + " (visible about " + ((Turns - 1) / 2 * spiral.pitch).ToString("0") + " m up and down)");

        // keep the WORLD ITEMS stairs switch covering the new turns
        var items = Object.FindObjectOfType<AstraWorldItems>();
        if (items != null)
        {
            items.stairRenderers = spiral.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray();
            UdonSharpEditorUtility.CopyProxyToUdon(items);
        }

        // 2. GPU instancing on the shared stair material (VRChat Android guide: enable GPU instancing)
        var stairMat = list[0].GetComponent<MeshRenderer>().sharedMaterial;
        stairMat.enableInstancing = true; EditorUtility.SetDirty(stairMat);
        report.Add("Stair material GPU instancing: " + stairMat.enableInstancing);

        // 3. Star dust: local space + no noise so Unity can pause it when off screen (procedural culling)
        var dust = GameObject.Find("10 - Stair opening star dust");
        if (dust != null)
        {
            var ps = dust.GetComponent<ParticleSystem>();
            var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var vel = ps.velocityOverLifetime; vel.space = ParticleSystemSimulationSpace.Local;
            var noise = ps.noise; noise.enabled = false;
            report.Add("Star dust procedural culling supported: " + ps.proceduralSimulationSupported);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Validation: stairs exist 40 m below and 40 m above spawn after recentering
        spiral.Recenter(0); Physics.SyncTransforms();
        float mid = 3.95f;
        foreach (float y0 in new[] { -40f, 40f })
        {
            bool hit = Physics.Raycast(new Vector3(0, y0, -mid), Vector3.down, 7f);
            report.Add("Stairs at " + y0 + " m: " + (hit ? "PASS" : "FAIL"));
        }
        File.WriteAllText("Review/claude-round1c-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND1C_OK " + string.Join(" | ", report));
    }
}
