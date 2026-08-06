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
            EditorUtility.DisplayDialog("세트 정리", message, "확인");
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

            if (doomed.Count == 0)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return 0;
            }

            foreach (GameObject go in doomed) Object.DestroyImmediate(go);
            // SaveAsPrefabAsset은 **같은 경로면 GUID를 유지**한다 — 씬·빌더의 참조가 안 끊긴다.
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[세트정리] {System.IO.Path.GetFileName(prefabPath)} — 기능물 {doomed.Count}개 제거: "
                + string.Join(", ", doomed.ConvertAll(g => g.name).ToArray()));
            return doomed.Count;
        }
    }
}
