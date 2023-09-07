public class IdleHide : UIHide
{
    private void OnEnable()
    {
        UserIdleDetector.OnUserActive += ShowElement;
        UserIdleDetector.OnUserIdle += HideElement;
    }


    private void OnDisable()
    {
        UserIdleDetector.OnUserActive -= ShowElement;
        UserIdleDetector.OnUserIdle -= HideElement;
    }
}
