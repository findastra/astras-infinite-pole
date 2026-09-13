using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDK3.Components;
using VRC.Udon;

public static class AstraGlitterBuilder
{
    const string Root="Assets/Astra";
    const string ScenePath=Root+"/Scenes/AstrasInfinitePole.unity";
    static Font font;
    static readonly string[] Names={"Fine glitter dust","Starbursts","Tiny stars","Diamond shards","Soft bokeh","Fairy lights","Falling shimmer","Rising sparks","Confetti flakes","Orbiting glints","Floating hearts","Snow crystals","Magic wisps"};
    static readonly string[] Textures={"circle_05","star_04","star_01","star_01","circle_01","flare_01","spark_03","spark_01","star_01","star_06","symbol_01","star_09","magic_03"};
    static readonly int[] Caps={3000,1200,600,900,300,400,1600,1000,800,500,300,650,250};
    static readonly float[] Rates={260,90,40,70,20,25,130,70,60,40,20,50,15};
    static readonly float[] Sizes={0.035f,0.14f,0.09f,0.055f,0.2f,0.08f,0.11f,0.07f,0.045f,0.12f,0.13f,0.09f,0.32f};
    [MenuItem("Astra/Upgrade Glitter")]
    public static void Upgrade()
    {
        EditorSceneManager.OpenScene(ScenePath);
        if(GameObject.Find("05 - Glitter collection")!=null)throw new Exception("Glitter is already installed; preserve scene edits.");
        if(!File.Exists(Root+"/Scenes/AstrasInfinitePole_Lean.unity"))
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/AstrasInfinitePole_Lean.unity",true);
        Directory.CreateDirectory(Root+"/Materials/Glitter");
        foreach(string path in Directory.GetFiles("Assets/ThirdParty/KenneyParticles/Textures","*.png"))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType=TextureImporterType.Default;ti.alphaIsTransparency=true;ti.mipmapEnabled=true;
            ti.maxTextureSize=256;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Compressed;ti.SaveAndReimport();
        }
        var program=AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(Root+"/Scripts/AstraGlitterControls.asset");
        if(program==null){program=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
        program.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>(Root+"/Scripts/AstraGlitterControls.cs");
        AssetDatabase.CreateAsset(program,Root+"/Scripts/AstraGlitterControls.asset");AssetDatabase.SaveAssets();}
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        var c=UnityEngine.Object.FindObjectOfType<AstraControls>();
        c.sparkles.gameObject.SetActive(false);
        GameObject.Find("Personal comfort panel").SetActive(false);
        var root=new GameObject("05 - Glitter collection");root.transform.SetParent(c.transform.parent,false);
        var g=root.AddUdonSharpComponent<AstraGlitterControls>();
        g.effects=new ParticleSystem[Names.Length];g.materials=new Material[Names.Length];g.maximumRates=Rates;
        for(int i=0;i<Names.Length;i++)
        {
            string matPath=Root+"/Materials/Glitter/"+Names[i]+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(Shader.Find("Astra/Glitter Gradient"));AssetDatabase.CreateAsset(material,matPath);}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/KenneyParticles/Textures/"+Textures[i]+".png");
            material.SetFloat("_Shape",i==3?1:i==8?2:0);
            material.SetColor("_ColorA",Color.HSVToRGB(0.74f,0.72f,1));material.SetColor("_ColorB",Color.HSVToRGB(0.49f,0.72f,1));
            material.SetFloat("_Brightness",1.3f);material.SetFloat("_Twinkle",0.55f);
            g.materials[i]=material;
            var go=new GameObject(Names[i]);go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(0,2,0);
            var ps=go.AddComponent<ParticleSystem>();g.effects[i]=ps;
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(101+i*71);
            var main=ps.main;main.loop=true;main.duration=12;main.prewarm=true;main.playOnAwake=true;
            main.startLifetime=new ParticleSystem.MinMaxCurve(6,10);main.maxParticles=Caps[i];main.startSpeed=new ParticleSystem.MinMaxCurve(0.015f,0.08f);
            main.startSize=new ParticleSystem.MinMaxCurve(Sizes[i]*0.45f,Sizes[i]);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            main.simulationSpace=ParticleSystemSimulationSpace.Local;main.startColor=Color.white;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(13,10,13);
            var emission=ps.emission;emission.rateOverTime=Rates[i]*0.65f;
            var noise=ps.noise;noise.enabled=true;noise.quality=ParticleSystemNoiseQuality.Low;noise.octaveCount=1;noise.strength=0.11f;noise.frequency=0.22f;noise.scrollSpeed=0.05f;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
            if(i==6)velocity.y=-0.32f;else if(i==7)velocity.y=0.28f;else velocity.y=0.025f;
            if(i==9) {velocity.orbitalY=0.12f;shape.scale=new Vector3(4,9,4);}
            if(i==8) {var rot=ps.rotationOverLifetime;rot.enabled=true;rot.z=0.55f;velocity.y=-0.12f;}
            if(i==11)velocity.y=-0.16f;
            var fade=ps.colorOverLifetime;fade.enabled=true;
            var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(i==4||i==12?0.28f:0.8f,0.16f),new GradientAlphaKey(0.65f,0.75f),new GradientAlphaKey(0,1)});fade.color=grad;
            var pr=go.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=material;pr.shadowCastingMode=ShadowCastingMode.Off;pr.receiveShadows=false;pr.lightProbeUsage=LightProbeUsage.Off;pr.reflectionProbeUsage=ReflectionProbeUsage.Off;pr.maxParticleSize=0.045f;
        }
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");BuildPanel(root.transform,c,g);
        c.glitter=g;
        UdonSharpEditorUtility.CopyProxyToUdon(g);UdonSharpEditorUtility.CopyProxyToUdon(c);
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        AstraDesktopTest.Prepare();AssetDatabase.SaveAssets();
        Validate();Render();Debug.Log("ASTRA_GLITTER_UPGRADE_OK");
    }
    static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
    {var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
    static Text Label(Transform parent,string text,float x,float y,int fontSize=22,float width=460)
    {var r=Rect(text,parent,new Vector2(x,y),new Vector2(width,Mathf.Max(36,fontSize*1.6f)));var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=fontSize;t.alignment=TextAnchor.MiddleLeft;t.color=new Color(0.87f,0.84f,0.95f);t.raycastTarget=false;return t;}
    static Slider Slide(Transform parent,string name,float y,float value,UdonBehaviour udon,string evt)
    {
        Label(parent,name,-267,y+30,20);
        var r=Rect(name+" slider",parent,new Vector2(-267,y),new Vector2(460,22));var image=r.gameObject.AddComponent<Image>();image.color=new Color(0.1f,0.09f,0.17f);
        var slider=r.gameObject.AddComponent<Slider>();var area=Rect("Handle area",r,Vector2.zero,new Vector2(434,22));var h=Rect("Handle",area,Vector2.zero,new Vector2(26,32));var hi=h.gameObject.AddComponent<Image>();hi.color=new Color(0.72f,0.6f,1);
        slider.handleRect=h;slider.targetGraphic=hi;slider.minValue=0;slider.maxValue=1;slider.value=value;
        var nav=slider.navigation;nav.mode=Navigation.Mode.None;slider.navigation=nav;
        UnityEventTools.AddStringPersistentListener(slider.onValueChanged,udon.SendCustomEvent,evt);return slider;
    }
    static Button Button(Transform parent,string label,float x,float y,float width,UdonBehaviour udon,string evt)
    {
        var r=Rect(label,parent,new Vector2(x,y),new Vector2(width,52));var im=r.gameObject.AddComponent<Image>();im.color=new Color(0.14f,0.1f,0.23f);
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var t=Label(r,label,0,0,19,width-20);t.alignment=TextAnchor.MiddleCenter;
        UnityEventTools.AddStringPersistentListener(b.onClick,udon.SendCustomEvent,evt);return b;
    }
    static void BuildPanel(Transform parent,AstraControls c,AstraGlitterControls g)
    {
        var r=Rect("Glitter studio",parent,Vector2.zero,new Vector2(1100,1160));r.position=new Vector3(1.9f,1.4f,0.15f);r.rotation=Quaternion.Euler(0,42,0);r.localScale=Vector3.one*0.0013f;
        r.gameObject.AddComponent<Canvas>().renderMode=RenderMode.WorldSpace;r.gameObject.AddComponent<GraphicRaycaster>();r.gameObject.AddComponent<VRCUiShape>();r.gameObject.AddComponent<BoxCollider>().size=new Vector3(1100,1160,1);r.gameObject.AddComponent<Image>().color=new Color(0.022f,0.016f,0.043f);
        Label(r,"ASTRA'S  /  GLITTER STUDIO",0,520,34,1000);Label(r,"INFINITE POLE     -     YOUR PERSONAL LOOK",0,470,19,1000);
        var udon=UdonSharpEditorUtility.GetBackingUdonBehaviour(g);var old=UdonSharpEditorUtility.GetBackingUdonBehaviour(c);
        g.densitySlider=Slide(r,"DENSITY",370,0.65f,udon,"ApplyDensity");
        g.densityLabel=r.Find("DENSITY").GetComponent<Text>();g.densityLabel.text="DENSITY  /  65%";
        c.sparkleSlider=g.densitySlider;
        g.hueA=Slide(r,"GRADIENT - COLOR A",270,0.74f,udon,"ApplyLook");g.hueB=Slide(r,"GRADIENT - COLOR B",180,0.49f,udon,"ApplyLook");
        g.saturation=Slide(r,"COLOR SATURATION",90,0.72f,udon,"ApplyLook");g.brightness=Slide(r,"GLITTER BRIGHTNESS",0,0.65f,udon,"ApplyLook");g.twinkle=Slide(r,"TWINKLE",-90,0.55f,udon,"ApplyLook");
        g.swatchA=Rect("Color A swatch",r,new Vector2(-62,300),new Vector2(30,22)).gameObject.AddComponent<Image>();g.swatchA.color=Color.HSVToRGB(0.74f,0.72f,1);g.swatchA.raycastTarget=false;
        g.swatchB=Rect("Color B swatch",r,new Vector2(-62,210),new Vector2(30,22)).gameObject.AddComponent<Image>();g.swatchB.color=Color.HSVToRGB(0.49f,0.72f,1);g.swatchB.raycastTarget=false;
        c.backgroundSlider=Slide(r,"BACKGROUND",-210,0,old,"SetBackground");c.volumeSlider=Slide(r,"MUSIC VOLUME",-300,0.4f,old,"SetVolume");
        c.musicLabel=Button(r,"MUSIC  /  OFF",-267,-390,460,old,"ToggleMusic").GetComponentInChildren<Text>();
        g.effectToggles=new Toggle[Names.Length];
        for(int i=0;i<Names.Length;i++)
        {
            float y=395-i*48;var tr=Rect(Names[i]+" toggle",r,new Vector2(277,y),new Vector2(470,40));
            var toggle=tr.gameObject.AddComponent<Toggle>();var bg=Rect("Box",tr,new Vector2(-211,0),new Vector2(30,30)).gameObject.AddComponent<Image>();bg.color=new Color(0.14f,0.11f,0.24f);
            var check=Rect("On",bg.transform,Vector2.zero,new Vector2(20,20)).gameObject.AddComponent<Image>();check.color=new Color(0.5f,0.9f,0.88f);
            toggle.targetGraphic=bg;toggle.graphic=check;toggle.isOn=true;Label(tr,Names[i],20,0,22,395);
            UnityEventTools.AddStringPersistentListener(toggle.onValueChanged,udon.SendCustomEvent,"ApplyDensity");g.effectToggles[i]=toggle;
        }
        Button(r,"ALL ON",151,-290,212,udon,"AllOn");Button(r,"ALL OFF",386,-290,212,udon,"AllOff");
        Button(r,"LOW",107,-380,133,udon,"Low");Button(r,"LUSH",273,-380,133,udon,"Lush");Button(r,"MAX",439,-380,133,udon,"Maximum");
        string[] presets={"AURORA","ROSE GOLD","ICE","SUNSET"};string[] methods={"Aurora","RoseGold","Ice","Sunset"};
        for(int i=0;i<4;i++)Button(r,presets[i],-390+i*260,-500,235,udon,methods[i]);
        Label(r,"Changes are local to you. LOW reduces density for VR comfort.",0,-550,18,1000);
    }
    public static void Validate()
    {
        var g=UnityEngine.Object.FindObjectOfType<AstraGlitterControls>();if(g==null||g.effects.Length!=13)throw new Exception("Expected 13 effects");
        int cap=0;for(int i=0;i<g.effects.Length;i++){if(g.effects[i]==null||g.materials[i]==null||g.effectToggles[i]==null)throw new Exception("Missing glitter reference");cap+=g.effects[i].main.maxParticles;if(g.materials[i].shader.name!="Astra/Glitter Gradient")throw new Exception("Missing shader");}
        if(cap>12000)throw new Exception("Glitter budget exceeded: "+cap);
        AstraWorldBuilder.Verify();
        File.WriteAllText("Review/glitter-validation.txt","13 toggleable effects: PASS\nHard particle cap across all effects: "+cap+"\nAll material/control references: PASS\nTexture max size: 256, mipmapped\nDefault density: 65%; Low: 12%; Max: 100%\nVR frame time: not yet measured\n");
    }
    public static void Render()
    {
        foreach(var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())ps.Simulate(9,true,true);
        var go=new GameObject("Preview camera");var cam=go.AddComponent<Camera>();cam.transform.position=new Vector3(0,1.6f,-3.4f);cam.fieldOfView=75;cam.nearClipPlane=0.01f;cam.farClipPlane=350;
        var rt=new RenderTexture(1920,1200,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var tex=new Texture2D(1920,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1200),0,0);tex.Apply();File.WriteAllBytes("Review/Glitter Preview.png",tex.EncodeToPNG());
        RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);
    }
    public static void PolishAndOpen()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var panel=GameObject.Find("Glitter studio");
        foreach(var text in panel.GetComponentsInChildren<Text>())
            if(text.fontSize>=30)text.rectTransform.sizeDelta=new Vector2(text.rectTransform.sizeDelta.x,60);
        if(panel.transform.Find("Opaque backing")==null)
        {
            var mat=new Material(Shader.Find("Unlit/Color"));mat.color=new Color(0.022f,0.016f,0.043f);
            AssetDatabase.CreateAsset(mat,Root+"/Materials/Glitter Panel Backing.mat");
            var back=GameObject.CreatePrimitive(PrimitiveType.Cube);back.name="Opaque backing";back.transform.SetParent(panel.transform,false);back.transform.localPosition=new Vector3(0,0,3);back.transform.localScale=new Vector3(1100,1160,2);
            UnityEngine.Object.DestroyImmediate(back.GetComponent<Collider>());var renderer=back.GetComponent<Renderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        foreach(var toggle in panel.GetComponentsInChildren<Toggle>())
            if(toggle.GetComponent<Image>()==null)toggle.gameObject.AddComponent<Image>().color=new Color(0,0,0,0.001f);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Validate();Render();
        VRC.SDKBase.Editor.VRCSettings.NumClients=0;
        EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        Debug.Log("ASTRA_GLITTER_READY");
    }
}
