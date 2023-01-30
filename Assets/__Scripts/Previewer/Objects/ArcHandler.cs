using UnityEngine;

public class ArcHandler : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LineRenderer centerLineRenderer;


    public void SetArcPoints(Vector3[] newPoints)
    {
        lineRenderer.positionCount = newPoints.Length;
        centerLineRenderer.positionCount = newPoints.Length;

        lineRenderer.SetPositions(newPoints);
        centerLineRenderer.SetPositions(newPoints);
    }


    public void SetMaterial(Material newMaterial, Material centerMaterial)
    {
        lineRenderer.sharedMaterial = newMaterial;
        centerLineRenderer.sharedMaterial = centerMaterial;
    }
}