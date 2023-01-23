using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ErrorPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public Color backgroundColor;
    public string message;

    private Image backgroundImage;
    private Coroutine moveCoroutine;
    private bool moving = false;


    private IEnumerator MoveToPositionCoroutine(Vector2 startPosition, Vector2 targetPosition, float transitionTime)
    {
        if(transitionTime <= 0)
        {
            transform.localPosition = targetPosition;
            yield break;
        }

        moving = true;

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / transitionTime;

            Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, t);
            transform.localPosition = newPosition;

            yield return null;
        }
        transform.localPosition = targetPosition;
        moving = false;
    }


    public void MoveToPosition(Vector2 startPosition, Vector2 targetPosition, float transitionTime)
    {
        if(moving)
        {
            StopCoroutine(moveCoroutine);
            moving = false;
        }

        moveCoroutine = StartCoroutine(MoveToPositionCoroutine(startPosition, targetPosition, transitionTime));
    }


    public void Close()
    {
        ErrorHandler.Instance.RemovePopup(this);
    }


    private void OnEnable()
    {
        backgroundImage = GetComponent<Image>();

        backgroundImage.color = backgroundColor;
        messageText.text = message;
    }
}