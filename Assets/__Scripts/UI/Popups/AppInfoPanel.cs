using UnityEngine;

public class AppInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject openInfoButton;


    private void OnEnable()
    {
        openInfoButton.SetActive(false);
    }


    private void OnDisable()
    {
        openInfoButton.SetActive(true);
    }
}