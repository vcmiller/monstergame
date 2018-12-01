using System;
using UnityEngine;
using System.Collections.Generic;
using SBR.Geometry;

namespace SBR {

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class Brush : MonoBehaviour {
        public Type type;
        public Vector3 size = new Vector3(1, 1, 1);
        public Vector3 textureWorldScale = new Vector3(1, 1, 1);
        public Vector2 textureLocalScale = new Vector2(1, 1);
        public int complexity = 16;
        public bool smooth = false;
        public float stepSeparation = 0.2f;
        public float stepHeight = 0.1f;
        public bool addBottomStep = true;
        public FreeformMesh mesh;

        public MeshFilter filter { get { return GetComponent<MeshFilter>(); } }
        public MeshRenderer render { get { return GetComponent<MeshRenderer>(); } }
        public MeshCollider col { get { return GetComponent<MeshCollider>(); } }
        
        [HideInInspector]
        public bool undone = false;

        [HideInInspector]
        [NonSerialized]
        public bool dirty;

        private Type lastType = Type.Freeform;
        private Vector3 lastSize;
        private Vector3 lastWorldScale;
        private Vector2 lastLocalScale;
        private int lastComplexity;
        private bool lastSmooth;
        private float lastStepSep;
        private float lastStepHeight;
        private bool lastBottomStep;
        private bool lastIsStatic;

        private void Start() {
            if (Application.isPlaying) {
                Destroy(this);
            } else {
                filter.sharedMesh = new Mesh();
                filter.sharedMesh.name = "Brush Mesh";
                col.sharedMesh = filter.sharedMesh;
                dirty = true;
            }
        }

        public void Update() {

            if (!dirty && !undone &&
                (lastType == type &&
                lastSize == size && 
                lastWorldScale == textureWorldScale && 
                lastLocalScale == textureLocalScale && 
                lastComplexity == complexity && 
                lastSmooth == smooth &&
                lastStepSep == stepSeparation &&
                lastStepHeight == stepHeight &&
                lastBottomStep == addBottomStep && 
                lastIsStatic == gameObject.isStatic)) {
                return;
            }

            Mesh m = filter.sharedMesh;

            if (m == null) {
                m = new Mesh();
                m.name = "Brush Mesh";
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<List<int>> indices = new List<List<int>>();

            if (type == Type.Box) {
                UpdateBox(vertices, uvs, indices);
                col.convex = true;
            } else if (type == Type.Slant) {
                UpdateSlant(vertices, uvs, indices);
                col.convex = true;
            } else if (type == Type.Cyllinder) {
                UpdateCyllinder(vertices, uvs, normals, indices);
                col.convex = true;
            } else if (type == Type.BlockStair) {
                UpdateBlockStair(vertices, uvs, indices);
            } else if (type == Type.SlantStair) {
                UpdateSlantStair(vertices, uvs, indices);
            } else if (type == Type.SeparateStair) {
                UpdateSeparateStair(vertices, uvs, indices);
            } else {
                if (lastType != Type.Freeform || mesh == null) {
                    mesh = new FreeformMesh();

                    UpdateBox(vertices, uvs, indices);
                }

                UpdateFreeform(vertices, uvs, indices);
            }

            m.subMeshCount = 0;
            m.vertices = null;
            m.uv = null;

            m.vertices = vertices.ToArray();
            m.uv = uvs.ToArray();
            m.subMeshCount = indices.Count;

            if (type == Type.Cyllinder && smooth) {
                m.normals = normals.ToArray();
            }

            for (int i = 0; i < indices.Count; i++) {
                m.SetIndices(indices[i].ToArray(), MeshTopology.Triangles, i);
            }

            m.RecalculateBounds();

            if (type != Type.Cyllinder || !smooth) {
                m.RecalculateNormals();
            }

            if (gameObject.isStatic)
#if UNITY_EDITOR
                UnityEditor.Unwrapping.GenerateSecondaryUVSet(m);
#endif

            filter.sharedMesh = m;
            col.sharedMesh = m;

            Material[] mats = render.sharedMaterials;
            if (m.subMeshCount != mats.Length && m.subMeshCount != 0) {
                Material[] newMats = new Material[m.subMeshCount];

                for (int i = 0; i < newMats.Length; i++) {
                    newMats[i] = mats[Mathf.Min(i, mats.Length - 1)];
                }

                render.sharedMaterials = newMats;
            }

            if (undone) {
                mesh.ForceGraphUpdate();
            }

            undone = false;
            dirty = false;
            lastType = type;
            lastSize = size;
            lastWorldScale = textureWorldScale;
            lastLocalScale = textureLocalScale;
            lastComplexity = complexity;
            lastSmooth = smooth;
            lastStepSep = stepSeparation;
            lastStepHeight = stepHeight;
            lastBottomStep = addBottomStep;
            lastIsStatic = gameObject.isStatic;
        }

        private void UpdateFreeform(List<Vector3> vertices, List<Vector2> uvs, List<List<int>> indices) {
            if (mesh == null) {
                mesh = new FreeformMesh();
            }

            indices.Add(new List<int>());

            mesh.GetMeshData(vertices, uvs, indices[0], textureWorldScale, textureLocalScale);
        }

        private void UpdateBlockStair(List<Vector3> vertices, List<Vector2> uvs, List<List<int>> indices) {
            if (mesh == null) {
                mesh = new FreeformMesh();
            } else {
                mesh.Clear();
            }

            int numSteps = Mathf.CeilToInt(size.y / stepSeparation);

            float stepLength = size.z / numSteps;

            int last1 = -1, last2 = -1, back1 = -1, back2 = -1;
            
            for (int i = 0; i < numSteps; i++) {
                float shAct = size.y / numSteps;
                float height = size.y - (i * shAct);
                int v1 = i > 0 ? last1 : mesh.AddVertex(new Vector3(0, height, i * stepLength));
                int v2 = i > 0 ? last2 : mesh.AddVertex(new Vector3(size.x, height, i * stepLength));
                int v3 = mesh.AddVertex(new Vector3(size.x, height, (i + 1) * stepLength));
                int v4 = mesh.AddVertex(new Vector3(0, height, (i + 1) * stepLength));

                int v5 = mesh.AddVertex(new Vector3(0, height - shAct, (i + 1) * stepLength));
                int v6 = mesh.AddVertex(new Vector3(size.x, height - shAct, (i + 1) * stepLength));
                
                int v7 = mesh.AddVertex(new Vector3(0, 0, i * stepLength));
                int v8 = mesh.AddVertex(new Vector3(size.x, 0, i * stepLength));
                int v9 = mesh.AddVertex(new Vector3(size.x, 0, (i + 1) * stepLength));
                int v10 = mesh.AddVertex(new Vector3(0, 0, (i + 1) * stepLength));

                mesh.AddFace(v4, v3, v2, v1); // Step
                mesh.AddFace(v3, v4, v5, v6); // Back
                mesh.AddFace(v7, v10, v4, v1);
                mesh.AddFace(v9, v8, v2, v3);

                if (i == 0) {
                    mesh.AddFace(v8, v7, v1, v2);
                    back1 = v7;
                    back2 = v8;
                } else if (i == numSteps - 1) {
                    mesh.AddFace(v9, v10, back1, back2);
                }

                last1 = v5;
                last2 = v6;
            }

            if (addBottomStep) {
                int v1 = mesh.AddVertex(new Vector3(0, 0, size.z + stepLength));
                int v2 = mesh.AddVertex(new Vector3(size.x, 0, size.z + stepLength));

                mesh.AddFace(v1, v2, v2, v1);
            }

            indices.Add(new List<int>());

            mesh.GetMeshData(vertices, uvs, indices[0], textureWorldScale, textureLocalScale);
        }

        private void UpdateSlantStair(List<Vector3> vertices, List<Vector2> uvs, List<List<int>> indices) {

            if (mesh == null) {
                mesh = new FreeformMesh();
            } else {
                mesh.Clear();
            }

            int numSteps = Mathf.CeilToInt(size.y / stepSeparation);

            float stepLength = size.z / numSteps;

            int last1 = -1, last2 = -1, back1 = -1, back2 = -1;

            for (int i = 0; i < numSteps; i++) {
                float shAct = size.y / numSteps;
                float height = size.y - (i * shAct);
                int v1 = i > 0 ? last1 : mesh.AddVertex(new Vector3(0, height, i * stepLength));
                int v2 = i > 0 ? last2 : mesh.AddVertex(new Vector3(size.x, height, i * stepLength));
                int v3 = mesh.AddVertex(new Vector3(size.x, height, (i + 1) * stepLength));
                int v4 = mesh.AddVertex(new Vector3(0, height, (i + 1) * stepLength));

                int v5 = mesh.AddVertex(new Vector3(0, height - shAct, (i + 1) * stepLength));
                int v6 = mesh.AddVertex(new Vector3(size.x, height - shAct, (i + 1) * stepLength));

                int v7 = mesh.AddVertex(new Vector3(0, i > numSteps - 2 ? 0 : height - 2 * shAct, i * stepLength));
                int v8 = mesh.AddVertex(new Vector3(size.x, i > numSteps - 2 ? 0 : height - 2 * shAct, i * stepLength));
                int v9 = mesh.AddVertex(new Vector3(size.x, i > numSteps - 3 ? 0 : height - 3 * shAct, (i + 1) * stepLength));
                int v10 = mesh.AddVertex(new Vector3(0, i > numSteps - 3 ? 0 : height - 3 * shAct, (i + 1) * stepLength));

                mesh.AddFace(v4, v3, v2, v1); // Step
                mesh.AddFace(v3, v4, v5, v6); // Back
                mesh.AddFace(v7, v10, v4, v1);
                mesh.AddFace(v9, v8, v2, v3);

                if (i == 0) {
                    mesh.AddFace(v8, v7, v1, v2);
                    back1 = v7;
                    back2 = v8;
                } else if (i == numSteps - 2) {
                    mesh.AddFace(v8, v7, back1, back2);
                    back1 = v7;
                    back2 = v8;
                } else if (i == numSteps - 1) {
                    mesh.AddFace(v9, v10, back1, back2);
                }

                last1 = v5;
                last2 = v6;
            }
            
            if (addBottomStep) {
                int v1 = mesh.AddVertex(new Vector3(0, 0, size.z + stepLength));
                int v2 = mesh.AddVertex(new Vector3(size.x, 0, size.z + stepLength));

                mesh.AddFace(v1, v2, v2, v1);
            }

            indices.Add(new List<int>());

            mesh.GetMeshData(vertices, uvs, indices[0], textureWorldScale, textureLocalScale);
        }

        private void UpdateSeparateStair(List<Vector3> vertices, List<Vector2> uvs, List<List<int>> indices) {

            if (mesh == null) {
                mesh = new FreeformMesh();
            } else {
                mesh.Clear();
            }

            int numSteps = Mathf.CeilToInt(size.y / stepSeparation);

            float stepLength = size.z / numSteps;

            for (int i = 0; i < numSteps; i++) {
                float shAct = size.y / numSteps;
                float height = size.y - (i * shAct);
                int v1 = mesh.AddVertex(new Vector3(0, height, i * stepLength));
                int v2 = mesh.AddVertex(new Vector3(size.x, height, i * stepLength));
                int v3 = mesh.AddVertex(new Vector3(size.x, height, (i + 1) * stepLength));
                int v4 = mesh.AddVertex(new Vector3(0, height, (i + 1) * stepLength));

                int v7 = mesh.AddVertex(new Vector3(0, height - stepHeight, i * stepLength));
                int v8 = mesh.AddVertex(new Vector3(size.x, height - stepHeight, i * stepLength));
                int v9 = mesh.AddVertex(new Vector3(size.x, height - stepHeight, (i + 1) * stepLength));
                int v10 = mesh.AddVertex(new Vector3(0, height - stepHeight, (i + 1) * stepLength));

                mesh.AddFace(v4, v3, v2, v1); // Step
                mesh.AddFace(v3, v4, v10, v9); // Back
                mesh.AddFace(v7, v10, v4, v1);
                mesh.AddFace(v9, v8, v2, v3);
                mesh.AddFace(v8, v7, v1, v2);
                mesh.AddFace(v7, v8, v9, v10);
            }

            if (addBottomStep) {
                int v1 = mesh.AddVertex(new Vector3(0, 0, size.z + stepLength));
                int v2 = mesh.AddVertex(new Vector3(size.x, 0, size.z + stepLength));

                mesh.AddFace(v1, v2, v2, v1);
            }

            indices.Add(new List<int>());

            mesh.GetMeshData(vertices, uvs, indices[0], textureWorldScale, textureLocalScale);
        }

        private void UpdateBox(List<Vector3> vertices, List<Vector2> uvs, List<List<int>> indices) {
            
            if (mesh == null) {
                mesh = new FreeformMesh();
            } else {
                mesh.Clear();
            }

            int v1 = mesh.AddVertex(new Vector3(size.x, size.y, size.z));
            int v2 = mesh.AddVertex(new Vector3(0, size.y, size.z));
            int v3 = mesh.AddVertex(new Vector3(0, size.y, 0));
            int v4 = mesh.AddVertex(new Vector3(size.x, size.y, 0));
            int v5 = mesh.AddVertex(new Vector3(size.x, 0, size.z));
            int v6 = mesh.AddVertex(new Vector3(0, 0, size.z));
            int v7 = mesh.AddVertex(new Vector3(0, 0, 0));
            int v8 = mesh.AddVertex(new Vector3(size.x, 0, 0));

            mesh.AddFace(v2, v1, v4, v3);
            mesh.AddFace(v1, v2, v6, v5);
            mesh.AddFace(v3, v4, v8, v7);
            mesh.AddFace(v2, v3, v7, v6);
            mesh.AddFace(v4, v1, v5, v8);
            mesh.AddFace(v7, v8, v5, v6);

            indices.Add(new List<int>());

            mesh.GetMeshData(vertices, uvs, indices[0], textureWorldScale, textureLocalScale);
        }

        private void UpdateSlant(List<Vector3> vertices, List<Vector2> uvs, List<List<int>> indices) {

            if (mesh == null) {
                mesh = new FreeformMesh();
            } else {
                mesh.Clear();
            }

            int v1 = mesh.AddVertex(new Vector3(0, size.y, 0));
            int v2 = mesh.AddVertex(new Vector3(size.x, size.y, 0));
            int v3 = mesh.AddVertex(new Vector3(size.x, 0, size.z));
            int v4 = mesh.AddVertex(new Vector3(0, 0, size.z));
            int v5 = mesh.AddVertex(new Vector3(0, 0, 0));
            int v6 = mesh.AddVertex(new Vector3(size.x, 0, 0));

            mesh.AddFace(v1, v2, v6, v5);
            mesh.AddFace(v5, v6, v3, v4);
            mesh.AddFace(v2, v1, v4, v3); // Slant
            mesh.AddFace(v5, v4, v1);
            mesh.AddFace(v6, v2, v3);

            indices.Add(new List<int>());

            mesh.GetMeshData(vertices, uvs, indices[0], textureWorldScale, textureLocalScale);
        }

        private void UpdateCyllinder(List<Vector3> vertices, List<Vector2> uvs, List<Vector3> normals, List<List<int>> indices) {
            var ext = size / 2;

            var offset = ext;
            
            Vector3 topCenter = new Vector3(0, ext.y, 0) + offset;
            Vector3 botCenter = new Vector3(0, -ext.y, 0) + offset;

            indices.Add(new List<int>());

            for (float i = 0; i < complexity; i++) {
                float angle = Mathf.PI * 2 * (i / complexity);
                float angle2 = Mathf.PI * 2 * ((i + 1) / complexity);

                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);

                float sin2 = Mathf.Sin(angle2);
                float cos2 = Mathf.Cos(angle2);

                Vector3 v1 = new Vector3(sin * ext.x, -ext.y, cos * ext.z) + offset;
                Vector3 v2 = new Vector3(sin * ext.x, ext.y, cos * ext.z) + offset;
                Vector3 v3 = new Vector3(sin2 * ext.x, ext.y, cos2 * ext.z) + offset;
                Vector3 v4 = new Vector3(sin2 * ext.x, -ext.y, cos2 * ext.z) + offset;

                float texrep = 4 * Mathf.Sqrt(size.x * size.z / (textureWorldScale.x * textureWorldScale.z));
                float txInc = texrep / complexity;
                float tx = texrep - (txInc * i);

                Vector2 tw = new Vector3(ext.x / textureWorldScale.x, ext.z / textureWorldScale.z);

                Tri(v2, v3, topCenter,
                    new Vector2((sin + 1) * tw.x, (cos + 1) * tw.y),
                    new Vector2((sin2 + 1) * tw.x, (cos2 + 1) * tw.y),
                    tw, 1, 1, vertices, uvs, indices[0]);

                for (int j = 0; j < 3; j++) {
                    normals.Add(Vector3.up);
                }

                CylQuad(v3, v2, v1, v4, tx - txInc, tx, 1, size.y / textureWorldScale.y, vertices, uvs, indices[0]);

                normals.Add(new Vector3(sin2, 0, cos2));
                normals.Add(new Vector3(sin, 0, cos));
                normals.Add(new Vector3(sin, 0, cos));
                normals.Add(new Vector3(sin2, 0, cos2));

                Tri(v4, v1, botCenter,
                    new Vector2((-sin2 + 1) * tw.x, (cos2 + 1) * tw.y),
                    new Vector2((-sin + 1) * tw.x, (cos + 1) * tw.y),
                    tw, 1, 1, vertices, uvs, indices[0]);

                for (int j = 0; j < 3; j++) {
                    normals.Add(Vector3.down);
                }
            }
        }

        private void Tri(Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3, float xscale, float yscale, List<Vector3> vertices, List<Vector2> uvs, List<int> indices) {
            xscale /= textureLocalScale.x;
            yscale /= textureLocalScale.y;

            vertices.Add(v1);
            uvs.Add(new Vector2(t1.x * xscale, t1.y * yscale));
            indices.Add(vertices.Count - 1);

            vertices.Add(v2);
            uvs.Add(new Vector2(t2.x * xscale, t2.y * yscale));
            indices.Add(vertices.Count - 1);

            vertices.Add(v3);
            uvs.Add(new Vector2(t3.x * xscale, t3.y * yscale));
            indices.Add(vertices.Count - 1);
        }

        private void CylQuad(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float t1x, float t2x, float xscale, float yscale, List<Vector3> vertices, List<Vector2> uvs, List<int> indices) {

            xscale /= textureLocalScale.x;
            yscale /= textureLocalScale.y;

            vertices.Add(v1);
            uvs.Add(new Vector2(t1x * xscale, yscale));
            indices.Add(vertices.Count - 1);

            vertices.Add(v2);
            uvs.Add(new Vector2(t2x * xscale, yscale));
            indices.Add(vertices.Count - 1);

            vertices.Add(v3);
            uvs.Add(new Vector2(t2x * xscale, 0));
            indices.Add(vertices.Count - 1);
            indices.Add(vertices.Count - 1);

            vertices.Add(v4);
            uvs.Add(new Vector2(t1x * xscale, 0));
            indices.Add(vertices.Count - 1);
            indices.Add(vertices.Count - 4);
        }

        private void Quad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float xscale, float yscale, List<Vector3> vertices, List<Vector2> uvs, List<int> indices) {
            xscale /= textureLocalScale.x;
            yscale /= textureLocalScale.y;

            vertices.Add(v1);
            uvs.Add(new Vector2(0, yscale));
            indices.Add(vertices.Count - 1);

            vertices.Add(v2);
            uvs.Add(new Vector2(xscale, yscale));
            indices.Add(vertices.Count - 1);

            vertices.Add(v3);
            uvs.Add(new Vector2(xscale, 0));
            indices.Add(vertices.Count - 1);
            indices.Add(vertices.Count - 1);

            vertices.Add(v4);
            uvs.Add(new Vector2(0, 0));
            indices.Add(vertices.Count - 1);
            indices.Add(vertices.Count - 4);

        }

        public enum Type {
            Box, Slant, Cyllinder, BlockStair, SlantStair, SeparateStair, Freeform
        }
    }
}