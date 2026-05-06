using DG.Tweening;
using UnityEngine;

namespace OSK
{
    [System.Serializable]
    public class PlayingSound
    {
        public SoundData SoundData;
        public AudioSource AudioSource;
        public bool IsPaused;
        public float RawVolume = 1f;

        /// <summary>
        /// Per-instance volume tween. Fixes the shared _tweener bug 
        /// where multiple simultaneous fades cancelled each other.
        /// </summary>
        [System.NonSerialized] public Tweener VolumeTween;

        /// <summary>
        /// Null-safe IsPlaying check. Prevents NullRef when AudioSource is already despawned.
        /// </summary>
        public bool IsPlaying => AudioSource != null && AudioSource.isPlaying;

        /// <summary>
        /// Kill the active volume tween if any.
        /// </summary>
        public void KillTween()
        {
            if (VolumeTween != null && VolumeTween.IsActive())
            {
                VolumeTween.Kill();
            }
            VolumeTween = null;
        }

        /// <summary>
        /// Reset all fields for object pooling reuse.
        /// </summary>
        public void Reset()
        {
            KillTween();
            SoundData = null;
            AudioSource = null;
            IsPaused = false;
            RawVolume = 1f;
        }
    }
}