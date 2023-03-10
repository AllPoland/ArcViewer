using UnityEngine;
using UnityEngine.UI;

public class HotReloadButton : MonoBehaviour
{
    [SerializeField] private GameObject buttonObject;
    [SerializeField] private GameObject loadingSpin;
    [SerializeField] private string enabledTooltip;
    [SerializeField] private string disabledTooltip;

    private Image image;
    private Button button;
    private Tooltip tooltip;


    public void UpdateLoading(bool loading)
    {
        image.enabled = !loading;
        button.enabled = !loading;
        loadingSpin.SetActive(loading);
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    private void Update()
    {
        if(!HotReloader.Loading && !string.IsNullOrEmpty(HotReloader.loadedMapPath) && Input.GetButtonDown("Reload"))
        {
            button.onClick?.Invoke();
        }
    }
#endif


    private void OnEnable()
    {
        if(!image)
        {
            image = buttonObject.GetComponent<Image>();
        }
        if(!button)
        {
            button = buttonObject.GetComponent<Button>();
        }
        if(!tooltip)
        {
            tooltip = buttonObject.GetComponent<Tooltip>();
        }

        UpdateLoading(HotReloader.Loading);

        button.interactable = !string.IsNullOrEmpty(HotReloader.loadedMapPath);
        tooltip.Text = button.interactable ? enabledTooltip : disabledTooltip;

        HotReloader.OnLoadingChanged += UpdateLoading;
    }
}