using TMPro;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 씬 가장자리 도보 게이트 조립 키트 (S-054b — 엣지 워크).
    /// 반투명 기둥 존 + 표지판(3D TMP)으로 "밟으면 걸어서 이동"을 시각화한다.
    /// </summary>
    internal static class EdgeGateBuildKit
    {
        private const string FONT_PATH = "Assets/Art/UI/Fonts/Pretendard-Regular SDF.asset";

        /// <summary>도보 게이트 1기 — direction: Next=시안(개척 방향) / Prev=앰버(되돌아가기).</summary>
        internal static void BuildGate(string name, Vector3 position, DistrictEdgeGate.Direction direction,
            GameStateSO gameState, float walkMinutes = 40f, float zoneDepth = 6f)
        {
            bool next = direction == DistrictEdgeGate.Direction.Next;
            Color tint = next ? new Color(0.208f, 0.878f, 0.784f, 0.35f) : new Color(1f, 0.624f, 0.271f, 0.35f);

            GameObject root = GreyboxStageBuilder.CreateEmpty(name, position);

            // 존 비주얼 — 반투명 기둥 (콜라이더는 루트 트리거).
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Zone";
            Object.DestroyImmediate(pillar.GetComponent<Collider>());
            pillar.transform.SetParent(root.transform, false);
            pillar.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            pillar.transform.localScale = new Vector3(0.5f, 2.8f, zoneDepth);
            Material zoneMat = GreyboxStageBuilder.GetOrCreateMaterial(
                next ? "EdgeGateNext" : "EdgeGatePrev",
                new Color(tint.r, tint.g, tint.b, 1f), true);
            pillar.GetComponent<Renderer>().sharedMaterial = zoneMat;

            // 표지판 — 3D TMP 라벨 (카메라 쪽 -Z를 본다).
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font != null)
            {
                GameObject signGo = new GameObject("Sign");
                signGo.transform.SetParent(root.transform, false);
                signGo.transform.localPosition = new Vector3(0f, 3.3f, 0f); // TMP 3D 기본면이 카메라(-Z) 정면 — 회전 불요
                TextMeshPro sign = signGo.AddComponent<TextMeshPro>();
                sign.font = font;
                sign.fontSize = 8f;
                sign.alignment = TextAlignmentOptions.Center;
                sign.text = next ? "다음 동네 →\n<size=60%>밟으면 걸어간다</size>"
                                 : "← 이전 동네\n<size=60%>밟으면 걸어간다</size>";
                sign.color = next ? new Color(0.208f, 0.878f, 0.784f) : new Color(1f, 0.624f, 0.271f);
                sign.rectTransform.sizeDelta = new Vector2(8f, 2.4f);
            }

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.4f, 0f);
            trigger.size = new Vector3(1.2f, 2.8f, zoneDepth);

            DistrictEdgeGate gate = root.AddComponent<DistrictEdgeGate>();
            GreyboxStageBuilder.SetReference(gate, "_gameState", gameState);
            SerializedObject serialized = new SerializedObject(gate);
            serialized.FindProperty("_direction").enumValueIndex = (int)direction;
            serialized.FindProperty("_walkMinutes").floatValue = walkMinutes;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
