using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

// Comfort controls. 2026-09-26 (Claude): the background colour and sparkle amount are shared with everyone in the instance.
// Music on/off and music volume stay personal (they only change what you hear).
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
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
    [UdonSynced] private float background = -1, sparkleAmount = -1;
    private bool got, started;
    private void Start()
    {
        if (!got) { background = backgroundSlider.value; sparkleAmount = sparkleSlider.value; if (Networking.IsOwner(gameObject)) RequestSerialization(); }
        started = true;
    }
    private void Share() { if (!started) return; Networking.SetOwner(Networking.LocalPlayer, gameObject); RequestSerialization(); }
    public override void OnDeserialization()
    {
        got = true;
        if (background >= 0 && Mathf.Abs(backgroundSlider.value - background) > .0001f) { backgroundSlider.SetValueWithoutNotify(background); ShowBackground(); }
        if (sparkleAmount >= 0 && Mathf.Abs(sparkleSlider.value - sparkleAmount) > .0001f) { sparkleSlider.SetValueWithoutNotify(sparkleAmount); ShowSparkles(); }
    }
    public void SetBackground()
    {
        if (started && Mathf.Abs(backgroundSlider.value - background) > .0001f) { background = backgroundSlider.value; Share(); }
        ShowBackground();
    }
    public void SetSparkles()
    {
        if (started && Mathf.Abs(sparkleSlider.value - sparkleAmount) > .0001f) { sparkleAmount = sparkleSlider.value; Share(); }
        ShowSparkles();
    }
    private void ShowBackground()
    {
        Color c = Color.Lerp(new Color(0.008f,0.005f,0.025f), new Color(0.08f,0.04f,0.15f), backgroundSlider.value);
        backgroundMaterial.SetColor("_Color", c);
        poleMaterial.SetColor("_VoidColor", c);
        RenderSettings.fogColor = c;
    }
    private void ShowSparkles()
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
