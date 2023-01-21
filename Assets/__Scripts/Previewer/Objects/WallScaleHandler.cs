using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallScaleHandler : MonoBehaviour
{
    [SerializeField] private GameObject wallBody;

    public Action<Vector3> OnScaleUpdated;


    public void SetScale(Vector3 scale)
    {
        wallBody.transform.localScale = scale;
        OnScaleUpdated?.Invoke(scale);
    }
}