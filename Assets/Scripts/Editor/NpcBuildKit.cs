using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// NPC 그레이박스 조립 공용 키트 (S-052). 캡슐 몸통+머리 피규어와
    /// 대사 시나리오 SO 에셋(Data/Dialogue/) GetOrCreate를 제공한다. 각 씬 빌더가 사용.
    /// </summary>
    internal static class NpcBuildKit
    {
        private const string NPC_INFO_SPRITE_PATH = "Assets/Art/UI/npc_info.png";
        private const string DIALOGUE_DIR = "Assets/Data/Dialogue";

        /// <summary>캡슐 몸통 + 머리 피규어. 반환 GO에 컴포넌트를 붙여 쓴다. 콜라이더는 호출부 몫.</summary>
        internal static (GameObject go, Renderer body) BuildFigure(
            string name, Vector3 position, string bodyMatName, Color bodyColor, float height)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty(name, position);

            Material bodyMat = GreyboxStageBuilder.GetOrCreateMaterial(bodyMatName, bodyColor, false);
            Material skinMat = GreyboxStageBuilder.GetOrCreateMaterial("NpcSkin", new Color(0.87f, 0.72f, 0.60f), false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            float bodyH = height * 0.72f;
            body.transform.localPosition = new Vector3(0f, bodyH * 0.5f, 0f);
            body.transform.localScale = new Vector3(height * 0.28f, bodyH * 0.5f, height * 0.28f);
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.sharedMaterial = bodyMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, bodyH + height * 0.13f, 0f);
            head.transform.localScale = Vector3.one * (height * 0.24f);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;

            // S-080 ③ — 눈 2개 (로컬 +z = 바라보는 방향 표지): 응시 여부를 눈으로 판별하게.
            Material eyeMat = GreyboxStageBuilder.GetOrCreateMaterial("NpcEye", new Color(0.08f, 0.09f, 0.12f), false);
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eye.name = side < 0 ? "EyeL" : "EyeR";
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(head.transform, false);
                eye.transform.localPosition = new Vector3(side * 0.22f, 0.12f, 0.42f);
                eye.transform.localScale = new Vector3(0.14f, 0.2f, 0.12f);
                eye.GetComponent<Renderer>().sharedMaterial = eyeMat;
            }

            return (root, bodyRenderer);
        }

        /// <summary>말 걸 수 있는 NPC용 트리거 콜라이더 — 센서가 같은 GO의 콜라이더로 찾는다.</summary>
        internal static void AddInteractTrigger(GameObject go, float height)
        {
            CapsuleCollider trigger = go.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.height = height;
            trigger.radius = 0.45f;
            trigger.center = new Vector3(0f, height * 0.5f, 0f);
        }

        /// <summary>대사 시나리오 SO — Data/Dialogue/에 생성·갱신(멱등, 빌더가 정본).</summary>
        internal static DialogueScenarioSO GetOrCreateScenario(string fileName, params (string speaker, string text)[] lines)
        {
            if (!AssetDatabase.IsValidFolder(DIALOGUE_DIR))
                AssetDatabase.CreateFolder("Assets/Data", "Dialogue");

            string path = DIALOGUE_DIR + "/" + fileName + ".asset";
            DialogueScenarioSO scenario = AssetDatabase.LoadAssetAtPath<DialogueScenarioSO>(path);
            if (scenario == null)
            {
                scenario = ScriptableObject.CreateInstance<DialogueScenarioSO>();
                AssetDatabase.CreateAsset(scenario, path);
            }

            scenario.lines = new DialogueScenarioSO.Line[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                scenario.lines[i] = new DialogueScenarioSO.Line { speaker = lines[i].speaker, text = lines[i].text };
            EditorUtility.SetDirty(scenario);
            return scenario;
        }

        /// <summary>심부름 노인 (S-052 ③) — 말 걸면 짐 옮기기 의뢰, 완료 후 보상. 부재 추첨 내장.</summary>
        internal static void BuildErrandNpc(string name, string speaker, Vector3 position,
            Vector3 targetPosition, GameStateSO gameState, int reward)
        {
            bool granny = speaker.Contains("할머니");
            var (go, body) = BuildFigure(name, position,
                granny ? "NpcGranny" : "NpcGrandpa",
                granny ? new Color(0.58f, 0.42f, 0.52f) : new Color(0.40f, 0.46f, 0.55f), 1.45f);
            AddInteractTrigger(go, 1.45f);
            AttachNameLabel(go, granny ? "granny" : null, speaker); // S-120 — 근접 이름표

            string key = granny ? "Granny" : "Grandpa";
            DialogueScenarioSO ask = GetOrCreateScenario("Scenario_" + key + "_Ask",
                (speaker, "이보게 총각... 이 짐이 무거워서 그러는데, 저기 빛나는 자리까지만 옮겨줄 수 있겠나?"),
                (speaker, "다 옮기고 나한테 다시 와주게. 고마움은 꼭 갚을 테니."));
            DialogueScenarioSO progress = GetOrCreateScenario("Scenario_" + key + "_Progress",
                (speaker, "저기 빛나는 자리까지 부탁하네. 천천히 해도 괜찮아."));
            DialogueScenarioSO thanks = GetOrCreateScenario("Scenario_" + key + "_Thanks",
                (speaker, "아이고, 고마워라! 젊은 사람이 참 착하네. 이거 얼마 안 되지만 받아 가게."));

            Material highlight = GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            Material boxMat = GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);

            ErrandNpc npc = go.AddComponent<ErrandNpc>();
            GreyboxStageBuilder.SetReference(npc, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(npc, "_askScenario", ask);
            GreyboxStageBuilder.SetReference(npc, "_progressScenario", progress);
            GreyboxStageBuilder.SetReference(npc, "_thanksScenario", thanks);
            GreyboxStageBuilder.SetReference(npc, "_boxHighlight", highlight);
            GreyboxStageBuilder.SetReference(npc, "_boxNormal", boxMat);
            GreyboxStageBuilder.SetReference(npc, "_highlightRenderer", body);
            GreyboxStageBuilder.SetReference(npc, "_normalMaterial", body.sharedMaterial);
            GreyboxStageBuilder.SetReference(npc, "_highlightMaterial", highlight);
            SerializedObject serialized = new SerializedObject(npc);
            serialized.FindProperty("_targetPosition").vector3Value = targetPosition;
            serialized.FindProperty("_reward").intValue = reward;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>행인 1명 — 위치·색·배회 반경. signal을 주면 교차 도로 신호를 지킨다 (S-076 ②).
        /// npcId·gameState를 주면 E 인사로 소셜앱에 등재된다 (S-080 ①).</summary>
        internal static void BuildPedestrian(string name, Vector3 position, Color color, float patrolHalf,
            TrafficLight signal = null, float roadX = 0f, string npcId = null, GameStateSO gameState = null)
        {
            var (go, bodyRenderer) = BuildFigure(name, position, "NpcWalker_" + name, color, 1.7f);

            // S-076 ② — 차 피격 감지용 몸통 트리거 + 키네마틱 RB (플레이어는 통과 유지 — 트리거라 밀지 않음).
            BoxCollider hitBox = go.AddComponent<BoxCollider>();
            hitBox.isTrigger = true;
            hitBox.center = new Vector3(0f, 0.85f, 0f);
            hitBox.size = new Vector3(0.5f, 1.7f, 0.5f);
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            PedestrianNpc npc = go.AddComponent<PedestrianNpc>();
            SerializedObject serialized = new SerializedObject(npc);
            serialized.FindProperty("_patrolHalf").floatValue = patrolHalf;
            if (signal != null)
            {
                serialized.FindProperty("_signal").objectReferenceValue = signal;
                serialized.FindProperty("_roadX").floatValue = roadX;
            }
            // S-080 ① — 인사 인터랙션·소셜 등재 배선.
            if (!string.IsNullOrEmpty(npcId)) serialized.FindProperty("_npcId").stringValue = npcId;
            if (gameState != null) serialized.FindProperty("_gameState").objectReferenceValue = gameState;
            serialized.FindProperty("_bodyRenderer").objectReferenceValue = bodyRenderer;
            serialized.FindProperty("_highlightMaterial").objectReferenceValue =
                GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AttachNameLabel(go, npcId, name); // S-120 — 근접 이름표
        }

        /// <summary>S-120 — 근접 이름표 부착: 이름은 NpcSO(Data/Npcs/npc_&lt;id&gt;)의 displayName,
        /// 프로필이 없으면 fallback. 표시·추종은 NpcNameLabel(SetHighlight 연동)이 담당.</summary>
        internal static void AttachNameLabel(GameObject go, string npcId, string fallback)
        {
            string displayName = fallback;
            if (!string.IsNullOrEmpty(npcId))
            {
                NpcSO profile = UnityEditor.AssetDatabase.LoadAssetAtPath<NpcSO>("Assets/Data/Npcs/npc_" + npcId + ".asset");
                if (profile != null && !string.IsNullOrEmpty(profile.displayName)) displayName = profile.displayName;
            }
            NpcNameLabel label = go.GetComponent<NpcNameLabel>();
            if (label == null) label = go.AddComponent<NpcNameLabel>();
            var serialized = new SerializedObject(label);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_npcId").stringValue = npcId ?? string.Empty; // S-124 — 호감도 표시
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // 에셋 참조는 리플렉션 직접 주입 — SerializedObject 경유는 SaveScene 시 유실된다(2026-07-20 실측).
            GreyboxStageBuilder.SetReference(label, "_gameState",
                UnityEditor.AssetDatabase.LoadAssetAtPath<GameStateSO>("Assets/Data/GameState.asset"));
            GreyboxStageBuilder.SetReference(label, "_backgroundSprite", LoadNpcInfoSprite());
            GreyboxStageBuilder.SetReference(label, "_hintFont",
                AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/UI/Fonts/Ramche SDF.asset"));
        }

        private static Sprite LoadNpcInfoSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(NPC_INFO_SPRITE_PATH) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.maxTextureSize < 2048))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(NPC_INFO_SPRITE_PATH);
        }
    }
}
