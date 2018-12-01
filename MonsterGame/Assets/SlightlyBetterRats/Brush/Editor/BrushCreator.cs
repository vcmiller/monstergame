using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SBR.Editor {
    public static class BrushCreator {
        private static Material _defaultMat;
        public static Material defaultMat {
            get {
                if (!_defaultMat) {
                    MeshRenderer mr = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
                    _defaultMat = mr.sharedMaterial;
                    Object.DestroyImmediate(mr.gameObject);
                }

                return _defaultMat;
            }
        }

        [MenuItem("GameObject/3D Object/Brush/Box")]
        public static void CreateBoxBrush() {
            CreateBrush(Brush.Type.Box);
        }

        [MenuItem("GameObject/3D Object/Brush/Slant")]
        public static void CreateSlantBrush() {
            CreateBrush(Brush.Type.Slant);
        }

        [MenuItem("GameObject/3D Object/Brush/Cyllinder")]
        public static void CreateCyllinderBrush() {
            CreateBrush(Brush.Type.Cyllinder);
        }

        [MenuItem("GameObject/3D Object/Brush/Block Stair")]
        public static void CreateBlockStairBrush() {
            CreateBrush(Brush.Type.BlockStair);
        }

        [MenuItem("GameObject/3D Object/Brush/Slant Stair")]
        public static void CreateSlantStairBrush() {
            CreateBrush(Brush.Type.SlantStair);
        }

        [MenuItem("GameObject/3D Object/Brush/Separate Stair")]
        public static void CreateSeparateStairBrush() {
            CreateBrush(Brush.Type.SeparateStair);
        }

        [MenuItem("GameObject/3D Object/Brush/Freeform")]
        public static void CreateFreeformBrush() {
            CreateBrush(Brush.Type.Freeform);
        }

        private static void CreateBrush(Brush.Type type) {
            GameObject brushGeom = GameObject.Find("BrushGeometry");

            if (!brushGeom) {
                brushGeom = new GameObject("BrushGeometry");
                brushGeom.isStatic = true;
            }

            GameObject brushObj = new GameObject(type + " Brush");
            brushObj.isStatic = true;
            brushObj.transform.parent = brushGeom.transform;

            Brush brush = brushObj.AddComponent<Brush>();
            brush.type = type;
            brush.GetComponent<MeshRenderer>().sharedMaterial = defaultMat;

            Selection.activeGameObject = brushObj;
        }
    }
    
}
