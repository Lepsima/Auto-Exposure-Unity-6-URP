// Special skybox for adding a sun shaped and oriented HDR source in a black skybox
Shader "Skybox/AE_SpaceSkybox" {
    Properties {
    	_Color ("Color", Color) = (1, 1, 1, 1)
        [Header(Sun Size. Make it small if bloom is ON)]_Size ("Size", Float) = 0.002
    	[Header(HDR Luminance. Can be VERY high)]_Glow ("Luminance", Float) = 300000
    }
    
    SubShader {
        Tags {
        	"Queue"="Background"
        	"RenderType"="Background"
        	"PreviewType"="Skybox"
        }
        
        Pass {
            Name "Background"
            
            Cull Off
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            float _Size;
            float _Glow;
            float4 _Color;
            
			struct Attributes {
			    float4 vertex : POSITION;
			};
            
			struct Varyings {
			    float4 positionSS : SV_POSITION;
				float3 positionOS : TEXCOORD0;
			};

			Varyings vert(Attributes IN) {
				Varyings OUT;
				OUT.positionSS = UnityObjectToClipPos(IN.vertex.xyz);
				OUT.positionOS = IN.vertex.xyz;
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target {
				float minus = (1.0f - _Size) * 0.05f + 0.95f;
				float3 eye_ray = normalize(mul((float3x3)unity_ObjectToWorld, IN.positionOS));

				float light_angle = dot(_WorldSpaceLightPos0.xyz, eye_ray) - minus;
				return _Color * clamp(light_angle, 0.0f, 1.0f) * _Glow;
			}
            ENDHLSL
        }
    }
}