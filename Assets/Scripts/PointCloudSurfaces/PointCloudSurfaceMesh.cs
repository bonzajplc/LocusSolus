using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;


public class PointCloudSurfaceMesh : MonoBehaviour
{
    public int maxVertices = 65000;
    public int maxIndices = 200000;
    public int pointCloudIndex = 0;

    // We'll pass native pointer to the mesh vertex buffer.
    // Also passing source unmodified mesh data.
    // The plugin will fill vertex data from native code.
    [DllImport ("RenderingPlugin")]
    private static extern void SetMeshBuffersFromUnity(int pointCloudindex, IntPtr vertexBuffer, int vertexCount, IntPtr indexBuffer, int indexCount);

    void Start()
	{
        SendMeshBuffersToPlugin();
	}

	private void SendMeshBuffersToPlugin ()
	{
		var filter = GetComponent<MeshFilter> ();
		var mesh = filter.mesh;
		// The plugin will want to modify the vertex buffer -- on many platforms
		// for that to work we have to mark mesh as "dynamic" (which makes the buffers CPU writable --
		// by default they are immutable and only GPU-readable).
		mesh.MarkDynamic ();

        Vector3[] vertices = new Vector3[maxVertices];

        vertices[0] = new Vector3(1000.0f, 1000.0f, 1000.0f);
        vertices[1] = new Vector3(-1000.0f, -1000.0f, -1000.0f);

        mesh.vertices = vertices;
        mesh.normals = new Vector3[maxVertices];
        mesh.colors = new Color[maxVertices];
        mesh.uv = null;
        mesh.uv2 = null;
        mesh.uv3 = null;
        mesh.uv4 = null;

        int[] points = new int[maxIndices];

        mesh.SetIndices( points, MeshTopology.Points, 0 );

        SetMeshBuffersFromUnity(pointCloudIndex, mesh.GetNativeVertexBufferPtr(0), maxVertices, 
            mesh.GetNativeIndexBufferPtr(), maxIndices );
	}
}
