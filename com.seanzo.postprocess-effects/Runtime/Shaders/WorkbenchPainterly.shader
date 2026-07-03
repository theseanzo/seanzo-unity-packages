Shader "Hidden/Workbench/Painterly"
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

        // Pass 0: Kuwahara
        Pass
        {
            Name "Kuwahara"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texel = SOURCE_TEXEL_SIZE.xy;
                int radius = max(1, (int)_FloatParam1);

                // Offsets for the 4 quadrants relative to center
                float2 ranges[4] = {
                    float2(-radius, -radius),
                    float2(0, -radius),
                    float2(-radius, 0),
                    float2(0, 0)
                };

                float minVar = 9999.0;
                float3 finalColor = 0;

                for(int i=0; i<4; i++)
                {
                    float3 m = 0;
                    float3 c2 = 0;
                    float n = 0;

                    [loop]
                    for(int y=0; y<=radius; y++)
                    {
                        for(int x=0; x<=radius; x++)
                        {
                            float2 sampleOffset = ranges[i] + float2(x, y);
                            float2 pos = uv + sampleOffset * texel;
                            float3 val = SAMPLE_SOURCE_LOD( pos, 0).rgb;
                            m += val;
                            c2 += val * val;
                            n += 1.0;
                        }
                    }

                    m /= n;
                    float v = dot((c2 / n) - (m * m), float3(0.299, 0.587, 0.114));

                    if (v < minVar)
                    {
                        minVar = v;
                        finalColor = m;
                    }
                }

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Pass 1: Oil Paint (Anisotropic Kuwahara, ported from rust oil_paint.wgsl)
        // _FloatParam1 = stroke_size (kernel radius, px)
        // _FloatParam2 = smoothness (ellipse stretch)
        // _FloatParam3 = direction_weight (gradient alignment 0..1)
        // _FloatParam4 = sharpness (weight falloff)
        Pass
        {
            Name "OilPaint"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // 3x3 Sobel luminance gradient
            float2 OilComputeGradient(float2 uv, float2 texel)
            {
                float3 tl = SAMPLE_SOURCE_LOD(uv + float2(-1.0, -1.0) * texel, 0).rgb;
                float3 tc = SAMPLE_SOURCE_LOD(uv + float2( 0.0, -1.0) * texel, 0).rgb;
                float3 tr = SAMPLE_SOURCE_LOD(uv + float2( 1.0, -1.0) * texel, 0).rgb;
                float3 ml = SAMPLE_SOURCE_LOD(uv + float2(-1.0,  0.0) * texel, 0).rgb;
                float3 mr = SAMPLE_SOURCE_LOD(uv + float2( 1.0,  0.0) * texel, 0).rgb;
                float3 bl = SAMPLE_SOURCE_LOD(uv + float2(-1.0,  1.0) * texel, 0).rgb;
                float3 bc = SAMPLE_SOURCE_LOD(uv + float2( 0.0,  1.0) * texel, 0).rgb;
                float3 br = SAMPLE_SOURCE_LOD(uv + float2( 1.0,  1.0) * texel, 0).rgb;

                float3 lum = float3(0.299, 0.587, 0.114);
                float l_tl = dot(tl, lum);
                float l_tc = dot(tc, lum);
                float l_tr = dot(tr, lum);
                float l_ml = dot(ml, lum);
                float l_mr = dot(mr, lum);
                float l_bl = dot(bl, lum);
                float l_bc = dot(bc, lum);
                float l_br = dot(br, lum);

                float gx = -l_tl - 2.0*l_ml - l_bl + l_tr + 2.0*l_mr + l_br;
                float gy = -l_tl - 2.0*l_tc - l_tr + l_bl + 2.0*l_bc + l_br;

                return float2(gx, gy);
            }

            // Weighted elliptical sector -> float4(mean.rgb, weighted luminance variance)
            float4 OilSampleSector(float2 uv, float2 texel, float radius, float angle,
                                   float sectorOffset, float smoothness, float sharpness)
            {
                float3 sum = 0.0;
                float3 sumSq = 0.0;
                float weightSum = 0.0;

                float cos_a = cos(angle + sectorOffset);
                float sin_a = sin(angle + sectorOffset);

                int r = (int)radius;
                float maxDistSq = radius * radius;
                float stretch = 1.0 + smoothness;

                [loop]
                for (int y = -r; y <= r; y++)
                {
                    for (int x = -r; x <= r; x++)
                    {
                        float fx = (float)x;
                        float fy = (float)y;

                        // Rotate into aligned space
                        float rx = fx * cos_a + fy * sin_a;
                        float ry = -fx * sin_a + fy * cos_a;

                        // Only the positive quadrant belongs to this sector
                        if (rx < 0.0 || ry < 0.0)
                            continue;

                        float rxs = rx / stretch;
                        float distSq = rxs * rxs + ry * ry;
                        if (distSq > maxDistSq)
                            continue;

                        float weight = exp(-sharpness * distSq / maxDistSq);

                        float2 offset = float2(fx, fy) * texel;
                        float3 color = SAMPLE_SOURCE_LOD(uv + offset, 0).rgb;

                        sum += color * weight;
                        sumSq += color * color * weight;
                        weightSum += weight;
                    }
                }

                if (weightSum < 0.001)
                {
                    float3 center = SAMPLE_SOURCE_LOD(uv, 0).rgb;
                    return float4(center, 0.0);
                }

                float3 mean = sum / weightSum;
                float variance = dot(sumSq / weightSum - mean * mean, float3(0.299, 0.587, 0.114));
                return float4(mean, max(variance, 0.0));
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texel = SOURCE_TEXEL_SIZE.xy;

                float radius     = _FloatParam1; // stroke_size
                float smoothness = _FloatParam2;
                float dirWeight  = _FloatParam3; // direction_weight
                float sharpness  = _FloatParam4;

                // Local gradient -> stroke angle (perpendicular to gradient)
                float2 gradient = OilComputeGradient(uv, texel);
                float gradMag = length(gradient);

                float strokeAngle = 0.0;
                if (gradMag > 0.001)
                    strokeAngle = atan2(gradient.y, gradient.x) + 3.14159265 * 0.5;

                float finalAngle = strokeAngle * dirWeight;

                float4 s0 = OilSampleSector(uv, texel, radius, finalAngle, 0.0,      smoothness, sharpness);
                float4 s1 = OilSampleSector(uv, texel, radius, finalAngle, PI * 0.5, smoothness, sharpness);
                float4 s2 = OilSampleSector(uv, texel, radius, finalAngle, PI,       smoothness, sharpness);
                float4 s3 = OilSampleSector(uv, texel, radius, finalAngle, PI * 1.5, smoothness, sharpness);

                // Pick the flattest (min variance) sector
                float minVar = s0.w;
                float3 result = s0.rgb;

                if (s1.w < minVar) { minVar = s1.w; result = s1.rgb; }
                if (s2.w < minVar) { minVar = s2.w; result = s2.rgb; }
                if (s3.w < minVar) { result = s3.rgb; }

                return float4(result, 1.0);
            }
            ENDHLSL
        }

        // Pass 2: Watercolor
        Pass
        {
            Name "Watercolor"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float WCHash(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float WCNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(WCHash(i + float2(0.0, 0.0)), WCHash(i + float2(1.0, 0.0)), u.x),
                    lerp(WCHash(i + float2(0.0, 1.0)), WCHash(i + float2(1.0, 1.0)), u.x),
                    u.y);
            }

            float WCFbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * WCNoise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }

            float WCDetectEdge(float2 uv, float2 texel)
            {
                float3 lum = float3(0.299, 0.587, 0.114);
                float tl = dot(SAMPLE_SOURCE_LOD(uv + float2(-1.0, -1.0) * texel, 0).rgb, lum);
                float tc = dot(SAMPLE_SOURCE_LOD(uv + float2( 0.0, -1.0) * texel, 0).rgb, lum);
                float tr = dot(SAMPLE_SOURCE_LOD(uv + float2( 1.0, -1.0) * texel, 0).rgb, lum);
                float ml = dot(SAMPLE_SOURCE_LOD(uv + float2(-1.0,  0.0) * texel, 0).rgb, lum);
                float mr = dot(SAMPLE_SOURCE_LOD(uv + float2( 1.0,  0.0) * texel, 0).rgb, lum);
                float bl = dot(SAMPLE_SOURCE_LOD(uv + float2(-1.0,  1.0) * texel, 0).rgb, lum);
                float bc = dot(SAMPLE_SOURCE_LOD(uv + float2( 0.0,  1.0) * texel, 0).rgb, lum);
                float br = dot(SAMPLE_SOURCE_LOD(uv + float2( 1.0,  1.0) * texel, 0).rgb, lum);
                float gx = -tl - 2.0*ml - bl + tr + 2.0*mr + br;
                float gy = -tl - 2.0*tc - tr + bl + 2.0*bc + br;
                return length(float2(gx, gy));
            }

            float3 WCBleedColor(float2 uv, float2 texel, float radius)
            {
                float3 sum = 0.0;
                float weightSum = 0.0;
                int r = (int)radius;

                [loop]
                for (int y = -r; y <= r; y++)
                {
                    for (int x = -r; x <= r; x++)
                    {
                        float2 offset = float2(x, y) * texel;
                        float2 sampleUV = uv + offset;

                        float dist = length(float2(x, y));
                        if (dist > radius)
                            continue;

                        float distWeight = 1.0 - (dist / radius);
                        float randomOffset = (WCHash(sampleUV * 100.0) - 0.5) * 0.3;
                        float weight = distWeight * (1.0 + randomOffset);

                        float3 color = SAMPLE_SOURCE_LOD(sampleUV, 0).rgb;
                        sum += color * weight;
                        weightSum += weight;
                    }
                }

                if (weightSum > 0.001)
                    return sum / weightSum;
                return SAMPLE_SOURCE_LOD(uv, 0).rgb;
            }

            float3 WCRgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.b, c.g, K.w, K.z), float4(c.g, c.b, K.x, K.y), step(c.b, c.g));
                float4 q = lerp(float4(p.x, p.y, p.w, c.r), float4(c.r, p.y, p.z, p.x), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 WCHsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 dims = SOURCE_TEXEL_SIZE.zw;
                float2 texel = SOURCE_TEXEL_SIZE.xy;

                float bleedRadius     = _FloatParam1;
                float edgeThreshold   = _FloatParam2;
                float saturationBoost = _FloatParam3;
                float paperTexture    = _FloatParam4;
                float wetEdge         = _VectorParam1.x;
                float granulation     = _VectorParam1.y;

                float edge = WCDetectEdge(uv, texel);
                float isEdge = step(edgeThreshold, edge);

                float bleedFactor = 1.0 - isEdge * 0.5;
                float3 color = WCBleedColor(uv, texel, bleedRadius * bleedFactor);

                float3 hsv = WCRgbToHsv(color);
                hsv.y = min(hsv.y * saturationBoost, 1.0);
                color = WCHsvToRgb(hsv);

                float wetEdgeDarken = 1.0 - (isEdge * wetEdge * 0.3);
                color *= wetEdgeDarken;

                float paperScale = 50.0;
                float paperNoise = WCFbm(uv * dims / paperScale);
                float paperEffect = lerp(1.0, 0.85 + paperNoise * 0.3, paperTexture);
                color *= paperEffect;

                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                float granulationNoise = WCNoise(uv * dims / 3.0);
                float granulationFactor = (1.0 - luminance) * granulation;
                color = lerp(color, color * (0.8 + granulationNoise * 0.4), granulationFactor);

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
