// 아지랑이 셰이더 (S-044 ③) — 정점 X 일렁임 + 상승 스크롤 밸류노이즈 알파.
// 지면 위 가로 밴드 쿼드에 붙인다 — 폭염 날 공기가 흔들리는 감.
Shader "DontLate/HeatHaze"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 0.98, 0.92, 0.10)
        _WobbleAmp ("Wobble Amplitude", Range(0, 0.5)) = 0.14
        _WobbleFreq ("Wobble Frequency", Range(0, 12)) = 5.5
        _ScrollSpeed ("Upward Scroll", Range(0, 3)) = 0.9
        _NoiseScale ("Noise Scale", Range(1, 40)) = 14
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
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
                // 일렁임 — 높이·시간 위상으로 X를 흔든다 (위로 갈수록 강하게).
                positionWS.x += sin(positionWS.y * 3.1 + _Time.y * _WobbleFreq + positionWS.x * 0.7)
                              * _WobbleAmp * input.uv.y;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 상승 스크롤 노이즈 2옥타브 — 열기 기둥이 위로 흐른다.
                float2 uv = input.uv * float2(_NoiseScale, _NoiseScale * 0.5);
                uv.y -= _Time.y * _ScrollSpeed;
                float noise = valueNoise(uv) * 0.65 + valueNoise(uv * 2.3 + 7.7) * 0.35;

                // 아래·위 가장자리 페이드 + 좌우 가장자리 페이드.
                half fadeY = saturate(input.uv.y * 3.0) * saturate((1.0 - input.uv.y) * 1.6);
                half fadeX = saturate(input.uv.x * 8.0) * saturate((1.0 - input.uv.x) * 8.0);

                half alpha = _Color.a * noise * fadeY * fadeX;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
