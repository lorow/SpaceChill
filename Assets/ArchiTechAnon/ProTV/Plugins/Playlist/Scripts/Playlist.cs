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
        [HideInInspector] public bool shuffleOnLoad = false;
        [HideInInspector] public bool autoplayList = false;
        [HideInInspector] public bool startFromRandomEntry = false;
        [HideInInspector] public bool continueWhereLeftOff = true;
        [HideInInspector] public bool autoplayOnVideoError = true;
        [HideInInspector] public bool showUrls;
        [HideInInspector] public VRCUrl[] urls;
        [HideInInspector] public string[] titles;
        [HideInInspector] public Sprite[] images;
        [NonSerialized] public int viewOffset;
        // entry caches
        private RectTransform[] entryCache;
        private Button[] buttonCache;
        private Text[] urlCache;
        private Text[] titleCache;
        private Image[] imageCache;
        // a 1 to 1 array corresponding to each URL specifying whether the element should be filtered (aka hidden) in the views.
        private bool[] hidden;
        // an array of the same size as the urls that stores corresponding references to the indexes within the urls array.
        // the order of this array is what gets modified when the playlist gets sorted.
        private int[] rawView = new int[0];
        // an array of a variable size (length of rawView or less) that represents the list of url indexes that are visible for rendering in the current view
        // this array also contains values which correspond to the indexes of the URL list, which may be non-sequential based on the rawView order
        private int[] filteredView = new int[0];
        // an array that represents the visible render shown in the scene based on the filteredView array
        // unlike the previous two, this array's contents corresponds to indexes of the filteredView array
        // eg: to get the actual URL based on a particular entry of the current view, you'd access it via urls[filteredView[currentView]]
        private int[] currentView = new int[0];
        private int nextRawViewIndex = 0;
        private int currentRawViewIndex = -1;
        private VideoError OUT_TvVideoPlayerError_VideoError_Error;
        private bool isLoading = false;
        private bool updateTVLabel = false;
        private Slider loading;
        private float loadingBarDamp;
        private float loadingPercent;
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

        public void _Initialize()
        {
            if (init) return;
            template.SetActive(false);
            hidden = new bool[urls.Length];
            initRawView();
            if (shuffleOnLoad) shuffle(rawView, 3);
            cacheFilteredView();
            if (tv == null) tv = transform.parent.GetComponent<TVManagerV2>();
            hasNoTV = tv == null;

            if (titles.Length != urls.Length)
            {
                warn($"Titles count ({titles.Length}) doesn't match Urls count ({urls.Length}).");
            }
            if (hasNoTV)
            {
                label = "No TV Connected";
                err("The TV reference was not provided. Please make sure the playlist knows what TV to connect to.");
            }
            else
            {
                if (autoplayList)
                {
                    nextRawViewIndex = currentRawViewIndex = 0;
                    if (startFromRandomEntry)
                        nextRawViewIndex = currentRawViewIndex = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * rawView.Length - 1);
                    tv.autoplayURL = urls[rawView[nextRawViewIndex]];
                    pickNext();
                }
                tv._RegisterUdonSharpEventReceiver(this);
                label = $"{tv.gameObject.name}/{name}";
            }
            init = true;
        }

        void Start()
        {
            _Initialize();
            cacheEntryRefs();
            _SeekView(rawToFiltered(nextRawViewIndex));
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
            if (isLoading)
            {
                if (hasLoading) loadingPercent = loading.value;
                if (loadingPercent > 0.95f) return;
                if (loadingPercent > 0.8f)
                    loadingPercent = Mathf.SmoothDamp(loadingPercent, 1f, ref loadingBarDamp, 0.4f);
                else loadingPercent = Mathf.SmoothDamp(loadingPercent, 1f, ref loadingBarDamp, 0.3f);
                if (hasLoading) loading.value = loadingPercent;
            }
        }


        // === TV EVENTS ===

        public void _TvMediaStart()
        {
            if (hasNoTV) return;
            if (!updateTVLabel) currentRawViewIndex = findRawViewIndex();
            if (currentRawViewIndex > -1)
            {
                string title = titles[rawView[currentRawViewIndex]];
                if (title != string.Empty)
                    tv.localLabel = title;
                else if (!showUrls)
                    tv.localLabel = "--Playlist Video--";
            }
            updateTVLabel = false;
        }

        public void _TvMediaEnd()
        {
            if (hasNoTV) return;
            if (autoplayList && !tv.loading && isTVOwner()) _SwitchTo(nextRawViewIndex);
        }

        public void _TvMediaChange()
        {
            if (hasNoTV) return;
            log("Media Change");
            if (autoplayList && !continueWhereLeftOff)
            {
                if (tv.url.Get() != urls[rawView[nextRawViewIndex]].Get())
                    nextRawViewIndex = 0;
            }
            retargetActive();
        }

        public void _TvVideoPlayerError()
        {
            if (hasNoTV) return;
            if (!autoplayOnVideoError || OUT_TvVideoPlayerError_VideoError_Error == VideoError.RateLimited) return; // TV auto-reloads on ratelimited, don't skip current video.
            if (autoplayList && tv.url.Get() == urls[rawView[nextRawViewIndex]].Get())
            {
                pickNext(); // this changes the value of nextRawViewIndex
                tv._DelayedChangeMediaTo(urls[rawView[nextRawViewIndex]]);
                pickNext();
            }
        }

        public void _TvLoading()
        {
            isLoading = true;
            loadingPercent = 0f;
            if (hasLoading) loading.value = 0f;
        }

        public void _TvLoadingEnd()
        {
            isLoading = false;
            loadingPercent = 1f;
            if (hasLoading) loading.value = 1f;
        }

        public void _TvLoadingAbort()
        {
            isLoading = false;
            loadingPercent = 0f;
            if (hasLoading) loading.value = 0f;
        }


        // === UI EVENTS ===

        public void _Next()
        {
            nextRawViewIndex = wrap(nextRawViewIndex + 1);
            _SwitchTo(nextRawViewIndex);
        }

        public void _Previous()
        {
            nextRawViewIndex = wrap(nextRawViewIndex - 2);
            _SwitchTo(nextRawViewIndex);
        }

        public void _SwitchToDetected()
        {
            if (!init) return;
            for (int i = 0; i < buttonCache.Length; i++)
            {
                if (!buttonCache[i].interactable)
                {
                    int rawViewIndex = Array.IndexOf(rawView, currentViewToListIndex(i));
                    log($"Detected view index {i}. Switching to list index {rawViewIndex}.");
                    _SwitchTo(rawViewIndex);
                    return;
                }
            }
        }

        public void _UpdateView()
        {
            if (!init || skipScrollbar) return;
            log("Update View");
            int filteredViewIndex = 0;
            if (scrollView.verticalScrollbar != null)
                filteredViewIndex = Mathf.FloorToInt((1f - scrollView.verticalScrollbar.value) * filteredView.Length);
            seekView(filteredViewIndex);
            retargetActive();
        }

        public void _Shuffle()
        {
            shuffle(rawView, 3);
            cacheFilteredView(); // must recache the filtered view after a shuffle to update to the new rawView order
            _SeekView(filteredToRaw(currentRawViewIndex));
        }

        public void _AutoPlay()
        {
            if (autoplayList) return; // already autoplay, skip
            autoplayList = true;
            if (startFromRandomEntry)
                nextRawViewIndex = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * rawView.Length - 1);
            else pickNext();
            if (tv.stateSync != 1 && !tv.loading) {
                _SwitchTo(nextRawViewIndex);
            }
        }

        public void _ManualPlay()
        {
            autoplayList = false;
        }

        public void _ToggleAutoPlay() {
            if (autoplayList) _ManualPlay();
            else _AutoPlay();
        }

        public void _ChangeAutoPlayTo(bool active) {
            if (active) _AutoPlay();
            else _ManualPlay();
        }

        // === Public Helper Methods

        public void _SwitchTo(int rawViewIndex)
        {
            if (isLoading || hasNoTV) return; // wait until the current video loading finishes/fails
            if (rawViewIndex >= rawView.Length)
                err($"Playlist Item {rawViewIndex} doesn't exist.");
            else if (rawViewIndex == -1) { } // do nothing
            else
            {
                nextRawViewIndex = currentRawViewIndex = rawViewIndex;
                log($"Switching to playlist item {rawViewIndex}");
                tv._ChangeMediaTo(urls[rawView[nextRawViewIndex]]);
                updateTVLabel = true;
                pickNext();
            }
        }

        public void _SeekView(int filteredIndex)
        {
            if (!init) return;
            // log("Seek View");
            filteredIndex = Mathf.Clamp(filteredIndex, 0, filteredView.Length - 1);
            if (scrollView.verticalScrollbar != null)
            {
                skipScrollbar = true;
                scrollView.verticalScrollbar.value = 1 - ((float)filteredIndex) / filteredView.Length;
                skipScrollbar = false;
            }
            seekView(filteredIndex);
            retargetActive();
        }

        public void _UpdateFilter(bool[] hide)
        {
            if (hide.Length != urls.Length)
            {
                log("Filter array must be the same size as the list of urls in the playlist");
                return;
            }
            hidden = hide;
            cacheFilteredView();

            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            // limit offset to the url max minus the last "page", account for the "extra" overflow row as well.
            var maxRow = (filteredView.Length - 1) / horizontalCount + 1;
            var contentHeight = maxRow * item.height;

            scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            _SeekView(0);

        }

        // === Helper Methods ===

        public void shuffle(int[] view, int cycles)
        {
            for (int j = 0; j < cycles; j++)
                Utilities.ShuffleArray(view);
        }

        private void initRawView()
        {
            rawView = new int[urls.Length];
            for (int i = 0; i < urls.Length; i++) rawView[i] = i;
        }

        // TODO change the logic so that filteredView has a persistent array that only modifies the internal values
        // instead of creating a new array every time. 
        // It's currently wasting memory when filters are applied in fast sequence (like searching on every letter change)
        // It should loop through, apply visible index values in order, then set to -1 the remainder of values
        // MUST also cache the number of visible entries as a field variable for later use.
        // That way it doesn't need to loop through the array every time to figure out how many visible there are.
        private void cacheFilteredView()
        {
            var count = 0;
            for (int i = 0; i < rawView.Length; i++)
                if (!hidden[rawView[i]]) count++;
            var cache = new int[count];
            count = 0;
            for (int i = 0; i < rawView.Length; i++)
            {
                var rawItem = rawView[i];
                if (!hidden[rawItem]) cache[count++] = rawItem;
            }
            filteredView = cache;
        }

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

        public void seekView(int filteredViewIndex)
        {
            // modifies the scope of the view, cache the offset for later use
            viewOffset = calculateViewOffset(filteredViewIndex);
            updateCurrentView(viewOffset);
        }

        private int calculateViewOffset(int rawOffset)
        {
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            var verticalCount = Mathf.FloorToInt(max.height / item.height);
            // limit offset to the url max minus the last "row", account for the "extra" overflow row as well.
            var maxRawRow = filteredView.Length / horizontalCount + 1;
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

        private void updateCurrentView(int filteredViewIndex)
        {
            currentView = new int[content.childCount];
            int numOfUrls = filteredView.Length;
            string _log = "None";
            for (int i = 0; i < content.childCount; i++)
            {
                if (filteredViewIndex >= numOfUrls)
                {
                    // urls have exceeded count, hide the remaining entries
                    content.GetChild(i).gameObject.SetActive(false);
                    currentView[i] = -1;
                    continue;
                }
                if (i == 0) _log = $"{filteredView[filteredViewIndex]}";
                else _log += $", {filteredView[filteredViewIndex]}";
                var entry = content.GetChild(i);
                entry.gameObject.SetActive(true);
                // update entry contents
                var url = urlCache[i];
                if (showUrls && url != null) url.text = urls[filteredView[filteredViewIndex]].Get();
                var title = titleCache[i];
                if (title != null) title.text = titles[filteredView[filteredViewIndex]];
                var image = imageCache[i];
                if (image != null)
                {
                    var imageEntry = images[filteredView[filteredViewIndex]];
                    image.sprite = imageEntry;
                    image.gameObject.SetActive(imageEntry != null);
                }
                currentView[i] = filteredViewIndex;
                filteredViewIndex++;
            }
            log(_log);
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
                if (hasLoading) loading.value = loadingPercent;
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
            if (currentRawViewIndex > -1)
            {
                var rawItem = rawView[currentRawViewIndex];
                if (urls[rawItem].Get() == url && !hidden[rawItem])
                    return Array.IndexOf(currentView, Array.IndexOf(filteredView, rawItem));
            }

            // then if the current index IS hidden or IS NOT playing on the TV, 
            // attempt a fuzzy search to find another index that matches that URL
            // do not need to check for hidden here as current view already has that taken into account
            for (int i = 0; i < currentView.Length; i++)
            {
                var listIndex = currentViewToListIndex(i);
                if (listIndex > -1 && urls[listIndex].Get() == url)
                {
                    // log($"List index {listIndex} matches TV url at view index {i}");
                    return i;
                }
            }
            // log("No matches at all");
            return -1;
        }

        private int findRawViewIndex()
        {
            var len = rawView.Length;
            var url = tv.url.Get();
            for (int i = 0; i < len; i++)
            {
                var rawItem = rawView[i];
                if (urls[rawItem].Get() == url)
                    return i;
            }
            return -1;
        }

        private int currentViewToListIndex(int index)
        {
            if (index == -1) return -1;
            if (index >= currentView.Length) return -1;
            if (currentView[index] == -1) return -1;
            if (currentView[index] >= filteredView.Length) return -1;
            return filteredView[currentView[index]];
        }

        private int filteredToRaw(int filteredIndex)
        {
            return Array.IndexOf(rawView, filteredView[filteredIndex]);
        }

        private int rawToFiltered(int rawIndex)
        {
            return Array.IndexOf(filteredView, rawView[rawIndex]);
        }


        private void pickNext()
        {
            var nextPossibleIndex = nextRawViewIndex;
            do
            {
                if (nextPossibleIndex != nextRawViewIndex)
                    log($"Item {nextPossibleIndex} is missing, skipping");
                nextPossibleIndex = wrap(nextPossibleIndex + 1);
                if (nextRawViewIndex == nextPossibleIndex) break; // exit if the entire list has been traversed
            } while (urls[rawView[nextPossibleIndex]].Get() == VRCUrl.Empty.Get());
            log($"Next playlist item {nextPossibleIndex}");
            nextRawViewIndex = nextPossibleIndex;
        }

        private int wrap(int value)
        {
            if (value < 0) value = rawView.Length + value; // adds a negative
            else if (value >= rawView.Length) value = value - rawView.Length; // subtracts the full length
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
