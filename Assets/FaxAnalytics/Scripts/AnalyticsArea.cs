using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// An area which the player may enter to trigger analytics.
/// Must be entered into AnalyticsManger's inspector to work.
/// If the trigger collider is not set, then it will always be active (unless the GameObject is disabled)
/// </summary>
public class AnalyticsArea : UdonSharpBehaviour
{
    /// <summary>
    /// Enter your URL here.
    /// It will be opened when a player enters the trigger.
    /// For example, you could put a prefilled Google Form here!
    /// </summary>
    [Header("Enter your URL here. Make sure to read ReadMe.txt")]
    [Tooltip("The URL to open when the player enters this trigger.")]
    public VRCUrl url = null;
    
    [Tooltip("The collider which triggers the analytics area.")]
    [SerializeField] private Collider trigger = null;

    /// <summary>
    /// True if the player is presently inside this trigger.
    /// </summary>
    private bool playerIsHere = false;

    /// <summary>
    /// True if this area has been triggered, but not yet checked by the AnalyticsManager.
    /// </summary>
    private bool triggered = false;

    private bool initialized = false;

    private void Start()
    {
        Debug.Log("Starting Analytics: " + gameObject.name);
       
        if (url == null || url.ToString().Equals(""))
        {
            Debug.LogError("An analytics area has no URL! Make sure to add one. Check the ReadMe for details.");
        }
        
        if (trigger == null)
        {
            Debug.Log($"AreaAnalytics {gameObject.name} has no collider. It's triggered instantly. " +
                      "Don't forget to add it to the AnalyticsManager!");
            Trigger();
        }
        else if (trigger.isTrigger == false)
        {
            Debug.LogError("AreaAnalytics must have \"Is Trigger\" enabled!");
        }

        initialized = true;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) return;
        Debug.Log("Player entered area " + gameObject.name);
        Trigger();

    }

    /// <summary>
    /// Disabled object cannot be triggered again, unless the GameObject is re-enabled.
    /// We set triggered to 'false', resetting the object. Just in case it every gets re-enabled.
    /// </summary>
    private void OnDisable()
    {
        triggered = false;
    }

    /// <summary>
    /// Analytics areas which trigger only once can be re-enabled to let them trigger again. 
    /// </summary>
    private void OnEnable()
    {
        if (initialized && trigger == null && !triggered)
        {
            Debug.Log($"AreaAnalytics {gameObject.name} has no collider. It's triggered instantly." +
                      ".. Again!");
            Trigger();
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) return;
        Debug.Log("Player left area " + gameObject.name);
        playerIsHere = false;
    }

    private void Trigger()
    {
        playerIsHere = true;
        triggered = true;
    }

    public bool IsPlayerHere()
    {
        return playerIsHere;
    }

    public bool HasBeenTriggered()
    {
        return triggered;
    }
}
