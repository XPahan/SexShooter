using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Loud one-shot 3D SFX for enemy events.
    /// High minDistance keeps full volume across typical combat distances.
    /// </summary>
    public static class EnemySfx
    {
        private const float MinDistance = 40f;
        private const float MaxDistance = 250f;

        public static void Play3D(AudioClip clip, Vector3 worldPos, float volume = 1f)
        {
            if (clip == null) return;

            var go = new GameObject("EnemySFX_" + clip.name);
            go.transform.position = worldPos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 1f;
            src.volume = Mathf.Clamp01(volume);
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = MinDistance;
            src.maxDistance = MaxDistance;
            src.dopplerLevel = 0f;
            src.spread = 0f;
            src.Play();
            Object.Destroy(go, clip.length + 0.2f);
        }
    }
}
