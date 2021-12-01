
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SlidingDoorSystem : UdonSharpBehaviour
{
    public Vector3 doorOffset;
    public Transform leftDoor;
    public Transform rightDoor;
    public float slideTime = 10;
    
    private Vector3 _desiredLeftPosition;
    private Vector3 _desiredRightPosition;
    
    private Vector3 _desiredLeftPositionOpen;
    private Vector3 _desiredRightPositionOpen;
    
    private Vector3 _startingPositionLeft;
    private Vector3 _startingPositionRight;
    public void Start()
    {
        _startingPositionLeft = _desiredLeftPosition = leftDoor.localPosition;
        _startingPositionRight = _desiredRightPosition=  rightDoor.localPosition;
        
        _desiredLeftPositionOpen = new Vector3(_startingPositionLeft.x + doorOffset.x, _startingPositionLeft.y + doorOffset.y, _startingPositionLeft.z + doorOffset.z);
        _desiredRightPositionOpen = new Vector3(_startingPositionRight.x - doorOffset.x, _desiredRightPosition.y - doorOffset.y, _desiredRightPosition.z - doorOffset.z);
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        _desiredLeftPosition = _desiredLeftPositionOpen;
        _desiredRightPosition = _desiredRightPositionOpen;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        _desiredLeftPosition = _startingPositionLeft;
        _desiredRightPosition = _startingPositionRight;
    }

    private void Update()
    {
        if (!(Vector3.Distance(leftDoor.localPosition, _desiredLeftPosition) > Vector3.kEpsilon) &&
            !(Vector3.Distance(rightDoor.localPosition, _desiredRightPosition) > Vector3.kEpsilon)) return;

        var step = Mathf.SmoothStep(0.0f, 1.0f, Time.deltaTime * slideTime);
        
        leftDoor.localPosition = Vector3.Lerp(
            leftDoor.localPosition,
            _desiredLeftPosition,
            step
        );
        
        rightDoor.localPosition = Vector3.Lerp(
            rightDoor.localPosition,
            _desiredRightPosition,
            step
        );
    }
}
