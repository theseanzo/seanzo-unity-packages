Shader "Hidden/Workbench/Color"
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

        // Pass 0: Posterize
        Pass
        {
            Name "Posterize"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float4 c = SAMPLE_SOURCE(input.texcoord);
                float levels = max(2, _FloatParam1);
                c.rgb = floor(c.rgb * (levels - 1.0) + 0.5) / (levels - 1.0);
                return c;
            }
            ENDHLSL
        }

        // Pass 1: White Balance (Placeholder)
        Pass { Name "WhiteBalance" HLSLPROGRAM #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target { return SAMPLE_SOURCE(input.texcoord); } ENDHLSL }

        // Pass 2: Color Grading (Placeholder)
        Pass { Name "ColorGrading" HLSLPROGRAM #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target { return SAMPLE_SOURCE(input.texcoord); } ENDHLSL }

        // Pass 3: LUT (Placeholder)
        Pass { Name "LUT" HLSLPROGRAM #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target { return SAMPLE_SOURCE(input.texcoord); } ENDHLSL }
    }
}
