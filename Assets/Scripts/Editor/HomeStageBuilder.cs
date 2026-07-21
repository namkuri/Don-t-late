using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// Home.unity(집 — 기상·휴식)에 방 그레이박스 무대를 조립하는 개발 도구 (D-052 존치 확정).
    /// 조작 없는 연출 씬 — 방 내부(바닥·벽·침대·창문·문)와 카메라만 깐다.
    /// 진행은 기존 하단 AdvanceButton(SceneFlowUIBuilder) 그대로. 멱등(__gb_ Clear 재사용).
    /// </summary>
    public static class HomeStageBuilder
    {
        private const string HOME_PATH = "Assets/Scenes/Home.unity";

        [MenuItem("DontLate/Build Home Stage", priority = 14)]
        public static void BuildHomeStage()
        {
            Scene scene = EditorSceneManager.OpenScene(HOME_PATH, OpenSceneMode.Single);
            GreyboxStageBuilder.Clear();

            Material floor = GreyboxStageBuilder.GetOrCreateMaterial("HomeFloor", new Color(0.42f, 0.35f, 0.27f), false);
            Material wall = GreyboxStageBuilder.GetOrCreateMaterial("HomeWall", new Color(0.55f, 0.52f, 0.46f), false);
            Material bed = GreyboxStageBuilder.GetOrCreateMaterial("HomeBed", new Color(0.30f, 0.42f, 0.55f), false);
            Material window = GreyboxStageBuilder.GetOrCreateMaterial("HomeWindow", new Color(0.75f, 0.85f, 0.95f), true);
            Material door = GreyboxStageBuilder.GetOrCreateMaterial("Door", new Color(0.45f, 0.38f, 0.32f), false);

            BuildRoom(floor, wall, window, door);
            BuildBed(bed);
            GreyboxStageBuilder.BuildPostVolume();
            GreyboxStageBuilder.ConfigureCamera();
            PullCameraIntoRoom();

            EditorSceneManager.SaveScene(scene, HOME_PATH);
            Debug.Log("[Home] 방 무대 조립 완료 — 연출 전용(조작 없음), 진행은 하단 버튼.");
        }

        private static void BuildRoom(Material floor, Material wall, Material window, Material door)
        {
            GameObject floorGo = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "HomeFloor", new Vector3(0f, -0.05f, 0f));
            floorGo.transform.localScale = new Vector3(8f, 0.1f, 6f);
            floorGo.GetComponent<Renderer>().sharedMaterial = floor;

            // 뒷벽(카메라 반대편)과 좌우벽 — 앞면은 카메라를 위해 연다.
            AddWall("WallBack", new Vector3(0f, 1.5f, 3f), new Vector3(8f, 3f, 0.15f), wall);
            AddWall("WallLeft", new Vector3(-4f, 1.5f, 0f), new Vector3(0.15f, 3f, 6f), wall);
            AddWall("WallRight", new Vector3(4f, 1.5f, 0f), new Vector3(0.15f, 3f, 6f), wall);

            // 창문 — 뒷벽에 밝은 이미시브 판(아침 햇살 그림).
            GameObject windowGo = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Quad, "HomeWindow", new Vector3(1.6f, 1.7f, 2.9f));
            Object.DestroyImmediate(windowGo.GetComponent<Collider>());
            windowGo.transform.localScale = new Vector3(1.4f, 1.1f, 1f);
            windowGo.GetComponent<Renderer>().sharedMaterial = window;

            // 현관문 — 우측 벽.
            GameObject doorGo = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, "HomeDoor", new Vector3(3.9f, 1f, -1.2f));
            Object.DestroyImmediate(doorGo.GetComponent<Collider>());
            doorGo.transform.localScale = new Vector3(0.12f, 2f, 1f);
            doorGo.GetComponent<Renderer>().sharedMaterial = door;
        }

        private static void AddWall(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GreyboxStageBuilder.CreatePrimitive(PrimitiveType.Cube, name, position);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void BuildBed(Material material)
        {
            GameObject root = GreyboxStageBuilder.CreateEmpty("Bed", new Vector3(-2.4f, 0f, 1.8f));

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            frame.transform.localScale = new Vector3(2.2f, 0.5f, 1.4f);
            Object.DestroyImmediate(frame.GetComponent<BoxCollider>());
            frame.GetComponent<Renderer>().sharedMaterial = material;

            GameObject pillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillow.name = "Pillow";
            pillow.transform.SetParent(root.transform, false);
            pillow.transform.localPosition = new Vector3(-0.8f, 0.58f, 0f);
            pillow.transform.localScale = new Vector3(0.5f, 0.16f, 0.7f);
            Object.DestroyImmediate(pillow.GetComponent<BoxCollider>());
        }

        // 방은 거리 무대보다 훨씬 작다 — 표준 리그(FOV 22·y8.1·z-40)로는 방이 점이 된다.
        private static void PullCameraIntoRoom()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.transform.position = new Vector3(0f, 2.2f, -7.5f);
            camera.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        }
    }
}
