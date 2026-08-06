// 그레이박스 스노 라이트 (S-053 ⑤) — URP Lit 대체 경량 셰이더.
// 전역 _DL_SnowMix(WorldWeatherManager 구동)에 따라 월드 윗면(normal.y)에 눈이 쌓인다 —
// 경사·상자·지붕 등 모든 상면 커버("평면 quad 눈"의 구멍 소멸). 메인 라이트 그림자 + 추가 라이트(가로등) 지원.
Shader "DontLate/GreyboxSnow"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // S-192 — **Forward+(클러스터) 배리언트.** 이게 없으면 아래 LIGHT_LOOP가 구식
            // 인덱스 루프로 컴파일되고, Forward+에선 그 인덱스가 유효하지 않아 추가광원이
            // 통째로 사라진다. 지면만 가로등을 못 받고 나무(URP Lit)는 받던 이유가 이것.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            float _DL_SnowMix; // 전역 — 0(맨바닥)..1(만설)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                // 윗면일수록 눈 — 0.5~0.85 구간 램프 (수직 벽엔 안 붙고 완만한 경사엔 얇게).
                half snowTop = saturate((normalWS.y - 0.5) / 0.35);
                half3 albedo = lerp(_BaseColor.rgb, half3(0.93, 0.95, 0.99), snowTop * saturate(_DL_SnowMix));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lighting = mainLight.color
                    * (saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation);

                // 추가 라이트 (가로등 앰버·간판 시안 — 밤 무드의 주역).
                // LIGHT_LOOP 매크로를 쓰는 이유: Forward+는 화면을 클러스터로 쪼개 픽셀마다
                // 다른 광원 목록을 넘긴다. 그 순회는 매크로 안에 숨어 있고, 매크로가
                // `inputData`라는 **이름 그대로** 화면 UV·월드 좌표를 참조한다(URP 규약).
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    lighting += light.color
                        * (saturate(dot(normalWS, light.direction)) * light.distanceAttenuation * light.shadowAttenuation);
                LIGHT_LOOP_END
                #endif

                half3 ambient = SampleSH(normalWS);
                half3 color = albedo * (lighting + ambient);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Universal Render Pipeline/Lit"
}
