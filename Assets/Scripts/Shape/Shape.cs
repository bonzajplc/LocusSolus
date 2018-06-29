using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

class Shape
{
    public Vector3[] vertices_;
    public Vector3[] normals_;
    //Vector3[] _tangents;
    //Vector3[] _bitangents;
    public Vector2[] uvs_;

    public int[] indices_;
    public int[] indicesLines_;

    public int numVertices_;
    public int numIndices_;

    public Vector3 GetPosition(int index) { return vertices_[index]; }
    public Vector3 GetNormal(int index) { return normals_[index]; }
    //public Vector3 getTangent( uint index) { return _tangents[index]; }
    //public Vector3 getBitangent( uint index) { return _bitangents[index]; }
    public Vector2 GetTexcoord(int index) { return uvs_[index]; }
    int[] GetTriangle(int index) { int[] tab = { indices_[index * 3 + 0], indices_[index * 3 + 1], indices_[index * 3 + 2] }; return tab; }

    public int GetNTrianglesInShape() { return (int)numIndices_ / 3; }

    public void AllocateShape(int numVertices, int numIndices)
    {
        vertices_ = new Vector3[numVertices];
        normals_ = new Vector3[numVertices];
        //_tangents = new Vector3[numVertices];
        //_bitangents = new Vector3[numVertices];
        uvs_ = new Vector2[numVertices];
        indices_ = new int[numIndices];

        numVertices_ = numVertices;
        numIndices_ = numIndices;
    }

    static Vector3 GetTriangleNormal(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 a = p0 - p1;
        Vector3 b = p2 - p1;

        Vector3 n = Vector3.Cross(b, a);
        return n;
    }

    protected void GenerateFlatNormals(int firstTriangle, int count)
    {
        int end = firstTriangle + count;
        for (int i = firstTriangle; i < end; ++i)
        {
            int[] tri = GetTriangle(i);

            Vector3 p0 = GetPosition(tri[0]);
            Vector3 p1 = GetPosition(tri[1]);
            Vector3 p2 = GetPosition(tri[2]);

            Vector3 n = GetTriangleNormal(p0, p1, p2);

            //Vector3 n0 = getNormal( tri[0] );
            //Vector3 n1 = getNormal( tri[1] );
            //Vector3 n2 = getNormal( tri[2] );

            normals_[tri[0]] = n;
            normals_[tri[1]] = n;
            normals_[tri[2]] = n;
        }

        for (int i = 0; i < numVertices_; ++i)
        {
            normals_[i].Normalize();
        }
    }

    public static Shape CreateShapeFromMesh(Mesh inputMesh)
    {
        Shape newMeshShape = new Shape();

        newMeshShape.numVertices_ = inputMesh.vertexCount;
        newMeshShape.numIndices_ = inputMesh.triangles.Length;

        newMeshShape.vertices_ = inputMesh.vertices;
        newMeshShape.normals_ = inputMesh.normals;

        if( inputMesh.uv.Length > 0 )
            newMeshShape.uvs_ = inputMesh.uv;

        newMeshShape.indices_ = inputMesh.triangles;

        return newMeshShape;
    }
};