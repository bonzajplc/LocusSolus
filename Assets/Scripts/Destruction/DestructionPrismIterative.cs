using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using DestructionEffect;

namespace DestructionEffectIterative
{
    class AppendConsumeStack
    {
        public Vector3[] _positions;
        public Vector3[] _normals;
        public Vector2[] _texcoords;

        public int _numTriangles = 0;
        int _maxTriangles = 1000;

        public void prepareStack(int maxVertices)
        {
            _maxTriangles = maxVertices;

            _positions = new Vector3[maxVertices];
            _normals = new Vector3[maxVertices];
            _texcoords = new Vector2[maxVertices];
        }

        public bool empty()
        {
            return _numTriangles == 0;
        }

        public bool full()
        {
            return _numTriangles == _maxTriangles;
        }

        public int appendTriangle(Vector3[] positions, Vector3[] normals, Vector2[] texcoords)
        {
            return appendTriangle(positions[0], positions[1], positions[2],
                                   normals[0], normals[1], normals[2],
                                   texcoords[0], texcoords[1], texcoords[2]);
        }

        public int appendTriangle(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 n0, Vector3 n1, Vector3 n2, Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            if (_numTriangles + 3 > _maxTriangles)
                return -1;

            int index = _numTriangles;
            _numTriangles += 3;

            for( int i = 0; i < 3; i++ )
            {
                _positions[index + 0] = p0;
                _positions[index + 1] = p1;
                _positions[index + 2] = p2;

                _normals[index + 0] = n0;
                _normals[index + 1] = n1;
                _normals[index + 2] = n2;

                _texcoords[index + 0] = uv0;
                _texcoords[index + 1] = uv1;
                _texcoords[index + 2] = uv2;
            }

            return 0;
        }

        public int consumeTriangle(Vector3[] positions, Vector3[] normals, Vector2[] texcoords)
        {
            if (_numTriangles == 0 )
                return -1;

            _numTriangles -= 3;
            int index = _numTriangles;

            for (int i = 0; i < 3; i++)
            {
                positions[0] = _positions[index + 0];
                positions[1] = _positions[index + 1];
                positions[2] = _positions[index + 2];

                normals[0] = _normals[index + 0];
                normals[1] = _normals[index + 1];
                normals[2] = _normals[index + 2];

                texcoords[0] = _texcoords[index + 0];
                texcoords[1] = _texcoords[index + 1];
                texcoords[2] = _texcoords[index + 2];
            }

            return 0;
        }

    }

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

        static Vector3[] N9 = new Vector3[9];

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

                Array.Copy(n, N9, 6);
                N9[6] = Vector3.Cross(p[2] - p[5], p[4] - p[5]);
                N9[7] = Vector3.Cross(p[3] - p[5], p[2] - p[5]);
                N9[8] = Vector3.Cross(p[4] - p[5], p[0] - p[3]);

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
                    output.setNormals(triOffset, N9[in0], N9[in1], N9[in2]);
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

        static void _processWithCollision(DestructionVertexData output, Vector3[] P, Vector3[] N, Vector2[] UV, DestructionSensor[] sensors, DestructionShapeParams shapeParams)
        {
            int numSensors = sensors.Length;

            Profiler.BeginSample("_processWithCollision");

            _extrude(tmpPos, tmpNrm, P, N, shapeParams.extrude, shapeParams.scaleInv);

            Vector3 center = Vector3.zero;

            for (uint i = 0; i < 6; ++i)
            {
                pscaled[i] = Vector3.Scale(tmpPos[i], shapeParams.scale);
                center += tmpPos[i];
            }

            center *= 1.0f / 6;

            bool truncated = false;

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

                        //truncated = false;
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

        static Vector3[] tmpP = new Vector3[3];
        static Vector3[] tmpN = new Vector3[3];
        static Vector2[] tmpUV = new Vector2[3];

        static float[] scaledLen = new float[3];

        static void _processIterative(DestructionVertexData output, AppendConsumeStack stack, DestructionSensor[] sensors, DestructionShapeParams shapeParams)
        {
            int numSensors = sensors.Length;

            while (!stack.empty())
            {
                bool collisions = false;

                //consume one triangle from a stack
                stack.consumeTriangle(tmpP, tmpN, tmpUV);

                Profiler.BeginSample("_testCollision");
                for (uint i = 0; i < numSensors && !collisions; ++i)
                {
                    collisions = _testCollision(tmpP, sensors[i].posLS, sensors[i].radius, shapeParams.scale);
                }
                Profiler.EndSample();

                if (!collisions)
                {
                    _processNoCollision(output, tmpP, tmpN, tmpUV, shapeParams.extrude, shapeParams.scaleInv);
                    continue;
                }

                //const float VARIATION_THRESHOLD = 0.2f;

                Vector3 ea = tmpP[1] - tmpP[0];
                Vector3 eb = tmpP[2] - tmpP[1];
                Vector3 ec = tmpP[0] - tmpP[2];

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

                if (partition != 0)
                {
                    int numChildVertices = bitcount(partition);

                    if (numChildVertices == 1 || numChildVertices == 2) // two new triangles
                    {
                        Profiler.BeginSample("_twoNewTriangles");
 
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
                            Vector3 tmpP3 = tmpP[0] + ea * 0.5f;
                            Vector2 tmpUV3 = Vector3.Lerp(tmpUV[0], tmpUV[1], 0.5f);
                            Vector3 tmpN3 = Vector3.Normalize(tmpN[0] + tmpN[1]);

                            stack.appendTriangle(tmpP[0], tmpP3, tmpP[2], tmpN[0], tmpN3, tmpN[2], tmpUV[0], tmpUV3, tmpUV[2]);
                            stack.appendTriangle(tmpP3, tmpP[1], tmpP[2], tmpN3, tmpN[1], tmpN[2], tmpUV3, tmpUV[1], tmpUV[2]);
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
                            Vector3 tmpP3 = tmpP[1] + eb * 0.5f;
                            Vector2 tmpUV3 = Vector3.Lerp(tmpUV[1], tmpUV[2], 0.5f);
                            Vector3 tmpN3 = Vector3.Normalize(tmpN[1] + tmpN[2]);

                            stack.appendTriangle(tmpP[0], tmpP[1], tmpP3, tmpN[0], tmpN[1], tmpN3, tmpUV[0], tmpUV[1], tmpUV3);
                            stack.appendTriangle(tmpP[0], tmpP3, tmpP[2], tmpN[0], tmpN3, tmpN[2], tmpUV[0], tmpUV3, tmpUV[2]);
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
                            Vector3 tmpP3 = tmpP[2] + ec * 0.5f;
                            Vector2 tmpUV3 = Vector3.Lerp(tmpUV[2], tmpUV[0], 0.5f);
                            Vector3 tmpN3 = Vector3.Normalize(tmpN[2] + tmpN[0]);

                            stack.appendTriangle(tmpP[0], tmpP[1], tmpP3, tmpN[0], tmpN[1], tmpN3, tmpUV[0], tmpUV[1], tmpUV3);
                            stack.appendTriangle(tmpP[1], tmpP[2], tmpP3, tmpN[1], tmpN[2], tmpN3, tmpUV[1], tmpUV[2], tmpUV3);
                        }

                        Profiler.EndSample();// "_twoNewTriangles"
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

                        const float ONE_OVER_THREE = 1.0f / 3.0f;

                        //Vector3 newPosition = 
                        Vector3 tmpP3 = (tmpP[0] + tmpP[1] + tmpP[2]) * ONE_OVER_THREE;
                        Vector2 tmpUV3 = (tmpUV[0] + tmpUV[1] + tmpUV[2]) * ONE_OVER_THREE;
                        Vector3 tmpN3 = (tmpN[0] + tmpN[1] + tmpN[2]) * ONE_OVER_THREE;

                        //static ushort[] triIndices = //[3 * 3] =
                        //{
                        //    0,1,3,
                        //    1,2,3,
                        //    2,0,3,
                        //};

                        stack.appendTriangle(tmpP[0], tmpP[1], tmpP3, tmpN[0], tmpN[1], tmpN3, tmpUV[0], tmpUV[1], tmpUV3);
                        stack.appendTriangle(tmpP[1], tmpP[2], tmpP3, tmpN[1], tmpN[2], tmpN3, tmpUV[1], tmpUV[2], tmpUV3);
                        stack.appendTriangle(tmpP[2], tmpP[0], tmpP3, tmpN[2], tmpN[0], tmpN3, tmpUV[2], tmpUV[0], tmpUV3);

                        Profiler.EndSample();// "_threeNewTriangles"
                   }
                }
                else
                {
                    _processWithCollision(output, tmpP, tmpN, tmpUV, sensors, shapeParams);
                }
            }
        }

        public static void generateTrianglesFromShape(DestructionVertexData output, AppendConsumeStack stack, Shape shape, DestructionShapeParams shapeParams, DestructionSensor[] sensors)
        {
            //rhi::debugDraw::addSphere( toVector3( sensor.posLSWithScale ), sensor.radius, 0xFF0000FF, true );
            tmpUV[0] = Vector3.zero;
            tmpUV[1] = Vector3.zero;
            tmpUV[2] = Vector3.zero;

            for (int itri = 0; itri < shape.numIndices_; itri += 3)
            {
                int itri0 = shape.indices_[itri];
                int itri1 = shape.indices_[itri + 1];
                int itri2 = shape.indices_[itri + 2];

                tmpP[0] = shape.GetPosition(itri0);
                tmpP[1] = shape.GetPosition(itri1);
                tmpP[2] = shape.GetPosition(itri2);

                tmpN[0] = shape.GetNormal(itri0);
                tmpN[1] = shape.GetNormal(itri1);
                tmpN[2] = shape.GetNormal(itri2);

                //Vector3 t[3] =
                //{
                //    Vector3( picoPolyShape::getTangent( shape, itri0 ) ),
                //    Vector3( picoPolyShape::getTangent( shape, itri1 ) ),
                //    Vector3( picoPolyShape::getTangent( shape, itri2 ) ),
                //};

                if( shape.uvs_ != null && shape.uvs_.Length > 0 )
                {
                    tmpUV[0] = shape.GetTexcoord(itri0);
                    tmpUV[1] = shape.GetTexcoord(itri1);
                    tmpUV[2] = shape.GetTexcoord(itri2);
                }

                stack.appendTriangle(tmpP, tmpN, tmpUV);

                if( stack.full() )
                {
                    _processIterative(output, stack, sensors, shapeParams);
                }
            }
            //process remaining triangles from stack
            _processIterative(output, stack, sensors, shapeParams);
        }
    }
}//