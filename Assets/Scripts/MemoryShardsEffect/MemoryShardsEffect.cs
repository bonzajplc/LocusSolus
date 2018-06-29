using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MemoryShardsEffect : MonoBehaviour {

    PatchedShape finalShape = null;
    Shape patchedShapeCopy = null;

    public uint minTrainglesPerpatch = 16;
    public uint maxTrainglesPerpatch = 32;
    public float extrudeAmount = 0.025f;
    public int randomSeed = 4096;

    private Material shardsMaterial = null;

    public ComputeShader shardsMotionCS = null;
    private int shardsMotionCSKernel = 0;

    private ComputeBuffer _vertexToPatch = null;
    private ComputeBuffer _patchStates = null;
    private ComputeBuffer _patchInfos = null;
    private ComputeBuffer _patchTransforms = null;

    struct PatchInfo_
    {
        public Vector3 patchAcceleration;
        public Vector3 patchAngularAcceleration;
        public float patchDrag;
        public float patchRandomValue; // 1 randomValue
    };

    struct PatchState_
    {
        public Vector3 patchVelocity;
        public Vector3 patchAngularVelocity;
    };

    // Use this for initialization
    void Start () {

        //create a copy of mesh for processing
        PatchedShape patchedShape = PatchedShape.CreatePatchedShapeFromMesh(GetComponent<MeshFilter>().sharedMesh, 
            Mathf.Max((int)minTrainglesPerpatch, (int)maxTrainglesPerpatch), (int)maxTrainglesPerpatch, randomSeed);
        OptimizedPatchedShape optimizedShape = OptimizedPatchedShape.CreateOptimizedPatchedShapeFromPatchedShape(patchedShape);
        finalShape = ExtrudedPatchedShape.CreateExtrudedPatchedShapeFromOptimizedPatchedShape(optimizedShape, extrudeAmount );

        patchedShapeCopy = new Shape();
        patchedShapeCopy.AllocateShape(finalShape.numVertices_, finalShape.numIndices_);

        finalShape.indices_.CopyTo(patchedShapeCopy.indices_, 0);

        Mesh mesh = GetComponent<MeshFilter>().mesh;

        mesh.vertices = finalShape.vertices_;
        mesh.normals = finalShape.normals_;
        mesh.uv = finalShape.uvs_;

        mesh.triangles = finalShape.indices_;

        //substituting material of the mesh renderer at startup
        shardsMaterial = new Material(Shader.Find("ArtSpaces/MemoryShardsShader"));
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material previewMaterial = null;

        if (renderer.materials.Length == 1)
        {
            previewMaterial = renderer.material;
            renderer.material = shardsMaterial;
        }
        else
            Debug.LogError("MemoryShardsEffect only supports meshes with one material!");

        //copy basic attributes if they are present in preview material
        //shardsMaterial.SetColor("_Color", Color.blue);

        _vertexToPatch = new ComputeBuffer(finalShape.numVertices_, 4 );  // 4 = sizeOf(uint)
        _patchTransforms = new ComputeBuffer(finalShape.nPatches_, (3 + 4) * sizeof(float));  // Vector3, Quaternion

        _vertexToPatch.SetData(finalShape.vertexToPatchIndex_);
        shardsMaterial.SetBuffer("_vertexToPatch", _vertexToPatch);

        _patchTransforms.SetData(finalShape.patchTransforms_);
        shardsMaterial.SetBuffer("_patchTransforms", _patchTransforms);

        //setup compute buffers for compute shader
        _patchStates = new ComputeBuffer(finalShape.nPatches_, (3 + 3) * sizeof(float));  // Vector3, Vector3
        _patchInfos = new ComputeBuffer(finalShape.nPatches_, (3 + 3 + 1 + 1) * sizeof(float));  // Vector3, Vector3, float, float

        shardsMotionCSKernel = shardsMotionCS.FindKernel("cs_updatePatch");

        PatchInfo_[] patchInfos = new PatchInfo_[finalShape.nPatches_];
        PatchState_[] patchStates = new PatchState_[finalShape.nPatches_];

        Random.InitState( GetHashCode() );

        for ( int i = 0; i < finalShape.nPatches_; i++ )
        {
            patchInfos[i].patchAcceleration = Random.onUnitSphere * 0.01f;
            patchInfos[i].patchAngularAcceleration = Random.onUnitSphere * 0.05f;
            patchInfos[i].patchDrag = Random.Range(0.0f, 1.0f);
            patchInfos[i].patchRandomValue = Random.Range(0.0f, 1.0f);

            patchStates[i].patchVelocity = Vector3.zero;
            patchStates[i].patchAngularVelocity = Vector3.zero;
        }

        _patchInfos.SetData(patchInfos);
        _patchStates.SetData(patchStates);
    }

    private void OnDestroy()
    {
        if (_vertexToPatch != null)
            _vertexToPatch.Release();
        if (_patchTransforms != null)
            _patchTransforms.Release();
        if (_patchInfos != null)
            _patchInfos.Release();
        if (_patchStates != null)
            _patchStates.Release();
    }

    // Update is called once per frame
    void Update () {

        shardsMotionCS.SetBuffer(shardsMotionCSKernel, "in_patchInfos", _patchInfos);
        shardsMotionCS.SetBuffer(shardsMotionCSKernel, "inout_patchStates", _patchStates);
        shardsMotionCS.SetBuffer(shardsMotionCSKernel, "inout_patchTransforms", _patchTransforms);
        shardsMotionCS.SetFloat("deltaTime", 1.0f/60.0f );

        shardsMotionCS.Dispatch(shardsMotionCSKernel, 64, 1, 1);

        /*   finalShape.VisualisePatches(patchedShapeCopy, ( Mathf.SmoothStep(0.0f, 2.0f, Time.time * 0.05f ) ) );

           Mesh mesh = GetComponent<MeshFilter>().mesh;

           mesh.vertices = patchedShapeCopy.vertices_;
           mesh.normals = patchedShapeCopy.normals_;
           mesh.uv = patchedShapeCopy.uvs_;

           mesh.triangles = patchedShapeCopy.indices_;*/
    }
}
