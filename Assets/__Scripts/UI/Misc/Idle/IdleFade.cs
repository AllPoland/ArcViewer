using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IdleFade : MonoBehaviour
{
    private bool _forceEnable = false;
    public bool forceEnable
    {
        get => _forceEnable;
        set
        {
            _forceEnable = value;
            if(value)
            {
                ShowElement();
            }
        }
    }

    [SerializeField] private Image target;
    [SerializeField] private Color enabledColor;
    [SerializeField] private Color disabledColor;
    [SerializeField] private float transitionTime;
    [SerializeField] private bool disableOnHide = true;

    private bool fading;
    private Coroutine fadeCoroutine;


    private IEnumerator FadeElementCoroutine(Color startColor, Color targetColor, bool activate)
    {
        if(transitionTime <= 0)
        {
            target.color = targetColor;
            if(!activate)
            {
                target.gameObject.SetActive(false || !disableOnHide);
            }

            yield break;
        }

        fading = true;

        if(activate)
        {
            target.gameObject.SetActive(true);
        }

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / transitionTime;
            target.color = Color.Lerp(startColor, targetColor, t);

            yield return null;
        }
        target.color = targetColor;

        if(!activate)
        {
            target.gameObject.SetActive(false || !disableOnHide);
        }

        fading = false;
    }


    public void ShowElement()
    {
        if(fading)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeElementCoroutine(target.color, enabledColor, true));
    }


    public void HideElement()
    {
        if(forceEnable)
        {
            return;
        }

        if(fading)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeElementCoroutine(target.color, disabledColor, false));
    }


    private void OnEnable()
    {
        UserIdleDetector.OnUserActive += ShowElement;
        UserIdleDetector.OnUserIdle += HideElement;
    }


    private void OnDisable()
    {
        UserIdleDetector.OnUserActive -= ShowElement;
        UserIdleDetector.OnUserIdle -= HideElement;
    }
}
