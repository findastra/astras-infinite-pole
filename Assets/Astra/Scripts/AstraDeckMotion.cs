using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// DJ deck motion (Claude, board task C1). Cosmetic and local only: no networking, no physics, no allocations per frame.
// - Spins each assigned platter around its own local up axis, starting from its authored pose.
// - Optionally pulses assigned lights' intensity and a color property on assigned renderers (via one reused property block).
// Everything optional may be left empty. Skips missing or switched-off objects. Stops when this object is switched off.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraDeckMotion : UdonSharpBehaviour {
 [Tooltip("Platter transforms to spin (e.g. Turntable left / right).")] public Transform[] platters;
 [Tooltip("Degrees per second per platter; sign = direction. Missing entries use Default Spin.")] public float[] spinSpeeds;
 [Tooltip("Spin speed for platters without their own entry. 33 rpm is about 200.")] public float defaultSpin=90f;
 [Tooltip("Optional lights to pulse.")] public Light[] pulseLights;
 [Tooltip("Optional renderers whose color property pulses (e.g. edge glow).")] public Renderer[] pulseRenderers;
 [Tooltip("Color property on those renderers' shader, e.g. _Rim or _Color.")] public string colorProperty="_Rim";
 [Tooltip("Base color written to that property; brightness is scaled by the pulse.")] public Color pulseColor=new Color(2.2f,.7f,1.8f,1f);
 [Tooltip("Pulses per second.")] public float pulseRate=.9f;
 [Tooltip("0 = steady, 1 = fully on/off.")] [Range(0,1)] public float pulseDepth=.35f;

 private Quaternion[] basePose;
 private float[] baseIntensity;
 private MaterialPropertyBlock block;
 private bool ready;

 private void Start(){
  int n=platters==null?0:platters.Length;
  basePose=new Quaternion[n];
  for(int i=0;i<n;i++)if(platters[i]!=null)basePose[i]=platters[i].localRotation;
  int l=pulseLights==null?0:pulseLights.Length;
  baseIntensity=new float[l];
  for(int i=0;i<l;i++)if(pulseLights[i]!=null)baseIntensity[i]=pulseLights[i].intensity;
  block=new MaterialPropertyBlock();
  ready=true;
 }

 private void Update(){
  if(!ready)return;
  float t=(float)(Networking.GetServerTimeInSeconds()%3600.0); // same spin and pulse for everyone
  for(int i=0;i<basePose.Length;i++){
   Transform p=platters[i];
   if(p==null||!p.gameObject.activeInHierarchy)continue;
   float speed=(spinSpeeds!=null&&i<spinSpeeds.Length)?spinSpeeds[i]:defaultSpin;
   p.localRotation=basePose[i]*Quaternion.Euler(0f,(t*speed)%360f,0f);
  }
  float wave=1f-pulseDepth*(.5f+.5f*Mathf.Sin(t*pulseRate*6.2831853f));
  for(int i=0;i<baseIntensity.Length;i++){
   Light li=pulseLights[i];
   if(li!=null&&li.gameObject.activeInHierarchy)li.intensity=baseIntensity[i]*wave;
  }
  if(pulseRenderers!=null&&pulseRenderers.Length>0&&colorProperty!=null&&colorProperty.Length>0){
   Color c=pulseColor*wave;c.a=pulseColor.a;
   for(int i=0;i<pulseRenderers.Length;i++){
    Renderer r=pulseRenderers[i];
    if(r==null||!r.gameObject.activeInHierarchy)continue;
    r.GetPropertyBlock(block);block.SetColor(colorProperty,c);r.SetPropertyBlock(block);
   }
  }
 }

 // Put everything back to its authored state when switched off.
 private void OnDisable(){
  if(!ready)return;
  for(int i=0;i<basePose.Length;i++)if(platters[i]!=null)platters[i].localRotation=basePose[i];
  for(int i=0;i<baseIntensity.Length;i++)if(pulseLights[i]!=null)pulseLights[i].intensity=baseIntensity[i];
 }
}
