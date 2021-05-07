using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerHeightDetector : UdonSharpBehaviour
{
    public GameObject[] adjustObjectsPosition;
    public float verticalPosError = 0.05f;
    public float maxVerticalPos   = 2.0f;
    public float minVerticalPos   = 0.5f;
    public float updateInterval   = 1.0f;

    [Space(10)]
    [Tooltip("Move assigned camera object up & down to test behaviour in Unity")]
    public GameObject editorCamera;

    VRCPlayerApi _localPlayer;
    bool _configured;

    float[] _defaultObjectsPosition;
    float _updateLastTime;

    void Start()
    {
        _localPlayer = Networking.LocalPlayer;
        _configured  = (adjustObjectsPosition != null && adjustObjectsPosition.Length > 0);

        if (_configured) {
            _defaultObjectsPosition = new float[adjustObjectsPosition.Length];
            for (int i = 0; i < adjustObjectsPosition.Length; i++) {
                if (adjustObjectsPosition[i] != null)
                    _defaultObjectsPosition[i] = adjustObjectsPosition[i].transform.position.y;
            }
        }
    }
    
    void Update()
    {
        if (!_configured)
            return;

        float t = Time.realtimeSinceStartup;
        if (t - _updateLastTime < updateInterval)
            return;

        var h = MeasurePlayerHeight(_localPlayer);

        for (int i = 0; i < adjustObjectsPosition.Length; i++) {
            var obj = adjustObjectsPosition[i];
            if (obj != null) {
                var pos = obj.transform.position;
                if (h < 0.001f) {
                    // reset to initial position
                    pos.y = _defaultObjectsPosition[i];
                    obj.transform.position = pos;
                }
                else {
                    var dy = pos.y - h;
                    if (Mathf.Abs(dy) >= verticalPosError) {
                        // readjust object position
                        pos.y = Mathf.Clamp(h, minVerticalPos, maxVerticalPos);
                        obj.transform.position = pos;
                    }
                }
            }
        }
        _updateLastTime = t;
    }

    float MeasurePlayerHeight(VRCPlayerApi p)
    {
        if (p == null)
            return (editorCamera != null ? editorCamera.transform.position.y : 0f);
        
        // check if avatar is invalid
        if (p.GetBonePosition(HumanBodyBones.Head).sqrMagnitude      < 0.000001f ||
            p.GetBonePosition(HumanBodyBones.LeftFoot).sqrMagnitude  < 0.000001f ||
            p.GetBonePosition(HumanBodyBones.RightFoot).sqrMagnitude < 0.000001f) {
            return 0f;
        }

        var pLegL  = p.GetBonePosition(HumanBodyBones.LeftUpperLeg);
        var pLegR  = p.GetBonePosition(HumanBodyBones.RightUpperLeg);
        var pSpine = p.GetBonePosition(HumanBodyBones.Spine);

        var legsMP = Vector3.LerpUnclamped(pLegL, pLegR, 0.5f);

        float lenLegL  = MeasureBoneLength2(p, HumanBodyBones.LeftUpperLeg,  HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftFoot);
        float lenLegR  = MeasureBoneLength2(p, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
        float spineLen = MeasureBoneLength(p, HumanBodyBones.Spine, HumanBodyBones.Head) + Vector3.Distance(legsMP, pSpine);

        float legsLen = Mathf.Max(lenLegL, lenLegR);
        float totalLen = legsLen + spineLen;
        
        return (totalLen > 0.001f ? totalLen : 1f);
    }

    float MeasureBoneLength(VRCPlayerApi p, HumanBodyBones b0, HumanBodyBones b1)
    {
        return Vector3.Distance(p.GetBonePosition(b0), p.GetBonePosition(b1));
    }

    float MeasureBoneLength2(VRCPlayerApi p, HumanBodyBones b0, HumanBodyBones b1, HumanBodyBones b2)
    {
        var p1 = p.GetBonePosition(b1);
        return Vector3.Distance(p.GetBonePosition(b0), p1) + Vector3.Distance(p1, p.GetBonePosition(b2));
    }
}
