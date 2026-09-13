using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraGlitterControls : UdonSharpBehaviour
{
    public ParticleSystem[] effects;
    public Material[] materials;
    public float[] maximumRates;
    public Toggle[] effectToggles;
    public Slider densitySlider, hueA, hueB, saturation, brightness, twinkle;
    public Image swatchA, swatchB;
    public Text densityLabel;
    private void Start() { ApplyLook(); ApplyDensity(); }
    public void ApplyDensity()
    {
        float density = Mathf.Clamp01(densitySlider.value);
        for(int i=0;i<effects.Length;i++)
        {
            bool enabledEffect=effectToggles[i].isOn && density>0;
            var emission=effects[i].emission;
            emission.rateOverTime=maximumRates[i]*density;
            if(enabledEffect) {if(!effects[i].isPlaying) effects[i].Play();}
            else effects[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        densityLabel.text="DENSITY  /  "+Mathf.RoundToInt(density*100)+"%";
    }
    public void ApplyLook()
    {
        Color a=Color.HSVToRGB(hueA.value,saturation.value,1);
        Color b=Color.HSVToRGB(hueB.value,saturation.value,1);
        for(int i=0;i<materials.Length;i++)
        {
            materials[i].SetColor("_ColorA",a);materials[i].SetColor("_ColorB",b);
            materials[i].SetFloat("_Brightness",brightness.value*2);
            materials[i].SetFloat("_Twinkle",twinkle.value);
        }
        swatchA.color=a;swatchB.color=b;
    }
    public void AllOn() {for(int i=0;i<effectToggles.Length;i++)effectToggles[i].SetIsOnWithoutNotify(true);ApplyDensity();}
    public void AllOff() {for(int i=0;i<effectToggles.Length;i++)effectToggles[i].SetIsOnWithoutNotify(false);ApplyDensity();}
    public void Low() {densitySlider.SetValueWithoutNotify(0.12f);ApplyDensity();}
    public void Lush() {densitySlider.SetValueWithoutNotify(0.65f);ApplyDensity();}
    public void Maximum() {densitySlider.SetValueWithoutNotify(1);ApplyDensity();}
    public void Aurora() {hueA.SetValueWithoutNotify(0.74f);hueB.SetValueWithoutNotify(0.49f);saturation.SetValueWithoutNotify(0.72f);ApplyLook();}
    public void RoseGold() {hueA.SetValueWithoutNotify(0.94f);hueB.SetValueWithoutNotify(0.12f);saturation.SetValueWithoutNotify(0.65f);ApplyLook();}
    public void Ice() {hueA.SetValueWithoutNotify(0.55f);hueB.SetValueWithoutNotify(0.64f);saturation.SetValueWithoutNotify(0.3f);ApplyLook();}
    public void Sunset() {hueA.SetValueWithoutNotify(0.02f);hueB.SetValueWithoutNotify(0.82f);saturation.SetValueWithoutNotify(0.8f);ApplyLook();}
}
