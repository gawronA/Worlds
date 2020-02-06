using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Worlds.ProceduralTerrain.MarchingCubes;

public class SurfaceChunk : MonoBehaviour
{
    private int m_id;
    private Surface m_surface;

    public int m_res { get; private set; }
    private int m_res2;
    private int m_res3;
    private int m_res_p;
    private int m_res2_p;
    private int m_res3_p;
    private int m_maxVerts;

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
        
    }

    private void OnDestroy()
    {
        if(m_cubeEdgeFlagsBuffer != null) m_cubeEdgeFlagsBuffer.Dispose();
        if(m_triangleConnectionTableBuffer != null) m_triangleConnectionTableBuffer.Dispose();
    }

    public void Initalize(int index, int resolution, Surface surface)
    {
        m_id = index;

        m_res = resolution;
        m_res2 = m_res * m_res;
        m_res3 = m_res2 * m_res;

        m_res_p = resolution + 1;
        m_res2_p = m_res_p * m_res_p;
        m_res3_p = m_res2_p * m_res_p;

        m_maxVerts = m_res3 * 5 * 3;

        m_surface = surface;

        m_cubeEdgeFlagsBuffer = new NativeArray<int>(256, Allocator.Persistent);
        for(int i = 0; i < 256; i++) m_cubeEdgeFlagsBuffer[i] = MarchingCubesTables.CubeEdgeFlags[i];

        m_triangleConnectionTableBuffer = new NativeArray<int>(256 * 16, Allocator.Persistent);
        for(int i = 0; i < 256; i++) for(int j = 0; j < 16; j++) m_triangleConnectionTableBuffer[i * 16 + j] = MarchingCubesTables.TriangleConnectionTable[i, j];

    }

    public void Refresh()
    {
        NativeArray<Vertex> verticesBuffer = new NativeArray<Vertex>(m_maxVerts, Allocator.TempJob);
        NativeArray<float> surfaceValues = new NativeArray<float>(m_res3_p, Allocator.TempJob);
        for(int i = 0; i < m_res3_p; i++) surfaceValues[i] = m_surface.m_surfaceValues[i];
        ResetVertBufferJob resetVertBufferJob = new ResetVertBufferJob()
        {
            _Vertices = verticesBuffer
        };
        
        //Normals

        TriangulateJob triangulateJob = new TriangulateJob()
        {
            _TriangleConnectionTable = m_triangleConnectionTableBuffer,
            _CubeEdgeFlags = m_cubeEdgeFlagsBuffer,
            _Res = m_res,
            _DensityMap = surfaceValues,
            _Vertices = verticesBuffer
        };

        JobHandle resetVertBufferJobHandle = resetVertBufferJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 16f));
        JobHandle triangulateJobHandle = triangulateJob.Schedule(m_res3, m_res3, resetVertBufferJobHandle);//Mathf.CeilToInt(m_res3 / 16f), resetVertBufferJobHandle);

        triangulateJobHandle.Complete();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for(int i = 0, ti = 0; i < m_maxVerts; i++)
        {
            if(verticesBuffer[i].position.w != -1f)
            {
                vertices.Add(new Vector3(verticesBuffer[i].position.x, verticesBuffer[i].position.y, verticesBuffer[i].position.z));
                triangles.Add(ti++);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        m_meshFilter.mesh = mesh;
        verticesBuffer.Dispose();
        surfaceValues.Dispose();
    }

    
}

