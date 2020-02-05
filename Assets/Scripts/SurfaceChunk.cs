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

    public int m_res { get; protected set; }
    private int m_res2;
    private int m_res3;
    private int m_maxVerts;

    float[] m_surfaceValues;

    //MC buffers
    NativeArray<int> m_cubeEdgeFlagsBuffer;
    NativeArray<int> m_triangleConnectionTableBuffer;

    void Update()
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
        m_res = resolution + 1;
        m_res2 = m_res * m_res;
        m_res3 = m_res2 * m_res;
        m_maxVerts = m_res3 * 5 * 3;

        m_surface = surface;
        m_surfaceValues = new float[m_res3];

        m_cubeEdgeFlagsBuffer = new NativeArray<int>(256, Allocator.Persistent);
        for(int i = 0; i < 256; i++) m_cubeEdgeFlagsBuffer[i] = MarchingCubesTables.CubeEdgeFlags[i];

        m_triangleConnectionTableBuffer = new NativeArray<int>(256 * 16, Allocator.Persistent);
        for(int i = 0; i < 256; i++) for(int j = 0; j < 16; j++) m_triangleConnectionTableBuffer[i] = MarchingCubesTables.TriangleConnectionTable[i, j];

    }

    public void Refresh()
    {
        NativeArray<Vertex> verticesBuffer = new NativeArray<Vertex>(m_maxVerts, Allocator.TempJob);

        ResetVertBufferJob resetVertBufferJob = new ResetVertBufferJob()
        {
            _Vertices = verticesBuffer
        };

        JobHandle jobHandle = resetVertBufferJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 16f));
        
        verticesBuffer.Dispose();
    }

    
}

