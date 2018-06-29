using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

class OptimizedPatchedShape: PatchedShape
{
    struct PatchVertex_
    {
        public PatchVertex_(Vector3 position, Vector3 normal, Vector3 uv)
        {
            position_ = position;
            normal_ = normal;
            uv_ = uv;
        }
        public Vector3 position_;
        public Vector3 normal_;
        public Vector3 uv_;
    }

    static public bool V3Equal(Vector3 a, Vector3 b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }

    class PatchVertexComparer_ : IEqualityComparer<PatchVertex_>
    {
        public bool Equals(PatchVertex_ x, PatchVertex_ y)
        {
            return V3Equal(x.position_, y.position_) && V3Equal(x.normal_, y.normal_) && V3Equal(x.uv_, y.uv_);
        }

        public int GetHashCode(PatchVertex_ x)
        {
            return x.position_.GetHashCode() + x.normal_.GetHashCode() + x.uv_.GetHashCode();
        }
    }

    static public OptimizedPatchedShape CreateOptimizedPatchedShapeFromPatchedShape( PatchedShape patchedShape )
    {
        uint idxRemapCapacity = 1024;
        uint[] idxRemap = new uint[idxRemapCapacity];

        OptimizedPatchedShape optimizedShape = new OptimizedPatchedShape();
        //allocate optimized shape
        optimizedShape.AllocateShape(patchedShape.numVertices_, patchedShape.numIndices_ );
        optimizedShape.numIndices_ = 0; //we will be filling it from beginning

        optimizedShape.patches_ = new Patch_[patchedShape.nPatches_];
        optimizedShape.nPatches_ = patchedShape.nPatches_;

        uint nDstVertices = 0;

	    for (uint ipatch = 0; ipatch < patchedShape.nPatches_; ++ipatch )
	    {
		    Patch_ patch = patchedShape.patches_[ipatch];
            //optimizedShape.patches_[ipatch] = new Patch_();

		    if (patch.nVertices_ > idxRemapCapacity )
		    {
			    idxRemapCapacity = patch.nVertices_;
			    idxRemap = null;
                idxRemap = new uint[idxRemapCapacity];
		    }

            uint dstIndex = nDstVertices;
            uint srcIndex = patch.startVertex_;

            // copy first vertex
            optimizedShape.vertices_[dstIndex] = patchedShape.vertices_[srcIndex];
            optimizedShape.normals_[dstIndex] = patchedShape.normals_[srcIndex];
            optimizedShape.uvs_[dstIndex] = patchedShape.uvs_[srcIndex];

            uint nv = 1;
            idxRemap[0] = 0;

            Dictionary<PatchVertex_, uint> vmap = new Dictionary<PatchVertex_, uint>(new PatchVertexComparer_());
            PatchVertex_ v0 = new PatchVertex_(patchedShape.vertices_[srcIndex], patchedShape.normals_[srcIndex], patchedShape.uvs_[srcIndex]);

            vmap[v0] = 0;

		    for (uint ivert = 1; ivert<patch.nVertices_; ++ivert )
		    {
			    PatchVertex_ v = new PatchVertex_(patchedShape.vertices_[srcIndex + ivert], patchedShape.normals_[srcIndex + ivert], patchedShape.uvs_[srcIndex + ivert]);

                uint val = 0;
                if (vmap.TryGetValue(v, out val))
                {
                    idxRemap[ivert] = val;
                }
                else
			    {
                    // not found
                    //
                    optimizedShape.vertices_[dstIndex + nv] = patchedShape.vertices_[srcIndex + ivert];
                    optimizedShape.normals_[dstIndex + nv] = patchedShape.normals_[srcIndex + ivert];
                    optimizedShape.uvs_[dstIndex + nv] = patchedShape.uvs_[srcIndex + ivert];

                    idxRemap[ivert] = nv;
                    vmap[v] = nv;

                    ++nv;
                }

                //mesh.vertexToPatchIndex_[ivert] = ipatch;
            }

		    for (int i = 0; i<patch.nVertices_; ++i )
		    {
			    optimizedShape.indices_[optimizedShape.numIndices_] = (int)(idxRemap[i] + nDstVertices);
                optimizedShape.numIndices_++;
            }

            optimizedShape.patches_[ipatch].startVertex_ = nDstVertices;
            optimizedShape.patches_[ipatch].nVertices_ = nv;
            optimizedShape.patches_[ipatch].startIndex_ = patch.startVertex_;
            optimizedShape.patches_[ipatch].nIndices_ = patch.nVertices_;

            nDstVertices += nv;
	    }

	    idxRemap = null;

        optimizedShape.numVertices_ = (int)nDstVertices;

        return optimizedShape;
    }  
}
