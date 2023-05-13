using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ErrorHandler : MonoBehaviour
{
    public static ErrorHandler Instance { get; private set; }

    [SerializeField] private GameObject errorPopup;
    [SerializeField] private float popupHeight;
    [SerializeField] private float transitionTime;
    [SerializeField] private Color errorColor;
    [SerializeField] private Color warningColor;
    [SerializeField] private Color notificationColor;
    [SerializeField] private int maxPopups;
    [SerializeField] private float messageLifeSpan;

    private List<ErrorPopup> activePopups = new List<ErrorPopup>();
    private List<QueuedPopup> queuedPopups = new List<QueuedPopup>();


    public void ShowPopup(ErrorType errorType, string message)
    {
        if(activePopups.Any(x => x.message == message))
        {
            //Don't display duplicate popups
            return;
        }

        GameObject popupObject = Instantiate(errorPopup, transform, false);
        popupObject.SetActive(false);

        ErrorPopup newPopup = popupObject.GetComponent<ErrorPopup>();
        newPopup.message = message;
        newPopup.lifeTime = messageLifeSpan;

        //Position the new popup directly above the screen
        newPopup.rectTransform.anchoredPosition = new Vector2(0, popupHeight);

        switch(errorType)
        {
            case ErrorType.Error:
                newPopup.backgroundColor = errorColor;
                break;
            case ErrorType.Warning:
                newPopup.backgroundColor = warningColor;
                break;
            case ErrorType.Notification:
                newPopup.backgroundColor = notificationColor;
                break;
            default:
                newPopup.backgroundColor = Color.black;
                break;
        }

        popupObject.SetActive(true);
        activePopups.Add(newPopup);

        UpdatePopups();
    }


    public void RemovePopup(ErrorPopup popup)
    {
        Destroy(popup.gameObject);
        activePopups.Remove(popup);

        UpdatePopups();
    }


    public void UpdatePopups()
    {
        if(activePopups.Count == 0)
        {
            return;
        }
        if(activePopups.Count > maxPopups)
        {
            RemovePopup(activePopups[0]);
            return;
        }

        float currentY = 0;
        foreach(ErrorPopup popup in activePopups)
        {
            if(popup.rectTransform.anchoredPosition.y == currentY)
            {
                //This popup doesn't need to be moved
                currentY += popupHeight;
                continue;
            }

            Vector2 targetPosition = new Vector2(0, currentY);
            if(popup.rectTransform.anchoredPosition.y < currentY)
            {
                //Popup needs to be moved up, but should do so instantly
                popup.MoveToPosition(popup.rectTransform.anchoredPosition, targetPosition, 0);
            }
            else
            {
                //Popup needs to be moved down, and should transition smoothly
                Vector2 startPosition = new Vector2(0, currentY + popupHeight);
                popup.MoveToPosition(startPosition, targetPosition, transitionTime);
            }

            currentY += popupHeight;
        }
    }


    public void QueuePopup(ErrorType errorType, string message)
    {
        QueuedPopup popup = new QueuedPopup
        {
            ErrorType = errorType,
            Message = message
        };

        queuedPopups.Add(popup);
    }


    private void Update()
    {
        if(queuedPopups.Count > 0)
        {
            foreach(QueuedPopup popup in queuedPopups)
            {
                ShowPopup(popup.ErrorType, popup.Message);
            }

            queuedPopups.Clear();
        }
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


    //Test methods do not use
    public void SendError()
    {
        ShowPopup(ErrorType.Error, "Gm");
    }
    public void SendWarning()
    {
        ShowPopup(ErrorType.Warning, "Gn");
    }
    public void SendNotification()
    {
        ShowPopup(ErrorType.Notification, "Good evening my good samaritan. I'd like to tell you a story of medium-to-long-length about a very odd situation I encountered in my youth. It all began with a pretzel, but not just any pretzel, mind you. This was a pretzel that would change the course of my entire life, and, by extension, the history of the universe as we know it.");
    }


    private struct QueuedPopup
    {
        public ErrorType ErrorType;
        public string Message;
    }
}


public enum ErrorType
{
    Error,
    Warning,
    Notification
}