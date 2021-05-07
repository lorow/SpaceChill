
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerInteraction : UdonSharpBehaviour
{
    public SphereCollider sphereColliderPrefab;

    [Space(10)]
    public bool  wristColliders           = true;
    public bool  indexFingerColliders     = true;
    public float wristColliderScale       = 1.0f;
    public float indexFingerColliderScale = 0.9f;

    [Space(10)]
    [Tooltip("Show/hide colliders meshes")]
    public bool visible = false;
    bool _wasVisible;

    [Space(10)]
    [Tooltip("When enabled, will use 'physical' buttons if player avatar supports hand colliders,\neither will switch buttons to 'laser pointer' mode.\nWhen disabled, player will have to switch buttons mode manually")]
    public bool autoSwitchButtonsMode = true;
    [Tooltip("Use 'laser pointer' mode by default for VR users?\nCan be switched later by calling appropriate SetButtonsModeX() method")]
    public bool useLegacyModeByDefault = false;
    [Tooltip("When set, appropriate game object will be activated (for VR users)")]
    public GameObject buttonsModeSettings;
    public GameObject buttonsModeSettingsLegacy;
    public GameObject buttonsModeSettingsPhysical;
    public PIPushButton[] buttons;
    
    bool _avatarSupported;

    VRCPlayerApi _localPlayer;
    bool isEditor;
    bool isUserInVR;

    SphereCollider[] _colliders = new SphereCollider[4];

    bool _buttonsConfigured;
    bool _configuredForVR;

    int updated = -1;

    void Start()
    {
        _localPlayer = Networking.LocalPlayer;
        isEditor     = (_localPlayer == null);
        isUserInVR   = (_localPlayer != null && _localPlayer.IsUserInVR());

        if (isUserInVR && sphereColliderPrefab != null) {
            // generate wrist & finger colliders for local player
            sphereColliderPrefab.gameObject.SetActive(false);

            for (int i = 0; i < 4; i++) {
                var obj = VRCInstantiate(sphereColliderPrefab.gameObject);
                obj.transform.parent = this.transform;
                _colliders[i] = obj.GetComponent<SphereCollider>();
            }
            _colliders[0].name = "PlayerInteractionCollider_Wrist_L";
            _colliders[1].name = "PlayerInteractionCollider_Wrist_R";
            _colliders[2].name = "PlayerInteractionCollider_Index_L";
            _colliders[3].name = "PlayerInteractionCollider_Index_R";

            _wasVisible = !visible;
            _configuredForVR = true;
        }
        else if (!isEditor) {
            if (buttonsModeSettings != null)
                buttonsModeSettings.SetActive(false);
        }
    }

    public void SetButtonsModeLegacy()
    {
        SetButtonsMode(0);
    }

    public void SetButtonsModePhysical()
    {
        SetButtonsMode(1);
    }
    
    public void SetButtonsMode(int mode)
    {
        //Debug.Log("SetButtonsMode(): " + mode);
        if (buttonsModeSettingsLegacy != null)
            buttonsModeSettingsLegacy.SetActive(mode == 0);
        if (buttonsModeSettingsPhysical != null)
            buttonsModeSettingsPhysical.SetActive(mode != 0);

        if (buttons != null) {
            foreach (var b in buttons) {
                if (b != null)
                    b.SetButtonMode(mode);
            }
        }
    }

    void Update()
    {
        updated++;

        if (updated < 1) {
            return;  // skip one frame
        }
        else if (!_buttonsConfigured) {
            SetButtonsMode(_configuredForVR && !useLegacyModeByDefault ? 1 : 0);
            _buttonsConfigured = true;
        }

        if (!_configuredForVR)
            return;

        if (_wasVisible != visible) {
            // show/hide colliders meshes
            for (int i = 0; i < 4; i++)
                _colliders[i].GetComponent<MeshRenderer>().enabled = visible;

            _wasVisible = visible;
        }

        bool success = false;

        if (wristColliders) {
            success = (
                UpdateColliderTransform(_colliders[0].transform, HumanBodyBones.LeftHand,  HumanBodyBones.LeftMiddleIntermediate,  0.5f, wristColliderScale * 0.5f) &&
                UpdateColliderTransform(_colliders[1].transform, HumanBodyBones.RightHand, HumanBodyBones.RightMiddleIntermediate, 0.5f, wristColliderScale * 0.5f)
                );
        }
        if (success && indexFingerColliders) {
            success = (
                UpdateColliderTransform(_colliders[2].transform, HumanBodyBones.LeftIndexIntermediate,  HumanBodyBones.LeftIndexDistal,  1.0f, indexFingerColliderScale * 2.0f) &&
                UpdateColliderTransform(_colliders[3].transform, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal, 1.0f, indexFingerColliderScale * 2.0f)
                );
        }

        if (success != _avatarSupported) {
            _colliders[0].gameObject.SetActive(success && wristColliders);
            _colliders[1].gameObject.SetActive(success && wristColliders);
            _colliders[2].gameObject.SetActive(success && indexFingerColliders);
            _colliders[3].gameObject.SetActive(success && indexFingerColliders);

            if (autoSwitchButtonsMode)
                SetButtonsMode(success ? 1 : 0);
        }
        _avatarSupported = success;
    }

    bool UpdateColliderTransform(Transform collider, HumanBodyBones bone0, HumanBodyBones bone1, float pos, float rad)
    {
        var p1 = _localPlayer.GetBonePosition(bone1);
        
        // check if bone position is not zero
        if (p1.sqrMagnitude < 0.000001f)
            return false;
        
        var p0 = _localPlayer.GetBonePosition(bone0);
        var v = p1 - p0;
        var d = v.magnitude;
        
        collider.localScale = Vector3.one * (d * rad);
        collider.position   = Vector3.Lerp(p0, p1, pos);
        return true;
    }
}
