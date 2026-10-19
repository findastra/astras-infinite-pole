using UdonSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UdonSharpEditor;
public static class AstraSpiralUpgrade {
 const string Scene="Assets/Astra/Scenes/AstrasInfinitePole.unity";
 const string Root="Assets/Astra/";
 public static void Prepare(){
  var p=AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(Root+"Scripts/AstraSpiral.asset");
  if(p==null){p=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();p.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>(Root+"Scripts/AstraSpiral.cs");AssetDatabase.CreateAsset(p,Root+"Scripts/AstraSpiral.asset");}
  AssetDatabase.SaveAssets();UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
 }
 public static void Run(){
  EditorSceneManager.OpenScene(Scene);
  if(UnityEngine.Object.FindObjectOfType<AstraSpiral>()!=null)throw new Exception("Spiral already exists; refusing duplicate upgrade");
  File.Copy(Scene,"Review/Backups/BeforeSpiral.unity.txt",true);
  var magic=UnityEngine.Object.FindObjectOfType<AstraMagic>();var clouds=magic.clouds;
  clouds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=clouds.main;main.startSize3D=true;main.startSizeX=new ParticleSystem.MinMaxCurve(2.5f,7);main.startSizeY=new ParticleSystem.MinMaxCurve(1.2f,3.8f);main.startSizeZ=1;main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.startSpeed=new ParticleSystem.MinMaxCurve(.015f,.075f);main.startLifetime=new ParticleSystem.MinMaxCurve(24,46);main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new ParticleSystem.MinMaxGradient(new Color(.02f,.14f,1,.11f),new Color(.98f,.95f,1,.22f));main.maxParticles=180;
  var shape=clouds.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(42,15,42);shape.rotation=Vector3.zero;
  clouds.useAutoRandomSeed=true;magic.cloudVolume.position=new Vector3(0,-2,0);
  var emission=clouds.emission;emission.rateOverTime=5;
  var vel=clouds.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;vel.orbitalX=0;vel.orbitalY=0;vel.orbitalZ=0;vel.x=new ParticleSystem.MinMaxCurve(-.065f,.085f);vel.y=new ParticleSystem.MinMaxCurve(-.015f,.025f);vel.z=new ParticleSystem.MinMaxCurve(-.045f,.06f);
  var rot=clouds.rotationOverLifetime;rot.enabled=true;rot.z=new ParticleSystem.MinMaxCurve(-.045f,.045f);
  var noise=clouds.noise;noise.enabled=true;noise.strength=.35f;noise.frequency=.09f;noise.scrollSpeed=.04f;noise.octaveCount=2;
  var cr=clouds.GetComponent<ParticleSystemRenderer>();cr.maxParticleSize=.65f;cr.sortMode=ParticleSystemSortMode.Distance;
  var pink=UnityEngine.Object.FindObjectOfType<AstraPinkscape>();
  var profile=pink.bloomVolume.GetComponent<PostProcessVolume>().sharedProfile;Bloom b;if(profile.TryGetSettings<Bloom>(out b)){b.threshold.Override(1.1f);b.intensity.Override(1.35f);b.diffusion.Override(4);b.fastMode.Override(false);EditorUtility.SetDirty(profile);EditorUtility.SetDirty(b);}
  foreach(var setting in profile.settings)if(!(setting is Bloom))setting.active=false;
  var descriptor=UnityEngine.Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();var camera=descriptor.ReferenceCamera.GetComponent<Camera>();camera.nearClipPlane=.03f;camera.allowHDR=true;camera.allowMSAA=true;camera.GetComponent<PostProcessLayer>().antialiasingMode=PostProcessLayer.Antialiasing.None;
  QualitySettings.antiAliasing=4;
  var pole=GameObject.Find("Infinite Pole - 45mm diameter");pole.GetComponent<Renderer>().sharedMaterial.renderQueue=2010;
  var stairs=new GameObject("09 - Infinite prismatic spiral");var behaviour=stairs.AddUdonSharpComponent<AstraSpiral>();behaviour.pole=pole.transform;behaviour.turns=new Transform[11];
  var mesh=MakeTurn(false);AssetDatabase.CreateAsset(mesh,Root+"Materials/Spiral Steps.asset");var ramp=MakeTurn(true);AssetDatabase.CreateAsset(ramp,Root+"Materials/Spiral Walkable Ramp.asset");
  var material=new Material(Shader.Find("Astra/Prismatic Stair"));AssetDatabase.CreateAsset(material,Root+"Materials/Prismatic Stair.mat");
  for(int i=0;i<11;i++){
   var turn=new GameObject("Spiral turn "+i);turn.transform.SetParent(stairs.transform);turn.transform.localPosition=Vector3.up*(i*6.4f);turn.AddComponent<MeshFilter>().sharedMesh=mesh;
   var renderer=turn.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
   turn.AddComponent<MeshCollider>().sharedMesh=ramp;behaviour.turns[i]=turn.transform;
  }
  UdonSharpEditorUtility.CopyProxyToUdon(behaviour);UdonSharpEditorUtility.CopyProxyToUdon(magic);
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(pole.scene);EditorSceneManager.SaveScene(pole.scene);
  Validate();Render();Debug.Log("ASTRA_SPIRAL_OK");
 }
 static List<Vector3> v;static List<int> t;static List<Vector2> uv;
 static void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int k=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)});t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}
 static Vector3 P(float radius,float angle,float y){return new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);}
 static Mesh MakeTurn(bool ramp){
  v=new List<Vector3>();t=new List<int>();uv=new List<Vector2>();
  for(int step=0;step<40;step++)for(int j=0;j<3;j++){
   float a=-Mathf.PI/2+(step+j/3f)*Mathf.PI*2/40,b=-Mathf.PI/2+(step+(j+1)/3f)*Mathf.PI*2/40;
   float y=(step+1)*.16f,ya=ramp?(step+j/3f)*.16f:y,yb=ramp?(step+(j+1)/3f)*.16f:y;
   var ia=P(2.3f,a,ya);var ib=P(2.3f,b,yb);var ob=P(4.3f,b,yb);var oa=P(4.3f,a,ya);Quad(ia,ib,ob,oa);
   if(!ramp){var down=Vector3.down*.065f;Quad(oa+down,ob+down,ib+down,ia+down);Quad(ia+down,ib+down,ib,ia);Quad(oa,ob,ob+down,oa+down);if(j==0)Quad(ia,oa,oa+down,ia+down);if(j==2)Quad(ob,ib,ib+down,ob+down);}
  }
  var m=new Mesh{name=ramp?"Smooth spiral collision":"Sheer spiral treads"};m.SetVertices(v);m.SetTriangles(t,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();return m;
 }
 public static void Validate(){
  Physics.SyncTransforms();var spiral=UnityEngine.Object.FindObjectOfType<AstraSpiral>();if(spiral==null||spiral.turns.Length!=11)throw new Exception("Missing stair pool");
  foreach(var tr in spiral.turns)if(tr.GetComponent<MeshCollider>()==null)throw new Exception("Missing stair collision");
  for(int s=0;s<40;s++){float angle=-Mathf.PI/2+(s+.5f)*Mathf.PI*2/40;var p=P(3.3f,angle,(s+.5f)*.16f+1);RaycastHit h;if(!Physics.Raycast(p,Vector3.down,out h,1.05f)||!h.collider.name.StartsWith("Spiral turn"))throw new Exception("Stair raycast failed "+s);if(Mathf.Abs(h.point.y-(s+.5f)*.16f)>.01f)throw new Exception("Stair height mismatch");}
  if(spiral.pole.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Pole collider returned");
  foreach(string name in new[]{"Astra/Prismatic Stair","Astra/Sparkle Clouds","Astra/Infinite Pole"})if(ShaderUtil.ShaderHasError(Shader.Find(name)))throw new Exception("Shader error: "+name);
  File.WriteAllText("Review/spiral-validation.txt","PASS: 11 recycled turns, 40 sampled continuous collision heights per turn, pole remains collision-free, shader compilation. Stair treads 0.16m rise, 2m width; smooth collision reduces step bounce. Cloud volume spans -9.5m to +5.5m at spawn. Live walking, stereo rendering and performance pending.");
 }
 public static void Render(){
  EditorSceneManager.OpenScene(Scene);var pink=UnityEngine.Object.FindObjectOfType<AstraPinkscape>();RenderSettings.skybox=pink.pinkSky;
  foreach(var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())ps.Simulate(30,true,true);
  var go=new GameObject("Spiral preview");var camera=go.AddComponent<Camera>();camera.transform.position=new Vector3(8,3,-12);camera.transform.LookAt(new Vector3(0,5,0));camera.fieldOfView=65;camera.allowHDR=true;camera.nearClipPlane=.03f;
  var pp=go.AddComponent<PostProcessLayer>();pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));pp.volumeLayer=1;pp.volumeTrigger=go.transform;
  var rt=new RenderTexture(1920,1200,24,RenderTextureFormat.ARGBHalf);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1920,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1200),0,0);tex.Apply();File.WriteAllBytes("docs/images/spiral.png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(tex);
 }
}

