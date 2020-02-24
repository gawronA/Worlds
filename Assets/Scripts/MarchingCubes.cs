using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Worlds
{
    namespace ProceduralTerrain
    {
        namespace MarchingCubes
        {
            public struct Vertex
            {
                public float4 position;
                public float3 normal;
            }

            [BurstCompile]
            public struct TriangulateJob : IJobParallelFor
            {
                [ReadOnly] public int _Res;

                [ReadOnly] public NativeArray<int> _TriangleConnectionTable;
                [ReadOnly] public NativeArray<int> _CubeEdgeFlags;
                [ReadOnly] public NativeArray<float> _DensityMap;
                [ReadOnly] public NativeArray<float3> _NormalTexture;

                [NativeDisableContainerSafetyRestriction]
                [WriteOnly] public NativeArray<Vertex> _Vertices;

                static readonly int2[] _EdgeConnections =
                {
                    new int2(0, 1), new int2(1, 2), new int2(2, 3), new int2(3, 0),
                    new int2(4, 5), new int2(5, 6), new int2(6, 7), new int2(7, 4),
                    new int2(0, 4), new int2(1, 5), new int2(2, 6), new int2(3, 7)
                };

                static readonly float3[] _EdgeDirections =
                {
                    new float3(1.0f, 0.0f, 0.0f), new float3(0.0f, 1.0f, 0.0f), new float3(-1.0f, 0.0f, 0.0f), new float3(0.0f, -1.0f, 0.0f),
                    new float3(1.0f, 0.0f, 0.0f), new float3(0.0f, 1.0f, 0.0f), new float3(-1.0f, 0.0f, 0.0f), new float3(0.0f, -1.0f, 0.0f),
                    new float3(0.0f, 0.0f, 1.0f), new float3(0.0f, 0.0f, 1.0f), new float3(0.0f, 0.0f, 1.0f),  new float3(0.0f, 0.0f, 1.0f)
                };

                static readonly float3[] _VertexOffsets =
                {
                    new float3(0.0f, 0.0f, 0.0f), new float3(1.0f, 0.0f, 0.0f), new float3(1.0f, 1.0f, 0.0f), new float3(0.0f, 1.0f, 0.0f),
                    new float3(0.0f, 0.0f, 1.0f), new float3(1.0f, 0.0f, 1.0f), new float3(1.0f, 1.0f, 1.0f), new float3(0.0f, 1.0f, 1.0f)
                };

                void FillCell(int x, int y, int z, out NativeArray<float> cell)
                {
                    int res = _Res + 1;     //resolution with padding
                    int res2 = res * res;

                    cell = new NativeArray<float>(8, Allocator.Temp);
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
                    //float3 uv = position / float3(_Res);
                    //float3 uv = position / normalsTextureSize;
                    //vert.normal = _Normals.SampleLevel(_LinearClamp, uv, 0);
                    vert.normal = LinearClamp(position);
                    return vert;
                }

                float3 LinearClamp(float3 position)
                {
                    float offset = 0.5f;
                    int3 xyzindex = (int3)(position - offset);
                    float3 xyztail = (position - offset) - xyzindex;
                    
                    float3 normalA = _NormalTexture[xyzindex.x + xyzindex.y * _Res + xyzindex.z * _Res * _Res];
                    float3 normalB;

                    if(xyzindex.x + 1 >= _Res && xyzindex.y + 1 >= _Res && xyzindex.z + 1 >= _Res) normalB = new float3(0f, 0f, 0f);
                    else if(xyzindex.x + 1 >= _Res && xyzindex.y + 1 >= _Res) normalB = new float3(0f, 0f, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * _Res * _Res].z);
                    else if(xyzindex.x + 1 >= _Res && xyzindex.z + 1 >= _Res) normalB = new float3(0f, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * _Res * _Res].y, 0f);
                    else if(xyzindex.y + 1 >= _Res && xyzindex.z + 1 >= _Res) normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * _Res * _Res].x, 0f, 0f);
                    else if(xyzindex.x + 1 >= _Res) normalB = new float3(0f, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * _Res * _Res].y, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * _Res * _Res].z);
                    else if(xyzindex.y + 1 >= _Res) normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * _Res * _Res].x, 0f, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * _Res * _Res].z);
                    else if(xyzindex.z + 1 >= _Res) normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * _Res * _Res].x, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * _Res * _Res].y, 0f);
                    else normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * _Res * _Res].x, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * _Res * _Res].y, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * _Res * _Res].z);

                    float3 normal = lerp(normalA, normalB, xyztail);
                    return normalize(normal);
                }

                public void Execute(int index)
                {
                    int x = index % _Res;
                    int y = (index / _Res) % _Res;
                    int z = (index / (_Res * _Res)) % _Res;

                    NativeArray<float> cube;
                    float3 position = new float3(x, y, z);
                    NativeArray<float3> edgeVertex = new NativeArray<float3>(12, Allocator.Temp);

                    //fill the cube with density at corners
                    FillCell(x, y, z, out cube);
                    
                    //find whether corner is inside or outside the surface
                    int flagIndex = 0;
                    for(int i = 0; i < 8; i++)
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
                    cube.Dispose();
                    edgeVertex.Dispose();
                }
            }

            [BurstCompile]
            public struct ResetVertBufferJob : IJobParallelFor
            {
                [NativeDisableContainerSafetyRestriction]
                [WriteOnly] public NativeArray<Vertex> _Vertices;

                public void Execute(int index)
                {
                    for(int i = 0; i < 3 * 5; i++)
                    {
                        Vertex vertex = new Vertex();
                        vertex.position = new float4(-1f, -1f, -1f, -1f);
                        vertex.normal = new float3(0f, 0f, 0f);
                        _Vertices[index * 3 * 5 + i] = vertex;
                    }
                }
            }

            [BurstCompile]
            public struct CalculateNormalTextureJob : IJobParallelFor
            {
                [ReadOnly] public int _Res;
                [ReadOnly] public NativeArray<float> _DensityMap;
                
                [WriteOnly] public NativeArray<float3> _NormalTexture;

                float dx, dy, dz;
                float value;

                public void Execute(int index)
                {
                    int res = _Res;
                    int res2 = res * res;

                    int res_p = _Res + 1;
                    int res2_p = res_p * res_p;

                    int x = index % res;
                    int y = (index / res) % res;
                    int z = (index / (res * res)) % res;

                    value = _DensityMap[x + y * res_p + z * res2_p];
                    dx = value - _DensityMap[(x + 1) + y * res_p + z * res2_p];
                    dy = value - _DensityMap[x + (y + 1) * res_p + z * res2_p];
                    dz = value - _DensityMap[x + y * res_p + (z + 1) * res2_p];
                    float3 normal = (dx != 0f || dy != 0f || dz != 0f) ? normalize(float3(dx, dy, dz)) : 0f;
                    _NormalTexture[index] = normal;
                }
            }
        }
    }
}