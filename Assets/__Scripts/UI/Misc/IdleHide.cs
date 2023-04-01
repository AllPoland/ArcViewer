using System.Collections;
using UnityEngine;

public class IdleHide : MonoBehaviour
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

    [SerializeField] private GameObject target;
    [SerializeField] private Vector2 enabledPos;
    [SerializeField] private Vector2 disabledPos;
    [SerializeField] private float transitionTime;
    [SerializeField] private bool disableOnHide = true;

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
                target.SetActive(false || !disableOnHide);
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
            target.SetActive(false || !disableOnHide);
        }

        moving = false;
    }


    public void ShowElement()
    {
        if(moving)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveElementCoroutine(targetTransform.anchoredPosition, enabledPos, true));
    }


    public void HideElement()
    {
        if(forceEnable)
        {
            return;
        }

        if(moving)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveElementCoroutine(targetTransform.anchoredPosition, disabledPos, false));
    }


    private void OnEnable()
    {
        if(!targetTransform)
        {
            targetTransform = target.GetComponent<RectTransform>();
        }

        UserIdleDetector.OnUserActive += ShowElement;
        UserIdleDetector.OnUserIdle += HideElement;
    }


    private void OnDisable()
    {
        UserIdleDetector.OnUserActive -= ShowElement;
        UserIdleDetector.OnUserIdle -= HideElement;
    }
}
