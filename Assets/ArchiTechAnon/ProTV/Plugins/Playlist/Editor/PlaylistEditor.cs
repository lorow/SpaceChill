using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UdonSharpEditor;
using System.Text;
using VRC.SDKBase;

namespace ArchiTech.Editor
{
    [CustomEditor(typeof(Playlist))]
    public class PlaylistEditor : UnityEditor.Editor
    {
        private static string newEntryIndicator = "@";
        private static string entryImageIndicator = "/";
        Playlist playlist;
        TVManagerV2 tv;
        ScrollRect scrollView;
        RectTransform content;
        GameObject template;
        bool shuffleOnLoad;
        bool autoplayList;
        bool startFromRandomEntry;
        bool continueWhereLeftOff;
        bool autoplayOnVideoError;
        bool showUrls = true;
        VRCUrl[] urls;
        string[] titles;
        Sprite[] images;
        int visibleCount;
        Vector2 scrollPos;
        PlaylistAction updateMode = PlaylistAction.NOOP;
        bool manualToImport = false;
        TextAsset importSrc;
        int perPage = 25;
        int currentFocus;
        int entriesCount;
        int imagesCount;
        int targetEntry;
        bool recache = true;

        private enum PlaylistAction
        {
            NOOP, OTHER,
            MOVEUP, MOVEDOWN,
            ADD, REMOVE, REMOVEALL,
            UPDATESELF, UPDATEALL, UPDATEVIEW
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            playlist = (Playlist)target;
            scrollView = playlist.scrollView;
            content = playlist.content;
            template = playlist.template;

            if (recache) cacheEntryInfo();

            EditorGUI.BeginChangeCheck();
            showPlaylistProperties();
            showPlaylistControls();
            showPlaylistEntries();
            if (EditorGUI.EndChangeCheck() && updateMode != PlaylistAction.NOOP)
            {
                Debug.Log("Changes Detected");
                Undo.RecordObject(playlist, "Modify Playlist Content");
                if (updateMode != PlaylistAction.OTHER)
                {
                    updateScene();
                    recache = true;
                }
                playlist.tv = tv;
                playlist.scrollView = scrollView;
                playlist.content = content;
                playlist.template = template;
                playlist.showUrls = showUrls;
                playlist.shuffleOnLoad = shuffleOnLoad;
                playlist.autoplayList = autoplayList;
                playlist.startFromRandomEntry = startFromRandomEntry;
                playlist.continueWhereLeftOff = continueWhereLeftOff;
                playlist.autoplayOnVideoError = autoplayOnVideoError;
                playlist.urls = urls;
                playlist.titles = titles;
                playlist.images = images;
                playlist._EDITOR_importSrc = importSrc;
                playlist._EDITOR_manualToImport = manualToImport;
                updateMode = PlaylistAction.NOOP;
            }

        }

        private void cacheEntryInfo()
        {
            var oldUrls = playlist.urls;
            if (oldUrls == null) oldUrls = new VRCUrl[0];
            urls = new VRCUrl[oldUrls.Length];
            Array.Copy(oldUrls, urls, oldUrls.Length);

            var oldTitles = playlist.titles;
            if (oldTitles == null || oldTitles.Length == 0) oldTitles = new string[oldUrls.Length];
            titles = new string[oldTitles.Length];
            Array.Copy(oldTitles, titles, oldTitles.Length);

            var oldImages = playlist.images;
            if (oldImages == null || oldImages.Length == 0) oldImages = new Sprite[oldUrls.Length];
            images = new Sprite[oldImages.Length];
            Array.Copy(oldImages, images, oldImages.Length);

            recache = false;
        }

        private void showPlaylistProperties()
        {
            EditorGUILayout.Space();

            tv = (TVManagerV2)EditorGUILayout.ObjectField("TV", playlist.tv, typeof(TVManagerV2), true);
            if (tv != playlist.tv) updateMode = PlaylistAction.OTHER;
            scrollView = (ScrollRect)EditorGUILayout.ObjectField("Playlist ScrollView", playlist.scrollView, typeof(ScrollRect), true);
            if (scrollView != playlist.scrollView) updateMode = PlaylistAction.OTHER;
            content = (RectTransform)EditorGUILayout.ObjectField("Playlist Item Container", playlist.content, typeof(RectTransform), true);
            if (content != playlist.content) updateMode = PlaylistAction.OTHER;
            template = (GameObject)EditorGUILayout.ObjectField("Playlist Item Template", playlist.template, typeof(GameObject), true);
            if (template != playlist.template) updateMode = PlaylistAction.OTHER;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Shuffle Playlist on Load");
            shuffleOnLoad = EditorGUILayout.Toggle(playlist.shuffleOnLoad);
            if (shuffleOnLoad != playlist.shuffleOnLoad) updateMode = PlaylistAction.OTHER;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Autoplay?");
            autoplayList = EditorGUILayout.Toggle(playlist.autoplayList);
            if (autoplayList != playlist.autoplayList) updateMode = PlaylistAction.OTHER;
            EditorGUILayout.EndHorizontal();

            if (autoplayList)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.PrefixLabel("Start from random entry");
                startFromRandomEntry = EditorGUILayout.Toggle(playlist.startFromRandomEntry);
                if (startFromRandomEntry != playlist.startFromRandomEntry) updateMode = PlaylistAction.OTHER;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.PrefixLabel("Continue from last known entry");
                continueWhereLeftOff = EditorGUILayout.Toggle(playlist.continueWhereLeftOff);
                if (continueWhereLeftOff != playlist.continueWhereLeftOff) updateMode = PlaylistAction.OTHER;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.PrefixLabel("Skip to next entry on video error");
                autoplayOnVideoError = EditorGUILayout.Toggle(playlist.autoplayOnVideoError);
                if (autoplayOnVideoError != playlist.autoplayOnVideoError) updateMode = PlaylistAction.OTHER;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                continueWhereLeftOff = playlist.continueWhereLeftOff;
                autoplayOnVideoError = playlist.autoplayOnVideoError;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Show urls in playlist?");
            showUrls = EditorGUILayout.Toggle(playlist.showUrls);
            if (showUrls != playlist.showUrls) updateMode = PlaylistAction.OTHER;
            EditorGUILayout.EndHorizontal();
        }

        private void showPlaylistControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(); // 1
            EditorGUILayout.BeginVertical(); // 2
            EditorGUILayout.LabelField("Video Playlist Items", GUILayout.Width(120f), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Update Scene", GUILayout.MaxWidth(100f)))
            {
                updateMode = PlaylistAction.UPDATEALL;
            }
            if (GUILayout.Button("Copy Playlist to Clipboard", GUILayout.ExpandWidth(false)))
            {
                GUIUtility.systemCopyBuffer = pickle();
            }
            EditorGUILayout.EndVertical(); // end 2
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(); // 2
            string detection = "";
            if (importSrc != null && playlist._EDITOR_manualToImport)
                detection = $" | Detected: {entriesCount} urls with {imagesCount} images";
            manualToImport = EditorGUILayout.ToggleLeft($"Load From Text File{detection}", playlist._EDITOR_manualToImport);
            if (manualToImport != playlist._EDITOR_manualToImport) updateMode = PlaylistAction.OTHER;
            if (manualToImport)
            {
                EditorGUILayout.BeginHorizontal(); // 3
                importSrc = (TextAsset)EditorGUILayout.ObjectField(playlist._EDITOR_importSrc, typeof(TextAsset), false, GUILayout.MaxWidth(300f));
                if (importSrc != playlist._EDITOR_importSrc)
                {
                    updateMode = PlaylistAction.OTHER;
                    entriesCount = 0;
                    imagesCount = 0;
                }
                if (importSrc != null)
                {
                    if (entriesCount == 0) entriesCount = countEntries(importSrc.text);
                    if (imagesCount == 0) imagesCount = countImages(importSrc.text);

                    if (GUILayout.Button("Import", GUILayout.ExpandWidth(false)))
                    {
                        parseContent(importSrc.text);
                        updateMode = PlaylistAction.UPDATEALL;
                    }
                }
                EditorGUILayout.EndHorizontal(); // end 3
            }
            else
            {
                importSrc = playlist._EDITOR_importSrc;
                EditorGUILayout.BeginHorizontal(); // 3
                if (GUILayout.Button("Add Entry", GUILayout.MaxWidth(100f)))
                {
                    updateMode = PlaylistAction.ADD;
                }

                EditorGUI.BeginDisabledGroup(urls.Length == 0); // 4
                if (GUILayout.Button("Remove All", GUILayout.MaxWidth(100f)))
                {
                    updateMode = PlaylistAction.REMOVEALL;
                }
                EditorGUI.EndDisabledGroup(); // end 4
                EditorGUILayout.EndHorizontal(); // end 3
            }

            EditorGUILayout.BeginHorizontal(); // 3
            var urlCount = urls.Length;
            var currentPage = currentFocus / perPage;
            var maxPage = urlCount / perPage;
            var oldFocus = currentFocus;
            EditorGUI.BeginDisabledGroup(currentPage == 0); // end 4
            if (GUILayout.Button("<<")) currentFocus -= perPage;
            EditorGUI.EndDisabledGroup(); // end 4
            EditorGUI.BeginDisabledGroup(currentFocus == 0); // 4
            if (GUILayout.Button("<")) currentFocus -= 1;
            EditorGUI.EndDisabledGroup(); // end 4
            // offset the slider's internal value range by one so that the numbers match up visually with the list
            currentFocus = EditorGUILayout.IntSlider(currentFocus + 1, 1, urlCount, GUILayout.ExpandWidth(true)) - 1;
            GUILayout.Label($"/ {urlCount}");

            EditorGUI.BeginDisabledGroup(currentFocus == urlCount); // 4
            if (GUILayout.Button(">")) currentFocus += 1;
            EditorGUI.EndDisabledGroup(); // end 4
            EditorGUI.BeginDisabledGroup(currentPage == maxPage); // 4
            if (GUILayout.Button(">>")) currentFocus += perPage;
            EditorGUI.EndDisabledGroup(); // end 4
            EditorGUILayout.EndHorizontal(); // end 3

            if (oldFocus != currentFocus)
            {
                updateMode = PlaylistAction.UPDATEVIEW;
            }

            EditorGUILayout.EndVertical(); // end 2
            EditorGUILayout.EndHorizontal(); // end 1
        }

        private int countEntries(string text)
        {
            if (text.Trim().Length == 0) return 0;
            string[] lines = text.Trim().Split('\n');
            int count = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith(newEntryIndicator)) count++;
            }
            return count;
        }

        private int countImages(string text)
        {
            if (text.Trim().Length == 0) return 0;
            string[] lines = text.Trim().Split('\n');
            int count = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith(entryImageIndicator)) count++;
            }
            return count;
        }

        private void parseContent(string text)
        {
            text = text.Trim();
            string[] lines = text.Split('\n');
            int count = countEntries(text);
            urls = new VRCUrl[count];
            titles = new string[count];
            images = new Sprite[count];
            count = -1;
            string currentTitle = "";
            Sprite currentImage = null;
            uint missingTitles = 0;
            foreach (string l in lines)
            {
                var line = l.Trim();
                if (line.StartsWith(newEntryIndicator))
                {
                    if (count > -1)
                    {
                        if (currentTitle.Length == 0)
                        {
                            Debug.Log($"Missing title at index {count}");
                            missingTitles++;
                        }
                        titles[count] = currentTitle.Trim();
                        currentTitle = "";
                        currentImage = null;
                    }
                    count++;
                    urls[count] = new VRCUrl(line.Substring(newEntryIndicator.Length).Trim());
                    continue;
                }
                if (count == -1) continue;
                if (line.StartsWith(entryImageIndicator) && currentImage == null && currentTitle == "")
                {
                    string assetFile = line.Substring(entryImageIndicator.Length).Trim();
                    currentImage = (Sprite)AssetDatabase.LoadAssetAtPath(assetFile, typeof(Sprite));
                    images[count] = currentImage;
                    continue;
                }
                if (currentTitle.Length > 0) currentTitle += '\n';
                currentTitle += line.Trim();
            }
            if (count > -1)
            {
                titles[count] = currentTitle.Trim();
                if (currentTitle.Length == 0)
                {
                    missingTitles++;
                    Debug.Log($"Missing title at index {count}");
                }
            }
            if (missingTitles > 0)
            {
                Debug.LogWarning($"Just a heads up, this playlist has {missingTitles} entries that don't have any titles.");
            }
        }

        private string pickle()
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < playlist.urls.Length; i++)
            {
                var url = playlist.urls[i];
                s.AppendLine("@" + url);
                if (i < playlist.images.Length)
                {
                    var image = playlist.images[i];
                    if (image != null) s.AppendLine("/" + AssetDatabase.GetAssetPath(image.texture));
                }
                if (i < playlist.titles.Length)
                {
                    var title = playlist.titles[i];
                    if (title != null) s.AppendLine(title + "\n");
                }
            }
            return s.ToString();
        }

        private void showPlaylistEntries()
        {
            var urlCount = urls.Length;
            var currentPage = currentFocus / perPage;
            var maxPage = urlCount / perPage;
            var pageStart = currentPage * perPage;
            var pageEnd = Math.Min(urlCount, pageStart + perPage);
            var height = Mathf.Min(330f, perPage * 55f) + 15f; // cap size at 330 + 15 for spacing for the horizontal scroll bar
            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(height)); // 1
            EditorGUI.BeginDisabledGroup(manualToImport); // 2
            for (var i = pageStart; i < pageEnd; i++)
            {
                EditorGUILayout.BeginHorizontal();  // 3
                EditorGUILayout.BeginVertical();    // 4

                // URL field management
                EditorGUILayout.BeginHorizontal(); // 5
                EditorGUILayout.LabelField($"Url {i}", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                var url = new VRCUrl(EditorGUILayout.TextField(urls[i].Get(), GUILayout.ExpandWidth(true)));
                if (url.Get() != urls[i].Get()) updateMode = PlaylistAction.UPDATESELF;
                urls[i] = url;
                EditorGUILayout.EndHorizontal(); // end 5

                // TITLE field management
                EditorGUILayout.BeginHorizontal(); // 5
                EditorGUILayout.LabelField("  Description", GUILayout.MaxWidth(100f), GUILayout.ExpandWidth(false));
                var title = EditorGUILayout.TextArea(titles[i], GUILayout.Width(250f), GUILayout.ExpandWidth(true));
                if (title != titles[i]) updateMode = PlaylistAction.UPDATESELF;
                titles[i] = title;
                EditorGUILayout.EndHorizontal(); // end 5

                EditorGUILayout.EndVertical(); // end 4
                var image = (Sprite)EditorGUILayout.ObjectField(images[i], typeof(Sprite), false, GUILayout.Height(50), GUILayout.Width(50));
                if (image != images[i]) updateMode = PlaylistAction.UPDATESELF;
                images[i] = image;
                if (!manualToImport)
                {
                    // Playlist entry actions
                    EditorGUILayout.BeginVertical(); // 4
                    if (GUILayout.Button("Remove"))
                    {
                        // Cannot modify urls list within loop else index error occurs
                        targetEntry = i;
                        updateMode = PlaylistAction.REMOVE;
                    }

                    // Playlist entry ordering
                    EditorGUILayout.BeginHorizontal(); // 5
                    EditorGUI.BeginDisabledGroup(i == 0); // 6
                    if (GUILayout.Button("Up"))
                    {
                        targetEntry = i;
                        updateMode = PlaylistAction.MOVEUP;
                    }
                    EditorGUI.EndDisabledGroup(); // end 6
                    EditorGUI.BeginDisabledGroup(i + 1 == urls.Length); // 6
                    if (GUILayout.Button("Down"))
                    {
                        targetEntry = i;
                        updateMode = PlaylistAction.MOVEDOWN;
                    }
                    EditorGUI.EndDisabledGroup(); // end 6
                    EditorGUILayout.EndHorizontal(); // end 5

                    EditorGUILayout.EndVertical(); // end 4
                }
                EditorGUILayout.EndHorizontal(); // end 3
                GUILayout.Space(3f);
            }
            EditorGUI.EndDisabledGroup(); // end 2
            EditorGUILayout.EndScrollView(); // end 1
        }

        #region Scene Updates

        private void updateScene()
        {
            Debug.Log("Updating Scene");
            if (scrollView?.viewport == null)
            {
                Debug.LogError("ScrollRect or associated viewport is null. Ensure they are connected in the inspector.");
                return;
            }
            switch (updateMode)
            {
                case PlaylistAction.ADD: addItem(); break;
                case PlaylistAction.MOVEUP: moveItem(targetEntry, targetEntry - 1); break;
                case PlaylistAction.MOVEDOWN: moveItem(targetEntry, targetEntry + 1); break;
                case PlaylistAction.REMOVE: removeItem(targetEntry); break;
                case PlaylistAction.REMOVEALL: removeAll(); break;
                default: break;
            }
            targetEntry = -1;
            switch (updateMode)
            {
                case PlaylistAction.UPDATEVIEW:
                case PlaylistAction.UPDATESELF: updateContents(); break;
                default: rebuildScene(); break;
            }
        }

        private void addItem()
        {
            Debug.Log($"Adding playlist item {urls.Length + 1}");
            var oldUrls = urls;
            var oldTitles = titles;
            var oldImages = images;
            urls = new VRCUrl[oldUrls.Length + 1];
            titles = new string[oldTitles.Length + 1];
            images = new Sprite[oldImages.Length + 1];
            int i = 0;
            for (; i < oldUrls.Length; i++)
            {
                urls[i] = oldUrls[i];
                titles[i] = oldTitles[i];
                images[i] = oldImages[i];
            }
            urls[i] = VRCUrl.Empty;
        }

        private void removeItem(int index)
        {
            Debug.Log($"Removing playlist item {index + 1}: {titles[index]}");
            var oldUrls = urls;
            var oldTitles = titles;
            var oldImages = images;
            urls = new VRCUrl[oldUrls.Length - 1];
            titles = new string[oldTitles.Length - 1];
            images = new Sprite[oldImages.Length - 1];
            int offset = 0;
            for (int i = 0; i < urls.Length; i++)
            {
                if (i == index)
                {
                    offset = 1;
                }
                urls[i] = oldUrls[i + offset];
                titles[i] = oldTitles[i + offset];
                images[i] = oldImages[i + offset];
            }
        }

        private void moveItem(int from, int to)
        {
            // no change needed
            if (from == to) return;
            Debug.Log($"Moving playlist item {from + 1} -> {to + 1}");
            // cache the source index
            var fromUrl = urls[from];
            var fromTitle = titles[from];
            var fromImage = images[from];
            // determines the direction to shift
            int direction = from < to ? 1 : -1;
            // calculate the actual start and end values for the loop
            int start = Math.Min(from, to);
            int end = start + Math.Abs(to - from);
            for (int i = start; i <= end; i++)
            {
                // don't assign the target values yet
                if (i == to) continue;
                urls[i] = urls[i + direction];
                titles[i] = titles[i + direction];
                images[i] = images[i + direction];
            }
            // assign the target values now
            urls[to] = fromUrl;
            titles[to] = fromTitle;
            images[to] = fromImage;
        }

        private void removeAll()
        {
            Debug.Log($"Removing all {urls.Length} playlist items");
            urls = new VRCUrl[0];
            titles = new string[0];
            images = new Sprite[0];
        }

        public void rebuildScene()
        {
            // determine how many entries can be shown within the physical space of the viewport
            calculateVisibleEntries();
            // destroy and rebuild the list of entries for the visibleCount
            rebuildEntries();
            // re-organize the layout to the viewport's size
            recalculateLayout();
            // update the internal content of each entry with in the range of visibleOffset -> visibleOffset + visibleCount and certain constraints
            updateContents();
            // ensure the attached scrollbar has the necessary event listener attached
            attachScrollbarEvent();
        }

        private void calculateVisibleEntries()
        {
            // calculate the x/y entry counts
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            var verticalCount = Mathf.FloorToInt(max.height / item.height) + 1; // allows Y overflow for better visual flow
            visibleCount = Mathf.Min(urls.Length, horizontalCount * verticalCount);
        }

        private void rebuildEntries()
        {
            // clear existing entries
            while (content.childCount > 0) DestroyImmediate(content.GetChild(0).gameObject);
            // rebuild entries list
            for (int i = 0; i < visibleCount; i++) createEntry();
        }

        private void createEntry()
        {
            // create scene entry
            GameObject entry = Instantiate(template, content, false);
            entry.name = $"Entry ({content.childCount})";
            entry.transform.SetAsLastSibling();

            var behavior = UdonSharpEditorUtility.GetBackingUdonBehaviour(playlist);
            var button = entry.GetComponentInChildren<Button>();

            if (button == null)
            {
                // trigger isn't present, put one on the template root
                button = entry.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var nav = new Navigation();
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
            }

            // clear old listners
            while (button.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(button.onClick, 0);

            // set UI event sequence for the button
            UnityAction<bool> interactable = System.Delegate.CreateDelegate(typeof(UnityAction<bool>), button, "set_interactable") as UnityAction<bool>;
            UnityAction<string> switchTo = new UnityAction<string>(behavior.SendCustomEvent);
            UnityEventTools.AddBoolPersistentListener(button.onClick, interactable, false);
            UnityEventTools.AddStringPersistentListener(button.onClick, switchTo, nameof(playlist._SwitchToDetected));
            UnityEventTools.AddBoolPersistentListener(button.onClick, interactable, true);
            entry.SetActive(true);
        }

        private void recalculateLayout()
        {
            // ensure the content box fills exactly 100% of the viewport.
            content.SetParent(scrollView.viewport);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = new Vector2(0, 0);
            var max = content.rect;
            float maxWidth = max.width;
            float maxHeight = max.height;
            int col = 0;
            int row = 0;
            // template always assumes the anchor PIVOT is located at X=0.0 and Y=1.0 (aka upper left corner)
            // TODO enforce this assumption
            float X = 0f;
            float Y = 0f;
            // TODO Take the left-right margins into account for spacing
            // should be able to make the assumption that all entries are the same structure (thus width/height) as template
            Rect tmpl = ((RectTransform)playlist.template.transform).rect;
            float entryHeight = tmpl.height;
            float entryWidth = tmpl.width;
            float listHeight = entryHeight;
            bool firstEntry = true;
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform entry = (RectTransform)content.GetChild(i);
                // expect fill in left to right.
                X = entryWidth * col;
                // detect if a new row is needed, first row will be row 0 implicitly
                if (firstEntry) firstEntry = false;
                else if (X + entryWidth > maxWidth)
                {
                    // reset the horizontal data
                    col = 0;
                    X = 0f;
                    // horizontal exceeds the shape of the container, shift to the next row
                    row++;
                }
                // calculate the target row
                Y = entryHeight * row;
                entry.anchoredPosition = new Vector2(X, -Y);
                col++; // target next column
            }
        }

        private int calculateVisibleOffset(int rawOffset)
        {
            Rect max = scrollView.viewport.rect;
            Rect item = ((RectTransform)template.transform).rect;
            var horizontalCount = Mathf.FloorToInt(max.width / item.width);
            if (horizontalCount == 0) horizontalCount = 1;
            var verticalCount = Mathf.FloorToInt(max.height / item.height);
            // limit offset to the url max minus the last "page", account for the "extra" overflow row as well.
            var maxRow = (urls.Length - 1) / horizontalCount + 1;
            var contentHeight = maxRow * item.height;
            // clamp the min/max row to the view area boundries
            maxRow = Mathf.Min(maxRow, maxRow - verticalCount);
            if (maxRow == 0) maxRow = 1;

            var maxOffset = maxRow * horizontalCount;
            var currentRow = rawOffset / horizontalCount; // int DIV causes stepped values
            var steppedOffset = currentRow * horizontalCount;
            // currentOffset will be smaller than maxOffset when the scroll limit has not yet been reached
            var targetOffset = Mathf.Min(steppedOffset, maxOffset);

            // update the scrollview content proxy's height
            float scrollHeight = Mathf.Max(contentHeight, max.height + item.height / 2);
            scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scrollHeight);
            if (scrollView.verticalScrollbar != null)
                scrollView.verticalScrollbar.value = 1f - (float)rawOffset / (maxOffset);

            return Mathf.Max(0, targetOffset);
        }

        private void updateContents()
        {
            int playlistIndex = calculateVisibleOffset(currentFocus);
            int numOfUrls = urls.Length;
            for (int i = 0; i < content.childCount; i++)
            {
                if (playlistIndex >= numOfUrls)
                {
                    // urls have exceeded count, hide the remaining entries
                    content.GetChild(i).gameObject.SetActive(false);
                    continue;
                }
                var entry = content.GetChild(i);
                entry.gameObject.SetActive(true);

                // track found components
                bool titleSet = false;
                bool urlSet = false;
                bool imageSet = false;

                // update entry contents
                Text[] textArr = entry.GetComponentsInChildren<Text>(true);
                foreach (Text component in textArr)
                {
                    if (!titleSet && component.name == "Title")
                    {
                        component.text = titles[playlistIndex];
                        EditorUtility.SetDirty(component); // this forces the scene to update for each change as they happen
                        titleSet = true;
                    }
                    else if (!urlSet && component.name == "Url" && showUrls)
                    {
                        component.text = urls[playlistIndex].Get();
                        EditorUtility.SetDirty(component); // this forces the scene to update for each change as they happen
                        urlSet = true;
                    }
                }

                Image[] imageArr = entry.GetComponentsInChildren<Image>(true);
                foreach (Image component in imageArr)
                {
                    if (!imageSet && (component.name == "Image" || component.name == "Poster"))
                    {
                        component.sprite = images[playlistIndex];
                        component.gameObject.SetActive(images[playlistIndex] != null);
                        EditorUtility.SetDirty(component); // this forces the scene to update for each change as they happen
                        imageSet = true;
                    }
                }
                playlistIndex++;
            }
        }

        private void attachScrollbarEvent()
        {
            var eventRegister = scrollView.verticalScrollbar.onValueChanged;
            // clear old listners
            while (eventRegister.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(eventRegister, 0);
            var playlistEvents = UdonSharpEditorUtility.GetBackingUdonBehaviour(playlist);
            var customEvent = new UnityAction<string>(playlistEvents.SendCustomEvent);

            UnityEventTools.AddStringPersistentListener(eventRegister, customEvent, nameof(playlist._UpdateView));
        }

        #endregion
    }
}