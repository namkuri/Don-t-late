using UnityEditor;
using UnityEngine;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 오디오 폴더 경로 계약 기반 자동 임포트 규칙 (아트 임포터와 같은 철학 — 계약 경로만 트리거).
    /// 계약 경로: Assets/Audio/BGM · Assets/Audio/SFX. 그 밖은 절대 건드리지 않는다.
    /// - BGM: Compressed In Memory · 모노(S-136 배포 예산) · 백그라운드 로드
    /// - SFX: Decompress On Load · 모노 강제(BOM §8 "2D")
    /// - amb_*(SFX 폴더 내 긴 루프 베드): Compressed In Memory · 모노 · 백그라운드·저비트레이트
    ///   (40s 루프를 DecompressOnLoad하면 RAM 낭비 — 긴 루프는 BGM식 로드가 정답. AU-018 ①)
    /// ⚠ Streaming 금지 — WebGL은 Web Audio API 기반이라 Streaming 로드타입을 지원하지 않는다 (D-040).
    /// </summary>
    public class AudioImportPostprocessor : AssetPostprocessor
    {
        private const string AUDIO_ROOT = "Assets/Audio/";
        private const string BGM = "BGM";
        private const string SFX = "SFX";

        // BGM은 재생시간이 길어 압축률이 곧 다운로드 예산이다(실측: q0.70 = ~256kbps → 10곡 20.6MB로 예산 2배 초과).
        // SFX는 짧아 용량 영향이 작으므로 BOM §8 지정값 q0.70을 유지한다.
        // S-136 — WebGL 배포 예산(GitHub 단일 파일 100MB)에 맞춰 BGM을 모노로 내린다.
        // 실측: BGM 11곡 스테레오 q0.30 = 빌드 내 17MB로 오디오 예산의 85%를 혼자 먹었다.
        // 같은 바이트를 쓸 거면 스테레오 저품질보다 **모노 유지품질**이 더 낫게 들린다
        // (채널당 비트레이트가 2배). 게임 BGM은 SFX 아래 깔리므로 스테레오 이미지 손실이 작다.
        // ⚠ AU-030 실측 — **0.26 아래로는 내려봐야 소용없다.** 같은 곡(Daylight Snowfall, 49.3s 모노)을
        // 품질만 바꿔 강제 재임포트하며 임포트 산출물 크기를 잰 결과:
        //   q0.05 = 374KB · q0.22 = 374KB · q0.26 = 374KB · q0.35 = 374KB · q0.50 = 431KB · q0.70 = 490KB · q0.90 = 555KB
        // 0.35 이하는 전부 같은 바이트가 나온다 — Unity Vorbis 인코더의 **하한 버킷**이고 현행 0.26이
        // 이미 그 안에 있다. 용량을 더 짜려고 이 숫자를 건드리지 말 것(0바이트 절감, 청감만 손해볼 위험).
        // 오디오를 더 줄여야 하면 남은 레버는 **곡 수·재생시간**뿐이다.
        private const float BGM_QUALITY = 0.26f;
        private const float SFX_QUALITY = 0.40f;

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
            // amb_*(SFX 폴더 내 긴 앰비언스 루프) — DecompressOnLoad 대신 BGM식 압축 상주로.
            bool isAmbLoop = !isBgm && System.IO.Path.GetFileName(assetPath).StartsWith("amb_");
            bool compressedInMemory = isBgm || isAmbLoop;

            importer.forceToMono = true;                   // S-136 — BGM 포함 전량 모노(위 예산 근거)
            importer.loadInBackground = compressedInMemory;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = compressedInMemory
                ? AudioClipLoadType.CompressedInMemory
                : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = isBgm || isAmbLoop ? BGM_QUALITY : SFX_QUALITY;  // 긴 루프는 저비트레이트
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
        }
    }
}
