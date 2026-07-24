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

        /// <summary>행인 1명 — 위치·색·배회 반경.</summary>
        internal static void BuildPedestrian(string name, Vector3 position, Color color, float patrolHalf)
        {
            var (go, _) = BuildFigure(name, position, "NpcWalker_" + name, color, 1.7f);
            PedestrianNpc npc = go.AddComponent<PedestrianNpc>();
            SerializedObject serialized = new SerializedObject(npc);
            serialized.FindProperty("_patrolHalf").floatValue = patrolHalf;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
