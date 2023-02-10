using UnityEngine;
using TMPro;

public class VersionText : MonoBehaviour
{
    [SerializeField] private string prefixText;


    private void OnEnable()
    {
        TextMeshProUGUI versionText = GetComponent<TextMeshProUGUI>();

        versionText.text = prefixText + Application.version;
    }
}