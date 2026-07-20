Shader "DontLate/LightCone"
{
    // 가로등 광추(god-ray) 페이크. 절두원뿔(frustum) 메시 측면에 그리는 애디티브 글로우.
    // 위(uv.y=1, 광원쪽) 진함 → 아래(uv.y=0) 옅음. 카메라를 정면으로 보는 면 진하고
    // 실루엣(그레이징) 면은 옅게(프레넬 유사) — 딱딱한 원뿔 윤곽 대신 안개 속 광추로 읽힌다.
    // 진짜 볼류메트릭(레이마칭) 금지 대체물. Cull Off·ZWrite Off·애디티브 — WebGL 안전.
    Properties
    {
        [HDR] _Color ("Color (amber HDR)", Color) = (1, 0.624, 0.271, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.5
        _TopBias ("Top-Down Gradient Power", Range(0.1, 4)) = 1.0
        _EdgePower ("Edge Softness Power", Range(0.1, 4)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "LightCone"
            Blend SrcAlpha One   // additive glow
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Alpha;
                float _TopBias;
                float _EdgePower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewWS = _WorldSpaceCameraPos - pos.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 위→아래 세로 그라디언트: 광원쪽(uv.y=1) 진함
                float grad = pow(saturate(IN.uv.y), _TopBias);

                // 프레넬 유사 소프트 엣지: 정면(dot~1) 진함, 실루엣(dot~0) 옅음. 양면이라 abs.
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewWS);
                float edge = pow(saturate(abs(dot(N, V))), _EdgePower);

                float a = grad * edge * _Alpha;
                return half4(_Color.rgb, saturate(a));
            }
            ENDHLSL
        }
    }
}
