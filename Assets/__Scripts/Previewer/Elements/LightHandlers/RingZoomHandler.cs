using System.Collections.Generic;
using UnityEngine;

public class RingZoomHandler : MonoBehaviour
{
    [SerializeField] private float firstRingPos;

    private List<Transform> rings = new List<Transform>();


    public void UpdateRingZoom(float step)
    {
        for(int i = 0; i < rings.Count; i++)
        {
            Transform ring = rings[i];
            float ringPosition = firstRingPos + (step * i);
            ring.localPosition = GetTargetPosition(ring, ringPosition);
        }
    }


    private Vector3 GetTargetPosition(Transform target, float zPos)
    {
        Vector3 position = target.localPosition;
        position.z = zPos;
        return position;
    }


    private void OnEnable()
    {
        RingManager.OnRingZoomPositionChanged += UpdateRingZoom;
        
        if(rings.Count == 0)
        {
            foreach(Transform child in transform)
            {
                //Who would've thought Transform is an IEnumerable that'll iterate all of its children??
                rings.Add(child);
            }
        }
    }


    private void OnDisable()
    {
        RingManager.OnRingZoomPositionChanged -= UpdateRingZoom;
    }
}