using UnityEngine;
using TMPro;

public class ScoreIndicatorHandler : MonoBehaviour
{
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private SpriteRenderer iconRenderer;


    public void SetIconActive(bool useIcon)
    {
        scoreText.enabled = !useIcon;
        iconRenderer.gameObject.SetActive(useIcon);
    }


    public void SetText(string text)
    {
        scoreText.text = text;
    }


    public void SetColor(Color color)
    {
        scoreText.color = color;
        iconRenderer.color = color;
    }
}