// 아지랑이 셰이더 v2 (S-044 ③ → S-045 ④ 실굴절) — 카메라 Opaque Texture를 노이즈 오프셋으로
// 재샘플해 **뒤 객체가 실제로 굴절**된다. 가장자리로 갈수록 오프셋이 0으로 붙어 이음새 없음.
Shader "DontLate/HeatHaze"
{
    Properties
    {
        _RefractStrength ("Refract Strength", Range(0, 0.05)) = 0.004
        _WobbleAmp ("Vertex Wobble", Range(0, 0.5)) = 0.10
        _WobbleFreq ("Wobble Frequency", Range(0, 12)) = 5.5
        _ScrollSpeed ("Upward Scroll", Range(0, 3)) = 1.1
        _NoiseScale ("Noise Scale", Range(1, 40)) = 12
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One Zero   // 씬 컬러를 재샘플해 통째로 그린다 — 오프셋 0이면 원본과 동일(무봉합)
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _RefractStrength;
                half _WobbleAmp;
                half _WobbleFreq;
                half _ScrollSpeed;
                half _NoiseScale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS.x += sin(positionWS.y * 3.1 + _Time.y * _WobbleFreq + positionWS.x * 0.7)
                              * _WobbleAmp * input.uv.y;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 상승 스크롤 노이즈 2옥타브 — 열기 흐름.
                float2 noiseUV = input.uv * float2(_NoiseScale, _NoiseScale * 0.5);
                noiseUV.y -= _Time.y * _ScrollSpeed;
                float n1 = valueNoise(noiseUV);
                float n2 = valueNoise(noiseUV * 2.3 + 7.7);

                // 가장자리 페이드 — 굴절량이 0으로 수렴해 쿼드 경계가 안 보인다.
                half fadeY = saturate(input.uv.y * 3.0) * saturate((1.0 - input.uv.y) * 1.6);
                half fadeX = saturate(input.uv.x * 8.0) * saturate((1.0 - input.uv.x) * 8.0);
                half fade = fadeY * fadeX;

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionHCS);
                float2 offset = (float2(n1, n2) - 0.5) * 2.0 * _RefractStrength * fade;

                half3 scene = SampleSceneColor(screenUV + offset);
                return half4(scene, 1.0);
            }
            ENDHLSL
        }
    }
}
