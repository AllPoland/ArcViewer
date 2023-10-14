using UnityEngine;

public class InputHide : UIHide
{
    [SerializeField] private bool forceShowWhenNotIdle = false;

    private bool idle => !forceShowWhenNotIdle || UserIdleDetector.userIdle;


    private void ForceSetVisible(bool visible)
    {
        if(visible || !idle)
        {
            ForceShowElement();
        }
        else ForceHideElement();
    }


    private void UpdateVisible(bool visible)
    {
        if(visible || !idle)
        {
            ShowElement();
        }
        else HideElement();
    }


    private void UserIdle() => UpdateVisible(UIHideInput.UIVisible);

    private void UserActive() => UpdateVisible(UIHideInput.UIVisible);


    private void OnEnable()
    {
        UIHideInput.OnUIVisibleChanged += UpdateVisible;
        ForceSetVisible(UIHideInput.UIVisible);

        if(forceShowWhenNotIdle)
        {
            UserIdleDetector.OnUserIdle += UserIdle;
            UserIdleDetector.OnUserActive += UserActive;
        }
    }


    private void OnDisable()
    {
        UIHideInput.OnUIVisibleChanged -= UpdateVisible;

        if(forceShowWhenNotIdle)
        {
            UserIdleDetector.OnUserIdle -= UserIdle;
            UserIdleDetector.OnUserActive -= UserActive;
        }
    }
}