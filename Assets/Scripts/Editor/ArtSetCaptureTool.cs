using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    internal static class ArtSetCaptureTool
    {
        private const string BACKDROP_ROOT = "__gb_ArtBackdrop";
        private const string HAND_DIR = "Assets/Prefabs/Hand";

        private static readonly Dictionary<string, string> SetForScene = new Dictionary<string, string>
        {
            { "Camp",      HAND_DIR + "/set_camp_1.prefab" },
            { "District",  HAND_DIR + "/set_district_2.prefab" },
            { "Main",      HAND_DIR + "/set_district_2.prefab" },
            { "Home",      HAND_DIR + "/set_home.prefab" },
            { "Hillside",  HAND_DIR + "/set_hillside.prefab" },
            { "Apartment", HAND_DIR + "/set_apartment.prefab" },
        };

        [MenuItem("DontLate/Art/① 선택 오브젝트를 세트에 담기", priority = 100)]
        private static void CaptureSelection()
        {
            if (!TryResolveSet(out string scenePath, out _)) return;

            GameObject[] picked = Selection.gameObjects;
            if (picked == null || picked.Length == 0)
            {
                Warn("씬에서 담을 오브젝트를 먼저 선택하세요.\n\n"
                    + "(Hierarchy 창에서 클릭 · 여러 개는 Ctrl 누르고 클릭)");
                return;
            }

            GameObject root = GetOrCreateBackdrop(scenePath);
            if (root == null) return;

            int moved = 0;
            foreach (GameObject go in picked)
            {
                if (go == null || go == root) continue;
                if (go.transform.IsChildOf(root.transform)) continue;
                Undo.SetTransformParent(go.transform, root.transform, "세트에 담기");
                moved++;
            }

            if (moved == 0)
            {
                Warn("옮길 것이 없습니다 — 고른 것이 이미 세트 안에 있습니다.\n"
                    + "이 경우엔 ②번 메뉴(현재 배치 저장)만 누르면 됩니다.");
                return;
            }

            if (!SaveSet(root, scenePath)) return;
            Info($"{moved}개를 세트에 담고 저장했습니다.\n\n{scenePath}");
        }

        [MenuItem("DontLate/Art/② 현재 배치 저장 (세트 프리팹에 적용)", priority = 101)]
        private static void SaveCurrent()
        {
            if (!TryResolveSet(out string scenePath, out _)) return;

            GameObject root = GameObject.Find(BACKDROP_ROOT);
            if (root == null)
            {
                Warn($"이 씬에 '{BACKDROP_ROOT}'가 없습니다.\n\n"
                    + "①번 메뉴로 오브젝트를 담으면 자동으로 만들어집니다.");
                return;
            }

            if (!SaveSet(root, scenePath)) return;
            Info($"현재 배치를 저장했습니다.\n\n{scenePath}");
        }

        [MenuItem("DontLate/Art/③ 세트 프리팹 폴더 열기", priority = 102)]
        private static void PingFolder()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(HAND_DIR);
            if (folder != null) EditorGUIUtility.PingObject(folder);
            Selection.activeObject = folder;
        }

        private static bool TryResolveSet(out string setPath, out string sceneName)
        {
            setPath = null;
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (SetForScene.TryGetValue(sceneName, out setPath)) return true;

            Warn($"'{sceneName}' 씬은 아직 세트 소켓이 없습니다.\n\n"
                + "담을 수 있는 씬: " + string.Join(", ", new List<string>(SetForScene.Keys).ToArray()));
            return false;
        }

        private static GameObject GetOrCreateBackdrop(string setPath)
        {
            GameObject root = GameObject.Find(BACKDROP_ROOT);
            if (root != null) return root;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(setPath);
            if (prefab != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                root.name = BACKDROP_ROOT;
                root.transform.position = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(root, "세트 인스턴스 생성");
                return root;
            }

            root = new GameObject(BACKDROP_ROOT);
            Undo.RegisterCreatedObjectUndo(root, "세트 루트 생성");
            return root;
        }

        private static bool SaveSet(GameObject root, string setPath)
        {
            Directory.CreateDirectory(HAND_DIR);

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
            if (source != null && AssetDatabase.GetAssetPath(source) == setPath)
            {
                PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
                AssetDatabase.SaveAssets();
                Debug.Log($"[아트세트] 변경분 적용 — {setPath} (자식 {root.transform.childCount}개)");
                return true;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAssetAndConnect(
                root, setPath, InteractionMode.UserAction, out bool ok);
            if (!ok || saved == null)
            {
                Warn($"저장에 실패했습니다 — {setPath}");
                return false;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[아트세트] 새로 저장 — {setPath} (자식 {root.transform.childCount}개)");
            return true;
        }

        private static void Info(string message) => EditorUtility.DisplayDialog("아트 세트", message, "확인");
        private static void Warn(string message) => EditorUtility.DisplayDialog("아트 세트 — 확인 필요", message, "확인");
    }
}
