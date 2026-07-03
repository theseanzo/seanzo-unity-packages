Shader "Hidden/Workbench/Framing"
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

        // Pass 0: Letterbox
        Pass
        {
            Name "Letterbox"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float targetAspect = max(0.1, _FloatParam1);
                float screenAspect = SOURCE_TEXEL_SIZE.z / SOURCE_TEXEL_SIZE.w;
                float4 c = SAMPLE_SOURCE( input.texcoord);
                
                // If Screen is Taller than Target (16:9 screen, 2.35 target) -> Horizontal Bars
                if (screenAspect < targetAspect) {
                    float visibleHeight = screenAspect / targetAspect;
                    float bar = (1.0 - visibleHeight) * 0.5;
                    if (input.texcoord.y < bar || input.texcoord.y > (1.0 - bar)) return float4(0,0,0,1);
                } 
                // If Screen is Wider than Target (Ultrawide screen, 16:9 target) -> Vertical Bars
                else {
                    float visibleWidth = targetAspect / screenAspect;
                    float bar = (1.0 - visibleWidth) * 0.5;
                    if (input.texcoord.x < bar || input.texcoord.x > (1.0 - bar)) return float4(0,0,0,1);
                }
                
                return c;
            }
            ENDHLSL
        }

        // Pass 1: Border
        Pass
        {
            Name "Border"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float width = _FloatParam1;
                float3 color = _VectorParam1.rgb;
                float4 c = SAMPLE_SOURCE( input.texcoord);
                
                if (input.texcoord.x < width || input.texcoord.x > 1.0-width || input.texcoord.y < width || input.texcoord.y > 1.0-width)
                    return float4(color, 1);
                return c;
            }
            ENDHLSL
        }
    }
}