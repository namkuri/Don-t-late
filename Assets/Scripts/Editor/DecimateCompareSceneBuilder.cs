using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 감축 비교 씬 (S-132) — 원본 vs Blender Decimate 24k 판을 나란히 세워 육안 판정한다.
    /// 좌 = 원본(약 50만 삼각형) / 우 = 감축본. 쌍 사이는 붙이고 쌍끼리는 띄워 짝이 눈에 잡히게.
    ///
    /// **프리팹으로 세운다** — 원본 FBX를 직접 인스턴스화하면 ScaleTable 캘리브레이션(목표 전고)이
    /// 통째로 빠져 실제 씬의 1/5 크기가 된다(실측 확인). 프리팹 팩토리가 정규화한 것이 정본이다.
    /// </summary>
    public static class DecimateCompareSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/DecimateCompare.unity";
        private const string PREFAB_ROOT = "Assets/Prefabs/Auto/";

        /// <summary>비교 대상 — 감축이 어려운 형태를 골고루 (얇은 구조·곡면·잡동사니).</summary>
        private static readonly (string name, string note)[] Pairs =
        {
            ("Hardware_store",    "상점 · 내부 잡동사니"),
            ("retro_korean_house", "한옥 · 기와"),
            ("Blue_Apartment_2",  "고층 · 창 격자"),
            ("Red_Church_unity",  "첨탑 · 얇은 수직"),
            ("korean_cafe",       "한옥 처마 · 곡면"),
            ("hospital",          "대형 매스"),
            ("Pub_unity",         "간판 · 차양"),
            ("control_tower",     "가늘고 높음 · 최난이도"),
            ("basic_tree",        "가지 · 잎"),
            ("blossom_tree",      "벚꽃 · 잎 밀집"),
            ("Food_cart_unity",   "천막 · 바퀴"),
            ("Bending_Mechine",   "자판기 · 평면"),
            ("bycle",             "가는 프레임 · 최난이도"),
            ("Signboard_unity",   "얇은 판"),
            ("couch",             "가구 곡면"),
            ("3_trash",           "잡동사니 덩어리"),
        };

        [MenuItem("DontLate/Build/Decimate Compare Scene (감축 비교)", priority = 32)]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 바닥 — 크기 감을 잡는 격자 대용.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(20f, 1f, 8f);
            floor.GetComponent<Renderer>().sharedMaterial =
                GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Art/UI/Fonts/DNFBitBitOTF SDF.asset");

            var missing = new List<string>();
            float cursorX = 0f;
            const float PAIR_GAP = 1.2f;   // 원본↔감축 간격 (짝으로 읽히게 좁게)
            const float GROUP_GAP = 4.5f;  // 쌍끼리 간격

            foreach ((string name, string note) in Pairs)
            {
                GameObject original = Load(name);
                GameObject reduced = Load("zz24_" + name) ?? Load("zz16_" + name);
                if (original == null || reduced == null) { missing.Add(name); continue; }

                // S-132 — 대형 건물은 표면적이 넓어 같은 예산으로는 찢어진다(남규님 지적).
                // 전고 비례 예산으로 뽑은 고예산판이 있으면 3열로 세워 비교한다.
                GameObject high = Load("zzHI_" + name);

                float width = Mathf.Max(FootprintX(original), FootprintX(reduced));
                float step = Mathf.Max(PAIR_GAP, width * 1.1f);

                Place(original, cursorX, name + "  [원본]");
                Place(reduced, cursorX + step, name + "  [24k]");
                if (high != null) Place(high, cursorX + step * 2f, name + "  [전고비례]");
                MakeLabel(font, cursorX + step * (high != null ? 1f : 0.5f),
                    name + "\n<size=60%>" + note + (high != null ? " · 원본/24k/전고비례" : "") + "</size>");

                cursorX += step * (high != null ? 3f : 2f) + GROUP_GAP;
            }

            // 사람 키 기준자 — 1.8u. 감축 손실이 "화면에서 얼마나 보이는지"의 척도.
            GameObject ruler = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ruler.name = "기준자_사람1.8u";
            ruler.transform.position = new Vector3(-3f, 0.9f, 0f);
            ruler.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            ruler.GetComponent<Renderer>().sharedMaterial =
                GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);

            GreyboxStageBuilder.ConfigureCamera();
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 6f, -26f);
                camera.transform.rotation = Quaternion.Euler(9f, 0f, 0f);
                camera.farClipPlane = 500f;
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log("[감축비교] 씬 조립 — 쌍 " + (Pairs.Length - missing.Count) + "/" + Pairs.Length
                + (missing.Count > 0 ? " · 누락: " + string.Join(", ", missing) : "")
                + " · 좌=원본 / 우=24k. 카메라를 좌우로 밀며 훑어보면 된다.");
        }

        private static GameObject Load(string name)
            => AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_ROOT + name + ".prefab");

        private static float FootprintX(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 2f;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            return Mathf.Max(1f, bounds.size.x);
        }

        private static void Place(GameObject prefab, float x, string label)
        {
            GameObject go = (GameObject)Object.Instantiate(prefab);
            go.name = label;
            go.transform.position = new Vector3(x, 0f, 0f);
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }

        private static void MakeLabel(TMP_FontAsset font, float x, string text)
        {
            if (font == null) return;
            GameObject go = new GameObject("Label");
            go.transform.position = new Vector3(x, -0.6f, -2.2f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro label = go.AddComponent<TextMeshPro>();
            label.font = font;
            label.text = text;
            label.fontSize = 4f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(10f, 3f);
        }
    }
}
