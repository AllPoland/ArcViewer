using System.IO;
using UnityEngine;

public class OpenLogButton : MonoBehaviour
{
    public void OpenLog()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        string logPath = Path.Combine(Application.persistentDataPath, "Player.log");
        Application.OpenURL(logPath);
#else
        Debug.LogWarning("Cannot open the log file in WebGL!");
#endif
    }
}