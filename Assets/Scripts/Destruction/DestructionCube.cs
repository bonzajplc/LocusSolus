using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using DestructionEffect;

//[ExecuteInEditMode]
public class DestructionCube : MonoBehaviour {

    public int numVertices = 5000;

    public float cubeWidth = 0.5f;
    public float cubeHeight = 0.5f;
    public float cubeDepth = 0.5f;

    public float extrude = 0.1f;
    private bool lastCollisionState = true;

    [Range(0.025f, 1f)]
    public float voxelRadius = 0.1f;

    [Range(0f, 100f)]
    public float sensorRadius = 0.35f;

    [Range(0f, 1f)]
    public float sensorInnerRadius = 0.8f;

    public Mesh inputMesh;

#if UNITY_EDITOR
    [ReadOnly]
    public float numGeneratedVertices = 0;
#endif

    Shape shape;
	DestructionVertexData output;
	DestructionSensor[] sensors = { new DestructionSensor () };
	DestructionShapeParams shapeParams = new DestructionShapeParams();

    DestructionEffectIterative.AppendConsumeStack iterativeStack;

    void Start()
    {
        //GetComponent<MeshFilter>().sharedMesh.MarkDynamic();

        if (Mathf.Abs(cubeWidth) < 0.0001f)
            cubeWidth = 0.0001f;
        if (Mathf.Abs(cubeHeight) < 0.0001f)
            cubeHeight = 0.0001f;
        if (Mathf.Abs(cubeDepth) < 0.0001f)
            cubeDepth = 0.0001f;

        if( inputMesh != null )
            shape = Shape.CreateShapeFromMesh(inputMesh);
        else
            shape = CubeShape.CreateCubeShape(1, false, new Vector3(cubeWidth, cubeHeight, cubeDepth) );


		output = new DestructionVertexData();
		output.prepareVertexData (numVertices);

        iterativeStack = new DestructionEffectIterative.AppendConsumeStack();
        iterativeStack.prepareStack(numVertices/6);
    }

    // Update is called once per frame
    void Update () {

        Profiler.BeginSample("clearIndices");
		output.clearIndices ();
        Profiler.EndSample();

        Vector3 localScale = transform.localScale;
        if (Mathf.Abs(localScale.x) < 0.0001f)
            localScale.x = 0.0001f;
        if (Mathf.Abs(localScale.y) < 0.0001f)
            localScale.y = 0.0001f;
        if (Mathf.Abs(localScale.z) < 0.0001f)
            localScale.z = 0.0001f;

        shapeParams.scale = localScale;
        shapeParams.scaleInv = new Vector3(1.0f / localScale.x, 1.0f / localScale.y, 1.0f / localScale.z);
        shapeParams.extrude = new Vector3(extrude, extrude, extrude);
        shapeParams.voxelRadius = voxelRadius;

        sensors[0].radius = sensorRadius;
        sensors[0].innerRadius = sensorRadius * sensorInnerRadius;
        sensors[0].posLS = transform.InverseTransformPoint(Camera.main.transform.position);
        sensors[0].posLS.Scale(localScale);
        sensors[0].AABB = new Bounds(Camera.main.transform.position, new Vector3(sensorRadius, sensorRadius, sensorRadius));

        Bounds rendererBounds = GetComponent<MeshRenderer>().bounds;
        bool collisionState = false;

        //check if sensors collide with AABB
        for ( int i = 0; i < sensors.Length; i++ )
        {
            if( rendererBounds.Intersects(sensors[i].AABB) )
            {
                collisionState = true;
                break;
            }
        }

        if( collisionState )
        {
            Profiler.BeginSample("generateTrianglesFromShape");
//		bool collisionState = DestructionEffectRecursive.DestructionPrism.generateTrianglesFromShape( output, shape, shapeParams, sensors, 1 );
            DestructionEffectIterative.DestructionPrism.generateTrianglesFromShape(output, iterativeStack, shape, shapeParams, sensors);
            Profiler.EndSample();
        }

        if (collisionState || lastCollisionState)
        {
            Profiler.BeginSample("copy vertices");

            Mesh mesh = GetComponent<MeshFilter>().mesh;
 
            if (collisionState)//copy triangles from generated mesh
            {
                mesh.vertices = output._positions;
                mesh.normals = output._normals;
                mesh.uv = output._texcoords;

                mesh.triangles = output._indices;
            }
            else//copy triangles from input
            {
                mesh.triangles = shape.indices_;

                mesh.vertices = shape.vertices_;
                mesh.normals = shape.normals_;
                mesh.uv = shape.uvs_;
            }

            Profiler.EndSample();
        }

        lastCollisionState = collisionState;

#if UNITY_EDITOR
        numGeneratedVertices = output._numVertices;
#endif
    }
}