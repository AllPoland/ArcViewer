using UnityEngine;

public class FreecamController : MonoBehaviour
{
    public static bool ControlsEnabled { get; private set; }

    private float verticalRotation;
    private float horizontalRotation;


    private void UpdateInput()
    {
        Vector2 delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        delta *= SettingsManager.GetFloat("freecamsensitivity");

        verticalRotation = Mathf.Clamp(verticalRotation - delta.y, -90f, 90f);
        horizontalRotation += delta.x;

        transform.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);

        const float movementSpeed = 4f;
        const float fastMovementSpeed = 8f;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMovementSpeed : movementSpeed;
        float movementAmount = currentSpeed * Time.deltaTime;

        Vector3 movement = Vector3.zero;

        movement += transform.forward * Input.GetAxis("Vertical") * movementAmount;
        movement += transform.right * Input.GetAxis("Horizontal") * movementAmount;
        movement += transform.up * Input.GetAxis("UpDown") * movementAmount;

        transform.position = transform.position + movement;
    }


    private static void SetControlsEnabled(bool enableControls)
    {
        Cursor.lockState = enableControls ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enableControls;

        ControlsEnabled = enableControls;
    }


    private void Update()
    {
        if(ControlsEnabled)
        {
            if(!Input.GetMouseButton(1))
            {
                //Right click was released
                SetControlsEnabled(false);
            }
            else
            {
                //Right click is still held, check for inputs
                UpdateInput();
            }
        }
        else if(Input.GetMouseButtonDown(1) && !UserIdleDetector.MouseOnUI && UserIdleDetector.MouseOnScreen())
        {
            //Right click is pressed
            SetControlsEnabled(true);
        }
    }


    private void OnEnable()
    {
        verticalRotation = transform.eulerAngles.x % 360;
        horizontalRotation = transform.eulerAngles.y % 360;

        if(verticalRotation > 180)
        {
            verticalRotation -= 360;
        }

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);

        //Force enable controls if right mouse button is already pressed
        SetControlsEnabled(Input.GetMouseButton(1));
    }


    private void OnDisable()
    {
        SetControlsEnabled(false);
    }
}