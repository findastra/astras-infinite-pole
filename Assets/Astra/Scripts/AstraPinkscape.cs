using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraPinkscape : UdonSharpBehaviour {
 public ParticleSystem petals;
 public GameObject bloomVolume;
 public Material pinkSky;
 public AstraGlitterControls glitter;
 public AstraMagic magic;
 public Text hoverLabel,petalLabel,bloomLabel;
 private bool hovering;
 private float baseHeight,startTime,savedGravity=1;
 public void ToggleHover(){
  var p=Networking.LocalPlayer;if(!Utilities.IsValid(p))return;
  hovering=!hovering;
  if(hovering){baseHeight=p.GetPosition().y;startTime=Time.time;savedGravity=p.GetGravityStrength();p.SetGravityStrength(0);}
  else{p.SetGravityStrength(savedGravity);var v=p.GetVelocity();v.y=0;p.SetVelocity(v);}
  hoverLabel.text=hovering?"HOVER  /  ON":"HOVER  /  OFF";
 }
 public override void OnPlayerRespawn(VRCPlayerApi p){if(p.isLocal&&hovering)ToggleHover();}
 public void TogglePetals(){if(petals.isPlaying){petals.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);petalLabel.text="PETALS  /  OFF";}else{petals.Play();petalLabel.text="PETALS  /  ON";}}
 public void ToggleBloom(){bloomVolume.SetActive(!bloomVolume.activeSelf);bloomLabel.text=bloomVolume.activeSelf?"BLOOM  /  ON":"BLOOM  /  OFF";}
 public void Pinkscape(){
  RenderSettings.skybox=pinkSky;
  glitter.hueA.value=.91f;glitter.hueB.value=.78f;glitter.saturation.value=.48f;glitter.brightness.value=.85f;glitter.ApplyLook();
  magic.RoseClouds();magic.cloudToggle.isOn=true;
  if(!petals.isPlaying)TogglePetals();
 }
 private void FixedUpdate(){
  var p=Networking.LocalPlayer;if(!Utilities.IsValid(p))return;
  petals.transform.position=p.GetPosition()+Vector3.up*3;
  if(!hovering)return;
  float phase=(Time.time-startTime)*Mathf.PI/12;
  float target=baseHeight+.3f*(1-Mathf.Cos(phase));
  var v=p.GetVelocity();v.y=Mathf.Clamp((target-p.GetPosition().y)*1.5f+.3f*Mathf.PI/12*Mathf.Sin(phase),-.12f,.12f);p.SetVelocity(v);
 }
}
