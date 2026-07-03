Shader "Hidden/Workbench/Distortion"
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

        // Pass 0: Wave
        Pass
        {
            Name "Wave"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float2 uv = input.texcoord;
                float amp = _FloatParam1;
                float freq = _FloatParam2;
                float speed = _FloatParam3;

                uv.x += sin(uv.y * freq + _Time.y * speed) * amp;
                uv.y += sin(uv.x * freq + _Time.y * speed) * amp;
                uv = clamp(uv, 0.0, 1.0);

                float4 col = SAMPLE_SOURCE(uv);
                return float4(col.rgb, 1.0);
            }
            ENDHLSL
        }

        // Pass 1: Block Displacement (Glitch), ported from rust block_displacement.wgsl
        Pass
        {
            Name "Block"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // Deterministic per-block hash (matches rust)
            float BlockHash(float2 p) {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float4 Frag(Varyings input) : SV_Target {
                float intensity = _FloatParam1;   // Probability of block displaced
                float blockSize = max(1.0, _FloatParam2); // Block height in pixels (guard against 0)
                float shiftAmount = _FloatParam3; // Max horizontal shift in UV space

                float2 uv = input.texcoord;

                // Which horizontal block this pixel belongs to (block height in pixels)
                float blockIndex = floor(uv.y * SOURCE_TEXEL_SIZE.w / blockSize);

                // Per-block seed, animated by time (t=0 matches rust default)
                float timeTick = floor(_Time.y * 10.0);
                float rand = BlockHash(float2(blockIndex, timeTick));

                if (rand < intensity)
                {
                    float displacement = (BlockHash(float2(blockIndex + 100.0, timeTick)) - 0.5) * 2.0 * shiftAmount;
                    uv.x = frac(uv.x + displacement);
                }

                return SAMPLE_SOURCE(uv);
            }
            ENDHLSL
        }
    }
}
