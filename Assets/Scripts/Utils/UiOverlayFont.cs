using TMPro;

namespace DontLate
{
    /// <summary>
    /// 런타임 생성 오버레이 라벨용 한글 폰트 공유점 (S-073 — 패드 라벨 한글 네모 재발 수리).
    /// Core의 InvoiceView(빌더가 프로젝트 기본 한글 폰트 주입)가 기동 시 등록하고, 런타임 스폰 오브젝트
    /// (DeliveryPoint 비콘 등)가 참조한다 — 비콘은 빌더 주입 경로가 없어 이 공유점이 유일하다.
    /// </summary>
    public static class UiOverlayFont
    {
        public static TMP_FontAsset Korean;
    }
}
