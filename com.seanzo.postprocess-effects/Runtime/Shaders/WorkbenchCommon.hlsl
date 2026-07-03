#ifndef WORKBENCH_COMMON_INCLUDED
#define WORKBENCH_COMMON_INCLUDED

// Include URP Core and Blit - Blit.hlsl provides:
// - Vert function (fullscreen triangle vertex shader)
// - Varyings struct (with texcoord member)
// - _BlitTexture and sampler_LinearClamp
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

// Generic Parameters for effects
float _FloatParam1;
float _FloatParam2;
float _FloatParam3;
float _FloatParam4;
float4 _VectorParam1;
float4 _VectorParam2;
int _IntParam1;
TEXTURE2D(_TextureParam); SAMPLER(sampler_TextureParam);

TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_CameraNormalsTexture); SAMPLER(sampler_CameraNormalsTexture);

// Helper macros - use _BlitTexture (provided by Blit.hlsl) and texcoord (from Blit.hlsl's Varyings)
#define SAMPLE_SOURCE(uv) SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv)
#define SAMPLE_SOURCE_LOD(uv, lod) SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, uv, lod)
#define SOURCE_TEXEL_SIZE _BlitTexture_TexelSize

float GetDepth(float2 uv)
{
    return SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
}

float GetLinearDepth(float2 uv)
{
    float d = GetDepth(uv);
    return Linear01Depth(d, _ZBufferParams);
}

float3 GetNormal(float2 uv)
{
    return SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv).rgb;
}

float Random(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
}

#endif
