using UnityEngine;
using TMPro;

public class AttachedFileHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fileText;

    private AttachedFile currentFile;


    public void SetFile(AttachedFile file)
    {
        fileText.text = $"{file.Name} | {file.Size} MB";
        currentFile = file;
    }


    public void OpenFile()
    {
        Application.OpenURL(currentFile.URL);
    }
}