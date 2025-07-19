using UnityEngine;

public class TunnelLaserController : RotatingLaserHandler
{
    [Space]
    [SerializeField] private bool rotateClockwiseDefault;
    [SerializeField] private float step = 5f;

    private bool laserModeParity = false;


    public void UpdateRingRotations(RingRotationEventArgs eventArgs)
    {
        if(eventArgs.affectBigRings != eventArgs.affectSmallRings)
        {
            //This event is only meant to update ring positions, and will give inaccurate parity info
            return;
        }

        if(eventArgs.currentEventIndex < 0 || eventArgs.currentEventIndex >= eventArgs.events.Count)
        {
            laserModeParity = false;
            return;
        }

        laserModeParity = eventArgs.events[eventArgs.currentEventIndex].Parity;
    }


    public override void UpdateLaserRotations(LaserSpeedEvent laserSpeedEvent, LightEventType type)
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
            float angle;
            if(laserModeParity)
            {
                //The laser mode is set to non-chaotic, so use the step mode
                angle = laserSpeedEvent.GetLaserRotationWithStep(TimeManager.CurrentTime, rotateClockwiseDefault, step, i);
            }
            else angle = laserSpeedEvent.GetLaserRotation(TimeManager.CurrentTime, i);

            SetLaserRotation(targets[i], angle, defaultRotations[i]);
        }
    }


    protected override void Start()
    {
        RingManager.OnRingRotationsChanged += UpdateRingRotations;
        base.Start();
    }


    protected override void OnDestroy()
    {
        RingManager.OnRingRotationsChanged -= UpdateRingRotations;
        base.OnDestroy();
    }
}