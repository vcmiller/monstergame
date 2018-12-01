using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SBR.Editor {
    [CustomEditor(typeof(SplineMesh))]
    public class SplineMeshInspector : UnityEditor.Editor {
        private SplineMesh myTarget { get { return target as SplineMesh; } }

        public override void OnInspectorGUI() {

            EditorGUI.BeginDisabledGroup(!myTarget.profile);

            var mf = myTarget.GetComponent<MeshFilter>();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Mesh")) {
                ExportMesh(mf.sharedMesh, myTarget.name);
            }

            var mc = myTarget.GetComponent<MeshCollider>();
            EditorGUI.BeginDisabledGroup(!mc || !myTarget.profile.separateCollisionMesh);
            if (GUILayout.Button("Export Collision")) {
                ExportMesh(mc.sharedMesh, myTarget.name + "_Collision");
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Export Meshes and Convert")) {
                Mesh mesh = ExportMesh(mf.sharedMesh, myTarget.name);
                Mesh colMesh = null;
                if (mesh && mc && myTarget.profile.separateCollisionMesh) {
                    colMesh = ExportMesh(mc.sharedMesh, myTarget.name + "_Collision");
                }

                if (mesh) {
                    mf.sharedMesh = mesh;
                    if (mc) {
                        mc.sharedMesh = colMesh != null ? colMesh : mesh;
                    }

                    Undo.DestroyObjectImmediate(myTarget);
                }
            }

            EditorGUI.EndDisabledGroup();

            if (myTarget) {
                serializedObject.Update();
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
            }
        }

        private Mesh ExportMesh(Mesh mesh, string name) {
            var path = EditorUtility.SaveFilePanelInProject("Save New Mesh", name + ".asset", "asset", "Save new asset to file");
            if (path.Length > 0) {
                var result = Instantiate(mesh);
                Unwrapping.GenerateSecondaryUVSet(result);
                AssetDatabase.CreateAsset(result, path);
                AssetDatabase.Refresh();
                return result;
            } else {
                return null;
            }
        }
    }
}