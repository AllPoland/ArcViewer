using UnityEngine;

public class DisableWebGL : MonoBehaviour
{
    [SerializeField] private bool enableWebGL;
    [SerializeField] private bool enableDesktop;


    private void OnEnable()
    {
#if UNITY_WEBGL
        if(enableWebGL) return;
#else
        if(enableDesktop) return;
#endif
        gameObject.SetActive(false);
    }
}