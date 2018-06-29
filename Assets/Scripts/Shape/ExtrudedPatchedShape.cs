using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

class ExtrudedPatchedShape: PatchedShape
{
    public struct Edge_
    {
        public int v1_;
        public int v2_;

        public Edge_( int v1, int v2 )
        {
            v1_ = v1;
            v2_ = v2;
        }
    }

    class EdgeComparer_ : IEqualityComparer<Edge_>
    {
        public bool Equals(Edge_ x, Edge_ y)
        {
            return ( x.v1_ == y.v1_ && x.v2_ == y.v2_) || (x.v1_ == y.v2_ && x.v2_ == y.v1_);
        }

        public int GetHashCode(Edge_ x)
        {
            return x.v1_.GetHashCode() + x.v2_.GetHashCode();
        }
    }

    static public ExtrudedPatchedShape CreateExtrudedPatchedShapeFromOptimizedPatchedShape(OptimizedPatchedShape optimizedShape, float extrudeAmount)
    {
        //uint idxRemapCapacity = 1024;
        //uint[] idxRemap = new uint[idxRemapCapacity];

        ExtrudedPatchedShape extrudedShape = new ExtrudedPatchedShape();

        //allocate optimized shape
        extrudedShape.AllocateShape(optimizedShape.numVertices_ * 8, optimizedShape.numIndices_ * 12 );
        extrudedShape.numIndices_ = 0; //we will be filling it from beginning

        extrudedShape.patches_ = new Patch_[optimizedShape.nPatches_];
        extrudedShape.nPatches_ = optimizedShape.nPatches_;

        uint nDstVertices = 0;
        uint nDstIndices = 0;
        int numberOfNewVertices = 0;

        for (uint ipatch = 0; ipatch < optimizedShape.nPatches_; ++ipatch)
        {
            Patch_ patch = optimizedShape.patches_[ipatch];

            extrudedShape.patches_[ipatch].startVertex_ = nDstVertices;
            extrudedShape.patches_[ipatch].startIndex_ = nDstIndices;

            int lastNumberOfNewVertices = numberOfNewVertices;

            //copy all vertices original vertices
            for (uint ivert = patch.startVertex_; ivert < patch.startVertex_ + patch.nVertices_; ++ivert)
            {
                extrudedShape.vertices_[nDstVertices] = optimizedShape.vertices_[ivert];
                extrudedShape.normals_[nDstVertices] = optimizedShape.normals_[ivert];
                extrudedShape.uvs_[nDstVertices] = optimizedShape.uvs_[ivert];

                nDstVertices++;
            }
            //taking care of indices
            for (uint iidx = patch.startIndex_; iidx < patch.startIndex_ + patch.nIndices_; ++iidx)
            { 
                extrudedShape.indices_[nDstIndices] = optimizedShape.indices_[iidx] + numberOfNewVertices;

                nDstIndices++;
            }

            //adding extruded vertices
            for (uint ivert = patch.startVertex_; ivert < patch.startVertex_ + patch.nVertices_; ++ivert)
            {
                extrudedShape.vertices_[nDstVertices] = optimizedShape.vertices_[ivert] - extrudeAmount * optimizedShape.normals_[ivert];
                extrudedShape.normals_[nDstVertices] = -optimizedShape.normals_[ivert];
                extrudedShape.uvs_[nDstVertices] = optimizedShape.uvs_[ivert];

                nDstVertices++;
                numberOfNewVertices++;
            }

            //adding indices in reverse order
            for (int iidx = (int)(patch.startIndex_ + patch.nIndices_) - 1; iidx >= patch.startIndex_; --iidx)
            {
                extrudedShape.indices_[nDstIndices] = optimizedShape.indices_[iidx] + numberOfNewVertices;

                nDstIndices++;
            }

            //taking care of edge vertices
            //first find all edges of a patch within just one triangle
            Dictionary<Edge_, uint> edges = new Dictionary<Edge_, uint>(new EdgeComparer_());

            for (uint tidx = patch.startIndex_; tidx < patch.startIndex_ + patch.nIndices_; tidx+=3)
            {
                Edge_ e1 = new Edge_(optimizedShape.indices_[tidx + 0], optimizedShape.indices_[tidx + 1]);
                Edge_ e2 = new Edge_(optimizedShape.indices_[tidx + 1], optimizedShape.indices_[tidx + 2]);
                Edge_ e3 = new Edge_(optimizedShape.indices_[tidx + 2], optimizedShape.indices_[tidx + 0]);

                uint val = 0;
                if (edges.TryGetValue(e1, out val))
                    edges[e1] = val + 1;
                else
                    edges[e1] = 1;

                if (edges.TryGetValue(e2, out val))
                    edges[e2] = val + 1;
                else
                    edges[e2] = 1;

                if (edges.TryGetValue(e3, out val))
                    edges[e3] = val + 1;
                else
                    edges[e3] = 1;
            }

            foreach (KeyValuePair<Edge_, uint> entry in edges)
            {
                if( entry.Value == 1 )
                {
                    //add this edge to extruded geometry

                    //we need to duplicate 4 vertices because of new normals
                    //these are:
                    int v1 = entry.Key.v1_ + lastNumberOfNewVertices;
                    int v2 = entry.Key.v2_ + lastNumberOfNewVertices;
                    int v3 = entry.Key.v1_ + (int)patch.nVertices_ + lastNumberOfNewVertices;
                    int v4 = entry.Key.v2_ + (int)patch.nVertices_ + lastNumberOfNewVertices;

                    Vector3 p1 = extrudedShape.vertices_[v1];
                    Vector3 p2 = extrudedShape.vertices_[v2];
                    Vector3 p3 = extrudedShape.vertices_[v3];
                    Vector3 p4 = extrudedShape.vertices_[v4];

                    //v1
                    extrudedShape.vertices_[nDstVertices] = extrudedShape.vertices_[v1];
                    extrudedShape.normals_[nDstVertices] = Vector3.Cross(p1 - p3, p1 - p2);
                    extrudedShape.uvs_[nDstVertices] = extrudedShape.uvs_[v1];
                    int newV1index = (int)nDstVertices;
                    nDstVertices++;
                    numberOfNewVertices++;

                    //v2
                    extrudedShape.vertices_[nDstVertices] = extrudedShape.vertices_[v2];
                    extrudedShape.normals_[nDstVertices] = Vector3.Cross(p2 - p1, p2 - p4);
                    extrudedShape.uvs_[nDstVertices] = extrudedShape.uvs_[v2];
                    int newV2index = (int)nDstVertices;
                    nDstVertices++;
                    numberOfNewVertices++;

                    //v3
                    extrudedShape.vertices_[nDstVertices] = extrudedShape.vertices_[v3];
                    extrudedShape.normals_[nDstVertices] = Vector3.Cross(p3 - p4, p3 - p1);
                    extrudedShape.uvs_[nDstVertices] = extrudedShape.uvs_[v3];
                    int newV3index = (int)nDstVertices;
                    nDstVertices++;
                    numberOfNewVertices++;

                    //v4
                    extrudedShape.vertices_[nDstVertices] = extrudedShape.vertices_[v4];
                    extrudedShape.normals_[nDstVertices] = Vector3.Cross(p4 - p2, p4 - p3);
                    extrudedShape.uvs_[nDstVertices] = extrudedShape.uvs_[v3];
                    int newV4index = (int)nDstVertices;
                    nDstVertices++;
                    numberOfNewVertices++;

                    extrudedShape.indices_[nDstIndices++] = newV1index;
                    extrudedShape.indices_[nDstIndices++] = newV3index;
                    extrudedShape.indices_[nDstIndices++] = newV2index;

                    extrudedShape.indices_[nDstIndices++] = newV2index;
                    extrudedShape.indices_[nDstIndices++] = newV3index;
                    extrudedShape.indices_[nDstIndices++] = newV4index;
                }
            }

            extrudedShape.patches_[ipatch].nVertices_ = nDstVertices - extrudedShape.patches_[ipatch].startVertex_;
            extrudedShape.patches_[ipatch].nIndices_ = nDstIndices - extrudedShape.patches_[ipatch].startIndex_;
        }

        extrudedShape.numVertices_ = (int)nDstVertices;
        extrudedShape.numIndices_ = (int)nDstIndices;

        //resize final array
        Array.Resize<Vector3>(ref extrudedShape.vertices_, extrudedShape.numVertices_);
        Array.Resize<Vector3>(ref extrudedShape.normals_, extrudedShape.numVertices_);
        Array.Resize<Vector2>(ref extrudedShape.uvs_, extrudedShape.numVertices_);

        Array.Resize<int>(ref extrudedShape.indices_, extrudedShape.numIndices_);

        extrudedShape.finalizePatches();

        return extrudedShape;
    }
}
