using UnityEngine;

[RequireComponent(typeof(MeshFilter)), ExecuteInEditMode]
public class BloomfogSkyboxQuad : MonoBehaviour
{
	private void Awake()
	{
		Vector3[] vertices = new Vector3[4]
		{
			new Vector3(-1f, -1f, 0f),
			new Vector3(1f, -1f, 0f),
			new Vector3(1f, 1f, 0f),
			new Vector3(-1f, 1f, 0f)
		};
		int[] triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
		Mesh mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.bounds = new Bounds(Vector3.zero, new Vector3(100000000f, 100000000f, 100000000f));
	}
}