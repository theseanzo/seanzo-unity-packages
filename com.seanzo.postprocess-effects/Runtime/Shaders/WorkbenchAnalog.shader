Shader "Hidden/Workbench/Analog"
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

        // Pass 0: Scanlines
        Pass
        {
            Name "Scanlines"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float4 c = SAMPLE_SOURCE(input.texcoord);
                float intensity = _FloatParam1;
                float count = _FloatParam2;
                float thickness = _FloatParam3;
                float speed = _FloatParam4;

                float linePos = input.texcoord.y * count + _Time.y * speed;
                float lineFract = frac(linePos);
                float halfThick = thickness * 0.5;
                float scanline = smoothstep(0.5 - halfThick, 0.5, lineFract) *
                                 smoothstep(0.5 + halfThick, 0.5, lineFract);
                c.rgb *= 1.0 - intensity * (1.0 - scanline);
                return c;
            }
            ENDHLSL
        }

        // Pass 1: Tracking Distortion (band-based VHS, ported from rust tracking.wgsl)
        Pass
        {
            Name "Tracking"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float TrackingHash(float p) {
                float p2 = frac(p * 0.1031);
                p2 *= p2 + 33.33;
                p2 *= p2 + p2;
                return frac(p2);
            }

            float TrackingNoise(float x) {
                float i = floor(x);
                float f = frac(x);
                return lerp(TrackingHash(i), TrackingHash(i + 1.0), f * f * (3.0 - 2.0 * f));
            }

            float4 Frag(Varyings input) : SV_Target {
                float intensity = _FloatParam1;
                float speed = _FloatParam2;
                float bandHeight = _FloatParam3;
                float2 uv = input.texcoord;

                float bandPos = floor(uv.y / bandHeight);
                float timeOffset = _Time.y * speed;
                float wobbleSeed = bandPos + timeOffset;

                float lowFreq = TrackingNoise(wobbleSeed * 0.5) - 0.5;
                float highFreq = (TrackingNoise(wobbleSeed * 3.0 + 100.0) - 0.5) * 0.3;

                float offset = (lowFreq + highFreq) * intensity;
                uv.x = clamp(uv.x + offset, 0.0, 1.0);

                return SAMPLE_SOURCE(uv);
            }
            ENDHLSL
        }

        // Pass 2: Color Bleed (YUV chroma smear, ported from rust color_bleed.wgsl)
        Pass
        {
            Name "ColorBleed"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target {
                float amount = _FloatParam1;

                float3 centerRGB = SAMPLE_SOURCE(input.texcoord).rgb;
                float sharpLuma = dot(centerRGB, float3(0.299, 0.587, 0.114));

                float blurredU = 0.0;
                float blurredV = 0.0;

                const int numSamples = 5;
                float offsetStep = amount / float(numSamples);

                [unroll]
                for (int i = 0; i < numSamples; i++) {
                    float offset = float(i) * offsetStep;
                    float3 sampleRGB = SAMPLE_SOURCE(input.texcoord + float2(offset, 0.0)).rgb;
                    float sampleLuma = dot(sampleRGB, float3(0.299, 0.587, 0.114));
                    float sampleU = (sampleRGB.b - sampleLuma) * 0.565;
                    float sampleV = (sampleRGB.r - sampleLuma) * 0.713;

                    float weight = 1.0 - (float(i) / float(numSamples)) * 0.5;
                    blurredU += sampleU * weight;
                    blurredV += sampleV * weight;
                }

                float totalWeight = 0.5 * float(numSamples) + 0.5 * float(numSamples - 1);
                blurredU /= totalWeight;
                blurredV /= totalWeight;

                float3 resultRGB;
                resultRGB.r = sharpLuma + 1.403 * blurredV;
                resultRGB.g = sharpLuma - 0.344 * blurredU - 0.714 * blurredV;
                resultRGB.b = sharpLuma + 1.770 * blurredU;

                return float4(clamp(resultRGB, 0.0, 1.0), 1.0);
            }
            ENDHLSL
        }

        // Pass 3: Static Noise (block-based corruption, ported from rust static_noise.wgsl)
        Pass
        {
            Name "StaticNoise"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float StaticHash(float2 p) {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float3 StaticHash3(float2 p) {
                float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
                float3 p4 = p3 + dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yzz) * p4.zyx);
            }

            float4 Frag(Varyings input) : SV_Target {
                float4 original = SAMPLE_SOURCE(input.texcoord);
                float intensity = _FloatParam1;
                float blockSize = _FloatParam2;

                float2 dims = SOURCE_TEXEL_SIZE.zw;
                float2 pixelPos = input.texcoord * dims;
                float2 blockPos = floor(pixelPos / blockSize);

                float timeSeed = floor(_Time.y * 30.0);
                float2 seed = blockPos + float2(timeSeed * 127.1, timeSeed * 311.7);

                float corruptionChance = StaticHash(seed);

                if (corruptionChance < intensity) {
                    float3 noiseColor;
                    if (_IntParam1 == 1) {
                        noiseColor = StaticHash3(seed + float2(1.0, 2.0));
                    } else {
                        float gray = StaticHash(seed + float2(3.0, 4.0));
                        noiseColor = float3(gray, gray, gray);
                    }
                    return float4(noiseColor, 1.0);
                }

                return original;
            }
            ENDHLSL
        }

        // Pass 4: Flicker
        Pass { Name "Flicker" HLSLPROGRAM #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target { return SAMPLE_SOURCE(input.texcoord); } ENDHLSL }

        // Pass 5: Interlacing
        Pass { Name "Interlacing" HLSLPROGRAM #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target { return SAMPLE_SOURCE(input.texcoord); } ENDHLSL }

        // Pass 6: Film Grain
        Pass { Name "FilmGrain" HLSLPROGRAM #pragma vertex Vert
            #pragma fragment Frag
            float4 Frag(Varyings input) : SV_Target { return SAMPLE_SOURCE(input.texcoord); } ENDHLSL }
    }
}
