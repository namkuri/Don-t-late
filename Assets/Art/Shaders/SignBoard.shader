// 전광판 셰이더 (S-043) — 프레넬 림 + HDR 이미시브 + 펄스.
// 전역 _DL_SignNight(0=낮 소등 · 1=밤 점등)를 WorldDayNightManager가 시각으로 구동 — 부드러운 점등.
Shader "DontLate/SignBoard"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.10, 0.10, 0.13, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0.2, 3.2, 2.8, 1)
        _EmissionStrength ("Emission Strength", Range(0, 6)) = 1.6
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.6
        _FresnelStrength ("Fresnel Strength", Range(0, 3)) = 1.2
        _PulseSpeed ("Pulse Speed", Range(0, 12)) = 2.4
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.22
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionStrength;
                half _FresnelPower;
                half _FresnelStrength;
                half _PulseSpeed;
                half _PulseAmount;
            CBUFFER_END

            float _DL_SignNight; // 전역 — 0 낮 / 1 밤 (매니저 구동)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 view = normalize(input.viewDirWS);

                // 프레넬 림 — 가장자리로 갈수록 발광 가산.
                half fresnel = pow(1.0 - saturate(dot(normal, view)), _FresnelPower);

                // 펄스 — 전광판 깜박임(사인 왕복).
                half pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);

                half night = saturate(_DL_SignNight);
                half3 emission = _EmissionColor.rgb * _EmissionStrength * pulse * night
                               * (0.55 + fresnel * _FresnelStrength);

                // 낮에는 베이스(꺼진 판), 밤에는 이미시브가 블룸 임계(HDR)를 뚫는다.
                half3 color = _BaseColor.rgb * (1.0 - 0.35 * night) + emission;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
