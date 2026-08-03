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

        // ⚠ S-132 — **현재 꺼져 있다.** 격자 정점 클러스터링은 이 프로젝트의 생성형 아트(포토그래메트리
        // 계열 · 얇고 촘촘한 표면)에 맞지 않는다. 실측 2회 전부 육안 반려:
        //   ① 목표 3000(=감사 상한): 건물이 알아볼 수 없는 덩어리 (Screenshots/s132_district_after.png)
        //   ② 목표 20000 + 고운 격자부터: 실루엣은 살지만 표면에 구멍이 숭숭 (s132_camp_v2.png)
        // 클러스터링은 얇은 면을 붙여버리고 남은 삼각형을 퇴화로 버려 "좀먹은" 표면을 만든다.
        // 제대로 하려면 **쿼드릭 오차 기반 감축**(Blender Decimate 등)이 필요하다 — 아트 레인 몫.
        // 코드는 남겨 둔다(재시도·참고용). 켜려면 DECIMATE_ENABLED = true.
        private const bool DECIMATE_ENABLED = false;

        // S-132 — **감축 목표는 감사 상한(PolyLimits)과 별개다.** 상한은 아트 스타일 목표치고,
        // 여기는 "WebGL에 실을 수 있으면서 형상이 살아남는" 크기다. 상한(건물 3000)을 그대로
        // 감축 목표로 쓰면 격자가 해상도 8까지 내려가 건물이 덩어리가 된다(1차 시공 실측·반려).
        private static readonly Dictionary<string, int> DecimateBudgets = new Dictionary<string, int>
        {
            { "Buildings", 20000 },
            { "Props", 8000 },
            { "Backgrounds", 20000 },
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

            // S-079 ③ — 3D 표면 텍스처는 밉맵 필수: 밉 없이 Point만 쓰면 원거리에서 모아레
            // (물결·얼룩 — 남규님 실측). UI·Portraits(화면 고정 스케일)는 밉 불필요라 끈다.
            string category = GetCategory(assetPath);
            bool surface = category == "Buildings" || category == "Props"
                || category == "Backgrounds" || category == "Characters";
            importer.mipmapEnabled = surface;
            if (surface)
            {
                importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
                importer.anisoLevel = 4; // 낮은 카메라 각도의 바닥류 완화
            }
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
            // S-132 — 감축이 **Blender(쿼드릭 Decimate)로 이관**되면서 임포트 시점에 메시를 읽을
            // 이유가 사라졌다(폴리 집계는 GetIndexCount로 대체 — 읽기 불가 메시에서도 동작).
            // 읽기 가능 메시는 빌드에 CPU 사본이 하나 더 들어가 **용량이 2배**가 된다. 끈다.
            // ⚠ 되켤 일이 있다면: false면 OnPostprocessModel의 `mesh.vertices`가 예외 없이
            //   빈 배열을 돌려주므로, 그 안에서 메시를 수정하는 코드는 동작하지 않는다(실측).
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.High; // 정점 양자화 — 픽셀 렌더라 손실 무해

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

            // 폴리 수 집계 → 예산 초과면 그 자리에서 감축 (S-132).
            int tris = CountTriangles(root);
            if (PolyLimits.TryGetValue(category, out int limit) && tris > limit)
            {
                // Characters는 건드리지 않는다 — 스킨드 메시라 본 웨이트가 클러스터링에 못 따라온다.
                if (category == "Characters")
                {
                    Debug.LogWarning(
                        $"[ArtImport] 폴리 초과: {file} ({category}) 실측 {tris} > 상한 {limit}. " +
                        "캐릭터는 자동 감축 제외 — 수동 데시메이트 필요.");
                }
                else if (DECIMATE_ENABLED && DecimateBudgets.TryGetValue(category, out int budget))
                {
                    int after = DecimateAll(root, budget);
                    Debug.Log($"[ArtImport] 폴리 감축: {file} ({category}) {tris} → {after} 삼각형 (감축목표 {budget}).");
                }
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

            // S-113 — 임베디드 텍스처 자동 추출 (2026-07-21 대기열 규칙 집행): 계약 경로 모델의
            // 임베디드 텍스처를 <분류>/Textures/로 추출 — 룩이 fbx 안에 갇히지 않게. 추출이 모델
            // 재임포트를 1회 유발하지만 그때는 추출물이 이미 있어 재추출 no-op(무한 재귀 없음).
            foreach (string path in targets)
                ExtractEmbeddedTextures(path);

            // Buildings·Props 모델 → Prefabs/Auto 프리팹 생성/갱신.
            // Auto 프리팹은 계약 경로 밖이라 재임포트를 재귀 트리거하지 않는다.
            CategoryPrefabFactory.BuildPrefabs(targets);
        }

        /// <summary>S-113 — 임베디드 텍스처를 같은 분류의 Textures/ 폴더로 추출. 이미 추출됐으면 no-op.</summary>
        internal static void ExtractEmbeddedTextures(string modelPath)
        {
            if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer)) return;

            string folder = System.IO.Path.GetDirectoryName(modelPath).Replace('\\', '/');
            string texFolder = folder + "/Textures";
            string marker = texFolder + "/" + System.IO.Path.GetFileNameWithoutExtension(modelPath);
            // 추출물 존재 판정: 같은 이름 접두 텍스처가 이미 있으면 스킵 (재임포트 무한루프 방지).
            if (System.IO.Directory.Exists(texFolder)
                && System.IO.Directory.GetFiles(texFolder, System.IO.Path.GetFileNameWithoutExtension(modelPath) + "*").Length > 0)
                return;

            if (!AssetDatabase.IsValidFolder(texFolder))
                AssetDatabase.CreateFolder(folder, "Textures");

            if (importer.ExtractTextures(texFolder))
                Debug.Log($"[임포터] {System.IO.Path.GetFileName(modelPath)} 임베디드 텍스처 추출 → {texFolder}/");
        }

        /// <summary>S-113 — 기존 반입분 일괄 추출 (Buildings·Props 전량).</summary>
        [MenuItem("DontLate/Art/Extract Embedded Textures (전량)")]
        public static void ExtractAllEmbeddedTextures()
        {
            int count = 0;
            foreach (string folder in new[] { "Assets/Art/Buildings", "Assets/Art/Props", "Assets/Art/Characters" })
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                {
                    ExtractEmbeddedTextures(AssetDatabase.GUIDToAssetPath(guid));
                    count++;
                }
            }
            Debug.Log($"[임포터] 임베디드 텍스처 일괄 추출 시도 — 모델 {count}종.");
        }

        // ── 폴리 감축 (S-132) ────────────────────────────────
        // 왜 필요한가: 생성형 아트(Trellis/Tripo) 산출물이 모델당 **50만 삼각형** 수준이라
        // WebGL 빌드가 1.19GB가 됐다(BuildReport 실측 = Mesh 77개 1,618MB). GitHub Pages는
        // 단일 파일 100MB·사이트 1GB 상한이라 배포 자체가 불가능했다.
        //
        // 방식 = **격자 정점 클러스터링**. 바운즈를 N³ 격자로 나눠 같은 칸의 정점을 하나로 합치고
        // 삼각형을 다시 엮는다(퇴화 삼각형은 버린다). 이 게임은 480×270으로 렌더한 뒤 정수 배율로
        // 확대하므로, 칸 크기를 1아트픽셀 아래로 잡으면 손실이 화면에 잡히지 않는다.
        // UV는 칸별 평균을 쓰되 **양자화한 UV를 클러스터 키에 포함**한다 — 안 그러면 UV 심(seam)을
        // 가로지르는 칸이 엉뚱한 텍셀을 물어 텍스처가 번진다.
        private static int DecimateAll(GameObject root, int budget)
        {
            var meshes = new HashSet<Mesh>();
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null) meshes.Add(mf.sharedMesh);

            int total = 0;
            foreach (Mesh mesh in meshes)
            {
                // 메시가 여럿이면 예산을 나눠 갖는다(합계가 상한을 넘지 않게).
                total += DecimateMesh(mesh, Mathf.Max(64, budget / meshes.Count));
            }
            return total;
        }

        /// <summary>
        /// 격자를 점점 거칠게 하며 **매번 적용**한다(누진 감축). 예산 이하가 되면 멈춘다.
        /// 매번 적용하는 게 핵심 — 예산에 못 미쳐도 마지막(가장 거친) 결과는 반드시 남는다.
        /// </summary>
        private static int DecimateMesh(Mesh mesh, int budget)
        {
            // 고운 격자부터 시작한다 — 거친 쪽에서 시작하면 형상이 먼저 죽는다.
            int[] resolutions = { 192, 144, 108, 80, 60, 44, 32 };
            int tris = CountTriangles(mesh);
            foreach (int res in resolutions)
            {
                tris = ClusterOnce(mesh, res);
                if (tris <= budget) break;
            }
            return tris;
        }

        /// <summary>한 번 클러스터링해 메시에 적용한다. 반환 = 결과 삼각형 수.</summary>
        private static int ClusterOnce(Mesh mesh, int resolution)
        {
            int triangleCount;
            Vector3[] srcVerts = mesh.vertices;
            Vector2[] srcUv = mesh.uv;
            bool hasUv = srcUv != null && srcUv.Length == srcVerts.Length;

            Bounds bounds = mesh.bounds;
            Vector3 size = bounds.size;
            float cell = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) / resolution;
            if (cell <= 0f) return CountTriangles(mesh);

            // UV 심 보존용 버킷 수는 격자와 함께 거칠어진다. 고정 16버킷으로 두면 정점마다
            // 사실상 고유 키가 생겨 **아무것도 합쳐지지 않는다**(S-132 1차 시공 실패 원인).
            // 거친 격자에서는 1버킷 = UV를 키에서 제외 → 확실히 줄어든다.
            int uvBuckets = Mathf.Max(1, resolution / 16);

            // 정점 → 클러스터 대표 인덱스
            var map = new Dictionary<long, int>(srcVerts.Length);
            var remap = new int[srcVerts.Length];
            var newVerts = new List<Vector3>();
            var newUv = new List<Vector2>();
            var accumCount = new List<int>();

            for (int i = 0; i < srcVerts.Length; i++)
            {
                Vector3 p = srcVerts[i] - bounds.min;
                long kx = (long)(p.x / cell), ky = (long)(p.y / cell), kz = (long)(p.z / cell);
                long ku = hasUv && uvBuckets > 1
                    ? (long)(srcUv[i].x * uvBuckets) * 31 + (long)(srcUv[i].y * uvBuckets)
                    : 0;
                long key = ((kx * 73856093) ^ (ky * 19349663) ^ (kz * 83492791) ^ (ku * 2654435761L));

                if (map.TryGetValue(key, out int slot))
                {
                    newVerts[slot] += srcVerts[i];
                    if (hasUv) newUv[slot] += srcUv[i];
                    accumCount[slot]++;
                }
                else
                {
                    slot = newVerts.Count;
                    map[key] = slot;
                    newVerts.Add(srcVerts[i]);
                    if (hasUv) newUv.Add(srcUv[i]);
                    accumCount.Add(1);
                }
                remap[i] = slot;
            }

            for (int i = 0; i < newVerts.Count; i++)
            {
                newVerts[i] /= accumCount[i];
                if (hasUv) newUv[i] /= accumCount[i];
            }

            // 서브메시 구조는 보존한다 — 머티리얼 슬롯이 여기 걸려 있다.
            int subCount = mesh.subMeshCount;
            var subTriangles = new List<int>[subCount];
            triangleCount = 0;
            for (int s = 0; s < subCount; s++)
            {
                int[] src = mesh.GetTriangles(s);
                var dst = new List<int>(src.Length);
                for (int t = 0; t + 2 < src.Length; t += 3)
                {
                    int a = remap[src[t]], b = remap[src[t + 1]], c = remap[src[t + 2]];
                    if (a == b || b == c || a == c) continue; // 퇴화 — 버린다
                    dst.Add(a); dst.Add(b); dst.Add(c);
                }
                subTriangles[s] = dst;
                triangleCount += dst.Count / 3;
            }

            mesh.Clear();
            mesh.indexFormat = newVerts.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(newVerts);
            if (hasUv) mesh.SetUVs(0, newUv);
            mesh.subMeshCount = subCount;
            for (int s = 0; s < subCount; s++) mesh.SetTriangles(subTriangles[s], s);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return triangleCount;
        }

        // ── 헬퍼 ─────────────────────────────────────────────
        /// <summary>읽기 불가 메시에서도 동작 — 인덱스 개수는 메타데이터라 항상 조회된다.</summary>
        private static int CountTriangles(Mesh mesh)
        {
            if (mesh == null) return 0;
            if (mesh.isReadable) return mesh.triangles.Length / 3;
            long indices = 0;
            for (int s = 0; s < mesh.subMeshCount; s++) indices += (long)mesh.GetIndexCount(s);
            return (int)(indices / 3);
        }

        private static int CountTriangles(GameObject root)
        {
            int tris = 0;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
                tris += CountTriangles(mf.sharedMesh);
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                tris += CountTriangles(smr.sharedMesh);
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
