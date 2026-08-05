using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 씬 가장자리 도보 게이트 조립 키트 (S-054b → S-062 개편).
    /// 반투명 기둥 존 + 둥둥 뜨는 방향 화살표(텍스트 없음) + 미해금 물리 벽.
    /// </summary>
    internal static class EdgeGateBuildKit
    {
        /// <summary>
        /// 도보 게이트 1기. 화살표는 씬 바깥 방향을 가리킨다.
        /// S-174 ⑤ — 색은 **좌우 동일**(앰버 #ff9f45). 종전엔 Next=시안/Prev=앰버로 갈라 놨는데,
        /// 색이 다르면 "다른 종류의 문"으로 읽힌다 — 둘 다 그냥 옆 동네로 걸어가는 길이다(남규님 지시).
        /// <paramref name="showArrow"/>가 false면 표식 없이 통로만 만든다(같은 자리에 게이트가
        /// 둘 겹치는 아파트 마당용 — 화살표가 겹쳐 보인다).
        /// </summary>
        internal static void BuildGate(string name, Vector3 position, DistrictEdgeGate.Direction direction,
            GameStateSO gameState, float zoneDepth = 6f, bool showArrow = true)
        {
            bool next = direction == DistrictEdgeGate.Direction.Next;
            Color color = new Color(1f, 0.624f, 0.271f);
            Material gateMat = GreyboxStageBuilder.GetOrCreateMaterial("EdgeGate", color, true);

            GameObject root = GreyboxStageBuilder.CreateEmpty(name, position);

            // 존 비주얼 — 반투명 기둥 (콜라이더는 루트 트리거).
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Zone";
            Object.DestroyImmediate(pillar.GetComponent<Collider>());
            pillar.transform.SetParent(root.transform, false);
            pillar.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            pillar.transform.localScale = new Vector3(0.5f, 2.8f, zoneDepth);
            // S-122 ⑪ — 렌더 OFF: 거리 한복판의 발광 반투명 "상자"로 읽혀 플레이스홀더처럼 보인다
            // (남규님 실관찰). 표식은 둥둥 뜨는 화살표가 전담한다. GO·머티리얼·치수는 남겨
            // 되켜기만 하면 원복되고, 통과 판정 트리거는 루트 소유라 무관하다.
            Renderer pillarRenderer = pillar.GetComponent<Renderer>();
            pillarRenderer.sharedMaterial = gateMat;
            pillarRenderer.enabled = false;

            // S-062 ③ — 둥둥 뜨는 방향 화살표 (셰브런 2장 + 축) — 씬 바깥쪽을 가리킨다.
            if (showArrow)
            {
                float outward = position.x >= 0f ? 1f : -1f;
                GameObject arrow = new GameObject("Arrow");
                arrow.transform.SetParent(root.transform, false);
                arrow.transform.localPosition = new Vector3(0f, 3.2f, 0f);
                BuildArrowMesh(arrow.transform, gateMat, outward);
                FloatingArrow bob = arrow.AddComponent<FloatingArrow>();
                SerializedObject bobSerialized = new SerializedObject(bob);
                bobSerialized.FindProperty("_pushDirection").vector3Value = new Vector3(outward, 0f, 0f);
                bobSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            // S-062 ④ — 미해금 물리 벽 (게이트가 잠김 판정 시 활성화).
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "LockWall";
            wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            wall.transform.localScale = new Vector3(0.6f, 3f, zoneDepth);
            wall.GetComponent<Renderer>().enabled = false; // 보이지 않는 벽 — 기둥 색이 잠김을 말해줄 몫은 추후
            wall.SetActive(false);

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.4f, 0f);
            trigger.size = new Vector3(1.2f, 2.8f, zoneDepth);

            DistrictEdgeGate gate = root.AddComponent<DistrictEdgeGate>();
            GreyboxStageBuilder.SetReference(gate, "_gameState", gameState);
            GreyboxStageBuilder.SetReference(gate, "_lockWall", wall);
            SerializedObject serialized = new SerializedObject(gate);
            serialized.FindProperty("_direction").enumValueIndex = (int)direction;
            // S-075 3 - _walkMinutes 은퇴 (엣지 워크 시간 소모 폐지).
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // 화살표 = 축 1 + 셰브런 2 (±45°) — outward 방향을 가리키는 납작 큐브 구성.
        private static void BuildArrowMesh(Transform parent, Material material, float outward)
        {
            void Piece(string pieceName, Vector3 localPos, Vector3 scale, float zAngle)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = pieceName;
                Object.DestroyImmediate(piece.GetComponent<Collider>());
                piece.transform.SetParent(parent, false);
                piece.transform.localPosition = localPos;
                piece.transform.localScale = scale;
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
                piece.GetComponent<Renderer>().sharedMaterial = material;
            }

            Piece("Shaft", Vector3.zero, new Vector3(1.1f, 0.22f, 0.22f), 0f);
            Piece("ChevronUp", new Vector3(outward * 0.55f, 0.22f, 0f), new Vector3(0.62f, 0.2f, 0.22f), outward * -45f);
            Piece("ChevronDown", new Vector3(outward * 0.55f, -0.22f, 0f), new Vector3(0.62f, 0.2f, 0.22f), outward * 45f);
        }
    }
}
