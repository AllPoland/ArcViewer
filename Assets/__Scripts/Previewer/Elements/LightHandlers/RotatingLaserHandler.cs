using System.Collections.Generic;
using UnityEngine;

public class RotatingLaserHandler : MonoBehaviour
{
    [SerializeField] private List<Transform> targets;
    [SerializeField] private LightEventType eventType;
    [SerializeField] private RotationAxis rotationAxis;

    private List<Vector3> defaultRotations = new List<Vector3>();


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
            float angle = laserSpeedEvent.GetLaserRotation(TimeManager.CurrentTime, i);
            SetLaserRotation(targets[i], angle, defaultRotations[i]);
        }
    }


    private void ResetRotations()
    {
        for(int i = 0; i < targets.Count; i++)
        {
            SetLaserRotation(targets[i], 0f, defaultRotations[i]);
        }
    }


    private void SetLaserRotation(Transform target, float angle, Vector3 defaultRotation)
    {
        Vector3 rotation = defaultRotation;
        switch(rotationAxis)
        {
            case RotationAxis.X:
                rotation.x = angle;
                break;
            case RotationAxis.Y:
                rotation.y = angle;
                break;
            case RotationAxis.Z:
                rotation.z = angle;
                break;
        }
        target.localEulerAngles = rotation;
    }


    private void Start()
    {
        LightManager.OnLaserRotationsChanged += UpdateLaserRotations;

        defaultRotations.Clear();
        foreach(Transform target in targets)
        {
            defaultRotations.Add(target.localEulerAngles);
        }
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