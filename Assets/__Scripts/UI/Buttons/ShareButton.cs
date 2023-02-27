using UnityEngine;
using UnityEngine.UI;

public class ShareButton : MonoBehaviour
{
    private Button button;


    private void OnEnable()
    {
        if(!button)
        {
            button = GetComponent<Button>();
        }

        button.interactable = !string.IsNullOrEmpty(UrlArgHandler.LoadedMapID) || !string.IsNullOrEmpty(UrlArgHandler.LoadedMapURL);
    }
}