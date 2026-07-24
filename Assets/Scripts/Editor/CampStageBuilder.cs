using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Camp.unity(물류캠프)에 짐싣기 그레이박스 무대를 조립하는 개발 도구 — S-008.
    /// 매니저는 Core 씬 상주이므로 만들지 않는다. GreyboxStageBuilder의 조립 헬퍼를 재사용해
    /// 지면·트럭 소품·적재존 패드 3개·박스 더미·플레이어·카메라만 깐다.
    /// LoadingZone.cs(S-005 납품 대기)가 도착하면 __gb_LoadZone_01~03에 부착한다.
    /// 다시 실행하면 이전 조립물(__gb_ 루트)을 지우고 새로 만든다(멱등).
    /// </summary>
    public static class CampStageBuilder
    {
        private const string CAMP_PATH = "Assets/Scenes/Camp.unity";
        private const int LOAD_ZONE_COUNT = 4; // S-039 ④ — 4번째 = 아파트행 물량

        [MenuItem("DontLate/Build/Camp Stage", priority = 12)]
        public static void BuildCampStage()
        {
            Scene scene = EditorSceneManager.OpenScene(CAMP_PATH, OpenSceneMode.Single);
            GreyboxStageBuilder.Clear();

            var (gameState, tuning, _) = GreyboxStageBuilder.GetOrCreateStageData();

            Material ground = GreyboxStageBuilder.GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material lane = GreyboxStageBuilder.GetOrCreateMaterial("Lane", new Color(0.34f, 0.33f, 0.30f), false);
            Material box = GreyboxStageBuilder.GetOrCreateMaterial("Box", GreyboxStageBuilder.ParseColor("#ff9f45"), false);
            Material truck = GreyboxStageBuilder.GetOrCreateMaterial("Truck", new Color(0.30f, 0.42f, 0.55f), false);

            Material highlight = GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            Material drink = GreyboxStageBuilder.GetOrCreateMaterial("Drink", GreyboxStageBuilder.ParseColor("#e04a35"), false);

            GreyboxStageBuilder.BuildGround(ground, lane);
            GreyboxStageBuilder.BuildWalkableVolume();
            GreyboxStageBuilder.BuildGroundMist();
            GreyboxStageBuilder.BuildStarField(); // S-033 ① — 캠프 밤하늘 별 (밤 페이드는 StarField.cs 공용)
            GreyboxStageBuilder.BuildDeliveryCart(new Vector3(-4f, 0f, 1.2f)); // S-039 ④ — 캠프에서도 대차 운반
            BuildTruck(truck, box, highlight);
            System.Collections.Generic.List<PickupBox> boxes = BuildPickupBoxes(box, highlight, tuning);
            BuildOrderBoard(gameState, boxes);
            BuildDrink(drink, highlight);
            BuildVendingMachine(tuning, drink, highlight);
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            GreyboxStageBuilder.AttachCameraFollow();

            EditorSceneManager.SaveScene(scene, CAMP_PATH);
            Debug.Log("[Camp] 무대 조립 완료 — 박스 " + LOAD_ZONE_COUNT
                    + "개를 E로 들어 트럭 짐칸 뒤에서 E로 싣는다 (S-009).");
        }

        // 트럭 = 소품 + 적재존(S-009). 짐칸 뒤에서 박스를 든 채 E → LoadingZone이 짐칸에 쌓는다.
        private static void BuildTruck(Material material, Material boxMaterial, Material highlight)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("Truck", new Vector3(9f, 0f, 1.8f));

            GameObject cargo = AddPart(root, "Cargo", new Vector3(-0.8f, 1.5f, 0f), new Vector3(4.0f, 2.2f, 2.0f), material);
            AddPart(root, "Cab", new Vector3(2.2f, 0.95f, 0f), new Vector3(1.6f, 1.5f, 1.9f), material);
            AddPart(root, "WheelF", new Vector3(2.2f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);
            AddPart(root, "WheelB", new Vector3(-1.6f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);

            // 적재 감지 트리거 — 짐칸 뒤편(보도 쪽) 앞 공간.
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(-0.8f, 1f, -1.8f);
            trigger.size = new Vector3(4.2f, 2f, 1.8f);

            // 실린 상자가 쌓이는 짐칸 내부 앵커.
            GameObject stack = new GameObject("StackRoot");
            stack.transform.SetParent(root.transform, false);
            stack.transform.localPosition = new Vector3(-1.6f, 0.5f, 0f);

            LoadingZone zone = root.AddComponent<LoadingZone>();
            GreyboxStageBuilder.SetReference(zone, "_stackRoot", stack.transform);
            GreyboxStageBuilder.SetReference(zone, "_boxVisualPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Auto/prop_box_parcel.prefab"));
            GreyboxStageBuilder.SetReference(zone, "_boxMaterial", boxMaterial);
            GreyboxStageBuilder.SetReference(zone, "_renderer", cargo.GetComponent<Renderer>());
            GreyboxStageBuilder.SetReference(zone, "_normalMaterial", material);
            GreyboxStageBuilder.SetReference(zone, "_highlightMaterial", highlight);
        }

        private static GameObject AddPart(GameObject root, string name, Vector3 localPos, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = size;
            Object.DestroyImmediate(part.GetComponent<BoxCollider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        // 대기 물량 = 손에 집히는 박스(PickupBox) — 주문 1건씩. E로 들고 트럭으로 나른다 (S-009).
        private static System.Collections.Generic.List<PickupBox> BuildPickupBoxes(Material material, Material highlight, TuningConfigSO tuning)
        {
            var built = new System.Collections.Generic.List<PickupBox>();
            for (int i = 0; i < LOAD_ZONE_COUNT; i++)
            {
                // 피라미드 스택 — 콜라이더(0.7u)가 겹치면 스폰 순간 물리 밀어내기로 자폭한다 (S-019 실측).
                var (boxGo, _, _) = GreyboxStageBuilder.CreateParcelBox(
                    "CampBox_" + (i + 1).ToString("00"),
                    new Vector3(-7f + (i % 2) * 0.9f, (i / 2) * 0.72f, 1.5f), material,
                    physical: true); // 실물 스택 (S-016 ⑥) — 아래 상자를 빼면 위가 떨어진다

                BoxDurability durability = boxGo.AddComponent<BoxDurability>(); // 취급주의 (S-019 ①)
                GreyboxStageBuilder.SetReference(durability, "_tuning", tuning);

                PickupBox pickup = boxGo.AddComponent<PickupBox>();
                GreyboxStageBuilder.SetReference(pickup, "_order", GetOrCreateCampOrder(i));
                GreyboxStageBuilder.SetReference(pickup, "_highlightMaterial", highlight);

                // 상차 절차(S-011): 폰으로 바코드를 찍은 짐만 들 수 있다.
                var serialized = new UnityEditor.SerializedObject(pickup);
                serialized.FindProperty("_requireScanned").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                built.Add(pickup);
            }
            return built;
        }

        // 주문판 (S-021 ③) — 캠프 복귀 시 소진 주문을 새 목적지로 교체.
        private static void BuildOrderBoard(GameStateSO gameState, System.Collections.Generic.List<PickupBox> boxes)
        {
            GameObject go = GreyboxStageBuilder.CreateEmpty("OrderBoard", Vector3.zero);
            CampOrderBoard board = go.AddComponent<CampOrderBoard>();
            SerializedObject serialized = new SerializedObject(board);
            serialized.FindProperty("_gameState").objectReferenceValue = gameState;
            SerializedProperty prop = serialized.FindProperty("_boxes");
            prop.arraySize = boxes.Count;
            for (int i = 0; i < boxes.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = boxes[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // 패드별 배송 건. 1번은 그레이박스 기존 건(빌라촌)을 재사용해 District 무대와 이어진다.
        // S-035(D-064): 로드된 기존 에셋도 정본 값으로 덮는다 — district 문자열이 스폰 계약이라
        // 구 구역명("달빛맨션 구역" 등)이 남으면 스폰 0. 같은 값 재기록 = 멱등.
        private static DeliveryOrderSO GetOrCreateCampOrder(int index)
        {
            if (index == 0)
                return AssetDatabase.LoadAssetAtPath<DeliveryOrderSO>("Assets/Data/Order_HappyVilla.asset");

            string path = "Assets/Data/Order_Camp" + (index + 1).ToString("00") + ".asset";
            DeliveryOrderSO order = AssetDatabase.LoadAssetAtPath<DeliveryOrderSO>(path);
            bool created = order == null;
            if (created) order = ScriptableObject.CreateInstance<DeliveryOrderSO>();

            order.orderId = 100 + index;
            switch (index)
            {
                case 1:
                    order.address = "골목연립 반지하";
                    order.district = DeliveryOrderSO.DISTRICT_VILLATOWN;
                    order.floor = -1;
                    order.deadlineMinuteOfDay = 15f * 60f;
                    order.reward = 900;
                    break;
                case 3: // S-039 ④ — 첫날부터 아파트행 물량
                    order.address = "늦지마아파트 202호";
                    order.district = DeliveryOrderSO.DISTRICT_APARTMENT;
                    order.floor = 2;
                    order.deadlineMinuteOfDay = 18f * 60f;
                    order.reward = 1600;
                    break;
                default: // 먹자골목(19시)은 저녁 마감 — "밤 배송량↑" 표현 (D-064).
                    order.address = "달빛호프 2층";
                    order.district = DeliveryOrderSO.DISTRICT_FOODALLEY;
                    order.floor = 2;
                    order.deadlineMinuteOfDay = 19f * 60f;
                    order.reward = 1400;
                    break;
            }

            if (created)
            {
                AssetDatabase.CreateAsset(order, path);
                AssetDatabase.SaveAssets();
            }
            else
            {
                EditorUtility.SetDirty(order);
                AssetDatabase.SaveAssetIfDirty(order);
            }
            return order;
        }

        // 자판기 (S-019 ②) — E=결제 배출, 상자 투척 명중도 배출.
        private static void BuildVendingMachine(TuningConfigSO tuning, Material drinkMaterial, Material highlight)
        {
            Material body = GreyboxStageBuilder.GetOrCreateMaterial("Vending", new Color(0.85f, 0.3f, 0.3f), false);

            GameObject go = GreyboxStageBuilder.CreatePrimitive(
                PrimitiveType.Cube, "Vending", new Vector3(4.5f, 1.0f, 2.2f));
            go.transform.localScale = new Vector3(1.0f, 2.0f, 0.7f);
            // 실물 콜라이더(비트리거) — 상자 투척 충돌 감지 겸 벽 역할.

            VendingMachine vending = go.AddComponent<VendingMachine>();
            GreyboxStageBuilder.SetReference(vending, "_tuning", tuning);
            GreyboxStageBuilder.SetReference(vending, "_drinkMaterial", drinkMaterial);
            GreyboxStageBuilder.SetReference(vending, "_highlightMaterial", highlight);
            GreyboxStageBuilder.SetReference(vending, "_renderer", go.GetComponent<Renderer>());
        }

        // 에너지드링크 — E로 회복(EnergyDrinkPickup — S-005).
        private static void BuildDrink(Material material, Material highlight)
        {
            GameObject go = GreyboxStageBuilder.CreatePrimitive(
                PrimitiveType.Capsule, "Drink", new Vector3(4f, 0.25f, -1f));
            go.transform.localScale = new Vector3(0.22f, 0.25f, 0.22f);
            var collider = go.GetComponent<Collider>();
            collider.isTrigger = true;
            go.GetComponent<Renderer>().sharedMaterial = material;

            EnergyDrinkPickup pickup = go.AddComponent<EnergyDrinkPickup>();
            GreyboxStageBuilder.SetReference(pickup, "_renderer", go.GetComponent<Renderer>());
            GreyboxStageBuilder.SetReference(pickup, "_normalMaterial", material);
            GreyboxStageBuilder.SetReference(pickup, "_highlightMaterial", highlight);
        }

    }
}
