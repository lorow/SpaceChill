using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

/// <summary>
/// Manages other analytics objects.
/// Currently supports analytics areas, which can be used to track which areas of the map players visit.
/// areasToCheckPeriodically contains a list of areas to check every X seconds.
/// areasToCheckWhenThePlayerFirstEntersThem are only activated once, when the player first enters them.
/// </summary>
[RequireComponent(typeof(BaseVRCVideoPlayer))]
public class AnalyticsManager : UdonSharpBehaviour
{
    [Header("Areas to keep track of")]

    [Tooltip("An array of all analytics areas you'd like to track.")]
    [SerializeField] private AnalyticsArea[] areasToCheckPeriodically = null;
    
    [Tooltip("An array of all analytics areas you'd like to track.")]
    [SerializeField] private AnalyticsArea[] areasToCheckOnceWhenTriggered = null;
    
    [Header("Settings")]
    
    [Tooltip("In seconds, how frequently to check which area the player is currently located in.")][UnityEngine.Range(5,600)]
    [SerializeField] private double periodicCheckFrequency = 15f;
    
    private double onceCheckFrequency = 1f;

    [Tooltip("If the player is not in an area listed in areasToCheckPeriodically, this URL will be opened instead.")]
    [SerializeField] private VRCUrl UrlPlayerIsInBetweenAreas = null;
    
    [Header("User Feedback")]
    
    [Tooltip("The GameObject to show when a connection error has occured")]
    [SerializeField] private GameObject[] showOnError = null;
    
    [Tooltip("The GameObject to show when the connection test is being performed")]
    [SerializeField] private GameObject[] showOnConnecting = null;

    [Tooltip("The GameObject to show when analytics are ready.")]
    [SerializeField] private GameObject[] showOnReady = null;


    [Header("Other references")]
    [Tooltip("The video player to repurpose for opening URLs")]
    [SerializeField] private BaseVRCVideoPlayer videoPlayer = null;

    [Tooltip("The URL to use for the connection test. Must point to a video file from a host that isn't whitelisted.")]
    [SerializeField] private VRCUrl connectionTestUrl = null;

    /// <summary>
    /// The last URL the player has queued. Used to re-submit a URL after establishing the connection.
    /// </summary>
    private VRCUrl queuedUrl = null;
    
    /// <summary>
    /// True if the connection test has been performed successfully.
    /// </summary>
    private bool _connectionTestOk = false;

    private bool _isPerformingConnectionTest = false;

    /// <summary>
    /// Timer for areas which are checked periodically.
    /// </summary>
    private double timerPeriodic;

    private double timerOnce;

    /// <summary>
    /// Set up the timers and start an initial connection test, in case the player has untrusted URLs allowed.
    /// </summary>
    void Start()
    {
        PerformConnectionTest();
    }

    /// <summary>
    /// Iterate through all areas and activate those that were triggered.
    /// This runs on a timer to save performance (and to prevent millions of Google Forms entries)
    /// </summary>
    private void Update()
    {
        timerPeriodic += Time.deltaTime;

        // Periodic check
        if (timerPeriodic >= periodicCheckFrequency)
        {
            var isPlayerInArea = false;
            
            timerPeriodic -= periodicCheckFrequency;
            foreach (var area in areasToCheckPeriodically)
            {
                if (area == null) continue;
                if (!area.gameObject.activeSelf) continue;
                if (!area.IsPlayerHere()) continue;
                Submit(area.url, false);
                isPlayerInArea = true;
                break;
            }
            
            if (!isPlayerInArea && UrlPlayerIsInBetweenAreas != null) Submit(UrlPlayerIsInBetweenAreas, false);
        }

        timerOnce += Time.deltaTime;
        
        // Once check
        if (timerOnce >= onceCheckFrequency)
        { 
            timerOnce -= onceCheckFrequency;
            foreach (var area in areasToCheckOnceWhenTriggered)
            {
                if (area == null) continue;
                if (!area.gameObject.activeSelf) continue;
                if (!area.HasBeenTriggered()) continue;
                area.gameObject.SetActive(false);
                Submit(area.url, false);
            }
        }
    }

    /// <summary>
    /// Attempts to open a URL, or starts the connection test if it hasn't suceeded yet.
    /// </summary>
    /// <param name="url">The URL to attempt to open</param>
    /// <param name="queue">If true, then the URL will be re-submitted after the connection test succeeds.
    /// Use this for "reconnect" type buttons.</param>
    public void Submit(VRCUrl url, bool queue)
    {
        if (_connectionTestOk)
        {
            Debug.Log("Submitting an Analytics URL. This will cause video player errors. (Which is fine)");
            videoPlayer.LoadURL(url);
            return;
        }

        if (queue) queuedUrl = url;
        PerformConnectionTest();
    }

    /// <summary>
    /// Test the connection by attempting to load an .mp4 URL which is not whitelisted.
    /// </summary>
    private void PerformConnectionTest()
    {
        if (_isPerformingConnectionTest) return;
        Debug.Log("Performing connection test with test URL...");
        SetActive(showOnConnecting, true);
        SetActive(showOnError, false);
        _isPerformingConnectionTest = true;
        videoPlayer.LoadURL(connectionTestUrl);
    }

    /// <summary>
    /// The test .mp4 was loaded successfully, so URL submission will now be allowed.
    /// </summary>
    public override void OnVideoReady()
    {
        _isPerformingConnectionTest = false;
        if (_connectionTestOk)
        {
            Debug.LogWarning("The connection test has already succeeded once, but another video was loaded. Odd.");
        }
        
        Debug.Log("SUCCESS! The player has allowed untrusted URLs. Analytics ready.");
        _connectionTestOk = true;
        SetActive(showOnConnecting,false);
        SetActive(showOnError,false);
        SetActive(showOnReady, true);

        if (queuedUrl != null)
        {
            Submit(queuedUrl, false);
            queuedUrl = null;
        }
    }

    /// <summary>
    /// This error is to be expected - unless it's part of the connection test!
    /// If the test .mp4 did not load, then untrusted URLs are not enabled.
    /// </summary>
    public override void OnVideoError(VideoError videoError)
    {
        _isPerformingConnectionTest = false;
        SetActive(showOnConnecting, false);
        if (_connectionTestOk) return;
        
        SetActive(showOnError, true);
        SetActive(showOnConnecting, false);
        Debug.Log("Either the player has untrusted URLs disabled, or the test URL is not a valid mp4 file.");
    }

    /// <summary>
    /// Mass-enable or disable an array of GameObjects.
    /// </summary>
    /// <param name="gameObjects">The GameObjects to call SetActive(active) on</param>
    /// <param name="active">Whether to activate the GameObjects.</param>
    private void SetActive(GameObject[] gameObjects, bool active)
    {
        foreach (var g in gameObjects)
        {
            g.SetActive(active);
        }
    }
}
