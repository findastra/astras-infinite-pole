using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Linked phone booths (Claude round 3g): while you stand in one booth, everyone in the other booth sounds
// as close and clear as if they were next to you, and they hear you the same way. Runs on every client, no network sync.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraPhoneLine : UdonSharpBehaviour {
 public float linkedNear=70f, linkedFar=100f; // the booths are ~45 m apart
 private VRCPlayerApi[] inA=new VRCPlayerApi[82], inB=new VRCPlayerApi[82];
 private int nA, nB;
 public void Enter(int booth,VRCPlayerApi p){
  if(booth==0){if(Find(inA,nA,p)<0&&nA<inA.Length){inA[nA]=p;nA++;}}
  else{if(Find(inB,nB,p)<0&&nB<inB.Length){inB[nB]=p;nB++;}}
  Apply();
 }
 public void Exit(int booth,VRCPlayerApi p){
  if(booth==0)nA=Remove(inA,nA,p);else nB=Remove(inB,nB,p);
  if(Utilities.IsValid(p)&&!p.isLocal)Normal(p);
  Apply();
 }
 public override void OnPlayerLeft(VRCPlayerApi p){nA=Remove(inA,nA,p);nB=Remove(inB,nB,p);Apply();}
 private void Apply(){
  VRCPlayerApi me=Networking.LocalPlayer;if(!Utilities.IsValid(me))return;
  bool meA=Find(inA,nA,me)>=0, meB=Find(inB,nB,me)>=0;
  for(int i=0;i<nA;i++){VRCPlayerApi p=inA[i];if(!Utilities.IsValid(p)||p.isLocal)continue;if(meB)Linked(p);else Normal(p);}
  for(int i=0;i<nB;i++){VRCPlayerApi p=inB[i];if(!Utilities.IsValid(p)||p.isLocal)continue;if(meA)Linked(p);else Normal(p);}
 }
 private void Linked(VRCPlayerApi p){p.SetVoiceDistanceNear(linkedNear);p.SetVoiceDistanceFar(linkedFar);p.SetVoiceGain(15f);p.SetVoiceLowpass(false);}
 private void Normal(VRCPlayerApi p){p.SetVoiceDistanceNear(0f);p.SetVoiceDistanceFar(25f);p.SetVoiceGain(15f);p.SetVoiceLowpass(true);}
 private int Find(VRCPlayerApi[] a,int n,VRCPlayerApi p){for(int i=0;i<n;i++)if(a[i]==p)return i;return -1;}
 private int Remove(VRCPlayerApi[] a,int n,VRCPlayerApi p){int i=Find(a,n,p);if(i<0)return n;a[i]=a[n-1];a[n-1]=null;return n-1;}
}
