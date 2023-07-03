using System;
using UnityEngine;

public class WallHandler : MonoBehaviour, IComparable<WallHandler>
{
    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock materialProperties;


    public int CompareTo(WallHandler other)
    {
        //Positive 1 means this wall is in front, negative 1 means the other wall is in front
        //0 means they're overlapping
        if(other == this)
        {
            return 0;
        }

        Vector3 cameraPosition = Camera.main.transform.position;
        Transform otherTransform = other.transform;

        Vector3 thisRelativePosition = transform.position - cameraPosition;
        Vector3 otherRelativePosition = otherTransform.position - cameraPosition;

        //Multiply by slightly less than 1 so as to not move *entirely* to the edge of the wall
        //This makes it possible to distinguish distance between walls that have touching edges
        float maxDist = 0.95f;
        Vector3 thisHalfScale = transform.localScale.Abs() / 2 * maxDist;
        Vector3 otherHalfScale = otherTransform.localScale.Abs() / 2 * maxDist;

        Vector3 thisMinPoint = thisRelativePosition - thisHalfScale;
        Vector3 thisMaxPoint = thisRelativePosition + thisHalfScale;

        Vector3 otherMinPoint = otherRelativePosition - otherHalfScale;
        Vector3 otherMaxPoint = otherRelativePosition + otherHalfScale;

        //If the walls aren't on the same side of the camera, they shouldn't be evaluated
        if(Mathf.Sign(thisMinPoint.x) != Mathf.Sign(otherMinPoint.x) && Mathf.Sign(thisMaxPoint.x) != MathF.Sign(otherMaxPoint.x))
        {
            if(thisMinPoint.x < otherMinPoint.x)
            {
                return -1;
            }
            else return 1;
        }
        else if(Mathf.Sign(thisMinPoint.y) != Mathf.Sign(otherMinPoint.y) && Mathf.Sign(thisMaxPoint.y) != MathF.Sign(otherMaxPoint.y))
        {
            if(thisMinPoint.y < otherMinPoint.y)
            {
                return -1;
            }
            else return 1;
        }
        else if(Mathf.Sign(thisMinPoint.z) != Mathf.Sign(otherMinPoint.z) && Mathf.Sign(thisMaxPoint.z) != MathF.Sign(otherMaxPoint.z))
        {
            if(thisMinPoint.z < otherMinPoint.y)
            {
                return -1;
            }
            else return 1;
        }

        Vector3 thisClosestPoint = thisRelativePosition;
        //Move each coordinate separately in order to properly account for wall scale
        thisClosestPoint.x = Mathf.MoveTowards(thisRelativePosition.x, 0f, thisHalfScale.x);
        thisClosestPoint.y = Mathf.MoveTowards(thisRelativePosition.y, 0f, thisHalfScale.y);
        thisClosestPoint.z = Mathf.MoveTowards(thisRelativePosition.z, 0f, thisHalfScale.z);

        if(thisClosestPoint == Vector3.zero)
        {
            //This wall overlaps with the camera, so obviously it should go in front
            return 1;
        }

        Vector3 otherClosestPoint = otherRelativePosition;
        otherClosestPoint.x = Mathf.MoveTowards(otherRelativePosition.x, 0f, otherHalfScale.x);
        otherClosestPoint.y = Mathf.MoveTowards(otherRelativePosition.y, 0f, otherHalfScale.y);
        otherClosestPoint.z = Mathf.MoveTowards(otherRelativePosition.z, 0f, otherHalfScale.z);

        if(otherClosestPoint == Vector3.zero)
        {
            return -1;
        }

        //Get the point on the wall that's nearest to the closest point on the other wall
        Vector3 thisComparisonPoint = thisRelativePosition;
        thisComparisonPoint.x = Mathf.MoveTowards(thisRelativePosition.x, otherClosestPoint.x, thisHalfScale.x);
        thisComparisonPoint.y = Mathf.MoveTowards(thisRelativePosition.y, otherClosestPoint.y, thisHalfScale.y);
        thisComparisonPoint.z = Mathf.MoveTowards(thisRelativePosition.z, otherClosestPoint.z, thisHalfScale.z);

        Vector3 otherComparisonPoint = otherRelativePosition;
        otherComparisonPoint.x = Mathf.MoveTowards(otherRelativePosition.x, thisClosestPoint.x, otherHalfScale.x);
        otherComparisonPoint.y = Mathf.MoveTowards(otherRelativePosition.y, thisClosestPoint.y, otherHalfScale.y);
        otherComparisonPoint.z = Mathf.MoveTowards(otherRelativePosition.z, thisClosestPoint.z, otherHalfScale.z);

        //Compare the distances between the two sorta converged points idk what to call them
        float thisComparisonDistance = thisComparisonPoint.magnitude;
        float otherComparisonDistance = otherComparisonPoint.magnitude;
        if(thisComparisonDistance < otherComparisonDistance)
        {
            return 1;
        }
        else if(thisComparisonDistance > otherComparisonDistance)
        {
            return -1;
        }
        else
        {
            float thisClosestDistance = thisClosestPoint.magnitude;
            float otherClosestDistance = otherClosestPoint.magnitude;
            if(thisClosestDistance < otherClosestDistance)
            {
                return 1;
            }
            else if(thisClosestDistance > otherClosestDistance)
            {
                return -1;
            }
            else return 0;
        }
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