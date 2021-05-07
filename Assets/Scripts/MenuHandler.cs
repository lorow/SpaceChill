
using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UnityEditor.PackageManager;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MenuHandler : UdonSharpBehaviour
{
    public Dictionary<string, GameObject> availableMenus;
    public GameObject activeMenu;

    public void SetActiveMenu(string menuName)
    {
        if (menuName.Length == 0) throw new Exception("Menu name was not provided, something went wrong");
        if (!availableMenus.Keys.Contains(menuName)) return;
        
        // first disable the old menu, then enable the new one
        activeMenu.SetActive(false);
        activeMenu = availableMenus[menuName];
        activeMenu.SetActive(true);
    }
}
