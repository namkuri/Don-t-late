using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-187 → **S-188에서 뒤집힘.** 세트 프리팹에서 걷어낼 것은 `__gb_*` 전부가 아니라
    /// **기능물뿐**이다.
    ///
    /// S-187의 오판: 씬에 같은 것이 두 벌 서는 겹침만 보고 "세트의 `__gb_*`를 전부 걷어낸다"로
    /// 갔다. 그런데 그 안에는 민지님이 아파트 벽·창에 입힌 `wall.mat`·`window.mat`·
    /// `qwen_image_*`가 물려 있었다 — **아트 작업물을 지운 것**이다(남규님 적발).
    ///
    /// 바로잡은 규칙: 겹침의 해법은 "세트를 비우기"가 아니라 **누가 이기는지 정하기**다.
    /// 시각물은 아트가 이기고(빌더의 사본을 재조립 때 지운다 — <see cref="ArtBackdropKit"/>),
    /// 기능물은 빌더가 이긴다(프리팹에 얼면 씬 참조가 끊긴다). 판정은 <see cref="ArtSetRules"/> 한 곳.
    ///
    /// 되돌리기: 이 도구는 프리팹을 **덮어쓴다**. git이 안전망이므로 실행 전 커밋 상태를 확인한다.
    /// </summary>
    internal static class ArtSetSanitizer
    {
        private const string HAND_DIR = "Assets/Prefabs/Hand";

        [MenuItem("DontLate/Art/④ 세트 프리팹에서 기능물 걷어내기", priority = 103)]
        private static void SanitizeAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { HAND_DIR });
            int touched = 0, removedTotal = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!System.IO.Path.GetFileName(path).StartsWith("set_")) continue;

                int removed = Sanitize(path);
                if (removed <= 0) continue;
                touched++;
                removedTotal += removed;
            }

            AssetDatabase.SaveAssets();
            string message = touched == 0
                ? "걷어낼 기능물이 없습니다 — 세트 프리팹에 시각물만 들어 있습니다."
                : $"{touched}개 프리팹에서 기능물 {removedTotal}개를 걷어냈습니다.\n\n"
                  + "머티리얼을 입힌 시각물은 그대로 남습니다(아트가 이깁니다).\n"
                  + "프리팹 변경분을 커밋해 주세요.";
            // S-202 — 모달 제거(남규님 제안). 다이얼로그는 스크립트로 부를 때 에디터를 멈춘다.
            Debug.Log("[세트정리] " + message.Replace("\n", " "));
        }

        /// <summary>
        /// 프리팹 하나를 열어 기능물 **최상위 자식**만 지운다.
        /// 최상위만 보는 이유: 빌더 산출물은 루트째로 담기므로, 그 루트를 지우면 하위도 함께 간다.
        /// 아트 모델 내부에 우연히 같은 이름의 노드가 있어도 건드리지 않는다.
        /// </summary>
        private static int Sanitize(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return 0;

            var doomed = new List<GameObject>();
            foreach (Transform child in root.transform)
                if (ArtSetRules.IsBuilderOwned(child.gameObject)) doomed.Add(child.gameObject);

            CollectExactTwins(root.transform, doomed); // S-206

            if (doomed.Count == 0)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return 0;
            }

            // 이름은 **지우기 전에** 챙긴다 — 파괴된 오브젝트의 name 접근은 예외다.
            string[] names = doomed.ConvertAll(g => g.name).ToArray();
            foreach (GameObject go in doomed) Object.DestroyImmediate(go);
            // SaveAsPrefabAsset은 **같은 경로면 GUID를 유지**한다 — 씬·빌더의 참조가 안 끊긴다.
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[세트정리] {System.IO.Path.GetFileName(prefabPath)} — 기능물·쌍둥이 {names.Length}개 제거: "
                + string.Join(", ", names));
            return names.Length;
        }

        /// <summary>
        /// S-206 — **트랜스폼까지 똑같은 쌍둥이 최상위 자식**을 한 벌만 남기고 걷어낸다.
        ///
        /// `set_hillside`에 13종이 이름·위치·회전·스케일이 전부 같은 채로 두 벌씩 들어 있었다
        /// (blue_house·retro_korean_house·old_stair·er… 실측: 두 벌 모두 같은 좌표·스케일).
        /// 완전히 겹친 메시 두 장은 Z파이팅만 만들 뿐 화면에 보태는 게 없다 — 의도된 배치일 수
        /// 없어서 기계적으로 지워도 안전하다. 반대로 **좌표가 조금이라도 다르면 손대지 않는다**:
        /// 같은 집을 여러 채 늘어놓는 것은 정상적인 배치이고, 그건 민지님 몫이다.
        /// </summary>
        private static void CollectExactTwins(Transform root, List<GameObject> doomed)
        {
            var seen = new List<Transform>();
            foreach (Transform child in root)
            {
                if (doomed.Contains(child.gameObject)) continue;

                bool twin = false;
                foreach (Transform other in seen)
                {
                    if (other.name != child.name) continue;
                    if (other.localPosition != child.localPosition) continue;
                    if (other.localRotation != child.localRotation) continue;
                    if (other.localScale != child.localScale) continue;
                    twin = true;
                    break;
                }

                if (twin) doomed.Add(child.gameObject);
                else seen.Add(child);
            }
        }
    }
}
