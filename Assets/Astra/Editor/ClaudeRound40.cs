// Claude, 2026-10-19. Round 40 (owner): "make sure the new phone booth is in Astra's Infinite Pole", replacing all
// three booths (Astra's choice). The new booth is Unity Unicorn's library asset liminal-doors-phone-booth v1.0.0: a
// handmade K6 kiosk with its own payphone, made in Liminal Doors and exported with unity-asset-creator.
// 40s  survey every phone booth (read-only) -> Review/round40/survey.txt
// 40i  import the booth package (Assets/LiminalDoors/Booth, Assets/ShadersShop/UnityStable)
// 40   APPLY: in every booth the old look (booth body, glass, sign glow, payphones) is switched off (kept) and the K6
//      prefab is placed in the same spot at the old booth's height, door facing the old opening. The booth's phone
//      line, voice zone, triggers and floor are not touched.
// 40r  restores the scene from Review/Backups/BeforeRound40/.   40p photos -> Review/round40/.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ClaudeRound40
{
    const string Out = "Review/round40";
    const string Backup = "Review/Backups/BeforeRound40/AstrasInfinitePole.unity.txt";
    const string PrefabPath = "Assets/LiminalDoors/Booth/Phone Booth.prefab";
    const string NewName = "K6 booth (library liminal-doors-phone-booth v1.0.0)";
    static readonly string[] OldLook = { "Booth body", "Booth glass", "Booth sign glow", "Payphone (CC0 pink)", "Payphone detail" };

    static List<Transform> Booths() => Object.FindObjectsOfType<Transform>(true)
        .Where(t => t.Find("Booth body") != null).OrderBy(t => t.position.y).ToList();

    static string Comps(Transform t) => string.Join(", ", t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c =>
    {
        if (c is BoxCollider b) return "BoxCollider" + (b.isTrigger ? "(trigger)" : "") + (b.enabled ? "" : "(off)") + " c" + b.center + " s" + b.size;
        if (c is Collider k) return c.GetType().Name + (k.isTrigger ? "(trigger)" : "") + (k.enabled ? "" : "(off)");
        if (c is MeshFilter mf) return "Mesh " + (mf.sharedMesh ? mf.sharedMesh.name + " " + mf.sharedMesh.bounds.size : "-");
        if (c is Renderer r) return c.GetType().Name + (r.enabled ? "" : "(off)") + " " + string.Join("/", r.sharedMaterials.Select(m => m ? m.name : "-"));
        return c.GetType().Name;
    }));

    static void Dump(System.Text.StringBuilder sb, Transform t, int d)
    {
        sb.AppendLine(new string(' ', d * 2) + (t.gameObject.activeSelf ? "" : "[off] ") + t.name + " | lp " + t.localPosition + " lr " + t.localEulerAngles + " ls " + t.localScale + " | " + Comps(t));
        foreach (Transform c in t) Dump(sb, c, d + 1);
    }

    [MenuItem("Astra/Claude/40s Survey the phone booths (read-only)")]
    public static void Survey()
    {
        Directory.CreateDirectory(Out); var sb = new System.Text.StringBuilder();
        foreach (var b in Booths())
        {
            sb.AppendLine("== " + ClaudeCloudInspect.Path(b) + " pos " + b.position + " rot " + b.eulerAngles + " lossy " + b.lossyScale + " fwd " + b.forward);
            var body = b.Find("Booth body").GetComponent<Renderer>(); if (body) sb.AppendLine("   body world bounds " + body.bounds.min + " .. " + body.bounds.max);
            Dump(sb, b, 1);
            // what else stands in the booth's space (siblings such as the voice zone or the cloud floor)
            var area = new Bounds(body.bounds.center, body.bounds.size + Vector3.one * .4f);
            foreach (var c in Object.FindObjectsOfType<Collider>(true).Where(c => !c.transform.IsChildOf(b) && c.bounds.Intersects(area)))
                sb.AppendLine("   near: " + ClaudeCloudInspect.Path(c.transform) + " | " + c.GetType().Name + (c.isTrigger ? "(trigger)" : "") + " bounds " + c.bounds.min + " .. " + c.bounds.max);
        }
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (p != null) { sb.AppendLine("== prefab " + PrefabPath); Dump(sb, p.transform, 1); }
        File.WriteAllText(Path.Combine(Out, "survey.txt"), sb.ToString());
    }

    [MenuItem("Astra/Claude/40i Import the K6 booth package")]
    public static void Import()
    {
        Directory.CreateDirectory(Out);
        string pkg = Path.GetFullPath("Review/round40/liminal-doors-phone-booth-v1.0.0.unitypackage");
        File.WriteAllText(Path.Combine(Out, "import.txt"), File.Exists(pkg) ? "importing " + pkg + "\n" : "MISSING " + pkg + "\n");
        if (File.Exists(pkg)) AssetDatabase.ImportPackage(pkg, false);
    }

    [MenuItem("Astra/Claude/40 Replace the booths with the K6 booth")]
    public static void Apply()
    {
        var rep = new List<string> { "Round 40 apply " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
        Directory.CreateDirectory(Out);
        var scene = EditorSceneManager.GetActiveScene(); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(Backup)); if (!File.Exists(Backup)) File.Copy(scene.path, Backup, true);
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new System.Exception("prefab missing: run 40i first");
            var pr = prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<MeshFilter>()?.sharedMesh != null).ToList();
            var pb = new Bounds(); bool first = true;
            foreach (var r in pr) foreach (var c in Corners(r.GetComponent<MeshFilter>().sharedMesh.bounds)) { var w = prefab.transform.InverseTransformPoint(r.transform.TransformPoint(c)); if (first) { pb = new Bounds(w, Vector3.zero); first = false; } else pb.Encapsulate(w); }
            // the kiosk alone, without the open door that swings out on +Z
            var body = prefab.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh && m.sharedMesh.name.StartsWith("Booth body"));
            float kioskH = body ? body.sharedMesh.bounds.size.y * body.transform.lossyScale.y : pb.size.y;
            rep.Add("prefab bounds " + pb.min + " .. " + pb.max + ", kiosk height " + kioskH.ToString("0.00") + " m");
            int n = 0;
            foreach (var b in Booths())
            {
                var bodyR = b.Find("Booth body").GetComponent<Renderer>();
                // old booth's look in its own space: floor height, centre, height
                var inv = b.worldToLocalMatrix; var ob = new Bounds(inv.MultiplyPoint3x4(bodyR.bounds.center), Vector3.zero);
                foreach (var c in Corners(bodyR.bounds)) ob.Encapsulate(inv.MultiplyPoint3x4(c));
                var old = b.Find(NewName); if (old != null) Object.DestroyImmediate(old.gameObject);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, b); go.name = NewName; var t = go.transform;
                float s = ob.size.y / kioskH;   // same height as the booth it replaces, in the booth's own units
                t.localRotation = Quaternion.identity; t.localScale = Vector3.one * s;
                // centre on the booth floor (the old body's bounds include its open door); both doors open on +Z
                var floor = b.Find("Cloud booth floor");
                t.localPosition = floor != null ? new Vector3(floor.localPosition.x, ob.min.y, floor.localPosition.z) : new Vector3(0, ob.min.y, 0);
                // the K6 floor (plinth top, 8 cm) is what a player stands on now; AstraCloudOrbit carries a standing
                // player only when the hit collider's name starts with "Cloud ", so the booths on orbiting clouds keep riding
                var kf = t.Find("Booth floor"); if (kf != null) kf.name = "Cloud booth floor (K6)";
                Undo.RegisterCreatedObjectUndo(go, "k6 booth");
                foreach (var nm in OldLook) { var o = b.Find(nm); if (o != null && o.gameObject.activeSelf) { Undo.RecordObject(o.gameObject, "k6 booth"); o.gameObject.SetActive(false); } }
                rep.Add("booth " + ClaudeCloudInspect.Path(b) + ": old look off; K6 at local " + t.localPosition + " scale " + s.ToString("0.000") + " -> " + (kioskH * s * b.lossyScale.y).ToString("0.00") + " m tall in the world (old " + (ob.size.y * b.lossyScale.y).ToString("0.00") + " m), old footprint " + (ob.size.x * b.lossyScale.x).ToString("0.00") + " x " + (ob.size.z * b.lossyScale.z).ToString("0.00") + " m");
                n++;
            }
            rep.Add("booths replaced: " + n);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            rep.Add("ROUND40_OK");
        }
        catch (System.Exception e) { rep.Add("ERROR " + e); Debug.LogException(e); }
        File.WriteAllText(Path.Combine(Out, "apply-report.txt"), string.Join("\n", rep) + "\n");
    }

    static IEnumerable<Vector3> Corners(Bounds bb) { for (int i = 0; i < 8; i++) yield return new Vector3(i % 2 == 0 ? bb.min.x : bb.max.x, (i / 2) % 2 == 0 ? bb.min.y : bb.max.y, i / 4 == 0 ? bb.min.z : bb.max.z); }

    [MenuItem("Astra/Claude/40r Restore scene from before round 40")]
    public static void Restore()
    {
        var path = EditorSceneManager.GetActiveScene().path; File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path); EditorSceneManager.OpenScene(path);
        File.WriteAllText(Path.Combine(Out, "restore.txt"), "restored " + path + "\n");
    }

    [MenuItem("Astra/Claude/40p K6 booth photos")]
    public static void Photos()
    {
        Directory.CreateDirectory(Out); int k = 0;
        foreach (var b in Booths())
        {
            var k6 = b.Find(NewName); if (k6 == null) continue;
            var rs = k6.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray(); if (rs.Length == 0) continue;
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); var c = bb.center; float h = bb.size.y; k++;
            var f = b.forward; var rgt = b.right;
            Shot("booth " + k + " front", c + f * h * 1.3f + Vector3.up * h * .05f, c, 50);
            Shot("booth " + k + " three-quarter", c + (f * 1.1f + rgt * .9f) * h + Vector3.up * h * .25f, c, 50);
            Shot("booth " + k + " inside", c + f * h * .32f + Vector3.up * h * .1f, c - f * h * .2f, 60);
            Shot("booth " + k + " context", c + (f * 3.2f + rgt * 2f) * h + Vector3.up * h * 1.2f, c, 55);
        }
    }

    static void Shot(string name, Vector3 pos, Vector3 look, float fov)
    {
        var go = new GameObject("Claude photo camera") { hideFlags = HideFlags.DontSave };
        var cam = go.AddComponent<Camera>(); cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 3000;
        var pp = go.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform;
        var rt = new RenderTexture(1400, 1000, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1400, 1000, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1400, 1000), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
    }
}
