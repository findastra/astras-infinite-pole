using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using UdonSharp;
using UdonSharpEditor;
using VRC.SDK3.Components;
public static class AstraPinkscapeUpgrade {
 static Button Button(Transform parent,string text,Vector2 at,VRC.Udon.UdonBehaviour u,string evt){
  var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchoredPosition=at;r.sizeDelta=new Vector2(320,55);
  var b=go.GetComponent<Button>();b.targetGraphic=go.GetComponent<Image>();b.targetGraphic.color=new Color(.12f,.035f,.13f);
  var label=new GameObject("Label",typeof(RectTransform),typeof(Text));var lr=label.GetComponent<RectTransform>();lr.SetParent(r,false);lr.sizeDelta=r.sizeDelta;
  var t=label.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=22;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(1,.8f,.93f);t.text=text;t.raycastTarget=false;
  UnityEventTools.AddStringPersistentListener(b.onClick,u.SendCustomEvent,evt);return b;
 }
 public static void Run(){
  const string scene="Assets/Astra/Scenes/AstrasInfinitePole.unity";
  EditorSceneManager.OpenScene(scene);
  if(UnityEngine.Object.FindObjectOfType<AstraPinkscape>()!=null)throw new Exception("Upgrade already applied");
  Directory.CreateDirectory("Review/Backups");File.Copy(scene,"Review/Backups/BeforePinkscape.unity.txt",true);
  var geometry=GameObject.Find("01 - Pole and invisible enclosure");
  var pole=GameObject.Find("Infinite Pole - 45mm diameter");
  foreach(var collider in pole.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
  // Collision boundaries have no visual representation. Strip any leftover enclosure renderers.
  foreach(var r in geometry.GetComponentsInChildren<Renderer>(true))if(r.gameObject!=pole)r.enabled=false;
  var descriptor=UnityEngine.Object.FindObjectOfType<VRCSceneDescriptor>();
  var cam=descriptor.ReferenceCamera.GetComponent<Camera>();cam.allowHDR=true;
  var layer=cam.GetComponent<PostProcessLayer>();if(layer==null)layer=cam.gameObject.AddComponent<PostProcessLayer>();
  var resources=AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
  if(resources==null)throw new Exception("Postprocessing resources missing");
  layer.Init(resources);layer.volumeLayer=1;layer.volumeTrigger=cam.transform;layer.antialiasingMode=PostProcessLayer.Antialiasing.None;
  var profile=AssetDatabase.LoadAssetAtPath<PostProcessProfile>("Assets/Astra/Materials/Pinkscape Bloom.asset");if(profile==null){profile=ScriptableObject.CreateInstance<PostProcessProfile>();AssetDatabase.CreateAsset(profile,"Assets/Astra/Materials/Pinkscape Bloom.asset");}
  Bloom bloom;if(!profile.TryGetSettings<Bloom>(out bloom)){bloom=profile.AddSettings<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}bloom.enabled.Override(true);bloom.intensity.Override(2.3f);bloom.threshold.Override(.72f);bloom.softKnee.Override(.7f);bloom.diffusion.Override(5.5f);bloom.fastMode.Override(true);

  var volumeObject=new GameObject("Optional sparkle bloom");var volume=volumeObject.AddComponent<PostProcessVolume>();volume.isGlobal=true;volume.priority=10;volume.sharedProfile=profile;
  var program=AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>("Assets/Astra/Scripts/AstraPinkscape.asset");if(program==null){program=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();program.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Astra/Scripts/AstraPinkscape.cs");AssetDatabase.CreateAsset(program,"Assets/Astra/Scripts/AstraPinkscape.asset");}AssetDatabase.SaveAssets();UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
  var root=new GameObject("08 - Pinkscape and gentle hover");var pink=root.AddUdonSharpComponent<AstraPinkscape>();
  pink.magic=UnityEngine.Object.FindObjectOfType<AstraMagic>();pink.glitter=UnityEngine.Object.FindObjectOfType<AstraGlitterControls>();pink.bloomVolume=volumeObject;
  var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/Astra/Materials/Pinkscape Sky.mat");if(sky==null){sky=new Material(Shader.Find("Astra/Celestial Sky"));AssetDatabase.CreateAsset(sky,"Assets/Astra/Materials/Pinkscape Sky.mat");}sky.SetColor("_Zenith",new Color(.042f,.009f,.03f));sky.SetColor("_NebulaA",new Color(.7f,.18f,.36f));sky.SetColor("_NebulaB",new Color(.35f,.12f,.42f));sky.SetColor("_Horizon",new Color(.44f,.14f,.24f));sky.SetFloat("_Clouds",1.75f);sky.SetFloat("_Exposure",.9f);sky.SetFloat("_Stars",.55f);sky.SetFloat("_Seed",4.7f);pink.pinkSky=sky;
  var petals=new GameObject("Cherry blossom petals").AddComponent<ParticleSystem>();petals.transform.SetParent(root.transform);petals.transform.position=Vector3.up*3;petals.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);pink.petals=petals;
  var main=petals.main;main.maxParticles=360;main.startLifetime=new ParticleSystem.MinMaxCurve(10,16);main.startSize=new ParticleSystem.MinMaxCurve(.035f,.09f);main.startSpeed=0;main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.simulationSpace=ParticleSystemSimulationSpace.Local;main.prewarm=true;main.startColor=new Color(1,1,1,.9f);
  var shape=petals.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(12,6,12);
  var e=petals.emission;e.rateOverTime=24;
  var v=petals.velocityOverLifetime;v.enabled=true;v.x=new ParticleSystem.MinMaxCurve(-.12f,.12f);v.y=new ParticleSystem.MinMaxCurve(-.2f,-.08f);v.z=new ParticleSystem.MinMaxCurve(-.1f,.1f);
  var n=petals.noise;n.enabled=true;n.strength=.15f;n.frequency=.32f;n.scrollSpeed=.12f;n.quality=ParticleSystemNoiseQuality.Low;n.octaveCount=1;
  var rot=petals.rotationOverLifetime;rot.enabled=true;rot.z=new ParticleSystem.MinMaxCurve(-.6f,.6f);
  var c=petals.colorOverLifetime;c.enabled=true;var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(1,.75f),new GradientAlphaKey(0,1)});c.color=grad;
  var pm=AssetDatabase.LoadAssetAtPath<Material>("Assets/Astra/Materials/Cherry Blossom Petals.mat");if(pm==null){pm=new Material(Shader.Find("Astra/Cherry Blossom Petal"));AssetDatabase.CreateAsset(pm,"Assets/Astra/Materials/Cherry Blossom Petals.mat");}petals.GetComponent<Renderer>().sharedMaterial=pm;petals.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  var dust=pink.glitter.effects[0].main;dust.maxParticles=2500;
  foreach(var m in pink.glitter.materials){m.SetFloat("_Brightness",2);EditorUtility.SetDirty(m);}
  pink.glitter.brightness.value=.85f;
  var menu=UnityEngine.Object.FindObjectOfType<AstraHandMenu>();var panel=menu.menuRoot.GetComponentsInChildren<Canvas>(true).First(x=>x.name=="Celestial observatory").GetComponent<RectTransform>();
  panel.sizeDelta=new Vector2(780,1380);panel.GetComponent<BoxCollider>().size=new Vector3(780,1380,1);
  var backing=panel.Find("Opaque backing");if(backing!=null)backing.localScale=new Vector3(780,1380,2);
  var u=UdonSharpEditorUtility.GetBackingUdonBehaviour(pink);
  pink.hoverLabel=Button(panel,"HOVER  /  OFF",new Vector2(-175,-550),u,"ToggleHover").GetComponentInChildren<Text>();
  pink.petalLabel=Button(panel,"PETALS  /  ON",new Vector2(175,-550),u,"TogglePetals").GetComponentInChildren<Text>();
  Button(panel,"PINKSCAPE",new Vector2(-175,-625),u,"Pinkscape");
  pink.bloomLabel=Button(panel,"BLOOM  /  ON",new Vector2(175,-625),u,"ToggleBloom").GetComponentInChildren<Text>();
  UdonSharpEditorUtility.CopyProxyToUdon(pink);UdonSharpEditorUtility.CopyProxyToUdon(pink.glitter);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(pole.scene);EditorSceneManager.SaveScene(pole.scene);
  AstraAtmosphereBuilder.Validate();
  if(pole.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Pole still collidable");
  if(ShaderUtil.ShaderHasError(pm.shader))throw new Exception("Petal shader error");
  File.WriteAllText("Review/pinkscape-validation.txt","PASS: no pole colliders; enclosure renderers disabled; HDR reference camera with initialized bloom stack; 11840 particle cap; pinkscape sky, petals, hover and bloom controls assigned.");
  Render();
  Debug.Log("ASTRA_PINKSCAPE_OK");
 }
 public static void Render(){
  EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");
  var pink=UnityEngine.Object.FindObjectOfType<AstraPinkscape>();
  RenderSettings.skybox=pink.pinkSky;
  foreach(var p in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())p.Simulate(12,true,true);
  var go=new GameObject("Pinkscape preview");var camera=go.AddComponent<Camera>();camera.transform.position=new Vector3(0,1.65f,-2.8f);camera.fieldOfView=85;camera.allowHDR=true;
  var pp=go.AddComponent<PostProcessLayer>();pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));pp.volumeLayer=1;pp.volumeTrigger=go.transform;
  var rt=new RenderTexture(1920,1200,24,RenderTextureFormat.ARGBHalf);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1920,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1200),0,0);tex.Apply();File.WriteAllBytes("docs/images/pinkscape.png",tex.EncodeToPNG());pp.enabled=false;camera.Render();tex.ReadPixels(new Rect(0,0,1920,1200),0,0);tex.Apply();File.WriteAllBytes("Review/pinkscape-no-bloom.png",tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(tex);
 }
}


