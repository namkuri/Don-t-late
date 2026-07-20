Shader "DontLate/GroundMist"
{
    // 바닥 안개층. 수평으로 눕힌 쿼드에 절차적 밸류노이즈 알파를 그린다.
    // _ScrollSpeed로 X축을 천천히 흘려 안개가 기어가는 인상을 준다. 쿼드 가장자리는 페이드.
    // _GlobalAlpha로 밤 페이드 — StarField.cs 컴포넌트를 그대로 재사용(같은 프로퍼티명 계약).
    // 알파블렌드(애디티브 아님) — 어두운 바닥 위에 옅은 회백 안개가 깔린다. Cull Off(양면).
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.60, 0.62, 0.72, 1)
        _ScrollSpeed ("Scroll Speed (X)", Float) = 0.03
        _NoiseScale ("Noise Scale", Float) = 4
        _Coverage ("Coverage", Range(0, 1)) = 0.5
        _GlobalAlpha ("Global Alpha (fade)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "GroundMist"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _ScrollSpeed;
                float _NoiseScale;
                float _Coverage;
                float _GlobalAlpha;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 밸류노이즈 — 셀 해시 4점 bilinear + smoothstep 보간
            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv * _NoiseScale;
                uv.x += _Time.y * _ScrollSpeed;
                float n = vnoise(uv) * 0.6 + vnoise(uv * 2.1 + 7.3) * 0.4;   // 2 옥타브

                // 쿼드 가장자리 페이드(하드엣지 방지)
                float2 e = smoothstep(0.0, 0.25, IN.uv) * smoothstep(0.0, 0.25, 1.0 - IN.uv);
                float edge = e.x * e.y;

                float a = saturate(n - (1.0 - _Coverage)) * edge * _GlobalAlpha;
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
