
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class VIbrateOnGrab : UdonSharpBehaviour
{
    public VRCPickup pickup;
    public bool hasSentVibration = false;
    
    private void Update()
    {
        if (pickup.IsHeld && !hasSentVibration)
        {
            pickup.PlayHaptics();
            hasSentVibration = true;
        }

        if (hasSentVibration && !pickup.IsHeld)
        {
            hasSentVibration = false;
        }
    }
}
