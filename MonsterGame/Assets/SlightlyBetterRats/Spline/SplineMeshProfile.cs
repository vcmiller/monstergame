using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SBR {
    [CreateAssetMenu(fileName = "NewProfile", menuName = "Spline Mesh Profile")]
    public class SplineMeshProfile : ScriptableObject {
        public event Action<bool> PropertyChanged;

        [Tooltip("Whether to build a separate mesh for the collision, or just use the render mesh.")]
        public bool separateCollisionMesh;

        [Tooltip("Whether to separate all materials in the MeshRenderer, or combine them all into one material slot.")]
        public bool keepSeparateMaterials;

        private int[] submeshStartIndices;

        [Flags]
        public enum StretchMode {
            Nothing = 0,
            Gaps = 1,
            Mesh = 2
        }

        public enum AlignMode {
            Deform,
            Align,
            ForceUpright,
            NoRotation
        }

        [System.Serializable]
        public class MeshInfo {
            public Mesh render;
            public Mesh collision;
            public int repeat = 1;
            public StretchMode stretchMode = StretchMode.Nothing;
            public AlignMode alignMode = AlignMode.Deform;
            public float gapBefore;
            public float gapAfter;

            public float meshLength { get { return render != null ? render.bounds.size.z : 0; } }
            public float gapLength { get { return gapBefore + gapAfter; } }
            public float totalLength { get { return meshLength + gapLength; } }
        }

        public MeshInfo[] meshes = new MeshInfo[0];

        void OnValidate() {
            if (!Application.isPlaying) {
                OnChanged(false);
            }
        }

        public void OnChanged(bool update) {
            if (PropertyChanged != null) {
                PropertyChanged(update);
            }
        }

        public virtual void CreateMeshes(SplineData spline, out Mesh mesh, out Mesh collisionMesh) {
            if (meshes.Length == 0) {
                Debug.LogError("SplineMeshProfile needs at least one mesh!");
            }

            int submeshCount = GetSubmeshCount();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int>[] triangles = new List<int>[submeshCount];

            List<Vector3> collisionVertices = null;
            List<Vector3> collisionNormals = null;
            List<int> collisionTriangles = null;
            
            if (separateCollisionMesh) {
                collisionVertices = new List<Vector3>();
                collisionNormals = new List<Vector3>();
                collisionTriangles = new List<int>();
            }

            if (meshes.Any(m => m.totalLength > 0)) {
                float f = 0;
                int meshIndex = 0;
                int rep = 0;

                float stretch = CalculateStretch(spline);

                while (f < 1) {
                    if (meshes[meshIndex].render) {
                        f = AddMesh(spline, meshIndex, stretch, f, vertices, normals, uvs, triangles, collisionVertices, collisionNormals, collisionTriangles);
                    } else {
                        f += meshes[meshIndex].gapLength / spline.length;
                    }

                    rep++;
                    if (rep >= meshes[meshIndex].repeat) {
                        rep = 0;
                        meshIndex = (meshIndex + 1) % meshes.Length;
                    }
                }
            } else {
                Debug.LogWarning("SplineMesh cannot be built without at least one mesh with a z size greater than 0.");
            }

            mesh = new Mesh();
            mesh.name = "Spline Mesh";
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) {
                mesh.SetTriangles(triangles[i], i);
            }
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            
            if (separateCollisionMesh) {
                collisionMesh = new Mesh();
                collisionMesh.name = "Spline Collision";
                collisionMesh.SetVertices(collisionVertices);
                collisionMesh.SetNormals(collisionNormals);
                collisionMesh.subMeshCount = 1;
                collisionMesh.SetTriangles(collisionTriangles, 0);
                collisionMesh.RecalculateTangents();
                collisionMesh.RecalculateBounds();
            } else {
                collisionMesh = mesh;
            }
        }

        public int GetSubmeshCount() {
            if (keepSeparateMaterials) {
                int submeshCount = 0;
                if (submeshStartIndices == null || submeshStartIndices.Length != meshes.Length) {
                    submeshStartIndices = new int[meshes.Length];
                }

                for (int i = 0; i < meshes.Length; i++) {
                    submeshStartIndices[i] = submeshCount;
                    if (meshes[i].render) {
                        submeshCount += meshes[i].render.subMeshCount;
                    }
                }

                return submeshCount;
            } else {
                return 1;
            }
        }

        protected virtual float CalculateStretch(SplineData spline) {
            float length = spline.length;

            float meshLength = 0;
            float stretchable = 0;

            int meshIndex = 0;
            int rep = 0;
            while (true) {
                float sz = meshes[meshIndex].meshLength;
                float gap = meshes[meshIndex].gapLength;

                if (meshLength + sz + gap <= length) {
                    meshLength += sz + gap;

                    if ((meshes[meshIndex].stretchMode & StretchMode.Mesh) == StretchMode.Mesh) {
                        stretchable += sz;
                    }

                    if ((meshes[meshIndex].stretchMode & StretchMode.Gaps) == StretchMode.Gaps) {
                        stretchable += gap;
                    }
                } else {
                    break;
                }

                rep++;
                if (rep >= meshes[meshIndex].repeat) {
                    rep = 0;
                    meshIndex = (meshIndex + 1) % meshes.Length;
                }
            }

            if (stretchable > 0) {
                return (length - meshLength + stretchable) / stretchable;
            } else {
                return 1;
            }
        }

        protected virtual float AddMesh(SplineData spline, int index, float stretchFactor, float meshStart, 
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int>[] triangles,
            List<Vector3> collisionVertices, List<Vector3> collisionNormals, List<int> collisionTriangles) {
            var mesh = meshes[index];
            var toAdd = mesh.render;
            var toAddC = mesh.collision;

            var stretch = mesh.stretchMode;
            var align = mesh.alignMode;

            float sizeZ = mesh.meshLength;
            float startGap = meshes[index].gapBefore;
            float endGap = meshes[index].gapAfter;

            if ((stretch & StretchMode.Mesh) == StretchMode.Mesh) {
                sizeZ *= stretchFactor;
            }

            if ((stretch & StretchMode.Gaps) == StretchMode.Gaps) {
                startGap *= stretchFactor;
                endGap *= stretchFactor;
            }

            meshStart += startGap / spline.length;
            float meshEnd = meshStart + (sizeZ + endGap) / spline.length;

            if (meshEnd <= 1 + (0.1f / spline.length)) {
                int vCount = vertices.Count;
                float minZ = toAdd.bounds.min.z;
                var center = toAdd.bounds.center;
                AddVertexDataDeformed(spline, toAdd, stretch, align, stretchFactor, meshStart, minZ, center, vertices, normals, uvs);
            
                for (int i = 0; i < toAdd.subMeshCount; i++) {
                    int destList = 0;
                    if (keepSeparateMaterials) {
                        destList = submeshStartIndices[index];
                    }

                    if (triangles[destList] == null) {
                        triangles[destList] = new List<int>();
                    }

                    AddTriangleData(toAdd, i, vCount, triangles[i]);
                }

                if (separateCollisionMesh && toAddC) {
                    int cvCount = collisionVertices.Count;
                    AddVertexDataDeformed(spline, toAddC, stretch, align, stretchFactor, meshStart, minZ, center, collisionVertices, collisionNormals, null);
                    AddTriangleData(toAddC, 0, cvCount, collisionTriangles);
                }
            }

            return meshEnd;
        }

        private void AddVertexDataDeformed(
            SplineData spline, Mesh mesh, StretchMode stretch, AlignMode align, 
            float stretchFactor, float meshStart, float minZ, Vector3 center,
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs) {
            
            Vector3[] inVerts = mesh.vertices;
            Vector3[] inNormals = mesh.normals;
            Vector2[] inUvs = uvs != null ? mesh.uv : null;
            
            float centerPos = Mathf.Clamp01(meshStart + (center.z - minZ) / spline.length);
            var centerRotation = spline.GetRotation(centerPos);

            if (align == AlignMode.ForceUpright) {
                centerRotation = Quaternion.Euler(0, centerRotation.eulerAngles.y, 0);
            } else if (align == AlignMode.NoRotation) {
                centerRotation = Quaternion.identity;
            }

            Vector3 centerOffset = spline.GetPoint(centerPos) + (centerRotation * Vector3.forward * minZ);

            Matrix4x4 alignMatrix = Matrix4x4.TRS(centerOffset, centerRotation, Vector3.one);

            for (int i = 0; i < inVerts.Length; i++) {
                var vert = inVerts[i];
                var normal = inNormals[i];
                var uv = Vector2.zero;
                if (inUvs != null && inUvs.Length > i) {
                    uv = inUvs[i];
                }

                vert.z -= minZ;
                if ((stretch & StretchMode.Mesh) == StretchMode.Mesh) {
                    vert.z *= stretchFactor;
                }

                if (align == AlignMode.Deform) {
                    float pos = Mathf.Clamp01(meshStart + (vert.z / spline.length));
                    var srot = spline.GetRotation(pos);
                    var spos = spline.GetPoint(pos);

                    vert = spos + (srot * Vector3.ProjectOnPlane(vert, Vector3.forward));
                    normal = srot * normal;
                } else {
                    vert = alignMatrix.MultiplyPoint(vert);

                    normal = alignMatrix.MultiplyVector(normal);
                }

                vertices.Add(vert);
                normals.Add(normal);
                if (uvs != null) uvs.Add(uv);
            }
        }

        private void AddTriangleData(Mesh mesh, int submesh, int vStart, List<int> triangles) {
            triangles.AddRange(mesh.GetTriangles(submesh).Select(t => t + vStart));
        }
    }
}