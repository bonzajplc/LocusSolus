using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using DestructionEffect;

namespace DestructionEffectRecursive
{
    class DestructionPrism
    {
        static uint N_PRISM_TRIS = 8;

       /*
        
       5_______________4
       |-            * |
       |  --        *  |
       |     --    *   |
       |          3    |
       |          |    |
       |          |    |
       |          |    |
       |          |    |
       2----------|----1
        --        |  *
           --     | *
              --  |*
                  0*/

        static uint[] prismIndices =
        {
            0,1,2,
            4,5,2,
            4,2,1,
            5,3,0,
            5,0,2,
            3,4,1,
            3,1,0,
            3,5,4,
        };

        static uint[] prismNormalIndices =
        {
            0,1,2,
            6,6,6,
            6,6,6,
            7,7,7,
            7,7,7,
            8,8,8,
            8,8,8,
            3,5,4,
        };

        static uint[] truncatedPrismIndices =
        {
            0,1,2,
            3,5,4,
        };

        static uint[] truncatedPrismNormalIndices =
        {
            0,1,2,
            3,5,4,
        };

        static void _extrude( Vector3[] prismPos, Vector3[] prismNrm, Vector3[] P, Vector3[] N, Vector3 amount, Vector3 scaleInv)
        {
            Profiler.BeginSample("_extrude");
            Vector3 shift = Vector3.Min(Vector3.one, scaleInv) * 0.001f;

            prismPos[0] = P[0];
            prismPos[1] = P[1];
            prismPos[2] = P[2];
            prismPos[3] = P[0] - Vector3.Scale(N[0], amount) - Vector3.Scale(P[0], shift);
            prismPos[4] = P[1] - Vector3.Scale(N[1], amount) - Vector3.Scale(P[1], shift);
            prismPos[5] = P[2] - Vector3.Scale(N[2], amount) - Vector3.Scale(P[2], shift);


            prismNrm[0] = N[0];
            prismNrm[1] = N[1];
            prismNrm[2] = N[2];
            prismNrm[3] = -N[0];
            prismNrm[4] = -N[1];
            prismNrm[5] = -N[2];
            Profiler.EndSample();
        }

        static Vector3[] N = new Vector3[9];

        static void _writePrism(DestructionVertexData output, Vector3[] p, Vector3[] n, Vector2[] uv, bool truncated )
        {
            if (truncated)
            {
                Profiler.BeginSample("_writeTruncatedPrism");

                //just two triangles
                for (uint i = 0; i < 2 * 3; i += 3)
                {
                    int triOffset = output.allocateTriangle();
                    if (triOffset == -1)
                        break;

                    uint i0 = truncatedPrismIndices[i + 0];
                    uint i1 = truncatedPrismIndices[i + 1];
                    uint i2 = truncatedPrismIndices[i + 2];

                    uint in0 = truncatedPrismNormalIndices[i + 0];
                    uint in1 = truncatedPrismNormalIndices[i + 1];
                    uint in2 = truncatedPrismNormalIndices[i + 2];

                    uint i0m = i0 % 3;
                    uint i1m = i1 % 3;
                    uint i2m = i2 % 3;

                    output.setPositions(triOffset, p[i0], p[i1], p[i2]);
                    output.setNormals(triOffset, n[in0], n[in1], n[in2]);
                    //output->setTangents ( triOffset, t[i0m] , t[i1m] , t[i2m] );
                    output.setTexcoords(triOffset, uv[i0m], uv[i1m], uv[i2m]);
                }

                Profiler.EndSample();
            }
            else
            {
                Profiler.BeginSample("_writePrism");

                Array.Copy(n, N, 6);
                N[6] = Vector3.Cross(p[2] - p[5], p[4] - p[5]);
                N[7] = Vector3.Cross(p[3] - p[5], p[2] - p[5]);
                N[8] = Vector3.Cross(p[4] - p[5], p[0] - p[3]);

                //          {
                //              n[0], n[1], n[2], n[3], n[4], n[5],
                //              ( Vector3.Cross( p[2] - p[5], p[4] - p[5] ) ),
                //              ( Vector3.Cross( p[3] - p[5], p[2] - p[5] ) ),
                //              ( Vector3.Cross( p[4] - p[3], p[0] - p[3] ) ),
                //          };

                for (uint i = 0; i < N_PRISM_TRIS * 3; i += 3)
                {
                    int triOffset = output.allocateTriangle();
                    if (triOffset == -1)
                        break;

                    uint i0 = prismIndices[i + 0];
                    uint i1 = prismIndices[i + 1];
                    uint i2 = prismIndices[i + 2];

                    uint in0 = prismNormalIndices[i + 0];
                    uint in1 = prismNormalIndices[i + 1];
                    uint in2 = prismNormalIndices[i + 2];

                    uint i0m = i0 % 3;
                    uint i1m = i1 % 3;
                    uint i2m = i2 % 3;

                    output.setPositions(triOffset, p[i0], p[i1], p[i2]);
                    output.setNormals(triOffset, N[in0], N[in1], N[in2]);
                    //output->setTangents ( triOffset, t[i0m] , t[i1m] , t[i2m] );
                    output.setTexcoords(triOffset, uv[i0m], uv[i1m], uv[i2m]);
                }
                Profiler.EndSample();
            }
        }

        static Vector3[] tmpPos = new Vector3[6];
        static Vector3[] tmpNrm = new Vector3[6];
        static Vector3[] pscaled = new Vector3[6];

        static void _processNoCollision(DestructionVertexData output, Vector3[] P, Vector3[] N, Vector2[] UV, Vector3 prismExtrude, Vector3 scaleInv)
        {
            Profiler.BeginSample("_processNoCollision");
            _extrude(tmpPos, tmpNrm, P, N, prismExtrude, scaleInv);
            _writePrism(output, tmpPos, tmpNrm, UV, true);
            Profiler.EndSample();
        }

        static float picoSmoothstep(float min, float max, float t)
        {
            //t = std::min( std::max( min, t ), max );
            t = Mathf.Clamp(t, min, max);
            float x = (t - min) / (max - min);
            return x * x * (3.0f - 2.0f * x);
        }

        static void _processWithCollision(DestructionVertexData output, Vector3[] P, Vector3[] N, Vector2[] UV, DestructionSensor[] sensors, uint numSensors, DestructionShapeParams shapeParams)
        {
            Profiler.BeginSample("_processWithCollision");

            _extrude(tmpPos, tmpNrm, P, N, shapeParams.extrude, shapeParams.scaleInv);

            Vector3 center = Vector3.zero;

            for (uint i = 0; i < 6; ++i)
            {
                pscaled[i] = Vector3.Scale(tmpPos[i], shapeParams.scale);
                center += tmpPos[i];
            }

            center *= 1.0f / 6;

            bool truncated = true;

            // --- collision with sensor
            for (uint isensor = 0; isensor < numSensors; ++isensor)
            {
                DestructionSensor sensor = sensors[isensor];

                for (uint i = 0; i < 6; ++i)
                {
                    Vector3 diff = pscaled[i] - sensor.posLS;

                    float dist = diff.sqrMagnitude; //length squared

                    if (dist < sensor.radius * sensor.radius)
                    {
                        dist = Mathf.Sqrt(dist);
                        float s = picoSmoothstep(sensor.innerRadius, sensor.radius, dist);
                        tmpPos[i] = Vector3.Lerp(center, tmpPos[i], s);

                        truncated = false;
                    }
                }
            }
            _writePrism(output, tmpPos, tmpNrm, UV, truncated);

            Profiler.EndSample();
        }

        static bool _testCollision(Vector3[] p, Vector3 sensorPos, float sensorRadius, Vector3 scale)
        {
            Vector3 scaledP0 = Vector3.Scale(p[0], scale);
            Vector3 scaledP1 = Vector3.Scale(p[1], scale);
            Vector3 scaledP2 = Vector3.Scale(p[2], scale);

            Vector3 minAABB = Vector3.Min(Vector3.Min(scaledP0, scaledP1), scaledP2);
            Vector3 maxAABB = Vector3.Max(Vector3.Max(scaledP0, scaledP1), scaledP2);

            Vector3 pointOnAABB = Vector3.Min(Vector3.Max(sensorPos, minAABB), maxAABB);
            Vector3 v = pointOnAABB - sensorPos;
            float dsqr = v.sqrMagnitude; //length squared
            bool collide = dsqr <= (sensorRadius * sensorRadius);

            return collide;
        }

        static uint getMaxIndex(float[] values)
        {
            uint maxindex = (values[0] > values[1]) ? (uint)0 : 1;
            return (values[maxindex] > values[2]) ? maxindex : 2;
        }

        static int bitcount(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        static void _processRecurse(DestructionVertexData output, Vector3[] P, Vector3[] N, Vector2[] UV, DestructionSensor[] sensors, uint numSensors, DestructionShapeParams shapeParams, uint depth)
        {
            bool collisions = false;

            Profiler.BeginSample("_testCollision");
            for (uint i = 0; i < numSensors && !collisions; ++i)
            {
                collisions = _testCollision(P, sensors[i].posLS, sensors[i].radius, shapeParams.scale);
            }
            Profiler.EndSample();

            if (!collisions)
            {
                _processNoCollision(output, P, N, UV, shapeParams.extrude, shapeParams.scaleInv);
                return;
            }

            //const float VARIATION_THRESHOLD = 0.2f;

            Vector3 ea = P[1] - P[0];
            Vector3 eb = P[2] - P[1];
            Vector3 ec = P[0] - P[2];

            float[] scaledLen = new float[3];
            ushort[] tmpIndices = new ushort[2 * 3];

            scaledLen[0] = Vector3.Scale(ea, shapeParams.scale).sqrMagnitude; // length squared
            scaledLen[1] = Vector3.Scale(eb, shapeParams.scale).sqrMagnitude; // length squared
            scaledLen[2] = Vector3.Scale(ec, shapeParams.scale).sqrMagnitude; // length squared

            float x = shapeParams.voxelRadius * 2.0f;
            float xsqr = x * x;

            Profiler.BeginSample("_partitioning");

            uint maxi = getMaxIndex(scaledLen);
            int partition = 0;

            if (scaledLen[maxi] > xsqr)
            {
                partition |= 1 << (int)maxi;

                float maxiLen = scaledLen[maxi];
                const float THRESHOLD = 2.5f;

                if (maxi == 0)
                {
                    partition |= ((maxiLen / scaledLen[1]) < THRESHOLD) ? 1 << 1 : 0;
                    partition |= ((maxiLen / scaledLen[2]) < THRESHOLD) ? 1 << 2 : 0;
                }
                else if (maxi == 1)
                {
                    partition |= ((maxiLen / scaledLen[0]) < THRESHOLD) ? 1 << 0 : 0;
                    partition |= ((maxiLen / scaledLen[2]) < THRESHOLD) ? 1 << 2 : 0;
                }
                else
                {
                    partition |= ((maxiLen / scaledLen[0]) < THRESHOLD) ? 1 << 0 : 0;
                    partition |= ((maxiLen / scaledLen[1]) < THRESHOLD) ? 1 << 1 : 0;
                }
            }

            Profiler.EndSample(); //"_partitioning"

            if (partition != 0 && depth < 16)
            {
                int numChildVertices = bitcount(partition);

                if (numChildVertices == 1 || numChildVertices == 2) // two new triangles
                {
                    Profiler.BeginSample("_twoNewTriangles");
                    Vector3[] pp =
                    {
                        P[0], P[1], P[2], Vector3.zero
                    };

                    Vector2[] uv =
                    {
                        UV[0], UV[1], UV[2], Vector2.zero
                    };

                    Vector3[] n =
                    {
                        N[0], N[1], N[2], Vector3.zero
                    };

                    // select longest edge

                    uint maxindex = (scaledLen[0] > scaledLen[1]) ? (uint)0 : 1;
                    maxindex = (scaledLen[maxindex] > scaledLen[2]) ? maxindex : 2;

                    if (maxindex == 0)
                    {
                        /*
                                        2
                                       *  *
                                     *      *
                                   *          *
                                 *              *
                               *                  *
                             *                      *
                           *                          *
                          0-------------3---------------1
                          */
                        pp[3] = P[0] + ea * 0.5f;
                        uv[3] = Vector3.Lerp(UV[0], UV[1], 0.5f);
                        n[3] = Vector3.Normalize(N[0] + N[1]);

                        tmpIndices[0] = 0; tmpIndices[1] = 3; tmpIndices[2] = 2;
                        tmpIndices[3] = 3; tmpIndices[4] = 1; tmpIndices[5] = 2;
                    }
                    else if (maxindex == 1)
                    {
                        /*
                                        2
                                       * *
                                     *     *
                                   *         *
                                 *             3
                               *                 *
                             *                     *
                           *                         *
                          0----------------------------1
                          */
                        pp[3] = P[1] + eb * 0.5f;
                        uv[3] = Vector3.Lerp(UV[1], UV[2], 0.5f);
                        n[3] = Vector3.Normalize(N[1] + N[2]);

                        tmpIndices[0] = 0; tmpIndices[1] = 1; tmpIndices[2] = 3;
                        tmpIndices[3] = 0; tmpIndices[4] = 3; tmpIndices[5] = 2;

                    }
                    else
                    {
                        /*
                                        2
                                       * *
                                     *     *
                                   *         *
                                 3             *
                               *                 *
                             *                     *
                           *                         *
                          0----------------------------1
                          */
                        pp[3] = P[2] + ec * 0.5f;
                        uv[3] = Vector3.Lerp(UV[2], UV[0], 0.5f);
                        n[3] = Vector3.Normalize(N[2] + N[0]);

                        tmpIndices[0] = 0; tmpIndices[1] = 1; tmpIndices[2] = 3;
                        tmpIndices[3] = 1; tmpIndices[4] = 2; tmpIndices[5] = 3;
                    }

                    Profiler.EndSample();// "_twoNewTriangles"

                    for (uint i = 0; i < 2 * 3; i += 3)
                    {
                        ushort i0 = tmpIndices[i + 0];
                        ushort i1 = tmpIndices[i + 1];
                        ushort i2 = tmpIndices[i + 2];
                        Vector3[] trip = { pp[i0], pp[i1], pp[i2] };
                        Vector2[] triuv = { uv[i0], uv[i1], uv[i2] };
                        Vector3[] trin = { n[i0], n[i1], n[i2] };
                        _processRecurse(output, trip, trin, triuv, sensors, numSensors, shapeParams, depth + 1);
                    }
                }
                else //three new triangles
                {
                    Profiler.BeginSample("_threeNewTriangles");
                    /*
                                    2
                                   *| *
                                 *  |   *
                               *    |     *
                              *     |       *
                            *       3         *
                          *     *       *       *
                         * *                 *   *
                       0---------------------------1


                       */
                    ushort[] indices = //[3 * 3] =
                    {
                        0,1,3,
                        1,2,3,
                        2,0,3,
                    };

                    const float ONE_OVER_THREE = 1.0f / 3.0f;

                    Vector3[] pp =
                    {
                        P[0], P[1], P[2],
                        ( P[0] + P[1] + P[2] ) * ONE_OVER_THREE,
                    };

                    Vector2[] uv =
                    {
                        UV[0], UV[1], UV[2],
                        ( UV[0] + UV[1] + UV[2] ) * ONE_OVER_THREE,
                    };

                    Vector3[] n =
                    {
                        N[0], N[1], N[2],
                        ( N[0] + N[1] + N[2] ) * ONE_OVER_THREE,
                    };

                    Profiler.EndSample();// "_threeNewTriangles"

                    for (uint i = 0; i < 3 * 3; i += 3)
                    {
                        ushort i0 = indices[i + 0];
                        ushort i1 = indices[i + 1];
                        ushort i2 = indices[i + 2];
                        Vector3[] trip = { pp[i0], pp[i1], pp[i2] };
                        Vector2[] triuv = { uv[i0], uv[i1], uv[i2] };
                        Vector3[] trin = { n[i0], n[i1], n[i2] };
                        _processRecurse(output, trip, trin, triuv, sensors, numSensors, shapeParams, depth + 1);
                    }
                }
            }
            else
            {
                _processWithCollision(output, P, N, UV, sensors, numSensors, shapeParams);
            }
        }

        public static bool generateTrianglesFromShape(DestructionVertexData output, Shape shape, DestructionShapeParams shapeParams, DestructionSensor[] sensors, uint numSensors)
        {
            //rhi::debugDraw::addSphere( toVector3( sensor.posLSWithScale ), sensor.radius, 0xFF0000FF, true );

            bool collisionState = false;

            for (int itri = 0; itri < shape.numIndices_; itri += 3)
            {
                int itri0 = shape.indices_[itri];
                int itri1 = shape.indices_[itri + 1];
                int itri2 = shape.indices_[itri + 2];

                Vector3[] n =
                {
                    shape.GetNormal( itri0 ),
                    shape.GetNormal( itri1 ),
                    shape.GetNormal( itri2 ),
                };
                //Vector3 t[3] =
                //{
                //    Vector3( picoPolyShape::getTangent( shape, itri0 ) ),
                //    Vector3( picoPolyShape::getTangent( shape, itri1 ) ),
                //    Vector3( picoPolyShape::getTangent( shape, itri2 ) ),
                //};

                Vector2[] uv =
                {
                    shape.GetTexcoord( itri0 ),
                    shape.GetTexcoord( itri1 ),
                    shape.GetTexcoord( itri2 ),
                };

                Vector3[] p =
                {
                    shape.GetPosition( itri0 ),
                    shape.GetPosition( itri1 ),
                    shape.GetPosition( itri2 ),
                };

                bool collisions = false;

                for (uint i = 0; i < numSensors && !collisions; ++i)
                {
                    collisions = _testCollision(p, sensors[i].posLS, sensors[i].radius, shapeParams.scale);
                }

                if (collisions)
                {
                    collisionState = true;
                    _processRecurse(output, p, n, uv, sensors, numSensors, shapeParams, 0);
                }
                else
                {
                    int triOffset = output.allocateTriangle();
                    if (triOffset == -1)
                        break;

                    output.setPositions(triOffset, p[0], p[1], p[2]);
                    output.setNormals(triOffset, n[0], n[1], n[2]);
                    //output->setTangents ( triOffset, t[i0m] , t[i1m] , t[i2m] );
                    output.setTexcoords(triOffset, uv[0], uv[1], uv[2]);
                }
            }

            return collisionState;
        }
    }
}//