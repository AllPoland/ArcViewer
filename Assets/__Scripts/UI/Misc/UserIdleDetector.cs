using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UserIdleDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static event Action OnUserIdle;
    public static event Action OnUserActive;

    [SerializeField] private float idleTimeout;

    private Vector2 previousPosition;
    private float idleTime;
    private bool mousedOver;
    private bool userIdle;


    private void CheckUserInput()
    {
        if((Vector2)Input.mousePosition != previousPosition || Input.anyKeyDown)
        {
            idleTime = idleTimeout;
            previousPosition = Input.mousePosition;

            if(userIdle)
            {
                SetUserActive();
            }
        }
        else if(idleTime <= 0 && !userIdle)
        {
            SetUserIdle();
        }
        else if(idleTime > 0)
        {
            idleTime -= Time.deltaTime;
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        mousedOver = true;
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        mousedOver = false;
    }


    private void SetUserIdle()
    {
        idleTime = 0;
        userIdle = true;

        OnUserIdle?.Invoke();
        Cursor.visible = !MouseOnScreen();
    }


    private void SetUserActive()
    {
        idleTime = idleTimeout;
        userIdle = false;

        OnUserActive?.Invoke();
        Cursor.visible = true;
    }


    private void Update()
    {
        if(UIStateManager.CurrentState == UIState.Previewer && !DialogueHandler.PopupActive)
        {
            if(MouseOnScreen())
            {
                if(mousedOver)
                {
                    //User isn't moused over UI
                    CheckUserInput();
                }
                else if(userIdle)
                {
                    //User is moused over UI
                    SetUserActive();
                }
            }
            else if(!userIdle)
            {
                //Cursor isn't on the screen, so idle should be default
                SetUserIdle();
            }
        }
        else if(userIdle)
        {
            //Not in a state where idling should be a thing
            SetUserActive();
        }
    }


    public static bool MouseOnScreen()
    {
        Vector2 mousePos = Input.mousePosition;
        return mousePos.x >= 0 && mousePos.y >= 0 && mousePos.x <= Screen.width && mousePos.y <= Screen.height;
    }
}