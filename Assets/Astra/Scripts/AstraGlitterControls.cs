using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

// Glitter effects, density and colours. 2026-09-26 (Claude): shared with everyone in the instance; late joiners get the current look.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AstraGlitterControls : UdonSharpBehaviour
{
    public ParticleSystem[] effects;
    public Material[] materials;
    public float[] maximumRates;
    public Toggle[] effectToggles;
    public Slider densitySlider, hueA, hueB, saturation, brightness, twinkle;
    public Image swatchA, swatchB;
    public Text densityLabel;
    [UdonSynced] private bool[] effectOn = new bool[0];
    [UdonSynced] private float density, hA, hB, sat, bri, twk;
    private bool got, started, pushing;
    private void Start()
    {
        if (!got) { Read(); if (Networking.IsOwner(gameObject)) RequestSerialization(); }
        started = true; ShowLook(); ShowDensity();
    }
    private void Read()
    {
        effectOn = new bool[effectToggles.Length];
        for (int i = 0; i < effectToggles.Length; i++) effectOn[i] = effectToggles[i].isOn;
        density = densitySlider.value; hA = hueA.value; hB = hueB.value; sat = saturation.value; bri = brightness.value; twk = twinkle.value;
    }
    private bool Near(float a, float b) { return Mathf.Abs(a - b) < .0001f; }
    private bool Differs()
    {
        if (effectOn.Length != effectToggles.Length) return true;
        for (int i = 0; i < effectToggles.Length; i++) if (effectToggles[i].isOn != effectOn[i]) return true;
        return !Near(density, densitySlider.value) || !Near(hA, hueA.value) || !Near(hB, hueB.value) || !Near(sat, saturation.value) || !Near(bri, brightness.value) || !Near(twk, twinkle.value);
    }
    private void Share() { if (!started || pushing || !Differs()) return; Read(); Networking.SetOwner(Networking.LocalPlayer, gameObject); RequestSerialization(); }
    public override void OnDeserialization()
    {
        got = true; pushing = true;
        for (int i = 0; i < effectToggles.Length && i < effectOn.Length; i++) effectToggles[i].SetIsOnWithoutNotify(effectOn[i]);
        densitySlider.SetValueWithoutNotify(density); hueA.SetValueWithoutNotify(hA); hueB.SetValueWithoutNotify(hB);
        saturation.SetValueWithoutNotify(sat); brightness.SetValueWithoutNotify(bri); twinkle.SetValueWithoutNotify(twk);
        pushing = false; ShowLook(); ShowDensity();
    }
    public void ApplyDensity() { Share(); ShowDensity(); }
    public void ApplyLook() { Share(); ShowLook(); }
    private void ShowDensity()
    {
        float d = Mathf.Clamp01(densitySlider.value);
        for (int i = 0; i < effects.Length; i++)
        {
            bool enabledEffect = effectToggles[i].isOn && d > 0;
            var emission = effects[i].emission;
            emission.rateOverTime = maximumRates[i] * d;
            if (enabledEffect) { if (!effects[i].isPlaying) effects[i].Play(); }
            else effects[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        densityLabel.text = "DENSITY  /  " + Mathf.RoundToInt(d * 100) + "%";
    }
    private void ShowLook()
    {
        Color a = Color.HSVToRGB(hueA.value, saturation.value, 1);
        Color b = Color.HSVToRGB(hueB.value, saturation.value, 1);
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor("_ColorA", a); materials[i].SetColor("_ColorB", b);
            materials[i].SetFloat("_Brightness", brightness.value * 2);
            materials[i].SetFloat("_Twinkle", twinkle.value);
        }
        swatchA.color = a; swatchB.color = b;
    }
    public void AllOn() { for (int i = 0; i < effectToggles.Length; i++) effectToggles[i].SetIsOnWithoutNotify(true); ApplyDensity(); }
    public void AllOff() { for (int i = 0; i < effectToggles.Length; i++) effectToggles[i].SetIsOnWithoutNotify(false); ApplyDensity(); }
    public void Low() { densitySlider.SetValueWithoutNotify(0.12f); ApplyDensity(); }
    public void Lush() { densitySlider.SetValueWithoutNotify(0.65f); ApplyDensity(); }
    public void Maximum() { densitySlider.SetValueWithoutNotify(1); ApplyDensity(); }
    public void Aurora() { hueA.SetValueWithoutNotify(0.74f); hueB.SetValueWithoutNotify(0.49f); saturation.SetValueWithoutNotify(0.72f); ApplyLook(); }
    public void RoseGold() { hueA.SetValueWithoutNotify(0.94f); hueB.SetValueWithoutNotify(0.12f); saturation.SetValueWithoutNotify(0.65f); ApplyLook(); }
    public void Ice() { hueA.SetValueWithoutNotify(0.55f); hueB.SetValueWithoutNotify(0.64f); saturation.SetValueWithoutNotify(0.3f); ApplyLook(); }
    public void Sunset() { hueA.SetValueWithoutNotify(0.02f); hueB.SetValueWithoutNotify(0.82f); saturation.SetValueWithoutNotify(0.8f); ApplyLook(); }
}
