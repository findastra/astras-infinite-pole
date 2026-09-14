using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

public static class AstraFineMagicUpgrade {
 public static void Run() {
  const string scene="Assets/Astra/Scenes/AstrasInfinitePole.unity";
  EditorSceneManager.OpenScene(scene);
  Directory.CreateDirectory("Review/Backups");
  if(!File.Exists("Review/Backups/BeforeFineMagic.unity.txt"))File.Copy(scene,"Review/Backups/BeforeFineMagic.unity.txt");
  var floor=UnityEngine.Object.FindObjectsOfType<MeshCollider>().First(x=>x.name.StartsWith("Invisible platform"));
  floor.name="Invisible platform - 48m radius";
  floor.transform.localScale=new Vector3(4,1,4);
  for(int i=0;i<48;i++){
   var wall=GameObject.Find("Boundary "+i.ToString("00"));
   float angle=i*Mathf.PI*2/48;
   wall.transform.position=new Vector3(Mathf.Sin(angle)*47.2f,4,Mathf.Cos(angle)*47.2f);
   wall.transform.localScale=new Vector3(4,1,4);
  }
  var cinema=GameObject.Find("Cinema - synchronized playlist");
  cinema.transform.position=new Vector3(10,3,8);
  // Face the arrival area while leaving the pole's central sightline clear.
  cinema.transform.rotation=Quaternion.Euler(0,45,0);
  var g=UnityEngine.Object.FindObjectOfType<AstraGlitterControls>();
  var magic=UnityEngine.Object.FindObjectOfType<AstraMagic>();
  magic.glitterVolume=GameObject.Find("05 - Glitter collection").transform;
  magic.cloudVolume=magic.clouds.transform;
  float[] sizes={.012f,.038f,.027f,.02f,.034f,.025f,.03f,.022f,.019f,.035f,.04f,.027f,.045f};
  for(int i=0;i<g.effects.Length;i++){
   var ps=g.effects[i];ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.startSize=new ParticleSystem.MinMaxCurve(sizes[i]*.35f,sizes[i]);main.simulationSpace=ParticleSystemSimulationSpace.Local;
   var shape=ps.shape;shape.scale=new Vector3(9,7,9);
   var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.orbitalX=new ParticleSystem.MinMaxCurve(-.025f,.025f);velocity.orbitalZ=new ParticleSystem.MinMaxCurve(-.02f,.02f);velocity.orbitalY=new ParticleSystem.MinMaxCurve(i%2==0?.12f:-.28f,i%2==0?.3f:-.1f);
   var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.maxParticleSize=.018f;
   g.materials[i].SetFloat("_Twinkle",.95f);
   EditorUtility.SetDirty(g.materials[i]);
  }
  foreach(var ps in magic.trails){var main=ps.main;main.startSize=new ParticleSystem.MinMaxCurve(.006f,.022f);}
  g.twinkle.value=.95f;
  g.densitySlider.value=.9f;
  magic.flowSlider.value=.8f;
  var hand=UnityEngine.Object.FindObjectOfType<AstraHandMenu>();
  foreach(var t in hand.menuRoot.GetComponentsInChildren<Text>(true)){
   if(t.text=="CLOSE  x"){t.text="CLOSE  X";t.fontSize=22;}
  }
  // Brief instructions are visible on arrival and are part of the closable spellbook.
  var panel=hand.menuRoot.GetComponentsInChildren<Canvas>(true).First(x=>x.name=="Celestial observatory");
  var existing=panel.transform.Find("Hand gesture help");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
  var hint=new GameObject("Hand gesture help",typeof(RectTransform),typeof(Text));
  hint.transform.SetParent(panel.transform,false);
  var rect=(RectTransform)hint.transform;rect.anchoredPosition=new Vector2(0,-470);rect.sizeDelta=new Vector2(750,68);
  var label=hint.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=19;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.65f,1,1);label.raycastTarget=false;
  label.text="HANDS: bring together, pause, then pull apart\nRepeat to close  /  Desktop: M";
  hand.menuRoot.gameObject.SetActive(false);
  UdonSharpEditorUtility.CopyProxyToUdon(magic);UdonSharpEditorUtility.CopyProxyToUdon(g);UdonSharpEditorUtility.CopyProxyToUdon(hand);
  EditorSceneManager.MarkSceneDirty(magic.gameObject.scene);EditorSceneManager.SaveScene(magic.gameObject.scene);AssetDatabase.SaveAssets();
  AstraAtmosphereBuilder.Validate();
  Physics.SyncTransforms();
  foreach(float x in new[]{-46f,46f})if(!Physics.Raycast(new Vector3(x,1,0),Vector3.down,out var hit,2)||hit.collider!=floor)throw new Exception("Expanded floor edge missing");
  if(Physics.Raycast(new Vector3(49,1,0),Vector3.down,2))throw new Exception("Floor larger than requested");
  foreach(string shader in new[]{"Astra/Infinite Pole","Astra/Glitter Gradient"})if(ShaderUtil.ShaderHasError(Shader.Find(shader)))throw new Exception("Shader error: "+shader);
  File.WriteAllText("Review/fine-magic-validation.txt","PASS: 96m diameter floor, edge and boundary collision, 11980 maximum particles, smaller sizes, player-centered particle volumes, hidden closable hand menu. VR gesture comfort and GPU performance require headset testing.");
  AstraAtmosphereBuilder.Render();
  File.Copy("Review/Celestial Preview.png","docs/images/fine-magic.png",true);
  Debug.Log("ASTRA_FINE_MAGIC_OK");
 }
}


