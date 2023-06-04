using UnityEngine;

public class StatsButton : MonoBehaviour
{
    private bool panelActive => DialogueHandler.Instance.statsPanel.activeInHierarchy;


    public void ToggleStatsPanel()
    {
        DialogueHandler.Instance.SetStatsPanelActive(!panelActive);
    }
}