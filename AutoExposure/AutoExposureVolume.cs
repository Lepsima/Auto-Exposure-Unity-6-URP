using System;
using UnityEngine;
using UnityEngine.Rendering;

// ReSharper disable once CheckNamespace
namespace Lepsima.Shaders.AutoExposure {

[Serializable]
[VolumeComponentMenu("Post-processing/AutoExposure")]
public class AutoExposure : VolumeComponent, IPostProcessComponent {
	[Tooltip("How many frames the compute pass stays inactive before executing again")]
	public ClampedIntParameter framesPerCompute = new(3, 1, 10);
	
	[Tooltip("Limits the average exposure computed, important to set the correct upper bound when looking at bright objects")]
	public FloatRangeParameter globalExposureRange = new(new Vector2(0f, 20.0f), 0f, 30f);
	
	[Tooltip("Sets the range of values (in terms of percentages) of the histogram that are accepted while finding a stable average exposure. Anything outside the value is discarded.")]
	public FloatRangeParameter exposureRange = new(new Vector2(-2.5f, 0.6f), -6.0f, 6.0f);

	[Tooltip("How quickly the exposure increases")]
	public FloatParameter increaseSpeed = new(10f);
	
	[Tooltip("How quickly the exposure decreases")]
	public FloatParameter decreaseSpeed = new(5f);
	
	[Tooltip("For reinhard tonemapping")]
	public FloatParameter whitePoint = new(3f);
	
	public bool IsActive() => framesPerCompute.overrideState;
}
}