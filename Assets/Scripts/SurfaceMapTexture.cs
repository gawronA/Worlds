using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMapTexture : MonoBehaviour
{
    public enum Display { surfaceMap };

    public Display display;
    public int z = 0;

    //public bool drawChunks = false;

    private Surface p_surface;
    private MeshRenderer meshRenderer;
    private Texture2D texture;
    
    private int m_res;
    private int m_chunk_res;



    private void OnEnable()
    {
        p_surface = GetComponentInParent<Surface>();
        meshRenderer = GetComponent<MeshRenderer>();
    }
    void Start()
    {
        if(display == Display.surfaceMap)
        {
            m_res = p_surface.m_num_of_chunks * p_surface.m_res;
            m_chunk_res = p_surface.m_res;
            texture = new Texture2D(m_res + 1, m_res + 1, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }
        meshRenderer.material.mainTexture = texture;
    }

    // Update is called once per frame
    void Update()
    {
        if(display == Display.surfaceMap)
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
                        color = new Color(0f, (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, 1f - (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
                        texture.SetPixel(x, y, color);
                    }
                    else
                    {
                        color = new Color(0f, (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, 1f - (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
                        texture.SetPixel(x, y, color + Color.red);
                    }
                }
                color = new Color(0f, (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, 1f - (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
                texture.SetPixel(x, y, color + Color.red);
            }
            for(x = 0; x < res; x++)
            {
                color = new Color(0f, (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, 1f - (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
                texture.SetPixel(x, y, color + Color.red);
            }
            color = new Color(0f, (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f, 1f - (p_surface.m_surfaceValues[x + y * res_p + z * res2_p] + 1f) / 2f);
            texture.SetPixel(x, y, color + Color.red);
        }
        
        
        texture.Apply();
    }
}
