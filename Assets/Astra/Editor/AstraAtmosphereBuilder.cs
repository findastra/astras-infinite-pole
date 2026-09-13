using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;
using UdonSharp.Video;
using VRC.Core;
using VRC.SDKBase;
using VRC.SDKBase.Editor;
using VRC.SDK3.Components;

public static class AstraAtmosphereBuilder
{
    const string Root="Assets/Astra";
    const string ScenePath=Root+"/Scenes/AstrasInfinitePole.unity";
    static Font font;
    public static void BuildAndCheck(){Build();AstraPlayCheck.RunGlitter();}
    public static void BuildAndUpload(){Build();AstraReleaseCheck.UpdateWorld();}
    [MenuItem("Astra/Build Celestial Upgrade")]
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory("Review/Backups");
        if(!File.Exists("Review/Backups/BeforeCelestial.unity.txt"))File.Copy(ScenePath,"Review/Backups/BeforeCelestial.unity.txt");
        var prior=GameObject.Find("06 - Celestial atmosphere");
        if(prior!=null)UnityEngine.Object.DestroyImmediate(prior);
        var c=UnityEngine.Object.FindObjectOfType<AstraControls>();
        var g=UnityEngine.Object.FindObjectOfType<AstraGlitterControls>();
        var root=new GameObject("06 - Celestial atmosphere");root.transform.SetParent(c.transform.parent,false);
        Directory.CreateDirectory(Root+"/Materials/Skies");
        var program=AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(Root+"/Scripts/AstraAtmosphere.asset");
        if(program==null){program=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();program.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>(Root+"/Scripts/AstraAtmosphere.cs");AssetDatabase.CreateAsset(program,Root+"/Scripts/AstraAtmosphere.asset");}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        var a=root.AddUdonSharpComponent<AstraAtmosphere>();a.glitter=g.materials;
        string[] names={"Velvet Nebula","Arctic Aurora","Rose Dusk","Midnight Stars"};
        Color[] colorsA={new Color(.25f,.055f,.48f),new Color(.015f,.35f,.27f),new Color(.43f,.095f,.19f),new Color(.045f,.05f,.14f)};
        Color[] colorsB={new Color(.035f,.3f,.38f),new Color(.12f,.075f,.42f),new Color(.4f,.21f,.065f),new Color(.035f,.07f,.16f)};
        a.skies=new Material[7];
        for(int i=0;i<4;i++){
            string path=Root+"/Materials/Skies/"+names[i]+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Astra/Celestial Sky"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_NebulaA",colorsA[i]);mat.SetColor("_NebulaB",colorsB[i]);
            mat.SetColor("_Horizon",colorsA[i]*.5f);mat.SetFloat("_Exposure",.8f);
            mat.SetFloat("_Clouds",i==3?.3f:1);mat.SetFloat("_Stars",i==3?1.6f:1);
            mat.SetFloat("_Seed",i*12.3f);a.skies[i]=mat;
        }
        a.skies[4]=c.backgroundMaterial;
        string[] photos={"belfast_sunset_puresky_2k","kloppenheim_02_puresky_2k"};
        for(int i=0;i<photos.Length;i++){
            string imagePath="Assets/ThirdParty/PolyHavenSkies/"+photos[i]+".hdr";
            var ti=(TextureImporter)AssetImporter.GetAtPath(imagePath);ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;ti.textureCompression=TextureImporterCompression.Compressed;ti.SaveAndReimport();
            string path=Root+"/Materials/Skies/"+photos[i]+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath));mat.SetFloat("_Exposure",.8f);a.skies[i+5]=mat;
        }
        var envelope=GameObject.Find("Void cylinder - visual envelope");if(envelope!=null)envelope.GetComponent<Renderer>().enabled=false;
        RenderSettings.skybox=a.skies[0];
        for(int i=0;i<g.materials.Length;i++){
            g.materials[i].SetFloat("_DriftSpeed",.004f);g.materials[i].SetFloat("_MorphSpeed",.007f+i*.0004f);g.materials[i].SetFloat("_Phase",i*.137f);
        }
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var panel=Rect("Celestial observatory",root.transform,Vector2.zero,new Vector2(780,1020));
        panel.position=new Vector3(-1.85f,1.45f,.15f);panel.rotation=Quaternion.Euler(0,-42,0);panel.localScale=Vector3.one*.0015f;
        panel.gameObject.AddComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        panel.gameObject.AddComponent<GraphicRaycaster>();panel.gameObject.AddComponent<VRCUiShape>();panel.gameObject.AddComponent<BoxCollider>().size=new Vector3(780,1020,1);
        panel.gameObject.AddComponent<Image>().color=new Color(.015f,.02f,.045f);
        var u=UdonSharpEditorUtility.GetBackingUdonBehaviour(a);
        Label(panel,"CELESTIAL OBSERVATORY",435,30);a.skyLabel=Label(panel,"VELVET NEBULA",375,22);
        string[] events={"Nebula","Aurora","RoseDusk","Midnight","QuietVoid","SunsetSky","MoonlitSky"};
        string[] labels={"VELVET NEBULA","ARCTIC AURORA","ROSE DUSK","MIDNIGHT STARS","QUIET VOID","BELFAST SUNSET","MOONLIT SKY"};
        for(int i=0;i<7;i++)Button(panel,labels[i],295-i*64,u,events[i]);
        Label(panel,"SKY BRIGHTNESS",-160,18);
        var sr=Rect("Sky brightness",panel,new Vector2(0,-210),new Vector2(650,20));
        sr.gameObject.AddComponent<Image>().color=new Color(.09f,.11f,.2f);a.exposure=sr.gameObject.AddComponent<Slider>();
        var area=Rect("Slide area",sr,Vector2.zero,new Vector2(620,20));var h=Rect("Handle",area,Vector2.zero,new Vector2(25,32));
        a.exposure.handleRect=h;a.exposure.targetGraphic=h.gameObject.AddComponent<Image>();a.exposure.value=.52f;
        UnityEventTools.AddStringPersistentListener(a.exposure.onValueChanged,u.SendCustomEvent,"ApplyExposure");
        a.motionLabel=Button(panel,"SLOW EVOLUTION  /  ON",-295,u,"ToggleEvolution").GetComponentInChildren<Text>();
        Label(panel,"Your sky and sparkle settings are personal.",-390,17);
        Label(panel,"Sunset & moonlit panoramas: Poly Haven / CC0",-440,15);
        var videoPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/USharpVideo/USharpVideo.prefab");
        if(videoPrefab==null)throw new Exception("Missing USharpVideo prefab");
        var video=(GameObject)PrefabUtility.InstantiatePrefab(videoPrefab);video.name="Cinema - synchronized playlist";video.transform.SetParent(root.transform,false);
        video.transform.localPosition=new Vector3(0,3,8);video.transform.localRotation=Quaternion.identity;video.transform.localScale=Vector3.one*.7f;
        var player=video.GetComponentInChildren<USharpVideoPlayer>(true);if(player==null)throw new Exception("Missing video controller");
        player.playlist=ReadPlaylist();player.loopPlaylist=true;player.shufflePlaylist=false;
        var serialized=new SerializedObject(player);serialized.FindProperty("defaultVolume").floatValue=.25f;serialized.FindProperty("defaultUnlocked").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach(var audio in video.GetComponentsInChildren<AudioSource>(true)){audio.spatialBlend=0;audio.playOnAwake=false;}
        foreach(var r in video.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
        foreach(var b in video.GetComponentsInChildren<UdonSharpBehaviour>(true))UdonSharpEditorUtility.CopyProxyToUdon(b);
        UdonSharpEditorUtility.CopyProxyToUdon(a);
        c.music.Stop();c.music.playOnAwake=false;
        c.musicLabel.text="AMBIENT PREVIEW / OFF";
        VRCSettings.ForceNoVR=false;
        EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
        Validate();Render();
        Debug.Log("ASTRA_CELESTIAL_BUILD_OK");
    }
    public static VRCUrl[] ReadPlaylist(){
        if(!File.Exists("Review/playlist-urls.txt"))return new VRCUrl[0];
        return File.ReadAllLines("Review/playlist-urls.txt").Select(s=>s.Trim()).Where(s=>s.StartsWith("https://")).Select(s=>new VRCUrl(s)).ToArray();
    }
    static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    static Text Label(Transform parent,string text,float y,int size){var r=Rect(text,parent,new Vector2(0,y),new Vector2(710,50));var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=new Color(.82f,.87f,1);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
    static Button Button(Transform parent,string text,float y,VRC.Udon.UdonBehaviour u,string evt){var r=Rect(text,parent,new Vector2(0,y),new Vector2(670,50));var im=r.gameObject.AddComponent<Image>();im.color=new Color(.055f,.085f,.14f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;Label(r,text,0,20);UnityEventTools.AddStringPersistentListener(b.onClick,u.SendCustomEvent,evt);return b;}
    public static void Validate(){
        AstraGlitterBuilder.Validate();
        var a=UnityEngine.Object.FindObjectOfType<AstraAtmosphere>();var p=UnityEngine.Object.FindObjectOfType<USharpVideoPlayer>();
        if(a==null||a.skies.Length!=7||a.skies.Any(s=>s==null))throw new Exception("Missing sky presets");
        if(p==null||!p.loopPlaylist)throw new Exception("Video player missing or playlist loop disabled");
        var pm=UnityEngine.Object.FindObjectOfType<PipelineManager>();if(pm.blueprintId!="wrld_0d424078-5852-497d-adc6-c97436052355")throw new Exception("World identity changed");
        foreach(var go in UnityEngine.Object.FindObjectsOfType<GameObject>())if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)>0)throw new Exception("Missing script: "+go.name);
        foreach(var shader in new[]{"Astra/Celestial Sky","Astra/Infinite Pole","Astra/Glitter Gradient"})if(UnityEditor.ShaderUtil.ShaderHasError(Shader.Find(shader)))throw new Exception("Shader failed: "+shader);
        File.WriteAllText("Review/celestial-validation.txt","PASS: seven sky options, 13 evolving effects, preserved world identity, colliders and references, video player with looping ordered playlist.\nPlaylist entries: "+p.playlist.Length+"\nTarget: "+EditorUserBuildSettings.activeBuildTarget+"\nActual online media playback, server processing, late-join sync and headset comfort remain to be verified.\n");
    }
    public static void Render(){
        foreach(var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())ps.Simulate(9,true,true);
        var go=new GameObject("Celestial preview camera");var cam=go.AddComponent<Camera>();cam.transform.position=new Vector3(0,1.65f,-3.6f);cam.fieldOfView=85;cam.nearClipPlane=.02f;cam.farClipPlane=350;
        var rt=new RenderTexture(1920,1200,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;
        var tex=new Texture2D(1920,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1200),0,0);tex.Apply();File.WriteAllBytes("Review/Celestial Preview.png",tex.EncodeToPNG());
        RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);
    }
}
