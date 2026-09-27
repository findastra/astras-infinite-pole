using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// One of the two linked phone booths: reports who steps in and out. (Claude round 3g)
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraPhoneBooth : UdonSharpBehaviour {
 public AstraPhoneLine line;
 public int booth; // 0 or 1
 public override void OnPlayerTriggerEnter(VRCPlayerApi p){if(Utilities.IsValid(p))line.Enter(booth,p);}
 public override void OnPlayerTriggerExit(VRCPlayerApi p){if(Utilities.IsValid(p))line.Exit(booth,p);}
}
