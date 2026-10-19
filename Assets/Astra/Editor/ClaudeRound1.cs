// Claude round 1 (2026-09-23): the look.
// Menu: Astra > Claude Round 1 - Look. Safe to run more than once.
// - Clouds removed (hidden, toggle hidden)
// - Pole: chrome sparkle shader (InfinitePole.shader), neutral tint
// - Stairs: no lines, pink glitter, wider (2.8m-5.1m radius), infinite up AND down, color slider in the menu
// - Floor: opening where the stairs pass so you can walk down; respawn height moved far below
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClaudeRound1
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const float Inner = 2.8f, Outer = 5.8f;          // was 2.3 / 4.3, then 2.8 / 5.1
    const float HoleIn = 2.6f, HoleOut = 6.0f, FloorR = 48f;

    [MenuItem("Astra/Claude Round 1 - Look")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND1 v2 start");
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        var scene = EditorSceneManager.OpenScene(Scene);
        Directory.CreateDirectory("Review/Backups");
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound1.unity.txt", true);

        // 1. Clouds off
        var magic = Object.FindObjectOfType<AstraMagic>();
        if (magic.cloudToggle != null) { magic.cloudToggle.isOn = false; magic.cloudToggle.gameObject.SetActive(false); }
        if (magic.clouds != null) { magic.clouds.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); magic.clouds.gameObject.SetActive(false); }
        UdonSharpEditorUtility.CopyProxyToUdon(magic);

        // 2. Pole: chrome tint (shader file already replaced)
        var pole = GameObject.Find("Infinite Pole - 45mm diameter");
        var poleMat = pole.GetComponent<Renderer>().sharedMaterial;
        poleMat.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(poleMat);

        // 3. Stairs: wider meshes, rebuilt in place so every reference stays valid
        Rebuild(AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Materials/Spiral Steps.asset"), false);
        Rebuild(AssetDatabase.LoadAssetAtPath<Mesh>(Root + "Materials/Spiral Walkable Ramp.asset"), true);
        var stairMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Prismatic Stair.mat");
        stairMat.shader = Shader.Find("Astra/Prismatic Stair");
        stairMat.SetColor("_Color", new Color(1f, .42f, .74f, 1f));
        stairMat.SetFloat("_Hue", 0f);
        stairMat.SetFloat("_Opacity", .3f); // .6 until round 3a (owner: more see through)
        stairMat.SetFloat("_Glitter", 2.5f);
        EditorUtility.SetDirty(stairMat);

        // 4. Color slider on the existing menu panel
        var spiral = Object.FindObjectOfType<AstraSpiral>();
        Transform panel = null;
        foreach (var tr in Resources.FindObjectsOfTypeAll<Transform>())
            if (tr.name == "Glitter studio" && tr.gameObject.scene == scene) { panel = tr; break; }
        if (panel == null) throw new System.Exception("Menu panel 'Glitter studio' not found");
        var existing = panel.Find("STAIR COLOR");
        Slider slider = existing != null ? existing.GetComponent<Slider>() : MakeSlider(panel, "STAIR COLOR", "STAIR COLOR  (pink  →  any color)", 0, -730);
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(spiral);
        while (slider.onValueChanged.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(slider.onValueChanged, 0);
        UnityEventTools.AddStringPersistentListener(slider.onValueChanged, udon.SendCustomEvent, "ApplyColor");
        slider.value = 0;
        spiral.stairMaterial = stairMat;
        spiral.colorSlider = slider;
        UdonSharpEditorUtility.CopyProxyToUdon(spiral);

        // 5. Floor with a stair opening + deep respawn so the stairs go down forever
        var floor = Object.FindObjectsOfType<MeshCollider>().First(x => x.name.StartsWith("Invisible platform"));
        string floorPath = Root + "Meshes/Platform With Stair Opening.asset";
        var floorMesh = AssetDatabase.LoadAssetAtPath<Mesh>(floorPath);
        if (floorMesh == null) { floorMesh = new Mesh(); AssetDatabase.CreateAsset(floorMesh, floorPath); }
        BuildFloor(floorMesh);
        floor.sharedMesh = null; floor.sharedMesh = floorMesh;
        floor.transform.localScale = Vector3.one;
        floor.name = "Invisible platform - 48m radius, stair opening";
        var descriptor = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
        descriptor.RespawnHeightY = -20000;
        EditorUtility.SetDirty(descriptor);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate(floor, spiral);
        Debug.Log("CLAUDE_ROUND1_OK");
    }

    // ---------- stairs ----------
    static List<Vector3> v; static List<int> t; static List<Vector2> uv;
    static void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int k = v.Count; v.AddRange(new[] { a, b, c, d }); uv.AddRange(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }); t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 }); }
    static Vector3 P(float r, float a, float y) { return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
    static void Rebuild(Mesh m, bool ramp)
    {
        v = new List<Vector3>(); t = new List<int>(); uv = new List<Vector2>();
        for (int step = 0; step < 40; step++) for (int j = 0; j < 3; j++)
        {
            float a = -Mathf.PI / 2 + (step + j / 3f) * Mathf.PI * 2 / 40, b = -Mathf.PI / 2 + (step + (j + 1) / 3f) * Mathf.PI * 2 / 40;
            float y = (step + 1) * .16f, ya = ramp ? (step + j / 3f) * .16f : y, yb = ramp ? (step + (j + 1) / 3f) * .16f : y;
            var ia = P(Inner, a, ya); var ib = P(Inner, b, yb); var ob = P(Outer, b, yb); var oa = P(Outer, a, ya); Quad(ia, ib, ob, oa);
            if (!ramp) { var down = Vector3.down * .065f; Quad(oa + down, ob + down, ib + down, ia + down); Quad(ia + down, ib + down, ib, ia); Quad(oa, ob, ob + down, oa + down); if (j == 0) Quad(ia, oa, oa + down, ia + down); if (j == 2) Quad(ob, ib, ib + down, ob + down); }
        }
        m.Clear(); m.SetVertices(v); m.SetTriangles(t, 0); m.SetUVs(0, uv); m.RecalculateNormals(); m.RecalculateBounds();
        EditorUtility.SetDirty(m);
    }

    // ---------- floor: center disc + outer ring, open where the stairs pass ----------
    static void BuildFloor(Mesh m)
    {
        const int seg = 96;
        var verts = new List<Vector3> { Vector3.zero }; var tris = new List<int>();
        for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2 / seg; verts.Add(new Vector3(Mathf.Sin(a) * HoleIn, 0, Mathf.Cos(a) * HoleIn)); }
        for (int i = 0; i < seg; i++) { tris.Add(0); tris.Add(i + 1); tris.Add((i + 1) % seg + 1); }
        int ring = verts.Count;
        for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2 / seg; verts.Add(new Vector3(Mathf.Sin(a) * HoleOut, 0, Mathf.Cos(a) * HoleOut)); verts.Add(new Vector3(Mathf.Sin(a) * FloorR, 0, Mathf.Cos(a) * FloorR)); }
        for (int i = 0; i < seg; i++)
        {
            int ii = ring + i * 2, io = ii + 1, ni = ring + ((i + 1) % seg) * 2, no = ni + 1;
            tris.AddRange(new[] { ii, io, no, ii, no, ni });
        }
        m.Clear(); m.name = "Invisible platform with stair opening"; m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
        EditorUtility.SetDirty(m);
    }

    // ---------- menu slider in Astra's style ----------
    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.anchoredPosition = pos; r.sizeDelta = size; return r; }
    static Slider MakeSlider(Transform p, string name, string text, float x, float y)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var lr = Rect(name + " label", p, new Vector2(x, y + 27), new Vector2(470, 38));
        var label = lr.gameObject.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = 17; label.color = new Color(.94f, .85f, 1); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        var r = Rect(name, p, new Vector2(x, y), new Vector2(460, 16));
        r.gameObject.AddComponent<Image>().color = new Color(.15f, .08f, .24f);
        var s = r.gameObject.AddComponent<Slider>();
        var area = Rect("Travel", r, Vector2.zero, new Vector2(430, 16));
        var h = Rect("Gem", area, Vector2.zero, new Vector2(24, 28));
        s.handleRect = h; s.targetGraphic = h.gameObject.AddComponent<Image>(); s.targetGraphic.color = new Color(1f, .45f, .78f);
        s.minValue = 0; s.maxValue = 1;
        return s;
    }

    static void Validate(MeshCollider floor, AstraSpiral spiral)
    {
        Physics.SyncTransforms();
        // Standing spots: spawn area, near pole, open stairs gap, outer floor
        if (!Physics.Raycast(new Vector3(0, 1, -2), Vector3.down, out var h1, 2) || h1.collider != floor) throw new System.Exception("Spawn floor missing");
        if (!Physics.Raycast(new Vector3(20, 1, 0), Vector3.down, out var h2, 2) || h2.collider != floor) throw new System.Exception("Outer floor missing");
        if (Physics.Raycast(new Vector3(0, .5f, 4f), Vector3.down, out var h3, 1) && h3.collider == floor) throw new System.Exception("Stair opening not open");
        // Stairs below the floor exist after recentering at spawn
        spiral.Recenter(0); Physics.SyncTransforms();
        if (!Physics.Raycast(new Vector3(0, -3f, -(Inner + Outer) / 2), Vector3.down, 7f)) throw new System.Exception("No stairs below floor");
        spiral.Recenter(0);
        foreach (string name in new[] { "Astra/Prismatic Stair", "Astra/Infinite Pole" })
            if (ShaderUtil.ShaderHasError(Shader.Find(name))) throw new System.Exception("Shader error: " + name);
        File.WriteAllText("Review/claude-round1-validation.txt", "PASS: floor at spawn and outer ring, stair opening open, stairs below floor, shaders compile. Visual look and walking need the desktop test.");
    }
}
