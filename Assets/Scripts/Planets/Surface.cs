using System.Collections.Generic;
using UnityEngine;
using Worlds.ProceduralTerrain.Generator;
using Worlds.ProceduralTerrain;

public class Surface : MonoBehaviour
{
    public int m_num_of_chunks = 1;
    public int m_chunk_res = 16;
    public float m_radius;

    public int m_mesh_res { get; private set; }
    public int m_surface_res { get; private set; }
    public float[] m_surfaceValues { get; private set; }
    Stamp stamp;

    private List<GameObject> m_chunks;

    public GameObject surfaceChunkPrefab;
    public GameObject surfaceMapTexturePrefab;
    void Start()
    {
        m_mesh_res = m_num_of_chunks * m_chunk_res;
        m_surface_res = m_num_of_chunks * m_chunk_res + 2;
        m_surfaceValues = new float[m_surface_res * m_surface_res * m_surface_res];
        stamp = new Stamp(m_surfaceValues, m_surface_res);
        stamp.Clear();
        stamp.Sphere(Vector3Int.one * m_mesh_res / 2, m_radius, Stamp.Align.Center);
        //generator = new PlanetGenerator(m_surfaceValues, m_surface_res);
        //generator.Clear();
        //generator.Sphere(Vector3Int.one * m_mesh_res / 2, m_radius);
        //generator.Point(Vector3Int.one * m_mesh_res / 2);
        
        {/*
            int x, y, z;
            for(z = 0; z < res; z++)
            {
                for(y = 0; y < res; y++)
                {
                    for(x = 0; x < res; x++)
                    {
                        if(y > 3) m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
                        else m_surfaceValues[x + y * res_p + z * res2_p] = 1f;
                    }
                    m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
                }
                for(x = 0; x < res; x++)
                {
                    m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
                }
                m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
            }
            for(y = 0; y < res; y++)
            {
                for(x = 0; x < res; x++)
                {
                    m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
                }
                m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
            }
            for(x = 0; x < res; x++)
            {
                m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
            }
            m_surfaceValues[x + y * res_p + z * res2_p] = -1f;
       */ }

        m_chunks = new List<GameObject>();
        for(int z = 0; z < m_num_of_chunks; z++)
        {
            for(int y = 0; y < m_num_of_chunks; y++)
            {
                for(int x = 0; x < m_num_of_chunks; x++)
                {
                    int index = x + y * m_num_of_chunks + z * m_num_of_chunks * m_num_of_chunks;
                    GameObject chunk = Instantiate(surfaceChunkPrefab, transform.position + new Vector3(x * m_chunk_res, y * m_chunk_res, z * m_chunk_res), Quaternion.identity, transform);
                    chunk.name = name + "_" + index.ToString();
                    m_chunks.Add(chunk);
                    chunk.GetComponent<SurfaceChunk>().Initalize(index);
                    chunk.GetComponent<SurfaceChunk>().Refresh();
                }
            }
        }
        Instantiate(surfaceMapTexturePrefab, transform.position + new Vector3(-20.3f, 1.12f, 0f), Quaternion.Euler(90f, -180f, 0), transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
