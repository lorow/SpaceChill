
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PIPushButton : UdonSharpBehaviour
{
    public UdonBehaviour button;

    public Transform travelEndPoint;

    [Tooltip("Enter value from 0 to 1")]
    public float actuationPoint = 0.6f;
    
    [Tooltip("Name of the custom event\n(standard 'Interact' will not work)")]
    public string pressMethod   = "DoInteract";
    public string unpressMethod = "";

    Vector3 _initialButtonLocalPos;
    float   _travelDistance;
    float   _actuationDistance;

    bool _configured;
    bool _pressed;
    bool _used;

    SphereCollider _sphereCollider;  // best for round buttons
    BoxCollider    _boxCollider;     // best for square buttons

    Collider _otherCollider;

    void Start()
    {
        var localPlayer = Networking.LocalPlayer;
        bool switchToLegacyMode = (localPlayer != null && !localPlayer.IsUserInVR());
        
        var  colliders = GetComponents<Collider>();
        bool foundCollider = false;

        if (colliders != null) {
            foreach (var col in colliders) {
                if (switchToLegacyMode || foundCollider)
                    col.enabled = false;
                else if (col.GetType() == typeof(SphereCollider)) {
                    _sphereCollider = (SphereCollider)col;
                    foundCollider = true;
                }
                else if (col.GetType() == typeof(BoxCollider)) {
                    _boxCollider = (BoxCollider)col;
                    foundCollider = true;
                }
            }
        }
        
        _configured = ((_sphereCollider != null || _boxCollider != null) && button != null && travelEndPoint != null);

        if (_configured) {
            var bt = button.gameObject.transform;
            _initialButtonLocalPos = bt.localPosition;
            _travelDistance    = Vector3.Distance(bt.position, travelEndPoint.position);
            _actuationDistance = _travelDistance * Mathf.Clamp01(actuationPoint);

            var buttonCollider = bt.GetComponent<Collider>();
            if (buttonCollider != null)
                buttonCollider.enabled = switchToLegacyMode;
        }
    }

    public void SetButtonModeLegacy()
    {
        SetButtonMode(0);
    }

    public void SetButtonModePhysical()
    {
        SetButtonMode(1);
    }

    public void SetButtonMode(int mode)
    {
        bool useLegacyMode = (mode == 0);
        
        if (_sphereCollider != null)
            _sphereCollider.enabled = !useLegacyMode;
        else if (_boxCollider != null)
            _boxCollider.enabled = !useLegacyMode;
        
        if (button != null) {
            var buttonCollider = button.gameObject.GetComponent<Collider>();
            if (buttonCollider != null)
                buttonCollider.enabled = useLegacyMode;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter() " + this.name + " | " + other.name);
        if (_configured && _otherCollider == null && IsValidCollider(other))
            InteractWithCollider((SphereCollider)other, false);
    }

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("OnTriggerStay() " + this.name + " | " + other.name);
        if (_configured && other == _otherCollider)
            InteractWithCollider((SphereCollider)other, false);
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("OnTriggerExit() " + this.name + " | " + other.name);
        if (_configured && other == _otherCollider) {
            button.transform.localPosition = _initialButtonLocalPos;
            _otherCollider = null;
            _used = false;
        }
    }

    bool IsValidCollider(Collider col)
    {
        return (col != null && col.name.IndexOf("PlayerInteractionCollider") >= 0 && col.GetType() == typeof(SphereCollider));
    }

    void InteractWithCollider(SphereCollider otherCollider, bool exit)
    {
        //Debug.Log("InteractWithCollider: " + col.name);
        
        button.transform.localPosition = _initialButtonLocalPos;

        var t1 = otherCollider.transform;
        var s1 = Mathf.Max(t1.lossyScale.x, Mathf.Max(t1.lossyScale.y, t1.lossyScale.z));
        var p1 = t1.position + t1.rotation * (otherCollider.center * s1);
        var r1 = otherCollider.radius * s1;

        var t0 = transform;
        var up = t0.up;

        float d = 0f;

        if (_sphereCollider != null) {
            var s0 = Mathf.Max(t0.lossyScale.x, Mathf.Max(t0.lossyScale.y, t0.lossyScale.z));
            var p0 = t0.TransformPoint(_sphereCollider.center);
            var r0 = _sphereCollider.radius * s0;
            d = r0 + r1 - Vector3.Distance(p0, p1);
        }
        else {
            var p0 = t0.TransformPoint(_boxCollider.center);
            var vec = p1 - p0;
            var hs = Vector3.Scale(_boxCollider.size, t0.lossyScale) * 0.5f;
            var dx = Vector3.Dot(vec, t0.right);
            var dz = Vector3.Dot(vec, t0.forward);
            var kx = 1f - Mathf.Max((Mathf.Abs(dx) - hs.x) / r1, 0f);
            var kz = 1f - Mathf.Max((Mathf.Abs(dz) - hs.z) / r1, 0f);
            d = (hs.y + r1 - Vector3.Dot(vec, up)) * kx * kz;
        }

        var dy = Vector3.Dot(p1 - travelEndPoint.position, up);
        //Debug.Log("dy = " + dy + ", d = " + d);

        // ignore collisions that came from behind of the object
        //   or when the button was pushed too far
        if (_otherCollider == null && dy < _travelDistance * 0.5f)
            return;

        _otherCollider = otherCollider;

        if (d > 0f)
            button.transform.position -= up * Mathf.Min(d, _travelDistance);

        float err = _travelDistance * 0.025f;

        if (!_used && !_pressed && d > _actuationDistance + err) {
            _pressed = true;
            _used = true;
            //Debug.Log("Pressed");
            if (pressMethod.Length > 0)
                button.SendCustomEvent(pressMethod);
        }
        else if (_pressed && d < _actuationDistance - err) {
            _pressed = false;
            //Debug.Log("Unpressed");
            if (unpressMethod.Length > 0)
                button.SendCustomEvent(unpressMethod);
        }
    }
}
