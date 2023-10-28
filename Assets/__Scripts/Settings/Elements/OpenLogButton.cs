using UnityEngine;

public class OpenLogButton : MonoBehaviour
{
    public void OpenLog()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        Application.OpenURL(LogLocationHandler.LogPath);
#else
        Debug.LogWarning("Cannot open the log file in WebGL!");
#endif
    }
}