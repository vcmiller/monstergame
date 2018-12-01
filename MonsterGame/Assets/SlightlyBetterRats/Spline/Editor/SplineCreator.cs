using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SBR.Editor {
    public static class SplineCreator {
        [MenuItem("GameObject/3D Object/Spline/Empty")]
        public static Spline CreateEmptySpline() {
            GameObject splineObj = new GameObject("Spline");
            splineObj.isStatic = true;

            Spline spline = splineObj.AddComponent<Spline>();

            Selection.activeGameObject = splineObj;
            return spline;
        }

        [MenuItem("GameObject/3D Object/Spline/Line")]
        public static Spline CreateLineSpline() {
            var spline = CreateEmptySpline();

            spline.spline.points = new SplineData.Point[] {
                new SplineData.Point() { position = new Vector3(0, 0, 0), tangent = new Vector3(0, 0, 2) },
                new SplineData.Point() { position = new Vector3(0, 0, 10), tangent = new Vector3(0, 0, 2) }
            };

            return spline;
        }

        [MenuItem("GameObject/3D Object/Spline/Circle")]
        public static Spline CreateCircleSpline() {
            var spline = CreateEmptySpline();

            spline.spline.points = new SplineData.Point[] {
                new SplineData.Point() { position = new Vector3(10, 0, 0), tangent = new Vector3(0, 0, 5.5f) },
                new SplineData.Point() { position = new Vector3(0, 0, 10), tangent = new Vector3(-5.5f, 0, 0) },
                new SplineData.Point() { position = new Vector3(-10, 0, 0), tangent = new Vector3(0, 0, -5.5f) },
                new SplineData.Point() { position = new Vector3(0, 0, -10), tangent = new Vector3(5.5f, 0, 0) },
            };
            spline.spline.closed = true;

            return spline;
        }

        [MenuItem("GameObject/3D Object/Spline/Mesh")]
        public static SplineMesh CreateSplineMesh() {
            var spline = CreateLineSpline();
            var mr = spline.gameObject.AddComponent<MeshRenderer>();
            spline.gameObject.AddComponent<MeshFilter>();
            spline.gameObject.AddComponent<MeshCollider>();

            mr.sharedMaterial = BrushCreator.defaultMat;

            var mesh = spline.gameObject.AddComponent<SplineMesh>();
            mesh.spline = spline;
            return mesh;
        }
    }
}
