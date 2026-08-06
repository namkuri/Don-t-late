using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-187b — 세트 프리팹과 빌더 사이의 **소유권 규칙 한 곳**.
    ///
    /// 배경: 아트가 씬에서 빌더 산출물(`__gb_*`)에 머티리얼을 입혀 보낸다 — 아파트 벽의
    /// `wall.mat`·`window.mat`, 창틀의 `qwen_image_*` 등. 그 설정을 살리려면 **세트에 담긴
    /// 시각물이 빌더 것을 이겨야** 한다(남규님 판정: "아트에서 셋팅해서 보낸 __gb들 셋팅까지
    /// 전부 차용해야 한다. 기존 씬에 배치되어 있던 __gb들을 삭제하는 게 맞다").
    ///
    /// 다만 **기능물은 반대**다. 엘리베이터·자동문·비번 게이트·대차·걷기볼륨은 컴포넌트와
    /// 씬 참조를 들고 있어, 프리팹으로 얼어붙으면 참조가 끊기거나 두 벌이 돌아 판정이 어긋난다.
    /// 이건 빌더가 매번 새로 만드는 것이 맞다.
    ///
    /// 그래서 경계는 **"게임 로직을 들고 있는가"** 하나다. 이름 목록으로 가르지 않는다 —
    /// 목록은 새 오브젝트가 생길 때마다 낡고, 낡으면 조용히 틀린다.
    /// 세 곳(담기 도구·정리 도구·백드롭 배치)이 **같은 판정**을 써야 어긋나지 않으므로 여기 모은다.
    /// </summary>
    internal static class ArtSetRules
    {
        /// <summary>
        /// 빌더가 소유해야 하는 것 — 세트에 담기지 않고, 담겼으면 걷어낸다.
        /// ① 게임 로직(MonoBehaviour)을 든 것 ② 카메라 ③ UI 층 ④ 씬 이름표.
        /// </summary>
        internal static bool IsBuilderOwned(GameObject go)
        {
            if (go == null) return false;

            string n = go.name;
            if (n.StartsWith("__ui_")) return true;                 // UI 층은 아트 대상이 아니다
            if (n == "Main Camera" || n.StartsWith("SceneLabel_")) return true;
            if (n == "Slots") return true;                          // 절차 배치용 마커

            // 게임 로직을 든 것은 빌더 몫 — 프리팹에 얼면 씬 참조가 끊긴다.
            if (go.GetComponentsInChildren<MonoBehaviour>(true).Length > 0) return true;
            if (go.GetComponentsInChildren<Camera>(true).Length > 0) return true;

            return false;
        }

        /// <summary>
        /// 아트가 이기는 것 — 세트에 같은 이름이 있으면 씬의 빌더 사본을 지운다.
        ///
        /// **렌더러가 있어야** 한다. 빌더는 눈에 안 보이는 마커도 `__gb_`로 만든다
        /// (`__gb_BoxOrigin`·`__gb_BeaconAnchor2F` 등 — 상자 스폰 위치, 비콘 앵커).
        /// 이런 건 빌더가 **직접 참조로 배선**하므로 씬 사본을 지우면 참조가 끊긴다.
        /// 아트가 손댈 이유도 없다(보이지 않으므로). 그래서 교체는 **보이는 것에만** 건다.
        /// </summary>
        internal static bool ArtOverridesBuilder(GameObject setChild)
        {
            if (setChild == null) return false;
            if (IsBuilderOwned(setChild)) return false;
            if (!setChild.name.StartsWith("__gb_")) return false;
            return setChild.GetComponentInChildren<Renderer>(true) != null;
        }
    }
}
