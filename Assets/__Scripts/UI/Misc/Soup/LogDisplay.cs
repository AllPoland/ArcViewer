using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private AttachedFileHandler attachedFileHandler;
    [SerializeField] private RectTransform content;


    public void SetLog(Log newLog)
    {
        logText.text = newLog.Text;

        if(!string.IsNullOrEmpty(newLog.File?.Name))
        {
            attachedFileHandler.gameObject.SetActive(true);
            attachedFileHandler.SetFile(newLog.File);
        }
        else attachedFileHandler.gameObject.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(logText.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)logText.transform.parent);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

        content.anchoredPosition = Vector2.zero;
    }
}


[Serializable]
public class Log
{
    public string Text;
    public AttachedFile File;
}


[Serializable]
public class AttachedFile
{
    public string Name;
    public float Size;
    public string URL;
}