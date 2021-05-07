
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class MenuActivator : UdonSharpBehaviour
{
    public MenuHandler menuHandler;
    public string menuName;
    private void OnTriggerEnter(Collider other)
    {
        menuHandler = other.gameObject.GetComponent<MenuHandler>();
        menuHandler.SetActiveMenu(menuName);
    }
}
