
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [DefaultExecutionOrder(-1)]
    public class QuickPlay : UdonSharpBehaviour
    {
        public TVManagerV2 tv;
        public VRCUrl url = VRCUrl.Empty;
        public VRCUrl altUrl = VRCUrl.Empty;
        public string title;
        private VRCPlayerApi local;
        private bool init = false;
        private bool debug = false;
        private string debugColor = "#ffaa66";

        public void _Initialize()
        {
            if (init) return;

            local = Networking.LocalPlayer;
            tv._RegisterUdonSharpEventReceiverWithPriority(this, 200);
            init = true;
        }

        void Start()
        {
            _Initialize();
            var shapes = GetComponentsInChildren(typeof(VRC_UiShape));
            foreach (Component s in shapes)
            {
                var box = s.GetComponent<BoxCollider>();
                var rect = s.GetComponent<RectTransform>();
                if (box != null)
                {
                    log("Auto-adjusting Canvas collider");
                    box.isTrigger = true;
                    box.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, (rect.sizeDelta.x + rect.sizeDelta.y) / 2);
                }
            }
        }

        new void Interact()
        {
            _Activate();
        }

        public void _Activate()
        {
            tv._ChangeMediaToWithAlt(url, altUrl);
        }

        public void _TvMediaStart()
        {
            var tvUrl = tv.url.Get();
            if (tvUrl == url.Get() || tvUrl == altUrl.Get())
                tv.localLabel = title;
        }

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(QuickPlay)} ({name})</color>] {value}");
        }
        private void warn(string value)
        {
            if (debug) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(QuickPlay)} ({name})</color>] {value}");
        }
        private void err(string value)
        {
            if (debug) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(QuickPlay)} ({name})</color>] {value}");
        }
    }
}
