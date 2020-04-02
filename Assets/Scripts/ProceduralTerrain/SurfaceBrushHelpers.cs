using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

namespace Worlds.ProceduralTerrain.Brush.Helpers
{
    public class PrimitiveTetrahedron
    {
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
        public Vector3 D;

        public PrimitiveTetrahedron(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
        {
            this.A = A;
            this.B = B;
            this.C = C;
            this.D = D;
        }

        public Vector4 Barycentric(Vector3 P)
        {
            Matrix<float> M = Matrix<float>.Build.DenseOfArray(new float[,] {
                                                                            { A.x, B.x, C.x, D.x },
                                                                            { A.y, B.y, C.y, D.y },
                                                                            { A.z, B.z, C.z, D.z },
                                                                            { 1f, 1f, 1f, 1f }
                                                                                                    });
            Matrix<float> L = Matrix<float>.Build.Dense(4, 1);
            Matrix<float> Mp = Matrix<float>.Build.DenseOfArray(new float[,]    {
                                                                                { P.x },
                                                                                { P.y },
                                                                                { P.z },
                                                                                { 1f }
                                                                                        });

            L = M.Inverse() * Mp;

            return new Vector4(L[0, 0], L[1, 0], L[2, 0], L[3, 0]);
        }

        public bool IsPointInside(Vector3 point)
        {
            bool isInside = false;
            Vector4 bar = Barycentric(point);
            if(bar.x >= 0 && bar.y >= 0 && bar.z >= 0 && bar.w >= 0) isInside = true;
            return isInside;
        }
    }
}