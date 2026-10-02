// Claude, 2026-10-01. Menu: Astra > Claude > 23 Stormscape, horizon fix, tighter beads.
//  1. Chandelier crystals overlap halfway into each other (ClaudeRound22 with Spacing .5), so strands read as one string.
//  2. Horizon: the pink cloud sea now melts fully into the sky's horizon colour before the camera's far plane
//     (the shader does the fade; here the sea's haze colour is matched to the sunset sky's horizon).
//  3. Stormscape: a shared STORMSCAPE button under PINKSCAPE on the sky panel. AstraStorm.cs fades the whole world to a
//     dark storm: dark rainbow grey/black clouds, storm sky, rain around you, lightning with thunder, faster chandeliers.
// Safe to rerun. Backup: Review/Backups/BeforeRound23.unity.txt. Report: Review/claude-round23-report.txt.
// Photos: Review/Photos/Round23/.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using Object = UnityEngine.Object;

public static class ClaudeRound23
{
    const string Root = "Assets/Astra/";
    const string StormName = "16 - Stormscape (Claude)";
    static List<string> report;

    [MenuItem("Astra/Claude/23 Stormscape, horizon fix, tighter beads")]
    public static void Run()
    {
        report = new List<string>();
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Review/Backups"); File.Copy(scene.path, "Review/Backups/BeforeRound23.unity.txt", true);
            Step("tighter beads", () => { ClaudeRound22.Spacing = .5f; ClaudeRound22.Run(); report.Add("beads: " + File.ReadAllText("Review/claude-round22-report.txt").Replace("\n", " | ")); });
            Step("horizon", Horizon);
            AstraStorm storm = null;
            Step("stormscape", () => storm = Storm());
            Step("menu button", () => MenuButton(storm));
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Step("photos", () => Photos(storm));
        }
        catch (Exception e) { report.Add("FAILED: " + e); }
        File.WriteAllText("Review/claude-round23-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND23 " + string.Join(" | ", report));
    }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("STEP FAILED (" + name + "): " + e.Message + "\n" + e.StackTrace); } }

    // ---------------- 2. horizon
    static void Horizon()
    {
        var sea = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Backdrop/Pink Cloud Sea.mat");
        var atm = Object.FindObjectOfType<AstraAtmosphere>();
        var sky = atm != null && atm.startSky != null ? atm.startSky : AssetDatabase.FindAssets("t:Material", new[] { Root + "Materials" }).Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(m => m != null && m.shader != null && m.shader.name == "Astra/Cloud Sea Sky");
        if (sea == null) { report.Add("CHECK: Pink Cloud Sea material not found"); return; }
        if (sky != null && sky.HasProperty("_Horizon"))
        {
            var before = sea.GetColor("_Horizon"); var h = sky.GetColor("_Horizon");
            sea.SetColor("_Horizon", new Color(h.r, h.g, h.b, 1)); EditorUtility.SetDirty(sea);
            report.Add("horizon: sea haze colour " + Fmt(before) + " -> sky horizon " + Fmt(h) + " (" + sky.name + "); sea now fades out fully by 85% of the far plane");
        }
        else report.Add("horizon: sky material with _Horizon not found; shader fade still applies");
        var cam = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>()?.ReferenceCamera?.GetComponent<Camera>();
        if (cam != null) report.Add("reference camera far plane: " + cam.farClipPlane + " m (sea fade 45%-85% of it)");
    }
    static string Fmt(Color c) { return "(" + c.r.ToString("0.00") + "," + c.g.ToString("0.00") + "," + c.b.ToString("0.00") + ")"; }

    // ---------------- 3. stormscape
    static Material Mat(string name, string shader)
    {
        Directory.CreateDirectory(Root + "Materials/Storm");
        string path = Root + "Materials/Storm/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh = Shader.Find(shader); if (sh == null) throw new Exception("shader missing: " + shader);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh; EditorUtility.SetDirty(m); return m;
    }
    static AstraStorm Storm()
    {
        EnsureProgram("AstraStorm");
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var old in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t != null && t.gameObject.scene == scene && t.name == StormName).ToList()) if (old != null) Object.DestroyImmediate(old.gameObject);
        foreach (var sh in new[] { "Astra/Glitter Cloud", "Astra/Cloud Sea", "Astra/Storm Sky", "Astra/Storm Rain", "Astra/Storm Bolt", "Astra/Prismatic Stair", "Astra/Rainbow Flow" })
        { var s = Shader.Find(sh); if (s == null) report.Add("CHECK: shader not found " + sh); else if (ShaderUtil.ShaderHasError(s)) report.Add("CHECK: shader has errors " + sh); }

        var skyMat = Mat("Storm Sky", "Astra/Storm Sky");
        var rainMat = Mat("Storm Rain", "Astra/Storm Rain"); rainMat.enableInstancing = true;
        var boltMat = Mat("Storm Bolt", "Astra/Storm Bolt");
        string texPath = Root + "Textures/Storm Bolt.png";
        var ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (ti != null) { ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.sRGBTexture = true; ti.alphaSource = TextureImporterAlphaSource.None; ti.SaveAndReimport(); }
        boltMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));

        var root = new GameObject(StormName); SceneManagerMove(root, scene);
        var storm = root.AddUdonSharpComponent<AstraStorm>();
        var follower = new GameObject("Storm follower").transform; follower.SetParent(root.transform, false);

        // rain: thin streaks falling all around you
        var rgo = new GameObject("Storm rain"); rgo.transform.SetParent(follower, false); rgo.transform.localPosition = new Vector3(0, 14f, 0);
        var ps = rgo.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = false; main.prewarm = false; main.maxParticles = 3200;
        main.startLifetime = 1.15f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(.022f, .04f);
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = new Color(1, 1, 1, 1);
        var em = ps.emission; em.rateOverTime = 1700f;
        var shp = ps.shape; shp.shapeType = ParticleSystemShapeType.Box; shp.scale = new Vector3(36f, 1f, 36f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(.9f); vel.y = new ParticleSystem.MinMaxCurve(-16f); vel.z = new ParticleSystem.MinMaxCurve(.3f);
        var pr = rgo.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = .05f; pr.lengthScale = 1.5f;
        pr.sharedMaterial = rainMat; pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false;

        // lightning bolts: six cards far out round you; one flickers on per strike
        var boltsRoot = new GameObject("Storm bolts").transform; boltsRoot.SetParent(follower, false);
        var quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx"); var rng = new System.Random(2310); var bolts = new List<GameObject>();
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + (float)rng.NextDouble() * 40f) * Mathf.Deg2Rad, rad = 170f + (float)rng.NextDouble() * 60f, hgt = 190f + (float)rng.NextDouble() * 60f;
            var b = new GameObject("Bolt " + (i + 1)); b.transform.SetParent(boltsRoot, false);
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            b.transform.localPosition = dir * rad + Vector3.up * (hgt * .5f - 45f);
            b.transform.localRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, (float)rng.NextDouble() * 16f - 8f);
            b.transform.localScale = new Vector3(hgt * .25f * (rng.NextDouble() < .5 ? -1 : 1), hgt, 1);
            b.AddComponent<MeshFilter>().sharedMesh = quad; var mr = b.AddComponent<MeshRenderer>(); mr.sharedMaterial = boltMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            b.SetActive(false); bolts.Add(b);
        }

        // thunder: 2D, everyone hears the same strike
        var au = root.AddComponent<AudioSource>(); au.playOnAwake = false; au.spatialBlend = 0f; au.volume = .55f; au.loop = false;
        var clips = Enumerable.Range(1, 3).Select(k => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Thunder " + k + ".wav")).Where(c => c != null).ToArray();
        if (clips.Length == 0) report.Add("CHECK: thunder clips not found");

        storm.stormSky = skyMat; storm.follower = follower; storm.rain = ps; storm.bolts = bolts.ToArray(); storm.thunder = au; storm.thunderClips = clips;
        storm.sways = Object.FindObjectsOfType<AstraVineSway>(true);
        storm.lights = Object.FindObjectsOfType<Light>(true).Where(l => l.type == LightType.Directional).ToArray();
        UdonSharpEditorUtility.CopyProxyToUdon(storm);
        report.Add("stormscape: storm sky, rain (" + main.maxParticles + " max), " + bolts.Count + " lightning bolts, " + clips.Length + " thunder sounds, "
            + storm.sways.Length + " chandeliers blow " + storm.windInStorm + "x faster, " + storm.lights.Length + " sun lights dim to 30%; fades over " + storm.fadeSeconds + " s; shared with everyone");
        return storm;
    }
    static void SceneManagerMove(GameObject g, UnityEngine.SceneManagement.Scene s) { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, s); }

    static void MenuButton(AstraStorm storm)
    {
        if (storm == null) { report.Add("menu: no storm object, button skipped"); return; }
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        var canvas = menu != null ? menu.menuRoot.GetComponentsInChildren<Canvas>(true).FirstOrDefault(x => x.name == "Celestial observatory") : null;
        if (canvas == null) { report.Add("CHECK: sky panel (Celestial observatory) not found; no button"); return; }
        var panel = canvas.GetComponent<RectTransform>();
        var u = UdonSharpEditorUtility.GetBackingUdonBehaviour(storm);
        var existing = panel.Find("STORMSCAPE");
        Button btn;
        if (existing == null)
        {
            // one more row at the bottom: grow the panel by a row and shift its contents up half a row so the margins stay even
            const float row = 75f, dy = row * .5f;
            foreach (Transform c in panel) { var rt = c as RectTransform; if (rt == null || rt.anchorMin != rt.anchorMax) continue; rt.anchoredPosition += new Vector2(0, dy); EditorUtility.SetDirty(rt); }
            panel.sizeDelta += new Vector2(0, row);
            var bc = panel.GetComponent<BoxCollider>(); if (bc != null) bc.size = new Vector3(panel.sizeDelta.x, panel.sizeDelta.y, bc.size.z);
            var backing = panel.Find("Opaque backing"); if (backing != null) backing.localScale = new Vector3(panel.sizeDelta.x, panel.sizeDelta.y, backing.localScale.z);
            var pink = panel.Find("PINKSCAPE") as RectTransform;
            float lowest = panel.Cast<Transform>().OfType<RectTransform>().Where(r => r.GetComponent<Selectable>() != null && r.anchorMin == r.anchorMax).Min(r => r.anchoredPosition.y);
            var go = new GameObject("STORMSCAPE", typeof(RectTransform), typeof(Image), typeof(Button)); var r2 = go.GetComponent<RectTransform>(); r2.SetParent(panel, false);
            r2.anchoredPosition = new Vector2(0, lowest - row); r2.sizeDelta = pink != null ? pink.sizeDelta : new Vector2(320, 55);
            btn = go.GetComponent<Button>(); btn.targetGraphic = go.GetComponent<Image>();
            var pinkImg = pink != null ? pink.GetComponent<Image>() : null; btn.targetGraphic.color = new Color(.07f, .065f, .1f);   // storm grey instead of the pink buttons
            if (pinkImg != null) { var img = (Image)btn.targetGraphic; img.sprite = pinkImg.sprite; img.type = pinkImg.type; }
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); var lr = label.GetComponent<RectTransform>(); lr.SetParent(r2, false); lr.sizeDelta = r2.sizeDelta;
            var pinkText = pink != null ? pink.GetComponentInChildren<Text>() : null;
            var t = label.GetComponent<Text>(); t.font = pinkText != null ? pinkText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = pinkText != null ? pinkText.fontSize : 22;
            t.alignment = TextAnchor.MiddleCenter; t.color = new Color(.82f, .86f, 1f); t.text = "STORMSCAPE  /  OFF"; t.raycastTarget = false;
            report.Add("menu: STORMSCAPE button added at y " + r2.anchoredPosition.y + " on the sky panel (panel now " + panel.sizeDelta.y + " tall)");
        }
        else { btn = existing.GetComponent<Button>(); report.Add("menu: STORMSCAPE button re-bound"); }
        while (btn.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(btn.onClick, 0);
        UnityEventTools.AddStringPersistentListener(btn.onClick, u.SendCustomEvent, "Toggle"); EditorUtility.SetDirty(btn);
        storm.label = btn.GetComponentInChildren<Text>(); UdonSharpEditorUtility.CopyProxyToUdon(storm);
    }

    // ---------------- photos (editor only; nothing is saved)
    static void Photos(AstraStorm storm)
    {
        string dir = "Review/Photos/Round23"; Directory.CreateDirectory(dir);
        var spiral = Object.FindObjectOfType<AstraSpiral>(); if (spiral != null) spiral.Recenter(0);
        var desc = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
        Vector3 spawn = desc != null && desc.spawns != null && desc.spawns.Length > 0 && desc.spawns[0] != null ? desc.spawns[0].position : new Vector3(0, 0, -2);
        Vector3 eye = spawn + Vector3.up * 1.6f; var outward = new Vector3(spawn.x, 0, spawn.z); if (outward.sqrMagnitude < .01f) outward = Vector3.back; outward.Normalize();
        foreach (var p in Object.FindObjectsOfType<ParticleSystem>()) p.Simulate(12, true, true);
        if (storm != null) storm.rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // no rain in the calm shots
        Shader.SetGlobalFloat("_UdonStorm", 0); Shader.SetGlobalFloat("_UdonFlash", 0);
        Shot(dir, "1 Horizon (calm)", eye, eye + outward * 100f, 70);

        var ch = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Chandelier strands");
        if (ch != null) { var cc = ch.position + Vector3.down * 2f; var cw = new Vector3(cc.x, 0, cc.z).normalized; Shot(dir, "2 Chandelier beads close", cc + cw * 2.2f + Vector3.Cross(Vector3.up, cw) * .6f, cc, 40); }

        if (storm != null)
        {
            var saved = RenderSettings.skybox; RenderSettings.skybox = storm.stormSky;
            Shader.SetGlobalFloat("_UdonStorm", 1); storm.follower.position = spawn; storm.rain.Simulate(3, true, true);
            Shot(dir, "3 Stormscape", new Vector3(15, 7, -15), new Vector3(0, 9, 0), 60);
            Shot(dir, "4 Stormscape eye level", eye, eye + outward * 100f + Vector3.up * 12f, 75);
            if (storm.bolts.Length > 0) storm.bolts[0].SetActive(true);
            var bdir = storm.bolts.Length > 0 ? storm.bolts[0].transform.position : eye + outward * 100f;
            Shader.SetGlobalFloat("_UdonFlash", .85f);
            Shot(dir, "5 Stormscape lightning", eye, new Vector3(bdir.x, eye.y + 25f, bdir.z), 75);
            if (ch != null) { var cc = ch.position + Vector3.down * 2f; var cw = new Vector3(cc.x, 0, cc.z).normalized; Shader.SetGlobalFloat("_UdonFlash", 0); Shot(dir, "6 Storm chandelier", cc + cw * 5f + Vector3.up, cc, 55); }
            foreach (var b in storm.bolts) b.SetActive(false);
            Shader.SetGlobalFloat("_UdonStorm", 0); Shader.SetGlobalFloat("_UdonFlash", 0); RenderSettings.skybox = saved;
            storm.rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        // the sky panel with the new button
        var menu = Object.FindObjectOfType<AstraHandMenu>(true);
        if (menu != null)
        {
            var canvas = menu.menuRoot.GetComponentsInChildren<Canvas>(true).FirstOrDefault(x => x.name == "Celestial observatory");
            if (canvas != null)
            {
                bool was = menu.menuRoot.gameObject.activeSelf; menu.menuRoot.gameObject.SetActive(true);
                var rt = canvas.GetComponent<RectTransform>(); var corners = new Vector3[4]; rt.GetWorldCorners(corners);
                var c = (corners[0] + corners[2]) * .5f; float h = Vector3.Distance(corners[0], corners[1]); float fov = 40f;
                float d = h * .62f / Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
                Shot(dir, "7 Sky panel", c - rt.forward * d, c, fov, 900, 1500);
                menu.menuRoot.gameObject.SetActive(was);
            }
        }
        report.Add("photos: " + dir);
    }
    static void Shot(string dir, string name, Vector3 pos, Vector3 look, float fov, int w = 1920, int h = 1080)
    {
        var go = new GameObject("Claude photo camera");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .05f;
        var refCam = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>()?.ReferenceCamera?.GetComponent<Camera>();
        cam.farClipPlane = refCam != null ? refCam.farClipPlane : 350f;          // same far plane as in VRChat, so the horizon looks the same
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release();
        Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
    static void EnsureProgram(string name)
    {
        string path = Root + "Scripts/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) == null)
        {
            var a = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            a.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "Scripts/" + name + ".cs");
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets();
        }
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
    }
}
