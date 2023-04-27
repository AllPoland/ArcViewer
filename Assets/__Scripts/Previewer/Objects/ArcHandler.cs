using UnityEngine;

public class ArcHandler : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private MaterialPropertyBlock materialProperties;


    public void SetArcPoints(Vector3[] newPoints)
    {
        lineRenderer.positionCount = newPoints.Length;
        lineRenderer.SetPositions(newPoints);
    }


    public void SetGradient(float curveLength, float endFadeStart, float endFadeLength)
    {
        //Sets the alpha gradient of the linerenderer to make the end fades consistent
        //Needed since gradients are based on percentage of length, not actual distance,
        //so longer arcs would have a longer fade at the end without this
        float fadeStart = endFadeStart / curveLength;
        float fadeEnd = endFadeLength / curveLength;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, fadeStart),
                new GradientAlphaKey(1f, fadeEnd),
                new GradientAlphaKey(1f, 1f - fadeEnd),
                new GradientAlphaKey(0f, 1f - fadeStart)
            }
        );
        lineRenderer.colorGradient = gradient;
    }


    public void SetMaterial(Material newMaterial, MaterialPropertyBlock properties)
    {
        lineRenderer.sharedMaterial = newMaterial;
        lineRenderer.SetPropertyBlock(properties);

        //This creates a proper copy of the material property block
        //Allows us to change properties on this arc without worrying about reference types
        if(materialProperties == null) materialProperties = new MaterialPropertyBlock();
        lineRenderer.GetPropertyBlock(materialProperties);
    }


    public void SetAlpha(float alpha)
    {
        if(alpha == materialProperties.GetColor("_BaseColor").a) return;

        Color color = materialProperties.GetColor("_BaseColor");
        color.a = alpha;
        materialProperties.SetColor("_BaseColor", color);
        lineRenderer.SetPropertyBlock(materialProperties);
    }
}