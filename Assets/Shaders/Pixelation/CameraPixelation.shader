Shader "Pixelation/CameraPixelation"
{
	Properties 
	{
	    [HideInInspector] _MainTex ("BaseMap", 2D) = "white" {}
	}
	SubShader 
	{
		Tags
		{
		    "RenderType"="Opaque"
		}
		LOD 200
		
		Pass
		{
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#include "Assets/Shaders/Pixelation/PixelationDefines.hlsl"

			#pragma vertex vert
			#pragma fragment frag
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_CameraPixelationDepthTexture);

            float2 _CameraOffset;

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                output.uv = input.uv;

                return output;
            }
            
            half4 sampleColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }
            float sampleDepth(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_CameraPixelationDepthTexture, sampler_MainTex, uv).x;
            }

            half4 frag(Varyings input, out float depth : SV_Depth) : SV_Target 
            {
                int pixelSize = PIXELATION_PIXEL_SIZE;

                float2 offset = -pixelSize * 0.5f;
                offset.x += fmod(
                    floor(input.vertex.x + _CameraOffset.x),
                    pixelSize
                );
                offset.y += fmod(
                    floor(input.vertex.y + _CameraOffset.y),
                    pixelSize
                );
                offset /= _ScreenParams.xy;

                float2 targetPixel = input.uv - offset;
                
                // <TODO> Maybe implement same depth corner-only pixelation thing for the DGPixelation?
                float d = sampleDepth(input.uv);
                depth = (d == 0) ? sampleDepth(targetPixel) : d;

                half4 col = sampleColor(targetPixel);
                col.xyz = gradeColor(col.xyz, PIXELATION_COLOR_VARIATION);

                // Pixelation offset testing
                /*
                float2 gridOffset = fmod(floor(input.vertex.xy + _CameraOffset), 32);
                gridOffset /= _ScreenParams.xy;
                if (gridOffset.x * gridOffset.y == 0)
                    return float4(0, 1, 0, 1);
                */

                return col;
            }

			ENDHLSL
		}
        
	} 
	FallBack "Diffuse"
}