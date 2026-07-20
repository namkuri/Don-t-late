using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 아트 폴더 경로 계약(D-002) 기반 자동 임포트 규칙.
    /// 계약 경로(Assets/Art/Buildings|Props|Characters|Backgrounds|Portraits|UI)만 트리거한다.
    /// 계약 밖 폴더(Assets/Art/Building·Car 등 사람 폴더)는 절대 건드리지 않는다.
    /// - 텍스처: Point 필터·압축 None·256px
    /// - 모델: 읽기 가능·폴리 상한 검사(경고만) / Characters 는 1.8u 높이 검사(경고만)
    /// - Buildings·Props 모델은 CategoryPrefabFactory 로 Prefabs/Auto 프리팹 생성/갱신
    /// </summary>
    public class ArtImportPostprocessor : AssetPostprocessor
    {
        private const string ART_ROOT = "Assets/Art/";

        // 계약 카테고리 6종 (이 이름들만 트리거 — 사람 폴더 Building·Car 제외)
        private static readonly string[] Categories =
        {
            "Buildings", "Props", "Characters", "Backgrounds", "Portraits", "UI"
        };

        // 폴리 상한 (삼각형 수) — 초과 시 경고만. 없는 카테고리는 검사 안 함.
        private static readonly Dictionary<string, int> PolyLimits = new Dictionary<string, int>
        {
            { "Buildings", 3000 },
            { "Props", 1500 },
            { "Characters", 5000 },
        };

        private const float CHARACTER_HEIGHT_ANCHOR = 1.8f; // u
        private const float CHARACTER_HEIGHT_TOLERANCE = 0.30f; // ±30%

        /// <summary>계약 경로면 카테고리명, 아니면 null. 접두어가 아니라 폴더 경계(+"/")로 판정.</summary>
        private static string GetCategory(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(ART_ROOT))
                return null;

            foreach (string c in Categories)
            {
                // "Assets/Art/Buildings/" — 사람 폴더 "Assets/Art/Building/"(단수)은 "s/" 경계에서 탈락.
                if (assetPath.StartsWith(ART_ROOT + c + "/"))
                    return c;
            }
            return null;
        }

        // ── 텍스처 ───────────────────────────────────────────
        private void OnPreprocessTexture()
        {
            if (GetCategory(assetPath) == null) return;

            var importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // 압축 None
            importer.maxTextureSize = 256;
        }

        // 애니메이션을 끄는 정적 카테고리 (소품·건물·배경 — 애니 불필요).
        // Characters 는 제외 — Mixamo 클립 임포트가 필요하다.
        private static readonly string[] NoAnimationCategories =
        {
            "Props", "Buildings", "Backgrounds"
        };

        // ── 모델 (임포트 전) ─────────────────────────────────
        private void OnPreprocessModel()
        {
            string category = GetCategory(assetPath);
            if (category == null) return;

            var importer = (ModelImporter)assetImporter;
            importer.isReadable = true; // 폴리·바운즈 검사를 위해

            // 소품·건물·배경: 애니 임포트 자체를 끈다 (Tripo 빈 클립 경고 원천 차단).
            if (System.Array.IndexOf(NoAnimationCategories, category) >= 0)
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }
        }

        // ── 머티리얼 (임포트 후) — 비-URP 셰이더를 URP/Lit로 리맵 ──
        // 계약 경로 모델의 임포트 머티리얼이 URP가 아니면(예: 표준/레거시 → 마젠타) URP/Lit로 교체.
        // 베이스맵 텍스처·베이스컬러는 보존한다. (M1-07 완성)
        private void OnPostprocessMaterial(Material material)
        {
            if (GetCategory(assetPath) == null) return;
            if (material == null || material.shader == null) return;
            if (material.shader.name.StartsWith("Universal Render Pipeline/")) return;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            // 표준/레거시 프로퍼티에서 베이스맵·색을 읽어 URP 프로퍼티로 이관.
            Texture baseMap = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Color baseColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

            material.shader = urpLit;
            if (baseMap != null && material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", baseMap);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);

            Debug.Log($"[ArtImport] 머티리얼 URP 리맵: {material.name} " +
                      $"({System.IO.Path.GetFileName(assetPath)})");
        }

        // ── 모델 (임포트 후) — 폴리·높이 검사 ────────────────
        private void OnPostprocessModel(GameObject root)
        {
            string category = GetCategory(assetPath);
            if (category == null) return;

            string file = System.IO.Path.GetFileName(assetPath);

            // 폴리 수 집계
            int tris = CountTriangles(root);
            if (PolyLimits.TryGetValue(category, out int limit) && tris > limit)
            {
                Debug.LogWarning(
                    $"[ArtImport] 폴리 초과: {file} ({category}) 실측 {tris} 삼각형 > 상한 {limit}. " +
                    "데시메이트 필요 (Blender 레인).");
            }

            // Characters 높이 앵커 검사
            if (category == "Characters" && TryGetBoundsHeight(root, out float height))
            {
                float min = CHARACTER_HEIGHT_ANCHOR * (1f - CHARACTER_HEIGHT_TOLERANCE);
                float max = CHARACTER_HEIGHT_ANCHOR * (1f + CHARACTER_HEIGHT_TOLERANCE);
                if (height < min || height > max)
                {
                    Debug.LogWarning(
                        $"[ArtImport] 높이 이탈: {file} 실측 {height:0.00}u, 앵커 {CHARACTER_HEIGHT_ANCHOR}u " +
                        $"허용 {min:0.00}~{max:0.00}u. 스케일 확인 필요.");
                }
            }
        }

        // ── 임포트 배치 완료 — 팩토리 트리거 ─────────────────
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var targets = new List<string>();
            foreach (string path in importedAssets)
            {
                string category = GetCategory(path);
                if (category != "Buildings" && category != "Props") continue;
                if (!(AssetImporter.GetAtPath(path) is ModelImporter)) continue;
                targets.Add(path);
            }

            if (targets.Count == 0) return;

            // Buildings·Props 모델 → Prefabs/Auto 프리팹 생성/갱신.
            // Auto 프리팹은 계약 경로 밖이라 재임포트를 재귀 트리거하지 않는다.
            CategoryPrefabFactory.BuildPrefabs(targets);
        }

        // ── 헬퍼 ─────────────────────────────────────────────
        private static int CountTriangles(GameObject root)
        {
            int tris = 0;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null)
                    tris += mf.sharedMesh.triangles.Length / 3;
            }
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh != null)
                    tris += smr.sharedMesh.triangles.Length / 3;
            }
            return tris;
        }

        /// <summary>루트 로컬 공간 기준 결합 바운즈의 높이(Y). 메시가 없으면 false.</summary>
        private static bool TryGetBoundsHeight(GameObject root, out float height)
        {
            height = 0f;
            bool any = false;
            Bounds acc = default;
            Matrix4x4 worldToRoot = root.transform.worldToLocalMatrix;

            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                EncapsulateMesh(mf.sharedMesh.bounds, worldToRoot * mf.transform.localToWorldMatrix, ref acc, ref any);
            }
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                EncapsulateMesh(smr.sharedMesh.bounds, worldToRoot * smr.transform.localToWorldMatrix, ref acc, ref any);
            }

            if (!any) return false;
            height = acc.size.y;
            return true;
        }

        private static void EncapsulateMesh(Bounds meshBounds, Matrix4x4 m, ref Bounds acc, ref bool any)
        {
            Vector3 c = meshBounds.center;
            Vector3 e = meshBounds.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!any) { acc = new Bounds(p, Vector3.zero); any = true; }
                else acc.Encapsulate(p);
            }
        }
    }
}
