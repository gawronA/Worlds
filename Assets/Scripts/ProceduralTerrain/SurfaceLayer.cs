using UnityEngine;

namespace Worlds.ProceduralTerrain.Generator.Engine
{
    public class SurfaceLayer
    {
        public enum MergeSize { Extend, Cut }
        public enum MergeMethod { Add, Subtract, Overlay }

        public string name;
        public Vector3Int position;
        public int res_x { get; private set; }
        public int res_y { get; private set; }
        public int res_z { get; private set; }
        public int size { get; private set; }

        public float[] values;

        private int i_y; //indexers in y and z direction
        private int i_z;

        public float this[int i]
        {
            get { return values[i]; }
            set { values[i] = value; }
        }

        public SurfaceLayer(int res)
        {
            res_x = res_y = res_z = res;
            size = res_x * res_y * res_z;

            i_y = res_x;
            i_z = res_y * res_y;

            position = Vector3Int.zero;

            values = new float[size];
            Clear();
        }

        public SurfaceLayer(int x, int y, int z)
        {
            res_x = x;
            res_y = y;
            res_z = z;
            size = res_x * res_y * res_z;

            i_y = res_x;
            i_z = res_y * res_y;

            position = Vector3Int.zero;

            values = new float[size];
            Clear();
        }

        public SurfaceLayer(int res, Vector3Int position)
        {
            res_x = res_y = res_z = res;
            size = res_x * res_y * res_z;

            i_y = res_x;
            i_z = res_y * res_y;

            this.position = position;

            values = new float[size];
            Clear();
        }

        public SurfaceLayer(int x, int y, int z, Vector3Int position)
        {
            res_x = x;
            res_y = y;
            res_z = z;
            size = res_x * res_y * res_z;

            i_y = res_x;
            i_z = res_y * res_y;

            this.position = position;

            values = new float[size];
            Clear();
        }

        public float Get(int x, int y, int z)
        {
            return values[x + y * i_y + z * i_z];
        }

        public void Set(int x, int y, int z, float value, MergeMethod mergeMethod)
        {
            switch(mergeMethod)
            {
                case MergeMethod.Add:
                    values[x + y * i_y + z * i_z] = Mathf.Clamp(values[x + y * i_y + z * i_z] + value, -1f, 1f);
                    break;

                case MergeMethod.Subtract:
                    values[x + y * i_y + z * i_z] = Mathf.Clamp(values[x + y * i_y + z * i_z] - value, -1f, 1f);
                    break;

                case MergeMethod.Overlay:
                    values[x + y * i_y + z * i_z] = Mathf.Clamp(value, -1f, 1f);
                    break;
            }
        }

        public void Clear()
        {
            for(int i = 0; i < size; i++)
            {
                values[i] = -1f;
            }
        }

        public static SurfaceLayer Merge(SurfaceLayer s1, SurfaceLayer s2, float multiplier, MergeMethod mergeMethod, MergeSize mergeSize)
        {
            switch(mergeSize)
            {
                case MergeSize.Cut:
                {
                    Vector3Int position = new Vector3Int(s1.position.x, s1.position.y, s1.position.z);
                    SurfaceLayer mergedLayer = new SurfaceLayer(s1.res_x, s1.res_y, s1.res_z, position);

                    for(int z = 0; z < mergedLayer.res_x; z++)
                    {
                        for(int y = 0; y < mergedLayer.res_y; y++)
                        {
                            for(int x = 0; x < mergedLayer.res_x; x++)
                            {
                                //copy
                                mergedLayer.Set(x, y, z, s1.Get(x, y, z), MergeMethod.Overlay);

                                //merge
                                int xi, yi, zi;
                                xi = (mergedLayer.position.x + x) - s2.position.x;
                                yi = (mergedLayer.position.y + y) - s2.position.y;
                                zi = (mergedLayer.position.z + z) - s2.position.z;
                                if((xi >= 0 && xi < s2.res_x) && (yi >= 0 && yi < s2.res_y) && (zi >= 0 && zi < s2.res_z))
                                    mergedLayer.Set(x, y, z, s2.Get(xi, yi, zi) * multiplier, mergeMethod);
                            }
                        }
                    }
                    return mergedLayer;
                }

                case MergeSize.Extend:
                {
                    Vector3Int position = new Vector3Int(s1.position.x > s2.position.x ? s2.position.x : s1.position.x,
                                                           s1.position.y > s2.position.y ? s2.position.y : s1.position.y,
                                                           s1.position.z > s2.position.z ? s2.position.z : s1.position.z);

                    int res_x = s1.position.x + s1.res_x > s2.position.x + s2.res_x ? s1.position.x - position.x + s1.res_x : s2.position.x - position.x + s2.res_x;
                    int res_y = s1.position.y + s1.res_y > s2.position.y + s2.res_y ? s1.position.y - position.y + s1.res_y : s2.position.y - position.y + s2.res_y;
                    int res_z = s1.position.z + s1.res_z > s2.position.z + s2.res_z ? s1.position.z - position.z + s1.res_z : s2.position.z - position.z + s2.res_z;

                    SurfaceLayer mergedLayer = new SurfaceLayer(res_x, res_y, res_z, position);

                    for(int z = 0; z < mergedLayer.res_x; z++)
                    {
                        for(int y = 0; y < mergedLayer.res_y; y++)
                        {
                            for(int x = 0; x < mergedLayer.res_x; x++)
                            {
                                //Copy
                                int xi, yi, zi;
                                xi = (position.x + x) - s1.position.x;
                                yi = (position.y + y) - s1.position.y;
                                zi = (position.z + z) - s1.position.z;
                                if((xi >= 0 && xi < s1.res_x) && (yi >= 0 && yi < s1.res_y) && (zi >= 0 && zi < s1.res_z))
                                    mergedLayer.Set(x, y, z, s1.Get(xi, yi, zi), MergeMethod.Overlay);
                                else
                                    mergedLayer.Set(x, y, z, -1f, MergeMethod.Overlay);

                                //merge
                                xi = (position.x + x) - s2.position.x;
                                yi = (position.y + y) - s2.position.y;
                                zi = (position.z + z) - s2.position.z;
                                if((xi >= 0 && xi < s2.res_x) && (yi >= 0 && yi < s2.res_y) && (zi >= 0 && zi < s2.res_z))
                                    mergedLayer.Set(x, y, z, s2.Get(xi, yi, zi) * multiplier, mergeMethod);
                            }
                        }
                    }
                    return mergedLayer;
                }

                default:
                    return new SurfaceLayer(0, Vector3Int.zero);
            }
        }
    }
}