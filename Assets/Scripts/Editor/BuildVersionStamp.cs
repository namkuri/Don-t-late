using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DontLate.EditorTools
{
    /// <summary>
    /// 버전 스탬프 (S-100 ②) — git 해시·시각을 Assets/Resources/build_version.txt로 굽는다.
    /// 에디터: 도메인 리로드마다 갱신(내용 동일하면 무기록) / 빌드: 전처리에서 강제 갱신 —
    /// 팀원 각자 PC에서 빌드해도 그 빌드의 커밋이 화면에 찍힌다. 파일은 PC별 생성물이라 gitignore.
    /// </summary>
    [InitializeOnLoad]
    public class BuildVersionStamp : IPreprocessBuildWithReport
    {
        private const string DIR = "Assets/Resources";
        private const string PATH = DIR + "/build_version.txt";

        static BuildVersionStamp() => Stamp();

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Stamp();

        private static void Stamp()
        {
            string hash = RunGit("rev-parse --short HEAD");
            // 공백 포함 포맷은 Windows 인자 파싱에 깨진다 — 언더스코어로 받고 치환.
            string when = RunGit("log -1 --format=%cd --date=format:%m-%d_%H:%M").Replace('_', ' ');
            bool dirty = RunGit("status --porcelain -uno").Length > 0;
            if (hash.Length == 0) return; // git 부재(zip 배포 등) — 기존 스탬프 유지

            string text = "v." + hash + (dirty ? "*" : "") + " (" + when + ")";
            if (File.Exists(PATH) && File.ReadAllText(PATH) == text) return; // 무변화 — 재임포트 소음 방지

            Directory.CreateDirectory(DIR);
            File.WriteAllText(PATH, text);
            AssetDatabase.ImportAsset(PATH);
        }

        private static string RunGit(string args)
        {
            try
            {
                var info = new ProcessStartInfo("git", args)
                {
                    WorkingDirectory = Path.GetDirectoryName(UnityEngine.Application.dataPath),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using Process process = Process.Start(info);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(3000);
                return process.ExitCode == 0 ? output : string.Empty;
            }
            catch { return string.Empty; } // git 미설치 PC — 스탬프 생략이 빌드를 막으면 안 된다
        }
    }
}
