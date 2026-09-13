using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

// Personal comfort controls: deliberately local, without network traffic or Update loops.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraControls : UdonSharpBehaviour
{
    public Material backgroundMaterial;
    public Material poleMaterial;
    public Material sparkleMaterial;
    public ParticleSystem sparkles;
    public AudioSource music;
    public Slider backgroundSlider;
    public Slider sparkleSlider;
    public Slider volumeSlider;
    public Text musicLabel;
    public AstraGlitterControls glitter;
    public void SetBackground()
    {
        Color c = Color.Lerp(new Color(0.008f,0.005f,0.025f), new Color(0.08f,0.04f,0.15f), backgroundSlider.value);
        backgroundMaterial.SetColor("_Color", c);
        poleMaterial.SetColor("_VoidColor", c);
        RenderSettings.fogColor = c;
    }
    public void SetSparkles()
    {
        if(glitter != null) { glitter.ApplyDensity(); return; }
        var emission = sparkles.emission;
        emission.rateOverTime = sparkleSlider.value * 22f;
        if (sparkleSlider.value <= 0) sparkles.Clear();
    }
    public void SetVolume() { music.volume = volumeSlider.value * 0.3f; }
    public void ToggleMusic()
    {
        if (music.isPlaying) { music.Stop(); musicLabel.text = "MUSIC  /  OFF"; }
        else if (music.clip != null) { music.Play(); musicLabel.text = "MUSIC  /  ON"; }
    }
}
