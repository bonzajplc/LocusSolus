using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

class PatchedShape: Shape
{
    public struct Patch_
    {
        public uint startVertex_;
        public uint nVertices_;
        public uint startIndex_;
        public uint nIndices_;
    }

    public struct PatchTransform_
    {
        public Vector3 position_;
        public Quaternion rotation_;
    }

    class TriangleAdjacency_
    {
        public uint triangleIndex_;
        public int nextTriangle_; //-2 uninitialized, -1 no next triangle (end of list)

       public TriangleAdjacency_( int nextTriangle )
        {
            triangleIndex_ = 0xffffffff;
            nextTriangle_ = nextTriangle;
        }
    }

    public Patch_[] patches_ = null;
    public PatchTransform_[] patchTransforms_ = null;
    public int nPatches_ = 0;
    public int[] vertexToPatchIndex_ = null;

    public void CreatePatches(Shape inputShape, int minTrianglesPerPatch, int maxTrianglesPerPatch, int seed)
    {
        Random.InitState(seed);

        patches_ = new Patch_[inputShape.GetNTrianglesInShape()];
        nPatches_ = 0;

        //patchedShape._nSurfaces = src.nSurfaces_;
        //patchedShape._surfaces = reinterpret_cast<_DstSurf*>(picoMallocAligned((dst.nSurfaces_) * sizeof(_DstSurf), 64));

        bool[] triangleUsed = new bool[inputShape.GetNTrianglesInShape()];

        int targetVerticesCount = 0;

        // triAdjacency
        //
        TriangleAdjacency_[] triangleAdjacency = new TriangleAdjacency_[inputShape.numIndices_];
        for (int i = 0; i < inputShape.numVertices_; ++i)
        {
            triangleAdjacency[i] = new TriangleAdjacency_(-2);
        }

        for (int i = inputShape.numVertices_; i < inputShape.numIndices_; ++i)
        {
            triangleAdjacency[i] = new TriangleAdjacency_(-1);
        }

        // build adjecency list
        //
        int nextFreeSlot = inputShape.numVertices_;
        for (uint ii = 0; ii < inputShape.numIndices_; ++ii)
        {
            int vertIdx = inputShape.indices_[ii];

            // append to end of the list
            //
            TriangleAdjacency_ ta = triangleAdjacency[vertIdx];
            if (ta.nextTriangle_ == -2)
            {
                ta.triangleIndex_ = ii / 3;
                ta.nextTriangle_ = -1;

                //triangleAdjacency[vertIdx] = ta;
            }
            else
            {
                while (ta.nextTriangle_ != -1)
                {
                    ta = triangleAdjacency[ta.nextTriangle_];
                }

                ta.nextTriangle_ = nextFreeSlot;
                TriangleAdjacency_ taNext = triangleAdjacency[nextFreeSlot];
                ++nextFreeSlot;

                taNext.triangleIndex_ = ii / 3;
                taNext.nextTriangle_ = -1;
            }
        }

        for (uint i = 0; i < inputShape.numIndices_; ++i)
        {
            Assert.IsTrue(triangleAdjacency[i].triangleIndex_ != 0xffffffff && triangleAdjacency[i].nextTriangle_ != -2);
        }

        uint dstTriangleIdx = 0;

        //        for (u32 isurf = 0; isurf < src.nSurfaces_; ++isurf)
        //        {
        //            const _SrcSurf&surf = src.surfaces_[isurf];

        //            _DstSurf & dstSurf = dst.surfaces_[isurf];
        //            dstSurf.startVertex = dstTriangleIdx * 3;

        for (int srcTriangleIdx = 0; srcTriangleIdx < inputShape.GetNTrianglesInShape(); ++srcTriangleIdx)
        {
            if (triangleUsed[srcTriangleIdx])
            {
                continue;
            }


            int curPatch = nPatches_;
            ++nPatches_;
            patches_[curPatch].startVertex_ = dstTriangleIdx * 3;
            patches_[curPatch].nVertices_ = 0;

            int nTrianglesLeft = inputShape.GetNTrianglesInShape() - srcTriangleIdx;
            int nTrianglesInThisPatchOrig = Mathf.Clamp(Random.Range((int)minTrianglesPerPatch, (int)maxTrianglesPerPatch), 1, nTrianglesLeft);

            int nTrianglesInThisPatch = nTrianglesInThisPatchOrig;
            int triangleIdx = srcTriangleIdx;

            _CreatePatchRecurse(inputShape, ref targetVerticesCount, ref nTrianglesInThisPatch, triangleIdx, triangleUsed, triangleAdjacency, ref patches_[curPatch], srcTriangleIdx, ref dstTriangleIdx);

            Assert.IsTrue((patches_[curPatch].nVertices_ / 3) <= maxTrianglesPerPatch);
        }

        numVertices_ = (int)dstTriangleIdx * 3;
        numIndices_ = (int)dstTriangleIdx * 3;
        //      }

        triangleAdjacency = null;
        triangleUsed = null;
    }

    static int wrap_inc(int value, int min, int max)
    {
        return (value == max) ? min : value + 1;
    }

    static int wrap_dec(int value, int min, int max)
    {
        return (value == min) ? max : value - 1;
    }

    void _CreatePatchRecurse(Shape inputShape, ref int targetVerticesCount, ref int nTrianglesInThisPatch, int triangleIdx, bool[] triangleUsed, TriangleAdjacency_[] triAdjacency,
											    ref Patch_ curPatch, int srcTriangleIdx, ref uint dstTriangleIdx )
    {
	    if ( nTrianglesInThisPatch == 0 )
		    return;

	    triangleUsed[triangleIdx] = true;

	    int tri0 = inputShape.indices_[triangleIdx * 3 + 0];
	    int tri1 = inputShape.indices_[triangleIdx * 3 + 1];
	    int tri2 = inputShape.indices_[triangleIdx * 3 + 2];

        vertices_[targetVerticesCount + 0] = inputShape.vertices_[tri0];
        normals_[targetVerticesCount + 0] = inputShape.normals_[tri0];
        uvs_[targetVerticesCount + 0] = inputShape.uvs_[tri0];


        vertices_[targetVerticesCount + 1] = inputShape.vertices_[tri1];
        normals_[targetVerticesCount + 1] = inputShape.normals_[tri1];
        uvs_[targetVerticesCount + 1] = inputShape.uvs_[tri1];


        vertices_[targetVerticesCount + 2] = inputShape.vertices_[tri2];
        normals_[targetVerticesCount + 2] = inputShape.normals_[tri2];
        uvs_[targetVerticesCount + 2] = inputShape.uvs_[tri2];

        targetVerticesCount += 3;
	    curPatch.nVertices_ += 3;
	    ++ dstTriangleIdx;

        // pick random edges
        //
        int randEdge0_0 = Random.Range(0, 2);
        int randEdge0_1 = wrap_inc(randEdge0_0, 0, 2);

        int randEdge1_0 = Random.Range(0, 2);

        if (randEdge1_0 == randEdge0_0)
            randEdge1_0 = wrap_inc(randEdge1_0, 0, 2);

        int randEdge1_1 = wrap_inc(randEdge1_0, 0, 2);

        int randEdge2_0 = 0;

        while (randEdge2_0 == randEdge0_0 || randEdge2_0 == randEdge1_0)
            randEdge2_0++;

        int randEdge2_1 = wrap_inc(randEdge2_0, 0, 2);

        int numRolledEdges = 3;
        //there's a 10% chance that there will be 2 edges
        //if (Random.Range(1, 10) == 1)
        //    numRolledEdges = 2;

        for ( int iedge = 0; iedge < numRolledEdges; iedge++ )
        {
            if (nTrianglesInThisPatch > 0)
            {
                int tmpEdge_0 = randEdge0_0;
                int tmpEdge_1 = randEdge0_1;
                if ( iedge == 1 )
                {
                    tmpEdge_0 = randEdge1_0;
                    tmpEdge_1 = randEdge1_1;
                }
                else if (iedge == 2)
                {
                    tmpEdge_0 = randEdge2_0;
                    tmpEdge_1 = randEdge2_1;
                }

                int randEdge_0 = inputShape.indices_[triangleIdx * 3 + tmpEdge_0];
                int randEdge_1 = inputShape.indices_[triangleIdx * 3 + tmpEdge_1];

                //PICO_ASSERT( randEdge_0 != randEdge_1 );

                // triangles adjacent to first vertex
                //
                TriangleAdjacency_ ta = triAdjacency[randEdge_0];
                bool secondVertexTraversed = false;
                while (ta != null)
                {
                    uint iinner = ta.triangleIndex_;

                    if (!triangleUsed[iinner])
                    {
                        //int idx0 = inputShape.indices_[iinner * 3 + 0];
                        //int idx1 = inputShape.indices_[iinner * 3 + 1];
                        //int idx2 = inputShape.indices_[iinner * 3 + 2];

                        //if (((randEdge_0 == idx0) || (randEdge_0 == idx1) || (randEdge_0 == idx2))
                        //    && ((randEdge_1 == idx0) || (randEdge_1 == idx1) || (randEdge_1 == idx2))
                        //    )
                        {
                            triangleIdx = (int)iinner;
                            nTrianglesInThisPatch -= 1;
                            if (nTrianglesInThisPatch > 0)
                            {
                                _CreatePatchRecurse(inputShape, ref targetVerticesCount, ref nTrianglesInThisPatch, triangleIdx, triangleUsed, triAdjacency, ref curPatch, srcTriangleIdx, ref dstTriangleIdx);
                                //    break;
                            }
                            else
                                break;
                        }
                    }

                    if (ta.nextTriangle_ != -1)
                        ta = triAdjacency[ta.nextTriangle_];
                    else
                    {
                        if( secondVertexTraversed )
                            ta = null;
                        else
                        {
                            ta = triAdjacency[randEdge_1];
                            secondVertexTraversed = true;
                        }
                    }
                }
            }
            else
                break;
        }
    }

    public void computeVertexToPatchIndexes()
    {
        vertexToPatchIndex_ = new int[numVertices_];

        for (int ipatch = 0; ipatch < nPatches_; ++ipatch)
        {
            Patch_ patch = patches_[ipatch];

            for (uint ivert = patch.startVertex_; ivert < patch.startVertex_ + patch.nVertices_; ++ivert)
            {
                vertexToPatchIndex_[ivert] = ipatch;
            }
        }
    }

    public void finalizePatches()
    {
        computeVertexToPatchIndexes();
        patchTransforms_ = new PatchTransform_[nPatches_];

        //calculate transforms based on patch centers

        for (int ipatch = 0; ipatch < nPatches_; ++ipatch)
        {
            Patch_ patch = patches_[ipatch];

            //copy first vertex
            patchTransforms_[ipatch].position_ = vertices_[patch.startVertex_];
            patchTransforms_[ipatch].rotation_ = Quaternion.identity;

            for (uint ivert = patch.startVertex_ + 1; ivert < patch.startVertex_ + patch.nVertices_; ++ivert)
            {
                patchTransforms_[ipatch].position_ += vertices_[ivert];
                patchTransforms_[ipatch].position_ /= 2.0f;
            }
 
            //transform patches
            for (uint ivert = patch.startVertex_ + 0; ivert < patch.startVertex_ + patch.nVertices_; ++ivert)
            {
                vertices_[ivert] -= patchTransforms_[ipatch].position_;
            }
        }
    }

    public static PatchedShape CreatePatchedShapeFromMesh(Mesh inputMesh, int minTrianglesPerPatch, int maxTrianglesPerPatch, int seed)
    {
        Shape meshShapeCopy = new Shape();

        meshShapeCopy.AllocateShape(inputMesh.vertexCount, inputMesh.triangles.Length);

        meshShapeCopy.vertices_ = inputMesh.vertices;
        meshShapeCopy.normals_ = inputMesh.normals;

        if (inputMesh.uv.Length > 0)
            meshShapeCopy.uvs_ = inputMesh.uv;

        meshShapeCopy.indices_ = inputMesh.triangles;

        PatchedShape patchedShape = new PatchedShape();

        patchedShape.AllocateShape(meshShapeCopy.numIndices_, meshShapeCopy.numIndices_);

        patchedShape.CreatePatches(meshShapeCopy, minTrianglesPerPatch, maxTrianglesPerPatch, seed);

        //free copy
        meshShapeCopy = null;

        return patchedShape;
    }

    public void VisualisePatches(Shape output, float extrudeScale)
    {
        Random.InitState(0);
        int idx = 0;
        for (int i = 0; i < nPatches_; i++)
        {
            //float rndExtrude = Random.Range(0.0f, 1.0f) * extrudeScale;

            Vector3 rndRotation = Random.onUnitSphere;
            Vector3 rndDirection = Random.onUnitSphere;

            Vector3 newPatchPosition =  patchTransforms_[i].position_ + extrudeScale * patchTransforms_[i].position_.normalized;// rndDirection;
            Quaternion newPatchRotation = Quaternion.AngleAxis(Time.timeSinceLevelLoad * 10.0f, rndRotation);

            Matrix4x4 m = Matrix4x4.Translate(newPatchPosition) * Matrix4x4.Rotate(newPatchRotation);

            for (uint j = patches_[i].startVertex_; j < patches_[i].startVertex_ + patches_[i].nVertices_; j++)
            {
                output.vertices_[idx] = m.MultiplyPoint( vertices_[idx] );
                output.normals_[idx] = m.MultiplyVector( normals_[idx] );
                output.uvs_[idx] = uvs_[idx];

                idx++;
            }
        }
    }
};
