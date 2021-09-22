# ArchiTechAnon ProTV Asset

## SUPPORT ME!
<a href='https://ko-fi.com/I3I84I3Z8' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Support me at ko-fi.com' /></a>

## Requirements
- Ensure latest VRCSDK3 (Udon) is imported (last tested with v2021.08.04.15.07)
- Ensure latest UdonSharp version is imported (last tested with [v0.20.2](https://github.com/MerlinVR/UdonSharp/releases/download/0.20.2/UdonSharp_v0.20.2.unitypackage))
- Import this package
- Done.

## Upgrading from 1.x
Due to the complete rewrite and rebuild with 2.x, there is no specific upgrade path. It is directly recommended to delete the old TV asset entierly before importing ProTV.

## Basic Usage
- Drag a ProTV prefab (located at `Assets/ArchiTechAnon/ProTV/Prefabs`) into your scene wherever you like, rotate in-scene and customize as needed.

You can find more about the ready-made TVs in the [`Prefabs Document`](./Docs/Prefabs.md).

## Features
- Full media synchronization (play/pause/stop/seek/loop)
- Resiliant and automatic sync correction for both Audio/Video and Time sync
- Sub-second sync delta between viewers
- Automatic ownership management
- Local only mode, for TVs that need to operate independently for all users.
- Media resync and reload capability
- 3D/2D audio toggle
- Near frame-perfect video looping (audio looping isn't always frame-perfect, depends on the video's codec)
- Media autoplay URL support
- Media autoplay delay offsets which help mitigate ratelimit issues with multiple TVs
- Media url params support (t/start/end/loop)
- Video player swap management for multiple video player configurations
- Pub/Sub event system for modular extension
- Instance owner/master locking support (master control is configurable, instance owner is always allowed)

## Core Architecture
In addition to the standard proxy controls for video players (play/pause/stop/volume/seek/etc), the two main unique driving factors that the core architecture accomplishes is event driven modularity as well as the multi-configuration management/swap mechanism.

ProTV 2.0 has been re-architected to be more modular and extensible. This is done through a pseudo pub/sub system. In essence, a behavior will pass its own reference to the TV (supports both ugraph and usharp) and then will receive custom events (see the [`Events Document`](./Docs/Events.md)) based on the TV's activity and state. The types of events directly reflect the various supported core features of the TV, such as the standard video and audio controls, as well as the video player swap mechanism for managing multiple configurations.

More details about the core architecture can be found in the [`Architecture Document`](./Docs/Architecture.md).  
Details for ready-made plugins for the TV can be found in the [`Plugins Document`](./Docs/Plugins.md).  

## Core Settings
- *Autoplay URL*  
Pretty straight forward. This field is a place to put a video that you wish to automatically start upon first load into the world. This only is true for the instance owner (aka master) upon first visit of a new instance (which is then synced to any late-joiners). Leave empty to not have any autoplay.

- *Autoplay Start Offset*  
With the Autoplay Start Offset, which is for when there are more than 1 TV in the world, you can tell the TV to wait for X + 3 seconds before loading the sync'd (or autoplay'd) URL after joining the instance.  
The 3 seconds is required as a buffer for the world to load in, and the field's value is added to that. It's recommended to offset each TV in the world by a 4 or 5 seconds, any less than that has risk of triggering the rate limiting, especially for those that have slower internet.   
If you only have 1 TV in the world, you don't need to worry about this.  

- *Paused Resync Threshold*  
This threshold value is used to determine how much of an offset is allowed from the sync time (video player becoming out-of-sync from owner) before forcing non-owners to jump to the current timestamp that the owner is at.  
One way to view it is as a live slideshow of what is currently playing. This is intended to allow people to see what is visible on the TV without actually having the media actively running.  

- *Automatic Resync Interval*
By default the TV will automatically trigger the resync logic every some number of seconds. This option specifies how often to automatically resync.  
The defaule value of 30 seconds is usually good enough for most cases, and shouldn't need to be changed unless for a specific need.

- *Initial Volume/Initial Player*  
Again, pretty straight forward. The initial volume determines at what volume the TV will start at upon joining the world.  
Like-wise, the initial player determines which video player configuration to load upon joining the world. The value is a number that represents the index of the Video Players array.

- *Retry Live Media*
This value defines how many attempts the TV should make when trying to load/reload a live stream. The purpose for this is to allow for a livestream to soft-fail without the videoplayer giving up entirely on that stream. It will attempt to reload the given number of times before finally accepting that the stream actually did end.

- *Start Hidden*  
This toggle makes it so the TV disables itself (via SetActive) after it has completed all initialization.  
**IMPORTANT**: DO NOT DISABLE THE TV VIA INSPECTOR OR SEPARATE UDON BEHAVIOR UPON JOINING A WORLD. This will cause issues with the video players. Only use this setting to have the TV be hidden when people join the world. After that any enable/disabling by the user or other scripts is fine. Just not on world join.

- *Allow Master Control*  
This is a setting that specifies whether or not the instance master is allowed to lock down the TV to master use only. This will prevent all other users from being able to tell the TV to play something else. NOTE: The instance _Owner_ will always have access to take control no matter what.

- *Locked By Default*
This is a setting that specifies whether or not the TV is master locked from the start. This only affects when the first user joins the instance as master. The locked state is synced for non-owners/late-joiners after that point.

- *Sync To Owner*  
This setting determines whether the TV will sync with the data that the owner delivers. Untick if you want the TV to be local only (like for a music player or something)


## Caveats
- General reminder: Not all websites are supported, especially those that implement custom video players. Sometimes the player is able to resolve those to a raw video url. Feel free to see what works.

- Due to a temporary limitation in Udon, I cannot completely remove the directionality of the default speakers when switching from 3D audio to 2D audio. Once that limitation is lifted, I will fix that.

- Quest does not have access to the YoutubeDL tool that desktop uses for resolving youtube/twitch/etc urls, due to technical limitations (not VRC devs fault). Here are some solutions to work around this:
    1) Download [YoutubeDL](https://github.com/ytdl-org/youtube-dl/releases/) yourself and resolve the URL locally and then put that resulting URL into the TV instead.  
    You will need to add the executable to the path ([HERE](https://www.c-sharpcorner.com/article/add-a-directory-to-path-environment-variable-in-windows-10/) is one of many articles online describing how to do so) and then once that's done, open up your desired terminal (most likely the Command Line on windows) and type in the following command: `youtube-dl -g -f "best" https://YourLinkHere`
    Copy the url that it spits out and paste into the TV. Done!
    2) Use an online tool like [GetVid](https://getvideo.org/en) or any one of the invidious instances that are around (there are various others as well). Just paste your url in and copy the desired video download link you want to play.

- *Be aware* that all youtube and twitch long-form urls have an embedded expiration for that url. This means that when you put the video into the player, anytime someone tries to (re)load the video (such as a late joiner), if the expiration has passed the video won't load. You'll need to refresh the URL every like 15 minutes or so.  
Not all sites have that, but it is known that youtube and twitch both implement the expiry limitation in their direct urls.

- There is a known issue with _low-latency mode_ on AMD GPUs (specifically older ones) where if an HLS livestream (like twitch or vrcdn) is sending too much data (such as a 1080p stream), there is significant artifacting and flickering that will likely occur. The easiest solution is to drop the stream quality by switching to a lower resolution video player, or by grabbing a specifically lower quality stream URL via the [YoutubeDL](https://github.com/ytdl-org/) tool mentioned already. A good universal quality that works well across the board is 480p.  
The command to specify the resolution is: `youtube-dl -g -f "best[height<=480]" https://YourLinkHere`
