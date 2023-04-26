using UnityEngine;

public class ArcHandler : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;


    public void SetArcPoints(Vector3[] newPoints)
    {
        lineRenderer.positionCount = newPoints.Length;
        lineRenderer.SetPositions(newPoints);
    }


    public void SetGradient(float curveLength, float endFadeLength)
    {
        //Sets the alpha gradient of the linerenderer to make the end fades consistent
        //Needed since gradients are based on percentage of length, not actual distance,
        //so longer arcs would have a longer fade at the end without this
        float ratio = endFadeLength / curveLength;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, ratio),
                new GradientAlphaKey(1f, 1f - ratio),
                new GradientAlphaKey(0f, 1f)
            }
        );
        lineRenderer.colorGradient = gradient;
    }


    public void SetMaterial(Material newMaterial, MaterialPropertyBlock properties)
    {
        lineRenderer.sharedMaterial = newMaterial;
        lineRenderer.SetPropertyBlock(properties);
    }
}