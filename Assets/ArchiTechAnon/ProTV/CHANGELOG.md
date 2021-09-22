# ArchiTechAnon TV Asset

## 2.0 Beta 8.4 (Current Version)
- Fixed entry placement regression in playlist auto-grid.
- Add skybox support for CubeMap style 360 video.
- Add skybox support for 3D video modes SideBySide and OverUnder.
- Add skybox support for brightness control.
- Add settings UI to the skybox TV prefab.
- Updated demo scene with new skybox data.
- Add custom meta support for the URLs. 
    - Can now specify custom data that is arbitrarily stored in the TV in the `string[] urlMeta` variable.
    - All meta entries are separated by a `;` and proceeds a hash (`#`) in the URL.
    - Example: With a url like `https://vimeo.com/207571146#Panoramic;OverUnder`, the `urlMeta` field will contain both `"Panoramic"` and `"OverUnder"`.
    - This meta portion of the URL can be used for pretty much anything as anything as the hash of a URL is ignored by servers. Use it to store information about any particular individual url (such as what skybox modes to apply).

## 2.0 Beta 8.3
- Fixed playlist auto-grid being limited to 255 rows or columns. Should be able to have many more than that now.
- Fixed playlist in-game performance issues by swapping from game object toggling to canvas component toggling.
    - This specifically fixes lag issue when desiring to hide the playlist.   
    While game object toggling is still supported, this new mode is highly recommended. Is utilized by calling `playlist -> _Enable/_Disable/_Toggle` events.
    - Playlist Search also makes use of this performance improvements by having a canvas component on the template root object (and thus on every playlist entry object).

## 2.0 Beta 8.2
- Cleaned up names of prefabs a bit (no breaking changes)
- Exported with 2019 LTS
- Added KoFi support links to the Docs. Support is inifinitely appreciated!
- Added Micro style controls to the MediaControls plugin.
- Added a one-off play url button control to the MediaControls plugin. This has definitely been requested quite a bit.

## 2.0 Beta 8.1
- Added better support for plugins being disabled by default getting enabled after the world load phase.
    - This guarantees that AT LEAST the `_TvReady` event will _ALWAYS_ be the first event called on a subscribed behavior.
- Fixed support for start/end time usage. 
    - Adds script variable `videoLength` to represent the full length of the backing video, where `videoDuration` now represents the amount of time between the start and end time of a video.
- Update Controls script to utilize the new usage of `videoDuration` and to properly display when the time is less than start time (for example if the AVPro buffer bug prevents the complete auto-seek that is expected, it will have the current time be a negative value)
- Add playlist search toggle for skipping playlists who's gameobject is disabled.  
- Change default automatic resync interval to 5 minutes.  
- Fixed initial volume not being assigned properly during the Start phase.
- Update VideoManagerV2 to rework the configuration options to have clearer names as well as more precise purpose.  


## 2.0 Beta 8.0
- Fixed playlist performance issues. 
- Added pagination to the playlist inspector for easier navigation.
- Playlist titles are now no longer limited to 140 characters
- Added playlist search prefab (part of the Playlist plugin system)
    - PROTIP: To add extra text to search by in a title, you can set the text size to any part of the title to 0  
    Such as: `Epic Meme Compilation #420 <size=0>2008 2012 ancient throwback classic </size>`
- Final reorganization of folder structure.
    - The root folder has been renamed from `TV` to `ProTV`
    - Updated documentation to reflect the update folder structure.
    - Anything that used to be in the `TV/Scripts/Plugins` folder is in their respective `ProTV/Plugins/*` folders.
    - All plugin specific files have been moved to the plugin specific folders (eg: `TV/Stuff/UI` -> `ProTv/Plugins/MediaControls/UI`)
    - The base `Stuff` folder has been removed in favor of individual folders.
- Finally remove the ProTV v1 TVManager and VideoManager (the legacy ones that should no longer be in use anyways)
- Fixed the MediaControls dropdown nested canvas issue (the one where the cursor hid parts of the menu)
- Add configuration options to `VideoManagerV2` for defining how the audio is handled during video player swap.
- Add missing and cleanup existing documentation.
- Fix improper queue behavior when the TV is in a locked state.

#### Known Issues
- If the owner has a video paused and a late joiner joins, the video won't be paused for them, it'll still play.
- (AVPro issue) Unable to seek to any point in the video until the download buffer (internal to AVPro) has reached that point.
- When testing locally, it is recommended NOT to disable the `Allow Master Control`. Due to an issue with how instance owner works locally, you will get locked out of the TV if you have `Locked By Default` enabled. This issue is NOT present once uploaded to VRChat servers, and can be safely disabled prior to uploading if the feature is needed.
- (*WHEN UPGRADING FROM BETA 6.8 OR PRIOR*) To complete the upgrade, you need to manually rename the file `SimplePlaylist.cs` to  `Playlist.cs`, which was located at `Assets/ArchiTechAnon/TV/Scripts/Plugins`, because unity hates file name changes apparently.
- (*WHEN UPGRADING FROM BETA 7.1 OR EARLIER*) If you have any playlists in your scene you will need to click the "Update Scene" button on each of them to regenerate the scene structure for the new click detection required for uncapped playlist entry count.

## 2.0 Beta 7.1
- Added a configurable auto resync interval that will trigger a resync for both Audio/Video and time sync between users. 
    - This helps ensure tight and accurate playback between all users, even in certain low performance situations.
- Removed the `Playing Threshold` configuration option as it's no longer used.
- Create folder `Assets/ArchiTechAnon/TV/Plugins` as the location for all plugin specific things to be moved to prior to official release.
- Updated 360 video from a sphere mesh to a new custom skybox swap mechanism.  
This is available as a prefab in `Assets/ArchiTechAnon/TV/Plugins/SkyboxSwapper`
- Fix improper implementation of _ChangeSeek* methods.
    - `_ChangeSeekTime` and `_ChangeSeekTimeTo(float)` now operate with an explicit time in seconds.  
    It uses the variable `IN_ChangeSeekTo_float_Seconds`.
    - Added `_ChangeSeekPercent` and `_ChangeSeekPercentTo(float)` to operate with a normalized percent value between 0.0 and 1.0.  
    It uses the variable `IN_ChangeSeekPercent_float_Percent`.
    It automatically takes into consideration any custom start and end time given via query parameters. 

## 2.0 Beta 7.0
- Add new Queue plugin.
- Fix various stability issues with live streams
- Aspect-Ratio now renders correctly (Thanks Merlin & Texelsaur!)
- Image support and Auto-Grid support added to the playlist plugin
- Example of a 360 video usage added to the demo scene.
- Mitigated race condition for owner vs other when loading media.
- Corrected the implementation of the MediaChange event to occur at the correct times.
- Fix some edge-case issues with autoplay.
- Added mitigations for certain audio/video desync issues.
- All TV events have been renamed from using the `_On` prefix to using the `_Tv` prefix to avoid naming confusion with normal udon events.
    - Example: `_OnPlay` would be `_TvPlay` and `_OnMediaStart` is now `_TvMediaStart`
    - NOTE: The outgoing variable names have also been updated respectively. Example: `OUT_OnOwnerChange_int_Id` is now `OUT_TvOwnerChange_int_Id`
- Simplified extension script and plugin names
    - `SimplePlaylist` is now just `Playlist`
    - Previously mentioned `LiveQueue` (new in this release) is going to be called simply `Queue` 
- Renamed `allowMasterLockToggle` flag on `TVManagerV2` to `allowMasterControl` for clarity on how the flag is actually used.

#### Known issues
- If TV owner has the player paused, late joiners will still play the video on join until a sync check occurs (play/pause/stop/seek/etc).

## 2.0 Beta 6
- Update sync data to take advantage of the Udon Network Update (UNU) changes
- Move all occasional data into manually synced variables
- Add network lag compensation logic to improve sync time accuracy
- Remove the playerId value from the info display text
- `BasicUI.cs` and `SlimUI.cs` have been merged into a single plugin `UnifiedControls_ActiveState.cs`
- Many refinements to the controls UI plugin
- Remove loop buttons; looping is now controlled exclusively by the loop url parameter
- Add Resync UI action for triggering the sync enforcement for one frame (in case the video sync drifts)
- Add Reload UI action for explicitly doing a media reload with a single click (just does _Stop then _Play behind the scenes)
- Adjusted some UI layout parameters for better structure
- `BasicUI` plugin has been rebuilt as the `GeneralControls` plugin
- `SlimUI` plugin has been rebuilt as the `SlimControls` plugin
- `SlimUIReduced` plugin has been rebuilt as the `MinifiedControls` plugin
- Updated the example scene to account for the controls plugins changes
- Update playlist inspector to accept pulling playlist info from a custom txt file


## 2.0 Beta 5
- Modify how time sync works. It now only enforces sync time from owner for the first few seconds, and then any time a state change of the TV affects the current sync time. Basically, the enforcement is a bit more lax to help support Quest playback better.
- Update the UIs to make use of the modified sync time activity. Sync button is now an actual "Resync" action, that will do a one-time activation of the sync time enforcement which will jump the video to the current time sync from the owner.
- Start using a formal CHANGELOG
