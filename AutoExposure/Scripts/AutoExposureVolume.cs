using System;
using UnityEngine;
using UnityEngine.Rendering;

// ReSharper disable once CheckNamespace
namespace Lepsima.Shaders.AutoExposure {

[Serializable]
[VolumeComponentMenu("Post-processing/AutoExposure")]
public class AutoExposure : VolumeComponent, IPostProcessComponent {
	
	[Tooltip("How many frames it takes to recalculate the average brightness. Decreasing this value can affect performance.")]
	public ClampedIntParameter framesPerCompute = new(5, 1, 20);

	[Space]
	[Tooltip("Limits the maximum brightness for very bright objects to work properly with bloom and other effects")]
	public FloatParameter brightnessLimit = new(15f);
	
	[Tooltip("For reinhard tonemapping")]
	public FloatParameter whitePoint = new(3f);

	[Tooltip("How much an individual pixel's exposure can vary from the original value.")]
	public FloatRangeParameter exposureRange = new(new Vector2(-2.5f, 0.6f), -6.0f, 6.0f);
	
	[Space]
	[Tooltip("How quickly the exposure increases")]
	public FloatParameter increaseSpeed = new(5f);
	
	[Tooltip("How quickly the exposure decreases")]
	public FloatParameter decreaseSpeed = new(5f);
	
	public bool IsActive() => framesPerCompute.overrideState;
}
}