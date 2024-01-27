using UnityEngine;

public class ClearCacheButton : MonoBehaviour
{
    public void ClearCache()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        CacheManager.ClearCache();
#endif
    }
}