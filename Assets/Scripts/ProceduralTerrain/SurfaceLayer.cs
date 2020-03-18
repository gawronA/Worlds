using UnityEngine;

namespace Worlds.ProceduralTerrain.Generator.Engine
{
    public class SurfaceLayer
    {
        enum MergeSize { Extend, Cut }
        enum MergeMethod { Add, Subtract, Overlay }

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

        public SurfaceLayer(int x, int y, int z, Vector3Int position)
        {
            res_x = x;
            res_y = y;
            res_z = z;
            size = res_x * res_y * res_z;

            i_y = res_x * res_x;
            i_z = res_y * res_y;

            this.position = position;

            values = new float[size];
        }

        public SurfaceLayer(int res, Vector3Int position)
        {
            res_x = res_y = res_z = res;
            size = res_x * res_y * res_z;

            i_y = res_x * res_x;
            i_z = res_y * res_y;

            this.position = position;

            values = new float[size];
        }

        public float Get(int x, int y, int z)
        {
            return values[x + y * i_y + z * i_z];
        }

        public void Set(int x, int y, int z, float value)
        {
            values[x + y * i_y + z * i_z] = value;
        }

        public static SurfaceLayer Merge(SurfaceLayer s1, SurfaceLayer s2, MergeMethod mergeMethod, MergeSize mergeSize)
        {
            Vector3Int relPos = s2.position - s1.position; //relative position from s1 to s2
            switch(mergeSize)
            {
                case MergeSize.Cut:
                    
                    break;

                case MergeSize.Extend:
                    Vector3Int position = new Vector3Int(   s1.position.x > s2.position.x ? s2.position.x : s1.position.x,
                                                            s1.position.y > s2.position.y ? s2.position.y : s1.position.y,
                                                            s1.position.z > s2.position.z ? s2.position.z : s1.position.z);
                    int res_x = s2.res_x > s1.res_x ? s2.res_x : s1.res_x;
                    int res_y = s2.res_y > s1.res_y ? s2.res_y : s1.res_y;
                    int res_z = s2.res_z > s1.res_z ? s2.res_z : s1.res_z;
                    

                    break;
            }
        }
    }
}