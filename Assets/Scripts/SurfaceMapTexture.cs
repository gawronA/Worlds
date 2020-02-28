using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMapTexture : MonoBehaviour
{
    public enum Display { surfaceMap, normalMap };

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

    // Update is called once per frame
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
                    color = new Color(0f, m_surface.m_surfaceValues[x + y * res + z * res2], 1 - m_surface.m_surfaceValues[x + y * res + z * res2]);
                    if(x == res - 1 || y == res - 1 || z == res - 1) color += Color.red;
                    else if(x == res - 2 || y == res - 2 || z == res - 2) color += Color.red * 0.75f;

                    texture.SetPixel(x, y, color);
                }
            }
        }
        else if(display == Display.normalMap)
        {
            int res = m_surface.m_surface_res;
            int res2 = res * res;
            Color color;
            texture.filterMode = FilterMode.Bilinear;
            for(int y = 0; y < m_surface.m_surface_res; y++)
            {
                for(int x = 0; x < m_surface.m_surface_res; x++)
                {
                    color = new Color();
                    if(x < res - 1 && y < res - 1 && z < res - 1)
                    {
                        Vector3 normal = Normal(x, y, z);
                        color += new Color((normal.x + 1f) / 2, (normal.y + 1f) / 2, (normal.z + 1f) / 2);
                    }
                    else if(x == res - 1 || y == res - 1 || z == res - 1) color += Color.red;

                    else if(x == res - 2 || y == res - 2 || z == res - 2) color += Color.red * 0.75f;

                    texture.SetPixel(x, y, color);
                }
            }
        }
        texture.Apply();
    }

    Vector3 Normal(int x, int y, int z)
    {
        int res = m_surface.m_surface_res;
        int res2 = res * res;

        float value = m_surface.m_surfaceValues[x + y * res + z * res2];
        float dx = value - m_surface.m_surfaceValues[(x + 1) + y * res + z * res2];
        float dy = value - m_surface.m_surfaceValues[x + (y + 1) * res + z * res2];
        float dz = value - m_surface.m_surfaceValues[x + y * res + (z + 1) * res2];
        Vector3 normal = (dx != 0f || dy != 0f || dz != 0f) ? new Vector3(dx, dy, dz).normalized : Vector3.zero;
        return normal;
    }
}
