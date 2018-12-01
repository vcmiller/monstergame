using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Some code in this class is adapted from Cinemachine

namespace SBR {
    [System.Serializable]
    public class SplineData {
        [System.Serializable]
        public class Point {
            public Vector3 position;
            public Vector3 tangent;
            public float roll;
        }

        public bool closed;

        public Point[] points = new Point[0];
        private float[] samples = new float[100];
        private bool samplesDirty = true;
        private float _length;

        public float min { get { return 0; } }
        public float max { get { return Mathf.Max(0, closed ? points.Length : points.Length - 1); } }

        public int sampleCount {
            get { return samples.Length; }
            set {
                if (value != samples.Length) {
                    samples = new float[value];
                    InvalidateSamples();
                }
            }
        }

        public float length {
            get {
                RefreshSamples();
                return _length;
            }
        }

        public void InvalidateSamples() {
            samplesDirty = true;
        }

        private void RefreshSamples() {
            if (!samplesDirty) return;

            Vector3 lastP = GetPointNonUniform(0);
            _length = 0;
            samples[0] = 0;
            for (int i = 1; i < samples.Length; i++) {
                Vector3 curP = GetPointNonUniform(i / (samples.Length - 1.0f));
                _length += Vector3.Distance(curP, lastP);
                samples[i] = _length;
                lastP = curP;
            }

            for (int i = 0; i < samples.Length; i++) {
                samples[i] /= _length;
            }

            samplesDirty = false;
        }

        private float Normalize(float pos) {
            if (pos > 1 || pos < -1) {
                pos %= 1;
            }

            if (pos < 0) {
                pos++;
            }

            return pos;
        }

        private float ToNonUniform(float pos) {
            pos = Normalize(pos);
            RefreshSamples();

            for (int i = 0; i < samples.Length - 1; i++) {
                if (samples[i] <= pos && samples[i + 1] >= pos) {
                    float f = (pos - samples[i]) / (samples[i + 1] - samples[i]);
                    return Mathf.Lerp(i, i + 1, f) / (samples.Length - 1.0f);
                }
            }

            return Mathf.Clamp01(pos);
        }

        private float ScaleToLength(float pos) {
            if (closed) {
                return pos * points.Length;
            } else {
                return pos * Mathf.Max(0, points.Length - 1);
            }
        }

        private float GetSegment(float pos, out int indexA, out int indexB) {
            pos = ScaleToLength(Normalize(pos));
            int rounded = Mathf.RoundToInt(pos);
            if (Mathf.Abs(pos - rounded) < 0.0001f) {
                indexA = indexB = (rounded == points.Length) ? 0 : rounded;
            } else {
                indexA = Mathf.FloorToInt(pos);
                indexB = Mathf.CeilToInt(pos);
                if (indexB >= points.Length) {
                    indexB = 0;
                }
            }
            return pos;
        }
        
        public Quaternion GetPointRotation(int index) {
            Quaternion q = Quaternion.identity;

            Vector3 lastFwd = Vector3.forward;

            for (int i = 0; i <= index; i++) {
                var fwd = points[i].tangent;
                Quaternion temp = Quaternion.FromToRotation(lastFwd, fwd);
                q = temp * q;
                lastFwd = fwd;
            }

            return q;
        }

        public Vector3 GetPointNonUniform(float pos) {
            if (points.Length == 0)
                return Vector3.zero;
            else {
                int indexA, indexB;
                pos = GetSegment(pos, out indexA, out indexB);
                if (indexA == indexB) {
                    return points[indexA].position;
                } else {
                    var ptA = points[indexA];
                    var ptB = points[indexB];
                    float t = pos - indexA;
                    float d = 1f - t;
                    Vector3 ctrl1 = ptA.position + ptA.tangent;
                    Vector3 ctrl2 = ptB.position - ptB.tangent;
                    return d * d * d * ptA.position + 3f * d * d * t * ctrl1
                        + 3f * d * t * t * ctrl2 + t * t * t * ptB.position;
                }
            }
        }

        public Vector3 GetPoint(float pos) {
            return GetPointNonUniform(ToNonUniform(pos));
        }

        public Vector3 GetTangentNonUniform(float pos) {
            if (points.Length == 0)
                return Vector3.forward;
            else {
                int indexA, indexB;
                pos = GetSegment(pos, out indexA, out indexB);
                if (indexA == indexB) {
                    return points[indexA].tangent;
                } else {
                    Point ptA = points[indexA];
                    Point ptB = points[indexB];
                    float t = pos - indexA;
                    Vector3 ctrl1 = ptA.position + ptA.tangent;
                    Vector3 ctrl2 = ptB.position - ptB.tangent;
                    return Vector3.Normalize((-3f * ptA.position + 9f * ctrl1 - 9f * ctrl2 + 3f * ptB.position) * t * t
                        + (6f * ptA.position - 12f * ctrl1 + 6f * ctrl2) * t
                        - 3f * ptA.position + 3f * ctrl1);
                }
            }
        }

        public Vector3 GetTangent(float pos) {
            return GetTangentNonUniform(ToNonUniform(pos));
        }

        public float GetRollNonUniform(float pos) {
            if (points.Length == 0) {
                return 0;
            } else {
                int indexA, indexB;
                pos = GetSegment(pos, out indexA, out indexB);
                if (indexA == indexB) {
                    return points[indexA].roll;
                } else {
                    float rollA = points[indexA].roll;
                    float rollB = points[indexB].roll;
                    if (indexB == 0) {
                        rollA = rollA % 360;
                        rollB = rollB % 360;
                    }
                    return Mathf.Lerp(rollA, rollB, pos - indexA);
                }
            }
        }

        public float GetRoll(float pos) {
            return GetRollNonUniform(ToNonUniform(pos));
        }

        public Quaternion GetRotationNonUniform(float pos) {
            if (points.Length == 0) {
                return Quaternion.identity;
            } else {
                int indexA, indexB;
                GetSegment(pos, out indexA, out indexB);
                float roll = GetRollNonUniform(pos);
                Vector3 fwd = GetTangentNonUniform(pos);

                if (fwd.sqrMagnitude > 0.0001f) {
                    Quaternion from = GetPointRotation(indexA);
                    Quaternion q = Quaternion.FromToRotation(points[indexA].tangent, fwd) * from;
                    return q * Quaternion.AngleAxis(roll, Vector3.forward);
                } else {
                    return Quaternion.identity;
                }
            }
        }

        public Quaternion GetRotation(float pos) {
            return GetRotationNonUniform(ToNonUniform(pos));
        }
    }
}