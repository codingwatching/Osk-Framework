using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace OSK
{
    [Serializable]
    [InlineProperty]
    public class SoundSetup
    {
        [LabelWidth(100)]
        public string id;

        [LabelWidth(100)] public AudioClip audioClip;

        [EnumToggleButtons] [LabelWidth(100)] public SoundType type = SoundType.SFX;

        [LabelWidth(100)] public float startTime;
        [LabelWidth(100)] public bool loop;

        [FoldoutGroup("Advanced", expanded: false)]
        [LabelText("VolumeFade")]
        [LabelWidth(100)]
        public VolumeFade volumeFade;

        [FoldoutGroup("Advanced")]
        [PropertyTooltip("Higher = more important")]
        [PropertyRange(0, 256)]
        [LabelWidth(100)]
        public int priority = 128;

        [FoldoutGroup("Advanced")] [Range(0.1f, 2f)] [LabelWidth(100)]
        public MinMaxFloat pitch;

        [FoldoutGroup("Advanced")] [PropertyRange(0, 10)] [LabelWidth(100)]
        public float playDelay;

        [FoldoutGroup("3D Settings")] [LabelWidth(100)]
        public Transform transform;

        [FoldoutGroup("3D Settings")] [LabelWidth(100)] [MinValue(0)]
        public int minDistance = 1;

        [FoldoutGroup("3D Settings")] [LabelWidth(100)] [MinValue(0)]
        public int maxDistance = 500;

        public SoundSetup(string id = "", AudioClip audioClip = null, SoundType type = SoundType.SFX,
            float startTime = 0, bool loop = false, VolumeFade volume = null, float playDelay = 0, int priority = 128,
            MinMaxFloat pitch = default, Transform transform = null, int minDistance = 1, int maxDistance = 500)
        {
            this.id = id;
            this.audioClip = audioClip;
            this.type = type;
            this.startTime = startTime;
            this.loop = loop;
            volumeFade = volume ?? new VolumeFade();
            this.playDelay = playDelay;
            this.priority = priority;
            this.pitch = pitch;
            this.transform = transform;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
        }

        public SoundSetup()
        {
            id = "";
            audioClip = null;
            type = SoundType.SFX;
            startTime = 0;
            loop = false;
            volumeFade = null; // = null get value in SoundSO
            playDelay = 0;
            priority = 128;
            pitch =  null; // = null get value in SoundSO
            transform = null;
            minDistance = 1;
            maxDistance = 500;
        }
    }

    [Serializable]
    public class VolumeFade
    {
        [LabelText("Init Volume")]
        [HorizontalGroup("Volume")]
        [LabelWidth(100)] [Min(0)] public float init;
        [HorizontalGroup("Volume")]
        [LabelWidth(100)] [Min(0)] public float target = 1;
        [HorizontalGroup("Volume")]
        [LabelWidth(100)] [Min(0)] public float duration;
        
        public VolumeFade(float init = 0, float target = 1, float duration = 0)
        {
            this.init = init;
            this.target = target;
            this.duration = duration;
        }
    }

    public partial class SoundManager
    {
        #region With SoundSetup

        /// <summary>
        ///  Play a sound with Id in list Sound SO using SoundSetup
        /// </summary>
        public AudioSource PlayID(SoundSetup soundSetup)
        {
            return Play(soundSetup.id, soundSetup.volumeFade, soundSetup.startTime, soundSetup.loop,
                soundSetup.playDelay,
                soundSetup.priority,
                soundSetup.pitch,
                soundSetup.transform, soundSetup.minDistance, soundSetup.maxDistance);
        }

        /// <summary>
        ///  Play a sound with ClipAudio using SoundSetup
        /// </summary>
        public AudioSource PlayClip(SoundSetup soundSetup)
        {
            return PlayAudioClip(soundSetup.audioClip, soundSetup.type, soundSetup.volumeFade, soundSetup.startTime,
                soundSetup.loop, soundSetup.playDelay,
                soundSetup.priority,
                soundSetup.pitch,
                soundSetup.transform, soundSetup.minDistance, soundSetup.maxDistance);
        }

        #endregion

        //─────────────────────────────────────────────────────────────
        #region Stop and Pause

        /// <summary>
        /// Stop all currently playing sounds and pending delayed plays.
        /// </summary>
        public void StopAll()
        {
            // Reverse iteration to safely remove from list
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.AudioSource != null) p.AudioSource.Stop();
                p.KillTween();
                if (p.AudioSource != null) Main.Pool.Despawn(p.AudioSource);
                ReturnPlayingSound(p);
            }
            _listSoundPlayings.Clear();
            StopAllPendingAudios();
        }

        public void Stop(string id)
        {
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData != null && p.SoundData.id == id)
                {
                    if (p.AudioSource != null) p.AudioSource.Stop();
                    p.KillTween();
                    if (p.AudioSource != null) Main.Pool.Despawn(p.AudioSource);
                    _listSoundPlayings.RemoveAt(i);
                    ReturnPlayingSound(p);
                }
            }
            StopPendingAudio(id);
        }

        public void Stop(AudioClip clip)
        {
            if (clip == null) return;
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.AudioSource != null && p.AudioSource.clip == clip)
                {
                    p.AudioSource.Stop();
                    p.KillTween();
                    Main.Pool.Despawn(p.AudioSource);
                    _listSoundPlayings.RemoveAt(i);
                    ReturnPlayingSound(p);
                }
            }
            StopPendingAudio(clip.name);
        }

        public void Stop(SoundType type)
        {
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData != null && p.SoundData.type == type)
                {
                    if (p.AudioSource != null) p.AudioSource.Stop();
                    p.KillTween();
                    if (p.AudioSource != null) Main.Pool.Despawn(p.AudioSource);
                    _listSoundPlayings.RemoveAt(i);
                    ReturnPlayingSound(p);
                }
            }
            // FIX: Previously used type.ToString() which never matched any clip name key
            StopAllPendingAudiosByType(type);
        }

        //─────────────────────────────────────────────────────────────
        #region Fade Stop

        /// <summary>
        /// Stop a sound by id with a smooth fade-out transition.
        /// </summary>
        public void StopWithFade(string id, float fadeDuration = 0.5f)
        {
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData != null && p.SoundData.id == id && p.AudioSource != null)
                {
                    FadeOutAndStop(p, fadeDuration);
                }
            }
            StopPendingAudio(id);
        }

        /// <summary>
        /// Stop all sounds of a type with a smooth fade-out transition.
        /// </summary>
        public void StopWithFade(SoundType type, float fadeDuration = 0.5f)
        {
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData != null && p.SoundData.type == type && p.AudioSource != null)
                {
                    FadeOutAndStop(p, fadeDuration);
                }
            }
            StopAllPendingAudiosByType(type);
        }

        /// <summary>
        /// Stop all sounds with a smooth fade-out transition.
        /// </summary>
        public void StopAllWithFade(float fadeDuration = 0.5f)
        {
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.AudioSource != null)
                {
                    FadeOutAndStop(p, fadeDuration);
                }
            }
            StopAllPendingAudios();
        }

        private void FadeOutAndStop(PlayingSound playing, float duration)
        {
            if (playing == null || playing.AudioSource == null) return;
            playing.KillTween();
            
            var source = playing.AudioSource;
            float startVolume = source.volume;
            
            playing.VolumeTween = DOVirtual.Float(startVolume, 0f, duration, v =>
            {
                if (source != null)
                    source.volume = v;
            }).OnComplete(() =>
            {
                if (source != null)
                {
                    source.Stop();
                    Main.Pool.Despawn(source);
                }
                _listSoundPlayings.Remove(playing);
                ReturnPlayingSound(playing);
            });
        }

        #endregion

        //─────────────────────────────────────────────────────────────
        #region Pending Audio

        public void StopPendingAudio(string clipId)
        {
            if (string.IsNullOrEmpty(clipId)) return;
            if (_playingTweens.TryGetValue(clipId, out var tween))
            {
                if (tween != null && tween.IsActive())
                    tween.Kill();
                _playingTweens.Remove(clipId);
            }
        }

        public void StopAllPendingAudios()
        {
            foreach (var tween in _playingTweens.Values)
            {
                if (tween != null && tween.IsActive())
                    tween.Kill();
            }
            _playingTweens.Clear();
        }
        
        /// <summary>
        /// FIX: Stop all pending delayed plays for sounds of a specific type.
        /// Previously Stop(SoundType) used type.ToString() as key which never matched.
        /// </summary>
        private void StopAllPendingAudiosByType(SoundType type)
        {
            // Collect keys to remove (avoid modifying dict during iteration)
            var keysToRemove = new List<string>();
            foreach (var kvp in _playingTweens)
            {
                var data = GetSoundInfo(kvp.Key);
                if (data != null && data.type == type)
                {
                    if (kvp.Value != null && kvp.Value.IsActive())
                        kvp.Value.Kill();
                    keysToRemove.Add(kvp.Key);
                }
            }
            for (int i = 0; i < keysToRemove.Count; i++)
                _playingTweens.Remove(keysToRemove[i]);
        }

        #endregion

        //─────────────────────────────────────────────────────────────
        public void PauseAll()
        {
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var p = _listSoundPlayings[i];
                if (p.AudioSource != null)
                {
                    p.AudioSource.Pause();
                    p.IsPaused = true;
                }
            }
        }

        public void Pause(SoundType type)
        {
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData != null && p.SoundData.type == type && p.AudioSource != null)
                {
                    p.IsPaused = true;
                    p.AudioSource.Pause();
                }
            }
        }

        public void ResumeAll()
        {
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var p = _listSoundPlayings[i];
                if (p.AudioSource != null)
                {
                    p.IsPaused = false;
                    p.AudioSource.UnPause();
                }
            }
        }

        public void Resume(SoundType type)
        {
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData != null && p.SoundData.type == type && p.AudioSource != null)
                {
                    p.AudioSource.UnPause();
                    p.IsPaused = false;
                }
            }
        }

        #endregion

        //─────────────────────────────────────────────────────────────
        #region Status

        public void SetMixerGroup(AudioMixerGroup mixerGroup)
        {
            if (_listSoundPlayings == null || _listSoundPlayings.Count == 0) return;
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var p = _listSoundPlayings[i];
                if (p.AudioSource != null)
                    p.AudioSource.outputAudioMixerGroup = mixerGroup;
            }
        }

        public void SetStatusSoundType(SoundType type, bool isOn)
        {
            SetSoundTypeEnabled(type, isOn);
            if (isOn)
            {
                Resume(type);
                if (type == SoundType.MUSIC)
                    RefreshSoundSettings();
            }
            else
            {
                Pause(type);
            }
        }


        public void SetStatusAllSound(bool isOn)
        {
            IsEnableMusic = isOn;
            IsEnableSoundSFX = isOn;
            IsEnableAmbience = isOn;
            IsEnableVoice = isOn;

            if (!isOn)
            {
                PauseAll();
            }
            else
            {
                ResumeAll();
                RefreshSoundSettings();
            }
        }
        
        public void RefreshSoundSettings()
        {
            if (!IsEnableMusic || _pendingMusic.Count <= 0) return;
            
            // Copy to temp list to avoid modification during iteration
            var pending = new List<SoundData>(_pendingMusic);
            _pendingMusic.Clear();
            
            for (int i = 0; i < pending.Count; i++)
            {
                Play(pending[i].id, loop: true);
            }
        }

        public void SetAllVolume(float volume)
        {
            MusicVolume = volume;
            SFXVolume = volume;
            AmbienceVolume = volume;
            VoiceVolume = volume;

            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var s = _listSoundPlayings[i];
                if (s.AudioSource == null || s.SoundData == null) continue;
                float multiplier = GetVolumeMultiplier(s.SoundData.type);
                s.AudioSource.volume = s.RawVolume * multiplier;
            }
        }

        public void SetAllVolume(SoundType type, float volume)
        {
            SetVolumeForType(type, volume);

            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var s = _listSoundPlayings[i];
                if (s.SoundData != null && s.SoundData.type == type && s.AudioSource != null)
                {
                    s.AudioSource.volume = s.RawVolume * GetVolumeMultiplier(type);
                }
            }
        }

        /// <summary>
        /// Create a volume fade tween. Returns the Tween for chaining.
        /// Replaces the old broken VolumeFade() that always returned 0.
        /// </summary>
        public Tween FadeVolume(float from, float to, float duration, Action<float> onUpdate = null)
        {
            return DOVirtual.Float(from, to, duration, v => onUpdate?.Invoke(v));
        }

        public AudioClip GetAudioClipInSO(string id)
        {
            var data = GetSoundInfo(id);
            return data?.audioClip;
        }

        public AudioClip GetAudioClipInSO(AudioClip audioClip)
        {
            var data = GetSoundInfo(audioClip);
            return data?.audioClip;
        }

        public AudioClip GetAudioClipOnScene(string id)
        {
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                if (_listSoundPlayings[i].SoundData != null && _listSoundPlayings[i].SoundData.id == id)
                    return _listSoundPlayings[i].AudioSource?.clip;
            }
            return null;
        }

        #endregion

        //─────────────────────────────────────────────────────────────
        #region Dispose

        /// <summary>
        /// Despawn an AudioSource with optional delay. Includes null-safety for clip access.
        /// </summary>
        public void Despawn(AudioSource audioSource, float delay = 0)
        {
            if (audioSource == null) return;

            // Find and cleanup the PlayingSound entry
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                if (_listSoundPlayings[i].AudioSource == audioSource)
                {
                    var p = _listSoundPlayings[i];
                    p.KillTween();
                    _listSoundPlayings.RemoveAt(i);
                    ReturnPlayingSound(p);
                    break;
                }
            }
            
            if (delay > 0)
                DespawnAudioSource(audioSource, delay).Run();
            else
                Main.Pool.Despawn(audioSource);
            
            // FIX: Null check for clip before accessing name
            if (audioSource.clip != null)
                StopPendingAudio(audioSource.clip.name);
        }

        public void DespawnMusicID(string id, float delay = 0)
        {
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData != null && p.SoundData.id == id)
                {
                    p.KillTween();
                    if (p.AudioSource != null)
                        DespawnAudioSource(p.AudioSource, delay).Run();
                    _listSoundPlayings.RemoveAt(i);
                    ReturnPlayingSound(p);
                }
            }
        }

        public void DestroyAll()
        {
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                p.KillTween();
                if (p.AudioSource != null)
                    Destroy(p.AudioSource);
                ReturnPlayingSound(p);
            }
            StopAllPendingAudios();

            _listSoundPlayings.Clear();
            _playingTweens.Clear();
            Main.Pool.DestroyAllInGroup(KEY_POOL.KEY_AUDIO_SOUND);
        }
        #endregion
    }
}