// Claude, 2026-10-18. Round 39 (owner): "make this payphone look realistic".
// The booth payphones were a few flat primitive slabs. Unity Unicorn's rule for real-looking props: start from a real
// scan or model, never from primitives. Base: Poly Haven "Korean Public Payphone 01" (UM JOORIN, CC0), restyled pink
// by Tools/Blender/restyle_payphone.py (rose-pink metal that keeps its scratches and grime, brand marks painted out,
// 48k -> 16k triangles), with a real PBR texture set on Unity's Standard shader.
// 39  APPLY: mesh asset + textures + material; in every phone booth the old "Payphone detail" is switched off (kept)
//     and "Payphone (CC0 pink)" is mounted flush on the same back wall at its true size (0.74 m), centred on the
//     old face. Booth triggers, AstraPhoneBooth and the phone line are not touched.
// 39r restores the scene from Review/Backups/BeforeRound39/.   39p photos -> Review/round39/.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ClaudeRound39
{
    const string Out = "Review/round39";
    const string Backup = "Review/Backups/BeforeRound39/AstrasInfinitePole.unity.txt";
    const string TexDir = "Assets/Astra/Textures/Payphone/";
    const string MeshPath = "Assets/Astra/Meshes/Payphone/Payphone pink.asset";
    const string MatPath = "Assets/Astra/Materials/Payphone Pink.mat";
    [System.Serializable] class Item { public string name, source; public int tris, tris_source; public float[] size_m, vertices, normals, uvs; public int[] triangles; }

    [MenuItem("Astra/Claude/39 Realistic payphone (CC0 base, pink)")]
    public static void Apply()
    {
        var rep = new List<string> { "Round 39 apply " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
        Directory.CreateDirectory(Out);
        var scene = EditorSceneManager.GetActiveScene(); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(Backup)); if (!File.Exists(Backup)) File.Copy(scene.path, Backup, true);
        try
        {
            var mesh = BuildMesh(rep); var mat = BuildMaterial(rep);
            int n = 0;
            foreach (var pd in Object.FindObjectsOfType<Transform>(true).Where(t => t.name == "Payphone detail").ToList())
            {
                var booth = pd.parent; var rs = pd.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) { rep.Add("skip (no renderers): " + ClaudeCloudInspect.Path(pd)); continue; }
                // old face bounds in the booth's own space
                var inv = booth.worldToLocalMatrix; var b = new Bounds(inv.MultiplyPoint3x4(rs[0].bounds.center), Vector3.zero);
                foreach (var r in rs) { var bb = r.bounds; for (int i = 0; i < 8; i++) b.Encapsulate(inv.MultiplyPoint3x4(new Vector3(i % 2 == 0 ? bb.min.x : bb.max.x, (i / 2) % 2 == 0 ? bb.min.y : bb.max.y, i / 4 == 0 ? bb.min.z : bb.max.z))); }
                var body = booth.Find("Booth body"); float boothH = body != null ? body.GetComponent<MeshFilter>().sharedMesh.bounds.size.y : 2.75f;
                float s = 1f / Mathf.Abs(booth.lossyScale.y), h = mesh.bounds.size.y * s;   // true size: the scan is 0.74 m tall, like a real payphone
                var old = booth.Find("Payphone (CC0 pink)"); if (old != null) Object.DestroyImmediate(old.gameObject);
                var go = new GameObject("Payphone (CC0 pink)"); var t = go.transform; t.SetParent(booth, false);
                t.localRotation = Quaternion.identity; t.localScale = Vector3.one * s;
                t.localPosition = new Vector3(b.center.x, b.center.y - h * .5f, b.min.z);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
                Undo.RegisterCreatedObjectUndo(go, "payphone");
                Undo.RecordObject(pd.gameObject, "payphone"); pd.gameObject.SetActive(false);
                // the booth's sign-glow mesh also carries a lit backboard where the old flat phone hung; the real phone
                // doesn't cover it, so this booth gets a copy of the mesh without the triangles inside the old phone's box
                var glow = booth.Find("Booth sign glow");
                if (glow != null)
                {
                    var gmf = glow.GetComponent<MeshFilter>(); var src = gmf.sharedMesh;
                    if (src != null && !src.name.EndsWith("(no payphone panel)"))
                    {
                        string gp = "Assets/Astra/Meshes/Payphone/" + src.name + " (no payphone panel).asset";
                        var cut = AssetDatabase.LoadAssetAtPath<Mesh>(gp);
                        if (cut == null)
                        {
                            var box = new Bounds(b.center, b.size + new Vector3(.12f, .12f, .25f));
                            var gl = glow.localToWorldMatrix; var bl = booth.worldToLocalMatrix;
                            var vs = src.vertices; var keep = new List<int>(); var tr = src.triangles; int dropped = 0;
                            for (int i = 0; i < tr.Length; i += 3)
                            {
                                var c = bl.MultiplyPoint3x4(gl.MultiplyPoint3x4((vs[tr[i]] + vs[tr[i + 1]] + vs[tr[i + 2]]) / 3f));
                                if (box.Contains(c)) { dropped++; continue; }
                                keep.Add(tr[i]); keep.Add(tr[i + 1]); keep.Add(tr[i + 2]);
                            }
                            cut = Object.Instantiate(src); cut.name = src.name + " (no payphone panel)"; cut.SetTriangles(keep, 0); cut.RecalculateBounds();
                            AssetDatabase.CreateAsset(cut, gp);
                            rep.Add("sign glow: " + src.name + " copied without the payphone backboard (" + dropped + " of " + tr.Length / 3 + " triangles), " + gp);
                        }
                        Undo.RecordObject(gmf, "payphone"); gmf.sharedMesh = cut;
                    }
                }
                rep.Add("booth " + ClaudeCloudInspect.Path(booth) + ": old detail off; new phone " + (h * booth.lossyScale.y).ToString("0.00") + " m tall in the world, back at z " + b.min.z.ToString("0.00") + ", scale " + s.ToString("0.00"));
                n++;
            }
            rep.Add("booths updated: " + n);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            rep.Add("ROUND39_OK");
        }
        catch (System.Exception e) { rep.Add("ERROR " + e); Debug.LogException(e); }
        File.WriteAllText(Path.Combine(Out, "apply-report.txt"), string.Join("\n", rep) + "\n");
    }

    static Mesh BuildMesh(List<string> rep)
    {
        var it = JsonUtility.FromJson<Item>(File.ReadAllText("Tools/Blender/out/payphone_pink.json"));
        int n = it.vertices.Length / 3; var v = new Vector3[n]; var nn = new Vector3[n]; var uv = new Vector2[n];
        for (int i = 0; i < n; i++) { v[i] = new Vector3(it.vertices[i * 3], it.vertices[i * 3 + 1], it.vertices[i * 3 + 2]); nn[i] = new Vector3(it.normals[i * 3], it.normals[i * 3 + 1], it.normals[i * 3 + 2]).normalized; uv[i] = new Vector2(it.uvs[i * 2], it.uvs[i * 2 + 1]); }
        Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
        var m = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath); bool fresh = m == null; if (fresh) m = new Mesh();
        m.Clear(); m.name = "Payphone pink (CC0 Poly Haven, Claude)"; m.indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        m.SetVertices(v); m.SetNormals(nn); m.SetUVs(0, uv); m.SetTriangles(it.triangles, 0); m.RecalculateBounds(); m.RecalculateTangents();
        if (fresh) AssetDatabase.CreateAsset(m, MeshPath); else EditorUtility.SetDirty(m);
        rep.Add("mesh: " + it.triangles.Length / 3 + " triangles (source " + it.tris_source + "), " + n + " vertices, size " + string.Join(" x ", it.size_m) + " m; " + it.source);
        return m;
    }

    static Texture2D Tex(string file, bool srgb, bool normal)
    {
        string p = TexDir + file; var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default; ti.sRGBTexture = srgb && !normal;
        ti.maxTextureSize = 1024; ti.mipmapEnabled = true; ti.alphaIsTransparency = false; ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static Material BuildMaterial(List<string> rep)
    {
        AssetDatabase.Refresh();
        var alb = Tex("payphone-pink-albedo-20261018.png", true, false);
        var ms = Tex("payphone-pink-metallic-smoothness-20261018.png", false, false);
        var nor = Tex("payphone-pink-normal-20261018.png", false, true);
        var occ = Tex("payphone-pink-occlusion-20261018.png", false, false);
        var m = AssetDatabase.LoadAssetAtPath<Material>(MatPath); bool fresh = m == null;
        if (fresh) { m = new Material(Shader.Find("Standard")) { name = "Payphone Pink" }; AssetDatabase.CreateAsset(m, MatPath); }
        m.shader = Shader.Find("Standard");
        m.SetTexture("_MainTex", alb); m.SetColor("_Color", Color.white);
        m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICGLOSSMAP"); m.SetFloat("_GlossMapScale", 1f);
        m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f);
        m.SetTexture("_OcclusionMap", occ); m.SetFloat("_OcclusionStrength", 1f);
        m.enableInstancing = true; EditorUtility.SetDirty(m);
        rep.Add("material: Standard (albedo, metallic-smoothness, normal, occlusion), instanced: " + MatPath);
        return m;
    }

    [MenuItem("Astra/Claude/39r Restore scene from before round 39")]
    public static void Restore()
    {
        var path = EditorSceneManager.GetActiveScene().path; File.Copy(Backup, path, true); AssetDatabase.ImportAsset(path); EditorSceneManager.OpenScene(path);
        File.WriteAllText(Path.Combine(Out, "restore.txt"), "restored " + path + "\n");
    }

    [MenuItem("Astra/Claude/39p Payphone photos")]
    public static void Photos()
    {
        Directory.CreateDirectory(Out); int k = 0;
        foreach (var booth in Object.FindObjectsOfType<Transform>().Where(t => t.Find("Payphone detail") != null).OrderBy(t => t.position.y).Take(3))
        {
            var ph = booth.Find("Payphone (CC0 pink)"); Transform show = ph != null && ph.gameObject.activeInHierarchy ? ph : booth.Find("Payphone detail");
            var rs = show.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); var target = bb.center;
            var fwd = booth.forward; k++;
            Shot("payphone " + k + " front", target + fwd * .75f * booth.lossyScale.y + Vector3.up * .08f, target, 50);
            Shot("payphone " + k + " angle", target + (fwd * .6f + booth.right * .45f) * booth.lossyScale.y + Vector3.up * .15f, target, 50);
        }
    }

    static void Shot(string name, Vector3 pos, Vector3 look, float fov)
    {
        var go = new GameObject("Claude photo camera") { hideFlags = HideFlags.DontSave };
        var cam = go.AddComponent<Camera>(); cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 2000;
        var pp = go.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform;
        var rt = new RenderTexture(1400, 1000, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1400, 1000, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1400, 1000), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
    }

    [MenuItem("Astra/Claude/39s Survey around the new payphones (read-only)")]
    public static void Survey()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ph in Object.FindObjectsOfType<Transform>().Where(t => t.name == "Payphone (CC0 pink)"))
        {
            var c = ph.GetComponent<Renderer>().bounds; sb.AppendLine("== " + ClaudeCloudInspect.Path(ph) + " bounds " + c.min + " .. " + c.max);
            foreach (var r in Object.FindObjectsOfType<Renderer>().Where(r => r.transform != ph && r.bounds.Intersects(new Bounds(c.center, c.size + Vector3.one * .6f))))
                sb.AppendLine("   " + ClaudeCloudInspect.Path(r.transform) + " | " + r.GetType().Name + " | mat " + (r.sharedMaterial ? r.sharedMaterial.name + " / " + r.sharedMaterial.shader.name : "-") + " | size " + r.bounds.size);
        }
        File.WriteAllText(Path.Combine(Out, "survey.txt"), sb.ToString());
    }

}
