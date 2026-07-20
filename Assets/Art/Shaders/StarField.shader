Shader "DontLate/StarField"
{
    // 절차적 밤하늘 별밭 v2. 배경 쿼드 1장에 셰이더로 정사각 별을 흩뿌린다.
    // UV를 _Density 그리드로 나누되 u에 _Aspect(쿼드 폭/높이)를 곱해 셀을 정사각화한다 —
    //   빌더가 localScale에서 aspect를 계산해 주입한다(쿼드 200×70 → 2.857).
    // 별은 셀 해시로 존재/위치/크기/색/반짝임 위상을 정한다. 원형 폴오프 금지(step 하드엣지).
    // 색은 4계열 스펙트럼 팔레트(흰/청백/노랑/주황, 저채도)에서 해시로 고르고, 작은 별일수록 어둡다(원근감).
    // 반짝임 v2 = 알파 진동 + 기본색↔흰색 색온도 미세 왕복(은은).
    // 하늘 상단으로 옅은 보라 그라디언트를 프리멀티플라이드 애디티브로 얹는다(검정 배경 방지).
    // _GlobalAlpha로 밤 페이드(StarField.cs가 MPB로 구동).
    Properties
    {
        [HDR] _Color ("Star Tint", Color) = (1, 1, 1, 1)
        _Aspect ("Aspect (quad w/h)", Float) = 2.857
        _Density ("Grid Density", Float) = 40
        _StarSizeMin ("Star Size Min (cell frac)", Range(0.01, 0.5)) = 0.02
        _StarSizeMax ("Star Size Max (cell frac)", Range(0.01, 0.5)) = 0.12
        _StarChance ("Star Chance", Range(0, 1)) = 0.25
        _TwinkleSpeed ("Twinkle Speed", Float) = 2.5
        _Intensity ("Star Intensity (HDR)", Float) = 1.3
        _SkyGradientStrength ("Sky Gradient Strength", Range(0, 2)) = 0.6
        _GlobalAlpha ("Global Alpha (fade)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "StarField"
            Blend SrcAlpha One   // 프리멀티플라이드 애디티브(출력 a=1) — 밤하늘에 별빛·하늘틴트가 더해진다
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
                float _Aspect;
                float _Density;
                float _StarSizeMin;
                float _StarSizeMax;
                float _StarChance;
                float _TwinkleSpeed;
                float _Intensity;
                float _SkyGradientStrength;
                float _GlobalAlpha;
            CBUFFER_END

            // 셀 좌표 → [0,1) 결정적 해시
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
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
                // u에 aspect를 곱해 셀을 정사각화(쿼드 가로 늘림 보정)
                float2 grid = float2(IN.uv.x * _Aspect, IN.uv.y) * _Density;
                float2 cell = floor(grid);
                float2 local = frac(grid);          // 셀 안의 지역 좌표 0..1 (정사각 셀)

                // 존재 판정 — 셀의 일부에만 별
                float exist = hash21(cell);
                float on = step(exist, _StarChance);

                // 셀 안 지터 위치(가장자리 여백 확보) · 크기 랜덤
                float2 jitter = float2(hash21(cell + 11.3), hash21(cell + 37.7));
                float2 center = 0.2 + jitter * 0.6;
                float sizeRand = hash21(cell + 71.9);
                float halfSize = lerp(_StarSizeMin, _StarSizeMax, sizeRand) * 0.5;

                // 정사각 마스크 — 원형 폴오프 없이 step 하드엣지
                float2 d = abs(local - center);
                float inside = step(d.x, halfSize) * step(d.y, halfSize);

                // 4계열 저채도 팔레트 — 해시로 별색 선택(흰 다수, 청백/노랑/주황 소수)
                float colorRand = hash21(cell + 53.1);
                half3 white  = half3(1.00, 1.00, 1.00);
                half3 blue   = half3(0.75, 0.85, 1.00);
                half3 yellow = half3(1.00, 0.97, 0.82);
                half3 orange = half3(1.00, 0.86, 0.66);
                half3 baseCol = white;
                baseCol = colorRand < 0.72 ? blue   : baseCol;
                baseCol = colorRand < 0.55 ? white  : baseCol; // 재정렬: <0.55 흰, <0.72 청백
                baseCol = (colorRand >= 0.72 && colorRand < 0.88) ? yellow : baseCol;
                baseCol = colorRand >= 0.88 ? orange : baseCol;

                // 크기-밝기 상관 — 작은 별일수록 어둡게(원근감)
                float bright = lerp(0.4, 1.0, sizeRand);

                // 반짝임 v2 — 별마다 위상 다른 알파 진동(0.35~1.0)
                float phase = exist * 62.83185;
                float tw = 0.35 + 0.325 * (sin(_Time.y * _TwinkleSpeed + phase) + 1.0);

                // 색온도 미세 왕복 — 기본색↔흰색을 다른 주파수로 은은히 lerp(최대 0.22)
                float temp = 0.5 + 0.5 * sin(_Time.y * _TwinkleSpeed * 0.6 + phase * 1.7);
                half3 starCol = lerp(baseCol, white, temp * 0.22);

                // _Intensity(HDR)를 곱해 별 픽셀을 블룸 임계(threshold) 위로 밀어올린다 — 별무리 번짐.
                half3 rgb = starCol * _Color.rgb * bright * _Intensity;
                float starA = on * inside * tw;

                // 하늘 상단으로 옅은 보라 그라디언트(#0a0d16 → #171030), 프리멀티플라이드 애디티브
                half3 skyBottom = half3(0.039, 0.051, 0.086); // #0a0d16
                half3 skyTop    = half3(0.090, 0.063, 0.188); // #171030
                half3 sky = lerp(skyBottom, skyTop, saturate(IN.uv.y)) * _SkyGradientStrength;

                half3 outRGB = (rgb * starA + sky) * _GlobalAlpha;
                return half4(outRGB, 1); // a=1 → SrcAlpha One이 순수 애디티브로 동작
            }
            ENDHLSL
        }
    }
}
