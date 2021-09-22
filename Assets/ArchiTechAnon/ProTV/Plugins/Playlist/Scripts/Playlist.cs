using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using VRC.SDK3.Components.Video;

namespace ArchiTech
{
    [DefaultExecutionOrder(-1)]
    public class Playlist : UdonSharpBehaviour
    {

        [HideInInspector] public ScrollRect scrollView;
        [HideInInspector] public RectTransform content;
        [HideInInspector] public GameObject template;
        [HideInInspector] public TVManagerV2 tv;
        [HideInInspector] public bool autoplayList = false;
        [HideInInspector] public bool continueWhereLeftOff = true;
        [HideInInspector] public bool autoplayOnVideoError = true;
        [HideInInspector] public bool showUrls;
        [HideInInspector] public VRCUrl[] urls;
        [HideInInspector] public string[] titles;
        [HideInInspector] public Sprite[] images;
        [HideInInspector] public bool[] hidden;
        [NonSerialized] public int visibleCount;
        // entry caches
        private RectTransform[] entryCache;
        private Button[] buttonCache;
        private Text[] urlCache;
        private Text[] titleCache;
        private Image[] imageCache;
        private int viewOffset;
        private int nextListIndex = 0;
        private int currentListIndex = -1;
        private int[] currentView = new int[0];
        private VideoError OUT_TvVideoPlayerError_VideoError_Error;
        private bool isLoading = false;
        private bool updateTVLabel = false;
        private Slider loading;
        private float loadingBarDamp;
        private Canvas[] canvases;
        private Collider[] colliders;
        private bool hasLoading;
        private bool hasNoTV;
        private bool skipScrollbar;
        private string label;
        [NonSerialized] public bool init = false;
        private bool debug = true;
        private string debugColor = "#ff8811";
        [HideInInspector] public TextAsset _EDITOR_importSrc;
        [HideInInspector] public bool _EDITOR_manualToImport;

        private void initialize()
        {
            if (init) return;
            template.SetActive(false);
            if (hidden == null || hidden.Length == 0) hidden = new bool[urls.Length];
            if (tv == null) tv = transform.parent.GetComponent<TVManagerV2>();
            hasNoTV = tv == null;
            if (hasNoTV)
            {
                label = "No TV Connected";
                err("The TV reference was not provided. Please make sure the playlist knows what TV to connect to.");
            }
            else
            {
                if (autoplayList)
                {
                    tv.autoplayURL = urls[nextListIndex];
                    pickNext();
                }
                tv._RegisterUdonSharpEventReceiver(this);
                label = $"{tv.gameObject.name}/{name}";
            }
            if (titles.Length != urls.Length)
            {
                warn($"Titles count ({titles.Length}) doesn't match Urls count ({urls.Length}).");
            }
            init = true;
        }

        void Start()
        {
            initialize();
            cacheEntryRefs();
            _SeekView(0);
            var shapes = (Component[])GetComponentsInChildren(typeof(VRC_UiShape));
            foreach (Component c in shapes)
            {
                var box = c.GetComponent<BoxCollider>();
                var rect = c.GetComponent<RectTransform>();
                if (box != null)
                {
                    log("Auto-adjusting Canvas collider");
                    box.isTrigger = true;
                    box.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 0);
                }
            }
        }

        void LateUpdate()
        {
            if (hasLoading)
            {
                if (isLoading)
                {
                    if (loading.value > 0.95f) return;
                    if (loading.value > 0.8f)
                        loading.value = Mathf.SmoothDamp(loading.value, 1f, ref loadingBarDamp, 0.4f);
                    else
                        loading.value = Mathf.SmoothDamp(loading.value, 1f, ref loadingBarDamp, 0.3f);
                }
            }
        }


        // === TV EVENTS ===

        public void _TvMediaStart()
        {
            if (hasNoTV) return;
            if (updateTVLabel && currentListIndex > -1)
            {
                string title = titles[currentListIndex];
                if (title != string.Empty)
                    tv.localLabel = titles[currentListIndex];
                else if (!showUrls)
                    tv.localLabel = "--Playlist Video--";
                updateTVLabel = false;
            }
        }

        public void _TvMediaEnd()
        {
            if (hasNoTV) return;
            if (autoplayList && !tv.loading && isTVOwner()) _SwitchTo(nextListIndex);
        }

        public void _TvMediaChange()
        {
            if (hasNoTV) return;
            log("Media Change");
            if (autoplayList && !continueWhereLeftOff)
            {
                if (tv.url.Get() != urls[nextListIndex].Get())
                    nextListIndex = 0;
            }
            retargetActive();
        }

        public void _TvVideoPlayerError()
        {
            if (hasNoTV) return;
            if (!autoplayOnVideoError || OUT_TvVideoPlayerError_VideoError_Error == VideoError.RateLimited) return; // TV auto-reloads on ratelimited, don't skip current video.
            if (autoplayList && tv.url.Get() == urls[nextListIndex].Get())
            {
                pickNext();
                tv._DelayedChangeMediaTo(urls[nextListIndex]);
                pickNext();
            }
        }

        public void _TvLoading()
        {
            isLoading = true;
            if (hasLoading) loading.value = 0f;
        }

        public void _TvLoadingEnd()
        {
            isLoading = false;
            if (hasLoading) loading.value = 1f;
        }

        public void _TvLoadingAbort()
        {
            isLoading = false;
            if (hasLoading) loading.value = 0f;
        }


        // === UI EVENTS ===

        public void _Next()
        {
            nextListIndex = wrap(nextListIndex + 1);
            _SwitchTo(nextListIndex);
        }

        public void _Previous()
        {
            nextListIndex = wrap(nextListIndex - 2);
            _SwitchTo(nextListIndex);
        }

        public void _SwitchTo(int listIndex)
        {
            if (isLoading || hasNoTV) return; // wait until the current video loading finishes/fails
            if (listIndex >= urls.Length)
                err($"Playlist Item {listIndex} doesn't exist.");
            else
            {
                nextListIndex = currentListIndex = listIndex;
                log($"Switching to playlist item {listIndex}");
                tv._ChangeMediaTo(urls[listIndex]);
                updateTVLabel = true;
                pickNext();
            }
        }

        // === Events for the UI ===

        public void _SwitchToDetected()
        {
            if (!init) return;
            for (int i = 0; i < buttonCache.Length; i++)
            {
                if (!buttonCache[i].interactable)
                {
                    int listIndex = viewToList(i);
                    log($"Detected view index {i}. Switching to list index {listIndex}.");
                    _SwitchTo(listIndex);
                    return;
                }
            }
        }

        public void _UpdateView()
        {
            if (!init || skipScrollbar) return;
            // log("Update View");
            int target = 0;
            if (scrollView.verticalScrollbar != null)
            {
                target = Mathf.FloorToInt((1f - scrollView.verticalScrollbar.value) * urls.Length);
            }
            seekView(target);
            retargetActive();
        }

        public void _SeekView(int item)
        {
            if (!init) return;
            // log("Seek View");
            if (scrollView.verticalScrollbar != null)
            {
                skipScrollbar = true;
                scrollView.verticalScrollbar.value = 1f - item / urls.Length;
                skipScrollbar = false;
            }
            seekView(item);
            retargetActive();
        }

        // === Helper Methods ===

        private void cacheEntryRefs()
        {
            int cacheSize = content.childCount;
            entryCache = new RectTransform[cacheSize];
            buttonCache = new Button[cacheSize];
            urlCache = new Text[cacheSize];
            titleCache = new Text[cacheSize];
            imageCache = new Image[cacheSize];
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform entry = (RectTransform)content.GetChild(i);
                entryCache[i] = entry;

                buttonCache[i] = entry.GetComponentInChildren<Button>();

                var url = entry.Find("Url");
                urlCache[i] = url == null ? null : url.GetComponent<Text>();

                var title = entry.Find("Title");
                titleCache[i] = title == null ? null : title.GetComponent<Text>();

                var image = entry.Find("Image");
                imageCache[i] = image == null ? null : image.GetComponent<Image>();
            }
        }

        public void seekView(int rawOffset)
        {
            // modifies the scope of the view, cache the offset for later use
            viewOffset = calculateViewOffset(rawOffset);
            updateContents(viewOffset);
        }

        private int calculateViewOffset(int rawOffset)
        {
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            var verticalCount = Mathf.FloorToInt(max.height / item.height);
            // limit offset to the url max minus the last "row", account for the "extra" overflow row as well.
            var maxRawRow = urls.Length / horizontalCount + 1;
            // clamp the min/max row to the view area boundries
            var maxRow = Mathf.Min(maxRawRow, maxRawRow - verticalCount);
            if (maxRow == 0) maxRow = 1;

            var maxOffset = maxRow * horizontalCount;
            var currentRow = rawOffset / horizontalCount; // int DIV causes stepped values, good
            var currentOffset = currentRow * horizontalCount;
            // currentOffset will be smaller than maxOffset when the scroll limit has not yet been reached
            var targetOffset = Mathf.Min(currentOffset, maxOffset);
            // log($"Raw {rawOffset} | H/V Count {horizontalCount}/{verticalCount} | Max RawRow/Row/Offset {maxRawRow}/{maxRow}/{maxOffset} | Current Row/Offset {currentRow}/{currentOffset}");
            return Mathf.Max(0, targetOffset);
        }

        private void updateContents(int playlistIndex)
        {
            currentView = new int[content.childCount];
            int numOfUrls = urls.Length;
            for (int i = 0; i < content.childCount; i++)
            {
                log($"Playlist index {playlistIndex} child index {i}");
                while (playlistIndex < numOfUrls && hidden[playlistIndex]) playlistIndex++; // skip all filtered entries
                if (playlistIndex >= numOfUrls)
                {
                    // urls have exceeded count, hide the remaining entries
                    content.GetChild(i).gameObject.SetActive(false);
                    currentView[i] = -1;
                    continue;
                }
                var entry = content.GetChild(i);
                entry.gameObject.SetActive(true);
                // update entry contents
                var url = urlCache[i];
                if (showUrls && url != null) url.text = urls[playlistIndex].Get();
                var title = titleCache[i];
                if (title != null) title.text = titles[playlistIndex];
                var image = imageCache[i];
                if (image != null)
                {
                    var imageEntry = images[playlistIndex];
                    image.sprite = imageEntry;
                    image.gameObject.SetActive(imageEntry != null);
                }
                currentView[i] = playlistIndex;
                playlistIndex++;
            }
        }

        private void retargetActive()
        {
            // if autoplay is disabled, try to see if the current media matches one on the playlist, if so, indicate loading
            if (hasLoading) loading.value = 0f;
            int found = findTargetViewIndex();
            // cache the found index's Slider component, otherwise null
            if (found > -1)
            {
                // log($"Media index found within view at entry {found}");
                loading = content.GetChild(found).GetComponentInChildren<Slider>();
                hasLoading = loading != null;
                if (hasLoading) loading.value = 1f;
            }
            else
            {
                // log($"Media index not within view");
                loading = null;
                hasLoading = false;
            }
        }

        private int findTargetViewIndex()
        {
            if (hasNoTV) return -1;
            var url = tv.url.Get();
            // if the current index is playing on the TV and not hidden, 
            //  return either it's position in the current view, or -1 if it's not visible in the current view
            if (currentListIndex > -1 && urls[currentListIndex].Get() == url && !hidden[currentListIndex])
            {
                return listToView(currentListIndex);
            }

            // then if the current index IS hidden or IS NOT playing on the TV, 
            // attempt a fuzzy search to find another index that matches that URL
            // do not need to check for hidden here as current view already has that taken into account
            for (int i = 0; i < currentView.Length; i++)
            {
                var listIndex = viewToList(i);
                if (listIndex > -1 && urls[listIndex].Get() == url)
                {
                    // log($"List index {listIndex} matches TV url at view index {i}");
                    return i;
                }
            }
            // log("No matches at all");
            return -1;
        }

        private int listToView(int index) => Array.IndexOf(currentView, index);

        private int viewToList(int index) => index > -1 && index < currentView.Length ? currentView[index] : -1;


        private void pickNext()
        {
            var nextPossibleIndex = nextListIndex;
            do
            {
                if (nextPossibleIndex != nextListIndex)
                    log($"Item {nextPossibleIndex} is missing, skipping");
                nextPossibleIndex = wrap(nextPossibleIndex + 1);
                if (nextListIndex == nextPossibleIndex) break; // exit if the entire list has been traversed
            } while (urls[nextPossibleIndex].Get() == VRCUrl.Empty.Get());
            log($"Next playlist item {nextPossibleIndex}");
            nextListIndex = nextPossibleIndex;
        }

        private int wrap(int value)
        {
            if (value < 0) value = urls.Length + value; // adds a negative
            else if (value >= urls.Length) value = value - urls.Length; // subtracts the full length
            return value;
        }

        private bool isTVOwner() => !hasNoTV && Networking.IsOwner(tv.gameObject);

        private void log(string value)
        {
            if (debug) Debug.Log($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Playlist)} ({label})</color>] {value}");
        }
        private void warn(string value)
        {
            if (debug) Debug.LogWarning($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Playlist)} ({label})</color>] {value}");
        }
        private void err(string value)
        {
            if (debug) Debug.LogError($"[<color=#1F84A9>A</color><color=#A3A3A3>T</color><color=#2861B4>A</color> | <color={debugColor}>{nameof(Playlist)} ({label})</color>] {value}");
        }
    }
}
