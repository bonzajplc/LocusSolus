using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

public class PointCloudSurfaceClip : MonoBehaviour
{
    [DllImport("RenderingPlugin")]
    private static extern int RegisterPointCloudClip(int clipID, IntPtr vertexBuffer, int vertexCount, IntPtr indexBuffer, int indexCount);

    [DllImport("RenderingPlugin")]
    private static extern int SetPointCloudClipFrameName(int clipID, [MarshalAs(UnmanagedType.LPStr)] string platformString );

    [SerializeField]
    public string clipDirectory = "";

#if UNITY_EDITOR
    [ReadOnly]
#endif
    [SerializeField]
    public int numberOfFrames = 0;
    [SerializeField]
    public int startFrame = 1;
    [SerializeField]
    public int endFrame = 1;
    [SerializeField]
    public bool loop = true;
    [SerializeField]
    public int maxVertices = 65000;
    [SerializeField]
    public int maxIndices = 200000;

    int frameNo = 0;
    string[] frameFiles = null;

    void Start()
	{
        var filter = GetComponent<MeshFilter>();
        var mesh = filter.mesh;
        // The plugin will want to modify the vertex buffer -- on many platforms
        // for that to work we have to mark mesh as "dynamic" (which makes the buffers CPU writable --
        // by default they are immutable and only GPU-readable).
        mesh.MarkDynamic();

        Vector3[] vertices = new Vector3[maxVertices];

        vertices[0] = new Vector3(100.0f, 100.0f, 100.0f);
        vertices[1] = new Vector3(-100.0f, -100.0f, -100.0f);

        mesh.vertices = vertices;
        mesh.normals = new Vector3[maxVertices];
        mesh.colors = new Color[maxVertices];
        mesh.uv = new Vector2[maxVertices];
        mesh.uv2 = null;
        mesh.uv3 = null;
        mesh.uv4 = null;

        int[] points = new int[maxIndices - maxIndices % 3 ];

        mesh.SetIndices(points, MeshTopology.Triangles, 0);

        RegisterPointCloudClip(GetHashCode(), mesh.GetNativeVertexBufferPtr(0), maxVertices,
            mesh.GetNativeIndexBufferPtr(), maxIndices);

        string dataPath = Application.dataPath;
        dataPath = dataPath.Replace("/Assets", "");

        dataPath += clipDirectory;
        frameFiles = Directory.GetFiles( dataPath, "*.bnzs" );
    }

    private void Update()
    {
        if (frameFiles.Length > 0)
        {

            int newFrame = (int)(Time.timeSinceLevelLoad * 30.0f) % endFrame;
            float fraction = (Time.timeSinceLevelLoad * 30.0f) - (int)(Time.timeSinceLevelLoad * 30.0f);
            Shader.SetGlobalFloat("_ArtSpaces_frame_fraction", fraction);

            if (newFrame != frameNo)
            {
                frameNo = newFrame;

                if (frameNo >= frameFiles.Length)
                    frameNo = 0;

                //Debug.Log(frameFiles[frameNo]);
                SetPointCloudClipFrameName(GetHashCode(), frameFiles[frameNo]);
            }
        }
    }
}
