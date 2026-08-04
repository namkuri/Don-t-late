using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-138 — 민지님이 씬에서 손으로 확정한 아트 배치를 빌더로 이관한 정본.
    ///
    /// 배경: 민지님이 `Camp 1.unity`·`District 2.unity`에서 슬롯 보드 위에 실아트를 얹어
    /// 배치를 확정했다. 그러나 씬 본문은 커밋 금지(D-061 — 빌더가 정본, 병합 지옥 방지)이므로
    /// **좌표만 추출해 여기 표로 굳히고, 빌더가 매번 재현**한다. 씬 파일은 버리고 배치는 남는다.
    ///
    /// ⚠ **localScale이 아니라 월드 크기로 기록한다** — S-132에서 프리팹 팩토리의 루트 스케일
    /// 덮어쓰기 버그를 고치고(`Vector3.Scale` 곱셈 교정) 전 모델을 재감축했기 때문에, 민지님이
    /// 씬을 만들던 시점의 프리팹과 현재 프리팹은 기준 크기가 다르다. 원 씬의 `localScale`을
    /// 그대로 복사하면 배경이 10배로 부푼다(실측: 결합 바운즈 전고 151u·중심 y=75).
    /// 그래서 **바운즈 중심 + 전고**를 기록하고, 빌드 때 프리팹 실측값으로 역산해 맞춘다 —
    /// 프리팹이 또 바뀌어도 배치는 살아남는다.
    /// </summary>
    public static class ArtBackdropKit
    {
        private const string BACKDROP_ROOT = "__gb_ArtBackdrop";
        private const string PREFAB_DIR = "Assets/Prefabs/Auto/";

        /// <summary>배치 1건 — 프리팹명 + 목표 바운즈 중심 + Y회전 + 목표 전고(u).</summary>
        public readonly struct Placement
        {
            public readonly string Prefab;
            public readonly Vector3 Center;
            public readonly float RotationY;
            public readonly float Height;

            public Placement(string prefab, Vector3 center, float rotationY, float height)
            {
                Prefab = prefab;
                Center = center;
                RotationY = rotationY;
                Height = height;
            }
        }

        // ── District 배경 파사드 (민지님 `District 2.unity` 실측) ──────────────
        // 원 씬에는 18개가 있었으나 8개는 전고 0.1~1.1u(0.1u 벤치 등)로 명백한 낙하 잔해라
        // 제외했다 — 같은 이름의 정상 배치가 "(1)" 사본으로 따로 있고 그쪽이 실물이다.
        public static readonly Placement[] District =
        {
            new Placement("orange_market",      new Vector3(-5.21f, 1.96f, 6.43f),  180f,  4.88f),
            new Placement("chicken_house",      new Vector3(-12.85f, 2.99f, 7.50f), 180f,  5.91f),
            new Placement("Food_cart_unity",    new Vector3(-18.13f, 1.23f, 6.12f), 180f,  3.41f),
            new Placement("blossom_tree",       new Vector3(5.52f, 5.57f, 5.55f),     0f, 11.19f),
            new Placement("cafe",               new Vector3(19.98f, 3.25f, 9.68f),  270f,  7.26f),
            new Placement("police",             new Vector3(12.46f, 5.49f, 12.75f), 270f, 11.76f),
            new Placement("brown_hall",         new Vector3(-26.11f, 5.49f, 22.02f),180f, 11.73f),
            new Placement("Laundry_Home_unity", new Vector3(-10.38f, 4.49f, 26.58f),180f,  9.64f),
            new Placement("retro_korean_house", new Vector3(8.64f, 4.60f, 25.13f),  180f,  8.12f),
            new Placement("Pub_unity",          new Vector3(-6.23f, 3.51f, 35.99f), 180f,  8.12f),
        };

        // ── Camp 물류장 소품·건물 (민지님 `Camp 1.unity` 실측) ────────────────
        public static readonly Placement[] Camp =
        {
            new Placement("Beacon_unity",       new Vector3(-4.66f, 0.50f, -3.61f),   0f,  1.02f),
            new Placement("Signboard_unity",    new Vector3(-12.38f, 0.50f, -1.62f),270f,  1.01f),
            new Placement("Signboard_unity",    new Vector3(-12.38f, 0.50f, 1.38f), 270f,  1.01f),
            new Placement("black_Trash_unity",  new Vector3(-11.42f, 0.32f, 0.78f),   0f,  0.75f),
            new Placement("black_Trash_unity",  new Vector3(-4.10f, 0.32f, 6.24f),    0f,  1.16f),
            new Placement("dirty_box",          new Vector3(0.03f, 0.29f, -3.45f),    0f,  0.62f),
            new Placement("dirty_box",          new Vector3(0.78f, 0.29f, -3.45f),    0f,  0.85f),
            new Placement("Bench_unity",        new Vector3(-7.54f, 0.32f, 7.59f),  165f,  1.75f),
            new Placement("basic_tree",         new Vector3(-21.95f, 2.26f, 10.35f),  0f,  7.03f),
            new Placement("basic_tree",         new Vector3(-7.20f, 3.01f, 10.35f),   0f,  7.03f),
            new Placement("Construction_unity", new Vector3(-13.75f, 3.71f, 9.45f), 270f,  8.19f),
            new Placement("Logistics_Center",   new Vector3(17.46f, 0.16f, 16.11f),   0f,  5.63f),
        };

        /// <summary>
        /// 배치표를 씬에 세운다. 멱등 — 기존 `__gb_ArtBackdrop` 루트를 지우고 새로 만든다.
        /// 배경층이라 콜라이더는 끈다(플레이어가 걸리면 안 된다 — 통행은 WalkableVolume 소관).
        /// </summary>
        public static GameObject Build(Placement[] placements, Transform parent = null)
        {
            GameObject existing = GameObject.Find(BACKDROP_ROOT);
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject root = new GameObject(BACKDROP_ROOT);
            if (parent != null) root.transform.SetParent(parent, false);

            int placed = 0;
            foreach (Placement p in placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + p.Prefab + ".prefab");
                if (prefab == null)
                {
                    Debug.LogWarning($"[아트배경] 프리팹 없음 — {p.Prefab} (배치 건너뜀). "
                        + "Art/ 재임포트로 Prefabs/Auto가 생성됐는지 확인.");
                    continue;
                }

                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, p.RotationY, 0f));

                // ① 프리팹 실측 전고로 목표 전고를 역산 — 프리팹 기준이 바뀌어도 크기가 유지된다.
                if (!TryGetBounds(go, out Bounds raw)) { Object.DestroyImmediate(go); continue; }
                float factor = raw.size.y > 0.001f ? p.Height / raw.size.y : 1f;
                go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * factor);

                // ② 스케일 반영 후 재실측해 바운즈 중심을 목표에 맞춘다(원점 이탈 모델 대응).
                if (TryGetBounds(go, out Bounds scaled))
                    go.transform.position += p.Center - scaled.center;

                foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                placed++;
            }

            Debug.Log($"[아트배경] {placed}/{placements.Length}개 배치");
            return root;
        }

        private static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            bounds = default;
            if (renderers.Length == 0) return false;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }
    }
}
