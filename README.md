# About
Auto Exposure for URP, Unity 6.0 (tested in 6000.0.51f1).

Includes the shaders and scripts needed to set up a simple auto exposure shader in URP, using a custom render feature and a volume override.

To set up the shader, follow the steps below.

# Usage
Make sure the project uses Unity 6.0 and the Universal Render Pipeline

First, import the .unitypackage file from the latest release.

Inside your Universal Renderer Data:
- Add a new render feature: **Auto Exposure Render Feature**
- Set the Render Shader to: **AutoExposure** (once set will show "Lepsima/AutoExposure")
- Set the Compute Shader to: **AverageExposureCS**
- Warnings may show up, finish the setup first.

#

On your scene:
- Go to your post processing volume
- Add a new override, search for "Auto Exposure"
- Enable all the settings
- Leave the default values for now

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

Setup complete! Additionally, this repository includes a custom made skybox that creates a customizable HDR sun with a completely black background.

If the skybox doesn't fit with your project, just make sure that there is an very bright HDR object where the sun should be.
