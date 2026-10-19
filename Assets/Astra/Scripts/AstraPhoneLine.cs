using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Linked phone booths (Claude round 3g; any number of booths since 2026-09-26).
// 2026-10-01: hub layout. One booth is the hub (the main-floor banner booth). While you stand in a booth, you hear
// the booths LINKED to yours: a lower booth hears the hub and the hub hears every booth, but two lower booths
// never hear each other. Set hub to -1 for the old everyone-hears-everyone behaviour.
// Every client works this out from the same trigger events, so everyone hears the same thing. No network sync.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraPhoneLine : UdonSharpBehaviour {
 public float linkedNear=250f, linkedFar=300f; // booths can be far apart, and the orbiting ones ride the stairs up and down
 public int hub=0;                             // the main-floor booth; every other booth talks only to this one
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
 // two booths are on the same call when they are different booths and at least one of them is the hub
 private bool Linked(int a,int b){if(a<0||b<0||a==b)return false;if(hub<0)return true;return a==hub||b==hub;}
 private void Apply(){
  VRCPlayerApi me=Networking.LocalPlayer;if(!Utilities.IsValid(me))return;
  int mine=BoothOf(me);
  for(int i=0;i<n;i++){
   VRCPlayerApi p=who[i];if(!Utilities.IsValid(p)||p.isLocal)continue;
   if(Linked(mine,where[i]))Near(p);else Normal(p);
  }
 }
 private void Near(VRCPlayerApi p){p.SetVoiceDistanceNear(linkedNear);p.SetVoiceDistanceFar(linkedFar);p.SetVoiceGain(15f);p.SetVoiceLowpass(false);}
 private void Normal(VRCPlayerApi p){p.SetVoiceDistanceNear(0f);p.SetVoiceDistanceFar(25f);p.SetVoiceGain(15f);p.SetVoiceLowpass(true);}
}
