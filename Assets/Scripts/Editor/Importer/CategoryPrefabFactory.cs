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

            AttachFootprintCollider(instance);

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
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
