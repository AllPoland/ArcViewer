using UnityEngine;
using UnityEngine.UI;

public class ScrollBarReset : MonoBehaviour
{
    [SerializeField] float defaultValue;

    private Scrollbar scrollbar;

    private void OnEnable()
    {
        if(!scrollbar)
        {
            scrollbar = GetComponent<Scrollbar>();
        }

        scrollbar.value = defaultValue;
    }
}