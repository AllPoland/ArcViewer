using System.Collections;
using UnityEngine;

public class UIHide : MonoBehaviour
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

    [SerializeField] protected GameObject target;
    [SerializeField] protected Vector2 enabledPos;
    [SerializeField] protected Vector2 disabledPos;
    [SerializeField] protected float transitionTime;
    [SerializeField] protected bool disableOnHide = true;

    private bool moving;
    private Coroutine moveCoroutine;
    private RectTransform targetTransform;


    private IEnumerator MoveElementCoroutine(Vector2 startPos, Vector2 targetPos, bool activate)
    {
        if(transitionTime <= 0)
        {
            targetTransform.anchoredPosition = targetPos;
            if(!activate)
            {
                target.SetActive(!disableOnHide);
            }

            yield break;
        }

        moving = true;

        if(activate)
        {
            target.SetActive(true);
        }

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / transitionTime;
            targetTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }
        targetTransform.anchoredPosition = targetPos;

        if(!activate)
        {
            target.SetActive(!disableOnHide);
        }

        moving = false;
    }


    public void ShowElement()
    {
        if(!gameObject.activeInHierarchy)
        {
            return;
        }

        if(moving)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveElementCoroutine(targetTransform.anchoredPosition, enabledPos, true));
    }


    public void HideElement()
    {
        if(forceEnable || !gameObject.activeInHierarchy)
        {
            return;
        }

        if(moving)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveElementCoroutine(targetTransform.anchoredPosition, disabledPos, false));
    }


    public void ForceShowElement()
    {
        if(moving)
        {
            StopCoroutine(moveCoroutine);
            moving = false;
        }

        targetTransform.anchoredPosition = enabledPos;
        target.SetActive(true);
    }


    public void ForceHideElement()
    {
        if(moving)
        {
            StopCoroutine(moveCoroutine);
            moving = false;
        }

        targetTransform.anchoredPosition = disabledPos;
        target.SetActive(!disableOnHide);
    }


    private void Awake()
    {
        targetTransform = target.GetComponent<RectTransform>();
        if(!targetTransform)
        {
            Debug.LogError($"The UI Hide target {target.name} must have a RectTransform component!");
            enabled = false;
        }
    }
}
