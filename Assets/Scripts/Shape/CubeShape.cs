using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class CubeShape: Shape
{
    bool _lineIndices = false;

    static int GetNPointsPerSide(int iterations)
    {
        int x = iterations + 1;
        return x * x;
    }

    static int GetNTrianglesPerSide(int iterations)
    {
        int x = iterations;
        return x * x * 2;
    }

    static int GetNPoints(int[] iterations, bool skipSidesWith1Iteration)
    {
        int sum = 0;
        for (int i = 0; i < 6; ++i)
        {
            if (iterations[i] == 1 && skipSidesWith1Iteration)
                continue;
            sum += GetNPointsPerSide(iterations[i]);
        }
        return sum;
    }

    static int GetNTriangles(int[] iterations, bool skipSidesWith1Iteration)
    {
        int sum = 0;
        for (int i = 0; i < 6; ++i)
        {
            if (iterations[i] == 1 && skipSidesWith1Iteration)
                continue;
            sum += GetNTrianglesPerSide(iterations[i]);
        }
        return sum;
    }

    void AllocateCube(int[] iterations, bool createLinesRS, bool skipSidesWith1Iteration)
    {
        int numVertices = GetNPoints(iterations, skipSidesWith1Iteration);
        int numTriangles = GetNTriangles(iterations, skipSidesWith1Iteration);

        AllocateShape(numVertices, numTriangles * 3);


        if (createLinesRS)
        {
            _lineIndices = true;
            indicesLines_ = new int[numTriangles * 3];
        }
    }

    void CreateCube(int[] iterations, bool skipSidesWith1Iteration, Vector3 extents)
    {
        float a = extents.x;
        float b = extents.y;
        float c = extents.z;

        Vector3[] baseCube =
        {
                new Vector3( -a, -b, c ),
                new Vector3(  a, -b, c ),
                new Vector3(  a,  b, c ),
                new Vector3( -a,  b, c ),

                new Vector3( -a, -b,-c ),
                new Vector3(  a, -b,-c ),
                new Vector3(  a,  b,-c ),
                new Vector3( -a,  b,-c ),
            };

        const int NUM_SIDES = 6;

        int[,] quads =
        {
                {0, 1, 2, 3}, // f
		        {1, 5, 6, 2}, // r
		        {5, 4, 7, 6}, // b
		        {4, 0, 3, 7}, // l
		        {3, 2, 6, 7}, // t
		        {4, 5, 1, 0}, // bt
            };

        int pointIndex = 0;
        int indexIndex = 0;
        int indexIndexLines = 0;

        for (int s = 0; s < NUM_SIDES; ++s)
        {
            int iter = iterations[s];
            if (iter == 1 && skipSidesWith1Iteration)
                continue;
            int stepsI = iter + 1;
            float step = 1.0f / (float)iter;

            Vector3 lb = baseCube[quads[s, 0]];
            Vector3 rb = baseCube[quads[s, 1]];
            Vector3 lu = baseCube[quads[s, 3]];
            Vector3 ru = baseCube[quads[s, 2]];

            int baseIndex = pointIndex;

            for (int y = 0; y < stepsI; ++y)
            {
                float baseT = (float)y * step;
                Vector3 base0 = Vector3.Lerp(lb, lu, baseT);
                Vector3 base1 = Vector3.Lerp(rb, ru, baseT);

                for (int x = 0; x < stepsI; ++x)
                {
                    float t = (float)x * step;
                    Vector3 pos = Vector3.Lerp(base0, base1, t);

                    vertices_[pointIndex] = pos;
                    uvs_[pointIndex] = new Vector2(t, baseT);
                    ++pointIndex;
                }
            }

            for (int y = 0; y < stepsI - 1; ++y)
            {
                int level0 = baseIndex + stepsI * y;
                int level1 = baseIndex + stepsI * (y + 1);

                for (int x = 0; x < stepsI - 1; ++x)
                {
                    int i0 = level0 + x;
                    int i1 = i0 + 1;
                    int i3 = level1 + x;
                    int i2 = i3 + 1;

                    indices_[indexIndex + 0] = i0;
                    indices_[indexIndex + 1] = i1;
                    indices_[indexIndex + 2] = i2;

                    indices_[indexIndex + 3] = i0;
                    indices_[indexIndex + 4] = i2;
                    indices_[indexIndex + 5] = i3;

                    indexIndex += 6;
                }
            }

            if (_lineIndices)
            {
                // this generates very inefficient index buffer for line rendering
                //
                for (int y = 0; y < stepsI - 1; ++y)
                {
                    int level0 = baseIndex + stepsI * y;
                    int level1 = baseIndex + stepsI * (y + 1);
                    for (int x = 0; x < stepsI - 1; ++x)
                    {
                        int i0 = level0 + x;
                        int i1 = i0 + 1;
                        int i3 = level1 + x;
                        int i2 = i3 + 1;

                        indicesLines_[indexIndexLines + 0] = i0;
                        indicesLines_[indexIndexLines + 1] = i1;

                        indicesLines_[indexIndexLines + 2] = i1;
                        indicesLines_[indexIndexLines + 3] = i2;

                        indicesLines_[indexIndexLines + 4] = i2;
                        indicesLines_[indexIndexLines + 5] = i0;

                        indexIndexLines += 6;

                        indicesLines_[indexIndexLines + 0] = i0;
                        indicesLines_[indexIndexLines + 1] = i2;

                        indicesLines_[indexIndexLines + 2] = i2;
                        indicesLines_[indexIndexLines + 3] = i3;

                        indicesLines_[indexIndexLines + 4] = i3;
                        indicesLines_[indexIndexLines + 5] = i0;

                        indexIndexLines += 6;
                    }
                }
            }
        }
    }

    public static Shape CreateCubeShape(int iterations, bool createLinesRS, Vector3 extents)
    {
        int[] boxIterations =
        {
                iterations, iterations, iterations, iterations, iterations, iterations,
            };

        CubeShape newCubeShape = new CubeShape();

        newCubeShape.AllocateCube(boxIterations, createLinesRS, false);
        newCubeShape.CreateCube(boxIterations, false, extents);
        newCubeShape.GenerateFlatNormals(0, newCubeShape.GetNTrianglesInShape());

        return newCubeShape;
    }
};
