using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
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
                public float3 position;
                public float3 normal;
            }

            public struct Triangle
            {
                public Vertex v1, v2, v3;
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

                [WriteOnly] public NativeQueue<Triangle>.ParallelWriter _Triangles;

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
                    vert.position = position * _Scale;
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

                            Vertex v1 = CreateVertex(vertex1_position, vertex2_position, vertex3_position);
                            Vertex v2 = CreateVertex(vertex2_position, vertex3_position, vertex1_position);
                            Vertex v3 = CreateVertex(vertex3_position, vertex1_position, vertex2_position);

                            Triangle triangle = new Triangle()
                            {
                                v1 = v1,
                                v2 = v2,
                                v3 = v3
                            };
                            _Triangles.Enqueue(triangle);
                        }
                    }
                    cube.Dispose();
                    edgeVertex.Dispose();
                }
            }
        }
    }
}