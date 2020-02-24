using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour
{
    public int m_res = 16;
    public float[] m_surfaceValues { get; private set; }


    public GameObject surfaceChunkPrefab;
    void Start()
    {
        int res = m_res;
        int res2 = res * res;

        int res_p = m_res + 1;
        int res2_p = res_p * res_p;

        m_surfaceValues = new float[res2_p * res_p];
        //int x = 0, y = 0, z = 0;
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

        GameObject obj = Instantiate(surfaceChunkPrefab);
        SurfaceChunk surfaceChunk = obj.GetComponent<SurfaceChunk>();
        surfaceChunk.Initalize(0, m_res, this);

        surfaceChunk.Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
