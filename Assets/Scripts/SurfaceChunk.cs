using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Worlds.ProceduralTerrain.MarchingCubes;

public class SurfaceChunk : MonoBehaviour
{
    public bool m_Refresh = false;
    public bool m_Draw_normals = false;
    private int m_id;
    private Surface m_surface;
    private Vector3Int m_position;
    private int m_offset;

    public int m_res { get; private set; }
    private int m_res2;
    private int m_res3;
    private int m_res_p;
    private int m_res2_p;
    private int m_res3_p;
    private int m_full_res_p;
    private int m_full_res2_p;
    private int m_maxVerts;

    private NativeArray<Vertex> m_verticesBuffer;
    private NativeArray<float3> m_normalTexture;
    private NativeArray<float> m_surfaceValues;
    private JobHandle m_triangulateJobHandle;
    private bool m_refreshed = false;

    private MeshFilter m_meshFilter;

    //MC buffers
    NativeArray<int> m_cubeEdgeFlagsBuffer;
    NativeArray<int> m_triangleConnectionTableBuffer;

    private void Awake()
    {
        m_meshFilter = GetComponent<MeshFilter>();
    }

    private void Update()
    {
        if(m_refreshed) CompleteTriangulation();
        if(m_Refresh)
        {
            Refresh();
            if(m_Draw_normals)
            {
                for(int i = 0; i < m_meshFilter.mesh.normals.Length; i++)
                {
                    Debug.DrawLine(m_meshFilter.mesh.vertices[i], m_meshFilter.mesh.vertices[i] + m_meshFilter.mesh.normals[i], Color.green, 60f);
                }
                m_Refresh = false;
            }
        }
    }

    private void OnDestroy()
    {
        m_triangulateJobHandle.Complete();
        if(m_cubeEdgeFlagsBuffer != null && m_cubeEdgeFlagsBuffer.Length != 0) m_cubeEdgeFlagsBuffer.Dispose();
        if(m_triangleConnectionTableBuffer != null && m_triangleConnectionTableBuffer.Length != 0) m_triangleConnectionTableBuffer.Dispose();
    }

    public void Initalize(int index)
    {
        m_id = index;

        m_surface = GetComponentInParent<Surface>();
        m_position = new Vector3Int(m_id % m_surface.m_num_of_chunks, (m_id / m_surface.m_num_of_chunks) % m_surface.m_num_of_chunks, (m_id / (m_surface.m_num_of_chunks * m_surface.m_num_of_chunks)) % m_surface.m_num_of_chunks);

        m_res = m_surface.m_res;
        m_res2 = m_res * m_res;
        m_res3 = m_res2 * m_res;

        m_res_p = m_surface.m_res + 1;
        m_res2_p = m_res_p * m_res_p;
        m_res3_p = m_res2_p * m_res_p;

        m_full_res_p = m_surface.m_num_of_chunks * m_surface.m_res + 1;
        m_full_res2_p = m_full_res_p * m_full_res_p;

        m_offset = m_position.x * m_res + m_position.y * m_res * m_full_res_p + m_position.z * m_res * m_full_res2_p;

        m_maxVerts = m_res3 * 5 * 3;
        if(m_maxVerts > 65535) throw new System.ArgumentException("Maximum number of vertices is 65535 (" + m_maxVerts.ToString() + ")");

        m_cubeEdgeFlagsBuffer = new NativeArray<int>(256, Allocator.Persistent);
        for(int i = 0; i < 256; i++) m_cubeEdgeFlagsBuffer[i] = MarchingCubesTables.CubeEdgeFlags[i];

        m_triangleConnectionTableBuffer = new NativeArray<int>(256 * 16, Allocator.Persistent);
        for(int i = 0; i < 256; i++) for(int j = 0; j < 16; j++) m_triangleConnectionTableBuffer[i * 16 + j] = MarchingCubesTables.TriangleConnectionTable[i, j];
    }

    public void Refresh()
    {
        m_verticesBuffer = new NativeArray<Vertex>(m_maxVerts, Allocator.TempJob);
        m_normalTexture = new NativeArray<float3>(m_res3, Allocator.TempJob);
        m_surfaceValues = new NativeArray<float>(m_res3_p, Allocator.TempJob);
        
        for(int z = 0; z < m_res_p; z++)
        {
            for(int y = 0; y < m_res_p; y++)
            {
                for(int x = 0; x < m_res_p; x++)
                {
                    m_surfaceValues[x + y * m_res_p + z * m_res2_p] = m_surface.m_surfaceValues[m_offset + x + y * m_full_res_p + z * m_full_res2_p];
                }
            }
        }
        
        ResetVertBufferJob resetVertBufferJob = new ResetVertBufferJob()
        {
            _Vertices = m_verticesBuffer
        };

        CalculateNormalTextureJob calculateNormalTextureJob = new CalculateNormalTextureJob()
        {
            _Res = m_res,
            _DensityMap = m_surfaceValues,
            _NormalTexture = m_normalTexture
        };

        TriangulateJob triangulateJob = new TriangulateJob()
        {
            _TriangleConnectionTable = m_triangleConnectionTableBuffer,
            _CubeEdgeFlags = m_cubeEdgeFlagsBuffer,
            _Res = m_res,
            _DensityMap = m_surfaceValues,
            _NormalTexture = m_normalTexture,
            _Vertices = m_verticesBuffer
        };

        JobHandle resetVertBufferJobHandle = resetVertBufferJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 8f));
        JobHandle calculateNormalTextureJobHandle = calculateNormalTextureJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 8f));
        JobHandle triangulateDependencies = JobHandle.CombineDependencies(resetVertBufferJobHandle, calculateNormalTextureJobHandle);
        m_triangulateJobHandle = triangulateJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 8f), triangulateDependencies);

        m_refreshed = true;
    }

    private void CompleteTriangulation()
    {
        m_triangulateJobHandle.Complete();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        for(int i = 0, ti = 0; i < m_maxVerts; i++)
        {
            if(m_verticesBuffer[i].position.w != -1f)
            {
                vertices.Add(new Vector3(m_verticesBuffer[i].position.x, m_verticesBuffer[i].position.y, m_verticesBuffer[i].position.z));
                triangles.Add(ti++);
                normals.Add(new Vector3(m_verticesBuffer[i].normal.x, m_verticesBuffer[i].normal.y, m_verticesBuffer[i].normal.z));
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = name + "_mesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);

        m_meshFilter.mesh.Clear();
        m_meshFilter.mesh = mesh;
        m_verticesBuffer.Dispose();
        m_normalTexture.Dispose();
        m_surfaceValues.Dispose();

        m_refreshed = false;
    }
}

