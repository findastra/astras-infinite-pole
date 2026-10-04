using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
// One menu, four tabs (Claude, 2026-10-03). WORLD / SKY / GLITTER / MAGIC. Which tab you are on is yours alone,
// so it is not synced - the switches themselves still share whatever they shared before.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraMenuTabs : UdonSharpBehaviour
{
    public GameObject[] pages;
    public Image[] tabBacks;
    public Text[] tabLabels;
    public Color onBack = new Color(.42f, .12f, .36f), offBack = new Color(.12f, .035f, .13f);
    public Color onText = new Color(1f, .92f, 1f), offText = new Color(.72f, .55f, .68f);
    private void Start() { Show(0); }
    public void Tab0() { Show(0); }
    public void Tab1() { Show(1); }
    public void Tab2() { Show(2); }
    public void Tab3() { Show(3); }
    private void Show(int i)
    {
        for (int k = 0; k < pages.Length; k++) if (pages[k] != null && pages[k].activeSelf != (k == i)) pages[k].SetActive(k == i);
        for (int k = 0; k < tabBacks.Length; k++) if (tabBacks[k] != null) tabBacks[k].color = k == i ? onBack : offBack;
        for (int k = 0; k < tabLabels.Length; k++) if (tabLabels[k] != null) tabLabels[k].color = k == i ? onText : offText;
    }
}
