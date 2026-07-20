Shader "DontLate/BeaconRise"
{
    Properties
    {
        _Color ("Color", Color) = (0.25, 0.88, 0.35, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _ScrollSpeed ("Scroll Speed", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "BeaconRise"
            Blend SrcAlpha One   // additive glow, contribution scaled by src alpha
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
                float _Alpha;
                float _ScrollSpeed;
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
                // 수직 알파 그라디언트: 아래(uv.y=0)=1 → 위(uv.y=1)=0
                float grad = saturate(1.0 - IN.uv.y);
                // 위 방향 스크롤 밴드 (Time 기반)
                float scroll = IN.uv.y - _Time.y * _ScrollSpeed;
                float bands = 0.6 + 0.4 * sin(scroll * 6.2831853 * 3.0);
                float a = grad * bands * _Alpha;
                return half4(_Color.rgb, saturate(a));
            }
            ENDHLSL
        }
    }
}
