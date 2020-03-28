using System.Collections.Generic;
using UnityEngine;
using Worlds.ProceduralTerrain.Generator.Engine;
using Worlds.ProceduralTerrain;

public class Surface : MonoBehaviour
{
    public int m_num_of_chunks = 1;
    public int m_chunk_res = 16;
    public float m_radius;
    [Range(0f, 1f)] public float m_fill = 0.5f;

    public int m_mesh_res { get; private set; }
    public int m_surface_res { get; private set; }
    public SurfaceLayer m_surface { get; private set; }

    private List<GameObject> m_chunks;

    public GameObject surfaceChunkPrefab;
    public GameObject surfaceMapTexturePrefab;
    void Start()
    {
        m_mesh_res = m_num_of_chunks * m_chunk_res;
        m_surface_res = m_mesh_res + 1;
        m_surface = new SurfaceLayer(m_surface_res);
        //SurfaceLayer sphere = SurfaceBrush.Sphere(Vector3Int.zero, m_radius, m_fill);
        //m_surface = SurfaceLayer.Merge(m_surface, sphere, 2f, SurfaceLayer.MergeMethod.Overlay, SurfaceLayer.MergeSize.Cut);
        SurfaceLayer tetra = SurfaceBrush.Tetrahedron(new Vector3Int(0, 0, 0), new Vector3Int(5, 0, 0), new Vector3Int(0, 0, 5), new Vector3Int(2, 5, 2), m_fill);
        m_surface = SurfaceLayer.Merge(m_surface, tetra, 2f, SurfaceLayer.MergeMethod.Overlay, SurfaceLayer.MergeSize.Cut);

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

    private void Update()
    {
        //SurfaceLayer sphere = SurfaceBrush.Sphere(Vector3Int.zero, m_radius, m_fill);
        //m_surface = SurfaceLayer.Merge(m_surface, sphere, 2f, SurfaceLayer.MergeMethod.Overlay, SurfaceLayer.MergeSize.Cut);
        SurfaceLayer tetra = SurfaceBrush.Tetrahedron(new Vector3Int(0, 0, 0), new Vector3Int(5, 0, 0), new Vector3Int(0, 0, 5), new Vector3Int(2, 5, 2), m_fill);
        m_surface = SurfaceLayer.Merge(m_surface, tetra, 2f, SurfaceLayer.MergeMethod.Overlay, SurfaceLayer.MergeSize.Cut);
    }
}
