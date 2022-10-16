using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TestMeshCollider : MonoBehaviour
{
	[SerializeField]
	MeshCollider meshCollider;

	private void Start()
	{
        meshCollider = GetComponent<MeshCollider>();

		Mesh mesh = new Mesh();
		mesh.vertices = new Vector3[]
		{
			new Vector3(0, 0), new Vector3(64f / Verse.Space.cellsPerMeter, 0),
			new Vector3(0, 0, .5f), new Vector3(64f / Verse.Space.cellsPerMeter, 0, .5f)
		};
		mesh.triangles = new int[]
		{
			2, 1, 0, 1, 2, 3
		};

        meshCollider.sharedMesh = mesh;
	}
}
