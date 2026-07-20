Shader "DontLate/Moon"
{
    // 밤 전용 달 쿼드. 32×32 픽셀아트 텍스처(moon_pixel.png)를 샘플한다 — 각진 실루엣·얼룩·달 토끼.
    // 텍스처 알파가 원판 마스크(본체 밖 alpha 0), rgb는 _Color 틴트·_Intensity(HDR)로 블룸 임계 위로
    // 밀어 달무리(halo)를 얻는다. _GlobalAlpha로 밤 페이드(StarField.cs가 MPB로 구동 — 프로퍼티명 일치).
    // 텍스처 임포터가 Point 필터를 걸어 도트가 계단형으로 유지되고, 그 뒤 블룸이 번짐을 얹는다.
    Properties
    {
        [NoScaleOffset] _MainTex ("Moon Texture", 2D) = "white" {}
        [HDR] _Color ("Tint (HDR)", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity (HDR)", Float) = 1.4
        _GlobalAlpha ("Global Alpha (fade)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Moon"
            Blend SrcAlpha OneMinusSrcAlpha   // 하늘 위 알파 블렌드 — 원판만 얹고 바깥은 투명
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _GlobalAlpha;
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
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 rgb = tex.rgb * _Color.rgb * _Intensity;   // HDR — 블룸 임계 위로
                half alpha = tex.a * _GlobalAlpha;               // 텍스처 원판 마스크 × 밤 페이드
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
