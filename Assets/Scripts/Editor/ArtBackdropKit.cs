using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-141 — 민지님 아트 배치를 씬에 세운다. **정본은 세트 프리팹**이다.
    ///
    /// 배치의 소유가 민지님에게 있다: `Prefabs/Hand/set_district_2.prefab`·`set_camp_1.prefab`을
    /// 민지님이 유니티에서 고쳐 올리면 코드 수정 없이 전 씬에 반영된다(카탈로그 pull·소켓 스왑
    /// — ARCHITECTURE §7). S-138에서 쓰던 좌표 하드코딩 표는 폐기했다 — 정본이 둘이면 갱신이
    /// 갈라지고, 실제로 프리팹(19개)과 표(10개)의 구성이 이미 어긋나 있었다.
    ///
    /// ⚠ 프리팹 원점 ≠ 씬 배치 원점. 민지님이 세트를 묶을 때 루트가 원점에 있지 않아서,
    /// 프리팹을 (0,0,0)에 놓으면 내용물이 통째로 밀린다. District는 실측상 정확히 강체 이동
    /// (-3.05, 0, +20.17)이라 그 값을 되돌려 원 배치를 복원한다. Camp는 민지님이 개별 조정을
    /// 해서 단일 오프셋으로 환원되지 않으므로(표본 2건이 X 0.51 vs -2.74로 불일치) **프리팹을
    /// 그대로 원점에 둔다** — 민지님이 의도한 최신 배치가 곧 정본이다.
    /// </summary>
    public static class ArtBackdropKit
    {
        private const string BACKDROP_ROOT = "__gb_ArtBackdrop";

        /// <summary>세트 1건 — 프리팹 경로 + 씬에 놓을 루트 위치.</summary>
        public readonly struct SetPlacement
        {
            public readonly string PrefabPath;
            public readonly Vector3 RootPosition;

            public SetPlacement(string prefabPath, Vector3 rootPosition)
            {
                PrefabPath = prefabPath;
                RootPosition = rootPosition;
            }
        }

        /// <summary>
        /// District 배경 파사드. 오프셋 근거 — 프리팹을 원점에 놓고 잰 자식 위치가 원 배치보다
        /// 일정하게 밀려 있었다(Pub_unity −3.04/+20.16 · blossom_tree −3.05/+20.18, 두 표본 일치).
        /// 이 값을 되돌리지 않으면 건물 절반이 z 음수로 넘어와 플레이어 앞을 가린다.
        /// </summary>
        public static readonly SetPlacement District =
            new SetPlacement("Assets/Prefabs/Hand/set_district_2.prefab", new Vector3(-3.05f, 0f, 20.17f));

        /// <summary>Camp 물류장. 민지님 조정본이 정본이라 원점 그대로 둔다(위 ⚠ 참조).</summary>
        public static readonly SetPlacement Camp =
            new SetPlacement("Assets/Prefabs/Hand/set_camp_1.prefab", Vector3.zero);

        // S-180 ② — 나머지 씬에도 **빈 소켓**을 깐다. 프리팹이 없으면 Build가 경고 한 줄 남기고
        // 그냥 지나가므로(아래 구현) 지금 당장은 아무 것도 안 바뀐다. 소켓을 먼저 까 두는 이유는,
        // 민지님이 그 씬에서 배치를 담는 순간 **다음 재조립부터 자동으로 살아나게** 하기 위해서다.
        // 소켓이 없으면 프리팹을 만들어도 아무도 꽂아 주지 않아 작업이 또 사라진다.
        public static readonly SetPlacement Home =
            new SetPlacement("Assets/Prefabs/Hand/set_home.prefab", Vector3.zero);

        public static readonly SetPlacement Hillside =
            new SetPlacement("Assets/Prefabs/Hand/set_hillside.prefab", Vector3.zero);

        public static readonly SetPlacement Apartment =
            new SetPlacement("Assets/Prefabs/Hand/set_apartment.prefab", Vector3.zero);

        /// <summary>
        /// 세트를 씬에 세운다. 멱등 — 기존 `__gb_ArtBackdrop` 루트를 지우고 새로 만든다.
        /// **프리팹 링크를 유지**한다: 민지님이 프리팹을 고치면 재조립 없이도 씬에 전파된다.
        /// 배경층이라 콜라이더는 끈다(통행 판정은 WalkableVolume 소관 — 배경에 걸리면 안 된다).
        /// </summary>
        public static GameObject Build(SetPlacement set, Transform parent = null)
        {
            GameObject existing = GameObject.Find(BACKDROP_ROOT);
            if (existing != null) Object.DestroyImmediate(existing);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(set.PrefabPath);
            if (prefab == null)
            {
                // S-180 ② — 빈 소켓은 **정상 상태**다(아직 아무도 배치를 담지 않은 씬).
                // 워닝으로 두면 재조립마다 콘솔이 더러워지고 "워닝 0건" 납품 기준이 무력해진다.
                Debug.Log($"[아트배경] 세트 미배치 — {set.PrefabPath} (담으면 자동으로 꽂힌다).");
                return null;
            }

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            root.name = BACKDROP_ROOT;
            root.transform.position = set.RootPosition;

            foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            Debug.Log($"[아트배경] {System.IO.Path.GetFileNameWithoutExtension(set.PrefabPath)} "
                + $"배치 — 자식 {root.transform.childCount}개 @ {set.RootPosition}");
            return root;
        }
    }
}
