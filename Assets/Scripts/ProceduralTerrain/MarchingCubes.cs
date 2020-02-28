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

            //[BurstCompile]
            public struct TriangulateJob : IJobParallelFor
            {
                [ReadOnly] public int _MeshRes;
                [ReadOnly] public int _SurfaceRes;
                [ReadOnly] public int _NormalRes;
                [ReadOnly] public float _Scale;

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

                Vertex CreateVertex(float3 position)
                {
                    Vertex vert = new Vertex();
                    vert.position = new float4((position), 1.0f) * _Scale;
                    vert.normal = Trilinear(position);
                    return vert;
                }

                Vertex CreateVertex(float3 position, float3 normal_vertex)
                {
                    Vertex vert = new Vertex();
                    vert.position = new float4((position), 1.0f) * _Scale;
                    vert.normal = normalize(cross(position, normal_vertex));
                    return vert;
                }

                float3 Trilinear(float3 position)
                {
                    int res = _NormalRes;
                    int res2 = res * res;

                    float offset = 0.5f;
                    position -= offset;

                    int x = (int)position.x;
                    int y = (int)position.y;
                    int z = (int)position.z;

                    float xtail = position.x - x;
                    float ytail = position.y - y;
                    float ztail = position.z - z;

                    float3 C000 = _NormalTexture[x + y * res + z * res2];                   //0
                    float3 C001 = _NormalTexture[x + (y + 1) * res + z * res2];             //y
                    float3 C010 = _NormalTexture[x + y * res + (z + 1) * res2];             //z
                    float3 C011 = _NormalTexture[x + (y + 1) * res + (z + 1) * res2];       //yz

                    float3 C100 = _NormalTexture[(x + 1) + y * res + z * res2];             //x
                    float3 C101 = _NormalTexture[(x + 1) + (y + 1) * res + z * res2];       //xy
                    float3 C110 = _NormalTexture[(x + 1) + y * res + (z + 1) * res2];       //xz
                    float3 C111 = _NormalTexture[(x + 1) + (y + 1) * res + (z + 1) * res2]; //xyz

                    float3 C00 = lerp(C000, C100, xtail);
                    float3 C01 = lerp(C001, C101, xtail);
                    float3 C10 = lerp(C010, C110, xtail);
                    float3 C11 = lerp(C011, C111, xtail);

                    float3 C0 = lerp(C00, C10, ztail);
                    float3 C1 = lerp(C01, C11, ztail);

                    float3 C = lerp(C0, C1, ytail);

                    return normalize(C);

                    //int3 xyzindex = (int3)(position - offset);
                    //float3 xyztail = (position - offset) - xyzindex;

                    /*float3 normalA = _NormalTexture[xyzindex.x + xyzindex.y * _Res + xyzindex.z * res2];
                    float3 normalB;

                    if(xyzindex.x + 1 >= _Res && xyzindex.y + 1 >= _Res && xyzindex.z + 1 >= _Res) normalB = new float3(0f, 0f, 0f);
                    else if(xyzindex.x + 1 >= _Res && xyzindex.y + 1 >= _Res) normalB = new float3(0f, 0f, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * res2].z);
                    else if(xyzindex.x + 1 >= _Res && xyzindex.z + 1 >= _Res) normalB = new float3(0f, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * res2].y, 0f);
                    else if(xyzindex.y + 1 >= _Res && xyzindex.z + 1 >= _Res) normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * res2].x, 0f, 0f);
                    else if(xyzindex.x + 1 >= _Res) normalB = new float3(0f, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * res2].y, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * res2].z);
                    else if(xyzindex.y + 1 >= _Res) normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * res2].x, 0f, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * res2].z);
                    else if(xyzindex.z + 1 >= _Res) normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * res2].x, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * res2].y, 0f);
                    else normalB = new float3(_NormalTexture[(xyzindex.x + 1) + xyzindex.y * _Res + xyzindex.z * res2].x, _NormalTexture[xyzindex.x + (xyzindex.y + 1) * _Res + xyzindex.z * res2].y, _NormalTexture[xyzindex.x + xyzindex.y * _Res + (xyzindex.z + 1) * res2].z);*/

                    //float3 normal = lerp(normalA, normalB, xyztail);
                    //return normalize(normal);
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