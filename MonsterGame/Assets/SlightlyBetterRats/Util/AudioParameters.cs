using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    /// <summary>
    /// Encapsulates many common parameters used when playing audio clips into a single field.
    /// </summary>
    [System.Serializable]
    public class AudioParameters {
        public AudioClip[] clips;

        [Range(0.0f, 1.0f)]
        public float volume = 1;
        public float pitch = 1;

        [Tooltip("Spatial blend as set on AudioSource. A value of 1 means fully spatialized, 0 means 2D.")]
        [Range(0.0f, 1.0f)]
        public float spaital = 0;
        public bool loop = false;

        [Tooltip("Cooldown between repeating the sound clip. If cooldownId is set, this is a global cooldown for all instances with the same ID.")]
        public float playCooldown = 0;

        [Tooltip("ID to use for cooldowns. Instances with the same ID share the same cooldown. If empty, cooldown is specific to this instance.")]
        public string cooldownId;

        private CooldownTimer playTimer;
        private static Dictionary<string, float> lastPlayTimes = new Dictionary<string, float>();

        private bool CanPlay() {
            if (playCooldown == 0) {
                return true;
            } else if (string.IsNullOrEmpty(cooldownId)) {
                if (playTimer == null) {
                    playTimer = new CooldownTimer(playCooldown);
                }
                return playTimer.Use();
            } else {
                if (!lastPlayTimes.ContainsKey(cooldownId) || Time.time - lastPlayTimes[cooldownId] > playCooldown) {
                    lastPlayTimes[cooldownId] = Time.time;
                    return true;
                } else {
                    return false;
                }
            }
        }

        public AudioSource PlayAtPoint(Vector3 point, Transform attach = null) {
            if (clips == null || clips.Length == 0) {
                Debug.LogError("Trying to play AudioInfo with no clips!");
                return null;
            }

            var clip = clips[Random.Range(0, clips.Length)];
            if (CanPlay()) {
                return Util.PlayClipAtPoint(clip, point, volume, spaital, pitch, loop, attach);
            } else {
                return null;
            }
        }

        public AudioSource Play() {
            var src = PlayAtPoint(Vector3.zero);
            src.spatialBlend = 0.0f;
            return src;
        }

        public static implicit operator bool(AudioParameters audio) {
            return audio.clips != null && audio.clips.Length > 0;
        }
    }
}