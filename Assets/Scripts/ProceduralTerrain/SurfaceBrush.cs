using UnityEngine;
using Worlds.ProceduralTerrain.Brush.Helpers;

namespace Worlds.ProceduralTerrain.Generator.Engine
{
    public static class SurfaceBrush
    {
        /*public static class Align
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
        public delegate Vector3Int Alignment(int box_length, int box_height, int box_depth);*/
            

        public static SurfaceLayer Point(Vector3Int position)
        {
            SurfaceLayer layer = new SurfaceLayer(1, position);
            layer.Set(0, 0, 0, 1f, SurfaceLayer.MergeMethod.Overlay);

            return layer;
        }

        public static SurfaceLayer Sphere(Vector3Int position, float radius, float fill)
        {
            float dr = radius * (1f - fill);
            float rmin = radius - dr;
            float rmax = radius + dr;

            int res = Mathf.CeilToInt(2f * rmax + 1);
            SurfaceLayer layer = new SurfaceLayer(res, position);

            for(int z = 0; z < res; z++)
            {
                for(int y = 0; y < res; y++)
                {
                    for(int x = 0; x < res; x++)
                    {
                        float magnitude = Mathf.Sqrt(Mathf.Pow(x - rmax, 2) + Mathf.Pow(y - rmax, 2) + Mathf.Pow(z - rmax, 2));
                        float value = Mathf.Lerp(1f, -1f, Mathf.Clamp01((magnitude - rmin) / (rmax - rmin + 0.001f)));
                        layer.Set(x, y, z, value, SurfaceLayer.MergeMethod.Overlay);
                    }
                }
            }

            return layer;
        }
        /*
        public void SquarePyramid(Vector3Int position, int base_length, int height, float fill, Alignment align)
        {
            float dx = base_length * (-2 * Mathf.Abs(fill - 0.5f) + 1);
            float dh = height *(-2 * Mathf.Abs(fill - 0.5f) + 1);

            Vector3 P1p = new Vector3(0f, 0f, 0f);
            Vector3 P2p = new Vector3(4f * dx + base_length, 0f, 0f);
            Vector3 P3p = new Vector3(4f * dx + base_length, 0f, 4f * dx + base_length);
            Vector3 P4p = new Vector3(0f, 0f, 4f * dx + base_length);
            Vector3 P5p = new Vector3((4f * dx + base_length) / 2f, 4f * dh * height, (4f * dx + base_length) / 2f);

            Vector3 P1 = new Vector3(2f * dx, 2f * dh, 2f * dx);
            Vector3 P2 = new Vector3(2f * dx + base_length, 2f * dh, 2f * dx);
            Vector3 P3 = new Vector3(2f * dx + base_length, 2f * dh, 2f * dx + base_length);
            Vector3 P4 = new Vector3(2f * dx, 2f * dh, 2f * dx + base_length);
            Vector3 P5 = new Vector3((4f * dx + base_length) / 2f, 2f * dh + height, (4f * dx + base_length) / 2f);

            position -= align(base_length, height, base_length);
            if(position.x >= m_layer.m_res || position.y >= m_layer.m_res || position.z >= m_layer.m_res) return;

            int offset = position.x + position.y * m_layer.m_res + position.z * m_layer.m_res2;

            int z = position.z < 0 ? -position.z : 0;
            for(z = 0; z < base_length && (position.z + z) < m_layer.m_res; z++)
            {
                int y = position.y < 0 ? -position.y : 0;
                for(y = 0; y < height && (position.y + y) < m_layer.m_res; y++)
                {
                    int x = position.x < 0 ? -position.x : 0;
                    for(x = 0; x < base_length && (position.x + x) < m_layer.m_res; x++)
                    {

                    }
                }
            }
        }
            
        public void Tetrahedron(Vector3 A, Vector3 B, Vector3 C, Vector3 D, float fill, Alignment align)
        {
            A = (1f / fill) * A;
            B = (1f / fill) * B;
            C = (1f / fill) * C;
            D = (1f / fill) * D;
            Vector3 O = new Vector3((A.x + B.x + C.x + D.x) / 4f, (A.y + B.y + C.y + D.y) / 4f, (A.z + B.z + C.z + D.z) / 4f);
            PrimitiveTetrahedron T1 = new PrimitiveTetrahedron(A, B, C, O);
            PrimitiveTetrahedron T2 = new PrimitiveTetrahedron(A, C, D, O);
            PrimitiveTetrahedron T3 = new PrimitiveTetrahedron(A, B, D, O);
            PrimitiveTetrahedron T4 = new PrimitiveTetrahedron(B, C, D, O);

                
        }*/
    }
}

