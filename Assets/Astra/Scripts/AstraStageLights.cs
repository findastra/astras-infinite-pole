using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// DJ cloud stage lights (Claude round 3h): moving heads sweep their beams in sync for everyone (server time),
// and pause when nobody local is near, to save work.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraStageLights : UdonSharpBehaviour {
 public Transform[] heads;
 public float[] yawBase;
 public float sweep=38f, tilt=24f, speed=.55f, activeRange=90f;
 private VRCPlayerApi player;
 private void Start(){player=Networking.LocalPlayer;}
 private void Update(){
  if(Utilities.IsValid(player)&&Vector3.Distance(player.GetPosition(),transform.position)>activeRange)return;
  float t=(float)(Networking.GetServerTimeInSeconds()%3600.0);
  for(int i=0;i<heads.Length;i++){
   float ph=i*1.7f;
   float yaw=yawBase[i]+Mathf.Sin(t*speed+ph)*sweep;
   float pitch=-35f+Mathf.Sin(t*speed*1.37f+ph*.6f)*tilt; // upward, sweeping over the crowd and the sky
   heads[i].localRotation=Quaternion.Euler(pitch,yaw,0);
  }
 }
}
