using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Cloud rings orbit the spiral stairs, some clockwise and some counterclockwise. (Claude round 3b)
// Positions come from server time, so everyone sees the same clouds with no network traffic.
// Standing on a moving cloud carries you along with it.
// 2026-10-03 (Claude): carrying a standing player keeps their yaw only (see PostLateUpdate).
// Keep-clear zones (round 3f): a cloud that drifts into the video screen or DJ area vanishes until it has passed.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraCloudOrbit : UdonSharpBehaviour {
 public Transform[] pivots;     // one per ring per stair turn
 public float[] speeds;         // degrees per second; sign = direction
 public float[] phases;         // degrees
 public Transform[] pivotTurns; // the stair turn each pivot rides on
 public float nearRange=26f;    // rings this close update every frame, the rest every 6th frame
 public Transform[] clouds;     // every cloud, grouped by pivot
 public int[] pivotFirst;       // index of each pivot's first cloud in clouds[]
 public int[] pivotCount;       // how many clouds each pivot carries
 public float[] cloudYaw;       // starting spin angle per cloud (degrees)
 public float[] cloudSpin;      // spin speed per cloud, degrees per second; sign = direction (round 3f)
 public Transform[] clearZones; // unscaled boxes, oriented by their rotation
 public Vector3[] clearHalf;    // half size of each zone in meters
 public float zoneBand=30f;     // only rings within this height of a zone are tested
 private VRCPlayerApi player;
 private Transform standing; private Vector3 lastPos; private float lastYaw; private bool haveYaw;
 private int frame;
 private void Start(){player=Networking.LocalPlayer;Spin(true);}
 private void Update(){Spin(false);}
 private void Spin(bool all){
  frame++;
  double t=Networking.GetServerTimeInSeconds();
  float py=Utilities.IsValid(player)?player.GetPosition().y:0f;
  int zones=clearZones==null?0:clearZones.Length;
  for(int i=0;i<pivots.Length;i++){
   float ty=pivotTurns[i].position.y;
   if(!all&&Mathf.Abs(ty-py)>nearRange&&(frame+i)%6!=0)continue;
   float a=(float)((t*speeds[i]+phases[i])%360.0);
   pivots[i].localRotation=Quaternion.Euler(0,a,0);
   bool zoneNear=false;
   for(int z=0;z<zones;z++)if(Mathf.Abs(ty-clearZones[z].position.y)<zoneBand){zoneNear=true;break;}
   int end=pivotFirst[i]+pivotCount[i];
   for(int j=pivotFirst[i];j<end;j++){
    Transform cloud=clouds[j];
    cloud.localRotation=Quaternion.Euler(0,(float)((cloudYaw[j]+t*cloudSpin[j])%360.0),0); // each cloud turns slowly on its own
    if(!zoneNear)continue;
    bool inside=false;
    Vector3 cp=cloud.position;
    for(int k=0;k<zones;k++){
     Vector3 d=clearZones[k].InverseTransformDirection(cp-clearZones[k].position);Vector3 h=clearHalf[k]; // oriented box (zone transforms are unscaled)
     if(Mathf.Abs(d.x)<h.x&&Mathf.Abs(d.y)<h.y&&Mathf.Abs(d.z)<h.z){inside=true;break;}
    }
    if(cloud.gameObject.activeSelf==inside)cloud.gameObject.SetActive(!inside);
   }
  }
 }
 public override void PostLateUpdate(){
  if(!Utilities.IsValid(player))return;
  if(!player.IsPlayerGrounded()){standing=null;return;}
  RaycastHit hit;Vector3 p=player.GetPosition();
  Transform now=null;
  // Only the Default layer (clouds live there). Without this mask the ray hit the player's own body,
  // which VRChat scripts may not touch, and the exception halted the whole script (orbit stopped).
  if(Physics.Raycast(p+Vector3.up*.25f,Vector3.down,out hit,.6f,1,QueryTriggerInteraction.Ignore)){
   Collider col=hit.collider;
   if(Utilities.IsValid(col)){Transform h=col.transform;if(h.name.StartsWith("Cloud "))now=h;}
  }
  if(now!=null&&now==standing){
   Vector3 delta=now.position-lastPos;
   // 2026-10-03 (Claude): this used to pass player.GetRotation() straight back with AlignPlayerWithSpawnPoint.
   // Looking far up or down puts pitch/roll into that rotation, the teleport wrote it onto the player's body,
   // and the next frame read it back bigger - the view spun out of control. Keep the yaw only, so the carry
   // never touches which way you are looking.
   if(delta.sqrMagnitude>.000001f&&delta.sqrMagnitude<1f){
    // 2026-10-04 (Claude): when you look almost straight up or down the flattened forward vector gets tiny and its
    // direction is noise, which can still twist you. Below that point keep the last good heading instead.
    Vector3 fwd=player.GetRotation()*Vector3.forward;fwd.y=0f;
    if(fwd.sqrMagnitude>.04f){lastYaw=Mathf.Atan2(fwd.x,fwd.z)*Mathf.Rad2Deg;haveYaw=true;}
    else if(!haveYaw){standing=now;lastPos=now.position;return;}
    Quaternion flat=Quaternion.Euler(0f,lastYaw,0f);
    player.TeleportTo(p+delta,flat,VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,true);
   }
  }
  standing=now;if(now!=null)lastPos=now.position;
 }
}
