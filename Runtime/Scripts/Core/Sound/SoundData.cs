using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace OSK
{
    public enum PlaybackMode
    {
        Single = 0,    // Always play the primary audioClip
        Sequence = 1,  // Play clips in order, cycling
        Random = 2,    // Pick a random clip each time
    }

    [Serializable]
    public class SoundData
    {
        public string group = "Default";
        public string id = "";
        public AudioClip audioClip;
        public SoundType type = SoundType.SFX;
        public AudioMixerGroup mixerGroup;        
        [Range(0, 1)] public float volume = 1;
        public MinMaxFloat pitch =  new MinMaxFloat(1, 1);

        // Enhanced configuration fields
        public bool loop = false;
        [Range(0, 256)] public int priority = 128;
        
        [Header("Spatial Settings")]
        [Range(0, 1)] public float spatialBlend = 0; // 0 = 2D, 1 = 3D
        [Min(0)] public int minDistance = 1;
        [Min(0)] public int maxDistance = 500;

        // Multi-clip settings (BroAudio style)
        [Header("Multi-Clip")]
        public PlaybackMode playbackMode = PlaybackMode.Single;
        public List<AudioClip> clips = new List<AudioClip>();
        [NonSerialized] private int _sequenceIndex = 0;

        /// <summary>
        /// Get the next AudioClip based on playbackMode.
        /// Single: returns audioClip. Sequence: cycles through clips. Random: picks randomly.
        /// Falls back to audioClip if clips list is empty.
        /// </summary>
        public AudioClip GetNextClip()
        {
            if (playbackMode == PlaybackMode.Single || clips == null || clips.Count == 0)
                return audioClip;

            switch (playbackMode)
            {
                case PlaybackMode.Sequence:
                    if (_sequenceIndex >= clips.Count) _sequenceIndex = 0;
                    var seqClip = clips[_sequenceIndex];
                    _sequenceIndex++;
                    return seqClip != null ? seqClip : audioClip;

                case PlaybackMode.Random:
                    var rndClip = clips[UnityEngine.Random.Range(0, clips.Count)];
                    return rndClip != null ? rndClip : audioClip;

                default:
                    return audioClip;
            }
        }

        /// <summary>Reset sequence index (e.g. when stopping)</summary>
        public void ResetSequence() => _sequenceIndex = 0;

#if UNITY_EDITOR
        public void Play(MinMaxFloat pitch)
        {
            var clip = GetNextClip();
            if (clip == null)
            {
                MyLogger.LogWarning("AudioClip is null.");
                return;
            }
            
            if (pitch != null)
            {
                SetPitch(pitch);
            }
            
            EditorAudioHelper.PlayClip(clip);
        } 

        public void Stop()
        {
            if (audioClip != null) EditorAudioHelper.StopClip(audioClip);
            if (clips != null)
            {
                for (int i = 0; i < clips.Count; i++)
                    if (clips[i] != null) EditorAudioHelper.StopClip(clips[i]);
            }
            ResetSequence();
        }
        
        public void SetVolume(float volume)
        {
            this.volume = volume;
            EditorAudioHelper.SetVolume(audioClip, volume);
        }
        
        public void SetPitch(MinMaxFloat  pitch)
        {
            this.pitch = pitch;
            EditorAudioHelper.SetPitch(audioClip, pitch.RandomValue);
        } 

        public bool IsPlaying() => EditorAudioHelper.IsClipPlaying(audioClip);

        public void UpdateId()
        {
            if (audioClip != null) id = audioClip.name;
            else if (clips != null && clips.Count > 0 && clips[0] != null) id = clips[0].name;
        }
#endif
    }

    public enum SoundType
    {
        MUSIC = 0,      // Background music
        SFX = 1,        // Sound effects
        AMBIENCE = 2,   // Ambience sounds
        VOICE = 3,      // Voice lines
    }
}