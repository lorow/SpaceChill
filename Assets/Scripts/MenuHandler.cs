
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
    public GameObject[] availableMenus;
    public GameObject activeMenu;

    public void SetActiveMenu(GameObject menu)
    {
        Debug.Log(menu.name);
        if (!menu) Debug.Log("Menu name was not provided, something went wrong");
        
        // UDON does not support lambdas or Lists yet so Array.exists is out
        foreach (var avaliableMenu in availableMenus)
        {
            if (avaliableMenu == menu)
            {
                // first disable the old menu, then enable the new one
                activeMenu.SetActive(false);
                activeMenu = menu;
                activeMenu.SetActive(true);
                Debug.Log($"Set {menu.name}");
            }
        }
    }
}
