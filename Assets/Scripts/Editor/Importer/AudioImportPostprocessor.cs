using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 오디오 폴더 경로 계약 기반 자동 임포트 규칙 (아트 임포터와 같은 철학 — 계약 경로만 트리거).
    /// 계약 경로: Assets/Audio/BGM · Assets/Audio/SFX. 그 밖은 절대 건드리지 않는다.
    /// - BGM: Compressed In Memory · 스테레오 유지 · 백그라운드 로드
    /// - SFX: Decompress On Load · 모노 강제(BOM §8 "2D")
    /// ⚠ Streaming 금지 — WebGL은 Web Audio API 기반이라 Streaming 로드타입을 지원하지 않는다 (D-040).
    /// </summary>
    public class AudioImportPostprocessor : AssetPostprocessor
    {
        private const string AUDIO_ROOT = "Assets/Audio/";
        private const string BGM = "BGM";
        private const string SFX = "SFX";

        // BGM은 재생시간이 길어 압축률이 곧 다운로드 예산이다(실측: q0.70 = ~256kbps → 10곡 20.6MB로 예산 2배 초과).
        // SFX는 짧아 용량 영향이 작으므로 BOM §8 지정값 q0.70을 유지한다.
        private const float BGM_QUALITY = 0.30f;
        private const float SFX_QUALITY = 0.70f;

        /// <summary>계약 경로면 카테고리명, 아니면 null. 폴더 경계("/")로 판정한다.</summary>
        private static string GetCategory(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(AUDIO_ROOT)) return null;
            if (path.StartsWith(AUDIO_ROOT + BGM + "/")) return BGM;
            if (path.StartsWith(AUDIO_ROOT + SFX + "/")) return SFX;
            return null;
        }

        private void OnPreprocessAudio()
        {
            string category = GetCategory(assetPath);
            if (category == null) return;

            var importer = (AudioImporter)assetImporter;
            bool isBgm = category == BGM;

            importer.forceToMono = !isBgm;
            importer.loadInBackground = isBgm;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = isBgm
                ? AudioClipLoadType.CompressedInMemory
                : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = isBgm ? BGM_QUALITY : SFX_QUALITY;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
        }
    }
}
