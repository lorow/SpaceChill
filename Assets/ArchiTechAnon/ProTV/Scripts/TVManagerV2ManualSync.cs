
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-9998)] // needs to initialize just after any TVManagerV2 script
    public class TVManagerV2ManualSync : UdonSharpBehaviour
    {
        private TVManagerV2 tv;

        [UdonSynced] private int state = 0;
        [UdonSynced] private VRCUrl urlMain = VRCUrl.Empty;
        [UdonSynced] private VRCUrl urlAlt = VRCUrl.Empty;
        [UdonSynced] private bool locked = false;
        [UdonSynced] private int urlRevision = 0;
        [UdonSynced] private bool loading = false;
        private bool debug = false;


        new void OnPreSerialization()
        {
            // Extract data from TV for manual sync
            log($"PreSerialization: ownerState {state} | locked {locked} | urlRevision {urlRevision}");
            log($"Main URL {urlMain} | Alt URL {urlAlt}");
            state = tv.stateSync;
            urlMain = tv.urlMainSync;
            urlAlt = tv.urlAltSync;
            locked = tv.lockedSync;
            urlRevision = tv.urlRevisionSync;
            loading = tv.loadingSync;
        }

        new void OnDeserialization()
        {
            log($"Deserialization: ownerState {state} | locked {locked} | urlRevision {urlRevision}");
            log($"Main URL {urlMain} | Alt URL {urlAlt}");
            // Update TV with new manually synced data
            tv.stateSync = state;
            tv.urlMainSync = urlMain;
            tv.urlAltSync = urlAlt;
            tv.lockedSync = locked;
            tv.urlRevisionSync = urlRevision;
            tv.loadingSync = loading;
            tv._PostDeserialization();
        }

        public void _SetTV(TVManagerV2 tv)
        {
            this.tv = tv;
        }

        public void _RequestSync()
        {
            log("Requesting manual serialization");
            RequestSerialization();
        }

        private void log(string value)
        {
            if (!debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#cccc44>TVManagerV2ManualSync ({tv.gameObject.name})</color>] {value}");
        }
        private void warn(string value)
        {
            if (!debug) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#cccc44>TVManagerV2ManualSync ({tv.gameObject.name})</color>] {value}");
        }
        private void err(string value)
        {
            if (!debug) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color=#cccc44>TVManagerV2ManualSync ({tv.gameObject.name})</color>] {value}");
        }
    }
}
