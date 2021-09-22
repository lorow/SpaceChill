
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace ArchiTech
{
    [DefaultExecutionOrder(-1)]
    public class PlaylistSearch : UdonSharpBehaviour
    {
        public bool searchInTitles = true;
        public bool searchInUrls = false;
        public bool searchHiddenPlaylists = false;
        public bool searchOnEachKeypress = true;
        public Playlist[] playlistsToSearch;
        private InputField searchInput;

        private VRCPlayerApi local;
        private bool init = false;
        private bool hasSearchTargets = false;
        private bool debug = true;
        private string debugColor = "#ff9966";

        private void initialize()
        {
            if (init) return;
            hasSearchTargets = playlistsToSearch != null && playlistsToSearch.Length > 0;
            searchInput = GetComponentInChildren<InputField>();
            local = Networking.LocalPlayer;
            init = true;
        }

        void Start()
        {
            initialize();
            var canvases = GetComponentsInChildren<Canvas>();
            foreach (Canvas c in canvases)
            {
                var box = c.GetComponent<BoxCollider>();
                var rect = c.GetComponent<RectTransform>();
                if (box != null)
                {
                    log("Auto-adjusting Canvas collider");
                    box.isTrigger = true;
                    box.size = new Vector3(rect.rect.width, rect.rect.height, 0);
                }
            }
        }

        public void _UpdateSearchOnKeypress()
        {
            if (searchOnEachKeypress) _UpdateSearch();
        }

        public void _UpdateSearch()
        {
            string searchTerm = searchInput.text.Trim();
            if (searchTerm != "")
                log($"Searching {playlistsToSearch.Length} playlists for '{searchTerm}'");
            searchTerm = searchTerm.ToLower();
            foreach (Playlist playlist in playlistsToSearch)
                if (playlist != null && (searchHiddenPlaylists || playlist.gameObject.activeInHierarchy))
                    filterPlaylist(playlist, searchTerm);
        }

        private int filterPlaylist(Playlist playlist, string searchTerm)
        {
            if (!playlist.init) return 0;
            var list = playlist.content;
            var titles = playlist.titles;
            var urls = playlist.urls;
            var hidden = new bool[urls.Length];
            if (searchTerm == "") {
                playlist.hidden = hidden;
                playlist._UpdateView();
                return urls.Length;
            }
            int count = 0;
            for (int i = 0; i < urls.Length; i++)
            {
                var shown = false;
                if (!shown && searchInTitles) shown = titles[i].ToLower().Contains(searchTerm);
                if (!shown && searchInUrls) shown = urls[i].Get().ToLower().Contains(searchTerm);
                hidden[i] = !shown;
                if (shown) count++;
            }
            playlist.hidden = hidden;
            // allows movement control to return to the user faster
            // playlist.SendCustomEventDelayedFrames(nameof(playlist._UpdateView), 2);
            playlist._UpdateView();
            return count;
        }

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistSearch)} ({name})</color>] {value}");
        }
        private void warn(string value)
        {
            if (debug) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistSearch)} ({name})</color>] {value}");
        }
        private void err(string value)
        {
            if (debug) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(PlaylistSearch)} ({name})</color>] {value}");
        }
    }
}
