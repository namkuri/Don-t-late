using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DontLate.EditorTools
{
    /// <summary>
    /// S-245 — 투명 머티리얼의 블렌드 계수를 알파 블렌딩으로 되돌린다.
    ///
    /// 증상: 밤에 스프라이트의 **투명해야 할 부분이 하얗게** 뜬다(남규님 관찰 — Home 씬 크랙·포스터).
    /// 원인: 머티리얼이 Blending Mode = **Alpha**로 표시돼 있는데 실제 계수는
    /// `SrcBlend = One`(Premultiply)로 굳어 있다. 우리 텍스처는 straight alpha(PNG)라
    /// 투명 픽셀이 `1×흰색 + (1−0)×배경`으로 **더해진다** — 낮에는 밝은 배경에 묻히고
    /// 밤에는 어두운 배경 위에 흰 사각형으로 드러난다.
    ///
    /// 대상은 **표시와 실제가 어긋난 것만**이다(`_Blend`=Alpha인데 `_SrcBlend`가 SrcAlpha가 아닌 경우).
    /// Blending Mode를 일부러 Premultiply/Additive로 고른 머티리얼은 건드리지 않는다.
    /// 코드가 만드는 머티리얼은 이미 SrcAlpha로 세팅한다(BlossomPetalEffect 등) — 여기 걸리지 않는다.
    ///
    /// 아트 반입 때 인스펙터에서 다시 Premultiply가 선택되면 재발하므로, 반입 후 한 번 돌리면 된다(멱등).
    /// </summary>
    public static class TransparentBlendFixer
    {
        private const float SURFACE_TRANSPARENT = 1f;
        private const float BLEND_ALPHA = 0f;

        [MenuItem("DontLate/Art/⑤ 투명 머티리얼 교정 (밤 백화)", priority = 45)]
        public static void FixAll()
        {
            var fixedNames = new List<string>();
            var skipped = new List<string>();
            var clipped = new List<string>();
            var particleSkipped = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;
                if (!material.HasProperty("_Surface") || !material.HasProperty("_SrcBlend")) continue;
                if (!Mathf.Approximately(material.GetFloat("_Surface"), SURFACE_TRANSPARENT)) continue;

                // S-246 — Alpha Clipping을 켠다(남규님 실측: "켜니까 정상됨"). 알파가 임계값 미만인
                // 픽셀을 아예 버리므로 투명부가 확실히 사라진다. 블렌드 교정만으로는 부족했다.
                // 파티클 셰이더는 제외한다 — 부드럽게 사라지는 것이 존재 이유라 잘라내면 각져 보인다
                // (꽃잎·눈·비). `_Cutoff`조차 없는 것도 있다.
                bool isParticle = material.shader != null && material.shader.name.Contains("/Particles/");
                if (isParticle) particleSkipped.Add(material.name);
                else if (material.HasProperty("_AlphaClip") && material.HasProperty("_Cutoff")
                         && !Mathf.Approximately(material.GetFloat("_AlphaClip"), 1f))
                {
                    material.SetFloat("_AlphaClip", 1f);
                    material.EnableKeyword("_ALPHATEST_ON"); // 프로퍼티만 켜면 셰이더가 안 본다
                    EditorUtility.SetDirty(material);
                    clipped.Add(material.name);
                    // `_Cutoff`는 손대지 않는다 — 사람이 맞춰 둔 값(fire 0.11 · orange 0.37 등)이 있다.
                }

                // 의도적으로 Premultiply/Additive/Multiply를 고른 것은 그대로 둔다.
                float blend = material.HasProperty("_Blend") ? material.GetFloat("_Blend") : BLEND_ALPHA;
                if (!Mathf.Approximately(blend, BLEND_ALPHA))
                {
                    skipped.Add(material.name + "(Blend=" + blend + ")");
                    continue;
                }

                if (Mathf.Approximately(material.GetFloat("_SrcBlend"), (float)BlendMode.SrcAlpha)) continue;

                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                EditorUtility.SetDirty(material);
                fixedNames.Add(material.name);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[투명머티리얼] 블렌드 교정 " + fixedNames.Count + "개: " + string.Join(", ", fixedNames)
                + "\nAlpha Clipping 켬 " + clipped.Count + "개: " + string.Join(", ", clipped)
                + (particleSkipped.Count > 0 ? "\n파티클이라 건너뜀 " + particleSkipped.Count + "개: " + string.Join(", ", particleSkipped) : "")
                + (skipped.Count > 0 ? "\n의도적 블렌드라 건너뜀 " + skipped.Count + "개: " + string.Join(", ", skipped) : ""));
        }
    }
}
