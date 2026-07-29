using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Buildings·Props 모델 임포트 시 Prefabs/Auto/&lt;파일명&gt;.prefab 을 생성/갱신한다.
    /// 멱등: 같은 경로에 SaveAsPrefabAsset 으로 덮어써 재임포트 시 중복이 생기지 않는다.
    /// Prefabs/Hand/ 는 절대 건드리지 않는다 (수제 불가침). 오직 Auto/ 에만 쓴다.
    /// 부착물: 메시 + 결합 바운즈 기반 풋프린트 BoxCollider.
    /// </summary>
    public static class CategoryPrefabFactory
    {
        private const string AUTO_ROOT = "Assets/Prefabs/Auto";

        /// <summary>모델 에셋 경로 목록에서 Auto 프리팹을 생성/갱신한다.</summary>
        public static void BuildPrefabs(List<string> modelPaths)
        {
            EnsureAutoFolder();

            foreach (string modelPath in modelPaths)
            {
                // 지연 실행 사이 에셋이 삭제됐을 수 있으니 존재 확인만 한다(경계 검증).
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null) continue;

                BuildOne(model, modelPath);
            }
        }

        private static void BuildOne(GameObject model, string modelPath)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(modelPath);
            string prefabPath = $"{AUTO_ROOT}/{name}.prefab";

            // 플레인 클론(Instantiate) → 독립 프리팹. InstantiatePrefab 은 소스 모델의
            // Prefab Variant 를 만들어 소스 삭제 시 부모 유실 에러가 나므로 쓰지 않는다.
            GameObject instance = Object.Instantiate(model);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            // S-110 가구 → S-111 전 모델 일반화: 목표 치수 정규화 + 바닥중심 스냅 (AI 생성물 크기·원점 미조정 대응).
            GameObject root = Normalize(instance, name);

            AttachFootprintCollider(root);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// S-110→S-111 — 전 모델 정규화: 목표 전고(가구=FurnitureSO.size.y 우선, 그 외=ScaleTable
        /// 키워드 표 — 인체 1.7u·문 2.1~2.4u 기준)로 스케일하고 결합 바운즈를 바닥중심 원점에
        /// 스냅한다(안전망). 원점 이탈은 경고 리포트 — 아트 원본 교정이 정도, 스냅은 이중 방어.
        /// 표에 없는 이름은 스케일 유지(스냅·경고만).
        /// </summary>
        private static GameObject Normalize(GameObject instance, string name)
        {
            Bounds bounds = CombinedBounds(instance);
            if (bounds.size.y < 0.001f) return instance;

            // 원점 이탈 검역 리포트 (스냅 전 실측 — 민지님 재작업 기준 피드백).
            if (Mathf.Abs(bounds.min.y) > bounds.size.y * 0.1f)
                Debug.LogWarning($"[프리팹팩토리] {name} 원점 이탈 — minY={bounds.min.y:0.00} (규격: 원점=바닥중심). 바닥 스냅 안전망 적용했으나 원본 교정 권장.");

            var so = AssetDatabase.LoadAssetAtPath<FurnitureSO>($"Assets/Data/Furniture/{name}.asset");
            float target = so != null && so.size.y > 0.01f ? so.size.y : ScaleTable.TargetHeight(name);
            float scale = target > 0.01f ? target / bounds.size.y : 1f;
            if (target > 0.01f)
                Debug.Log($"[프리팹팩토리] {name} 스케일 캘리브레이션 — 실측고 {bounds.size.y:0.00}u → 목표 {target:0.0}u (×{scale:0.000})");
            instance.transform.localScale = Vector3.one * scale;

            GameObject wrapper = new GameObject(name);
            instance.transform.SetParent(wrapper.transform, true);
            bounds = CombinedBounds(instance); // 스케일 반영 재실측
            instance.transform.position = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            return wrapper;
        }

        private static Bounds CombinedBounds(GameObject root)
        {
            Renderer[] rends = root.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                bounds.Encapsulate(rends[i].bounds);
            return bounds;
        }

        /// <summary>결합 렌더러 바운즈로 루트에 BoxCollider 를 붙인다.</summary>
        private static void AttachFootprintCollider(GameObject instance)
        {
            Renderer[] rends = instance.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;

            Bounds bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                bounds.Encapsulate(rends[i].bounds);

            BoxCollider box = instance.AddComponent<BoxCollider>();
            // 인스턴스는 원점·무회전·무스케일이므로 월드 바운즈 = 루트 로컬 바운즈.
            box.center = instance.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
        }

        private static void EnsureAutoFolder()
        {
            if (AssetDatabase.IsValidFolder(AUTO_ROOT)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Auto");
        }
    }
}
