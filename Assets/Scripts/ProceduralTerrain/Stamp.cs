using UnityEngine;

namespace Worlds
{
    namespace ProceduralTerrain
    {
        public class Stamp
        {
            public static class Align
            {
                public static Vector3Int Center(int box_length, int box_height, int box_depth)
                {
                    return new Vector3Int(box_length / 2, box_height / 2, box_depth / 2);
                }

                public static Vector3Int BottomCenter(int box_length, int box_height, int box_depth)
                {
                    return new Vector3Int(box_length / 2, 0, box_depth / 2);
                }
            }
            public delegate Vector3Int Alignment(int box_length, int box_height, int box_depth);
            float[] m_surfaceValues;

            int m_res;
            int m_res2;
            int m_res3;

            public Stamp(float[] surfaceValues, int resolution)
            {
                m_surfaceValues = surfaceValues;
                m_res = resolution;
                m_res2 = m_res * m_res;
                m_res3 = m_res2 * m_res;
            }

            public void Clear()
            {
                for(int i = 0; i < m_res3; i++)
                {
                    m_surfaceValues[i] = -1f;
                }
            }

            public void Point(Vector3Int center)
            {
                m_surfaceValues[center.x + center.y * m_res + center.z * m_res2] = 1f;
            }

            public void SquarePyramid(Vector3Int center, int base_length, int height, Alignment align)
            {
                center -= align(base_length, height, base_length);
                int offset = center.x + center.y * m_res + center.z * m_res2;
                for(int z = 0; z < base_length; z++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        for(int x = 0; x < base_length; x++)
                        {

                        }
                    }
                }
            }

            public void Sphere(Vector3Int center, float radius, Alignment align)
            {
                center -= align(2 * (int)radius, 2 * (int)radius, 2 * (int)radius);
                if(center.x >= m_res || center.y >= m_res || center.z >= m_res) return;

                int offset = center.x + center.y * m_res + center.z * m_res2;
                
                int z = center.z < 0 ? -center.z : 0;
                for(; z <= (radius * 2) && center.z + z < m_res; z++)
                {
                    int y = center.y < 0 ? -center.y : 0;
                    for(; y <= (radius * 2) && center.y + y < m_res; y++)
                    {
                        int x = center.x < 0 ? -center.x : 0;
                        for(; x <= (radius * 2) && center.x + x < m_res; x++)
                        {
                            if(Mathf.Pow(x - radius, 2) + Mathf.Pow(y - radius, 2) + Mathf.Pow(z - radius, 2) <= Mathf.Pow(radius, 2))
                            {
                                m_surfaceValues[offset + x + y * m_res + z * m_res2] = 1f;
                            }
                            else m_surfaceValues[offset + x + y * m_res + z * m_res2] = -1f;
                        }
                    }
                }
            }

            public void GradualSphere(Vector3Int center, float radius, float fill, Alignment align)
            {
                float dr = radius * (-2 * Mathf.Abs(fill - 0.5f) + 1);
                float rmin = radius - dr;
                float rmax = radius + dr;

                center -= align(4 * (int)radius, 4 * (int)radius, 4 * (int)radius);
                if(center.x >= m_res || center.y >= m_res || center.z >= m_res) return;

                int offset = center.x + center.y * m_res + center.z * m_res2;

                int z = center.z < 0 ? -center.z : 0;
                for(; z <= (radius * 4) && (center.z + z) < m_res; z++)
                {
                    int y = center.y < 0 ? -center.y : 0;
                    for(; y <= (radius * 4) && (center.y + y) < m_res; y++)
                    {
                        int x = center.x < 0 ? -center.x : 0;
                        for(; x <= (radius * 4) && (center.x + x) < m_res; x++)
                        {
                            float magnitude = Mathf.Sqrt(Mathf.Pow(x - 2 * radius, 2) + Mathf.Pow(y - 2 * radius, 2) + Mathf.Pow(z - 2 * radius, 2));
                            float value = Mathf.Lerp(1f, -1f, Mathf.Clamp01((magnitude - rmin) / (rmax - rmin + 0.001f)));
                            m_surfaceValues[offset + x + y * m_res + z * m_res2] = value;
                        }
                    }
                }
            }

            public void Cube(Vector3Int center, float length)
            {
                center -= Vector3Int.one * (int)(length / 2f);
                int offset = center.x + center.y * m_res + center.z * m_res2;
                for(int z = 0; z <= length; z++)
                {
                    for(int y = 0; y <= length; y++)
                    {
                        for(int x = 0; x <= length; x++)
                        {
                            if(x <= length && y <= length && z <= length) m_surfaceValues[offset + x + y * m_res + z * m_res2] = 1f;
                            else m_surfaceValues[offset + x + y * m_res + z * m_res2] = -1f;
                        }
                    }
                }
            }

            
        }
    }
}

