using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ErrorPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public RectTransform rectTransform;
    public Color backgroundColor;
    public string message;
    public float lifeTime;

    private Image backgroundImage;
    private Coroutine moveCoroutine;
    private bool moving = false;


    private IEnumerator MoveToPositionCoroutine(Vector2 startPosition, Vector2 targetPosition, float transitionTime)
    {
        if(transitionTime <= 0)
        {
            rectTransform.anchoredPosition = targetPosition;
            yield break;
        }

        moving = true;

        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime / transitionTime;

            Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, t);
            rectTransform.anchoredPosition = newPosition;

            yield return null;
        }
        rectTransform.anchoredPosition = targetPosition;
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


    private void Update()
    {
        if(lifeTime <= 0)
        {
            Close();
            return;
        }

        lifeTime -= Time.deltaTime;
    }
}