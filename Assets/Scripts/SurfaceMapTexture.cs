using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMapTexture : MonoBehaviour
{
    public enum Display { surfaceMap};

    public Display display = Display.surfaceMap;
    public int z = 0;

    private Surface m_surface;
    private MeshRenderer meshRenderer;
    private Texture2D texture;
    
    void Start()
    {
        m_surface = GetComponentInParent<Surface>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        texture = new Texture2D(m_surface.m_surface_res, m_surface.m_surface_res, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        
        meshRenderer.material.mainTexture = texture;
    }

    void Update()
    {
        if(display == Display.surfaceMap)
        {
            texture.filterMode = FilterMode.Point;
            int res = m_surface.m_surface_res;
            int res2 = res * res;
            Color color;
            for(int y = 0; y < m_surface.m_surface_res; y++)
            {
                for(int x = 0; x < m_surface.m_surface_res; x++)
                {
                    color = new Color(0f, m_surface.m_surface.values[x + y * res + z * res2], 1 - m_surface.m_surface.values[x + y * res + z * res2]);
                    if(x == res - 1 || y == res - 1 || z == res - 1) color += Color.red;
                    texture.SetPixel(x, y, color);
                }
            }
        }
        texture.Apply();
    }
}
