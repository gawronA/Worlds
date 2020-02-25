using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour
{
    public int m_num_of_chunks = 1;
    public int m_res = 16;
    public float[] m_surfaceValues { get; private set; }

    private List<GameObject> m_chunks;


    public GameObject surfaceChunkPrefab;
    void Start()
    {
        int res = m_num_of_chunks * m_res;
        int res2 = res * res;

        int res_p = m_num_of_chunks * m_res + 1;
        int res2_p = res_p * res_p;

        m_surfaceValues = new float[res2_p * res_p];
        m_chunks = new List<GameObject>();
        //int x = 0, y = 0, z = 0;
        {
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
        }

        for(int z = 0; z < m_num_of_chunks; z++)
        {
            for(int y = 0; y < m_num_of_chunks; y++)
            {
                for(int x = 0; x < m_num_of_chunks; x++)
                {
                    int index = x + y * m_num_of_chunks + z * m_num_of_chunks * m_num_of_chunks;
                    GameObject obj = Instantiate(surfaceChunkPrefab, transform.position + new Vector3(x * m_res, y * m_res, z * m_res), Quaternion.identity, transform);
                    obj.name = name + "_" + index.ToString();
                    m_chunks.Add(obj);
                    obj.GetComponent<SurfaceChunk>().Initalize(index);
                    obj.GetComponent<SurfaceChunk>().Refresh();
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
