using UnityEngine;
using UnityEngine.UI;

public class CoverImageHandler : MonoBehaviour
{
    public static CoverImageHandler Instance { get; private set; }

    [SerializeField] private Sprite defaultCoverImage;
    [SerializeField] private Image image;


    public void SetImageFromData(byte[] data)
    {
        Texture2D newTexture = new Texture2D(2, 2);
        bool loaded = newTexture.LoadImage(data);

        if(!loaded)
        {
            Debug.LogWarning("Unable to load cover image!");
            ErrorHandler.Instance?.DisplayPopup(ErrorType.Warning, "Unable to load cover image!");
            UpdateImage(defaultCoverImage);
        }
        else
        {
            //Why does this method take SO MANY ARGUMENTS
            UpdateImage(Sprite.Create(newTexture,
                new Rect(0, 0, newTexture.width, newTexture.height), new Vector2(0.5f, 0.5f), 100));
        }
    }


    public void SetDefaultImage()
    {
        UpdateImage(defaultCoverImage);
    }


    public void UpdateImage(Sprite newImage)
    {
        if(newImage.rect.width != newImage.rect.height)
        {
            Debug.LogWarning("Cover image isn't a square!");
            ErrorHandler.Instance?.DisplayPopup(ErrorType.Warning, "The cover image isn't a square!");
            
            newImage = defaultCoverImage;
        }

        image.sprite = newImage;
    }


    private void OnEnable()
    {
        if(Instance != null && Instance != this)
        {
            this.enabled = false;
            return;
        }

        Instance = this;
    }
}