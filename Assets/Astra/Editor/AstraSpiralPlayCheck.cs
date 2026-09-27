using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDKBase;
[InitializeOnLoad]
public static class AstraSpiralPlayCheck {
 static double ready;static int stage;static Vector3 spawn;static float gravity;
 static AstraSpiralPlayCheck(){EditorApplication.playModeStateChanged+=s=>{if(!SessionState.GetBool("Astra.SpiralTest",false))return;if(s==PlayModeStateChange.EnteredPlayMode){ready=EditorApplication.timeSinceStartup+8;stage=0;EditorApplication.update+=Tick;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("Astra.SpiralTest",false);if(Application.isBatchMode)EditorApplication.Exit(SessionState.GetInt("Astra.SpiralResult",1));}};}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");SessionState.SetBool("Astra.SpiralTest",true);SessionState.SetInt("Astra.SpiralResult",1);EditorApplication.isPlaying=true;}
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Tick(){if(EditorApplication.timeSinceStartup<ready)return;
  try{
   var p=Networking.LocalPlayer;var stair=UnityEngine.Object.FindObjectOfType<AstraSpiral>();
   if(stage==0){spawn=p.GetPosition();gravity=p.GetGravityStrength();p.SetGravityStrength(0);p.TeleportTo(new Vector3(0,140,-3.3f),Quaternion.identity);p.SetVelocity(Vector3.zero);stage=1;ready=EditorApplication.timeSinceStartup+1;return;}
   if(stage==1){
    int center=Mathf.Max(5,Mathf.FloorToInt(p.GetPosition().y/6.4f));
    Check(stair.turns.Min(t=>t.position.y)>90,"Actual Udon did not recycle turns at altitude");
    Check(Mathf.Abs(stair.pole.position.y-p.GetPosition().y)<.1f,"Pole did not follow altitude");
    Physics.SyncTransforms();RaycastHit hit;
    Check(Physics.Raycast(new Vector3(0,center*6.4f+4.2f,3.3f),Vector3.down,out hit,1.1f)&&hit.collider.name.StartsWith("Spiral turn"),"Recycled collision failed");
    var magic=UnityEngine.Object.FindObjectOfType<AstraMagic>();Check(Mathf.Abs(magic.cloudVolume.position.y-(p.GetPosition().y-2))<.1f,"Cloud follow height failed");
    p.TeleportTo(spawn,Quaternion.identity);p.SetVelocity(Vector3.zero);stage=2;ready=EditorApplication.timeSinceStartup+1;return;
   }
   Check(stair.turns.Min(t=>t.position.y)==0,"Stair origin did not restore after returning to spawn");p.SetGravityStrength(gravity);
   File.WriteAllText("Review/spiral-play-check.txt","PASS: actual Udon recycled the 11-turn pool at 140m; collision raycast on a recycled turn passed; pole and clouds followed altitude; ground-level pool restored after returning to spawn. Headset comfort and extended climbing remain unverified.");
   SessionState.SetInt("Astra.SpiralResult",0);Debug.Log("ASTRA_SPIRAL_PLAY_OK");
  }catch(Exception e){File.WriteAllText("Review/spiral-play-check.txt","FAILED: "+e);Debug.LogException(e);}
  EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
 }
}
