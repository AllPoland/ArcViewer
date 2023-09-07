public class InputHide : UIHide
{
    private void ForceSetVisible(bool visible)
    {
        if(visible)
        {
            ForceShowElement();
        }
        else ForceHideElement();
    }


    private void UpdateVisible(bool visible)
    {
        if(visible)
        {
            ShowElement();
        }
        else HideElement();
    }


    private void OnEnable()
    {
        UIHideInput.OnUIVisibleChanged += UpdateVisible;
        ForceSetVisible(UIHideInput.UIVisible);
    }


    private void OnDisable()
    {
        UIHideInput.OnUIVisibleChanged -= UpdateVisible;
    }
}