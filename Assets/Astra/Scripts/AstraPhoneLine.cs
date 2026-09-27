using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Linked phone booths (Claude round 3g; any number of booths since 2026-09-26): while you stand in a booth, everyone standing
// in any OTHER booth sounds close and clear, wherever that booth is, and they hear you the same way.
// Every client tracks who is in which booth from the same trigger events, so everyone hears everyone the same way. No network sync.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraPhoneLine : UdonSharpBehaviour {
 public float linkedNear=250f, linkedFar=300f; // booths can be far apart, and the orbiting ones ride the stairs up and down
 private VRCPlayerApi[] who=new VRCPlayerApi[160];
 private int[] where=new int[160];
 private int n;
 public void Enter(int booth,VRCPlayerApi p){
  for(int i=0;i<n;i++)if(who[i]==p&&where[i]==booth){Apply();return;}
  if(n<who.Length){who[n]=p;where[n]=booth;n++;}
  Apply();
 }
 public void Exit(int booth,VRCPlayerApi p){
  for(int i=n-1;i>=0;i--)if(who[i]==p&&where[i]==booth)RemoveAt(i);
  if(Utilities.IsValid(p)&&!p.isLocal&&BoothOf(p)<0)Normal(p);
  Apply();
 }
 public override void OnPlayerLeft(VRCPlayerApi p){
  for(int i=n-1;i>=0;i--)if(who[i]==p||!Utilities.IsValid(who[i]))RemoveAt(i);
  Apply();
 }
 private void RemoveAt(int i){n--;who[i]=who[n];where[i]=where[n];who[n]=null;}
 private int BoothOf(VRCPlayerApi p){for(int i=0;i<n;i++)if(who[i]==p)return where[i];return -1;}
 private void Apply(){
  VRCPlayerApi me=Networking.LocalPlayer;if(!Utilities.IsValid(me))return;
  int mine=BoothOf(me);
  for(int i=0;i<n;i++){
   VRCPlayerApi p=who[i];if(!Utilities.IsValid(p)||p.isLocal)continue;
   if(mine>=0&&where[i]!=mine)Linked(p);else Normal(p);
  }
 }
 private void Linked(VRCPlayerApi p){p.SetVoiceDistanceNear(linkedNear);p.SetVoiceDistanceFar(linkedFar);p.SetVoiceGain(15f);p.SetVoiceLowpass(false);}
 private void Normal(VRCPlayerApi p){p.SetVoiceDistanceNear(0f);p.SetVoiceDistanceFar(25f);p.SetVoiceGain(15f);p.SetVoiceLowpass(true);}
}
