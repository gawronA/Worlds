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
                [ReadOnly] public int _MeshRes;
                [ReadOnly] public int _SurfaceRes;
                [ReadOnly] public float _Scale;

                [ReadOnly] public NativeArray<int> _TriangleConnectionTable;
                [ReadOnly] public NativeArray<int> _CubeEdgeFlags;
                [ReadOnly] public NativeArray<float> _DensityMap;

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
                    int res = _SurfaceRes;
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
                    return (delta == 0.0f) ? 0.5f : -v1 / delta;
                }

                Vertex CreateVertex(float3 position, float3 normal_vertex1, float3 normal_vertex2)
                {
                    Vertex vert = new Vertex();
                    vert.position = new float4((position), 1.0f) * _Scale;
                    float3 u = normal_vertex1 - position;
                    float3 v = normal_vertex2 - position;
                    vert.normal = normalize(cross(u, v));
                    return vert;
                }

                public void Execute(int index)
                {
                    int x = index % _MeshRes;
                    int y = (index / _MeshRes) % _MeshRes;
                    int z = (index / (_MeshRes * _MeshRes)) % _MeshRes;

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
                            float3 vertex1_position = edgeVertex[_TriangleConnectionTable[flagIndex * 16 + (3 * i + 0)]];
                            float3 vertex2_position = edgeVertex[_TriangleConnectionTable[flagIndex * 16 + (3 * i + 1)]];
                            float3 vertex3_position = edgeVertex[_TriangleConnectionTable[flagIndex * 16 + (3 * i + 2)]];

                            _Vertices[buffer_index * 15 + (3 * i + 0)] = CreateVertex(vertex1_position, vertex2_position, vertex3_position);
                            _Vertices[buffer_index * 15 + (3 * i + 1)] = CreateVertex(vertex2_position, vertex3_position, vertex1_position);
                            _Vertices[buffer_index * 15 + (3 * i + 2)] = CreateVertex(vertex3_position, vertex1_position, vertex2_position);
                        }
                    }
                    cube.Dispose();
                    edgeVertex.Dispose();
                }
            }

            //[BurstCompile]
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

            //[BurstCompile]
            public struct CalculateNormalTextureJob : IJobParallelFor
            {
                [ReadOnly] public int _SurfaceRes;
                [ReadOnly] public int _NormalRes;
                [ReadOnly] public NativeArray<float> _DensityMap;
                
                [WriteOnly] public NativeArray<float3> _NormalTexture;

                float dx, dy, dz;
                float value;

                public void Execute(int index)
                {
                    int res = _SurfaceRes;
                    int res2 = res * res;

                    int x = index % _NormalRes;
                    int y = (index / _NormalRes) % _NormalRes;
                    int z = (index / (_NormalRes * _NormalRes)) % _NormalRes;

                    value = _DensityMap[x + y * res + z * res2];
                    dx = value - _DensityMap[(x + 1) + y * res + z * res2];
                    dy = value - _DensityMap[x + (y + 1) * res + z * res2];
                    dz = value - _DensityMap[x + y * res + (z + 1) * res2];
                    float3 normal = (dx != 0f || dy != 0f || dz != 0f) ? normalize(float3(dx, dy, dz)) : 0f;
                    _NormalTexture[index] = normal;
                }
            }
        }
    }
}