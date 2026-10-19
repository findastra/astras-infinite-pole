using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// DJ cloud mic zone: whoever stands behind the decks is heard clearly across the whole world. (Claude round 3c)
// Runs on every client for every player, so everyone hears the DJ the same way. Leaving the zone restores normal voice.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraDJBooth : UdonSharpBehaviour {
 public float gain=18f, near=20f, far=250f;
 public override void OnPlayerTriggerEnter(VRCPlayerApi p){
  if(!Utilities.IsValid(p))return;
  p.SetVoiceGain(gain);p.SetVoiceDistanceNear(near);p.SetVoiceDistanceFar(far);p.SetVoiceLowpass(false);
 }
 public override void OnPlayerTriggerExit(VRCPlayerApi p){
  if(!Utilities.IsValid(p))return;
  p.SetVoiceGain(15f);p.SetVoiceDistanceNear(0f);p.SetVoiceDistanceFar(25f);p.SetVoiceVolumetricRadius(0f);p.SetVoiceLowpass(true); // VRChat defaults
 }
}
