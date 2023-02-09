using System;
using UnityEngine;

public class WallHandler : MonoBehaviour
{
    [SerializeField] private GameObject wallBody;

    public Action<Vector3> OnScaleUpdated;


    public void SetScale(Vector3 scale)
    {
        wallBody.transform.localScale = scale;
        OnScaleUpdated?.Invoke(scale);
    }
}