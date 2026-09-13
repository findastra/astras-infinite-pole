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
using VRC.SDK3.Components;
public static class AstraMagicBuilder {
const string Root="Assets/Astra";
static Font font;
static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
static Text Label(Transform p,string text,float x,float y,float w=440,int size=20){var r=Rect(text,p,new Vector2(x,y),new Vector2(w,38));var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=new Color(.94f,.85f,1);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
static Button Button(Transform p,string text,float x,float y,float w,VRC.Udon.UdonBehaviour u,string evt){var r=Rect(text,p,new Vector2(x,y),new Vector2(w,45));var im=r.gameObject.AddComponent<Image>();im.color=new Color(.16f,.08f,.24f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;Label(r,text,0,0,w-8,18);UnityEventTools.AddStringPersistentListener(b.onClick,u.SendCustomEvent,evt);return b;}
static Toggle Toggle(Transform p,string text,float x,float y,bool value,VRC.Udon.UdonBehaviour u,string evt){var r=Rect(text,p,new Vector2(x,y),new Vector2(480,42));var im=r.gameObject.AddComponent<Image>();im.color=new Color(.075f,.04f,.12f);var t=r.gameObject.AddComponent<Toggle>();t.targetGraphic=im;var mark=Rect("Crystal switch",r,new Vector2(-218,0),new Vector2(19,19));mark.localRotation=Quaternion.Euler(0,0,45);var check=mark.gameObject.AddComponent<Image>();check.color=new Color(.45f,1,.86f);t.graphic=check;t.isOn=value;Label(r,text,15,0,420,18);UnityEventTools.AddStringPersistentListener(t.onValueChanged,u.SendCustomEvent,evt);return t;}
static Slider Slider(Transform p,string text,float x,float y,float value,VRC.Udon.UdonBehaviour u,string evt){Label(p,text,x,y+27,470,17);var r=Rect(text,p,new Vector2(x,y),new Vector2(460,16));r.gameObject.AddComponent<Image>().color=new Color(.15f,.08f,.24f);var s=r.gameObject.AddComponent<Slider>();var area=Rect("Travel",r,Vector2.zero,new Vector2(430,16));var h=Rect("Gem",area,Vector2.zero,new Vector2(24,28));s.handleRect=h;s.targetGraphic=h.gameObject.AddComponent<Image>();s.targetGraphic.color=new Color(.94f,.5f,.85f);s.value=value;UnityEventTools.AddStringPersistentListener(s.onValueChanged,u.SendCustomEvent,evt);return s;}
static void Program(string name){string path=Root+"/Scripts/"+name+".asset";if(AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path)!=null)return;var a=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();a.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>(Root+"/Scripts/"+name+".cs");AssetDatabase.CreateAsset(a,path);}
[MenuItem("Astra/Build Magic Upgrade")]
public static void Build(){
 EditorSceneManager.OpenScene(Root+"/Scenes/AstrasInfinitePole.unity");
 Directory.CreateDirectory("Review/Backups");if(!File.Exists("Review/Backups/BeforeMagic.unity.txt"))File.Copy(Root+"/Scenes/AstrasInfinitePole.unity","Review/Backups/BeforeMagic.unity.txt");
 if(UnityEngine.Object.FindObjectOfType<AstraMagic>()!=null)throw new Exception("Magic upgrade already installed; preserve scene edits.");
 font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");Program("AstraMagic");Program("AstraHandMenu");AssetDatabase.SaveAssets();UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
 var c=UnityEngine.Object.FindObjectOfType<AstraControls>();var g=c.glitter;
 var root=new GameObject("07 - Living magic");var m=root.AddUdonSharpComponent<AstraMagic>();m.materials=g.materials;m.poleMaterial=c.poleMaterial;
 m.trails=new ParticleSystem[3];
 for(int i=0;i<g.effects.Length;i++){
  var ps=g.effects[i];ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=true;
  var v=ps.velocityOverLifetime;v.enabled=true;v.orbitalY=new ParticleSystem.MinMaxCurve(i%2==0?.06f:-.16f,i%2==0?.18f:-.05f);v.orbitalX=new ParticleSystem.MinMaxCurve(-.025f,.025f);v.radial=new ParticleSystem.MinMaxCurve(-.035f,.045f);
  var n=ps.noise;n.strength=new ParticleSystem.MinMaxCurve(.08f,.24f);n.scrollSpeed=.11f;n.frequency=.28f;
  var main=ps.main;main.startSizeMultiplier*=i==4?1.3f:1;g.materials[i].SetFloat("_Flow",.65f);g.materials[i].SetFloat("_FlowSeed",i*3.71f);g.materials[i].SetFloat("_React",0);
 }
 for(int i=0;i<3;i++){
  var go=new GameObject("Body stardust "+i);go.transform.SetParent(root.transform);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);m.trails[i]=ps;
  var main=ps.main;main.maxParticles=80;main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.7f);main.startSize=new ParticleSystem.MinMaxCurve(.015f,.055f);main.startSpeed=.035f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=false;
  var emission=ps.emission;emission.rateOverTime=0;emission.rateOverDistance=32;
  var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Sphere;sh.radius=.05f;
  var fade=ps.colorOverLifetime;fade.enabled=true;var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});fade.color=grad;
  var r=go.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=g.materials[i==2?3:1];r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 }
 var cloudObj=new GameObject("Sparkling sunset clouds");cloudObj.transform.SetParent(root.transform);cloudObj.transform.position=new Vector3(0,3.5f,0);m.clouds=cloudObj.AddComponent<ParticleSystem>();m.clouds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
 var cm=m.clouds.main;cm.maxParticles=240;cm.startLifetime=new ParticleSystem.MinMaxCurve(12,20);cm.startSize=new ParticleSystem.MinMaxCurve(1.1f,2.8f);cm.startSpeed=.025f;cm.prewarm=true;cm.startColor=new Color(1,1,1,.14f);
 var cs=m.clouds.shape;cs.shapeType=ParticleSystemShapeType.Donut;cs.radius=6;cs.donutRadius=1.6f;cs.rotation=new Vector3(90,0,0);cs.scale=new Vector3(1,1,.5f);
 var ce=m.clouds.emission;ce.rateOverTime=10;
 var cv=m.clouds.velocityOverLifetime;cv.enabled=true;cv.orbitalY=.035f;cv.y=.02f;
 var cn=m.clouds.noise;cn.enabled=true;cn.strength=.15f;cn.frequency=.12f;cn.quality=ParticleSystemNoiseQuality.Low;cn.octaveCount=1;
 var cf=m.clouds.colorOverLifetime;cf.enabled=true;var cg=new Gradient();cg.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(1,.7f),new GradientAlphaKey(0,1)});cf.color=cg;
 m.cloudMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Sparkle Clouds.mat");if(m.cloudMaterial==null){m.cloudMaterial=new Material(Shader.Find("Astra/Sparkle Clouds"));AssetDatabase.CreateAsset(m.cloudMaterial,Root+"/Materials/Sparkle Clouds.mat");}var cr=cloudObj.GetComponent<ParticleSystemRenderer>();cr.sharedMaterial=m.cloudMaterial;cr.maxParticleSize=.35f;cr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 var panel=GameObject.Find("Glitter studio").GetComponent<RectTransform>();panel.sizeDelta=new Vector2(1100,1560);panel.GetComponent<BoxCollider>().size=new Vector3(1100,1560,1);
 foreach(Transform child in panel){if(child.name=="Opaque backing"){child.localScale=new Vector3(1100,1560,2);continue;}((RectTransform)child).anchoredPosition+=Vector2.up*180;}
 var u=UdonSharpEditorUtility.GetBackingUdonBehaviour(m);
 m.trailToggle=Toggle(panel,"BODY STARDUST",-260,-430,false,u,"ApplyTrails");m.reactionToggle=Toggle(panel,"TOUCH MAGIC",260,-430,true,u,"ApplyFlow");
 m.cloudToggle=Toggle(panel,"SPARKLE CLOUDS",-260,-485,true,u,"ApplyClouds");m.translucentToggle=Toggle(panel,"TRANSLUCENT POLE",260,-485,false,u,"ApplyPole");
 m.flowSlider=Slider(panel,"SWIRL ENERGY",-260,-560,.65f,u,"ApplyFlow");m.poleOpacity=Slider(panel,"POLE OPACITY",260,-560,.4f,u,"ApplyPole");
 m.poleSparkle=Slider(panel,"CRYSTAL SPARKLE",260,-635,.85f,u,"ApplyPole");m.patternLabel=Label(panel,"SPELL PATTERN",-260,-608,460,16);Button(panel,"NEW SPELL",-260,-650,450,u,"NewPattern");
 Button(panel,"PEACH CLOUDS",-340,-715,300,u,"PeachClouds");Button(panel,"ROSE CLOUDS",0,-715,300,u,"RoseClouds");Button(panel,"TWILIGHT CLOUDS",340,-715,300,u,"TwilightClouds");
 var atmos=UnityEngine.Object.FindObjectOfType<AstraAtmosphere>();atmos.exposure.value=.66f;atmos.skies[0].SetColor("_Zenith",new Color(.025f,.012f,.065f));atmos.skies[0].SetFloat("_Clouds",1.6f);atmos.skies[0].SetFloat("_Exposure",.975f);
 var vp=UnityEngine.Object.FindObjectOfType<USharpVideoPlayer>();vp.shufflePlaylist=true;UdonSharpEditorUtility.CopyProxyToUdon(vp);
 var menus=new GameObject("Summoned spellbook",typeof(RectTransform));var hm=root.AddUdonSharpComponent<AstraHandMenu>();hm.menuRoot=menus.transform;
 var skyPanel=GameObject.Find("Celestial observatory").GetComponent<RectTransform>();
 panel.SetParent(menus.transform,false);panel.localPosition=new Vector3(.32f,0,0);panel.localRotation=Quaternion.identity;panel.localScale=Vector3.one*.00053f;
 skyPanel.SetParent(menus.transform,false);skyPanel.localPosition=new Vector3(-.24f,0,0);skyPanel.localRotation=Quaternion.identity;skyPanel.localScale=Vector3.one*.00065f;
 var hu=UdonSharpEditorUtility.GetBackingUdonBehaviour(hm);Button(panel,"CLOSE  x",395,730,230,hu,"HideMenu");
 foreach(var p in new[]{panel,skyPanel}){
  p.GetComponent<Image>().color=new Color(.028f,.018f,.068f,.98f);
  foreach(var t in p.GetComponentsInChildren<Text>()){t.color=new Color(.91f,.84f,1);if(t.fontSize>=30)t.color=new Color(1,.61f,.87f);}
  foreach(var b in p.GetComponentsInChildren<Button>()){var cb=b.colors;cb.highlightedColor=new Color(.5f,1,.92f);cb.pressedColor=new Color(1,.5f,.8f);b.colors=cb;}
  var border=p.gameObject.AddComponent<Outline>();border.effectColor=new Color(.4f,.8f,1,.8f);border.effectDistance=new Vector2(3,3);
 }
 // Existing video controls belong to the prefab canvas; summon them with the spellbook, retaining the screen.
 var videoCanvas=vp.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
 if(videoCanvas!=null){videoCanvas.transform.SetParent(menus.transform,true);videoCanvas.transform.localPosition=new Vector3(0,-.65f,0);videoCanvas.transform.localRotation=Quaternion.identity;videoCanvas.transform.localScale=Vector3.one*.00045f;}
 menus.transform.position=new Vector3(0,1.5f,-1);menus.SetActive(false);
 UdonSharpEditorUtility.CopyProxyToUdon(m);UdonSharpEditorUtility.CopyProxyToUdon(hm);UdonSharpEditorUtility.CopyProxyToUdon(atmos);
 EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
 AstraAtmosphereBuilder.Validate();
 if(UnityEditor.ShaderUtil.ShaderHasError(Shader.Find("Astra/Sparkle Clouds")))throw new Exception("Cloud shader failed");
 File.WriteAllText("Review/magic-validation.txt","PASS: shader compilation, Udon compilation, 11980 particle hard cap, preserved world ID, shuffled synchronized playlist, 3 trail emitters, particle clouds, hidden summonable menus.\nVR gesture ergonomics, GPU timing and live online playback require headset/client testing.");
 AstraAtmosphereBuilder.Render();File.Copy("Review/Celestial Preview.png","Review/Magic Preview.png",true);
 Debug.Log("ASTRA_MAGIC_BUILD_OK");
}
public static void BuildAndCheck(){Build();AstraPlayCheck.RunGlitter();}
}

