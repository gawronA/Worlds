using System.Collections.Generic;
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
    public int m_lod_mesh_divider = 1;
    public int m_current_lod
    {
        get { return (int)Mathf.Log(m_lod_mesh_divider, 2); }
        set
        {
            m_lod_mesh_divider = (int)Mathf.Pow(2, value);
            SetResolutions();
            Refresh();
        }
    }
    public float[] m_lod_dist = { 200f, 400f, 600f };

    //surface data
    private int m_mesh_res;
    private int m_mesh_res3;

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
    private NativeQueue<Triangle> m_trianglesBuffer;
    private NativeArray<float> m_surfaceValues;

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
        for(int i = 0; i < m_lod_dist.Length; i++)
        {
            if(m_player_distance <= m_lod_dist[i])
            {
                if(m_current_lod != i) m_current_lod = i;
                break;
            }
        }

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
        if(m_trianglesBuffer.IsCreated) m_trianglesBuffer.Dispose();
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

        m_trianglesBuffer = new NativeQueue<Triangle>(Allocator.TempJob);
        m_surfaceValues = new NativeArray<float>(m_surface_res3, Allocator.TempJob);

        //Przepisywanie surfaceValue
        for(int z = 0; z < m_surface_res; z++) 
        {
            for(int y = 0; y < m_surface_res; y++)
            {
                for(int x = 0; x < m_surface_res; x++)
                {
                    m_surfaceValues[x + y * m_surface_res + z * m_surface_res2] = m_surface.m_surfaceValues[m_offset +  (x * m_lod_mesh_divider) + 
                                                                                                                        (y * m_lod_mesh_divider) * m_full_res + 
                                                                                                                        (z * m_lod_mesh_divider) * m_full_res2];
                }
            }
        }
        
        TriangulateJob triangulateJob = new TriangulateJob()
        {
            _TriangleConnectionTable = m_triangleConnectionTableBuffer,
            _CubeEdgeFlags = m_cubeEdgeFlagsBuffer,
            _MeshRes = m_mesh_res,
            _SurfaceRes = m_surface_res,
            _DensityMap = m_surfaceValues,
            _Triangles = m_trianglesBuffer.AsParallelWriter(),
            _Scale = m_lod_mesh_divider
        };

        m_triangulateJobHandle = triangulateJob.Schedule(m_mesh_res3, m_mesh_res / 2);
        m_refreshed = true;
    }

    private void CompleteTriangulation()
    {
        
        m_triangulateJobHandle.Complete();
        
        
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int triangles_count = m_trianglesBuffer.Count;
        for(int i = 0, ti = 0; i < triangles_count; i++)
        {
            Triangle triangle = m_trianglesBuffer.Dequeue();
            vertices.Add(new Vector3(triangle.v1.position.x, triangle.v1.position.y, triangle.v1.position.z));
            triangles.Add(ti++);
            normals.Add(new Vector3(triangle.v1.normal.x, triangle.v1.normal.y, triangle.v1.normal.z));
            
            vertices.Add(new Vector3(triangle.v2.position.x, triangle.v2.position.y, triangle.v2.position.z));
            triangles.Add(ti++);
            normals.Add(new Vector3(triangle.v2.normal.x, triangle.v2.normal.y, triangle.v2.normal.z));
            
            vertices.Add(new Vector3(triangle.v3.position.x, triangle.v3.position.y, triangle.v3.position.z));
            triangles.Add(ti++);
            normals.Add(new Vector3(triangle.v3.normal.x, triangle.v3.normal.y, triangle.v3.normal.z));
        }

        Mesh mesh = new Mesh();
        mesh.name = name + "_mesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);

        m_meshFilter.mesh.Clear();
        m_meshFilter.mesh = mesh;
        m_trianglesBuffer.Dispose();
        m_surfaceValues.Dispose();

        m_refreshed = false;
    }

    private void SetResolutions()
    {
        m_mesh_res = m_surface.m_chunk_res / m_lod_mesh_divider;
        m_mesh_res3 = m_mesh_res * m_mesh_res * m_mesh_res;

        m_surface_res = m_mesh_res + 1;
        m_surface_res2 = m_surface_res * m_surface_res;
        m_surface_res3 = m_surface_res2 * m_surface_res;
    }
}

