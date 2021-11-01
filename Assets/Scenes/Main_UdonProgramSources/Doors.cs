
using Thry;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Doors : UdonSharpBehaviour
{
    public Collider playerTrigger;
    public Transform leftDoor;
    public Transform rightDoor;
    public int doorOffset = 3;
    public float slideTime = 5;
    private float allowedOffset = 0.001f;

    private void handleDoorPosition(Transform door, int slideVector)
    {
        if (door == null)
            return;

        var doorPosition = door.position;
        var desiredDoorPosition = new Vector3((doorPosition.x - (slideVector * allowedOffset)), doorPosition.y, doorPosition.z);
        // TODO add actual slide mechanic   
        
    }
    
    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        handleDoorPosition(leftDoor, -1);
        handleDoorPosition(rightDoor, 1);
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        handleDoorPosition(leftDoor, 1);
        handleDoorPosition(rightDoor,-1);
    }
}
