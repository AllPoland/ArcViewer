using UnityEngine;

public class DownloadCancelButton : MonoBehaviour
{
    public void CancelDownload()
    {
        WebMapLoader.CancelDownload();
    }
}