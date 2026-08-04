using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 색보정 수치표 (S-131) — 시간대·날씨·구역 세 겹을 인스펙터에서 조절한다.
    /// 구 구조는 <see cref="WorldWeatherManager"/>에 하드코딩돼 있어 색감 한 번 만지려면
    /// 코드 수정 + 재컴파일이 필요했다. 색감은 반복 조정이 잦은 영역이라 데이터로 뺀다.
    ///
    /// ⚠ 이 프로젝트에 .cube LUT 파일은 없다 — URP Volume의 ColorAdjustments·WhiteBalance·Bloom
    /// 조합이 곧 "LUT"다. 볼륨 자체는 WorldWeatherManager가 런타임에 만든다(에셋 무오염).
    ///
    /// **합성 규칙** (WorldWeatherManager.RefreshGradeTarget이 집행):
    ///   노출·채도·색온도·블룸 = 시간대 + 날씨 + 구역  (더한다)
    ///   컬러필터              = 시간대 × 날씨 × 구역  (곱한다 · 흰색이 무변화)
    /// </summary>
    [CreateAssetMenu(menuName = "DontLate/ColorGrade", fileName = "ColorGrade")]
    public class ColorGradeSO : ScriptableObject
    {
        /// <summary>한 겹의 색보정 기여분. 전부 **가산**이고 filter만 **곱산**이다.</summary>
        [System.Serializable]
        public struct Layer
        {
            [Tooltip("노출 보정 (스톱). 0 = 무변화. ±0.5면 눈에 띄게 밝고 어둡다.")]
            [Range(-1.5f, 1.5f)] public float exposure;

            [Tooltip("채도. 0 = 무변화 · −100 = 흑백 · +100 = 두 배.")]
            [Range(-100f, 100f)] public float saturation;

            [Tooltip("색온도. 0 = 무변화 · 음수 = 차갑게(푸름) · 양수 = 따뜻하게(주황).")]
            [Range(-100f, 100f)] public float temperature;

            [Tooltip("컬러 필터 — 흰색이 무변화. 세 겹이 서로 곱해진다.")]
            public Color filter;

            [Tooltip("블룸 강도 기여분. 시간대가 기준값을 주고 날씨가 얹는다.")]
            [Range(-1f, 1.5f)] public float bloom;

            /// <summary>곱셈용 필터. 검게 비워두면(신규 필드 기본값 = 투명 검정) 화면이 통째로
            /// 까매지므로 흰색으로 되돌린다 — 인스펙터 입력 경계 방어.</summary>
            public Color SafeFilter =>
                filter.r + filter.g + filter.b < 0.001f ? Color.white : filter;
        }

        // S-145 — 남규님 요구: "날씨,시간 상관없는 디폴트 컬러 그레이딩(기본 채도값 조절 등)".
        // 아래 3층(시간대·날씨·구역)은 전부 **상황별 보정**이라, 게임 전체의 룩 기준선을 옮기려면
        // 14칸을 일일이 고쳐야 했다. 이 층은 그 위가 아니라 **밑**에 깔려 항상 더해진다 —
        // 여기 채도를 −20 주면 시간대·날씨와 무관하게 화면 전체가 그만큼 차분해진다.
        [Header("기본 — 항상 적용 (전역 룩 기준선)")]
        [Tooltip("시간대·날씨·구역과 무관하게 항상 더해지는 층. 게임 전체 톤을 여기서 조인다.")]
        public Layer baseGrade;

        [Header("시간대 — 기준 톤 (블룸의 기준값도 여기)")]
        public Layer morning;
        public Layer day;
        public Layer evening;
        public Layer night;

        [Header("날씨 — 시간대 위에 얹는 보정")]
        public Layer clear;
        public Layer cloudy;
        public Layer rain;
        public Layer snow;
        public Layer fog;
        public Layer heat;
        public Layer storm;

        [Header("구역 — 동네 분위기")]
        public Layer villaTown;
        public Layer foodAlley;
        public Layer apartment;

        public Layer ForPhase(DayPhase phase) => phase switch
        {
            DayPhase.Morning => morning,
            DayPhase.Evening => evening,
            DayPhase.Night => night,
            _ => day,
        };

        public Layer ForWeather(WeatherType weather) => weather switch
        {
            WeatherType.Cloudy => cloudy,
            WeatherType.Rain => rain,
            WeatherType.Snow => snow,
            WeatherType.Fog => fog,
            WeatherType.Heat => heat,
            WeatherType.Storm => storm,
            _ => clear,
        };

        public Layer ForDistrict(string district)
        {
            if (district == DeliveryOrderSO.DISTRICT_VILLATOWN) return villaTown;
            if (district == DeliveryOrderSO.DISTRICT_FOODALLEY) return foodAlley;
            if (district == DeliveryOrderSO.DISTRICT_APARTMENT) return apartment;
            return Neutral;
        }

        /// <summary>무변화 층 — 구역이 없거나 SO 미주입 시의 기준.</summary>
        public static Layer Neutral => new Layer { filter = Color.white };
    }
}
