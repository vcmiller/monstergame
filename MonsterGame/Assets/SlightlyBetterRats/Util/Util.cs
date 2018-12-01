using UnityEngine;
using System.Collections;


namespace SBR {
    public static class Util {
        public static AudioSource PlayClipAtPoint(AudioClip clip, Vector3 point, float volume = 1, float spatial = 1, float pitch = 1, bool loop = false, Transform attach = null) {
            if (clip == null) {
                return null;
            }

            GameObject obj = new GameObject();
            obj.name = "One shot audio (SBR)";
            obj.transform.parent = attach;
            obj.transform.position = point;

            var src = obj.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = loop;
            src.spatialBlend = spatial;
            src.volume = volume;
            src.pitch = pitch;
            src.Play();

            if (!loop) {
                Object.Destroy(obj, clip.length);
            }

            return src;
        }

        public static void DrawDebugBounds(Bounds bounds, Color color) {
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z), new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), color);
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), new Vector3(bounds.max.x, bounds.max.y, bounds.min.z), color);
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z), new Vector3(bounds.max.x, bounds.max.y, bounds.max.z), color);
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), color);
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z), new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), color);
            Debug.DrawLine(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), new Vector3(bounds.max.x, bounds.max.y, bounds.min.z), color);
            Debug.DrawLine(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), new Vector3(bounds.max.x, bounds.max.y, bounds.max.z), color);
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), new Vector3(bounds.min.x, bounds.max.y, bounds.max.z), color);
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z), new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), color);
            Debug.DrawLine(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), color);
            Debug.DrawLine(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z), new Vector3(bounds.max.x, bounds.max.y, bounds.max.z), color);
            Debug.DrawLine(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), new Vector3(bounds.min.x, bounds.max.y, bounds.max.z), color);
        }

        public static void GetPoints(this CapsuleCollider capsule, out Vector3 point1, out Vector3 point2, out float radius, out float height) {
            Vector3 capsuleCenter = capsule.transform.TransformPoint(capsule.center);
            Vector3 capsuleUp;
            float scaleY;
            float scaleXZ;

            if (capsule.direction == 0) {
                capsuleUp = capsule.transform.right;
                scaleY = capsule.transform.lossyScale.x;
                scaleXZ = Mathf.Max(Mathf.Abs(capsule.transform.localScale.y), Mathf.Abs(capsule.transform.localScale.z));
            } else if (capsule.direction == 1) {
                capsuleUp = capsule.transform.up;
                scaleY = capsule.transform.lossyScale.y;
                scaleXZ = Mathf.Max(Mathf.Abs(capsule.transform.localScale.x), Mathf.Abs(capsule.transform.localScale.z));
            } else {
                capsuleUp = capsule.transform.forward;
                scaleY = capsule.transform.lossyScale.z;
                scaleXZ = Mathf.Max(Mathf.Abs(capsule.transform.localScale.x), Mathf.Abs(capsule.transform.localScale.y));
            }

            radius = scaleXZ * capsule.radius;

            height = Mathf.Max(scaleY * capsule.height, radius * 2);

            float h = height / 2 - radius;

            point1 = capsuleCenter + capsuleUp * h;
            point2 = capsuleCenter - capsuleUp * h;
        }
    }
}