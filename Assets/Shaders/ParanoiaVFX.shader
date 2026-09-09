Shader "Hidden/ParanoiaFullscreen"
{
    Properties
    {
        [Header(Global)]
        _Tension ("Tension", Range(0,1)) = 0.30

        [Header(Chromatic Aberration)]
        _AberrAmount ("Amount", Range(0,1)) = 0.55
        _AberrEdge1 ("Ramp starts", Range(0,1)) = 0.00
        _AberrEdge2 ("Fully in", Range(0,1)) = 0.70
        _AberrFloor ("Output floor", Range(0,1)) = 0.20
        _AberrCeiling ("Output ceiling", Range(0,1)) = 1.00
        _AberrBaseOffset ("Base offset", Range(0,0.05)) = 0.004
        _AberrBreathOffset ("Breath offset", Range(0,0.05)) = 0.011
        _AberrRadialBase ("Radial base", Range(0,2)) = 0.5
        _AberrRadialScale ("Radial scale", Range(0,10)) = 3.0

        [Header(Lens Warp)]
        _WarpAmount ("Amount", Range(0,1)) = 0.35
        _WarpEdge1 ("Ramp starts", Range(0,1)) = 0.10
        _WarpEdge2 ("Fully in", Range(0,1)) = 0.80
        _WarpFloor ("Output floor", Range(0,1)) = 0.15
        _WarpCeiling ("Output ceiling", Range(0,1)) = 1.00
        _WarpBaseStrength ("Base strength", Range(0,1)) = 0.15
        _WarpBreathStrength ("Breath strength", Range(0,1)) = 0.12
        _WarpRadialScale ("Radial scale", Range(0,10)) = 4.0

        [Header(Grain)]
        _GrainAmount ("Amount", Range(0,1)) = 0.45
        _GrainEdge1 ("Ramp starts", Range(0,1)) = 0.00
        _GrainEdge2 ("Fully in", Range(0,1)) = 0.60
        _GrainFloor ("Output floor", Range(0,1)) = 0.15
        _GrainCeiling ("Output ceiling", Range(0,1)) = 1.00
        _GrainStrength ("Strength", Range(0,1)) = 0.15

        [Header(Desaturation)]
        _DesatAmount ("Amount", Range(0,1)) = 0.55
        _DesatEdge1 ("Ramp starts", Range(0,1)) = 0.00
        _DesatEdge2 ("Fully in", Range(0,1)) = 1.00
        _DesatFloor ("Output floor", Range(0,1)) = 0.20
        _DesatCeiling ("Output ceiling", Range(0,1)) = 1.00
        _DesatLumaR ("Luma weight R", Range(0,1)) = 0.299
        _DesatLumaG ("Luma weight G", Range(0,1)) = 0.587
        _DesatLumaB ("Luma weight B", Range(0,1)) = 0.114

        [Header(Color Cast)]
        _CastAmount ("Amount", Range(0,1)) = 0.40
        _CastEdge1 ("Ramp starts", Range(0,1)) = 0.00
        _CastEdge2 ("Fully in", Range(0,1)) = 1.00
        _CastFloor ("Output floor", Range(0,1)) = 0.00
        _CastCeiling ("Output ceiling", Range(0,1)) = 1.00
        _CastColor ("Cast tint", Color) = (1.06, 0.95, 0.78, 1)

        [Header(Vignette)]
        _VignAmount ("Amount", Range(0,1)) = 0.70
        _VignEdge1 ("Ramp starts", Range(0,1)) = 0.00
        _VignEdge2 ("Fully in", Range(0,1)) = 1.00
        _VignFloor ("Output floor", Range(0,1)) = 1.00
        _VignCeiling ("Output ceiling", Range(0,1)) = 1.00
        _VignRadius ("Radius", Range(0,2)) = 0.78
        _VignRadiusPerAmount ("Radius pulled in by amount", Range(0,1)) = 0.22
        _VignBreathScale ("Breath scale", Range(0,1)) = 0.13
        _VignTensionBias ("Tension bias", Range(0,2)) = 0.5
        _VignSoftness ("Softness", Range(0,2)) = 0.5

        [Header(Ghost Trails)]
        _GhostAmount ("Amount", Range(0,1)) = 0.30
        _GhostEdge1 ("Ramp starts", Range(0,1)) = 0.00
        _GhostEdge2 ("Fully in", Range(0,1)) = 1.00
        _GhostFloor ("Output floor", Range(0,1)) = 1.00
        _GhostCeiling ("Output ceiling", Range(0,1)) = 1.00
        _GhostTapCount ("Echo count", Range(0,3)) = 3
        _GhostFalloff ("Echo falloff", Range(0,1)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
        }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "Paranoia"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Tension;

            float _BreathPhase;

            float _AberrAmount, _AberrEdge1, _AberrEdge2, _AberrFloor, _AberrCeiling;
            float _AberrBaseOffset, _AberrBreathOffset, _AberrRadialBase, _AberrRadialScale;

            float _WarpAmount, _WarpEdge1, _WarpEdge2, _WarpFloor, _WarpCeiling;
            float _WarpBaseStrength, _WarpBreathStrength, _WarpRadialScale;

            float _GrainAmount, _GrainEdge1, _GrainEdge2, _GrainFloor, _GrainCeiling;
            float _GrainStrength;

            float _DesatAmount, _DesatEdge1, _DesatEdge2, _DesatFloor, _DesatCeiling;
            float _DesatLumaR, _DesatLumaG, _DesatLumaB;

            float _CastAmount, _CastEdge1, _CastEdge2, _CastFloor, _CastCeiling;
            float4 _CastColor;

            float _VignAmount, _VignEdge1, _VignEdge2, _VignFloor, _VignCeiling;
            float _VignRadius, _VignRadiusPerAmount, _VignBreathScale, _VignTensionBias, _VignSoftness;

            float _GhostAmount, _GhostEdge1, _GhostEdge2, _GhostFloor, _GhostCeiling;
            float _GhostTapCount, _GhostFalloff;

            TEXTURE2D(_GhostTex0);
            TEXTURE2D(_GhostTex1);
            TEXTURE2D(_GhostTex2);

            #define SRC(u) SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, (u))

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float EvalRamp(float amount, float edge1, float edge2,
                           float rampFloor, float rampCeiling, float tension)
            {
                float k = saturate((tension - edge1) / max(edge2 - edge1, 1e-5));
                k = k * k * (3.0 - 2.0 * k);
                return amount * lerp(rampFloor, rampCeiling, k);
            }

            float Breath(float phase)
            {
                return 0.5 + 0.5 * sin(phase);
            }

            float2 ApplyLensWarp(float2 uv, float strength, float breath,
                                 float baseStrength, float breathStrength, float radialScale)
            {
                float k = strength * (baseStrength + breathStrength * breath);
                float2 c = uv - 0.5;
                return 0.5 + c * (1.0 + k * dot(c, c) * radialScale);
            }

            half3 SampleChromatic(float2 uv, float strength, float breath, float baseOffset,
                                  float breathOffset, float radialBase, float radialScale)
            {
                float2 dir = uv - 0.5;
                float r2 = dot(dir, dir);
                float ab = strength * (baseOffset + breathOffset * breath)
                    * (radialBase + r2 * radialScale);

                half3 col;
                col.r = SRC(uv + dir * ab).r;
                col.g = SRC(uv).g;
                col.b = SRC(uv - dir * ab).b;
                return col;
            }

            half3 ApplyDesaturation(half3 col, float strength, float lumaR, float lumaG, float lumaB)
            {
                float lum = dot(col, float3(lumaR, lumaG, lumaB));
                return lerp(col, lum.xxx, strength);
            }

            half3 ApplyColorCast(half3 col, float strength, float3 tint)
            {
                return lerp(col, col * tint, strength);
            }

            half3 ApplyVignette(half3 col, float2 centered, float strength, float breath, float tension,
                                float radius, float radiusPerAmount, float breathScale,
                                float tensionBias, float softness)
            {
                float r = radius - strength * radiusPerAmount
                    + breath * strength * breathScale * (tensionBias + tension);
                float mask = smoothstep(r, r - softness, length(centered));
                return col * lerp(1.0, mask, strength);
            }

            half3 ApplyGrain(half3 col, float2 uv, float time, float strength, float grainStrength)
            {
                float n = hash21(uv * _ScreenParams.xy + time) - 0.5;
                return col + n * strength * grainStrength;
            }

            #define GHOST(tex, u) SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, (u)).rgb

            half3 ApplyGhostTrails(half3 col, float2 uv, float strength, float tapCount, float falloff)
            {
                float w = strength;

                col = lerp(col, GHOST(_GhostTex0, uv), w * step(1.0, tapCount));
                w *= falloff;
                col = lerp(col, GHOST(_GhostTex1, uv), w * step(2.0, tapCount));
                w *= falloff;
                col = lerp(col, GHOST(_GhostTex2, uv), w * step(3.0, tapCount));

                return col;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float t = _Time.y;
                float ten = _Tension;

                float eAberr = EvalRamp(_AberrAmount, _AberrEdge1, _AberrEdge2, _AberrFloor, _AberrCeiling, ten);
                float eWarp = EvalRamp(_WarpAmount, _WarpEdge1, _WarpEdge2, _WarpFloor, _WarpCeiling, ten);
                float eGrain = EvalRamp(_GrainAmount, _GrainEdge1, _GrainEdge2, _GrainFloor, _GrainCeiling, ten);
                float eDesat = EvalRamp(_DesatAmount, _DesatEdge1, _DesatEdge2, _DesatFloor, _DesatCeiling, ten);
                float eCast = EvalRamp(_CastAmount, _CastEdge1, _CastEdge2, _CastFloor, _CastCeiling, ten);
                float eVign = EvalRamp(_VignAmount, _VignEdge1, _VignEdge2, _VignFloor, _VignCeiling, ten);
                float eGhost = EvalRamp(_GhostAmount, _GhostEdge1, _GhostEdge2, _GhostFloor, _GhostCeiling, ten);

                float breath = Breath(_BreathPhase);
                float2 centered = uv - 0.5;

                uv = ApplyLensWarp(uv, eWarp, breath,
                                   _WarpBaseStrength, _WarpBreathStrength, _WarpRadialScale);

                half3 col = SampleChromatic(uv, eAberr, breath, _AberrBaseOffset,
                                            _AberrBreathOffset, _AberrRadialBase,
                                            _AberrRadialScale);
                col = ApplyGhostTrails(col, uv, eGhost, _GhostTapCount, _GhostFalloff);
                col = ApplyDesaturation(col, eDesat, _DesatLumaR, _DesatLumaG, _DesatLumaB);
                col = ApplyColorCast(col, eCast, _CastColor.rgb);
                col = ApplyVignette(col, centered, eVign, breath, ten, _VignRadius,
                                    _VignRadiusPerAmount, _VignBreathScale, _VignTensionBias,
                                    _VignSoftness);
                col = ApplyGrain(col, uv, t, eGrain, _GrainStrength);

                return half4(max(col, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}