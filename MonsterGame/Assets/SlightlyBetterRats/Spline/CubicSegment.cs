using UnityEngine;

namespace SBR {
    public struct CubicSegment {
        private float a, b, c, d;

        public CubicSegment(float uStart, float uEnd, float start, float end, float tangentStart, float tangentEnd) {
            // Construct matrix to solve system of equations.
            Matrix4x4 mat = new Matrix4x4();

            mat[0, 0] = uStart * uStart * uStart; mat[0, 1] = uStart * uStart; mat[0, 2] = uStart; mat[0, 3] = 1;
            mat[1, 0] = uEnd * uEnd * uEnd; mat[1, 1] = uEnd * uEnd; mat[1, 2] = uEnd; mat[1, 3] = 1;
            mat[2, 0] = 3 * uStart * uStart; mat[2, 1] = 2 * uStart; mat[2, 2] = 1; mat[2, 3] = 0;
            mat[3, 0] = 3 * uEnd * uEnd; mat[3, 1] = 2 * uEnd; mat[3, 2] = 1; mat[3, 3] = 0;

            Matrix4x4 m2 = Matrix4x4.Inverse(mat);
            Vector4 v = new Vector4(start, end, tangentStart, tangentEnd);

            // Get solution vector.
            Vector4 result = m2 * v;

            // Store results.
            a = result.x;
            b = result.y;
            c = result.z;
            d = result.w;

            //Log::log << "f(u) = " << a << "x^3 + " << b << "x^2 + " << c << "x + " << d << "\n";
        }

        public float getPoint(float u) {
            // Basic cubic function.
            return
                a * u * u * u +
                b * u * u +
                c * u +
                d;
        }

        public float getDerivative(float u) {
            return
                3 * a * u * u +
                2 * b * u +
                c;
        }

    }
}