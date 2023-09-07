using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : MonoBehaviour
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

    [SerializeField] protected Image target;
    [SerializeField] protected Color enabledColor;
    [SerializeField] protected Color disabledColor;
    [SerializeField] protected float transitionTime;
    [SerializeField] protected bool disableOnHide = true;

    private bool fading;
    private Coroutine fadeCoroutine;


    private IEnumerator FadeElementCoroutine(Color startColor, Color targetColor, bool activate)
    {
        if(transitionTime <= 0)
        {
            target.color = targetColor;
            if(!activate)
            {
                target.gameObject.SetActive(!disableOnHide);
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
            target.gameObject.SetActive(!disableOnHide);
        }

        fading = false;
    }


    public void ShowElement()
    {
        if(!gameObject.activeInHierarchy)
        {
            return;
        }

        if(fading)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeElementCoroutine(target.color, enabledColor, true));
    }


    public void HideElement()
    {
        if(forceEnable || !gameObject.activeInHierarchy)
        {
            return;
        }

        if(fading)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeElementCoroutine(target.color, disabledColor, false));
    }


    public void ForceShowElement()
    {
        if(fading)
        {
            StopCoroutine(fadeCoroutine);
            fading = false;
        }

        target.color = enabledColor;
        target.gameObject.SetActive(true);
    }


    public void ForceHideElement()
    {
        if(fading)
        {
            StopCoroutine(fadeCoroutine);
            fading = false;
        }

        target.color = disabledColor;
        target.gameObject.SetActive(!disableOnHide);
    }
}