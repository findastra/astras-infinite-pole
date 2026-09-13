using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraAtmosphere : UdonSharpBehaviour
{
    public Material[] skies;
    public Material[] glitter;
    public Text skyLabel, motionLabel;
    public Slider exposure;
    private int selectedSky;
    private bool evolving = true;
    private void Start() { SelectSky(0); ApplyMotion(); }
    public void Nebula() { SelectSky(0); }
    public void Aurora() { SelectSky(1); }
    public void RoseDusk() { SelectSky(2); }
    public void Midnight() { SelectSky(3); }
    public void QuietVoid() { SelectSky(4); }
    public void SunsetSky() { SelectSky(5); }
    public void MoonlitSky() { SelectSky(6); }
    private void SelectSky(int index) {
        selectedSky=index;RenderSettings.skybox=skies[index];ApplyExposure();
        string[] names={"VELVET NEBULA","ARCTIC AURORA","ROSE DUSK","MIDNIGHT STARS","QUIET VOID","BELFAST SUNSET","MOONLIT SKY"};
        skyLabel.text=names[index];
    }
    public void ApplyExposure() {
        if(selectedSky!=4)skies[selectedSky].SetFloat("_Exposure",Mathf.Lerp(0.15f,1.4f,exposure.value));
    }
    public void ToggleEvolution() { evolving=!evolving;ApplyMotion(); }
    private void ApplyMotion() {
        for(int i=0;i<glitter.Length;i++) {
            glitter[i].SetFloat("_DriftSpeed",evolving?0.004f:0);
            glitter[i].SetFloat("_MorphSpeed",evolving?0.007f+i*0.0004f:0);
        }
        motionLabel.text=evolving?"SLOW EVOLUTION  /  ON":"SLOW EVOLUTION  /  OFF";
    }
}
