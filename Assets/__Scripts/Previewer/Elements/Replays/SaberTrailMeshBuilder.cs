using System.Collections.Generic;
using UnityEngine;

public class SaberTrailMeshBuilder : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;

    [Space]
    [SerializeField] private bool isRightHand;
    [SerializeField] private Vector3 handleOffset;
    [SerializeField] private Vector3 tipOffset;

    [Space]

    private Mesh mesh;


    private Vector3 GetVertexPosition(ReplayFrame frame, ReplayFrame nextFrame, float t, Vector3 pointOffset)
    {
        Vector3 currentPosition = isRightHand ? frame.rightSaberPosition : frame.leftSaberPosition;
        Quaternion currentRotation = isRightHand ? frame.rightSaberRotation : frame.leftSaberRotation;

        Vector3 nextPosition = isRightHand ? nextFrame.rightSaberPosition : nextFrame.leftSaberPosition;
        Quaternion nextRotation = isRightHand ? nextFrame.rightSaberRotation : nextFrame.leftSaberRotation;

        Vector3 saberPosition = Vector3.Lerp(currentPosition, nextPosition, t);
        Quaternion saberRotation = Quaternion.Lerp(currentRotation, nextRotation, t);

        saberPosition.z -= ObjectManager.PlayerCutPlaneDistance;

        Vector3 offset = saberRotation * pointOffset;
        return transform.InverseTransformPoint(saberPosition) + transform.InverseTransformDirection(offset);
    }


    public void SetFrames(List<ReplayFrame> frames, int startIndex)
    {
        float lifetime = SettingsManager.GetFloat("sabertraillength");
        int segmentCount = SettingsManager.GetInt("sabertrailsegments");

        mesh.Clear();
        Vector3[] vertices = new Vector3[segmentCount * 2];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];

        float segmentLength = lifetime / segmentCount;
        float endTime = TimeManager.CurrentTime - lifetime;

        int faceCount = segmentCount - 1;
        int triangleCount = faceCount * 2;
        int[] triangles = new int[triangleCount * 3];

        int frameIndex = startIndex;
        for(int i = 0; i < segmentCount; i++)
        {
            float timeDifference = segmentLength * i;
            float segmentTime = TimeManager.CurrentTime - timeDifference;

            if(segmentTime < endTime)
            {
                break;
            }

            //Make sure the first segment always lines up with the last frame
            if(i > 0)
            {
                //Find the last frame used for this segment
                while(frames[frameIndex].Time > segmentTime && frameIndex > 0)
                {
                    frameIndex--;
                }
            }

            int handleIndex = i * 2;
            int tipIndex = handleIndex + 1;

            bool useNextFrame = frameIndex + 1 < frames.Count;
            ReplayFrame frame = frames[frameIndex];
            ReplayFrame nextFrame = useNextFrame ? frames[frameIndex + 1] : frame;

            float frameDifference = nextFrame.Time - frame.Time;
            float frameProgress = segmentTime - frame.Time;
            float t = frameProgress / frameDifference;

            vertices[handleIndex] = GetVertexPosition(frame, nextFrame, t, handleOffset);
            vertices[tipIndex] = GetVertexPosition(frame, nextFrame, t, tipOffset);

            normals[handleIndex] = Vector3.up;
            normals[tipIndex] = Vector3.up;

            float uvX = (float)i / segmentCount;
            uvs[handleIndex] = new Vector2(uvX, 0f);
            uvs[tipIndex] = new Vector2(uvX, 1f);

            if(i < segmentCount - 1)
            {
                //Add triangles linking to the next segment
                int bottomIndex = handleIndex * 3;
                triangles[bottomIndex] = handleIndex;
                triangles[bottomIndex + 1] = handleIndex + 1;
                triangles[bottomIndex + 2] = handleIndex + 2;

                int topIndex = tipIndex * 3;
                triangles[topIndex] = tipIndex;
                triangles[topIndex + 1] = tipIndex + 1;
                triangles[topIndex + 2] = tipIndex + 2;
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.uv = uvs;
        mesh.uv2 = uvs;
        mesh.triangles = triangles;

        meshFilter.mesh = mesh;
    }


    private void Awake()
    {
        mesh = new Mesh();
    }
}