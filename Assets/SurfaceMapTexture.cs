using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMapTexture : MonoBehaviour
{
    [Range(0, 16)] public int z = 0;

    private Surface parent_surface;
    private Texture2D texture;
    private int m_res;
    private float r_intensity = 0.15f;
    void Start()
    {
        parent_surface = GetComponentInParent<Surface>();
        texture = new Texture2D(parent_surface.m_res + 1, parent_surface.m_res + 1, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        GetComponent<MeshRenderer>().material.mainTexture = texture;
        m_res = parent_surface.m_res;

    }

    // Update is called once per frame
    void Update()
    {
        int res = m_res;
        int res2 = res * res;

        int res_p = m_res + 1;
        int res2_p = res_p * res_p;
        int x, y;
        Color color;
        for(y = 0; y < res; y++)
        {
            for(x = 0; x < res; x++)
            {
                if(z != res)
                {
                    color = new Color((parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    color = new Color((parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
                    texture.SetPixel(x, y, color + new Color(r_intensity, 0f, 0f));
                }
            }
            color = new Color((parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
            texture.SetPixel(x, y, color + new Color(r_intensity, 0f, 0f));
        }
        for(x = 0; x < res; x++)
        {
            color = new Color((parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
            texture.SetPixel(x, y, color + new Color(r_intensity, 0f, 0f));
        }
        color = new Color((parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, (parent_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
        texture.SetPixel(x, y, color + new Color(r_intensity, 0f, 0f));
        /*
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
        for(int i = 0; i < parent_surface.m_res + 1; i++)
        {
            for(int j = 0; j < parent_surface.m_res + 1; j++)
            {
                
                
            }
        }*/
        texture.Apply();
    }
}
