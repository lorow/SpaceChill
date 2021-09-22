
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using UnityEngine.UI;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(-9999)] // needs to initialize before anything else if possible
    public class TVManagerV2 : UdonSharpBehaviour
    {
        private float EPSILON = Mathf.Epsilon;

        // list of all events that the TVManagerV2 produces.
        private const string EVENT_READY = "_TvReady";
        private const string EVENT_PLAY = "_TvPlay";
        private const string EVENT_PAUSE = "_TvPause";
        private const string EVENT_STOP = "_TvStop";
        private const string EVENT_MEDIASTART = "_TvMediaStart";
        private const string EVENT_MEDIAEND = "_TvMediaEnd";
        private const string EVENT_MEDIALOOP = "_TvMediaLoop";
        private const string EVENT_MEDIACHANGE = "_TvMediaChange";
        private const string EVENT_OWNERCHANGE = "_TvOwnerChange";
        private const string EVENT_VIDEOPLAYERCHANGE = "_TvVideoPlayerChange";
        private const string EVENT_VIDEOPLAYERERROR = "_TvVideoPlayerError";
        private const string EVENT_MUTE = "_TvMute";
        private const string EVENT_UNMUTE = "_TvUnMute";
        private const string EVENT_VOLUMECHANGE = "_TvVolumeChange";
        private const string EVENT_AUDIOMODE3D = "_TvAudioMode3d";
        private const string EVENT_AUDIOMODE2D = "_TvAudioMode2d";
        private const string EVENT_ENABLELOOP = "_TvEnableLoop";
        private const string EVENT_DISABLELOOP = "_TvDisableLoop";
        private const string EVENT_SYNC = "_TvSync";
        private const string EVENT_DESYNC = "_TvDeSync";
        private const string EVENT_LOCK = "_TvLock";
        private const string EVENT_UNLOCK = "_TvUnLock";
        private const string EVENT_LOADING = "_TvLoading";
        private const string EVENT_LOADINGEND = "_TvLoadingEnd";
        private const string EVENT_LOADINGSTOP = "_TvLoadingAbort";
        // These variable names are used to pass information back to any event listeners that have been registered.
        // They follow the pattern of the word OUT, the expected type of the target variable and a meaningful name on what that variable is related to.
        // EG: OUT_float_Volume means the data is outgoing, will target a variable of type 'float', and represents the TV's volume value.
        private const string OUT_ERROR = "OUT_TvVideoPlayerError_VideoError_Error";
        private const string OUT_VOLUME = "OUT_TvVolumeChange_float_Percent";
        private const string OUT_VIDEOPLAYER = "OUT_TvVideoPlayerChange_int_Index";
        private const string OUT_OWNER = "OUT_TvOwnerChange_int_Id";


        // === Event input variables (update these from external udon graphs. U# should use the corresponding parameterized methods instead) ===
        // These fields represent storage for incoming data from other scripts. 
        // The format is as follows: the word IN, the event that the data is related to, 
        //  the expected type of the data, and a meaningful name on what that variable is related to.
        // Eg: IN_ChangeVolume_float_Percent means the data is incoming, will be used by the event named 'ChangeVideo', 
        //  and represents the TV's volume percent value as a normalized float (between 0.0 and 1.0).

        // parameter for _ChangeVideo event
        [NonSerialized] public VRCUrl IN_ChangeMedia_VRCUrl_Url = VRCUrl.Empty;
        // [System.NonSerialized] public VRCUrl IN_ChangeMedia_VRCUrl_UrlQuest = VRCUrl.Empty;
        // parameter for _ChangeVolume event, expects it to be a normalized float between 0f and 1f
        [NonSerialized] public float IN_ChangeVolume_float_Percent = 0f;
        // parameter for _ChangeSeekTime event
        [NonSerialized] public float IN_ChangeSeekTime_float_Seconds = 0f;
        // parameter for the _ChangeSeekPercent event, expects it to be a normalized float between 0f and 1f
        [NonSerialized] public float IN_ChangeSeekPercent_float_Percent = 0f;
        // paramter for _ChangeVideoPlayer event
        [NonSerialized] public int IN_ChangeVideoPlayer_int_Index = 2;
        // parameter for _RegisterUdonEventReceiver event
        [NonSerialized] public UdonBehaviour IN_RegisterUdonEventReceiver_UdonBehavior_Subscriber;
        // parameter for _RegisterUdonEventReceiver event
        [NonSerialized] public byte IN_RegisterUdonEventReceiver_byte_Priority = 128;
        private byte defaultPriority = 128;


        // enum values for state/stateSync/currentState
        private const int STOPPED = 0;
        private const int PLAYING = 1;
        private const int PAUSED = 2;

        [Tooltip("This is the URL to set as automatically playing when the first user joins a new instance. This has no bearing on an existing instance as the TV has already been syncing data after the initial point.")]
        public VRCUrl autoplayURL = VRCUrl.Empty;
        [Tooltip("This is used to offset the delay of the initial attempt for a TV to fetch it's URL when a user joins a world. Primarily used if there are multiple TVs in the world to avoid excessive rate limiting issues. Make sure each TV has a different value (recommend intervals of 3).")]
        [Range(0f, 60f)] public float autoplayStartOffset = 0f;
        [Tooltip("The volume that the TV starts off at.")]
        [Range(0f, 1f)] public float initialVolume = 0.3f;
        [Tooltip("The player (based on the VideoManagers list below) for the TV to start off on.")]
        public int initialPlayer = 0;
        [Tooltip("Time difference allowed between owner's synced seek time and the local seek time while the video is paused locally. Can be thought of as a 'frame preview' of what's currently playing. It's good to have this at a higher value, NOT recommended to have this value less than 1.0")]
        public float pausedResyncThreshold = 5.0f;
        [Min(5f)]
        [Tooltip("The interval for the TV to trigger an automatic resync to correct any AV and Time desync issues. Defaults to 5 minutes.")]
        public float automaticResyncInterval = 300f;
        [Tooltip("Specifies how many times to try and reload a live stream before registering that the media has actually ended, sending related events.")]
        public int retryLiveMedia = 1;
        // There is serious problems with the TV (specifically AVPro rendering to materials) when it's game object is manually disabled in the editor.
        // So instead of disabling it in the editor, the world creator should activate this flag to have the TV automatically hide itself after the initilization completes.
        // This allows AVPro to correctly init the materials references and avoids whitescreen issues.
        [Tooltip("Set this flag to have the TV auto-hide itself after initialization. DO NOT HAVE THE TV DISABLED MANUALLY! THIS WILL CAUSE PROBLEMS! Please use this flag instead.")]
        public bool startHidden = false;
        // this option is explicitly used for the edge case where world owners want to have anyone use the TV regardless.
        // It prevents the instance master from being able to lock the TV down by any means, when the world creator doesn't want them to.
        // Helps against malicious users in the edge case. 
        [Tooltip("This option enables the instance master to have admin powers for the TV. Leaving enabled should be perfectly acceptable in most cases.")]
        public bool allowMasterControl = true;
        [Tooltip("Determines if the video player starts off as locked down to master only. Good for worlds that do public events and similar.")]
        public bool lockedByDefault = false;

        // This flag is to track whether or not the local player is able to operate independently of the owner
        // Setting to false gives the local player full control of their local player. 
        // Once they value is set to true, it will automatically resync with the owner, even if the video URL has changed since desyncing.
        [Tooltip("Flag that determies whether the video player should sync with the owner. If false, the local user has full control over the player and only affects the local user.")]
        public bool syncToOwner = true;
        [Space]

        // Storage for all event subscriber behaviors. 
        // Due to some odd type issues, it requires being stored as a list of components instead of UdonBehaviors.
        // All possible events that this TV triggers will be sent to ALL targets in order of addition
        private Component[] eventTargets;
        private byte[] eventTargetWeights;

        // === Video Manager control ===
        public VideoManagerV2[] videoManagers;
        // assigned when the active manager switches to the next one.
        private VideoManagerV2 prevManager;
        // main manager reference that everything operates off of.
        [NonSerialized] public VideoManagerV2 activeManager;
        // assigned when the user selects a manager to switch to.
        private VideoManagerV2 nextManager;

        // === Synchronized variables and their local counterparts ===

        private TVManagerV2ManualSync syncData;

        [NonSerialized] [UdonSynced] public float syncTime = 0f;
        [NonSerialized] public float currentTime;
        [UdonSynced] private int lagCompSync;
        private float lagComp;
        // ownerState/localState values: 0 = stopped, 1 = playing, 2 = paused
        // ownerState is the value that is synced
        // localState is the sync tracking counterpart (used to detect state change from the owner)
        // currentState is the ACTUAL state that the local video player is in.
        // localState and currentState are separated to allow for local to not be forced into the owner's state completely
        // The primary reason for this deleniation is to allow for the local to pause without having to desync.
        // For eg: Someone isn't interested in most videos, but still wants to know what is playing, so they pause it and let it do the pausedThreshold resync (every 5 seconds)
        //      One could simply mute the video, yes, but some people might not want the distraction of an active video playing if they happen to be in front of a mirror
        //      where the TV is reflected. This allows a much more comfortable "keep track of" mode for those users.
        [NonSerialized] public int stateSync = STOPPED;
        [NonSerialized] public int state = STOPPED;
        [NonSerialized] public int currentState = STOPPED;
        [NonSerialized] public VRCUrl urlSync = VRCUrl.Empty;
        [NonSerialized] public VRCUrl url = VRCUrl.Empty;
        [NonSerialized] public VRCUrl urlQuestSync = VRCUrl.Empty;
        [NonSerialized] public VRCUrl urlQuest = VRCUrl.Empty;
        // a miscellaneous string that is used to describe the current video. 
        // Allows for different extensions to share things like custom video titles in a centralized way.
        // Is automatically by default the current URL's full domain name.
        // The ideal place to update this is during the _OnMediaStart event.
        [NonSerialized] public string localLabel = string.Empty;
        [NonSerialized] public string[] urlMeta = new string[0];
        [NonSerialized] public bool lockedSync = false;
        [NonSerialized] public bool locked = false;
        [NonSerialized] public int urlRevisionSync;
        [NonSerialized] public int urlRevision;
        [NonSerialized] public bool loadingSync;
        [NonSerialized] public bool loading;


        // === Fields for tracking internal state ===
        [NonSerialized] public float startTime;
        [NonSerialized] public float endTime;
        // actual length of the loaded media
        [NonSerialized] public float videoLength;
        // amount of the time that the video is set to play for (basically endTime minus startTime)
        [NonSerialized] public float videoDuration;
        [NonSerialized] public bool loop;
        [NonSerialized] public bool mute;
        [NonSerialized] public bool audio3d = true;
        [NonSerialized] public float volume = 0.5f;
        [NonSerialized] public int videoPlayer = -1;
        [NonSerialized] public bool isLive = false;
        [NonSerialized] public bool retryingLiveMedia;
        private int retryCount = 0;

        // Time delay before allowing the TV to update it's active video
        // This value is always assigned as: Time.realtimeSinceStartup + someOffsetValue;
        // It is checked using this structure: if (Time.realtimeSinceStartup < waitUntil) { waitIsOver(); }
        private float waitUntil = 0f;
        // Time to seek to at time sync check
        // This value is set for a couple different reasons.
        // If the video player is switching locally to a different player, it will use Mathf.Epsilon to signal seemless seek time for the player being swapped to.
        // If the video URL contains a t=, startat=, starttime=, or start= params, it will assign that value so to start the video at that point once it's loaded.
        private float jumpToTime = 0f;
        // This flag simply enables the local player to be paused without forcing hard-sync to the owner's state.
        // This results in a pause that, when the owner pauses then plays, it won't foroce the local player to unpause unintentionally.
        // This flag cooperates with the pausedThreshold constant to enable resyncing every 5 seconds without actually having the video playing.
        private bool locallyPaused = false;
        // Flag to check if an error occured. When true, it will prevent auto-reloading of the video.
        // Player will need to be forced to refresh by intentionally pressing (calling the method) Play.
        // The exception to this rule is when the error is of RateLimited type. 
        // This error will trigger a auto-reload after a 3 second delay to prevent excess requests from spamming the network causing more rate limiting triggers.
        private bool errorOccurred = false;


        // === Flags used to prevent infinite event loops ==
        // (like volume change -> _ChangeVolume -> OnVolumeChange -> volume change -> _ChangeVolume -> OnVolumeChange -> etc)
        private string[] haltedEvents = new string[23];

        // === Misc variables ===
        private string playerNameOverride = string.Empty;
        private bool refreshAfterWait = false;
        private bool sendEvents = false;
        private bool activeInitialized = false;
        private bool enforceSyncTime = true;
        private float syncEnforceWait;
        private float autoSyncWait;
        private bool initialCatchUp = true;
        private float syncEnforcementTimeLimit = 3f;
        private bool hasActiveManager = false;
        private VRCPlayerApi localPlayer;
        private bool debug = true;
        [NonSerialized] public bool init = false;

        private void initialize()
        {
            if (init) return;
            init = true;
            log($"Starting TVManagerV2");
            localPlayer = Networking.LocalPlayer;
            syncData = transform.GetComponentInChildren<TVManagerV2ManualSync>();
            syncData._SetTV(this);
            if (videoManagers == null) videoManagers = GetComponentsInChildren<VideoManagerV2>();
            if (videoManagers != null)
            {
                foreach (VideoManagerV2 m in videoManagers)
                {
                    if (m != null) m._SetTV(this);
                }
            }
            // assign inital video if owner
            IN_ChangeMedia_VRCUrl_Url = url = urlSync = autoplayURL;
            // determine initial locked state
            if (lockedByDefault)
            {
                if (localPlayer.isMaster) lockedSync = true;
                locked = true;
            }
            // load initial video player (sets the nextManager value)
            _ChangeVideoPlayerTo(initialPlayer);
            nextManager._ChangeVolume(volume = initialVolume);
            // re-enable event sending for the on ready event
            sendEvents = eventTargets != null && eventTargets.Length > 0;
            forwardEvent(EVENT_READY);
            if (startHidden) gameObject.SetActive(false);
            if (localPlayer.isMaster) syncData._RequestSync();
            // make the script wait a few seconds before trying to fetch the video data for the first time.
            waitUntil = Time.realtimeSinceStartup + 3f + autoplayStartOffset;
            if (automaticResyncInterval == 0f) automaticResyncInterval = Mathf.Infinity;
            autoSyncWait = Time.realtimeSinceStartup + automaticResyncInterval;
            syncEnforceWait = waitUntil + syncEnforcementTimeLimit;
        }

        // === Subscription Methods ===

        void Start()
        {
            log("Start");
            initialize();
        }

        void OnEnable()
        {
            log("Enable");
            initialize();
            // if the TV was disabled before Update check ran, the TV was set off by default from some external method, like a Touch Control or likewise.
            // If that's the case, the startHidden flag is redundant so simply unset the flag.
            if (startHidden) startHidden = false;
            if (hasActiveManager)
            {
                if (isOwner()) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_Play));
                else _Play();
            }
        }

        void OnDisable()
        {
            if (hasActiveManager)
            {
                // In order to prevent a loop glitch due to owner not updating syncTime when the object is disabled
                // send a command as owner to everyone to pause the video. 
                // There are other solutions that might work, but this is the most elegant that could be found so far.
                if (isOwner()) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_Pause));
                else _Pause();
            }
        }

        void Update()
        {
            if (init) { } else return; // has not yet been initialized
            var time = Time.realtimeSinceStartup;
            // wait until the timeout has cleard
            if (time < waitUntil) return;
            if (activeManager != nextManager)
            {
                log($"Manager swap: Next {nextManager == null} -> Active {activeManager == null} -> Prev {prevManager == null}");
                prevManager = activeManager;
                activeManager = nextManager;
                hasActiveManager = true;
                activeInitialized = true;
                _RefreshMedia();
                return; // video has been refreshed, hold till next Update call
            }
            else if (refreshAfterWait)
            {
                log("Refresh video via update local");
                refreshAfterWait = false;
                // For some reason when rate limiting happens, the auto reload causes loading to be enabled unexpectely
                // This might cause unexpected edgecases at some point. Keep a close eye on media change issues related to loading states.
                loading = false;
                _RefreshMedia();
                return;
            }
            // activeManager has not been fully init'd yet, skip current cycle.
            if (activeInitialized) { } else return;
            // when video player is switching (as denoted by the epsilon jump time), use the prevManager reference.
            var player = jumpToTime == EPSILON ? prevManager.player : activeManager.player;
            currentTime = player.GetTime();

            if (isLive) return; // video is a livestream
            if (errorOccurred) return; // blocking error has occurred

            if (player.IsPlaying && time > autoSyncWait)
            {
                // every so often trigger an automatic resync
                enforceSyncTime = true;
                autoSyncWait = time + automaticResyncInterval;
            }

            if (syncToOwner) { }
            else
            {
                if (enforceSyncTime && time >= syncEnforceWait)
                {
                    // single time update for local-only mode.
                    // Also helps fix audio/video desync/drift in most cases.
                    enforceSyncTime = false;
                    log($"Sync enforcement requested. Updating to {currentTime}");
                    currentTime += 0.05f; // anything smaller than this won't be registered by AVPro
                    player.SetTime(currentTime);
                }
                return;
            }
            // To run sync logic, the following conditions must be met.
            // syncToOwner enabled, no blocking error occurred, video is not a livestream

            // This handles updating the sync time from owner to others.
            // owner must be playing and local must not be stopped
            if (isOwner())
            {
                if (enforceSyncTime)
                {
                    if (time >= syncEnforceWait)
                    {
                        // single time update for owner. 
                        // Also helps fix audio/video desync/drift in most cases.
                        enforceSyncTime = false;
                        log($"Sync enforcement requested. Updating to {currentTime}");
                        currentTime += 0.05f; // anything smaller than this won't be registered by AVPro
                        player.SetTime(currentTime);
                    }
                }
                syncTime = currentTime;
            }
            else if (loading) { } // skip if TV is loading a video
            else if (currentState != STOPPED)
            {
                var compSyncTime = syncTime + lagComp;
                if (compSyncTime > endTime) compSyncTime = endTime;
                float syncDelta = Mathf.Abs(currentTime - compSyncTime);
                if (player.IsPlaying)
                {
                    // sync time enforcement check should ONLY be for when the video is playing
                    // Also helps fix audio/video desync/drift in most cases.
                    if (enforceSyncTime)
                    {
                        if (time >= syncEnforceWait)
                        {
                            currentTime = compSyncTime;
                            log($"Sync enforcement requested. Updating to {compSyncTime}");
                            player.SetTime(currentTime);
                            enforceSyncTime = false;
                        }
                    }
                }
                // video sync enforcement will always occur for paused mode as the user expects the video to not be active, so we can skip forward as needed.
                else if (syncDelta > pausedResyncThreshold)
                {
                    log($"Paused sync threshold exceeded. Updating to {compSyncTime}");
                    currentTime = compSyncTime;
                    player.SetTime(currentTime);
                }
            }
        }

        void LateUpdate()
        {
            if (activeInitialized) { } else return;
            // when video player is switching (as denoted by the epsilon jump time), use the prevManager reference.
            var player = jumpToTime == EPSILON ? prevManager.player : activeManager.player;
            currentTime = player.GetTime();
            // loop/media end check
            if (currentTime + 0.05f >= endTime)
            {
                var owner = isOwner();
                if (owner && loop)
                {
                    // owner when loop is active
                    activeManager.player.SetTime(startTime);
                    syncTime = currentTime = startTime;
                    forwardEvent(EVENT_MEDIALOOP);
                }
                else if (currentState == PLAYING && endTime > 0f)
                {
                    if (loop)
                    {
                        if (syncToOwner && syncTime >= currentTime) { } // sync is enabled but sync time hasn't been passed, skip
                        else
                        {
                            // non-owner when owner has loop (causing the sync time to start over)
                            activeManager.player.SetTime(startTime);
                            // update current time to start time so this only executes once, prevents accidental spam
                            currentTime = startTime;
                            // loop is cheap so make the sync timeout minimal but NOT 0
                            queueSync(0.01f);
                            forwardEvent(EVENT_MEDIALOOP);
                        }
                    }
                    else
                    {
                        // in any other condition, pause the video, specifying the media has finished
                        activeManager.player.Pause();
                        activeManager.player.SetTime(endTime);
                        currentState = PAUSED;
                        currentTime = endTime;
                        if (owner) syncTime = currentTime;
                        forwardEvent(EVENT_PAUSE);
                        forwardEvent(EVENT_MEDIAEND);
                    }
                }
            }
        }

        new void OnPreSerialization()
        {
            lagCompSync = Networking.GetServerTimeInMilliseconds();
        }

        new void OnDeserialization()
        {
            lagComp = (Networking.GetServerTimeInMilliseconds() - lagCompSync) * 0.001f;
        }

        new void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (isOwner()) syncData._RequestSync();
            forwardVariable(OUT_OWNER, player.playerId);
            forwardEvent(EVENT_OWNERCHANGE);
        }

        public void _PostDeserialization()
        {
            if (syncToOwner) { } else return;
            if (Time.realtimeSinceStartup < waitUntil) return;
            if (urlRevision != urlRevisionSync)
            {
                log("URL change via deserialization");
                urlRevision = urlRevisionSync;
                url = urlSync;
                // trigger video load
                IN_ChangeMedia_VRCUrl_Url = url;
                queueRefresh(0f);
            }
            if (locked != lockedSync)
            {
                log($"Lock change via deserialization {locked} -> {lockedSync}");
                locked = lockedSync;
                forwardEvent(locked ? EVENT_LOCK : EVENT_UNLOCK);
            }
            if (loading) return; // do not allow certain actions to occur while a video is loading
            if (state != stateSync)
            {
                log($"State change via deserialization {state} -> {stateSync}");
                state = stateSync;
                switch (state)
                {
                    // always enforce stopping
                    case STOPPED: _Stop(); break;
                    // allow the local player to be paused if owner is playing
                    case PLAYING: if (!locallyPaused) play(); break;
                    // if owner pauses, unset the local pause flag
                    case PAUSED: pause(); locallyPaused = false; break;
                    default: break;
                }
            }
            // Give the syncTime enough time to catch up before running the time sync logic
            // This mitigates certain issues when switching videos
            queueSync(0.3f);
        }


        // === VideoManager events ===

        public void _OnVideoPlayerError(VideoError error)
        {
            err($"Video Error: {error}");
            if (error == VideoError.RateLimited)
            {
                log("Refresh via rate limit error, retrying in 5 seconds...");
                errorOccurred = false; // skip error shortcircut and use a time check instead
                queueRefresh(5f);
            }
            else if (isLive && !errorOccurred && error == VideoError.PlayerError)
            {
                log("Livestream error: Attempting retry");
                if (retryingLiveMedia && retryCount == retryLiveMedia)
                {
                    log("Retries failed. Halting.");
                    retryingLiveMedia = false;
                    retryCount = 0;
                    errorOccurred = true;
                }
                else
                {
                    log($"Retry number: {retryCount + 1}");
                    currentState = PAUSED;
                    retryingLiveMedia = true;
                    retryCount++;
                    queueRefresh(0f);
                }
            }
            else errorOccurred = true;
            setLoadingState(false);
            forwardVariable(OUT_ERROR, error);
            forwardEvent(EVENT_VIDEOPLAYERERROR);
        }


        // Once the active manager detects the player has finished loading, get video information and log
        public void _OnVideoPlayerStart()
        {
            var owner = isOwner();
            cacheVideoInfo();
            if (!activeManager.isVisible) activeManager._Show();
            if (loading)
            {
                if (prevManager != activeManager)
                {
                    if (prevManager != null)
                    {
                        prevManager._ApplyStateTo(activeManager);
                        // this epsilon check is explicitly for when the video players switch, in order to make the timejump much more seamless for non-owners.
                        if (jumpToTime == Mathf.Epsilon)
                        {
                            log("Epsilon time jump set to previous manager");
                            jumpToTime = prevManager.player.GetTime();
                        }
                        log($"Hiding previous manager {prevManager.gameObject.name}");
                        prevManager._Hide();
                        forwardVariable(OUT_VOLUME, activeManager.volume);
                        forwardEvent(EVENT_VOLUMECHANGE);
                        forwardEvent(activeManager.audio3d ? EVENT_AUDIOMODE3D : EVENT_AUDIOMODE2D);
                        forwardEvent(activeManager.muted ? EVENT_MUTE : EVENT_UNMUTE);

                    }
                    // video manager wasn't changed, skip epsilon time jump
                    else if (jumpToTime == Mathf.Epsilon)
                    {
                        log("Epsilon time jump set to start time due to not switching video players.");
                        jumpToTime = startTime;
                    }
                    prevManager = activeManager;
                }
                log($"{name} [{activeManager.gameObject.name}] Now Playing: {url}");
                localLabel = getUrlDomain(url);
                if (owner)
                {
                    state = stateSync = PLAYING;
                    syncData._RequestSync();
                }
                setLoadingState(false);
                forwardEvent(EVENT_MEDIASTART);
            }
            if (!mute) activeManager._UnMute();
            if (endTime <= startTime)
            {
                log("endTime preceeds startTime. Updating.");
                startTime = 0f; // invalid start time given.
            }
            if (jumpToTime < startTime)
            {
                log("jumpToTime preceeds startTime. Updating.");
                jumpToTime = startTime;
            }
            if (jumpToTime > 0f)
            {
                log($"Jumping {name} [{activeManager.gameObject.name}] to timestamp: {jumpToTime}");
                activeManager.player.SetTime(jumpToTime);
                jumpToTime = 0f;
            }
            errorOccurred = false;
            currentState = PLAYING;
            initialCatchUp = true;
            activeManager.player.Play();
            forwardEvent(EVENT_PLAY);
            queueSync(0.3f);
        }



        // === Public events to control the TV from user interfaces ===

        public void _RefreshMedia()
        {
            if (!init) return;
            if (loading)
            {
                log("Media is currently loading. Skip.");
                return; // disallow refreshing media while TV is loading another video
            }
            // compare input URL and previous URL
            bool hasInputUrl = IN_ChangeMedia_VRCUrl_Url != null && IN_ChangeMedia_VRCUrl_Url.Get() != string.Empty;
            if (hasInputUrl)
            {
                log($"New URL : {IN_ChangeMedia_VRCUrl_Url}");
                url = IN_ChangeMedia_VRCUrl_Url;
                if (isOwner())
                {
                    urlSync = url;
                    urlRevisionSync++;
                    syncData._RequestSync();
                }
            }
            IN_ChangeMedia_VRCUrl_Url = VRCUrl.Empty;

            if (syncToOwner) url = urlSync;
            if (url.Get() == string.Empty)
            {
                log("URL is blank. Skip.");
                return;
            }
            log($"{name} [{nextManager.gameObject.name}] loading URL: {url}");
            nextManager.player.LoadURL(url);
            if (hasInputUrl) forwardEvent(EVENT_MEDIACHANGE);
            setLoadingState(true);
        }

        public void _ChangeMedia()
        {
            if (!changeMediaChecks()) return;
            if (_TakeOwnership())
            {
                log("Media Change Check");
                _RefreshMedia();
            }
        }
        // equivalent to: udonBehavior.SetProgramVariable("IN_ChangeMedia_VRCUrl_Url", (VRCUrl) url); udonBehavior.SendCustomEvent("_ChangeMedia");
        public void _ChangeMediaTo(VRCUrl url)
        {
            log("Change Media by param check");
            IN_ChangeMedia_VRCUrl_Url = url;
            _ChangeMedia();
        }

        // public void _ChangeQuestMediaTo(VRCUrl url)
        // {
        //     if (!changeMediaChecks()) return;
        // }

        public void _DelayedChangeMediaTo(VRCUrl url)
        {
            if (!changeMediaChecks()) return;
            log("Delayed Media Change");
            IN_ChangeMedia_VRCUrl_Url = url;
            // refresh next frame
            queueRefresh(0f);
        }

        private bool changeMediaChecks()
        {
            if (!init) return false;
            if (loading)
            {
                warn("Cannot change to another media while loading.");
                return false;
            }
            if (locked && !isMasterOwner())
            {
                warn("Video player is locked to master. Cannot change media for non-masters.");
                return false;
            }
            return true;
        }


        // equivalent to: udonBehavior.SetProgramVariable("IN_ChangeVideoPlayer_int_Index", (VRCUrl) url); udonBehavior.SendCustomEvent("_ChangeVideoPlayer");
        public void _ChangeVideoPlayer()
        {
            if (!init) return;
            // no need to change if same is picked
            if (IN_ChangeVideoPlayer_int_Index == videoPlayer) return;
            // do not allow changing resolution while a video is loading.
            if (loading)
            {
                IN_ChangeVideoPlayer_int_Index = videoPlayer;
                return;
            }
            if (IN_ChangeVideoPlayer_int_Index >= videoManagers.Length)
            {
                err($"Video Player swap value too large: Expected value between 0 and {videoManagers.Length - 1} - Actual {IN_ChangeVideoPlayer_int_Index}");
                return;
            }
            // special condition for time jump between switching video players
            if (hasActiveManager) jumpToTime = Mathf.Epsilon;
            nextManager = videoManagers[IN_ChangeVideoPlayer_int_Index];
            videoPlayer = IN_ChangeVideoPlayer_int_Index;
            forwardVariable(OUT_VIDEOPLAYER, videoPlayer);
            forwardEvent(EVENT_VIDEOPLAYERCHANGE);
            log($"Is next player null? {nextManager == null}");
            log($"Switching to: {gameObject.name} [{nextManager.gameObject.name}]");
            IN_ChangeVideoPlayer_int_Index = -1;
        }

        public void _ChangeVideoPlayerTo(int videoPlayer)
        {
            IN_ChangeVideoPlayer_int_Index = videoPlayer;
            _ChangeVideoPlayer();
        }

        public void ALL_Play() => _Play();
        public void _Play()
        {
            if (isOwner() || state != STOPPED)
            {
                if (currentState == STOPPED)
                {
                    log("Refresh video via Play (owner playing/local stopped)");
                    _RefreshMedia();
                }
                else
                {
                    play();
                }
            }
        }
        private void play()
        {
            if (!init) return;
            log("Normal Play");
            if (syncToOwner && stateSync == PAUSED && !isOwner()) return;
            BaseVRCVideoPlayer player;
            if (hasActiveManager) player = activeManager.player;
            else player = nextManager.player;
            var owner = isOwner();
            if (owner)
            {
                stateSync = state = PLAYING;
                syncData._RequestSync();
            }
            // if video is at end and user forces play, force loop the video one time.
            log($"Current {currentTime} End {endTime}");
            if (currentTime + 0.05f >= endTime)
            {
                log("Single loop");
                currentTime = startTime;
                player.SetTime(startTime);
                forwardEvent(EVENT_MEDIALOOP);
            }
            player.Play();
            currentState = PLAYING;
            if (syncToOwner && !owner) queueSync(0.2f);
            forwardEvent(EVENT_PLAY);
        }

        public void ALL_Pause() => _Pause();
        public void _Pause()
        {
            if (state != STOPPED)
            {
                pause();
            }
        }
        private void pause()
        {
            if (!init) return;
            BaseVRCVideoPlayer player = hasActiveManager ? activeManager.player : nextManager.player;
            player.Pause();
            if (isOwner())
            {
                stateSync = state = PAUSED;
                syncData._RequestSync();
            }
            currentState = PAUSED;
            locallyPaused = true; // flag to determine if pause was locally triggered
            forwardEvent(EVENT_PAUSE);
        }

        public void _Stop()
        {
            if (!hasActiveManager) return;
            log("Stopping, hiding active");
            if (loading)
            {
                log("Stop called while loading");
                // if stop is called while loading a video, the video loading will be halted instead of the active player
                nextManager._Hide();
                loading = false;
                locallyPaused = false;
                errorOccurred = false;
                forwardEvent(EVENT_LOADINGSTOP);
                return;
            }
            activeManager._Hide();
            if (isOwner())
            {
                stateSync = state = STOPPED;
                activeManager.player.Stop();
                activeManager.player.SetTime(0f);
                syncData._RequestSync();
            }
            currentState = STOPPED;
            setLoadingState(false);
            locallyPaused = false;
            errorOccurred = false;
            forwardEvent(EVENT_STOP);
        }

        public void _Mute()
        {
            if (!hasActiveManager) return;
            mute = true;
            activeManager._Mute();
            forwardEvent(EVENT_MUTE);
        }
        public void _UnMute()
        {
            if (!hasActiveManager) return;
            mute = false;
            activeManager._UnMute();
            forwardEvent(EVENT_UNMUTE);
        }
        public void _ToggleMute()
        {
            if (!hasActiveManager) return;
            mute = !mute;
            activeManager._ChangeMute(mute);
            forwardEvent(mute ? EVENT_MUTE : EVENT_UNMUTE);
        }
        public void _ChangeMuteTo(bool mute)
        {
            if (!hasActiveManager) return;
            this.mute = mute;
            activeManager._ChangeMute(mute);
            forwardEvent(mute ? EVENT_MUTE : EVENT_UNMUTE);
        }
        public void _ChangeVolume()
        {
            if (!hasActiveManager) return;
            volume = IN_ChangeVolume_float_Percent;
            activeManager._ChangeVolume(volume);
            forwardVariable(OUT_VOLUME, volume);
            forwardEvent(EVENT_VOLUMECHANGE);
            IN_ChangeVolume_float_Percent = 0f;
        }
        // equivalent to: udonBehavior.SetProgramVariable("IN_ChangeVolume_float_Percent", (float) volumePercent); udonBehavior.SendCustomEvent("_ChangeVolume");
        public void _ChangeVolumeTo(float volume)
        {
            IN_ChangeVolume_float_Percent = volume;
            _ChangeVolume();
        }
        public void _AudioMode3d()
        {
            if (!hasActiveManager) return;
            audio3d = true;
            activeManager._Use3dAudio();
            forwardEvent(EVENT_AUDIOMODE3D);
        }
        public void _AudioMode2d()
        {
            if (!hasActiveManager) return;
            audio3d = false;
            activeManager._Use2dAudio();
            forwardEvent(EVENT_AUDIOMODE2D);
        }
        public void _ChangeAudioModeTo(bool audio3d)
        {
            if (!hasActiveManager) return;
            this.audio3d = audio3d;
            activeManager._ChangeAudioMode(audio3d);
            forwardEvent(audio3d ? EVENT_AUDIOMODE3D : EVENT_AUDIOMODE2D);
        }
        public void _ToggleAudioMode()
        {
            _ChangeAudioModeTo(!audio3d);
        }

        public void _ReSync()
        {
            if (syncToOwner) queueSync(0f);
        }
        public void _Sync()
        {
            syncToOwner = true;
            enforceSyncTime = true;
            forwardEvent(EVENT_SYNC);
        }
        public void _DeSync()
        {
            syncToOwner = false;
            enforceSyncTime = false;
            forwardEvent(EVENT_DESYNC);
        }
        public void _ChangeSyncTo(bool sync)
        {
            if (sync) _Sync();
            else _DeSync();
        }
        public void _ToggleSync()
        {
            _ChangeSyncTo(!syncToOwner);
        }

        // equivalent to: udonBehavior.SetProgramVariable("IN_ChangeSeekTime_float_Percent", (float) seekPercent); udonBehavior.SendCustomEvent("_ChangeSeekTime");
        public void _ChangeSeekTime()
        {
            if (!hasActiveManager) return;
            BaseVRCVideoPlayer player = activeManager.player;
            float dur = player.GetDuration();
            // inifinty and 0 are livestreams, they cannot adjust seek time.
            if (dur == Mathf.Infinity || dur == 0f) return;
            player.SetTime(IN_ChangeSeekTime_float_Seconds);
            IN_ChangeSeekTime_float_Seconds = 0f;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ALL_ManualQuickSeek));
        }
        public void _ChangeSeekTimeTo(float seconds)
        {
            if (seconds > endTime) seconds = endTime;
            if (seconds < startTime) seconds = startTime;
            IN_ChangeSeekTime_float_Seconds = seconds;
            _ChangeSeekTime();
        }

        public void _ChangeSeekPercent()
        {
            // map the percent value to the range of the start and end time to the the target timestamp
            IN_ChangeSeekTime_float_Seconds = (endTime - startTime) * IN_ChangeSeekPercent_float_Percent + startTime;
            _ChangeSeekTime();
        }
        public void _ChangeSeekPercentTo(float seekPercent)
        {
            if (seekPercent > 1f) seekPercent = 1f;
            if (seekPercent < 0f) seekPercent = 0f;
            IN_ChangeSeekPercent_float_Percent = seekPercent;
            _ChangeSeekPercent();
        }

        public void _SeekForward()
        {
            if (!hasActiveManager) return;
            if (isLive) enforceSyncTime = true;
            else _ChangeSeekTimeTo(activeManager.player.GetTime() + 10f);
        }
        public void _SeekBackward()
        {
            if (!hasActiveManager) return;
            if (isLive) enforceSyncTime = true;
            else _ChangeSeekTimeTo(activeManager.player.GetTime() - 10f);
        }

        public void _Lock()
        {
            if (!hasActiveManager) return;
            if (isMasterOwner())
            {
                if (_TakeOwnership())
                {
                    locked = lockedSync = true;
                    syncData._RequestSync();
                    forwardEvent(EVENT_LOCK);
                }
            }
        }
        public void _UnLock()
        {
            if (!hasActiveManager) return;
            if (isMasterOwner())
            {
                if (_TakeOwnership())
                {
                    locked = lockedSync = false;
                    syncData._RequestSync();
                    forwardEvent(EVENT_UNLOCK);
                }
            }
        }
        public void _ChangeLockTo(bool lockActive)
        {
            if (lockActive) _Lock();
            else _UnLock();
        }
        public void _ToggleLock()
        {
            if (locked) _UnLock();
            else _Lock();
        }

        // Use this method to subscribe to the TV's event forwarding.
        // Useful for attaching multiple control panels or behaviors for various side effects to happen.
        public void _RegisterUdonEventReceiver()
        {
            UdonBehaviour target = IN_RegisterUdonEventReceiver_UdonBehavior_Subscriber;
            if (target == null) return; // called without setting the behavior
            byte priority = IN_RegisterUdonEventReceiver_byte_Priority;
            sendEvents = true;
            if (eventTargets == null)
            {
                eventTargets = new Component[0];
                eventTargetWeights = new byte[0];
            }
            int index = 0;
            for (; index < eventTargetWeights.Length; index++)
            {
                if (priority < eventTargetWeights[index]) break;
            }
            log($"Expanding event register to {eventTargets.Length + 1}: Adding {target.gameObject.name}");
            var _targets = eventTargets;
            var _weights = eventTargetWeights;
            eventTargets = new Component[_targets.Length + 1];
            eventTargetWeights = new byte[_targets.Length + 1];
            int i = 0;
            int offset = 0;
            for (; i < _targets.Length; i++)
            {
                if (i == index) offset = 1;
                eventTargets[i + offset] = _targets[i];
                eventTargetWeights[i + offset] = _weights[i];
            }
            eventTargets[index] = target;
            eventTargetWeights[index] = priority;
            IN_RegisterUdonEventReceiver_UdonBehavior_Subscriber = null;
            IN_RegisterUdonEventReceiver_byte_Priority = defaultPriority;
            // forward the ready state for registrations that happen after the TV init phase
            if (init) forwardEvent(EVENT_READY);
            // else initialize();
        }

        public void _RegisterUdonSharpEventReceiver(UdonSharpBehaviour target)
        {
            IN_RegisterUdonEventReceiver_UdonBehavior_Subscriber = (UdonBehaviour)(Component)target;
            IN_RegisterUdonEventReceiver_byte_Priority = defaultPriority;
            _RegisterUdonEventReceiver();
        }

        public void _RegisterUdonSharpEventReceiverWithPriority(UdonSharpBehaviour target, byte priority)
        {
            IN_RegisterUdonEventReceiver_UdonBehavior_Subscriber = (UdonBehaviour)(Component)target;
            IN_RegisterUdonEventReceiver_byte_Priority = priority;
            _RegisterUdonEventReceiver();
        }

        public bool _TakeOwnership()
        {
            if (!init) return false;
            if (Networking.IsOwner(gameObject)) return true; // local already owns the TV
            // TODO: implement authentication behavior callback logic here
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            Networking.SetOwner(Networking.LocalPlayer, syncData.gameObject);
            return true;
        }

        public void _RequestSync()
        {
            if (!init) return;
            if (isOwner()) syncData._RequestSync();
        }



        // === Networked methods ===

        public void ALL_ManualQuickSeek()
        {
            if (syncToOwner) queueSync(1f);
        }

        public void ALL_ManualSeek()
        {
            if (syncToOwner) queueSync(syncEnforcementTimeLimit);
        }


        // === Helper Methods ===

        private void queueRefresh(float time)
        {
            refreshAfterWait = true;
            waitUntil = Time.realtimeSinceStartup + time;
        }

        private void queueSync(float time)
        {
            enforceSyncTime = true;
            syncEnforceWait = Time.realtimeSinceStartup + time;
        }

        private Component[] expand(Component[] arr, int add)
        {
            // expand beyond size of self if children are found
            var newArray = new Component[arr.Length + add];
            var index = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null) continue;
                newArray[index] = arr[i];
                index++;
            }
            return newArray;
        }

        private byte[] expandByte(byte[] arr, int add)
        {
            // expand beyond size of self if children are found
            var newArray = new byte[arr.Length + add];
            var index = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                newArray[index] = arr[i];
                index++;
            }
            return newArray;
        }

        private void cacheVideoInfo()
        {
            videoLength = activeManager.player.GetDuration();
            isLive = videoLength == Mathf.Infinity || videoLength == 0f;
            // grab parameters
            float value;
            string param;
            // check for start param
            param = getUrlParam(urlSync, "start");
            if (float.TryParse(param, out value)) startTime = value;
            else startTime = 0f;
            // check for end param
            param = getUrlParam(urlSync, "end");
            if (float.TryParse(param, out value)) endTime = value;
            else endTime = videoLength;
            videoDuration = endTime - startTime;
            // check for loop param
            int toggle;
            param = getUrlParam(urlSync, "loop");
            int.TryParse(param, out toggle);
            bool check = toggle != 0;
            if (loop != check)
            {
                loop = check;
                forwardEvent(loop ? EVENT_ENABLELOOP : EVENT_DISABLELOOP);
            }
            // check for t or start params, only update jumpToTime if start or t succeeds
            // only parse if another jumpToTime value has not been set.
            if (jumpToTime == startTime)
            {
                param = getUrlParam(urlSync, "t");
                if (float.TryParse(param, out value)) jumpToTime = value;
            }
            urlMeta = getUrlMeta(urlSync);
            log("Params set after video is ready");
        }

        private bool isOwner() => Networking.IsOwner(gameObject);
        private bool isMasterOwner() => allowMasterControl && localPlayer.isMaster || localPlayer.isInstanceOwner;


        private void forwardEvent(string eventName)
        {
            if (sendEvents && haltEvent(eventName))
            {
                log($"Forwarding event {eventName} to {eventTargets.Length} listeners");
                foreach (var target in eventTargets)
                    if (target != null)
                        ((UdonBehaviour)target).SendCustomEvent(eventName);
            }
            releaseEvent(eventName);
        }

        private void forwardVariable(string variableName, object value)
        {
            if (sendEvents && variableName != null)
            {
                log($"Forwarding variable {variableName} to {eventTargets.Length} listeners");
                foreach (var target in eventTargets)
                    if (target != null)
                        ((UdonBehaviour)target).SetProgramVariable(variableName, value);
            }
        }

        // These two methods are used to prevent recursive event propogation between the TV and subscribed behaviors.
        // Only allows for 1 depth of calling an event before releasing from it's own context.
        private bool haltEvent(string eventName)
        {
            int insert = -1;
            for (int i = 0; i < haltedEvents.Length; i++)
            {
                if (haltedEvents[i] == eventName) return false;
                if (insert == -1 && haltedEvents[i] == null) insert = i;
            }
            haltedEvents[insert] = eventName;
            return true;
        }

        private void releaseEvent(string eventName)
        {
            for (int i = 0; i < haltedEvents.Length; i++)
            {
                haltedEvents[i] = null;
            }
        }

        private void setLoadingState(bool yes)
        {
            loading = yes;
            if (isOwner()) loadingSync = loading;
            if (loading) forwardEvent(EVENT_LOADING);
            else forwardEvent(EVENT_LOADINGEND);
        }

        private string getUrlDomain(VRCUrl url)
        {
            // strip the protocol
            var s = url.Get().Split(new string[] { "://" }, 2, System.StringSplitOptions.None);
            if (s.Length == 1) return string.Empty;
            // strip everything after the first slash
            s = s[1].Split(new char[] { '/' }, 2, System.StringSplitOptions.None);
            // just to be sure, strip everything after the question mark if one is present
            s = s[0].Split(new char[] { '?' }, 2, System.StringSplitOptions.None);
            // return the url's domain value
            return s[0];
        }

        private int getUrlParamAsInt(VRCUrl url, string name, int _default)
        {
            string param = getUrlParam(url, name);
            int value;
            return System.Int32.TryParse(param, out value) ? value : _default;
        }


        private string getUrlParam(VRCUrl url, string name)
        {
            // strip everything before the query parameters
            string[] s = url.Get().Split(new char[] { '?' }, 2, System.StringSplitOptions.None);
            if (s.Length == 1) return string.Empty;
            // just to be sure, strip everything after the url bang if one is present
            s = s[1].Split(new char[] { '#' }, 2, System.StringSplitOptions.None);
            // attempt to find parameter name match
            s = s[0].Split('&');
            foreach (string param in s)
            {
                string[] p = param.Split(new char[] { '=' }, 2, System.StringSplitOptions.None);
                if (p[0] == name) return p[1];
            }
            // if one can't be found, return an empty string
            return string.Empty;
        }

        private string[] getUrlMeta(VRCUrl url)
        {
            string[] s = url.Get().Split(new char[] { '#' }, 2, System.StringSplitOptions.None);
            if (s.Length == 1) return new string[0];
            return s[1].Split(';');
        }


        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ff00>TVManagerV2 ({name})</color>] {value}");
        }
        private void warn(string value)
        {
            if (debug) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ff00>TVManagerV2 ({name})</color>] {value}");
        }
        private void err(string value)
        {
            if (debug) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#00ff00>TVManagerV2 ({name})</color>] {value}");
        }
    }
}
