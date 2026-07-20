Shader "DontLate/SignGlow"
{
    // 간판 위에 씌우는 additive 발광판. 판때기 티가 안 나게 UV 기반으로 가장자리를
    // 부드럽게 죽인다(_Softness). 색은 HDR — 블룸이 물려 네온처럼 번진다.
    Properties
    {
        [HDR] _Color ("Color (HDR)", Color) = (0.208, 0.878, 0.784, 1)
        _Intensity ("Intensity", Float) = 1
        _Softness ("Edge Softness", Range(0.001, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SignGlow"
            Blend SrcAlpha One   // additive — 기여도는 src alpha(가장자리 폴오프)로 스케일
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
                float _Intensity;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 중심(0) → 가장자리(1) 거리를 양 축에서 취해 사각 폴오프.
                float2 d = abs(IN.uv - 0.5) * 2.0;
                float edge = max(d.x, d.y);
                // 안쪽은 꽉 차고 바깥 _Softness 폭만 부드럽게 사라진다.
                float a = 1.0 - smoothstep(1.0 - _Softness, 1.0, edge);
                return half4(_Color.rgb * _Intensity, saturate(a));
            }
            ENDHLSL
        }
    }
}
