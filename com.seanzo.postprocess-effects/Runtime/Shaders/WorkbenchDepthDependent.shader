Shader "Hidden/Workbench/DepthDependent"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "WorkbenchCommon.hlsl"
        ENDHLSL

        // Pass 0: Fog
        Pass
        {
            Name "Fog"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragFog

            float4 FragFog(Varyings input) : SV_Target
            {
                float4 color = SAMPLE_SOURCE(input.texcoord);
                float depth = GetLinearDepth(input.texcoord);

                // Skip background (depth ~ 1.0) to match rust's default output
                if (depth >= 0.9999)
                    return color;

                // Params: 1=Density, 2=Start, 3=End, V1=Color
                float density = _FloatParam1;
                float start = _FloatParam2;
                float end = _FloatParam3;
                float3 fogColor = _VectorParam1.rgb;

                float fogFactor = saturate((depth - start) / (end - start));
                float blend = saturate(fogFactor * density);
                // Simple linear for now, extend for modes

                return float4(lerp(color.rgb, fogColor, blend), color.a);
            }
            ENDHLSL
        }

        // Pass 1: Edge Detection
        Pass
        {
            Name "EdgeDetection"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragEdge

            float4 FragEdge(Varyings input) : SV_Target
            {
                float4 color = SAMPLE_SOURCE(input.texcoord);
                float2 uv = input.texcoord;
                float2 texel = SOURCE_TEXEL_SIZE.xy * _FloatParam2;

                // Sobel Depth
                float d00 = GetLinearDepth(uv + texel * float2(-1, -1));
                float d10 = GetLinearDepth(uv + texel * float2( 0, -1));
                float d20 = GetLinearDepth(uv + texel * float2( 1, -1));
                float d01 = GetLinearDepth(uv + texel * float2(-1,  0));
                float d21 = GetLinearDepth(uv + texel * float2( 1,  0));
                float d02 = GetLinearDepth(uv + texel * float2(-1,  1));
                float d12 = GetLinearDepth(uv + texel * float2( 0,  1));
                float d22 = GetLinearDepth(uv + texel * float2( 1,  1));

                float gx = -1*d00 -2*d01 -1*d02 + 1*d20 + 2*d21 + 1*d22;
                float gy = -1*d00 -2*d10 -1*d20 + 1*d02 + 2*d12 + 1*d22;

                float edge = sqrt(gx*gx + gy*gy);

                // Params: 1=Threshold, 2=Thickness, 3=Intensity, V1=EdgeColor
                float threshold = _FloatParam1;
                float intensity = _FloatParam3;
                float3 edgeColor = _VectorParam1.rgb;

                float edgeBlend = saturate((edge - threshold) * intensity);
                return float4(lerp(color.rgb, edgeColor, edgeBlend), color.a);
            }
            ENDHLSL
        }

        // Pass 2: SSAO (Simplified for brevity, usually requires multi-pass)
        Pass
        {
            Name "SSAO"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSSAO

            float4 FragSSAO(Varyings input) : SV_Target
            {
                float4 color = SAMPLE_SOURCE(input.texcoord);
                // Real SSAO requires View Space reconstruction and noise texture
                // Placeholder tint based on depth
                float ao = 1.0;
                return color * ao;
            }
            ENDHLSL
        }
    }
}
