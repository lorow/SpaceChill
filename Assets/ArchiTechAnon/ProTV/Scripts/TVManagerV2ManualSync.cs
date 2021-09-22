
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace ArchiTech
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(-9999)] // needs to initialize before anything else if possible
    public class TVManagerV2ManualSync : UdonSharpBehaviour
    {
        private TVManagerV2 tv;

        [UdonSynced] private int state = 0;
        [UdonSynced] private VRCUrl url = VRCUrl.Empty;
        // [UdonSynced] private VRCUrl urlQuest = VRCUrl.Empty;
        [UdonSynced] private bool locked = false;
        [UdonSynced] private int urlRevision = 0;
        [UdonSynced] private bool loading = false;
        private bool debug = false;


        new void OnPreSerialization()
        {
            // Extract data from TV for manual sync
            state = tv.stateSync;
            url = tv.urlSync;
            // urlQuest = tv.urlQuestSync;
            locked = tv.lockedSync;
            urlRevision = tv.urlRevisionSync;
            loading = tv.loadingSync;
        }

        new void OnDeserialization()
        {
            log($"Deserialization: ownerState {state} | syncUrl {url} | locked {locked} | urlRevision {urlRevision}");
            // Update TV with new manually synced data
            tv.stateSync = state;
            tv.urlSync = url;
            // tv.urlQuestSync = urlQuest;
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
