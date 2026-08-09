using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-212 — 빌라촌 상주 NPC 3인(박말순·나아라·오지혜)을 **빌더 산출물로** 세운다.
    ///
    /// 왜 이 파일이 생겼나: 민지님이 PR#57에서 이 셋을 `Village.unity` 본문에 손으로 배치했다.
    /// 그런데 씬 본문은 커밋하지 않는 것이 이 프로젝트의 규칙이고(D-061 — 빌더가 정본), 그래서
    /// 반입할 때 배치가 통째로 떨어져 나갔다("다 없어짐" — 남규님 관찰). 규칙을 어겨 씬을 받는 대신
    /// **배치를 코드로 옮긴다** — 그러면 어느 PC에서든 메뉴 한 번으로 같은 거리가 재현된다.
    ///
    /// 수치는 전부 민지님 씬 YAML에서 뽑은 실측값이다(위치·회전·스케일·클립·지속시간·대사 풀).
    /// 눈으로 맞춘 배치라 반올림하지 않고 그대로 옮겼다 — 고치는 건 민지님 몫이다.
    /// </summary>
    internal static class VillageCastBuilder
    {
        private const string ANIM_ROOT = "VillageNpcAnimations";
        private const string NPC_INFO_SPRITE = "Assets/Art/UI/npc_info.png";

        /// <summary>NPC 1인 — 모델·자세·번갈아 재생할 두 동작·대사 풀.</summary>
        private readonly struct CastMember
        {
            public readonly string Name;
            public readonly string ModelPath;
            public readonly Vector3 Position;
            public readonly float RotationY;
            public readonly float Scale;
            public readonly string FirstClipPath;
            public readonly string SecondClipPath;
            public readonly float FirstDuration;
            public readonly float SecondDuration;
            public readonly bool UsePedestrianMovement;
            public readonly string TalkPoolPath;
            /// <summary>씬에서 갈아 끼운 머티리얼. 비면 FBX 임베디드 머티리얼 그대로.</summary>
            public readonly string MaterialPath;

            public CastMember(string name, string modelPath, Vector3 position, float rotationY, float scale,
                string firstClipPath, string secondClipPath, float firstDuration, float secondDuration,
                bool usePedestrianMovement, string talkPoolPath, string materialPath = null)
            {
                Name = name; ModelPath = modelPath; Position = position; RotationY = rotationY; Scale = scale;
                FirstClipPath = firstClipPath; SecondClipPath = secondClipPath;
                FirstDuration = firstDuration; SecondDuration = secondDuration;
                UsePedestrianMovement = usePedestrianMovement; TalkPoolPath = talkPoolPath;
                MaterialPath = materialPath;
            }
        }

        private static readonly CastMember[] Cast =
        {
            // 박말순 — 가게 앞에 서서 화를 낸다(두 화내기 동작을 번갈아). 배회하지 않는다.
            new CastMember("malsoon", "Assets/Art/Characters/malsoon/malsoon.fbx",
                new Vector3(14.910889f, -0.0718658f, -0.6943889f), -37.509f, 1.6452f,
                "Assets/Art/Characters/Animation/A_malsoon/malsoon_Angry.fbx",
                "Assets/Art/Characters/Animation/A_malsoon/malsoon_Angry_2.fbx",
                3f, 3f, false, "Assets/Data/Dialogue/Source/parkmalsoon-random-talk.json",
                "Assets/Art/Characters/Materials/malsoon.fbm.mat"),

            // 나아라 — 서 있다 걷다를 반복(배회 ON).
            new CastMember("naara", "Assets/Art/Characters/naara/gs_girl_mixamo_rig_final.fbx",
                new Vector3(-13.921684f, 0.03569287f, -3.0089107f), 135f, 1.4685f,
                "Assets/Art/Characters/naara/naara_Idle.fbx",
                "Assets/Art/Characters/naara/gs_girl_walking.fbx",
                4f, 3f, true, "Assets/Data/Dialogue/Source/na-ara-random-talk.json",
                "Assets/Art/Characters/Materials/gs_girl.mat"),

            // 오지혜 — 대기 자세와 인사를 번갈아(배회 ON).
            new CastMember("jihye", "Assets/Art/Characters/jihye/jihye.fbx",
                new Vector3(2.555153f, -0.016147971f, 2.6524115f), -59.554f, 2.0518f,
                "Assets/Art/Characters/Animation/A_jihye/jihye_Idle.fbx",
                "Assets/Art/Characters/Animation/A_jihye/jihye_Standing Greeting.fbx",
                4f, 3f, true, "Assets/Data/Dialogue/Source/yoo-jihye-random-talk.json"),
        };

        /// <summary>
        /// 빌라촌 NPC를 세운다. 멱등 — 같은 이름의 기존 오브젝트를 지우고 새로 만든다.
        /// 이름에 `__gb_` 접두어를 붙이지 않는 이유: 민지님 씬에 있던 이름 그대로 둬야
        /// 그분이 씬을 열었을 때 같은 하이어라키를 본다. 대신 여기서 직접 청소한다.
        /// </summary>
        internal static void Build(Scene scene)
        {
            Sprite infoBackground = AssetDatabase.LoadAssetAtPath<Sprite>(NPC_INFO_SPRITE);

            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == ANIM_ROOT || IsCastName(root.name)) Object.DestroyImmediate(root);

            var director = new GameObject(ANIM_ROOT);
            int built = 0;

            foreach (CastMember member in Cast)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(member.ModelPath);
                if (model == null)
                {
                    Debug.LogWarning("[빌라촌NPC] 모델 없음 — " + member.ModelPath + " (" + member.Name + " 생략)");
                    continue;
                }

                // Variant 결합 회피 — 독립 클론으로 만든다 (2026-07-20 실수→규칙).
                var instance = (GameObject)Object.Instantiate(model);
                instance.name = member.Name;
                instance.transform.SetPositionAndRotation(
                    member.Position, Quaternion.Euler(0f, member.RotationY, 0f));
                instance.transform.localScale = Vector3.one * member.Scale;

                // 민지님이 씬에서 갈아 끼운 머티리얼. FBX 임베디드 머티리얼은 텍스처를 못 찾아
                // 새하얗게 나온다(malsoon·naara 실측: baseMap 없음) — 그 교체까지가 배치다.
                if (!string.IsNullOrEmpty(member.MaterialPath))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(member.MaterialPath);
                    if (material == null) Debug.LogWarning("[빌라촌NPC] 머티리얼 없음 — " + member.MaterialPath);
                    else foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterial = material;
                }

                var animation = director.AddComponent<AlternatingNpcAnimation>();
                // SerializedObject 주입은 저장 시 오브젝트 참조가 {fileID: 0}으로 날아갈 수 있다
                // (2026-07-20 실수→규칙) — 리플렉션으로 직접 넣는다.
                GreyboxStageBuilder.SetReference(animation, "_target", instance);
                GreyboxStageBuilder.SetReference(animation, "_firstClip", LoadClip(member.FirstClipPath));
                GreyboxStageBuilder.SetReference(animation, "_secondClip", LoadClip(member.SecondClipPath));
                GreyboxStageBuilder.SetReference(animation, "_randomTalkPool",
                    AssetDatabase.LoadAssetAtPath<TextAsset>(member.TalkPoolPath));
                GreyboxStageBuilder.SetReference(animation, "_npcInfoBackground", infoBackground);
                SetPrivate(animation, "_firstDuration", member.FirstDuration);
                SetPrivate(animation, "_secondDuration", member.SecondDuration);
                SetPrivate(animation, "_usePedestrianMovement", member.UsePedestrianMovement);
                built++;
            }

            Debug.Log("[빌라촌NPC] 상주 NPC " + built + "/" + Cast.Length + "인 배치 (S-212 — 민지님 손배치를 코드로 복원).");
        }

        private static bool IsCastName(string name)
        {
            foreach (CastMember member in Cast)
                if (member.Name == name) return true;
            return false;
        }

        /// <summary>
        /// FBX 안의 애니메이션 클립을 꺼낸다. 서브에셋이라 `LoadAssetAtPath`로는 안 잡히고,
        /// 미리보기용 `__preview__` 클립이 섞여 나오므로 걸러 낸다.
        /// </summary>
        private static AnimationClip LoadClip(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            }
            Debug.LogWarning("[빌라촌NPC] 클립 없음 — " + path);
            return null;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(target, value);
        }
    }
}
