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
            Material pad = GreyboxStageBuilder.GetOrCreateMaterial("LoadPad", new Color(0.13f, 0.55f, 0.49f), false);

            Material highlight = GreyboxStageBuilder.GetOrCreateMaterial("Highlight", GreyboxStageBuilder.ParseColor("#35e0c8"), true);
            Material drink = GreyboxStageBuilder.GetOrCreateMaterial("Drink", GreyboxStageBuilder.ParseColor("#e04a35"), false);

            GreyboxStageBuilder.BuildGround(ground, lane);
            GreyboxStageBuilder.BuildWalkableVolume();
            GreyboxStageBuilder.BuildGroundMist();
            BuildTruck(truck);
            BuildLoadZonePads(pad, highlight);
            BuildBoxPile(box);
            BuildDrink(drink, highlight);
            GreyboxStageBuilder.BuildPlayer(gameState, tuning);
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();

            EditorSceneManager.SaveScene(scene, CAMP_PATH);
            Debug.Log("[Camp] 무대 조립 완료 — 적재존 패드 " + LOAD_ZONE_COUNT
                    + "개(__gb_LoadZone_XX). LoadingZone.cs 도착 시 패드에 부착.");
        }

        // 트럭 = 연출 소품(ARCHITECTURE §4 — 주행 없음). 적재함+캡+바퀴 큐브 조합, 우측 도로변.
        private static void BuildTruck(Material material)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("Truck", new Vector3(9f, 0f, 1.8f));

            AddPart(root, "Cargo", new Vector3(-0.8f, 1.5f, 0f), new Vector3(4.0f, 2.2f, 2.0f), material);
            AddPart(root, "Cab", new Vector3(2.2f, 0.95f, 0f), new Vector3(1.6f, 1.5f, 1.9f), material);
            AddPart(root, "WheelF", new Vector3(2.2f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);
            AddPart(root, "WheelB", new Vector3(-1.6f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 2.1f), material);
        }

        private static void AddPart(GameObject root, string name, Vector3 localPos, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = size;
            Object.DestroyImmediate(part.GetComponent<BoxCollider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        // 적재존 패드 — 패드당 주문 1건, E로 적재(LoadingZone — S-005).
        private static void BuildLoadZonePads(Material material, Material highlight)
        {
            for (int i = 0; i < LOAD_ZONE_COUNT; i++)
            {
                GameObject padGo = GreyboxStageBuilder.CreatePrimitive(
                    PrimitiveType.Cube, "LoadZone_" + (i + 1).ToString("00"),
                    new Vector3(-2f + i * 2f, 0.05f, 0f));
                padGo.transform.localScale = new Vector3(1.2f, 0.06f, 1.2f);
                padGo.GetComponent<BoxCollider>().isTrigger = true;
                padGo.GetComponent<Renderer>().sharedMaterial = material;

                LoadingZone zone = padGo.AddComponent<LoadingZone>();
                GreyboxStageBuilder.SetReference(zone, "_order", GetOrCreateCampOrder(i));
                GreyboxStageBuilder.SetReference(zone, "_renderer", padGo.GetComponent<Renderer>());
                GreyboxStageBuilder.SetReference(zone, "_normalMaterial", material);
                GreyboxStageBuilder.SetReference(zone, "_highlightMaterial", highlight);
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

        // 대기 물량 더미 — 순수 배경(상호작용 없음. 적재는 LoadingZone 몫).
        private static void BuildBoxPile(Material material)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("BoxPile", new Vector3(-7f, 0f, 1.5f));

            AddPart(root, "Box1", new Vector3(0f, 0.4f, 0f), Vector3.one * 0.8f, material);
            AddPart(root, "Box2", new Vector3(0.9f, 0.4f, 0.2f), Vector3.one * 0.8f, material);
            AddPart(root, "Box3", new Vector3(0.45f, 1.2f, 0.1f), Vector3.one * 0.8f, material);
            AddPart(root, "Box4", new Vector3(1.8f, 0.35f, -0.1f), Vector3.one * 0.7f, material);
        }
    }
}
