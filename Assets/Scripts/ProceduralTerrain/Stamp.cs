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

            public void Sphere(Vector3Int center, float radius, Alignment align)
            {
                center -= align(2 * (int)radius, 2 * (int)radius, 2 * (int)radius);
                int offset = center.x + center.y * m_res + center.z * m_res2;
                for(int z = 0; z <= (radius * 2); z++)
                {
                    for(int y = 0; y <= (radius * 2); y++)
                    {
                        for(int x = 0; x <= (radius * 2); x++)
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

            public void Point(Vector3Int center)
            {
                m_surfaceValues[center.x + center.y * m_res + center.z * m_res2] = 1f;
            }
        }
    }
}

