using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace DestructionEffect
{
    class DestructionVertexData
    {
        public Vector3[] _positions;
		public Vector3[] _normals;
		public Vector2[] _texcoords;

        public int[] _indices;

		public int _numVertices = 0;
		int _maxVertices = 1000;

		public void prepareVertexData( int maxVertices )
		{
			_maxVertices = maxVertices;

			_positions = new Vector3[maxVertices];
			_normals = new Vector3[maxVertices];
			_texcoords = new Vector2[maxVertices];
            _indices = new int[maxVertices * 3];
		}

		public void clearIndices()
		{
            Array.Clear(_indices, 0, _maxVertices * 3);

            _numVertices = 0;
		}

		public int allocateTriangle()
        {
            if (_numVertices + 3 > _maxVertices)
                return -1;

            int index = _numVertices;
            _numVertices += 3;

			_indices [index + 0] = index + 0;
			_indices [index + 1] = index + 1;
			_indices [index + 2] = index + 2;

            return index;
        }

        public void setPositions(int offset, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            //PICO_ASSERT(offset + 3 <= maxVertices_);
            _positions[offset + 0] = p0;
            _positions[offset + 1] = p1;
            _positions[offset + 2] = p2;
        }

        public void setNormals(int offset, Vector3 n0, Vector3 n1, Vector3 n2)
        {
            //PICO_ASSERT(offset + 3 <= maxVertices_);
            _normals[offset + 0] = n0;
            _normals[offset + 1] = n1;
            _normals[offset + 2] = n2;
        }

        //void setTangents( uint offset, Vector3 t0, Vector3 t1, Vector3 t2 )
        //{
        //    PICO_ASSERT( offset + 3 <= maxVertices_ );
        //    tangents_[offset + 0] = t0;
        //    tangents_[offset + 1] = t1;
        //    tangents_[offset + 2] = t2;
        //}

        public void setTexcoords(int offset, Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            //PICO_ASSERT(offset + 3 <= maxVertices_);
            _texcoords[offset + 0] = uv0;
            _texcoords[offset + 1] = uv1;
            _texcoords[offset + 2] = uv2;
        }
    }
}//