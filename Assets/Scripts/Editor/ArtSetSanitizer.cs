using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-187 — 세트 프리팹에 섞여 들어간 **빌더 생성물**을 걷어낸다.
    ///
    /// 왜 필요했나: 담기 도구(S-180)가 선택한 것을 그대로 담았고, 씬에는 빌더가 만든
    /// `__gb_*`가 함께 있었다. 그래서 `set_apartment`에 40개, `set_hillside`에 25개의
    /// 빌더 생성물이 들어갔다 — 재조립하면 빌더가 한 벌, 세트가 또 한 벌을 꽂아
    /// **같은 자리에 두 벌**이 선다. 이것이 남규님이 본 "겹침"의 원인이다.
    /// (담기 도구 자체는 `IsBuilderOwned` 필터로 막았으므로 재발하지 않는다.)
    ///
    /// 되돌리기: 이 도구는 프리팹을 **덮어쓴다**. git이 안전망이므로 실행 전 커밋 상태를 확인한다.
    /// </summary>
    internal static class ArtSetSanitizer
    {
        private const string HAND_DIR = "Assets/Prefabs/Hand";

        [MenuItem("DontLate/Art/④ 세트 프리팹에서 빌더 생성물 걷어내기", priority = 103)]
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
                ? "걷어낼 빌더 생성물이 없습니다 — 세트 프리팹이 전부 순수 아트입니다."
                : $"{touched}개 프리팹에서 빌더 생성물 {removedTotal}개를 걷어냈습니다.\n\n"
                  + "이제 재조립해도 두 벌이 겹치지 않습니다.\n프리팹 변경분을 커밋해 주세요.";
            EditorUtility.DisplayDialog("세트 정리", message, "확인");
            Debug.Log("[세트정리] " + message.Replace("\n", " "));
        }

        /// <summary>
        /// 프리팹 하나를 열어 빌더 생성물 **최상위 자식**만 지운다.
        /// 최상위만 보는 이유: 빌더 산출물은 루트째로 담기므로, 그 루트를 지우면 하위도 함께 간다.
        /// 아트 모델 내부에 우연히 같은 이름의 노드가 있어도 건드리지 않는다.
        /// </summary>
        private static int Sanitize(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return 0;

            var doomed = new List<GameObject>();
            foreach (Transform child in root.transform)
                if (IsBuilderOwned(child.gameObject.name)) doomed.Add(child.gameObject);

            if (doomed.Count == 0)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return 0;
            }

            foreach (GameObject go in doomed) Object.DestroyImmediate(go);
            // SaveAsPrefabAsset은 **같은 경로면 GUID를 유지**한다 — 씬·빌더의 참조가 안 끊긴다.
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[세트정리] {System.IO.Path.GetFileName(prefabPath)} — 빌더 생성물 {doomed.Count}개 제거: "
                + string.Join(", ", doomed.ConvertAll(g => g.name).ToArray()));
            return doomed.Count;
        }

        /// <summary>담기 도구(ArtSetCaptureTool)와 **같은 기준**을 쓴다 — 둘이 갈리면 또 섞인다.</summary>
        private static bool IsBuilderOwned(string n)
        {
            if (n.StartsWith("__gb_") || n.StartsWith("__ui_")) return true;
            if (n == "Main Camera" || n.StartsWith("SceneLabel_")) return true;
            if (n == "Slots" || n == "CenterLine") return true;
            return false;
        }
    }
}
