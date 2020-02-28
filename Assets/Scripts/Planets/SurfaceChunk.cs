﻿using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Worlds.ProceduralTerrain.MarchingCubes;

public class SurfaceChunk : MonoBehaviour
{
    //debug
    public bool m_Refresh = false;
    public bool m_Draw_normals = false;

    //chunk data
    private int m_id;
    private int m_offset;
    private Vector3Int m_position;

    //player
    public float m_player_distance;

    //LOD groups
    public int m_current_lod = 2;
    public float m_LOD0_dist = 200f; //0 - 200m
    public float m_LOD1_dist = 400f; //m_LOD0_dist - 400m
    public float m_LOD2_dist = 600f;

    //surface data
    /*public int m_res { get; private set; }
    private int m_res2;
    private int m_res3;
    private int m_res_p;
    private int m_res2_p;
    private int m_res3_p;
    private int m_full_res_p;
    private int m_full_res2_p;*/

    private int m_mesh_res;
    //private int m_mesh_res2;
    private int m_mesh_res3;

    private int m_normal_res;
    private int m_normal_res2;
    private int m_normal_res3;

    private int m_surface_res;
    private int m_surface_res2;
    private int m_surface_res3;

    private int m_full_res;
    private int m_full_res2;

    //refs
    private Surface m_surface;
    private MeshFilter m_meshFilter;

    //MC daata
    private NativeArray<int> m_cubeEdgeFlagsBuffer;
    private NativeArray<int> m_triangleConnectionTableBuffer;
    private NativeArray<Vertex> m_verticesBuffer;
    private NativeArray<float3> m_normalTexture;
    private NativeArray<float> m_surfaceValues;
    private int m_maxVerts;

    //job
    private JobHandle m_triangulateJobHandle;
    private bool m_refreshed = false;

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
                    Debug.DrawLine(transform.TransformPoint(m_meshFilter.mesh.vertices[i]), transform.TransformPoint(m_meshFilter.mesh.vertices[i] + m_meshFilter.mesh.normals[i]), Color.green, 60f);
                }
                m_Refresh = false;
            }
        }
    }

    private void OnDestroy()
    {
        m_triangulateJobHandle.Complete();
        if(m_verticesBuffer != null && m_verticesBuffer.Length != 0) m_verticesBuffer.Dispose();
        if(m_normalTexture != null && m_normalTexture.Length != 0) m_normalTexture.Dispose();
        if(m_surfaceValues != null && m_surfaceValues.Length != 0) m_surfaceValues.Dispose();
        if(m_cubeEdgeFlagsBuffer != null && m_cubeEdgeFlagsBuffer.Length != 0) m_cubeEdgeFlagsBuffer.Dispose();
        if(m_triangleConnectionTableBuffer != null && m_triangleConnectionTableBuffer.Length != 0) m_triangleConnectionTableBuffer.Dispose();
    }

    public void Initalize(int index)
    {
        m_id = index;

        m_surface = GetComponentInParent<Surface>();
        m_position = new Vector3Int(m_id % m_surface.m_num_of_chunks, (m_id / m_surface.m_num_of_chunks) % m_surface.m_num_of_chunks, (m_id / (m_surface.m_num_of_chunks * m_surface.m_num_of_chunks)) % m_surface.m_num_of_chunks);
        
        SetResolutions();
        m_full_res = m_surface.m_num_of_chunks * m_surface.m_chunk_res + 2;
        m_full_res2 = m_full_res * m_full_res;
        m_offset = m_position.x * m_surface.m_chunk_res + m_position.y * m_surface.m_chunk_res * m_full_res + m_position.z * m_surface.m_chunk_res * m_full_res2;

        m_cubeEdgeFlagsBuffer = new NativeArray<int>(256, Allocator.Persistent);
        for(int i = 0; i < 256; i++) m_cubeEdgeFlagsBuffer[i] = MarchingCubesTables.CubeEdgeFlags[i];

        m_triangleConnectionTableBuffer = new NativeArray<int>(256 * 16, Allocator.Persistent);
        for(int i = 0; i < 256; i++) for(int j = 0; j < 16; j++) m_triangleConnectionTableBuffer[i * 16 + j] = MarchingCubesTables.TriangleConnectionTable[i, j];
    }

    public void Refresh()
    {
        SetResolutions();
        m_verticesBuffer = new NativeArray<Vertex>(m_maxVerts, Allocator.TempJob);
        m_normalTexture = new NativeArray<float3>(m_normal_res3, Allocator.TempJob);
        m_surfaceValues = new NativeArray<float>(m_surface_res3, Allocator.TempJob);

        //Przepisywanie surfaceValue
        for(int z = 0; z < m_surface_res; z++) 
        {
            for(int y = 0; y < m_surface_res; y++)
            {
                for(int x = 0; x < m_surface_res; x++)
                {
                    m_surfaceValues[x + y * m_surface_res + z * m_surface_res2] = m_surface.m_surfaceValues[m_offset +  (x * m_current_lod) + 
                                                                                                                        (y * m_current_lod) * m_full_res + 
                                                                                                                        (z * m_current_lod) * m_full_res2];
                }
            }
        }
        
        ResetVertBufferJob resetVertBufferJob = new ResetVertBufferJob()
        {
            _Vertices = m_verticesBuffer
        };

        CalculateNormalTextureJob calculateNormalTextureJob = new CalculateNormalTextureJob()
        {
            _SurfaceRes = m_surface_res,
            _NormalRes = m_normal_res,
            _DensityMap = m_surfaceValues,
            _NormalTexture = m_normalTexture
        };

        TriangulateJob triangulateJob = new TriangulateJob()
        {
            _TriangleConnectionTable = m_triangleConnectionTableBuffer,
            _CubeEdgeFlags = m_cubeEdgeFlagsBuffer,
            _MeshRes = m_mesh_res,
            _SurfaceRes = m_surface_res,
            _NormalRes = m_normal_res,
            _DensityMap = m_surfaceValues,
            _NormalTexture = m_normalTexture,
            _Vertices = m_verticesBuffer,
            _Scale = m_current_lod
        };

        //JobHandle resetVertBufferJobHandle = resetVertBufferJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 8f));
        //JobHandle calculateNormalTextureJobHandle = calculateNormalTextureJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 8f));
        //JobHandle triangulateDependencies = JobHandle.CombineDependencies(resetVertBufferJobHandle, calculateNormalTextureJobHandle);
        resetVertBufferJob.Run(m_mesh_res3);
        calculateNormalTextureJob.Run(m_normal_res3);
        triangulateJob.Run(m_mesh_res3);
        //m_triangulateJobHandle = triangulateJob.Schedule(m_res3, Mathf.CeilToInt(m_res3 / 8f), triangulateDependencies);

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

    private void SetResolutions()
    {
        m_mesh_res = m_surface.m_chunk_res / m_current_lod;
        m_mesh_res3 = m_mesh_res * m_mesh_res * m_mesh_res;

        m_normal_res = m_mesh_res + 1;
        m_normal_res2 = m_normal_res * m_normal_res;
        m_normal_res3 = m_normal_res2 * m_normal_res;

        m_surface_res = m_normal_res + 1;
        m_surface_res2 = m_surface_res * m_surface_res;
        m_surface_res3 = m_surface_res2 * m_surface_res;

        m_maxVerts = m_mesh_res3 * 5 * 3;
        /*m_res = m_surface.m_res / m_current_lod;
        m_res2 = m_res * m_res;
        m_res3 = m_res2 * m_res;

        m_res_p = m_res + 1;
        m_res2_p = m_res_p * m_res_p;
        m_res3_p = m_res2_p * m_res_p;

        m_maxVerts = m_res3 * 5 * 3;*/
    }
}

