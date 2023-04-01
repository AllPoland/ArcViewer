using UnityEngine;

public class ClearCacheButton : MonoBehaviour
{
    public void ClearCache()
    {
        FileCache.ClearCache();
    }
}