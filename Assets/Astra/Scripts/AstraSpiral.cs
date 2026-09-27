using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
// Infinite spiral: recycled turns (21 since round 1c) follow the local player up AND down. (Claude round 1)
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraSpiral : UdonSharpBehaviour {
 public Transform[] turns;
 public Transform pole;
 public float pitch=6.4f;
 public Material stairMaterial;
 public Slider colorSlider;
 private int lastCenter=int.MinValue;
 private VRCPlayerApi player; // cached once (VRChat perf tip)
 private void Start(){player=Networking.LocalPlayer;Recenter(0);ApplyColor();}
 private void LateUpdate(){
  if(!Utilities.IsValid(player)){player=Networking.LocalPlayer;return;}
  float y=player.GetPosition().y;
  int center=Mathf.FloorToInt(y/pitch);
  if(center!=lastCenter)Recenter(center);
  // Collision-free visual pole follows the viewer in both directions.
  if(pole!=null){var pos=pole.position;pos.y=y;pole.position=pos;}
 }
 public void Recenter(int center){
  lastCenter=center;
  int n=turns.Length;int half=(n-1)/2;
  for(int i=center-half;i<=center+half;i++){
   int slot=((i%n)+n)%n;
   turns[slot].localPosition=new Vector3(0,i*pitch,0);
  }
 }
 public void ApplyColor(){
  if(stairMaterial==null||colorSlider==null)return;
  stairMaterial.SetFloat("_Hue",colorSlider.value);
 }
}
