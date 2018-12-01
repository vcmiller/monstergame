using UnityEngine;

namespace SBR {
    public struct CubicSegmentVector {
        CubicSegment x;
        CubicSegment y;
        CubicSegment z;

        public CubicSegmentVector(float uStart, float uEnd, Vector3 start, Vector3 end, Vector3 tangentStart, Vector3 tangentEnd) {
            // Create individual curves.
            x = new CubicSegment(uStart, uEnd, start.x, end.x, tangentStart.x, tangentEnd.x);
            y = new CubicSegment(uStart, uEnd, start.y, end.y, tangentStart.y, tangentEnd.y);
            z = new CubicSegment(uStart, uEnd, start.z, end.z, tangentStart.z, tangentEnd.z);
        }

        public Vector3 getPoint(float u) {
            // Get point along all curves.
            return new Vector3(x.getPoint(u), y.getPoint(u), z.getPoint(u));
        }

        public Vector3 getDerivative(float u) {
            return new Vector3(x.getDerivative(u), y.getDerivative(u), z.getDerivative(u));

        }
    }
}