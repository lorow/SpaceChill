# Fax Analytics

## Summary

This package lets you gather data about users who visit your world! 

You can use `AreaAnalytics` prefabs in your world to track... 

- What areas of your world users visit
- How much time users spend in each area
- How many users fell off the map, or discovered secret areas
- What your users (dis)like about your world

You'll get that info in Google Forms (or an alternative service) as a pie chart. Or you can redirect the data to a spreadsheet.

There's once caveat: Users must have "Allow Untrusted URLs" enabled in their VRChat settings. You can show users a prompt to enable the setting.

## Requirements
- Some knowledge about VRChat world creation
- VRChat SDK3
- UdonSharp by MerlinVR: https://github.com/MerlinVR/UdonSharp

## How to use

1. Add an `AreaAnalytics` prefab to your scene. 

    - Move / resize the `Collider` to cover the area you'd like to keep track of.

    - Or delete the collider. Then the Analytics component will be triggered as soon as the `GameObject` becomes active.

2. Enter a URL into the `AreaAnalytics` prefab. You might want to use a pre-filled Google Form for this.

    - In Unity, the the menu `Fax Analytics -> How-To: Google Forms` for a manual. There you can convert your Google Forms URL automatically.
    - Repeat this step for every area of your map you would like to track.

3. Add an `AnalyticsManager` prefab to your scene. You'll need to add references to every `AreaAnalytics` you've created.
    - Areas in `Areas to check periodically` are useful knowing how much time players spend in a given area of the map. You can change how often these areas are check via the `Periodic Check Frequency`.
    - Areas in `Areas To Check Once When Triggered` are useful for things you'd only like to check once. I. e. if they fell off the map once, or if they pressed a button. The area prefab's GameObject is disabled after the player triggers it once.
      - If you manually re-enable an area again, it'll be checked again, too.

4. That's it! Though there's some optional features you can use.
    - The `AnalyticsManager` lets you show specific `GameObjects` depending on the player's "Allow Untrusted URLs" setting. You can leave these empty.
      - In the example scene this is used to let the user rate the world. The prompts change depending on the user's settings.
    - The `AnalyticsManager`'s `Url Player Is In Between Areas` can be used to track players who are not in an area listed in `Areas to check periodically`.

## Example scene

Open the `AnalyticsExampleScene` for an example of what Fax Analytics can do.
- The scene features a left and right area. As the player moves around, Fax Analytics tracks whether the player is in the left area, the right area or in neither area.
- It also tracks if a player falls off the map (once), or if they press the test button (and how many times).
- Players can rate the map. They see an error message if something goes wrong. And they can say what they like about the world.
  - The like / dislike and the player's reason are submitted together. So you can figure out *why* players did (not) like your world.
- You can see the live results here: https://docs.google.com/forms/d/1xWywUZUnnNSxj7zuXu524q5qdjHc0YlW3nSSGIBTedU/edit#responses

## Known issues

* VRChat likes to retry loading a video *once* after failing. Each response gets submitted twice. You could filter this data after exporting it to a spreadsheet.
* Enabling "Allow Untrusted URLs" and then disabling it will prevent analytics from working again. The user will not see an error message.
* Setting up the Like / Dislike system for worlds is quite difficult. Hit me up on Discord if you need any help (Fax#6041)
* If you're using Google Forms, avoid answers with multiple words. For example "LovePugs" instead of "Love Pugs". Spaces get turned into plus symbols in the URL, and VRChat doesn't like that. 

## License

You can use and modify this, but please credit me 🤠

![Faxample_Result](D:\Repos\AnalyticsForVRChat\Assets\FaxAnalytics\Textures\Faxample_Result.png)