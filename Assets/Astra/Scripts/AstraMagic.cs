using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraMagic : UdonSharpBehaviour
{
    public Material[] materials;
    public ParticleSystem[] trails;
    public Toggle trailToggle, reactionToggle;
    public Slider flowSlider;
    public ParticleSystem clouds; public Material cloudMaterial; public Toggle cloudToggle;
    public Text patternLabel; public Material poleMaterial; public Toggle translucentToggle; public Slider poleOpacity,poleSparkle;
    public void ApplyPole(){poleMaterial.SetFloat("_Opacity",translucentToggle.isOn?Mathf.Lerp(.08f,.75f,poleOpacity.value):1);poleMaterial.SetFloat("_Sparkle",poleSparkle.value*4);}
    private VRCPlayerApi player;
    private Vector3 lastPosition, wake;
    private float nextUpdate;
    private void Start() { player=Networking.LocalPlayer; NewPattern(); ApplyFlow(); ApplyTrails(); ApplyClouds(); ApplyPole(); }
    public void NewPattern() {
        float seed=Random.Range(0f,1000f);
        for(int i=0;i<materials.Length;i++) materials[i].SetFloat("_FlowSeed",seed+i*3.71f);
        patternLabel.text="SPELL PATTERN  /  "+Mathf.FloorToInt(seed);

    }
    public void ApplyClouds() {if(cloudToggle.isOn)clouds.Play();else clouds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    public void PeachClouds() {cloudMaterial.SetColor("_ColorA",new Color(1,.32f,.16f));cloudMaterial.SetColor("_ColorB",new Color(1,.65f,.38f));}
    public void RoseClouds() {cloudMaterial.SetColor("_ColorA",new Color(1,.16f,.47f));cloudMaterial.SetColor("_ColorB",new Color(.55f,.3f,1));}
    public void TwilightClouds() {cloudMaterial.SetColor("_ColorA",new Color(.2f,.5f,1));cloudMaterial.SetColor("_ColorB",new Color(.8f,.3f,1));}
    public void ApplyFlow() { for(int i=0;i<materials.Length;i++) materials[i].SetFloat("_Flow",flowSlider.value); }
    public void ApplyTrails() {
        for(int i=0;i<trails.Length;i++) {
            if(trailToggle.isOn) trails[i].Play();
            else trails[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    private void LateUpdate() {
        if(!Utilities.IsValid(player)) { player=Networking.LocalPlayer; return; }
        Vector3 feet=player.GetPosition();
        Vector3 head=player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        Vector3 left=player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
        Vector3 right=player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
        Vector3 torso=Vector3.Lerp(feet,head,.52f);
        if(Vector3.Distance(feet,lastPosition)>3f) for(int i=0;i<trails.Length;i++)trails[i].Clear();
        lastPosition=feet;
        trails[0].transform.position=left;trails[1].transform.position=right;trails[2].transform.position=torso;
        if(Time.time<nextUpdate)return;
        nextUpdate=Time.time+.05f;
        wake=Vector3.Lerp(wake,Vector3.ClampMagnitude(player.GetVelocity(),3f),.2f);
        for(int i=0;i<materials.Length;i++) {
            materials[i].SetVector("_Body",new Vector4(torso.x,torso.y,torso.z,Mathf.Max(.4f,Vector3.Distance(feet,head)*.45f)));
            materials[i].SetVector("_HandL",new Vector4(left.x,left.y,left.z,0));
            materials[i].SetVector("_HandR",new Vector4(right.x,right.y,right.z,0));
            materials[i].SetVector("_Wake",new Vector4(wake.x,wake.y,wake.z,0));
            materials[i].SetFloat("_React",reactionToggle.isOn?1:0);
        }
    }
}


