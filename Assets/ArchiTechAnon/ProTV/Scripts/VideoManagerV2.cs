using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;

namespace ArchiTech
{
    [RequireComponent(typeof(BaseVRCVideoPlayer))]
    [DefaultExecutionOrder(-9998)] // init immediately after the TV
    public class VideoManagerV2 : UdonSharpBehaviour
    {
        [NonSerialized] public BaseVRCVideoPlayer player;
        [NonSerialized] public bool isVisible;
        [Tooltip("Flag whether or not to have this video manager should automatcially manage the visibility of attached screens.")]
        public bool autoManageScreenVisibility = true;
        [Tooltip("Flag whether or not to have this video manager should automatically control the attached speakers' audio setup (2d/3d).")]
        public bool autoManageAudioMode = true;
        [Tooltip("Flag whether or not to have this video manager should automatically control the attached speakers' volume.")]
        public bool autoManageVolume = true;
        [Tooltip("Flag whether or not to have this video manager should automatically control the attached speakers' mute state.")]
        public bool autoManageMute = true;
        [Tooltip("Amount to set the audio spread in degrees (0-360) when switching to 3D audio mode. Set to a negative number to disable updating the spread automatically.")]
        [Range(0f, 360f)] public float spread3D = -1f;
        public GameObject[] screens;
        public AudioSource[] speakers;
        private TVManagerV2 tv;
        private VideoError lastError;
        [System.NonSerialized] public bool muted = true;
        [System.NonSerialized] public float volume = 0.5f;
        [System.NonSerialized] public bool audio3d = true; 

        private bool init = false;
        private bool skipLog = false;
        private string namePrefix;

        private void initialize()
        {
            if (init) return;
            player = (BaseVRCVideoPlayer)GetComponent(typeof(BaseVRCVideoPlayer));
            player.EnableAutomaticResync = false;
            namePrefix = transform.parent.name;
            init = true;
        }
        void Start()
        {
            initialize();
            log($"Hiding self");
            _Hide();
        }


        // === Player Proxy Methods ===

        // new void OnVideoStart() => tv._OnVideoPlayerStart();
        // new void OnVideoEnd() => tv.OnVideoPlayerEnd();
        new void OnVideoError(VideoError error) => tv._OnVideoPlayerError(error);
        // new void OnVideoLoop() => tv.OnVideoPlayerLoop();
        // new void OnVideoPause() => tv.OnVideoPlayerPause();
        // new void OnVideoPlay() => tv.OnVideoPlayerPlay();
        new void OnVideoReady() => tv._OnVideoPlayerStart();


        // === Public events to control the video player parts ===

        public void _Show()
        {
            if (!init) initialize();
            if (autoManageScreenVisibility)
            {
                foreach (var screen in screens)
                {
                    if (screen == null) continue;
                    screen.SetActive(true);
                }
            }
            if (autoManageMute) _UnMute();
            isVisible = true;
            if (tv != null)
                log($"{tv.gameObject.name} [{gameObject.name}] activated");
        }

        public void _Hide()
        {
            if (!init) initialize();
            if (autoManageMute) _Mute();
            player.Stop();
            if (autoManageScreenVisibility)
            {
                foreach (var screen in screens)
                {
                    if (screen == null) continue;
                    screen.SetActive(false);
                }
            }
            isVisible = false;
            log("Deactivated");
        }

        public void _ApplyStateTo(VideoManagerV2 other)
        {
            if (autoManageVolume)
            {
                other._ChangeMute(muted);
                other._ChangeVolume(volume);
            }
            if (autoManageAudioMode)
            {
                other._ChangeAudioMode(audio3d);
            }
        }

        public void _ChangeMute(bool muted)
        {
            if (!init) initialize();
            this.muted = muted;
            foreach (AudioSource speaker in speakers)
            {
                if (speaker == null) continue;
                log($"Setting {speaker.gameObject.name} Mute to {muted}");
                speaker.mute = muted;
            }
        }
        public void _Mute() => _ChangeMute(true);
        public void _UnMute() => _ChangeMute(false);


        public void _ChangeAudioMode(bool use3dAudio)
        {
            if (!init) initialize();
            this.audio3d = use3dAudio;
            float blend = use3dAudio ? 1.0f : 0.0f;
            float spread = use3dAudio ? spread3D : 360f;
            foreach (AudioSource speaker in speakers)
            {
                if (speaker == null) continue;
                speaker.spatialBlend = blend;
                if (spread3D >= 0) speaker.spread = spread;
            }
        }
        public void _Use3dAudio() => _ChangeAudioMode(true);
        public void _Use2dAudio() => _ChangeAudioMode(false);


        public void _ChangeVolume(float volume)
        {
            if (!init) initialize();
            this.volume = volume;
            foreach (AudioSource speaker in speakers)
            {
                if (speaker == null) continue;
                speaker.volume = volume;
            }
        }


        // ================= Helper Methods =================

        public void _SetTV(TVManagerV2 manager) {
            tv = manager;
            namePrefix = tv.gameObject.name;
        }

        private void log(string value)
        {
            if (!skipLog) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ccaa>{nameof(VideoManagerV2)} ({namePrefix}/{name})</color>] {value}");
        }
        private void warn(string value)
        {
            if (!skipLog) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> |  <color=#00ccaa>{nameof(VideoManagerV2)} ({namePrefix}/{name})</color>] {value}");
        }
        private void err(string value)
        {
            if (!skipLog) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> |  <color=#00ccaa>{nameof(VideoManagerV2)} ({namePrefix}/{name})</color>] {value}");
        }
    }

}