using UnityEngine;

public class AppInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject mapSelect;


    public void SetMapSelect()
    {
        mapSelect.SetActive(true);
        infoPanel.SetActive(false);
    }


    public void SetInfoScreen()
    {
        if(MapLoader.Loading || UIStateManager.CurrentState != UIState.MapSelection)
        {
            return;
        }

        infoPanel.SetActive(true);
        mapSelect.SetActive(false);
    }


    private void Update()
    {
        if(Input.GetButtonDown("Cancel") && infoPanel.activeInHierarchy)
        {
            SetMapSelect();
        }
    }
}