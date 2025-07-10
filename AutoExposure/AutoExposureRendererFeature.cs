using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// ReSharper disable once CheckNamespace
namespace Lepsima.Shaders.AutoExposure {
	
public class AutoExposureRendererFeature : ScriptableRendererFeature {
	
	// "After Post-Processing" is not recommended if bloom is enabled, it's manageable otherwise
	public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
	
	[Tooltip("The Blit Shader that applies the average luminance to the screen")]
	public Shader renderShader;
	[Tooltip("The Compute Shader that calculates the average luminance")]
	public ComputeShader computeShader;
	
	private AverageComputePass _computePass;// Computes the average exposure of the screen
	private ExposureRenderPass _renderPass;	// Applies the average exposure to the screen
    private Material _material;				// Auto-generated material using the render shader
	
    private static float _targetExposure = 0;	// Latest computed average exposure
    private static float _currentExposure = 0;	// The actual exposure, always approaching the target exposure
    private static float _lastTime = 0;			// The timestamp of the last compute pass
    private static int _computeCounter = 0;		// The amount of skipped compute passes, resets on every compute pass

    private static AutoExposure _autoExposure; // Reference to the Volume's auto exposure component
    
	public override void Create() {
		if (!SystemInfo.supportsComputeShaders) {
            Debug.LogWarning("Device does not support compute shaders. Auto Exposure will be disabled.");
			return;
		}

		if (!renderShader || !computeShader) {
			Debug.LogWarning("Render or Compute shader missing. Auto Exposure will be disabled.");
			return;
		}
		
		// Create materials and passes
		_material = CoreUtils.CreateEngineMaterial(renderShader);
		_computePass = new AverageComputePass(computeShader, renderPassEvent);
		_renderPass = new ExposureRenderPass(_material, renderPassEvent);

		// Set defaults
		_targetExposure = 0;
		_currentExposure = 0;
		_computeCounter = 0;
		_lastTime = Time.time;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		// Update the auto exposure component reference
		_autoExposure = VolumeManager.instance.stack.GetComponent<AutoExposure>();
		if (!_autoExposure || !_autoExposure.IsActive()) return;
		
		if (_computePass == null || _renderPass == null) {
			Debug.LogWarning("Graphics or Compute shader missing. The pass will be skipped.");
			return;
		}
		
		// Execute the compute pass once every X frames (3 by default), can be set to 1:1
		if (_computeCounter % Mathf.Max(_autoExposure.framesPerCompute.value, 1.0f) == 0) {
			renderer.EnqueuePass(_computePass);
			_computeCounter = 0;
		}
		_computeCounter++;
		
		// The render pass will take the last exposure compute result
		renderer.EnqueuePass(_renderPass);
		UpdateExposure();
	}

	private static void UpdateExposure() {
		if (!_autoExposure) return;
		
		// Delta time, using the built-in one seems to flicker a lot, weird
		float deltaTime = Time.time - _lastTime;
		_lastTime = Time.time;

		// Exposure difference, skip if zero
		float diff = _targetExposure - _currentExposure;
		if (diff == 0) return;
		
		/* Linear smoothing, stable and reliable, but very bad looking
		// Increase or Decrease the current exposure using the settings in the volume
		float delta;
		if (_currentExposure > _targetExposure)
				delta = Math.Max(diff, _autoExposure.decreaseSpeed.value * -deltaTime);
		else
			delta = Math.Min(diff, _autoExposure.increaseSpeed.value * deltaTime);
		
		_currentExposure += delta;
		*/

		float decay = diff > 0 ? _autoExposure.increaseSpeed.value : _autoExposure.decreaseSpeed.value;
		_currentExposure = ExpDecay(_currentExposure, _targetExposure, decay, deltaTime);

	}

	protected override void Dispose(bool disposing) {
		CoreUtils.Destroy(_material);
	}

	// Set the target exposure, 0 is full Black, 1 is full White and >1 is very bright White
	private static void SetExposure(float avg) {
		if (!_autoExposure) return;
		avg = Mathf.Max((Mathf.Log( avg, 2.0f) + 2.4f) * 0.3703704f, avg);
		
		float min = _autoExposure.globalExposureRange.value.x;
		float max = _autoExposure.globalExposureRange.value.y;
		
		_targetExposure = Mathf.Clamp(avg, min, max);
	}

	// FrameRate independent lerp, Source -> https://www.youtube.com/watch?v=LSNQuFEDOyQ
	public static float ExpDecay(float a, float b, float decay, float deltaTime) => b + (a - b) * Mathf.Exp(-decay * deltaTime);
	
	// Get the smoothed current exposure
	private static float GetExposure() => _currentExposure;
	
	// Compute Shader pass
	private class AverageComputePass : ScriptableRenderPass {
		// Shader parameters
		private static readonly int Dims = Shader.PropertyToID("_Dims");
		private static readonly int Source = Shader.PropertyToID("_Source");
        private static readonly int TempOut = Shader.PropertyToID("_OutBuffer");
        
        // Compute shader reference
        private const int ThreadGroupSize = 32;
        private readonly ComputeShader _computeShader;

        public AverageComputePass(ComputeShader compute, RenderPassEvent renderPassEvent) {
	        this.renderPassEvent = renderPassEvent;
	        _computeShader = compute;
        }
        
        private class PassData {
            public ComputeShader ComputeShader; // Shader reference
            public TextureHandle Texture;		// Screen texture
	        public Vector2Int Size;				// Size of the screen
            public BufferHandle Output;			// Output buffer
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
	        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
	        
	        // Screen texture
	        TextureHandle texture = resourceData.activeColorTexture;
	        TextureDesc textureDesc = texture.GetDescriptor(renderGraph);
	        
	        // Initialize output buffer, this needs to be done every time in case the screen changes size
	        Vector2Int size = new(textureDesc.width, textureDesc.height);
	        BufferDesc outBufferDesc = new(Mathf.CeilToInt( (float)size.y / ThreadGroupSize) * 2, 4) {
		        target = GraphicsBuffer.Target.Structured,
		        name = "Auto Exposure Output Buffer"
	        };
	        
	        // Create the buffer in the render graph
	        BufferHandle outputHandle = renderGraph.CreateBuffer(outBufferDesc);
            
	        // Build the compute pass
            using IComputeRenderGraphBuilder builder = renderGraph.AddComputePass("Avg Exposure Compute Pass", out PassData passData);
            
            // Set parameters
            passData.ComputeShader = _computeShader;
            passData.Texture = texture;
            passData.Output = outputHandle;
            passData.Size = size;
            
            builder.AllowPassCulling(false); // The output buffer is detected as unused by default, this isn't true
            builder.UseTexture(resourceData.activeColorTexture);
            builder.UseBuffer(passData.Output, AccessFlags.Write);
            builder.SetRenderFunc((PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
        }
        
        private static void ExecutePass(PassData data, ComputeGraphContext cgContext) {
	        ComputeCommandBuffer cmd = cgContext.cmd;
	        
	        // Set compute parameters and dispatch
	        cmd.SetComputeIntParams(data.ComputeShader, Dims, data.Size.x, data.Size.y);
	        cmd.SetComputeTextureParam(data.ComputeShader,0, Source, data.Texture);
            cmd.SetComputeBufferParam(data.ComputeShader,0, TempOut, data.Output);
            cmd.DispatchCompute(data.ComputeShader,0, data.Size.y, 1, 1);
            
            // Im unsure if doing this async is necessary, but seems to give better results
            AsyncGPUReadback.Request(data.Output, OnCompleteReadBack);
        }
        
        private static void OnCompleteReadBack(AsyncGPUReadbackRequest request) {
	        if (request.hasError || !_autoExposure) return;
	        
	        // Get Compute result
	        float[] groupValues = request.GetData<float>().ToArray();
	        
	        // Add up all the workgroup's results
	        double pixelLumTotal = 0;
	        uint pixels = 0;
	        for (int i = 0; i < groupValues.Length / 2; i++) {
		        pixelLumTotal += groupValues[i * 2];
		        pixels += (uint)Mathf.CeilToInt(groupValues[i * 2 + 1]);
	        }
	        
	        // Average the results and set as the target exposure
	        SetExposure((float)(pixelLumTotal / pixels));
        }
	}
	
	private class ExposureRenderPass : ScriptableRenderPass { 
		// Shader parameters
		private static readonly int ScreenExposureProp = Shader.PropertyToID("_Exposure");
		private static readonly int ExposureRangeProp = Shader.PropertyToID("_Range");
		private static readonly int WhitePointProp = Shader.PropertyToID("_WhitePoint");
		
		private const string PassNameTempRT = "Exposure Temp Pass";
		private const string PassNameCameraColor = "Exposure Camera Pass";
		private readonly Material _material;
		
		private class PassData {
			public TextureHandle Source;// Screen texture
			public float Exposure;		// Current exposure
			public Vector2 Range;		// Exposure range
			public float WhitePoint;	// Variable for tonemapping
			public Material Material;	// Material reference
		}
		
		public ExposureRenderPass(Material material, RenderPassEvent renderPassEvent) {
			this.renderPassEvent = renderPassEvent;
			_material = material;
			
			requiresIntermediateTexture = true;
			profilingSampler = new ProfilingSampler(nameof(ExposureRenderPass));
		}
		
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			
			// Get current screen texture
			TextureHandle cameraColorTextureHandle = resourceData.activeColorTexture;
			RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
			desc.msaaSamples = 1;
			desc.depthBufferBits = 0;

			// Create the temporal texture pass
			TextureHandle tempTextureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_TempRT", true);
			using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PassNameTempRT, out PassData passData, profilingSampler)) {
				builder.UseTexture(cameraColorTextureHandle);
				builder.SetRenderAttachment(tempTextureHandle, 0);

				passData.Source = cameraColorTextureHandle;
				passData.Material = _material;
				passData.Exposure = GetExposure();
				passData.Range = _autoExposure.exposureRange.value;
				passData.WhitePoint = _autoExposure.whitePoint.value;
				
				builder.SetRenderFunc((PassData data, RasterGraphContext graphContext) => ExecutePass(data, graphContext));
			}
			
			// Apply back the temporal texture to the screen
			using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PassNameCameraColor, out PassData passData, profilingSampler)) {
				builder.UseTexture(tempTextureHandle);
				builder.SetRenderAttachment(cameraColorTextureHandle, 0);
				
				passData.Source = tempTextureHandle;
				passData.Material = null;
				
				builder.SetRenderFunc((PassData data, RasterGraphContext graphContext) => ExecutePass(data, graphContext));
			}
		}

		private static void ExecutePass(PassData passData, RasterGraphContext graphContext) {
			RasterCommandBuffer cmd = graphContext.cmd;

			// Temporal texture pass
			if (passData.Material) {
				passData.Material.SetFloat(ScreenExposureProp, passData.Exposure);
				passData.Material.SetVector(ExposureRangeProp, passData.Range);
				passData.Material.SetFloat(WhitePointProp, passData.WhitePoint);
				Blitter.BlitTexture(cmd, passData.Source, new Vector4(1, 1, 0, 0), passData.Material, 0);
			}
			// Back to screen pass
			else {
				Blitter.BlitTexture(cmd, passData.Source, new Vector4(1, 1, 0, 0), 0, false);
			}
		}
	}
}
}