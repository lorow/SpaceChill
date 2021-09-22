
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    public class AudioLinkAdapter : UdonSharpBehaviour
    {
        public TVManagerV2 tv;
        public VRCAudioLink.AudioLink audioLinkInstance;
        public string speakerName = "AudioLink";
        [Tooltip("Optionally specify world music to pause while the TV is playing. Will resume world music a given number of seconds after TV stops playing.")]
        public AudioSource worldMusic;
        [Tooltip("How long to wait after the TV has finished before resuming the world music.")]
        public float worldMusicResumeDelay = 20f;
        [Tooltip("How long does the world music take to fade in after the delay has completed.")]
        public float worldMusicFadeInTime = 4f;
        private float worldMusicVolume;
        private float worldMusicFadeAmount;
        private int OUT_OnVideoPlayerChange_int_Index;
        private bool worldMusicActive = true;
        private bool hasWorldMusic = false;
        private bool init = false;
        private bool debug = false;

        private void initialize()
        {
            if (init) return;
            hasWorldMusic = worldMusic != null;
            if (hasWorldMusic)
            {
                if (audioLinkInstance.audioSource != worldMusic)
                    audioLinkInstance.audioSource = worldMusic;
                worldMusicVolume = worldMusic.volume;
            }
            tv._RegisterUdonSharpEventReceiver(this);
            init = true;
        }

        void Start()
        {
            initialize();
        }

        void Update()
        {
            if (hasWorldMusic)
            {
                if (worldMusic.isPlaying && worldMusic.volume < worldMusicVolume)
                {
                    if (worldMusicFadeInTime == 0f)
                    {
                        worldMusic.volume = worldMusicVolume;
                    }
                    else
                    {
                        worldMusicFadeAmount += Time.deltaTime;
                        worldMusic.volume = Mathf.SmoothStep(0f, worldMusicVolume, worldMusicFadeAmount / worldMusicFadeInTime);
                    }
                }
            }
        }

        public void _TvMediaStart()
        {
            if (hasWorldMusic && worldMusicActive)
            {
                worldMusic.volume = 0f;
                worldMusicFadeAmount = 0f;
                worldMusic.Pause();
                worldMusicActive = false;
            }
            updateAudioSource(tv.videoPlayer);
        }

        public void _TvMediaEnd() => resumeWorldMusic();
        public void _TvStop() => resumeWorldMusic();

        private void resumeWorldMusic()
        {
            if (hasWorldMusic)
            {
                worldMusicActive = true;
                SendCustomEventDelayedSeconds(nameof(_ActivateWorldMusic), worldMusicResumeDelay);
            }
        }

        public void _ActivateWorldMusic()
        {
            if (hasWorldMusic && worldMusicActive)
            {
                log("Resuming world music...");
                audioLinkInstance.audioSource = worldMusic;
                worldMusic.UnPause();
            }
        }

        public void _TvVideoPlayerChange()
        {
            updateAudioSource(OUT_OnVideoPlayerChange_int_Index);
        }

        private void updateAudioSource(int videoPlayer)
        {
            var manager = tv.videoManagers[videoPlayer];
            log($"Updating AudioLink for {manager.gameObject.name}");
            var speakers = manager.speakers;
            foreach (var speaker in speakers)
            {
                if (speaker.gameObject.name == speakerName)
                {
                    log("Valid source found");
                    audioLinkInstance.audioSource = speaker;
                    return;
                }
            }
        }


        private void log(string value)
        {
            if (!debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>AudioLinkAdapter</color>] {value}");
        }
        private void warn(string value)
        {
            if (!debug) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>AudioLinkAdapter</color>] {value}");
        }
        private void err(string value)
        {
            if (!debug) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#55ccaa>AudioLinkAdapter</color>] {value}");
        }
    }
}
