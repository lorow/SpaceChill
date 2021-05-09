
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class MenuActivator : UdonSharpBehaviour
{
    public MenuHandler menuHandler;
    public GameObject menu;
    private void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        menuHandler.SetActiveMenu(menu);
    }
}
