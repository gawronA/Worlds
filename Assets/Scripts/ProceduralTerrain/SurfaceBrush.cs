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

        public static SurfaceLayer Sphere(Vector3Int position, float radius)
        {
            int res = Mathf.CeilToInt(2f * radius);
            SurfaceLayer layer = new SurfaceLayer(res, position);

            for(int z = 0; z < res; z++)
            {
                for(int y = 0; y < res; y++)
                {
                    for(int x = 0; x < res; x++)
                    {
                        float value = Mathf.Sqrt(Mathf.Pow(x - (int)radius, 2) + Mathf.Pow(y - (int)radius, 2) + Mathf.Pow(z - (int)radius, 2)) <= radius ? 1f : -1f;
                        layer.Set(x, y, z, value, SurfaceLayer.MergeMethod.Overlay);
                    }
                }
            }
            return layer;
        }
                
        public static SurfaceLayer Tetrahedron(Vector3Int A, Vector3Int B, Vector3Int C, Vector3Int D)
        {
            Vector3Int position = new Vector3Int(   (int)Mathf.Min(new float[] { A.x, B.x, C.x, D.x }),
                                                    (int)Mathf.Min(new float[] { A.y, B.y, C.y, D.y }),
                                                    (int)Mathf.Min(new float[] { A.z, B.z, C.z, D.z }));

            int resx = Mathf.CeilToInt(Mathf.Max(new float[] { A.x, B.x, C.x, D.x }));
            int resy = Mathf.CeilToInt(Mathf.Max(new float[] { A.y, B.y, C.y, D.y }));
            int resz = Mathf.CeilToInt(Mathf.Max(new float[] { A.z, B.z, C.z, D.z }));
            SurfaceLayer layer = new SurfaceLayer(resx, resy, resz, position);

            PrimitiveTetrahedron tetrahedron = new PrimitiveTetrahedron(A, B, C, D);

            for(int z = 0; z < resz; z++)
            {
                for(int y = 0; y < resy; y++)
                {
                    for(int x = 0; x < resx; x++)
                    {
                        Vector3 point = new Vector3(x, y, z) + position;
                        float value = tetrahedron.IsPointInside(point) ? 1f : -1f;
                        layer.Set(x, y, z, value, SurfaceLayer.MergeMethod.Overlay);
                    }
                }
            }
            return layer;
        }
    }
}

