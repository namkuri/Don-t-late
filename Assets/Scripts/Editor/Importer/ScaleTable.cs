using System.Collections.Generic;

namespace DontLate.EditorTools
{
    /// <summary>
    /// AI 생성 모델 스케일 캘리브레이션 표 (S-111) — 파일명 키워드 → 목표 전고(u).
    /// 기준: 인체 1.7u · 건물 출입문 2.1~2.4u (남규님 산정 규칙). 정확명 우선, 키워드 폴백.
    /// 값 조정 = 이 표 수정 + 재임포트(우클릭 Reimport 또는 art_swap 재실행) — 코드가 정본.
    /// </summary>
    public static class ScaleTable
    {
        /// <summary>정확 파일명(소문자, 확장자 없이) 우선 매칭.</summary>
        private static readonly Dictionary<string, float> Exact = new Dictionary<string, float>
        {
            { "door", 2.2f },            // 출입문 그 자체 — 게이지 기준
            { "logi_center", 6.5f },      // S-115 — 캠프 배경용 (기본 center 10u는 무대 압도)
            { "logistics_center", 6.5f },
            { "prop_box_parcel", 0.6f },  // S-112 — 택배상자 (사람 무릎~허리)
            { "prop_streetlamp", 4.5f },  // S-112 — 가로등 등주 실높이
            { "old_stair", 3.2f },
            { "belt", 0.9f },            // 컨베이어 벨트
            { "cafe", 3.2f },            // Props의 카페 부스
            { "market", 2.2f },          // 포장 매대
            { "orange_market", 2.2f },
            { "chicken_house", 4.2f },   // Buildings·Props 동명 — 소형 점포 취급
        };

        /// <summary>키워드 폴백 — 위에서부터 첫 매칭 (구체적 키워드를 앞에).</summary>
        private static readonly (string keyword, float height)[] Keywords =
        {
            // 건물 — 출입문 2.1~2.4u가 성립하는 전고
            ("apartment", 14f), ("tower", 15f), ("hospital", 12f), ("building", 11f),
            ("center", 10f), ("amusement", 10f), ("church", 9f), ("residence", 8f),
            ("hall", 7f), ("construction", 8f), ("fire_house", 6f), ("police", 5.5f),
            ("house", 5.5f), ("home", 5.5f), ("cafe", 4.5f), ("store", 4.5f), ("pub", 4.5f),
            ("laundry", 4.5f), ("photo", 4.5f), ("hardware", 4.5f), ("stair", 5f),
            // 차량·대형 소품
            ("truck", 2.8f), ("van", 2.2f), ("taxi", 1.5f), ("food_cart", 1.8f),
            ("bending", 1.9f), ("vending", 1.9f), ("signboard", 1.5f), ("beacon", 1.2f),
            // 자연물
            ("blossom_tree", 5f), ("tree", 6f),
            // 가로 소품
            ("bench", 0.85f), ("trash", 1.0f), ("poster", 1.2f), ("bycle", 1.1f),
            // 실내 소품
            ("couch", 0.85f), ("chair", 0.9f), ("desk", 0.75f), ("bed", 0.55f),
            ("tv", 0.6f), ("clock", 0.35f), ("rug", 0.06f), ("pot", 0.7f),
            ("teddy", 0.45f), ("drink", 0.25f), ("box", 0.6f),
        };

        /// <summary>목표 전고 조회 — 없으면 0(스케일 유지, 스냅·경고만).</summary>
        public static float TargetHeight(string assetName)
        {
            string lower = assetName.ToLowerInvariant();
            if (Exact.TryGetValue(lower, out float exactHeight)) return exactHeight;
            foreach ((string keyword, float height) in Keywords)
                if (lower.Contains(keyword)) return height;
            return 0f;
        }
    }
}
