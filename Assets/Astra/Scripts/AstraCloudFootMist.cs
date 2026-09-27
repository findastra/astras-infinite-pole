using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
// Cloud foot mist (Claude, board task C1). One small mist emitter for the LOCAL player only: while you stand on one of
// the assigned cloud colliders, soft mist puffs around your feet and lower legs. It never moves the player,
// changes gravity, collision or voice. Everything is local; nothing is networked.
// Setup (Astra): assign Mist (a small particle system; its particle count is set there), Cloud Colliders
// (the clouds' box colliders), and optionally Mist Toggle; bind that toggle's On Value Changed to ApplyToggle.
// Events: EnableMist, DisableMist, ToggleMist, ApplyToggle.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraCloudFootMist : UdonSharpBehaviour {
 [Tooltip("The mist particle system. It is moved to the local player's feet.")] public ParticleSystem mist;
 [Tooltip("Colliders that count as clouds. Anything else under your feet stops the mist.")] public Collider[] cloudColliders;
 [Tooltip("Optional World items switch. Bind its On Value Changed to ApplyToggle.")] public Toggle mistToggle;
 [Tooltip("Mist on at start (when no toggle is assigned).")] public bool mistEnabled=true;
 [Tooltip("Seconds between ground checks (the mist itself follows your feet every frame).")] public float checkInterval=.2f;
 [Tooltip("How far below your feet a cloud surface may be and still count as standing on it (meters).")] public float footThreshold=.35f;
 [Tooltip("Height above the feet the mist sits at (meters).")] public float mistHeight=.08f;

 private VRCPlayerApi player;
 private bool onCloud;
 private float nextCheck;
 private const int DefaultLayerMask=1; // clouds live on the Default layer; ignores players, UI and triggers

 private void Start(){
  player=Networking.LocalPlayer;
  if(mistToggle!=null)mistEnabled=mistToggle.isOn;
  StopMist(true);
 }

 private void Update(){
  if(!mistEnabled||mist==null)return;
  if(!Utilities.IsValid(player)){player=Networking.LocalPlayer;return;}
  if(Time.time>=nextCheck){nextCheck=Time.time+checkInterval;onCloud=CheckCloud();}
  if(onCloud){
   mist.transform.position=player.GetPosition()+Vector3.up*mistHeight; // follows feet, also on moving clouds
   if(!mist.isEmitting)mist.Play();
  }else if(mist.isEmitting)StopMist(false);
 }

 private bool CheckCloud(){
  if(!player.IsPlayerGrounded())return false;
  RaycastHit hit;
  Vector3 from=player.GetPosition()+Vector3.up*.3f;
  if(!Physics.Raycast(from,Vector3.down,out hit,.3f+footThreshold,DefaultLayerMask,QueryTriggerInteraction.Ignore))return false;
  Collider c=hit.collider;
  if(!Utilities.IsValid(c)||cloudColliders==null)return false; // not every hit object is accessible to Udon
  for(int i=0;i<cloudColliders.Length;i++)if(cloudColliders[i]==c)return true;
  return false;
 }

 private void StopMist(bool clear){
  onCloud=false;
  if(mist==null)return;
  mist.Stop(true,clear?ParticleSystemStopBehavior.StopEmittingAndClear:ParticleSystemStopBehavior.StopEmitting);
 }

 public override void OnPlayerRespawn(VRCPlayerApi p){if(Utilities.IsValid(p)&&p.isLocal)StopMist(true);}

 public void EnableMist(){mistEnabled=true;nextCheck=0;}
 public void DisableMist(){mistEnabled=false;StopMist(true);}
 public void ToggleMist(){if(mistEnabled)DisableMist();else EnableMist();}
 public void ApplyToggle(){if(mistToggle==null)return;if(mistToggle.isOn)EnableMist();else DisableMist();}

 private void OnDisable(){StopMist(true);}
}
