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
        private const int LOAD_ZONE_COUNT = 3;

        [MenuItem("DontLate/Build Camp Stage", priority = 13)]
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
            BuildTruck(truck, box, highlight);
            BuildPickupBoxes(box, highlight);
            BuildDrink(drink, highlight);
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();

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
        private static void BuildPickupBoxes(Material material, Material highlight)
        {
            for (int i = 0; i < LOAD_ZONE_COUNT; i++)
            {
                GameObject boxGo = GreyboxStageBuilder.CreatePrimitive(
                    PrimitiveType.Cube, "CampBox_" + (i + 1).ToString("00"),
                    new Vector3(-7f + (i % 2) * 1f, 0.4f + (i / 2) * 0.85f, 1.5f + i * 0.15f));
                boxGo.transform.localScale = Vector3.one * 0.8f;
                boxGo.GetComponent<BoxCollider>().isTrigger = true;
                boxGo.GetComponent<Renderer>().sharedMaterial = material;

                PickupBox pickup = boxGo.AddComponent<PickupBox>();
                GreyboxStageBuilder.SetReference(pickup, "_order", GetOrCreateCampOrder(i));
                GreyboxStageBuilder.SetReference(pickup, "_renderer", boxGo.GetComponent<Renderer>());
                GreyboxStageBuilder.SetReference(pickup, "_normalMaterial", material);
                GreyboxStageBuilder.SetReference(pickup, "_highlightMaterial", highlight);

                // 상차 절차(S-011): 폰으로 바코드를 찍은 짐만 들 수 있다.
                var serialized = new UnityEditor.SerializedObject(pickup);
                serialized.FindProperty("_requireScanned").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 패드별 배송 건. 1번은 그레이박스 기존 건(행복빌라)을 재사용해 District 무대와 이어진다.
        private static DeliveryOrderSO GetOrCreateCampOrder(int index)
        {
            if (index == 0)
                return AssetDatabase.LoadAssetAtPath<DeliveryOrderSO>("Assets/Data/Order_HappyVilla.asset");

            string path = "Assets/Data/Order_Camp" + (index + 1).ToString("00") + ".asset";
            DeliveryOrderSO order = AssetDatabase.LoadAssetAtPath<DeliveryOrderSO>(path);
            if (order != null) return order;

            order = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            order.orderId = 100 + index;
            order.address = index == 1 ? "청운상가 2층" : "달빛맨션 502호";
            order.floor = index == 1 ? 2 : 5;
            order.deadlineMinuteOfDay = index == 1 ? 15f * 60f : 19f * 60f;
            order.reward = index == 1 ? 900 : 1400;
            AssetDatabase.CreateAsset(order, path);
            AssetDatabase.SaveAssets();
            return order;
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
