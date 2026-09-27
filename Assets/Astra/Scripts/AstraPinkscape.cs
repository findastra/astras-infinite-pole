using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
// Pinkscape preset, petals and bloom. 2026-09-26 (Claude): petals, bloom and the PINKSCAPE preset are shared with everyone
// in the instance. HOVER stays personal: it only changes how your own avatar moves.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AstraPinkscape : UdonSharpBehaviour {
 public ParticleSystem petals;
 public GameObject bloomVolume;
 public Material pinkSky;
 public AstraGlitterControls glitter;
 public AstraMagic magic;
 public Text hoverLabel,petalLabel,bloomLabel;
 public AstraAtmosphere atmosphere;
 [UdonSynced] private bool petalsOn,bloomOn;
 private bool got,started;
 private bool hovering;
 private float baseHeight,startTime,savedGravity=1;
 private void Start(){
  if(!got){petalsOn=petals.isPlaying;bloomOn=bloomVolume.activeSelf;if(Networking.IsOwner(gameObject))RequestSerialization();}
  started=true;Show();
 }
 public void ToggleHover(){
  var p=Networking.LocalPlayer;if(!Utilities.IsValid(p))return;
  hovering=!hovering;
  if(hovering){baseHeight=p.GetPosition().y;startTime=Time.time;savedGravity=p.GetGravityStrength();p.SetGravityStrength(0);}
  else{p.SetGravityStrength(savedGravity);var v=p.GetVelocity();v.y=0;p.SetVelocity(v);}
  hoverLabel.text=hovering?"HOVER  /  ON":"HOVER  /  OFF";
 }
 public override void OnPlayerRespawn(VRCPlayerApi p){if(p.isLocal&&hovering)ToggleHover();}
 public void TogglePetals(){petalsOn=!petalsOn;Share();Show();}
 public void ToggleBloom(){bloomOn=!bloomOn;Share();Show();}
 public void Pinkscape(){
  if(atmosphere!=null)atmosphere.PinkCloudSea();else RenderSettings.skybox=pinkSky;
  glitter.hueA.value=.91f;glitter.hueB.value=.78f;glitter.saturation.value=.48f;glitter.brightness.value=.85f;glitter.ApplyLook();
  magic.RoseClouds();magic.cloudToggle.isOn=true;
  petalsOn=true;Share();Show();
 }
 private void Share(){if(!started)return;Networking.SetOwner(Networking.LocalPlayer,gameObject);RequestSerialization();}
 public override void OnDeserialization(){got=true;Show();}
 private void Show(){
  if(petalsOn){if(!petals.isPlaying)petals.Play();}else if(petals.isPlaying)petals.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  petalLabel.text=petalsOn?"PETALS  /  ON":"PETALS  /  OFF";
  if(bloomVolume.activeSelf!=bloomOn)bloomVolume.SetActive(bloomOn);
  bloomLabel.text=bloomOn?"BLOOM  /  ON":"BLOOM  /  OFF";
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
