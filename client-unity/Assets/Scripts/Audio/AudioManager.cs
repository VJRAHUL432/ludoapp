using System.Collections.Generic;
using UnityEngine;
using Ludo.Core;

namespace Ludo.Audio
{
    /// <summary>
    /// Centralised audio with low GC: one music AudioSource and a small ring of
    /// SFX sources. Audio clips themselves should be Vorbis-compressed and
    /// loaded from Resources or Addressables.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public const string ClipDice = "sfx_dice";
        public const string ClipMove = "sfx_move";
        public const string ClipKill = "sfx_kill";
        public const string ClipWin  = "sfx_win";
        public const string ClipClick = "sfx_click";
        public const string ClipBgm  = "music_bgm";

        private AudioSource _music;
        private AudioSource[] _sfxRing;
        private int _sfxIdx;

        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        public bool Muted { get; private set; }

        public static AudioManager Create()
        {
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.priority = 0;
            _music.volume = 0.5f;

            _sfxRing = new AudioSource[6];
            for (int i = 0; i < _sfxRing.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.loop = false;
                s.playOnAwake = false;
                s.priority = 128;
                s.volume = 0.9f;
                _sfxRing[i] = s;
            }

            // restore mute pref
            if (ServiceLocator.TryGet<SessionStore>(out var ss))
                Muted = ss.Muted;

            ApplyMute();
        }

        public void PlaySfx(string clipKey)
        {
            if (Muted) return;
            var clip = LoadClip(clipKey);
            if (clip == null) return;
            var src = _sfxRing[_sfxIdx];
            _sfxIdx = (_sfxIdx + 1) % _sfxRing.Length;
            src.PlayOneShot(clip);
        }

        public void PlayMusic(string clipKey)
        {
            var clip = LoadClip(clipKey);
            if (clip == null) return;
            if (_music.clip == clip && _music.isPlaying) return;
            _music.clip = clip;
            if (!Muted) _music.Play();
        }

        public void StopMusic()
        {
            if (_music.isPlaying) _music.Stop();
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            ApplyMute();
            if (ServiceLocator.TryGet<SessionStore>(out var ss)) ss.Muted = Muted;
        }

        private void ApplyMute()
        {
            _music.mute = Muted;
            for (int i = 0; i < _sfxRing.Length; i++) _sfxRing[i].mute = Muted;
            if (Muted && _music.isPlaying) _music.Pause();
            else if (!Muted && _music.clip != null && !_music.isPlaying) _music.Play();
        }

        private AudioClip LoadClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_clips.TryGetValue(key, out var c)) return c;
            c = Resources.Load<AudioClip>($"Audio/{key}");
            if (c == null) Debug.LogWarning($"[Audio] missing clip: {key}");
            _clips[key] = c; // cache even nulls to avoid repeated lookups
            return c;
        }
    }
}
