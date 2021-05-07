using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PIButton : UdonSharpBehaviour
{
    [Tooltip("Game object to toggle")]
    public GameObject toggleObject;

    [Tooltip("More game objects to toggle")]
    public GameObject[] toggleObjects;

    [Space(10)]
    [Tooltip("Target script")]
    public UdonBehaviour target;

    [Tooltip("Toggle boolean property on the target script")]
    public string toggleProperty;
    
    [Tooltip("Calling method (custom event) on the target script")]
    public string callMethod;

    [Space(10)]
    [Tooltip("0 - Off, 1 - On, else Toggle")]
    public int operation = -1;
    
    [Space(10)]
    public AudioSource pressAudio;

    VRCPlayerApi _localPlayer;

    void Start()
    {
        _localPlayer = Networking.LocalPlayer;
    }

    void OnMouseDown()
    {
        if (_localPlayer == null)
            DoInteract();
    }

    public override void Interact()
    {
        DoInteract();
    }
    
    public void DoInteract()
    {
        PlayAudio(pressAudio);

        if (toggleObject != null) {
            if (operation == 0)
                toggleObject.SetActive(false);
            else if (operation == 1)
                toggleObject.SetActive(true);
            else
                toggleObject.SetActive(!toggleObject.activeSelf);
        }

        if (toggleObjects != null && toggleObjects.Length > 0) {
            foreach (var obj in toggleObjects) {
                if (obj != null) {
                    if (operation == 0)
                        obj.SetActive(false);
                    else if (operation == 1)
                        obj.SetActive(true);
                    else
                        obj.SetActive(!obj.activeSelf);
                }
            }
        }

        if (target != null) {
            if (toggleProperty != null && toggleProperty.Length > 0) {
                if (operation == 0)
                    target.SetProgramVariable(toggleProperty, false);
                else if (operation == 1)
                    target.SetProgramVariable(toggleProperty, true);
                else {
                    bool active = !(bool)target.GetProgramVariable(toggleProperty);
                    target.SetProgramVariable(toggleProperty, active);
                }
            }

            if (callMethod != null && callMethod.Length > 0)
                target.SendCustomEvent(callMethod);
        }
    }

    void PlayAudio(AudioSource audio)
    {
        if (audio != null)
            audio.Play();
    }
}
