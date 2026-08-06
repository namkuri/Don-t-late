using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-186 ③ — 먹자골목(FoodStreet.unity).
    ///
    /// 왜 별도 빌더인가: 종전엔 빌라촌·먹자골목이 `District.unity` **한 씬을 공유**해
    /// "네 구역인데 두 곳이 똑같다"가 됐다(남규님 지적). 구역 : 씬을 1:1로 갈라야
    /// 개척이 보상으로 읽힌다.
    ///
    /// 왜 베끼지 않는가: 무대 골격(지면·도로·신호·행인·비콘 스포너)은 District와 같아야 한다 —
    /// 그건 코어루프의 규격이지 구역의 개성이 아니다. 그래서 **같은 빌더를 호출**하고
    /// (S-144 Main 선례) 구역색은 **건물 풀**로만 낸다.
    /// </summary>
    public static class FoodStreetSceneBuilder
    {
        private const string FOODSTREET_PATH = "Assets/Scenes/FoodStreet.unity";

        /// <summary>
        /// 먹자골목 건물 풀 — 음식점·카페·주점 위주.
        /// 이름은 `Assets/Art/Buildings/*.fbx` 파일명과 맞춘다(풀 선정이 파일명으로 걸린다).
        /// 실물이 없는 이름을 적어도 조용히 건너뛰므로, 아트가 늘면 여기에 이름만 더하면 된다.
        /// </summary>
        private static readonly string[] FoodBuildings =
        {
            "Pub_unity",        // 주점
            "brown_cafe",       // 카페
            "korean_cafe",      // 한식 카페
            "korean_cafe_2",
            "chicken_house",    // 치킨집
            "store_2",          // 편의점
            "brown_hall",       // 홀(식당)
            "Hardware_store",   // 상가 채움
            "blue_store_house",
        };

        [MenuItem("DontLate/Build/Food Street Stage", priority = 14)]
        public static void BuildFoodStreetStage()
        {
            EnsureSceneFile();
            EnsureSetSeed();
            DistrictSceneBuilder.BuildStage(FOODSTREET_PATH, FoodBuildings, ArtBackdropKit.FoodStreet);
            Debug.Log("[FoodStreet] 먹자골목 무대 조립 완료 — 음식점 풀 " + FoodBuildings.Length + "종.");
        }

        /// <summary>
        /// S-192 — 먹자골목 전용 세트를 **빌라촌 세트의 사본으로 시작**한다.
        ///
        /// 빈 소켓으로 두면 소켓을 판 그 순간부터 거리가 통째로 비어 버린다(실측 — 배경이 사라졌다).
        /// 지금 보이는 거리를 그대로 물려받고, 아트가 여기서부터 먹자골목답게 고쳐 나가는 편이
        /// 안전하다. 한 번만 만든다 — 이후 재조립은 아트가 담은 내용을 건드리지 않는다.
        /// </summary>
        private static void EnsureSetSeed()
        {
            const string seed = "Assets/Prefabs/Hand/set_district_2.prefab";
            string target = ArtBackdropKit.FoodStreet.PrefabPath;
            if (System.IO.File.Exists(target)) return;
            if (!System.IO.File.Exists(seed))
            {
                Debug.Log("[FoodStreet] 씨앗 세트 없음 — 빈 소켓으로 시작한다: " + seed);
                return;
            }

            AssetDatabase.CopyAsset(seed, target);
            AssetDatabase.ImportAsset(target);
            Debug.Log("[FoodStreet] 전용 세트 생성(빌라촌 사본) — " + target
                + " · 이제 먹자골목에서 담아도 빌라촌은 안 바뀐다.");
        }

        /// <summary>
        /// 씬 파일이 없으면 만든다. `BuildStage`는 `OpenScene`으로 시작하므로 파일이 먼저 있어야 한다
        /// (Hillside 선례와 같은 이유 — 최초 실행에서 씬이 없어 터지는 것을 막는다).
        /// </summary>
        private static void EnsureSceneFile()
        {
            if (System.IO.File.Exists(FOODSTREET_PATH)) return;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, FOODSTREET_PATH);
            Debug.Log("[FoodStreet] 씬 파일 신규 생성 — " + FOODSTREET_PATH);
        }
    }
}
