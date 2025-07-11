Shader "Lepsima/AutoExposure" {
    SubShader {
        
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        LOD 100
        ZWrite Off Cull Off
        
        Pass {
            Name "Auto Exposure Pass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            #pragma vertex Vert
            #pragma fragment frag

            SamplerState sampler_BlitTexture; // Screen texture

			float _MaxBrightness;	// Limit the final brightness to this value
            float _Exposure;		// The average exposure to work with
            float _WhitePoint;		// White point for reinhard tonemapping
            float2 _Range;			// Min and Max luminosity change, how much can each pixel be offset from it's original luminosity

            // Converts from RGB space to Yxy (float3.x is Luminance)
			float3 convert_rgb_to_Yxy(float3 rgb) {
			    float3 xyz;
			    xyz.x = dot(rgb, float3(0.4124f, 0.3576f, 0.1805f));
			    xyz.y = dot(rgb, float3(0.2126f, 0.7152f, 0.0722f));
			    xyz.z = dot(rgb, float3(0.0193f, 0.1192f, 0.9505f));

			    float sum = xyz.x + xyz.y + xyz.z;

			    float x = (sum > 0.0) ? (xyz.x / sum) : 0.0;
			    float y = (sum > 0.0) ? (xyz.y / sum) : 0.0;

			    return float3(xyz.y, x, y); // Y, x, y
			}

            // Converts from Yxy (float3.x is Luminance) space to RGB
            float3 convert_Yxy_to_rgb(float3 Yxy) {
			    float Y = Yxy.x;
			    float x = Yxy.y;
			    float y = Yxy.z;

			    float X = y > 0.0 ? x * Y / y : 0.0;
			    float Z = y > 0.0 ? (1.0 - x - y) * Y / y : 0.0;

			    float3 XYZ = float3(X, Y, Z);

			    float3x3 M_XYZ2RGB = float3x3(
			         3.2406f, -1.5372f, -0.4986f,
			        -0.9689f,  1.8758f,  0.0415f,
			         0.0557f, -0.2040f,  1.0570f
			    );

			    return mul(M_XYZ2RGB, XYZ);
			}

            // Tone Mapping function
            float reinhard2(float lp, float wp) {
                return lp * (1.0f + lp / (wp * wp)) / (1.0f + lp);
            }

			// Sample texture and return RGB and A in separate variables
			void sample_split(float2 uv, out float3 rgb, out float alpha) {
			    float4 color = _BlitTexture.SampleBias(sampler_BlitTexture, uv, _GlobalMipBias.x);;
			    rgb = color.rgb;
			    alpha = color.a;
			}
            
            // Fragment shader, adaptation of: https://bruop.github.io/tonemapping/
            float4 frag(Varyings input) : SV_Target {
            	float3 rgb;
            	float alpha;
                sample_split(input.texcoord, rgb, alpha);
 
                // Yxy.x is Y, the luminance
                float3 Yxy = convert_rgb_to_Yxy(rgb);
            	Yxy.x = min(Yxy.x, _MaxBrightness);

                // Tone mapping
                float lp = Yxy.x / (9.6f * _Exposure + 0.0001f);
                float new_lum = reinhard2(lp, _WhitePoint);

            	// Clamp the added luminosity and convert back to RGB
            	Yxy.x += clamp(new_lum - Yxy.x, _Range.x, _Range.y);
                rgb = convert_Yxy_to_rgb(Yxy);

            	return float4(rgb, alpha);
            }
            
            ENDHLSL
        }
    }
}