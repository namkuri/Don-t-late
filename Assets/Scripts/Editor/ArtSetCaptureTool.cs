using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-180 — **실씬에서 한 아트 배치를 세트 프리팹에 담는 도구.**
    ///
    /// 왜 필요한가: 씬 본문(.unity)은 커밋하지 않고 각 PC가 빌더로 재조립한다(D-061).
    /// 그래서 씬 루트에 흩어 놓은 배치는 다음 재조립 때 통째로 사라진다 — 민지님이 겪은
    /// "푸시할 때마다 날아간다"가 이것이다. 살아남는 유일한 자리는 **세트 프리팹**이고,
    /// `ArtBackdropKit`이 그 프리팹을 링크 유지로 꽂아 준다.
    ///
    /// 기존 안내(S-118)는 "임시 빈 씬에서 만들어 Hand 폴더로 드래그"였다. 하지만 조명·플레이어
    /// 키·카메라 프레이밍을 보며 맞추려면 실씬에서 작업할 수밖에 없다 — 그 경로의 저장법이
    /// 없었던 게 공백이다. 이 도구가 그 공백을 메운다.
    /// </summary>
    internal static class ArtSetCaptureTool
    {
        private const string BACKDROP_ROOT = "__gb_ArtBackdrop";
        private const string HAND_DIR = "Assets/Prefabs/Hand";

        // 씬 이름 → 세트 프리팹 경로. 소켓이 깔린 씬만 담을 수 있다(S-180 ②에서 5개로 확대).
        private static readonly Dictionary<string, string> SetForScene = new Dictionary<string, string>
        {
            { "Camp",      HAND_DIR + "/set_camp_1.prefab" },
            { "District",  HAND_DIR + "/set_district_2.prefab" },
            { "Main",      HAND_DIR + "/set_district_2.prefab" }, // Main은 District 무대를 재사용한다
            { "Home",      HAND_DIR + "/set_home.prefab" },
            { "Hillside",  HAND_DIR + "/set_hillside.prefab" },
            { "Apartment", HAND_DIR + "/set_apartment.prefab" },
        };

        [MenuItem("DontLate/Art/① 선택 오브젝트를 세트에 담기", priority = 100)]
        private static void CaptureSelection()
        {
            if (!TryResolveSet(out string scenePath, out string sceneName)) return;

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
            var skipped = new List<string>();
            foreach (GameObject go in picked)
            {
                if (go == null || go == root) continue;
                // 이미 세트 안에 있는 건 건드리지 않는다 — 부모를 바꾸면 좌표만 흔들린다.
                if (go.transform.IsChildOf(root.transform)) continue;
                // S-188 — **기능물만 거른다.** 빌더가 만든 벽·바닥이라도 머티리얼을 입혔다면
                // 그건 아트 작업물이므로 담아야 한다(재조립 때 빌더 사본이 대신 지워진다).
                // 반면 엘베·자동문·걷기볼륨처럼 로직을 든 것은 프리팹에 얼면 씬 참조가 끊긴다.
                if (ArtSetRules.IsBuilderOwned(go)) { skipped.Add(go.name); continue; }
                // 월드 좌표를 지킨 채 부모만 바꾼다(worldPositionStays: true).
                Undo.SetTransformParent(go.transform, root.transform, "세트에 담기");
                moved++;
            }

            if (skipped.Count > 0)
                Debug.Log("[아트세트] 기능물 " + skipped.Count + "개는 담지 않았다(재조립이 다시 만든다): "
                    + string.Join(", ", skipped.ToArray()));

            if (moved == 0)
            {
                Warn("옮길 것이 없습니다 — 고른 것이 이미 세트 안에 있습니다.\n"
                    + "이 경우엔 ②번 메뉴(현재 배치 저장)만 누르면 됩니다.");
                return;
            }

            if (!SaveSet(root, scenePath)) return;
            Info($"{moved}개를 세트에 담고 저장했습니다.\n\n{scenePath}\n\n"
                + "이제 씬을 재조립해도 남습니다. 프리팹 파일(.prefab + .meta)만 커밋해 주세요 — "
                + "씬 파일(.unity)은 커밋하지 않습니다.");
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
            Info($"현재 배치를 저장했습니다.\n\n{scenePath}\n\n"
                + "프리팹 파일(.prefab + .meta)만 커밋해 주세요.");
        }

        [MenuItem("DontLate/Art/③ 세트 프리팹 폴더 열기", priority = 102)]
        private static void PingFolder()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(HAND_DIR);
            if (folder != null) EditorGUIUtility.PingObject(folder);
            Selection.activeObject = folder;
        }

        // ── 내부 ────────────────────────────────────────────────

        private static bool TryResolveSet(out string setPath, out string sceneName)
        {
            setPath = null;
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (SetForScene.TryGetValue(sceneName, out setPath)) return true;

            Warn($"'{sceneName}' 씬은 아직 세트 소켓이 없습니다.\n\n"
                + "담을 수 있는 씬: " + string.Join(", ", new List<string>(SetForScene.Keys).ToArray())
                + "\n\n필요하면 디스코드로 알려주세요 — 소켓을 깔아 드립니다.");
            return false;
        }

        private static GameObject GetOrCreateBackdrop(string setPath)
        {
            GameObject root = GameObject.Find(BACKDROP_ROOT);
            if (root != null) return root;

            // 세트 프리팹이 이미 있으면 그것을 꽂아 이어서 담는다(기존 배치를 덮어쓰지 않는다).
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

        /// <summary>루트를 세트 프리팹으로 저장한다. 이미 프리팹 인스턴스면 변경분을 적용한다.</summary>
        private static bool SaveSet(GameObject root, string setPath)
        {
            Directory.CreateDirectory(HAND_DIR);

            // 프리팹 인스턴스면 **Apply**가 정답이다 — SaveAsPrefabAsset으로 덮으면 링크가
            // 새로 맺어지며 다른 씬에 꽂힌 같은 세트의 연결이 흔들린다.
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
                Warn($"저장에 실패했습니다 — {setPath}\n\n"
                    + "파일이 잠겨 있거나 경로가 잘못됐을 수 있습니다. 디스코드로 알려주세요.");
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
