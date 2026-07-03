Shader "Hidden/Workbench/Retro"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _TextureParam ("Blue Noise/Atlas", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "WorkbenchCommon.hlsl"
        ENDHLSL

        // Pass 0: Halftone
        Pass
        {
            Name "Halftone"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float4 c = SAMPLE_SOURCE( input.texcoord);
                float luma = Luminance(c.rgb);

                float scale = max(2.0, _FloatParam1);
                float angleRad = radians(_FloatParam2);
                float intensity = _FloatParam3;

                float2 dims = SOURCE_TEXEL_SIZE.zw;

                // Rotate UV around center for an angled dot grid
                float cosA = cos(angleRad);
                float sinA = sin(angleRad);
                float2 centered = input.texcoord - 0.5;
                float2 rotated = float2(
                    centered.x * cosA - centered.y * sinA,
                    centered.x * sinA + centered.y * cosA
                ) + 0.5;

                // Scale into dot grid cells
                float2 scaledUV = rotated * dims / scale;
                float2 cell = frac(scaledUV) - 0.5;
                float dist = length(cell);

                // Dot radius from inverse luma (darker = bigger dot)
                float dotRadius = (1.0 - luma) * 0.5;
                float dotMask = 1.0 - smoothstep(dotRadius - 0.05, dotRadius + 0.05, dist);

                float3 halftone = float3(dotMask, dotMask, dotMask);
                float3 result = lerp(c.rgb, halftone, intensity);
                return float4(result, 1.0);
            }
            ENDHLSL
        }

        // Pass 1: Dithering (Bayer)
        Pass
        {
            Name "Dithering"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float4 c = SAMPLE_SOURCE( input.texcoord);
                
                float levels = max(2.0, _FloatParam1); // Color levels
                float2 screenPos = input.texcoord * SOURCE_TEXEL_SIZE.zw;
                
                // 4x4 Bayer Matrix
                int x = (int)screenPos.x % 4;
                int y = (int)screenPos.y % 4;
                
                // Flat array for 4x4 matrix
                // 0  8  2  10
                // 12 4  14 6
                // 3  11 1  9
                // 15 7  13 5
                int idx = y * 4 + x;
                float bayer[16] = {
                    0.0/16.0, 8.0/16.0, 2.0/16.0, 10.0/16.0,
                    12.0/16.0, 4.0/16.0, 14.0/16.0, 6.0/16.0,
                    3.0/16.0, 11.0/16.0, 1.0/16.0, 9.0/16.0,
                    15.0/16.0, 7.0/16.0, 13.0/16.0, 5.0/16.0
                };
                
                float threshold = bayer[idx] - 0.5;
                
                // Spread color
                c.rgb += threshold / (levels - 1.0);
                c.rgb = floor(c.rgb * (levels - 1.0) + 0.5) / (levels - 1.0);
                
                return c;
            }
            ENDHLSL
        }

        // Pass 2: BlueNoise
        Pass 
        { 
            Name "BlueNoise" 
            HLSLPROGRAM 
            #pragma vertex Vert 
            #pragma fragment Frag 
            float4 Frag(Varyings input) : SV_Target { 
                float4 c = SAMPLE_SOURCE( input.texcoord);
                float levels = max(2.0, _FloatParam1);
                
                // Sample noise texture (tiled)
                float2 noiseUV = input.texcoord * SOURCE_TEXEL_SIZE.zw / 64.0; // Assume 64x64 noise
                float noise = SAMPLE_TEXTURE2D(_TextureParam, sampler_TextureParam, noiseUV).r;
                
                float threshold = (noise - 0.5);
                c.rgb += threshold / (levels - 1.0);
                c.rgb = floor(c.rgb * (levels - 1.0) + 0.5) / (levels - 1.0);
                
                return c;
            } 
            ENDHLSL 
        }

        // Pass 3: Pixelate
        Pass
        {
            Name "Pixelate"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float blockSize = max(1.0, _FloatParam1);
                float2 texel = SOURCE_TEXEL_SIZE.xy;
                float2 blockUV = floor(input.texcoord / (texel * blockSize)) * (texel * blockSize);
                // Center sample
                blockUV += (texel * blockSize) * 0.5; 
                return SAMPLE_SOURCE( blockUV);
            }
            ENDHLSL
        }

        // Pass 4: AsciiArt
        Pass 
        { 
            Name "AsciiArt" 
            HLSLPROGRAM 
            #pragma vertex Vert 
            #pragma fragment Frag 
            
            // 8x8 bitmap font embedded as a simple hack since we don't have the texture
            // Just returning blocky luma for now to simulate the "grid" feel without the asset
            float4 Frag(Varyings input) : SV_Target 
            { 
                float2 cellSize = float2(8.0, 16.0); // Char size
                float2 texel = SOURCE_TEXEL_SIZE.xy;
                
                float2 cellUV = floor(input.texcoord / (texel * cellSize)) * (texel * cellSize);
                float4 c = SAMPLE_SOURCE( cellUV);
                float luma = Luminance(c.rgb);
                
                // Quantize to 4 levels
                float q = floor(luma * 4.0) / 4.0;
                return float4(q, q, q, 1); 
            } 
            ENDHLSL 
        }
    }
}