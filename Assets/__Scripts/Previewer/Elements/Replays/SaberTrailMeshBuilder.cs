using System.Collections.Generic;
using UnityEngine;

public class SaberTrailMeshBuilder : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;

    [Space]
    [SerializeField] private bool isRightHand;
    [SerializeField] private Vector3 tipOffset;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;


    public void SetFrames(List<ReplayFrame> frames, int startIndex)
    {
        float lifetime = SettingsManager.GetFloat("sabertraillength");
        float trailWidth = SettingsManager.GetFloat("sabertrailwidth");
        int segmentCount = SettingsManager.GetInt("sabertrailsegments");

        mesh.Clear();
        vertices = new Vector3[segmentCount * 2];
        uvs = new Vector2[vertices.Length];

        float segmentLength = lifetime / segmentCount;
        float endTime = TimeManager.CurrentTime - lifetime;

        int faceCount = segmentCount - 1;
        int triangleCount = faceCount * 2;
        triangles = new int[triangleCount * 3];

        int frameIndex = startIndex;
        for(int i = 0; i < segmentCount; i++)
        {
            float timeDifference = segmentLength * i;
            float segmentTime = Mathf.Max(TimeManager.CurrentTime - timeDifference, 0f);

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

            float frameDifference = Mathf.Max(nextFrame.Time - frame.Time, 0.001f);
            float frameProgress = segmentTime - frame.Time;
            float t = frameProgress / frameDifference;

            Vector3 currentPosition = isRightHand ? frame.rightSaberPosition : frame.leftSaberPosition;
            Quaternion currentRotation = isRightHand ? frame.rightSaberRotation : frame.leftSaberRotation;
            Vector3 nextPosition = isRightHand ? nextFrame.rightSaberPosition : nextFrame.leftSaberPosition;
            Quaternion nextRotation = isRightHand ? nextFrame.rightSaberRotation : nextFrame.leftSaberRotation;

            Vector3 saberPosition = Vector3.Lerp(currentPosition, nextPosition, t);
            Quaternion saberRotation = Quaternion.Lerp(currentRotation, nextRotation, t);

            saberPosition.z -= ObjectManager.PlayerCutPlaneDistance;
            saberPosition = transform.InverseTransformPoint(saberPosition);

            Vector3 tipPoint = transform.InverseTransformDirection(saberRotation * tipOffset);

            Vector3 tipDirection = tipPoint.normalized;
            Vector3 handlePoint = tipPoint - (tipDirection * trailWidth);

            vertices[handleIndex] = saberPosition + handlePoint;
            vertices[tipIndex] = saberPosition + tipPoint;

            //UVs don't reach fully to the top/bottom because there're weird artifacts
            //for some reason
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
        mesh.uv = uvs;
        mesh.triangles = triangles;

        meshFilter.mesh = mesh;
    }


    private void Awake()
    {
        mesh = new Mesh();
    }
}