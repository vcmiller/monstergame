using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace SBR.Geometry {
    [Serializable]
    public class Vertex {
        public Vector3 position;

        public Vertex(Vector3 position) {
            this.position = position;
        }

        public Vertex() { }
    }

    [Serializable]
    public class Face {
        public List<int> vertices;

        public Face(IEnumerable<int> vertices) {
            this.vertices = new List<int>(vertices);
        }

        public Face(params int[] vertices) : this((IEnumerable<int>)vertices) { }

        public Vector3 GetNormal(FreeformMesh mesh) {
            if (vertices == null || vertices.Count < 3) {
                return Vector3.zero;
            }

            Vector3 normal = Vector3.zero;

            for (int i = 0; i < vertices.Count; i++) {
                int j = (i + 1) % vertices.Count;

                Vector3 v1 = mesh.vertices[vertices[i]].position;
                Vector3 v2 = mesh.vertices[vertices[j]].position;

                normal.x += (v1.y - v2.y) * (v1.z + v2.z);
                normal.y += (v1.z - v2.z) * (v1.x + v2.x);
                normal.z += (v1.x - v2.x) * (v1.y + v2.y);
            }

            return normal.normalized;
        }
        
        public Vector3[] GetPointsArray(Transform transform, FreeformMesh mesh) {
            List<Vector3> points = new List<Vector3>();

            foreach (var v in vertices) {
                Vector3 pos = mesh.vertices[v].position;
                pos = transform.TransformPoint(pos);
                points.Add(pos);
            }

            return points.ToArray();
        }

        public Vector3 GetCenter(FreeformMesh mesh) {
            return mesh.GetVerticesCenter(vertices);
        }

        public override bool Equals(object obj) {
            var f = obj as Face;

            if (f == null) {
                return false;
            }

            int s = f.vertices.IndexOf(vertices[0]);

            if (vertices.Count != f.vertices.Count || s < 0) {
                return false;
            }

            bool fwd = true, rev = true;
            for (int i = 0; i < vertices.Count; i++) {
                fwd &= vertices[i] == f.vertices[(s + i) % f.vertices.Count];
                rev &= vertices[i] == f.vertices[(s - i + f.vertices.Count) % f.vertices.Count];
            }
            
            return fwd || rev;
        }

        public override int GetHashCode() {
            int code = 0;

            foreach (int i in vertices) {
                code ^= i.GetHashCode();
            }

            return code;
        }

        public Face() { }
    }

    [Serializable]
    public class FreeformMesh {
        public List<Face> faces;
        public List<Vertex> vertices;

        [NonSerialized]
        private Dictionary<UnorderedPair<int>, List<int>> edgeFaces;
        [NonSerialized]
        private Dictionary<int, HashSet<int>> vertexNeighbors;
        [NonSerialized]
        private Dictionary<int, HashSet<int>> vertexFaces;
        [NonSerialized]
        private Dictionary<int, HashSet<UnorderedPair<int>>> faceEdges;

        [NonSerialized]
        private bool graphDirty = true;

        public FreeformMesh() {
            faces = new List<Face>();
            vertices = new List<Vertex>();
        }

        public void ForceGraphUpdate() {
            graphDirty = true;
        }

        private void UpdateGraph() {
            if (!graphDirty) return;
            edgeFaces = new Dictionary<UnorderedPair<int>, List<int>>();
            vertexNeighbors = new Dictionary<int, HashSet<int>>();
            vertexFaces = new Dictionary<int, HashSet<int>>();
            faceEdges = new Dictionary<int, HashSet<UnorderedPair<int>>>();

            for (int i = 0; i < faces.Count; i++) {
                Face face = faces[i];

                for (int j = 0; j < face.vertices.Count; j++) {
                    int next = face.vertices[(j + 1) % face.vertices.Count];
                    int prev = face.vertices[((j - 1) + face.vertices.Count) % face.vertices.Count];

                    int vert = face.vertices[j];
                    if (!vertexFaces.ContainsKey(vert)) {
                        vertexFaces[vert] = new HashSet<int>();
                    }

                    vertexFaces[vert].Add(i);

                    if (!vertexNeighbors.ContainsKey(vert)) {
                        vertexNeighbors[vert] = new HashSet<int>();
                    }

                    vertexNeighbors[vert].Add(prev);
                    vertexNeighbors[vert].Add(next);

                    UnorderedPair<int> edge1 = new UnorderedPair<int>(prev, vert);

                    if (!edgeFaces.ContainsKey(edge1)) {
                        edgeFaces[edge1] = new List<int>();
                    }

                    edgeFaces[edge1].Add(i);
                }
            }

            foreach (var pair in edgeFaces) {
                foreach (var face in pair.Value) {
                    if (!faceEdges.ContainsKey(face)) {
                        faceEdges[face] = new HashSet<UnorderedPair<int>>();
                    }

                    faceEdges[face].Add(pair.Key);
                }
            }

            graphDirty = false;
        }

        private bool EdgesAreContiguous(ICollection<UnorderedPair<int>> edges, out List<int> vertices) {
            UpdateGraph();
            var iter = edges.GetEnumerator();
            iter.MoveNext();
            var edge = iter.Current;
            iter.Dispose();

            int t1, t2;
            if (!GetEdgeOrder(edge, out t1, out t2)) {
                vertices = null;
                return false;
            }

            vertices = new List<int> {t1, t2};

            for (int i = 1; i < edges.Count; i++) {
                int prev = vertices[vertices.Count - 2];
                int cur = vertices[vertices.Count - 1];
                
                var curEdge = new UnorderedPair<int>(prev, cur);

                if (edgeFaces[curEdge].Count > 1) {
                    vertices = null;
                    return false;
                }

                bool found = false;
                foreach (var e in edges) {
                    if (e == curEdge || (e.t1 != cur && e.t2 != cur)) continue;
                    int next = e.Not(cur);

                    if (next == vertices[0]) {
                        if (i == edges.Count - 1) {
                            return true;
                        } else {
                            vertices = null;
                            return false;
                        }
                    }
                        
                    vertices.Add(next);
                    found = true;
                    break;
                }

                if (found) continue;
                vertices = null;
                return false;
            }

            vertices = null;
            return false;
        }

        public bool FacesAreConnected(IEnumerable<int> faces) {
            UpdateGraph();

            HashSet<int> faceSet = new HashSet<int>(faces);
            Queue<int> frontier = new Queue<int>();

            var en = faces.GetEnumerator();
            en.MoveNext();
            int first = en.Current;
            en.Dispose();

            frontier.Enqueue(first);
            faceSet.Remove(first);

            while (faceSet.Count > 0) {
                if (frontier.Count == 0) {
                    return false;
                }

                var front = frontier.Dequeue();
                var edges = faceEdges[front];

                foreach (var edge in edges) {
                    foreach (var f in edgeFaces[edge]) {
                        if (faceSet.Remove(f)) {
                            frontier.Enqueue(f);
                        }
                    }
                }
            }

            return true;
        }

        public void Clear() {
            vertices.Clear();
            faces.Clear();
        }

        public int AddVertex(Vector3 vertex) {
            if (vertices == null) {
                vertices = new List<Vertex>();
            }

            graphDirty = true;

            vertices.Add(new Vertex(vertex));
            return vertices.Count - 1;
        }

        public bool AddFace(params int[] verts) {
            return AddFace((IEnumerable<int>) verts);
        }

        public bool AddFace(IEnumerable<int> verts) {
            if (faces == null) {
                faces = new List<Face>();
            }

            Face newFace = new Face(verts);

            if (!faces.Contains(newFace)) {
                faces.Add(new Face(verts));
                graphDirty = true;
                return true;
            } else {
                return false;
            }
        }

        private bool GetEdgeOrder(UnorderedPair<int> edge, out int t1, out int t2) {
            UpdateGraph();

            var n = edgeFaces[edge];
            
            if (n.Count != 1) {
                t1 = -1;
                t2 = -1;
                return false;
            }

            Face f = faces[n[0]];

            int ind1 = f.vertices.IndexOf(edge.t1);
            int ind2 = f.vertices.IndexOf(edge.t2);

            if (ind2 == (ind1 + 1) % f.vertices.Count) {
                t1 = edge.t2;
                t2 = edge.t1;
                return true;
            } else if (ind1 == (ind2 + 1) % f.vertices.Count) {
                t1 = edge.t1;
                t2 = edge.t2;
                return true;
            } else {
                t1 = -1;
                t2 = -1;
                return false;
            }
        }

        public bool AddFace(ICollection<UnorderedPair<int>> edges) {
            List<int> vertLoop;
            
            if (edges.Count > 2 && EdgesAreContiguous(edges, out vertLoop)) {
                return AddFace(vertLoop);
            } else if (edges.Count == 2) {
                var iter = edges.GetEnumerator();
                iter.MoveNext();
                var e1 = iter.Current;
                iter.MoveNext();
                var e2 = iter.Current;
                iter.Dispose();
                
                int v1, v2, v3, v4;
                if (GetEdgeOrder(e1, out v1, out v2) && GetEdgeOrder(e2, out v3, out v4)) {
                    if (v1 == v4) {
                        return AddFace(v1, v2, v3);
                    } else if (v2 == v3) {
                        return AddFace(v1, v2, v4);
                    } else {
                        return AddFace(v1, v2, v3, v4);
                    }
                }
            }

            return false;
        }

        public Vector3 GetVerticesCenter(ICollection<int> verts) {
            Vector3 c = verts.Aggregate(Vector3.zero, (current, index) => current + vertices[index].position);

            return c / verts.Count;
        }

        public Vector3 GetEdgeCenter(UnorderedPair<int> edge) {
            return (vertices[edge.t1].position + vertices[edge.t2].position) / 2.0f;
        }

        public Vector3 GetFacesNormal(ICollection<int> faces) {
            Vector3 n = faces.Aggregate(Vector3.zero, (current, index) => current + this.faces[index].GetNormal(this));

            
            if (n.sqrMagnitude < 0.01f) {
                var iter = faces.GetEnumerator();
                iter.MoveNext();
                int c = iter.Current;
                iter.Dispose();
                
                return this.faces[c].GetNormal(this);
            } else {
                return n.normalized;
            }

        }

        public HashSet<int> GetUniqueVertexList(IEnumerable<UnorderedPair<int>> edges) {
            HashSet<int> result = new HashSet<int>();

            foreach (var edge in edges) {
                result.Add(edge.t1);
                result.Add(edge.t2);
            }

            return result;
        }

        public HashSet<int> GetUniqueVertexList(IEnumerable<int> faces) {
            HashSet<int> result = new HashSet<int>();

            foreach (int face in faces) {
                foreach (int vertex in this.faces[face].vertices) {
                    result.Add(vertex);
                }
            }

            return result;
        }

        public List<UnorderedPair<int>> GetEdges() {
            List<UnorderedPair<int>> edges = new List<UnorderedPair<int>>();

            foreach (var f in faces) {
                for (int i = 0; i < f.vertices.Count; i++) {
                    int j = (i + 1) % f.vertices.Count;

                    UnorderedPair<int> edge = new UnorderedPair<int>(f.vertices[i], f.vertices[j]);

                    if (!edges.Contains(edge)) {
                        edges.Add(edge);
                    }
                }
            }

            return edges;
        }

        private bool IsNotInnerEdge(UnorderedPair<int> edge, ICollection<int> faceIndices) {
            foreach (var neighbor in edgeFaces[edge]) {
                if (!faceIndices.Contains(neighbor)) {
                    return true;
                }
            }

            return false;
        }

        private bool IsOuterEdge(UnorderedPair<int> edge, ICollection<int> faceIndices) {
            bool outer = false, inner = false;

            foreach (var neighbor in edgeFaces[edge]) {
                if (!faceIndices.Contains(neighbor)) {
                    outer = true;
                } else {
                    inner = true;
                }

                if (outer && inner) {
                    return true;
                }
            }

            return false;
        }

        private UnorderedPair<int> FindOuterEdge(ICollection<int> faceIndices) {

            foreach (int face in faceIndices) {
                foreach (var edge in faceEdges[face]) {
                    if (IsNotInnerEdge(edge, faceIndices)) {
                        return edge;
                    }
                }
            }

            return new UnorderedPair<int>(-1, -1);
        }

        public void DeleteFaces(IEnumerable<int> f) {
            HashSet<Face> toRemove = new HashSet<Face>();
            HashSet<int> affectedVerts = new HashSet<int>();

            foreach (int face in f) {
                toRemove.Add(faces[face]);
                
                foreach (int vert in faces[face].vertices) {
                    affectedVerts.Add(vert);
                }
            }

            faces.RemoveAll(face => toRemove.Contains(face));

            graphDirty = true;


            DeleteVertices(affectedVerts, v => !vertexNeighbors.ContainsKey(v) || vertexNeighbors[v].Count == 0);
        }

        public void DeleteEdges(IEnumerable<UnorderedPair<int>> edges) {
            UpdateGraph();
            var affectedVertices = GetUniqueVertexList(edges);

            foreach (var edge in edges) {
                var neighbors = edgeFaces[edge];

                // We only delete edges that have exactly two neighboring faces, because we need to merge those faces.
                if (neighbors.Count != 2) continue;
                int f1 = neighbors[0];
                int f2 = neighbors[1];

                int delFace = f1;
                int keepFace = f2;

                int start = faces[delFace].vertices.IndexOf(edge.t1);
                int next = (start + 1) % faces[delFace].vertices.Count;

                if (faces[delFace].vertices[next] == edge.t2) {
                    delFace = f2;
                    keepFace = f1;

                    start = faces[delFace].vertices.IndexOf(edge.t1);
                }

                List<int> transfer = new List<int>();

                var delVerts = faces[delFace].vertices;
                var keepVerts = faces[keepFace].vertices;

                for (int i = (start + 1) % delVerts.Count; delVerts[i] != edge.t2; i = (i + 1) % delVerts.Count) {
                    transfer.Add(delVerts[i]);
                }

                int pos = keepVerts.IndexOf(edge.t1);
                keepVerts.InsertRange(pos + 1, transfer);

                // Mark face for deletion. This is easier than remembering the indices of marked faces.
                faces[delFace].vertices = null;

                // Do some cleanup. This allows us to delete a list of edges without completely rebuilding the graph each time.
                edgeFaces.Remove(edge);

                foreach (var ef in edgeFaces) {
                    int i = ef.Value.IndexOf(delFace);
                    if (i >= 0) {
                        ef.Value[i] = keepFace;
                    }
                }
            }

            faces.RemoveAll(face => face.vertices == null);

            graphDirty = true;
            DeleteVertices(affectedVertices, v => !vertexNeighbors.ContainsKey(v) || vertexNeighbors[v].Count == 2);
        }

        private void DeleteVertices(IEnumerable<int> affectedVertices, Predicate<int> pred) {
            UpdateGraph();

            List<int> deleteVerts = new List<int>();
            
            foreach (int vert in affectedVertices) {

                if (pred(vert)) {
                    deleteVerts.Add(vert);
                    if (vertexFaces.ContainsKey(vert)) {
                        var vf = vertexFaces[vert];

                        foreach (int face in vf) {
                            faces[face].vertices.Remove(vert);
                        }
                    }
                }
            }
            
            // Delete vertices that are no longer needed.
            for (int i = 0; i < deleteVerts.Count; i++) {
                vertices.RemoveAt(deleteVerts[i]);

                foreach (var face in faces) {
                    for (int j = 0; j < face.vertices.Count; j++) {
                        if (face.vertices[j] > deleteVerts[i]) {
                            face.vertices[j]--;
                        }
                    }
                }

                for (int j = i + 1; j < deleteVerts.Count; j++) {
                    if (deleteVerts[j] > deleteVerts[i]) {
                        deleteVerts[j]--;
                    }
                }
            }

            graphDirty = true;
        }

        public int WeldVertices(ICollection<int> selectedVertices) {
            int keep = AddVertex(GetVerticesCenter(selectedVertices));

            foreach (var face in faces) {
                for (int i = 0; i < face.vertices.Count; i++) {
                    if (selectedVertices.Contains(face.vertices[i])) {
                        face.vertices[i] = keep;
                    }
                }
            }

            DeleteVertices(selectedVertices, vert => true);
            graphDirty = true;

            return vertices.Count - 1;
        }

        public bool InsertLoop(ICollection<UnorderedPair<int>> edges) {
            if (edges.Count != 1) {
                return false;
            }
            
            UpdateGraph();

            var iter = edges.GetEnumerator();
            iter.MoveNext();
            var edge = iter.Current;
            iter.Dispose();

            List<UnorderedPair<int>> edgeLoop = new List<UnorderedPair<int>> {edge};
            List<int> faceLoop = new List<int> {edgeFaces[edge][0]};

            bool oneSide = false;
            
            while (true) {
                int cnt = faceLoop.Count;
                int cur = faceLoop[cnt - 1];

                foreach (var e in faceEdges[cur]) {
                    if (!e.Adjacent(edgeLoop[edgeLoop.Count - 1])) {
                        edgeLoop.Add(e);

                        if (e == edgeLoop[0]) {
                            break;
                        }

                        var ef = edgeFaces[e];

                        foreach (int f in ef) {
                            if (f != cur && faceEdges[f].Count == 4 && f != faceLoop[0]) {
                                faceLoop.Add(f);
                                break;
                            }
                        }

                        break;
                    }
                }

                if (faceLoop.Count == cnt || edgeLoop[edgeLoop.Count - 1] == edgeLoop[0]) {
                    oneSide = edgeLoop[edgeLoop.Count - 1] != edgeLoop[0];
                    break;
                }
            }

            if (oneSide) {
                while (true) {
                    int cnt = faceLoop.Count;
                    var cur = edgeLoop[0];

                    foreach (int f in edgeFaces[cur]) {
                        if (f != faceLoop[0] && faceEdges[f].Count == 4) {
                            faceLoop.Insert(0, f);

                            foreach (var e in faceEdges[f]) {
                                if (!e.Adjacent(cur)) {
                                    edgeLoop.Insert(0, e);
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    if (faceLoop.Count == cnt) {
                        break;
                    }
                }
            }

            List<int> addedVerts = new List<int>();
            
            for (int i = 0; i < faceLoop.Count; i++) {
                Face face = faces[faceLoop[i]];
                UnorderedPair<int> edge1 = edgeLoop[i];
                UnorderedPair<int> edge2 = edgeLoop[i + 1];

                int v1, v2;

                if (i == 0) {
                    v1 = AddVertex(GetEdgeCenter(edge1));
                    addedVerts.Add(v1);
                } else {
                    v1 = addedVerts[addedVerts.Count - 1];
                }

                if (i == faceLoop.Count - 1 && edge2 == edgeLoop[0]) {
                    v2 = addedVerts[0];
                } else {
                    v2 = AddVertex(GetEdgeCenter(edge2));
                    addedVerts.Add(v2);
                }

                Face newFace = new Face();
                newFace.vertices = new List<int>();
                
                InsertVertex(face, edge1, v1);
                InsertVertex(face, edge2, v2);

                int ind1 = face.vertices.IndexOf(v1);
                int ind2 = face.vertices.IndexOf(v2);

                if (ind1 == -1 || ind2 == -1) {
                    Debug.LogError("Could not insert vertex");
                    continue;
                }

                for (int j = ind1; j != (ind2 + 1) % face.vertices.Count; j = (j + 1) % face.vertices.Count) {
                    newFace.vertices.Add(face.vertices[j]);

                    if (j != ind1 && j != ind2) {
                        face.vertices[j] = -1;
                    }
                }

                face.vertices.RemoveAll(v => v < 0);
                faces.Add(newFace);
            }

            graphDirty = true;
            return true;
        }

        private void InsertVertex(Face face, UnorderedPair<int> edge, int vertex) {
            int i1 = face.vertices.IndexOf(edge.t1);
            int i2 = face.vertices.IndexOf(edge.t2);

            if (i2 == (i1 + 1) % face.vertices.Count) {
                face.vertices.Insert(i1 + 1, vertex);
                graphDirty = true;
            } else if (i1 == (i2 + 1) % face.vertices.Count) {
                face.vertices.Insert(i2 + 1, vertex);
                graphDirty = true;
            } else {
                Debug.Log("Vertex not inserted!");
            }
        }

        public bool ExtrudeFaces(HashSet<int> selected) {
            if (!FacesAreConnected(selected)) {
                Debug.LogError("Cannot Extrude Disconnected Faces");
                return false;
            }

            var startEdge = FindOuterEdge(selected);

            if (startEdge.t1 == -1) {
                Debug.LogError("Cannot Find Outer Edge");
                return false;
            }

            // Make sure the first two vertices are in the right order by comparing with a selected face
            foreach (var f in edgeFaces[startEdge]) {
                if (!selected.Contains(f)) continue;
                int t1index = faces[f].vertices.IndexOf(startEdge.t1);
                int t2index = faces[f].vertices.IndexOf(startEdge.t2);

                if (t1index == (t2index + 1) % faces[f].vertices.Count) {
                    // Edge is in the wrong order. Fix it (this doesn't modify the orignal edge).
                    int temp = startEdge.t2;
                    startEdge.t2 = startEdge.t1;
                    startEdge.t1 = temp;
                }

                break;
            }

            Vector3 normal = GetFacesNormal(selected);
            List<int> edgeLoop = new List<int> {startEdge.t1, startEdge.t2};

            while (true) {
                int cur = edgeLoop[edgeLoop.Count - 1];
                int prev = edgeLoop[edgeLoop.Count - 2];
                int next = -1;

                foreach (int cand in vertexNeighbors[cur]) {
                    var edge = new UnorderedPair<int>(cur, cand);

                    if (cand == prev || !IsOuterEdge(edge, selected)) continue;
                    next = cand;
                    break;
                }

                if (next != -1) {
                    if (next == edgeLoop[0]) {
                        break;
                    }

                    edgeLoop.Add(next);
                } else {
                    Debug.LogError("Edge Loop Not Found");
                    return false;
                }
            }

            Face lastCreatedFace = null;

            int v1 = -1, v2 = -1;

            foreach (var v in edgeLoop) {
                int newVert = AddVertex(vertices[v].position);

                if (lastCreatedFace != null) {
                    lastCreatedFace.vertices.Add(v);
                    lastCreatedFace.vertices.Add(newVert);
                } else {
                    v1 = v;
                    v2 = newVert;
                }

                lastCreatedFace = new Face(newVert, v);
                faces.Add(lastCreatedFace);

                foreach (int face in selected) {
                    for (int k = 0; k < faces[face].vertices.Count; k++) {
                        if (faces[face].vertices[k] == v) {
                            faces[face].vertices[k] = newVert;
                        }
                    }
                }
            }

            lastCreatedFace.vertices.Add(v1);
            lastCreatedFace.vertices.Add(v2);

            foreach (int v in GetUniqueVertexList(selected)) {
                vertices[v].position += normal;
            }

            graphDirty = true;

            return true;
        }

        public void SelectEdgeLoop(ICollection<UnorderedPair<int>> edges, UnorderedPair<int> start, bool recurses = true) {
            UpdateGraph();
            edges.Add(start);

            int prev = start.t1;
            int cur = start.t2;
            while (true) {
                UnorderedPair<int> curEdge = new UnorderedPair<int>(prev, cur);

                if (vertexNeighbors[cur].Count != 4) {
                    break;
                }

                bool found = false;
                foreach (var v in vertexNeighbors[cur]) {
                    var e = new UnorderedPair<int>(cur, v);

                    bool ok = true;
                    foreach (int f in edgeFaces[e]) {
                        if (edgeFaces[curEdge].Contains(f)) {
                            ok = false;
                            break;
                        }
                    }

                    if (ok) {
                        found = true;
                        edges.Add(e);
                        prev = cur;
                        cur = e.Not(cur);
                        break;
                    }
                }

                if (!found || cur == start.t1) {
                    break;
                }
            }

            if (recurses && cur != start.t1) {
                SelectEdgeLoop(edges, new UnorderedPair<int>(start.t2, start.t1), false);
            }
        }

        public void Select(Transform transform, Vector2 mousePos, Camera camera, out int selVertex, out int selFace, out UnorderedPair<int> selEdge, float radius = 10, float bump = 0.2f) {
            selVertex = -1;
            selFace = -1;
            selEdge = new UnorderedPair<int>(-1, -1);

            float minDist = Mathf.Infinity;

            Ray ray = camera.ScreenPointToRay(mousePos);

            ray.origin = transform.InverseTransformPoint(ray.origin);
            ray.direction = transform.InverseTransformDirection(ray.direction);

            for (int v = 0; v < vertices.Count; v++) {
                Vertex vert = vertices[v];

                Vector3 world = transform.TransformPoint(vert.position);
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z > 0) {
                    screen.z = 0;
                    float mouseDist2 = Vector2.SqrMagnitude((Vector2)screen - mousePos);

                    float camDist = Vector3.Distance(camera.transform.position, world) - bump * 3;
                    if (mouseDist2 < radius * radius && camDist < minDist) {
                        selVertex = v;
                        minDist = camDist;
                    }
                }
            }

            foreach (var e in GetEdges()) {
                Vertex vert1 = vertices[e.t1];
                Vertex vert2 = vertices[e.t2];

                Vector3 v1 = vert1.position;
                Vector3 v2 = vert2.position;
                Vector3 segment = v2 - v1;
               
                Vector3 c = Vector3.Cross(camera.transform.forward, segment);
                Vector3 n = Vector3.Cross(c, segment);
                Plane plane = new Plane(n, v1);
                float camDist;

                if (plane.Raycast(ray, out camDist) && camDist - bump < minDist) {
                    Vector3 hitPos = ray.GetPoint(camDist);
                    Vector3 hitProj = Math3D.ProjectPointOnLineSegment(v1, v2, hitPos);

                    Vector3 projScreen = camera.WorldToScreenPoint(transform.TransformPoint(hitProj));
                    if (projScreen.z > 0 && Vector2.Distance(projScreen, mousePos) < radius * 0.6f) {
                        selEdge = e;
                        selVertex = -1;
                        minDist = camDist - bump;
                    }
                }
            }

            for (int f = 0; f < faces.Count; f++) {
                Face face = faces[f];
                Vector3 normal = face.GetNormal(this);
                Plane plane = new Plane(normal, vertices[face.vertices[0]].position);

                float dist;
                if (plane.Raycast(ray, out dist) && dist < minDist) {
                    Vector3 hitPos = ray.GetPoint(dist);

                    bool inside = true;

                    for (int i = 0; i < face.vertices.Count; i++) {

                        int j = (i + 1) % face.vertices.Count;

                        Vector3 v1 = vertices[face.vertices[i]].position;
                        Vector3 v2 = vertices[face.vertices[j]].position;

                        Vector3 edge = v2 - v1;
                        Vector3 toHit = hitPos - v1;
                        Vector3 cross = Vector3.Cross(edge, toHit);

                        if (Vector3.Dot(cross, normal) < 0) {
                            inside = false;
                            break;
                        }
                    }

                    if (inside) {
                        selVertex = -1;
                        selEdge.t1 = -1;
                        selEdge.t2 = -1;
                        selFace = f;
                        minDist = dist;
                    }
                }
            }
        }

        public void GetMeshData(List<Vector3> vertexList, List<Vector2> uvList, List<int> indexList, Vector3 textureWorldScale, Vector2 textureLocalScale) {
            foreach (Face face in faces) {
                Vector3 normal = face.GetNormal(this);

                float nx = Mathf.Abs(normal.x);
                float ny = Mathf.Abs(normal.y);
                float nz = Mathf.Abs(normal.z);

                int projMode;
                Vector2 mult = new Vector2(1, 1);

                if (nx >= ny && nx >= nz) {
                    projMode = 0;
                    mult.x = Mathf.Sign(normal.x);
                } else if (ny >= nx && ny >= nz) {
                    projMode = 1;
                    mult.y = Mathf.Sign(normal.y);
                } else {
                    projMode = 2;
                    mult.x = -Mathf.Sign(normal.z);
                }

                mult.Scale(textureLocalScale);

                int startIndex = vertexList.Count;

                foreach (int index in face.vertices) {
                    Vector3 vertex = vertices[index].position;

                    vertexList.Add(vertex);

                    Vector3 vMul = vertex;
                    vMul.Scale(textureWorldScale);

                    if (projMode == 0) {
                        uvList.Add(new Vector2(vMul.z * mult.x, vMul.y * mult.y));
                    } else if (projMode == 1) {
                        uvList.Add(new Vector2(vMul.x * mult.x, vMul.z * mult.y));
                    } else {
                        uvList.Add(new Vector2(vMul.x * mult.x, vMul.y * mult.y));
                    }
                }

                for (int i = 1; i < face.vertices.Count - 1; i++) {
                    indexList.Add(startIndex);
                    indexList.Add(startIndex + i);
                    indexList.Add(startIndex + i + 1);
                }
            }
        }
    }
}