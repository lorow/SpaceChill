# Fake Glass

A glass shader for Unity/VRchat that (ab)uses some Unity features to get nice, clean looking glass without a heavy performance impact. Unlike many other glass shaders, it does not use a Grab Pass, saving a lot of performance.

![Preview](https://files.catbox.moe/sf5q2z.jpg)

## Installation

Download the repository. Then place the Shader/ folder with the shader into your Assets/ directory.

## Usage (Normal)

You must have reflection probes in the scene for this shader to work properly.

The Diffuse Colour setting controls the solidity of the glass through the alpha channel. 

The Glow Strength will add a soft glow coming from the "inside" of the glass. It's multiplied by the Tint Texture. 

To control the refraction effect, use the IOR and Refraction Power sliders.

The Surface Mask will define "foggy" areas of the glass. The brighter it is, the blurrier the glass gets. It uses the reflection probe for this, so players and other objects won't be seen in foggy glass. 

If you're using this on a single glass object, it's recommended to place a reflection probe in the middle of the object, and assign it as the object's Anchor Override. 

Play around with the options until it looks nice. 

## Usage (Rain)

When using the rain versions, make sure the Rain Mask, Droplet Normals, and Ripple Normals textures are assigned.

By default, the speed of the rain is 1 cycle/second. Depending on how hard you want the rain to look, you can lower this to make the rain's pattern less noticeable.

The different channels of the mask channel correspond to different aspects of the rain effect. If you want to change how the rain effect looks, here's what you need to know.
* Red channel controls the ripple effect on the ground.
* Green channel controls the droplet streak effect on walls.
* Blue channel controls the mask over the droplets to give them the appearance of motion.

## UI is weird!

Probably will be fixed later.

## License?

This work is licensed under a [Creative Commons Attribution-ShareAlike 4.0 International License](http://creativecommons.org/licenses/by-sa/4.0/)