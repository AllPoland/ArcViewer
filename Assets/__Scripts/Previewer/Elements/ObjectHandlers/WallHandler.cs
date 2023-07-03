using System;
using UnityEngine;

public class WallHandler : MonoBehaviour, IComparable<WallHandler>
{
    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock materialProperties;


    public int CompareTo(WallHandler other)
    {
        Vector3 cameraPosition = Camera.main.transform.position;
        Transform otherTransform = other.transform;

        //Multiply by slightly less than 1 so as to not move *entirely* to the edge of the wall
        //This makes it possible to distinguish distance between walls that have touching edges
        float maxDist = 1f - 0.001f;
        Vector3 thisHalfScale = transform.localScale / 2 * maxDist;
        Vector3 otherHalfScale = otherTransform.localScale / 2 * maxDist;

        Vector3 thisClosestPoint = transform.position;
        //Move each coordinate separately in order to properly account for wall scale
        thisClosestPoint.x = Mathf.MoveTowards(thisClosestPoint.x, cameraPosition.x, thisHalfScale.x);
        thisClosestPoint.y = Mathf.MoveTowards(thisClosestPoint.y, cameraPosition.y, thisHalfScale.y);
        thisClosestPoint.z = Mathf.MoveTowards(thisClosestPoint.z, cameraPosition.z, thisHalfScale.z);

        if(thisClosestPoint == cameraPosition)
        {
            //This wall overlaps with the camera, so obviously it should go in front
            //Positive 1 means this wall is in front, negative 1 means the other wall is in front
            return 1;
        }

        Vector3 otherClosestPoint = otherTransform.position;
        otherClosestPoint.x = Mathf.MoveTowards(otherClosestPoint.x, cameraPosition.x, otherHalfScale.x);
        otherClosestPoint.y = Mathf.MoveTowards(otherClosestPoint.y, cameraPosition.y, otherHalfScale.y);
        otherClosestPoint.z = Mathf.MoveTowards(otherClosestPoint.z, cameraPosition.z, otherHalfScale.z);

        if(otherClosestPoint == cameraPosition)
        {
            return -1;
        }

        //Get the point on the wall that's nearest to the closest point on the other wall
        Vector3 thisComparisonPoint = transform.position;
        thisComparisonPoint.x = Mathf.MoveTowards(thisComparisonPoint.x, otherClosestPoint.x, thisHalfScale.x);
        thisComparisonPoint.y = Mathf.MoveTowards(thisComparisonPoint.y, otherClosestPoint.y, thisHalfScale.y);
        thisComparisonPoint.z = Mathf.MoveTowards(thisComparisonPoint.z, otherClosestPoint.z, thisHalfScale.z);

        Vector3 otherComparisonPoint = otherTransform.position;
        otherComparisonPoint.x = Mathf.MoveTowards(otherComparisonPoint.x, thisClosestPoint.x, otherHalfScale.x);
        otherComparisonPoint.y = Mathf.MoveTowards(otherComparisonPoint.y, thisClosestPoint.y, otherHalfScale.y);
        otherComparisonPoint.z = Mathf.MoveTowards(otherComparisonPoint.z, thisClosestPoint.z, otherHalfScale.z);

        //Compare the distances between the two sorta converged points idk what to call them
        float thisComparisonDistance = Vector3.Distance(cameraPosition, thisComparisonPoint);
        float otherComparisonDistance = Vector3.Distance(cameraPosition, otherComparisonPoint);
        if(thisComparisonDistance < otherComparisonDistance)
        {
            return 1;
        }
        else if(thisComparisonDistance == otherComparisonDistance)
        {
            return 0;
        }
        else return -1;
    }


    public void SetProperties(MaterialPropertyBlock properties)
    {
        meshRenderer.SetPropertyBlock(properties);
        materialProperties = properties;
    }


    public void SetAlpha(float alpha)
    {
        if(materialProperties == null)
        {
            Debug.LogWarning("Tried to set alpha on a wall with no material properties!");
            return;
        }

        Color wallColor = materialProperties.GetColor("_BaseColor");
        wallColor.a = alpha;
        materialProperties.SetColor("_BaseColor", wallColor);

        meshRenderer.SetPropertyBlock(materialProperties);
    }


    public void SetSortingOrder(int order)
    {
        meshRenderer.sortingOrder = order;
    }
}