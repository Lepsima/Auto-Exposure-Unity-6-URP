# About
Auto Exposure Shader for Unity 6.0 (tested in 6000.0.51f1) using URP.

This shader uses a custom render feature and needs to be applied to the post processing volume to see the settings
#
The default settings are set in a deep space scenario, the shader hasn't been tested on other scenarios.
This repository also includes a custom skybox used in that scenario, a dark background with a procedural HDR sun.

I'll try to make a proper .unitypackage file for easier installation

# Usage
Make sure the project uses Unity 6.0 and the Universal Render Pipeline, and move the repository files to your project's asset folder

Inside your Universal Renderer Data:
- Add a new render feature: **Auto Exposure Render Feature**
- Set the Render Shader to: **Lepsima/AutoExposure**
- Set the Compute Shader to: **AverageExposureCS**
- Warnings may show up, finish the setup first.

#

On your scene:
- Go to your post processing volume
- Add a new override, search for "Auto Exposure"
- The default values are set on a deep space scene, leave them for now

#

Now depending if you have a camera stack setup or not, follow the appropiate steps:

Main Camera Setup (Single Camera):
- The camera should have "Post Processing" enabled
- The "Renderer" should be the one you added the feature in the first step


Camera Stack Setup (Multi Camera)
- The last camera to be rendered (NOT the base one) is the camera where the effect should be applied
- Make sure the "Renderer" of the effect camera is the same one you added the render feature to.
- The other cameras should have a DIFFERENT "Renderer", if this is not the case, make a new one or clone the current one and REMOVE the Auto Exposure feature
- If multiple cameras have the same renderer the effect could be applied multiple times

#

This repository includes a custom made skybox that creates a customizable HDR sun with a completely black background.

If the skybox doesn't fit, just make sure that there is an very bright HDR object where the sun should be.
