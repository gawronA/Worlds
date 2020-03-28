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
            Initalize(res, res, res, Vector3Int.zero);
        }

        public SurfaceLayer(int x, int y, int z)
        {
            Initalize(x, y, z, Vector3Int.zero);
        }

        public SurfaceLayer(int res, Vector3Int position)
        {
            Initalize(res, res, res, position);
        }

        public SurfaceLayer(int x, int y, int z, Vector3Int position)
        {
            Initalize(x, y, z, position);
        }

        private void Initalize(int x, int y, int z, Vector3Int position)
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

        public void Merge(SurfaceLayer layer, float multiplier, MergeMethod mergeMethod, MergeSize mergeSize)
        {
            switch(mergeSize)
            {
                case MergeSize.Cut:
                {
                    for(int z = 0; z < res_x; z++)
                    {
                        for(int y = 0; y < res_y; y++)
                        {
                            for(int x = 0; x < res_x; x++)
                            {
                                //merge
                                int xi, yi, zi;
                                xi = (position.x + x) - layer.position.x;
                                yi = (position.y + y) - layer.position.y;
                                zi = (position.z + z) - layer.position.z;
                                if((xi >= 0 && xi < res_x) && (yi >= 0 && yi < res_y) && (zi >= 0 && zi < res_z))
                                   Set(x, y, z, layer.Get(xi, yi, zi) * multiplier, mergeMethod);
                            }
                        }
                    }
                }
                break;

                /*case MergeSize.Extend:
                {
                    Vector3Int position = new Vector3Int(this.position.x > layer.position.x ? layer.position.x : this.position.x,
                                                           this.position.y > layer.position.y ? layer.position.y : this.position.y,
                                                           this.position.z > layer.position.z ? layer.position.z : this.position.z);

                    int res_x = this.position.x + this.res_x > layer.position.x + layer.res_x ? this.position.x - position.x + this.res_x : layer.position.x - position.x + layer.res_x;
                    int res_y = this.position.y + this.res_y > layer.position.y + layer.res_y ? this.position.y - position.y + this.res_y : layer.position.y - position.y + layer.res_y;
                    int res_z = this.position.z + this.res_z > layer.position.z + layer.res_z ? this.position.z - position.z + this.res_z : layer.position.z - position.z + layer.res_z;

                    Initalize(res_x, res_y, res_z, position);

                    for(int z = 0; z < res_x; z++)
                    {
                        for(int y = 0; y < res_y; y++)
                        {
                            for(int x = 0; x < res_x; x++)
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
                }*/
            }
        }

        public void Filter(float[] kernel)
        {
            int kernelSize = (int)Mathf.Pow(kernel.Length, 1f/3f);
            float[] values = new float[size];
            int offset = kernelSize / 2;
            for(int z = 0; z < res_z; z++)
            {
                for(int y = 0; y < res_y; y++)
                {
                    for(int x = 0; x < res_x; x++)
                    {
                        float sum = 0;
                        float sumDiv = 0;
                        //kernel
                        for(int kz = 0; kz < kernelSize; kz++)
                        {
                            for(int ky = 0; ky < kernelSize; ky++)
                            {
                                for(int kx = 0; kx < kernelSize; kx++)
                                {
                                    int xs = x + kx - offset;
                                    int ys = y + ky - offset;
                                    int zs = z + kz - offset;
                                    if((xs>=0 && xs < res_x) && (ys>=0 && ys < res_y) && (zs >= 0 && zs < res_z))
                                    {
                                        float kernelValue = kernel[kx + ky * kernelSize + kz * kernelSize * kernelSize];
                                        sum += kernelValue * Get(xs, ys, zs);
                                        sumDiv += kernelValue;
                                    }
                                }
                            }
                        }
                        sum /= sumDiv;
                        values[x + y * res_x + z * res_y * res_y] = sum;
                    }
                }
            }
            this.values = values;
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

    public static class FilterKernel3D
    {
        public static float[] Mean(int size)
        {
            int size3 = size * size * size;
            float[] kernel = new float[size3];
            for(int i = 0; i < size3; i++) kernel[i] = 1f;
            
            return kernel;
        }

        public static float[] Gaussian(int size, float sigma)
        {
            int size3 = size * size * size;
            float[] kernel = new float[size3];

            int offset = size / 2;
            for(int z = 0; z < size; z++)
            {
                for(int y = 0; y < size; y++)
                {
                    for(int x = 0; x < size; x++)
                    {
                        kernel[x + y * size + z * size * size] = 1f / Mathf.Pow(Mathf.Sqrt(2f * Mathf.PI) * sigma, 3) * Mathf.Exp(-((Mathf.Pow(x - offset, 2) + Mathf.Pow(y - offset, 2) + Mathf.Pow(z - offset, 2)) / (2 * Mathf.Pow(sigma, 2))));
                    }
                }
            }
            return kernel;
        }
    }
}