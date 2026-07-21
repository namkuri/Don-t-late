using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// SFX 플레이스홀더를 코드로 합성한다 — pipelines/audio.md 폴백 원칙("전부 불가 → 무음+최소 신디").
    /// 실음원이 확보되면 **같은 파일명으로 덮어쓰면 끝**이다(BOM §8 파일명=bom_id 스왑 계약).
    /// 이미 파일이 있으면 건드리지 않는다 — 실음원을 합성물로 되돌리는 사고 방지.
    /// </summary>
    public static class SfxSynthGenerator
    {
        private const string SFX_ROOT = "Assets/Audio/SFX";
        private const int SAMPLE_RATE = 44100;

        [MenuItem("DontLate/Generate Placeholder SFX", priority = 20)]
        public static void GenerateMenu()
        {
            int made = EnsurePlaceholders();
            Debug.Log(made > 0
                ? "[SfxSynth] 플레이스홀더 " + made + "종 생성 — 실음원이 오면 같은 파일명으로 덮어쓰면 된다."
                : "[SfxSynth] 이미 전부 존재 — 생성하지 않았다.");
        }

        /// <summary>없는 것만 생성하고 생성 개수를 돌려준다(멱등).</summary>
        public static int EnsurePlaceholders()
        {
            if (!AssetDatabase.IsValidFolder(SFX_ROOT))
                AssetDatabase.CreateFolder("Assets/Audio", "SFX");

            int made = 0;
            made += Write("sfx_pickup", BuildPickup());
            made += Write("sfx_delivery_ok", BuildDeliveryOk());
            made += Write("sfx_late_buzzer", BuildLateBuzzer());

            if (made > 0) AssetDatabase.Refresh();
            return made;
        }

        private static int Write(string bomId, float[] samples)
        {
            string path = SFX_ROOT + "/" + bomId + ".wav";
            if (File.Exists(path)) return 0; // 실음원 보호 — 있으면 절대 덮지 않는다

            WriteWav(path, samples);
            return 1;
        }

        // ── 합성 ─────────────────────────────────────────────
        // JUICE 원칙 "작은 순간 1~2레이어" 준수 — 전부 단순 파형 1~2겹.

        /// <summary>택배 픽업 — 짧게 위로 튀는 블립.</summary>
        private static float[] BuildPickup()
        {
            return Render(0.12f, (t, n) =>
            {
                float sweep = Mathf.Lerp(620f, 1240f, n);
                return Mathf.Sin(2f * Mathf.PI * sweep * t) * Decay(n, 6f) * 0.5f;
            });
        }

        /// <summary>배송 완료 — 딩동(A5→E6) 2음 차임.</summary>
        private static float[] BuildDeliveryOk()
        {
            const float second = 0.18f;
            return Render(0.55f, (t, n) =>
            {
                float ding = Mathf.Sin(2f * Mathf.PI * 880f * t) * Decay(n, 3.5f);
                if (t < second) return ding * 0.45f;

                float t2 = t - second;
                float dong = Mathf.Sin(2f * Mathf.PI * 1319f * t2) * Decay(t2 / (0.55f - second), 3.5f);
                return (ding * 0.25f + dong * 0.45f);
            });
        }

        /// <summary>지각 실패 — 낮게 깔리는 사각파 부저(살짝 처지는 피치).</summary>
        private static float[] BuildLateBuzzer()
        {
            return Render(0.45f, (t, n) =>
            {
                float freq = Mathf.Lerp(112f, 92f, n);
                float square = Mathf.Sin(2f * Mathf.PI * freq * t) >= 0f ? 1f : -1f;
                return square * Decay(n, 2.2f) * 0.32f;
            });
        }

        /// <summary>t=경과초, n=0~1 정규화 진행도를 받아 샘플값을 만든다.</summary>
        private static float[] Render(float seconds, Func<float, float, float> voice)
        {
            int count = Mathf.RoundToInt(seconds * SAMPLE_RATE);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SAMPLE_RATE;
                float n = i / (float)count;
                samples[i] = voice(t, n);
            }

            // 클릭 방지 — 앞뒤 2ms 페이드.
            int fade = Mathf.Min(count / 2, SAMPLE_RATE / 500);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                samples[i] *= k;
                samples[count - 1 - i] *= k;
            }
            return samples;
        }

        private static float Decay(float n, float rate) => Mathf.Exp(-rate * n);

        private static void WriteWav(string path, float[] samples)
        {
            int dataBytes = samples.Length * 2;

            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);              // PCM
                writer.Write((short)1);              // 모노
                writer.Write(SAMPLE_RATE);
                writer.Write(SAMPLE_RATE * 2);       // 바이트레이트
                writer.Write((short)2);              // 블록 정렬
                writer.Write((short)16);             // 비트뎁스
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);

                foreach (float sample in samples)
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * 32760f));
            }
        }
    }
}
