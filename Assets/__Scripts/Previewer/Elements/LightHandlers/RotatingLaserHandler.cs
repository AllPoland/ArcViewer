using System.Collections.Generic;
using UnityEngine;

public class RotatingLaserHandler : MonoBehaviour
{
    [SerializeField] private List<LightHandler> targets;
    [SerializeField] private LightEventType eventType;
    [SerializeField] private RotationAxis rotationAxis;


    public void UpdateLaserRotations(LaserSpeedEvent laserSpeedEvent, LightEventType type)
    {
        if(type != eventType)
        {
            return;
        }

        if(laserSpeedEvent == null)
        {
            //This means there haven't been any speed events (or there aren't any)
            ResetRotations();
            return;
        }

        for(int i = 0; i < targets.Count; i++)
        {
            LightHandler target = targets[i];
            float angle = laserSpeedEvent.GetLaserRotation(TimeManager.CurrentTime, i);
            SetLaserRotation(target, angle);
        }
    }


    private void ResetRotations()
    {
        foreach(LightHandler target in targets)
        {
            SetLaserRotation(target, 0f);
        }
    }


    private void SetLaserRotation(LightHandler target, float angle)
    {
        target.transform.localEulerAngles = GetLaserEulerAngles(target.transform, angle);
    }


    private Vector3 GetLaserEulerAngles(Transform target, float rotationAngle)
    {
        Vector3 currentRotation = target.localEulerAngles;
        switch(rotationAxis)
        {
            case RotationAxis.X:
                return new Vector3(rotationAngle, currentRotation.y, currentRotation.z);
            default:
            case RotationAxis.Y:
                return new Vector3(currentRotation.x, rotationAngle, currentRotation.z);
            case RotationAxis.Z:
                return new Vector3(currentRotation.x, currentRotation.y, rotationAngle);
        }
    }


    private void OnEnable()
    {
        LightManager.OnLaserRotationsChanged += UpdateLaserRotations;
    }


    private void OnDisable()
    {
        LightManager.OnLaserRotationsChanged -= UpdateLaserRotations;
    }


    enum RotationAxis
    {
        X,
        Y,
        Z
    }
}