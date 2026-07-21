# orders/audio.md — 오디오 발주 대장 (append-only)

> 형식: [guides/distributed-workflow.md](../guides/distributed-workflow.md) §3. 발주·결과 시각은 파일 안에 명시.
> 공통 규격: [[BOM]] §8 (정정본은 D-040 반영) · 라이선스 기록 = 반입 입장권(교정 불가 게이트) ·
> 총 오디오 예산 ≤ 10MB (SFX 포함).

---

## AU-001 · 발주 2026-07-21 21:20 → ClaudeCode (본 세션 실행)

BGM 10곡 반입 → 임포트 → `WorldAudioManager` 구현 → 인게임 재생·낮밤 전환 확인까지 풀스택.

### 배경 — 그릴링 세션 결과 (2026-07-21 20:30~21:20)

Director가 `C:\Works\Game\Don-t-late-bgm\`에 ElevenLabs 생성 BGM을 확보. 규격 대조 과정에서
차단 사실·구조 결함이 다수 드러나 결정 11건으로 정리했다.

**확보 자산 (WAV 10곡 · 48kHz/16bit/stereo PCM · 총 132MB)**

| 파일 | 길이 | 원본 FLAC 분류 |
|---|---|---|
| `Ironic_Stillness_2026-07-20T145653` | 60s | 낮·밤 **양쪽** (afternoon-01 = night-01, 바이트 동일) |
| `Sunlit_Seoul_Afternoon_2026-07-20T154627` | 60s | 낮 |
| `Seoul_Alley_Reflection_2026-07-20T161148` | 60s | 낮 |
| `Breezy_Town_Stroll_2026-07-20T161422` | **180s** | 낮 |
| `Seoul_Afternoon_Stroll_2026-07-20T155537` | 60s | 밤 |
| `Late_for_Work_8-Bit_Panic_2026-07-19T072529` | 60s | 미분류 — **8비트, 규격 이탈** → Title 후보 |
| `Pixel_Seoul_Breeze_2026-07-19T103036` | 60s | 미분류 |
| `Seoul_Pixel_Breeze_2026-07-19T103406` | 60s | 미분류 |
| `Seoul_Pixel_Boulevard_2026-07-19T103537` | 60s | 미분류 |
| `Sunlit_Stroll_in_Seoul_2026-07-20T154103` | 60s | 미분류 |

분류 증거는 `Don-t-late-bgm/MAPPING.md`에 보존(FLAC PCM MD5 ↔ WAV MD5 대조로 확정).
**제목으로 낮/밤을 추정하면 틀린다** — `Seoul_Alley_Reflection`(밤 느낌)이 실제로는 낮,
`Seoul_Afternoon_Stroll`(낮 느낌)이 실제로는 밤이었다.

**조사에서 드러난 차단 사실 4건**

1. **Unity는 FLAC 미지원** (`.wav`/`.aif`/`.mp3`/`.ogg`+트래커만) → Director가 WAV로 재확보 완료
2. **WebGL은 `Streaming` 로드타입 미지원** (Web Audio API 기반, 스레드 불가) → BOM §8 규격 무효
3. **`AudioListener`가 District 씬에만 존재** — Core·Main·Home·Camp·Travel 전부 무음 구조
4. **`WorldDayNightManager`는 조명을 페이즈로 안 바꾼다** — `t = minuteOfDay/1440` 연속 보간.
   `DayPhase` 4단계는 이벤트 통지 전용 이산값

**폐기**: `afternoon-bgm-03`·`night-bgm-03` 2곡 — WAV 대응본 없음, 재확보 포기(Director 결정).
FLAC 원본 8개 삭제 완료.

### 목표

Core 씬에서 BGM이 재생되고, 시각이 Evening(17시)에 진입하면 밤곡으로 3초 크로스페이드되며,
Director가 인게임에서 10곡을 순회 청취해 컷·분류를 판정할 수 있는 상태.

### 입력·산출 위치

**입력**
- 원본 WAV 10곡: `C:\Works\Game\Don-t-late-bgm\*.wav`
- 프롬프트 설계서 3종: 같은 폴더 `afternoon-bgm.md`·`afternoon-bgm-02.md`·`night-bgm.md`
- 분류·권리 기록: 같은 폴더 `MAPPING.md`

**산출 — 신규**
- `Assets/Audio/BGM/*.wav` (10곡 · **git ignore**)
- `Assets/Audio/CREDITS.md` (커밋)
- `Assets/Scripts/SO/BgmLibrarySO.cs`
- `Assets/Scripts/Managers/WorldAudioManager.cs`
- `Assets/Scripts/Editor/Importer/AudioImportPostprocessor.cs`
- `Assets/Data/BgmLibrary.asset`

**산출 — 수정**
- `Assets/Scripts/Editor/CoreSceneBuilder.cs` — `AudioListener` + `WorldAudioManager` 조립
- `Assets/Scripts/Editor/DistrictSceneBuilder.cs` — `AudioListener` 제거
- `.gitignore` · `planning/BOM.md` §8 · `planning/decisions.md`

### 기대 (구현 명세)

**1. 반입·git**
- `Assets/Audio/BGM/`에 WAV 10곡 복사 (파일명 **원본 유지** — `bom_id` 리네임은 컷·분류 확정 후.
  지금 `bgm_day_01`로 바꾸면 미확정 분류를 파일명에 못박는다)
- `.gitignore` 추가:
  ```
  # 오디오 원본 — 컷 판정 전까지 미커밋 (D-042)
  /[Aa]ssets/Audio/**/*.wav
  /[Aa]ssets/Audio/**/*.wav.meta
  ```
- `Assets/Audio/CREDITS.md`: 도구(Eleven Music) · 플랜(유료 구독) · 권리 근거(상업 사용 무기한) ·
  생성일 · 프롬프트 설계서 전문 · 곡별 원제·길이

**2. 임포트 자동화** — `AudioImportPostprocessor.cs`
- 계약 경로 `Assets/Audio/BGM/`·`Assets/Audio/SFX/`만 트리거 (아트 임포터와 동일 철학 — 계약 밖 폴더 불가침)
- BGM: `loadType = CompressedInMemory` · `preloadAudioData = false` · `forceToMono = false` ·
  `compressionFormat = Vorbis` · `quality = 0.7` · `loadInBackground = true`
- SFX: `loadType = DecompressOnLoad` · `forceToMono = true` (BOM §8 "2D")
- ⚠ `Streaming` 금지 — WebGL 미지원 (근거: Unity Manual · Audio in Web)

**3. 데이터** — `BgmLibrarySO.cs`
```csharp
public enum BgmSlot { Unsorted, Day, Night, Title }

[CreateAssetMenu(menuName = "DontLate/BgmLibrary")]
public class BgmLibrarySO : ScriptableObject {
    [Serializable] public class Entry { public AudioClip clip; public BgmSlot slot; }
    public List<Entry> entries = new List<Entry>();
}
```
- 곡 컷 = 리스트 원소 제거 1회 / 분류 변경 = 드롭다운 1회. 코드 수정 없이 인스펙터로 끝난다
- 초기값: `MAPPING.md` 확정분 5곡만 슬롯 배정, 나머지 5곡은 `Unsorted`.
  **제목으로 추정하지 않는다** (추정이 틀린다는 실증이 있다)

**4. 매니저** — `WorldAudioManager.cs` (Core 상주 싱글톤 규약)
- `AudioSource` 2개(A/B) — 둘 다 `spatialBlend = 0`(2D) · `loop = true`. 크로스페이드용
- **세션 시작 1회 추첨**: Day 풀·Night 풀·Title 풀에서 각 1곡. `Unsorted`는 추첨 제외
- **no-repeat**: 직전 세션 선택곡을 `PlayerPrefs`에 남겨 추첨에서 제외 (풀 크기 1이면 무시)
- 구독 (`OnEnable`/`OnDisable` 짝 필수):
  - `WorldEvents.DayPhaseChanged` → `Morning`·`Day` = 낮곡 / `Evening`·`Night` = 밤곡
  - `WorldEvents.SceneTransitionCompleted` → `GameScene.Main` = 타이틀곡
- 전환 = **3초 크로스페이드** 코루틴 (한쪽 볼륨 ↓, 다른 쪽 ↑). 같은 곡이면 아무것도 안 함
- 마스터 볼륨 `[SerializeField] private float _volume` 인스펙터 노출 (기본 0.5)
- **신규 `WorldEvents` 이벤트 없음** — 기존 이벤트 구독만. BGM은 상태 변화를 통지하지 않는다

**5. 청취·판정 도구** (`#if UNITY_EDITOR` 조건부 — 릴리스 빌드에서 사라짐)
- `OnGUI`로 현재 곡 표시: `[Day 3/6] Seoul_Pixel_Breeze`
- `Keyboard.current` 직접 읽기 — **`InputAction` 추가 금지** (에디터 전용 도구를 게임 입력 계약에 넣지 않는다)
  - `N` = 현재 슬롯 내 다음 곡 (크로스페이드 없이 즉시)
  - `B` = Day ↔ Night 슬롯 토글
- 목적: 랜덤에만 맡기면 10곡 판정에 10판 이상 걸리고, 곡명이 안 보이면 피드백을 파일명으로 못 돌려준다

**6. 씬 조립**
- `CoreSceneBuilder`: `Managers`에 `WorldAudioManager` 추가 + `BgmLibrary.asset` 주입 +
  **`AudioListener` 오브젝트 1개** 생성 (선례 D-021 "태양은 Core 소유"와 동형)
- `DistrictSceneBuilder`: `AudioListener` 제거 + 주석 갱신 (씬당 2개면 Unity 경고)
- ⚠ 씬·프리팹 파일 직접 편집 없음 — 빌더 코드 수정으로만 표현 (Git 경계 준수)

**7. 문서**
- `BOM.md` §8 정정 4건 (아래 D-040)
- `decisions.md`에 D-039~D-042 append

### 수용기준

**셀프검증 3종 (CODE_RULES §8)**
1. `unity-cli editor refresh --compile` 통과
2. `unity-cli console --type error,warning` **0건** (`AudioListener` 중복 경고 포함 0)
3. `unity-cli editor play --wait` 후 관찰 — 아래 실측치를 **보고에 수치로** 적는다

**Play 모드 실측 항목**
- `exec`로 `AudioSource.isPlaying == true` + `clip.name` 확인 → 낮 풀의 곡인지 대조
- `WorldDayNightManager.SetTime(16, 59)` 강제 → 17:00 전이 시 **3초 이내 밤곡으로 교체**되고
  전환 중 두 소스의 볼륨이 교차하는 것을 실측 (한쪽 ↓·다른 쪽 ↑ 수치)
- `N`키 순회 시 화면 곡명이 갱신되는 것 확인
- Core 씬 외 씬(Home/Camp)에서도 소리가 유지되는지 확인 — 리스너 Core 이전의 목적

**예산 실측 (필수 보고 항목)**
- 임포트 후 각 클립의 **실제 압축 크기**를 집계해 보고.
  10곡 총 재생시간 **720초** → 8.4~11.3MB 추정으로 **예산 10MB(SFX 포함)를 초과할 수 있다.**
  특히 `Breezy_Town_Stroll`(180초)이 혼자 3곡 몫이다. 실측치가 컷 판정의 입력이 된다

**하지 않는 것 (YAGNI — CODE_RULES §7)**
- SFX 훅·`AudioMixer`·로우패스 변주 **선제작 금지** (SFX 음원 자체가 없고, 밤 변주는 전용 곡으로 대체됨)
- 루프 이음새 크로스페이드 처리 **이번엔 안 한다** — 컷될 곡에 작업을 낭비하지 않는다. 판정 후 별도 발주
- `bom_id` 리네임 **이번엔 안 한다** — 위와 같은 이유

### 실패 시

```
[BLOCKED] 막힌 것 / 시도한 것 / 필요한 것(결정·정보·연결) / 긴급도
```
검증 조건 완화·기대값 하드코딩으로 통과시키는 것 금지.

### 후속 (이 발주 밖 — 판정 후 별도 건)

| 순서 | 항목 |
|---|---|
| AU-002 | Director 인게임 청취 → 컷·슬롯 확정 → `bom_id` 리네임 → `assets_manifest.md` 등재 → ignore 해제 후 커밋 |
| AU-003 | 루프 이음새 크로스페이드 처리 (도구 필요 — ffmpeg 등) + 볼륨 정규화 |
| AU-004 | SFX — J-1 승인 여부에 따라 4종 또는 11종. 음원 생성부터 |
| — | `docs/TECH_SPEC.md` 오디오 절 신설 (믹스 비율 규격이 부재 — BOM이 참조하는데 대상이 없다) |

### 결과 · 2026-07-21 22:05 (리드 45분 · 재시도 0 · 자가교정 3회)

**셀프검증 3종 통과** — 컴파일 OK · 콘솔 **0건**(클린런 실측) · Play 기대동작 확인.

**관찰 기록 (판정어 아님 — 실측치)**

| 항목 | 관찰 |
|---|---|
| 세션 추첨 | `[BGM] 세션 추첨 · Day=… · Title=… · Night=…` 로그 발화. 재시작 시 Day 선택이 `Breezy`→`Seoul_Alley`→`Sunlit`로 바뀌는 것 관찰 (no-repeat 동작) |
| Main 씬 | `clip=Late_for_Work_8-Bit_Panic` — Title 슬롯 정확 |
| Home 전이 후 | `clip=Sunlit_Seoul_Afternoon` — Day 슬롯으로 교대 |
| 낮→밤 크로스페이드 | `SetTime(16,59)` → 17:00 Evening 진입 시 밤곡 `0.00→0.08→0.16→0.24`, 낮곡 `0.50→0.42→0.34→0.26`, **합 0.50 유지**. 완료 후 밤곡 0.50 단독, 이전 소스 `Stop()`+클립 해제 |
| AudioListener | **1개** (Core 소유) |
| N키 순회 | Day 풀 3곡 정확 순환 — `Sunlit → Breezy → Seoul_Alley → Sunlit` |
| B키 슬롯 전환 | `Day → Night → Title → Day`, 각 슬롯의 추첨곡 재생 |

**예산 실측 (핵심 보고 항목)**

q0.70 임포트 시 **20.61MB — 예산 10MB의 2배 초과**. Vorbis q70이 실제 **~256kbps**로 인코딩된다.
**q0.30(~118kbps)으로 조정해 10곡 10.04MB**(51% 감축). 게임 BGM 표준 대역이라 품질 손실은 수용 범위.

| 곡 | 크기 | 비트레이트 |
|---|---|---|
| Breezy_Town_Stroll (180s) | **2.53MB** | 118kbps |
| 나머지 9곡 (각 60s) | 0.78~0.88MB | 109~124kbps |
| **합계** | **10.04MB** | — |

`Breezy_Town_Stroll` 혼자 **전체의 25%**를 먹는다 — 컷 판정의 최대 변수.
현 상태는 예산을 정확히 소진하므로 **SFX 여유가 없다. 컷은 선택이 아니라 필수.**

**자가교정 3회 (실측이 설계를 뒤집은 지점)**

1. **q70 → q30** — 예산 2배 초과 실측 후 조정 (D-043)
2. **AudioListener 소유권** — Core로 올리자 `Main.unity`와 2개 충돌, 콘텐츠 씬으로 되돌리자 Core 단독
   구간에 0개 경고. → Core 소유 유지 + `Main.unity`의 중복분을 빌더가 정리 (D-041 실행 정정)
3. **`_debugIndex` 미동기** — 추첨된 곡과 커서가 어긋나 첫 N키가 같은 곡을 다시 골랐다.
   `SyncDebugIndex`로 슬롯 전환 시 커서를 현재 곡에 맞춤

**덤으로 해소된 기존 버그**: `[SceneFlow] Main → Main 는 허용되지 않은 전이` —
원인은 `Main.unity` 안에 남아 있던 `CoreBootstrap`이었다([[ai_evidence]]에 "미수정"으로 기록돼 있던 건).

**사람 작업 필요 (다음 세션)**

1. **인게임 청취로 컷·슬롯 판정** — Core 씬 Play → `N`(다음곡)·`B`(슬롯전환), 화면 좌상단에 곡명 표시.
   `Assets/Data/BgmLibrary.asset` 인스펙터에서 슬롯 드롭다운 변경 / 컷은 항목 제거
2. 현 슬롯: **Day 3 · Night 1 · Title 1 · Unsorted 5** (`MAPPING.md` 확정분만 배정, 추정 없음)
3. `Ironic_Stillness`는 원본에서 낮·밤 양쪽에 중복 배치돼 있어 **Unsorted**로 뒀다 — 청취로 확정 필요

**커밋 제외**: `Assets/Audio/BGM/*.wav`(D-042) · `Assets/Scenes/Main.unity`(씬 커밋 금지 — 빌더가 재현)

### 후속 · 2026-07-21 22:40 — 첫 컷 1건 (Director 청취 판정)

`Late_for_Work_8-Bit_Panic` **삭제** — 8비트 사운드로 나머지 곡과 분위기 불일치.
프로젝트·아카이브 양쪽에서 제거 · `BgmLibrary.asset` 항목 제거(10→9).

- **예산 10.04MB → 9.23MB** (9곡)
- **Title 슬롯 공백** — 유일한 Title 곡이었다. 빈 슬롯이면 매니저가 현 재생을 유지하므로
  Main 씬에서는 낮곡이 이어진다(무음 아님). Unsorted 5곡 중 재지정 필요
- 현 배정: **Day 3 · Night 1 · Title 0 · Unsorted 5**
