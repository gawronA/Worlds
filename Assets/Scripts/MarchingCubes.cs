using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Worlds
{
    namespace ProceduralTerrain
    {
        namespace MarchingCubes
        {
            public struct Vertex
            {
                public Vector4 position;
                //float3 normal;
            }

            public struct TriangulateJob : IJobParallelFor
            {
                /*struct Vertex
                {
                    public float4 position;
                    //float3 normal;
                }*/

                int _Res;

                [ReadOnly] NativeArray<int> _TriangleConnectionTable;
                [ReadOnly] NativeArray<int> _CubeEdgeFlags;
                [ReadOnly] NativeArray<float> _DensityMap;

                NativeArray<Vertex> _Vertices;

                static int2[] _EdgeConnections =
                {
                    new int2(0, 1), new int2(1, 2), new int2(2, 3), new int2(3, 0),
                    new int2(4, 5), new int2(5, 6), new int2(6, 7), new int2(7, 4),
                    new int2(0, 4), new int2(1, 5), new int2(2, 6), new int2(3, 7)
                };

                static float3[] _EdgeDirections =
                {
                    new float3(1.0f, 0.0f, 0.0f), new float3(0.0f, 1.0f, 0.0f), new float3(-1.0f, 0.0f, 0.0f), new float3(0.0f, -1.0f, 0.0f),
                    new float3(1.0f, 0.0f, 0.0f), new float3(0.0f, 1.0f, 0.0f), new float3(-1.0f, 0.0f, 0.0f), new float3(0.0f, -1.0f, 0.0f),
                    new float3(0.0f, 0.0f, 1.0f), new float3(0.0f, 0.0f, 1.0f), new float3(0.0f, 0.0f, 1.0f),  new float3(0.0f, 0.0f, 1.0f)
                };

                static float3[] _VertexOffsets =
                {
                    new float3(0.0f, 0.0f, 0.0f), new float3(1.0f, 0.0f, 0.0f), new float3(1.0f, 1.0f, 0.0f), new float3(0.0f, 1.0f, 0.0f),
                    new float3(0.0f, 0.0f, 1.0f), new float3(1.0f, 0.0f, 1.0f), new float3(1.0f, 1.0f, 1.0f), new float3(0.0f, 1.0f, 1.0f)
                };

                void FillCell(int x, int y, int z, out float[] cell)
                {
                    int res = _Res;
                    int res2 = res * res;

                    cell = new float[12];
                    cell[0] = _DensityMap[x + y * res + z * res2];
                    cell[1] = _DensityMap[(x + 1) + y * res + z * res2];
                    cell[2] = _DensityMap[(x + 1) + ((y + 1) * res) + (z * res2)];
                    cell[3] = _DensityMap[x + (y + 1) * res + z * res2];

                    cell[4] = _DensityMap[x + y * res + (z + 1) * res2];
                    cell[5] = _DensityMap[(x + 1) + y * res + (z + 1) * res2];
                    cell[6] = _DensityMap[(x + 1) + (y + 1) * res + (z + 1) * res2];
                    cell[7] = _DensityMap[x + (y + 1) * res + (z + 1) * res2];
                }

                float CalculateOffset(float v1, float v2)
                {
                    float delta = v2 - v1;
                    //return (delta == 0.0f) ? 0.5f : (_DensityOffset - v1) / delta;
                    return (delta == 0.0f) ? 0.5f : -v1 / delta;
                }

                Vertex CreateVertex(float3 position/*, float3 normalsTextureSize*/)
                {
                    Vertex vert = new Vertex();
                    vert.position = new float4((position), 1.0f);

                    //float3 uv = position / normalsTextureSize;
                    //vert.normal = _Normals.SampleLevel(_LinearClamp, uv, 0);
                    return vert;
                }

                public void Execute(int index)
                {
                    int x = index % _Res;
                    int y = (index / _Res) % _Res;
                    int z = (index / (_Res * _Res)) % _Res;

                    float[] cube = new float[8];
                    float3 position = new float3(x, y, z);
                    float3[] edgeVertex = new float3[12];

                    //fill the cube with density at corners
                    FillCell(x, y, z, out cube);

                    //find whether corner is inside or outside the surface
                    int flagIndex = 0;
                    for(int i = 0; i < 8; i++)
                        //if(cube[i] <= _DensityOffset)
                        if(cube[i] <= 0.0f)
                            flagIndex |= 1 << i;

                    //Find which edges are intersected by the surface
                    int edgesFlags = _CubeEdgeFlags[flagIndex];

                    //If there is no intersections, return
                    if(edgesFlags == 0)
                        return;

                    //Find appropriate edge intersection points
                    for(int i = 0; i < 12; i++)
                    {
                        if((edgesFlags & (1 << i)) != 0)
                        {
                            float offset = CalculateOffset(cube[_EdgeConnections[i].x], cube[_EdgeConnections[i].y]);
                            edgeVertex[i] = position + _VertexOffsets[_EdgeConnections[i].x] + (offset * _EdgeDirections[i]);
                        }
                    }

                    //Do the triangulation
                    int buffer_index = index;
                    float3 normalsTextureSize = new float3(_Res);
                    for(int i = 0; i < 5; i++) //up to 5 triangles
                    {
                        if(_TriangleConnectionTable[flagIndex * 16 + 3 * i] >= 0)
                        {
                            float3 vertex_position;
                            vertex_position = edgeVertex[_TriangleConnectionTable[flagIndex * 16 + (3 * i + 0)]];
                            _Vertices[buffer_index * 15 + (3 * i + 0)] = CreateVertex(vertex_position);
                            vertex_position = edgeVertex[_TriangleConnectionTable[flagIndex * 16 + (3 * i + 1)]];
                            _Vertices[buffer_index * 15 + (3 * i + 1)] = CreateVertex(vertex_position);
                            vertex_position = edgeVertex[_TriangleConnectionTable[flagIndex * 16 + (3 * i + 2)]];
                            _Vertices[buffer_index * 15 + (3 * i + 2)] = CreateVertex(vertex_position);
                        }
                    }
                }
            }
        
            public struct ResetVertBufferJob :IJobParallelFor
            {
                /*struct Vertex
                {
                    public float4 position;
                    //float3 normal;
                };*/
                [NativeDisableContainerSafetyRestriction]
                [WriteOnly] public NativeArray<Vertex> _Vertices;

                public void Execute(int index)
                {
                    for(int i = 0; i < 3 * 5; i++)
                    {
                        Vertex vertex = new Vertex();
                        vertex.position = new float4(-1f, -1f, -1f, -1f);
                        //vertex.normal = new float3(0f, 0f, 0f);
                        _Vertices[index * 3 * 5 + i] = vertex;
                    }
                }
            }
        }
    }
}