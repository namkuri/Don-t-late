using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 코어 루프 확인용 그레이박스 무대를 현재 씬에 조립한다.
    /// 씬 파일을 커밋하지 않고 팀 전원이 같은 무대를 재현하기 위한 개발 도구다.
    /// 생성물은 전부 "__gb_" 접두어를 달고, 다시 실행하면 지우고 새로 만든다(멱등).
    /// </summary>
    public static class GreyboxStageBuilder
    {
        private const string PREFIX = "__gb_";
        private const string DATA_ROOT = "Assets/Data";
        private const string GREYBOX_ROOT = "Assets/Data/Greybox";

        [MenuItem("DontLate/Build Greybox Stage", priority = 0)]
        public static void Build()
        {
            Clear();

            GameStateSO gameState = GetOrCreate<GameStateSO>(DATA_ROOT + "/GameState.asset", ConfigureGameState);
            TuningConfigSO tuning = GetOrCreate<TuningConfigSO>(DATA_ROOT + "/Tuning.asset", ConfigureTuning);
            DeliveryOrderSO order = GetOrCreate<DeliveryOrderSO>(DATA_ROOT + "/Order_HappyVilla.asset", ConfigureOrder);

            ResetSession(gameState, order);

            Material ground = GetOrCreateMaterial("Ground", new Color(0.24f, 0.24f, 0.26f), false);
            Material lane = GetOrCreateMaterial("Lane", new Color(0.34f, 0.33f, 0.30f), false);
            Material body = GetOrCreateMaterial("Player", new Color(0.88f, 0.88f, 0.90f), false);
            Material nose = GetOrCreateMaterial("Facing", new Color(0.95f, 0.35f, 0.35f), false);
            Material box = GetOrCreateMaterial("Box", ParseColor("#ff9f45"), false);
            Material door = GetOrCreateMaterial("Door", new Color(0.45f, 0.38f, 0.32f), false);
            Material highlight = GetOrCreateMaterial("Highlight", ParseColor("#35e0c8"), true);

            BuildGround(ground, lane);
            BuildManagers(gameState, tuning);
            BuildWalkableVolume();
            BuildPickupBox(order, box, highlight);
            BuildDeliveryPoint(order, door, highlight);
            BuildPlayer(gameState, tuning, body, nose);
            ConfigureCamera();

            Debug.Log("[Greybox] 무대 조립 완료 — Play를 누르면 WASD 이동 / E 상호작용. "
                    + "적재 1건(#" + order.orderId + ") 마감 " + (order.deadlineMinuteOfDay / 60f).ToString("0.0") + "시.");
        }

        [MenuItem("DontLate/Clear Greybox Stage", priority = 1)]
        public static void Clear()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null || !go.name.StartsWith(PREFIX)) continue;
                if (go.transform.parent != null) continue;
                Undo.DestroyObjectImmediate(go);
            }
        }

        // ── 무대 구성 ────────────────────────────────────────

        private static void BuildGround(Material groundMaterial, Material laneMaterial)
        {
            GameObject ground = CreatePrimitive(PrimitiveType.Plane, "Ground", Vector3.zero);
            ground.transform.localScale = new Vector3(12f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject lane = CreatePrimitive(PrimitiveType.Cube, "Lane", new Vector3(0f, 0.02f, 0f));
            Object.DestroyImmediate(lane.GetComponent<BoxCollider>());
            lane.transform.localScale = new Vector3(40f, 0.04f, 6f);
            lane.GetComponent<Renderer>().sharedMaterial = laneMaterial;
        }

        private static void BuildManagers(GameStateSO gameState, TuningConfigSO tuning)
        {
            GameObject managers = CreateEmpty("Managers", Vector3.zero);

            WorldDeliveryManager delivery = managers.AddComponent<WorldDeliveryManager>();
            SetReference(delivery, "_gameState", gameState);

            WorldDeadlineManager deadline = managers.AddComponent<WorldDeadlineManager>();
            SetReference(deadline, "_gameState", gameState);
            SetReference(deadline, "_tuning", tuning);

            WorldDayNightManager dayNight = managers.AddComponent<WorldDayNightManager>();
            SetReference(dayNight, "_gameState", gameState);
            SetReference(dayNight, "_tuning", tuning);
        }

        private static void BuildWalkableVolume()
        {
            GameObject volume = CreateEmpty("Walkable", Vector3.zero);
            BoxCollider collider = volume.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(40f, 4f, 6f);
            collider.center = new Vector3(0f, 2f, 0f);
            volume.AddComponent<WalkableVolume>();
        }

        private static void BuildPickupBox(DeliveryOrderSO order, Material normal, Material highlight)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Cube, "Box", new Vector3(-5f, 0.4f, 0f));
            go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            go.GetComponent<BoxCollider>().isTrigger = true;

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = normal;

            PickupBox pickup = go.AddComponent<PickupBox>();
            SetReference(pickup, "_order", order);
            SetReference(pickup, "_renderer", renderer);
            SetReference(pickup, "_normalMaterial", normal);
            SetReference(pickup, "_highlightMaterial", highlight);
        }

        private static void BuildDeliveryPoint(DeliveryOrderSO order, Material normal, Material highlight)
        {
            GameObject go = CreatePrimitive(PrimitiveType.Cube, "Door", new Vector3(6f, 1f, 2.6f));
            go.transform.localScale = new Vector3(1f, 2f, 0.2f);
            go.GetComponent<BoxCollider>().isTrigger = true;

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = normal;

            DeliveryPoint point = go.AddComponent<DeliveryPoint>();
            SetReference(point, "_expectedOrder", order);
            SetReference(point, "_renderer", renderer);
            SetReference(point, "_normalMaterial", normal);
            SetReference(point, "_highlightMaterial", highlight);
        }

        private static void BuildPlayer(GameStateSO gameState, TuningConfigSO tuning, Material bodyMaterial, Material noseMaterial)
        {
            GameObject player = CreateEmpty("Player", new Vector3(0f, 0.1f, 0f));
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = PREFIX + "Body";
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = PREFIX + "Facing";
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.25f, 0.42f);
            nose.transform.localScale = new Vector3(0.18f, 0.18f, 0.45f);
            nose.GetComponent<Renderer>().sharedMaterial = noseMaterial;

            PlayerManager hub = player.AddComponent<PlayerManager>();
            player.AddComponent<PlayerAnimationManager>();
            SetReference(hub, "_tuning", tuning);
            SetReference(hub, "_gameState", gameState);

            // 든 상자가 붙는 자리 — 가슴 높이 앞쪽.
            GameObject carryAnchor = new GameObject(PREFIX + "CarryAnchor");
            carryAnchor.transform.SetParent(player.transform, false);
            carryAnchor.transform.localPosition = new Vector3(0f, 1.05f, 0.45f);
            SetReference(player.GetComponent<PlayerStatusManager>(), "_carryAnchor", carryAnchor.transform);

            GameObject sensor = new GameObject(PREFIX + "Sensor");
            sensor.transform.SetParent(player.transform, false);
            sensor.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            sensor.AddComponent<InteractionSensor>();
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            Undo.RecordObject(camera, "Greybox Camera");
            Undo.RecordObject(camera.transform, "Greybox Camera");
            camera.orthographic = false;
            camera.fieldOfView = 22f;
            camera.transform.position = new Vector3(0f, 8.1f, -40.4f);
            camera.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
        }

        // ── 데이터 ───────────────────────────────────────────

        private static void ConfigureGameState(GameStateSO state)
        {
            state.startDay = 1;
            state.startMinuteOfDay = 8f * 60f;
            state.startMoney = 0;
            state.startDebt = 10000;
        }

        private static void ConfigureTuning(TuningConfigSO tuning)
        {
            tuning.gameMinutesPerRealSecond = 2f;
        }

        private static void ConfigureOrder(DeliveryOrderSO order)
        {
            order.orderId = 7;
            order.address = "행복빌라 301호";
            order.floor = 3;
            order.deadlineMinuteOfDay = 10f * 60f;
            order.reward = 5000;
            order.memo = "그레이박스 확인용";
        }

        /// <summary>메뉴를 다시 누르면 적재·정산이 초기 상태로 돌아온다.</summary>
        private static void ResetSession(GameStateSO state, DeliveryOrderSO order)
        {
            state.day = state.startDay;
            state.minuteOfDay = state.startMinuteOfDay;
            state.money = state.startMoney;
            state.debt = state.startDebt;
            state.completedCount = 0;
            state.lateCount = 0;
            state.cargo.Clear();
            state.cargo.Add(order);
            EditorUtility.SetDirty(state);
            AssetDatabase.SaveAssetIfDirty(state);
        }

        // ── 헬퍼 ─────────────────────────────────────────────

        private static GameObject CreateEmpty(string name, Vector3 position)
        {
            GameObject go = new GameObject(PREFIX + name);
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");
            return go;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = PREFIX + name;
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, "Build Greybox Stage");
            return go;
        }

        private static void SetReference(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrCreate<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(DATA_ROOT);
            asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static Material GetOrCreateMaterial(string name, Color color, bool emissive)
        {
            string path = GREYBOX_ROOT + "/GB_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(DATA_ROOT);
            EnsureFolder(GREYBOX_ROOT);

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        private static Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
