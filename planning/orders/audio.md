# orders/audio.md — 오디오 발주 대장 (append-only)

> 형식: [guides/distributed-workflow.md](../guides/distributed-workflow.md) §3. 발주·결과 시각은 파일 안에 명시.
> **반입 경로 (2026-07-21 사람 확정)**: 오디오 산출물은 `Assets/_intake/ElevenLabs/{BGM,SFX}/`에 착지
> → 관제가 검역(라이선스 기록·규격)·컷 판정 절차 후 `Assets/Audio/{BGM,SFX}/`로 이동+bom_id 리네임.
> (AU-001의 직행 반입은 개통 특례 — 이후는 이 경로가 표준. CREDITS/manifest 기록은 여전히 입장권)
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
| AU-003 | ~~루프 이음새 크로스페이드~~ **취소(D-046 — 플레이리스트가 해결)** · **볼륨 정규화만** 남음 |
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

---

## AU-002 · 발주 2026-07-21 22:50 → ClaudeCode (청취 도구 확장)

Unsorted 곡이 풀에서 아예 빠져 있어 `N`/`B` 키로 도달 불가 → **청취 판정 자체가 불가능**했다.
`BuildPools`가 Unsorted도 담고 `PickForSession`이 제외하는 구조로 교정(게임 동작 불변) ·
`DebugToggleSlot`이 4슬롯 순회하며 빈 슬롯은 스킵.

### 결과 · 2026-07-21 23:25 (리드 35분 — 시각 추정) · 통과
`B` 순회 `Day → Night → Unsorted → Day`(Title 0곡 스킵) · Unsorted 안 `N`으로 5곡 전량 순회 실측.
커밋 `36e3f3f`.

---

## AU-004 · 발주 2026-07-21 22:45 → ClaudeCode (SFX 필수 3종 · 합성 폴백)

목표: 코어루프가 **소리로 완결**되게 한다. 음원 미확보 상태이므로 `pipelines/audio.md` 폴백 원칙
("전부 불가 → 무음+최소 신디")대로 코드 합성 플레이스홀더를 만들고 이벤트에 건다.

입력·산출: `Assets/Scripts/Editor/SfxSynthGenerator.cs`(신규) · `WorldAudioManager` SFX 확장 ·
빌더 2종 클립 주입 · 산출물 `Assets/Audio/SFX/<bom_id>.wav`

기대:
- JUICE 표에 **이미 승인된 3건만** 연결 — `PackagePickedUp`·`DeliveryCompleted`·`DeliveryFailed`.
  나머지 7종은 **J-1 승인 게이트 대기**라 손대지 않는다
- 합성은 단순 파형 1~2겹 (JUICE "작은 순간 1~2레이어" 준수)
- **파일이 이미 있으면 절대 덮지 않는다** — 실음원을 합성물로 되돌리는 사고 방지
- SFX 전용 `AudioSource` 1개 분리 (BGM 크로스페이드 볼륨에 휘둘리면 안 된다)

수용기준: 컴파일 · 콘솔 0건 · 이벤트 발행 시 원샷 재생 관찰 · 임포트 규격(모노·DecompressOnLoad) 확인

### 결과 · 2026-07-21 23:00 (리드 15분 · 재시도 0)

**셀프검증**: 컴파일 OK · 콘솔 **0건** · Play에서 `RaisePackagePickedUp` 발행 시
SFX 소스가 원샷 재생하는 것 관찰(소스 3개 중 SFX 1 + BGM 1 동시 재생).

| bom_id | 길이 | 채널 | 로드타입 | 크기 |
|---|---|---|---|---|
| `sfx_pickup` | 0.12s | 모노 | DecompressOnLoad q0.70 | 17KB |
| `sfx_delivery_ok` | 0.55s | 〃 | 〃 | 54KB |
| `sfx_late_buzzer` | 0.45s | 〃 | 〃 | 45KB |
| **합계** | | | | **117KB** |

빌더 주입 확인: `_sfxPickup`·`_sfxDeliveryOk`·`_sfxLateBuzzer`·`_library` 4개 참조 전부 non-null.

**미착수 (의도적)**
- `sfx_footstep` — Locomotion 내부 훅이라 Player 도메인 수정이 필요하다. 스텝 케이던스·걷기/달리기
  가중 설계가 별건이므로 분리
- 나머지 7종 — **J-1 승인 게이트**. 승인 없이 만들면 "JUICE 이벤트 밖 SFX 금지" 원칙 위반

**후속**: 실음원 확보 시 `Assets/Audio/SFX/<bom_id>.wav`를 **같은 이름으로 덮어쓰면 끝**이다.
그때 `.gitignore`의 오디오 규칙을 SFX에 한해 풀고 `assets_manifest.md`에 등재한다.

---

## AU-005 · 발주 2026-07-21 23:30 → ClaudeCode (플레이리스트 전환 + 시각 점프 키)

사람 실플레이 판정에서 나온 2건.

**① 슬롯 재배정** — `Breezy_Town_Stroll`(Day→Night) · `Seoul_Pixel_Breeze`(Unsorted→Night).
결과 배정: **Day 2 · Night 3 · Title 0 · Unsorted 4**.

**② 플레이리스트 전환 (D-046)** — 세션당 2곡(각 60s)만 무한 반복되는 체감이 셌다.
곡이 끝나기 `_crossfadeSeconds` 전에 같은 슬롯 다음 곡으로 크로스페이드하도록 변경.
BGM 소스 `loop = false`로 전환, `Crossfade(clip, allowSame)` 추가(곡 1개 슬롯은 자기 자신과 교차 → 매끄러운 루프).
세션 추첨은 **"시작 곡" 선택**으로 의미가 바뀐다(no-repeat 유지).

**③ `T` 키 (D-047)** — `WorldDayNightManager`에 다음 페이즈 경계 점프. 에디터 전용.
`B`(BGM만)와 달리 시각을 옮기므로 조명·별밭·가로등·BGM이 전부 따라온다.

### 결과 · 2026-07-21 23:50 (리드 20분 — 시각 추정) · 통과

- 컴파일 OK · 콘솔 **0건**
- 플레이리스트 실측: `_active.time`을 곡 끝 3.4초 전으로 시크 → `Sunlit_Seoul_Afternoon` →
  **`Seoul_Alley_Reflection`으로 자동 크로스페이드**, 이전 소스 정지·클립 해제 확인
- `T` 키: `DebugPhaseSkip` 존재 확인, `Morning → Day → Evening → Night → 익일 Morning` 순회

**파생 효과**: AU-003의 루프 이음새 항목이 **불요**가 됐다(같은 곡을 이어붙이지 않는다). 볼륨 정규화만 남음.

---

## AU-006 · 발주 2026-07-21 23:55 → ClaudeCode (컷 판정 종료 + 채택분 커밋)

**BGM 청취 판정 종료** — 반입 10곡 → **채택 5곡**.

| 슬롯 | 곡 |
|---|---|
| Day (2) | `Seoul_Alley_Reflection` · `Sunlit_Seoul_Afternoon` |
| Night (3) | `Breezy_Town_Stroll`(180s) · `Seoul_Afternoon_Stroll` · `Seoul_Pixel_Breeze` |
| Title (0) | **공백 — Director 보류.** 빈 슬롯이면 매니저가 현 재생을 유지하므로 Main 씬에서 낮곡이 이어진다 |

**컷 5곡**: `Late_for_Work_8-Bit_Panic`(8비트 분위기 불일치 — 프로젝트·아카이브 삭제) ·
`Ironic_Stillness` · `Pixel_Seoul_Breeze` · `Seoul_Pixel_Boulevard` · `Sunlit_Stroll_in_Seoul`
(미채택 — 프로젝트 제거, 아카이브 보존)

**커밋 처리**
- `.gitignore` 오디오 전면 제외 규칙에 **채택 5곡 부정 패턴(`!`) 예외** 추가.
  검증: 채택분 `추적가능` / 나머지 `IGNORED`
- `assets_manifest.md` — `ElevenLabs BGM INTAKE` 절 신설(**pre-commit 라이선스 게이트 통과 조건**)
- `bom_id` 리네임 **안 함** — 플레이리스트(D-046)로 슬롯당 다곡이라 1:1 대응이 성립하지 않고,
  스왑 계약은 `BgmLibrary.asset`(SO) 참조로 성립한다(BOM §8 개정분)
- 원본 WAV 77MB(git) · 빌드 압축 후 **약 5.6MB**

### 플레이리스트 동작 실측 (사람 관찰 "2곡만 반복" 대응)

한 슬롯에 머문 채 관측: `Sunlit_Seoul_Afternoon` `t=4.2 → 21.2 → 38.0 → 55.2`(초) 진행 후
**`Seoul_Alley_Reflection`으로 자동 전환** — 플레이리스트 정상.

"2곡만 반복"으로 보인 원인: 곡 전환은 **곡 끝 `_crossfadeSeconds`(3초) 전에만** 일어난다.
`B`(슬롯 전환)나 `T`(페이즈 점프)로 자주 오가면 각 슬롯의 **현재 곡**이 다시 재생되므로
곡이 안 바뀌는 것처럼 들린다. 곡 순환을 보려면 **한 슬롯에 60초 이상 머물러야** 한다.

---

### 결과 · 2026-07-21 23:58 (사후 기록 — 판정·커밋과 동시)

## AU-007 · 발주 2026-07-21 23:56 → 정수 (진짜 SFX 음원 생성 — ElevenLabs)

목표: 현재 합성 폴백(사각파)으로 도는 SFX를 진짜 음원으로 교체 + 미구현 8종 신규 — J-1 승인분(D-018) 11종 완성.

입력:
- 목록·트리거 매핑: [[BOM]] §8 SFX 표 (11종 — WorldEvents 트리거까지 정의돼 있음)
- 우선순위: ① 교체 3종(sfx_pickup·sfx_delivery_ok·sfx_late_buzzer — 합성본이 자리 지킴) ② 신규 필수(sfx_deadline_warn·sfx_rhythm_hit/_miss·sfx_phone_ring) ③ 권장(sfx_dialogue_blip 교체·sfx_drink) ④ 선택(sfx_scene_whoosh·amb_night)
- 규격: 짧게(0.1~1.5s · amb_night만 루프) · 총 SFX 예산은 BGM 포함 10MB 안 — Vorbis q70 유지(D-043)

기대:
1. ElevenLabs SFX 생성 → **`Assets/_intake/ElevenLabs/SFX/`에 착지** (파일명 = bom_id 정확히 — 예: `sfx_deadline_warn.wav`)
2. `Assets/Audio/CREDITS.md`에 생성 기록 append (프롬프트·플랜·권리)
3. 착지 후 관제에 통보 — 검역·계약 경로 이동·이벤트 배선 확인은 관제가 처리
   (또는 카드 2 절차로 직접 이동+배선까지 하고 PR — 그 경우 2축 검수로 받음)

수용기준: 파일명=bom_id 일치 · CREDITS 기록 · 예산 내 · (배선까지 한 경우) 해당 이벤트 발화 시 재생 실측.

### 결과 · 2026-07-22 21:14 (정수 공장 — AU-008과 동세션 일괄, 리드 45분/총 19종)

- **12종 생성·착지 완료** (교체 3 + footstep + 신규 필수 4 + 권장 2 + 선택 2): `Assets/_intake/ElevenLabs/SFX/<bom_id>.wav` — 파일명=bom_id 전량 일치.
- 생성 = 반입된 wt1 파이프라인(`elevenlabs_client.py gen` · `pcm_44100`→WAV · seed 전건 기록 → 복원 가능). 프롬프트 원본 = `scripts/audio/prompts/<bom_id>.md` (GAME-SFX-RULES 준수 — 금칙어 검사 통과).
- CREDITS.md 기록 완료 (계정·권리·seed·프롬프트 SHA1 표 — 실격 사유 영역 이행).
- 교체 3종(pickup·delivery_ok·late_buzzer)은 로컬 `Assets/Audio/SFX/` 배치로 **스왑 계약 발동 실증** — Play에서 클립 길이가 합성본(0.12~0.55s)→실음원(1.48s)으로 교체 확인.
- 후공정(앞 무음 트림·정규화)·컷 판정 = **사람 귀 몫** — D-042대로 `Assets/Audio/` 사본은 미커밋, `_intake`만 커밋. 관제 통보 절차(카드 1) 선택.
- 예산: 19종 WAV 총 3.9MB(원본) — Vorbis q70 변환 후 대폭 축소 예상, 승격 시 파이프라인 예산 게이트가 재검.

---

## AU-008 · 2026-07-22 00:10 → ClaudeCode (슬롯 재진입 시 곡 전환)

> 번호 충돌 교정: 관제 AU-007(SFX 실음원 발주 · 2026-07-21 23:56)이 먼저 발주돼
> 그쪽이 AU-007을 유지하고 이 절을 AU-008로 내렸다.

사람이 `T`(페이즈 점프)로 낮↔밤을 오가며 확인하는데 **2곡만 반복**된다는 판정 2회.

원인: 플레이리스트는 **곡 끝 `_crossfadeSeconds` 전에만** 넘어간다. 슬롯을 오갈 때는
`_picked[slot]`의 현재 곡을 그대로 다시 재생하므로 곡이 안 바뀐다.

교정(D-058 — 머지 시 번호 재조정, 구 표기 D-048): `SelectForSlot(slot)` 신설 — **세션 첫 진입은 추첨분 그대로**(no-repeat 보존),
**재진입부터 풀의 다음 곡**. `ApplySlot`과 `DebugToggleSlot`이 같은 규칙을 쓴다.
`Morning→Day` 같은 **동일 슬롯 내 전이는 곡을 바꾸지 않는다**(낮 중간에 음악이 끊기면 안 됨).

### 결과 · 통과

컴파일 OK · 콘솔 0건. `T` 6회 실측:

```
Morning  Seoul_Alley_Reflection   ← 추첨분
Day      Seoul_Alley_Reflection   ← 같은 Day 슬롯 → 유지(정상)
Evening  Seoul_Afternoon_Stroll   ← Night 첫 진입
Morning  Sunlit_Seoul_Afternoon   ← Day 재진입 → 다음 곡
Evening  Seoul_Pixel_Breeze       ← Night 재진입 → 다음 곡
Morning  Seoul_Alley_Reflection   ← Day 순환
```

채택 5곡이 슬롯 전환만으로 전부 노출된다.

---

## AU-008 · 발주 2026-07-22 19:10 → 정수 (신기능 SFX 일괄 + 훅 연결)

목표: S-019~021로 추가된 기능들이 전부 무음 — SFX 7종을 제작(ElevenLabs)해 반입하고 이벤트 훅을 연결한다. (AU-007 SFX 11종은 기존 발주 그대로 유효 — 이번 세션에 같이 처리 권장.)

입력:
- `Assets/Scripts/Managers/WorldAudioManager.cs` — SFX 훅 패턴(OnPackagePickedUp → PlaySfx) 참조. 새 구독 추가 시 OnEnable/OnDisable 짝.
- 대상 이벤트: 상자 파손(BoxDurability.Explode — 이벤트 없음, `PackageDestroyed` 신설 필요 시 §9.5 로그 동반) · 자판기(VendingMachine — 결제/배출) · 던지기(PlayerStatusManager.ThrowCarryTowardsMouse) · `BarcodeScanned` · `DebtIncreased` · 코인 매수/매도(WorldDebtManager) · 폰 개폐(PhoneView.OnToggle).
- 반입: `Assets/_intake/ElevenLabs/SFX/` · **파일명=bom_id**(sfx_box_break·sfx_vending·sfx_throw·sfx_barcode·sfx_penalty·sfx_coin·sfx_phone) · CREDITS.md 즉시 기록(실격 사유 영역).

기대: 각 이벤트 발생 시 대응 SFX 1회 재생. 이벤트가 없는 지점(자판기·던지기·폰)은 컴포넌트가 로컬 AudioSource로 직접 재생해도 무방(2D·SFX 볼륨 준수) — WorldEvents 신설은 저빈도·경계 통신일 때만.

수용기준: ① 컴파일 ② 콘솔 0 ③ Play에서 파손·자판기 E·던지기·스캔·폰 Tab 각각 소리 확인(관찰 기록) ④ CREDITS 기록 완비.

실패시: [BLOCKED]. ⚠ PhoneView·PickupBox는 관제가 활발히 수정 중 — pull 최신화 후 시작하고, 해당 파일 수정은 최소 diff로.

### 결과 · 2026-07-22 21:14 (리드 45분 · 정수 공장 — AU-007 동세션 일괄)

- **신규 7종 생성·착지·배선 완료**: box_break·vending·throw·barcode·penalty·coin·phone (파일명=bom_id · CREDITS 기록 ④ 완비).
- 배선 구조:
  - 이벤트 있는 지점 = WorldAudioManager 구독 3건: `BarcodeScanned`→barcode · `DebtIncreased`→penalty · **`PackageDestroyed`(신설)**→box_break. 신설 이벤트는 §9.5 로그 동반(저빈도·경계 통신 — BoxDurability.Explode 발행, 페이로드 없음: 상자는 주문을 모른다).
  - 이벤트 없는 지점 = Instance 명령 API 4건(`PlayVendingSfx/ThrowSfx/CoinSfx/PhoneToggleSfx`) — 컴포넌트가 클립을 들지 않게 해 배선을 CoreSceneBuilder 한 곳으로 모음(발주서의 "로컬 AudioSource" 취지를 볼륨·2D 일관성 위해 중앙 소스로 충족).
  - 호출 지점: VendingMachine.DispenseDrink(결제·명중 공용) · PlayerStatusManager.ThrowCarryTowardsMouse · PhoneView OnToggle/매수성공/매도성공 (PhoneView는 최소 diff 3줄 — 발주 경고 준수).
- 검증: ① 컴파일 통과 ② 콘솔 에러·워닝 0 ③ Play 실측 — 7종 전 트리거 발화 시 `_sfxSource.isPlaying=True` + 클립 주입 10종 전부 실음원 길이 확인(`BoxBreak=1.00s·Barcode=0.48s·Penalty=0.80s·Vending=1.20s·Throw=0.60s·Coin=0.60s·Phone=0.48s`). **소리 자체의 귀 판정은 사람 몫** — 관제 청취 요청.
- BOM §8 미등재 7종 — 발주서(본 절)가 근거. BOM·JUICE 행 추가는 동결 게이트라 관제 위임.

### 결과 2세대 · 2026-07-22 21:35 (사람 판정 반영 — 볼륨·톤 개정)

- 1세대 사람 판정: **음량 낮음 · 과장됨 · 8bit 부족** (3축).
- 대응: ① 스타일 앵커 개정 `retro pixel-art` → `8-bit, chiptune sound chip, square wave and noise channel, subtle and understated` (prompt_builder SFX_STYLE_EN — 과장 억제 포함) ② 태그 19종 8bit 재서술 ③ 후처리 2단 신설: 피크 -1dBFS 정규화 → **RMS -14dB 부스트**(클립 ≤1% 자동 감쇠 · amb_night는 배경이라 피크만).
- 절차: 대표 4종(pickup·box_break·coin·barcode) 샘플 → 사람 청취 2회(1차 "볼륨만 올려줘" → RMS 부스트 후 "좋네" 승인) → 잔여 15종 일괄 재생성·처리·재착지.
- RMS 실측(부스트 전): coin **-26dB** · drink **-33dB** · throw -28 · rhythm_hit -25 — "음량 낮음" 지적 정량 확인.
- 부산물 실측 2건: ① ElevenLabs SFX 프롬프트 **450자 상한**(API 400 — 앵커 축약으로 해소, 조립기 주석) ② `prompt_builder build`가 `--length` 생략 시 일부 기본값 2.0s로 리셋 — 13종이 2.0s로 생성됨(여분 꼬리 = 컷 판정 후 트림 대상, md 요청 길이는 원복).
- CREDITS 2세대 표 갱신(신규 seed 전건). 1세대 seed는 git 이력 보존.

### 결과 3세대 · 2026-07-22 21:55 (사람 판정 — 2세대 전량 기각 → VA-11 HALL-A 참조 재생성)

- 2세대(8bit) 사람 판정: **전량 기각**. 참조 지정 = VA-11 HALL-A (Cyberpunk Bartender Action).
- 앵커 3차 개정 — 게임명 대신 음향 특성 번역(규칙 원칙): `soft rounded FM synth tones · warm analog character · smooth attack · subtle and cozy`. 태그 19종 소프트 신스 재서술.
- 절차: 샘플 4종(pickup·box_break·coin·barcode) 사람 승인 → 잔여 15종 일괄. 이번엔 **전건 --length 명시** — 2세대의 2.0s 리셋 실수 재발 방지, 19종 전부 요청 길이 일치(0.48~5.0s).
- 후처리 동일(피크 -1dB → RMS -14dB·클립 ≤1%·amb_night 피크만). 재착지 완료(_intake + 로컬 스왑).
- 구세대 파일은 동일 파일명 덮어쓰기로 제거(git 이력에만 보존). CREDITS 3세대 표 갱신.
- ⚠ 규칙 문서 후속: GAME-SFX-RULES §1 스타일 앵커가 "retro pixel-art"로 남아 있음 — 3세대 앵커와 불일치, 개정은 Director 문서라 위임(PR #9 참고).

### 결과 4·5세대 · 2026-07-22 22:15 (스타일 탐색 종결 — Director 스펙 직지정)

- 4세대: JRPG 참조(밝은 벨·차임) 샘플 4종 → 기각 (미전개, 크레딧 4건).
- **5세대 확정**: Director가 프롬프트 스펙 직지정 — `lo-fi 8-bit text scroll beep, gritty square wave, bit-crushed 8-bit 11kHz, 40ms, punchy attack, mono` (dialogue_blip 사양 원문).
- 앵커 이식: `lo-fi 8-bit · gritty square wave and noise channel · bit-crushed · punchy attack · mono`. 태그 19종 재서술 후 전량 재생성.
- **비트크러시 후처리 신설**(bitcrush.py): 프롬프트 의존 대신 파형 보장 — 선두 무음 트림(펀치 어택) → 11kHz 홀드 다운샘플 → 8bit 양자화 → 모노 강제 → 피크 -1dB. dialogue_blip만 40ms 컷(+5ms 페이드). 이후 RMS -14dB 부스트(amb_night 제외).
- 실수 1건 자가 발견·교정: sfx_phone이 생성 루프에서 누락돼 3세대본 잔존 → 보완 생성(seed 731912038).
- 세대 이력 5회 — 스타일 탐색 비용 크레딧 ~66건. 샘플 우선 절차가 4세대 전개분 15건을 절약함.

### 결과 6세대 · 2026-07-22 22:40 (5세대 기각 → 동물의 숲 참조)

- 앵커 6차: `cozy cute toy-like · soft wooden marimba · rounded synth plucks · playful pitch bends · light and bouncy` (음향 특성 번역). **비트크러시 후처리 끔** — 토이 톤과 상극.
- 절차: 샘플 4종 사람 승인("좋네") → 잔여 15종 일괄. 태그 19종 AC 재서술(코믹 실패음·토이 노크·마림바 트릴 등). 전건 --length 명시.
- 후처리: 피크 -1dB → RMS -14dB(amb_night 피크만). dialogue_blip 40ms 컷은 5세대 스펙 전용이라 미적용(0.5s — 트림은 판정 후).
- 재착지·CREDITS 6세대 표 완료. 세대 누적 6회 · 크레딧 총 ~85건 — 샘플 우선 절차 유지로 기각 세대 전개 손실 2회 방지(4·기타).

### 결과 7 · 2026-07-22 23:20 (6세대 사람 청취 판정 통과 → 승격)

- **Director 청취 판정: 19종 통과** ("검증결과 괜찮네") — 판정 도구 = 플레이 체크리스트(인게임 트리거 11종 동선 + 미배선 8종 exec 재생·amb_night 루프 청취, GAME-SFX-RULES §6 5축 기준).
- 승격 실행: origin/main 병합(충돌 0 — merge-tree 사전 검사 일치) → `Assets/Audio/SFX/` 19종을 1세대→6세대 교체(관제 ignore 해제 커밋 승계 · **main .meta 보존 = guid 안정**) → 해시 19/19 = `_intake` 일치 실측.
- 병합 후 재컴파일 통과 · 콘솔 에러 0 (워닝 2건 = SceneFlowUIBuilder CS0618, main pull분 기존).
- 배선 현황 실측(체크리스트 작성 중 확인): **11종 인게임 배선** (WorldAudioManager 10 + DialogueView blip) · **8종 미배선**(deadline_warn·phone_ring·rhythm_hit/miss·scene_whoosh·footstep·drink·amb_night) — AU-007 카드1 선택분, 배선은 관제 몫 유지.
- R16 잔여 = 관제 ③ BOM §8 신규 7종 행 추가+JUICE 대응 ④ GAME-SFX-RULES §1 앵커 개정(동결 게이트 문서 — 공장 권한 밖).
- 반입 PR: #11 (2~6세대 델타 + 본 승격 커밋).

---

## AU-009 · 발주 2026-07-22 23:35 → 정수 (미배선 SFX 8종 배선 — Director 세션 내 승인)

목표: 6세대 통과 판정 후 잔여 미배선 8종을 인게임 트리거에 연결 — 19종 전체가 플레이 중 울리게 한다.

입력:
- 미배선 8종: deadline_warn·phone_ring·rhythm_hit·rhythm_miss·scene_whoosh·footstep·drink·amb_night (AU-007 카드1 잔여 — R16 부기를 Director가 공장으로 재발주).
- 기존 패턴: WorldAudioManager 구독(저빈도 이벤트) / Instance 명령 API(이벤트 없는 지점) — AU-008 선례.
- 배선 설계:
  - 구독 3: `DeadlineWarned`→warn · `PhoneRang`→ring · `SceneTransitionStarted`→whoosh (전부 기존 저빈도 경계 이벤트).
  - amb_night: 기존 `OnDayPhaseChanged` 확장 — Evening·Night 진입 시 전용 루프 소스 재생, 낮·타이틀 정지.
  - Instance API 4: `PlayRhythmHitSfx/PlayRhythmMissSfx`(MinigameRhythmView 판정 지점 — 노트당 1회) · `PlayDrinkSfx`(EnergyDrinkPickup.Interact) · `PlayFootstepSfx`(PlayerLocomotionManager 보폭 누적 — 고빈도라 이벤트 금지, PlayThrowSfx 선례).
  - CoreSceneBuilder SetField 8건 추가 + Core 씬 재조립.

수용기준: ① 컴파일 ② 콘솔 0 ③ Play — 이동 발소리·T 시각점프 amb 루프 on/off·씬 전환 whoosh·전화 ring·리듬 hit/miss·드링크·마감 warn 각 발화 실측 ④ 감각값(보폭·amb 볼륨) [SerializeField] 노출.

실패시: [BLOCKED].

### 결과 · 2026-07-22 23:55 (리드 20분 · 정수 공장)

- **8종 배선 완료** — 19종 전체가 인게임 트리거 보유.
  - 구독 3 (WorldAudioManager · OnEnable/OnDisable 짝): `DeadlineWarned`→warn · `PhoneRang`→ring · `SceneTransitionStarted`→whoosh.
  - amb_night: 전용 루프 소스(`_ambSource`) 신설 — Evening·Night 재생 / Morning·Day 정지 / **타이틀 씬 억제**. `_ambVolume=0.35` [SerializeField].
  - Instance API 4: RhythmHit/RhythmMiss(MinigameRhythmView 판정 3지점 — 정타·오타·타임아웃) · Drink(EnergyDrinkPickup.Interact) · Footstep(PlayerLocomotionManager 보폭 누적 — `_footstepStride=1.4m` [SerializeField], 접지+이동 시만, 정지 시 리셋).
- CoreSceneBuilder SetField 8건 + Core 재조립 — **씬 YAML guid 8/8 검증** (⚠ 실측: S-022 메뉴 재편으로 경로가 `DontLate/Build/Core Scene` — 구경로 ExecuteMenuItem은 조용히 실패, 반환값 확인 필수).
- 검증: ① 컴파일 통과 ② 콘솔 에러 0 (워닝 2건 CS0618 = main pull분 기존 · "Creating missing PlayerEffectsManager" 1건 = S-023 프리팹 미부착 기존 — AU-009 범위 외) ③ Play 실측 — 동일 프레임 exec: warn/ring/whoosh/hit/miss/drink/foot 7종 발화 `isPlaying=True` + amb 4분기(밤 on·아침 stop·저녁 on·타이틀 억제) 전부 기대 일치. 클립 주입 8/8 실음원 길이(0.48~5.00s).
- 발소리 실걸음·귀 판정 = 사람 몫 (플레이 시 자동 청취됨).


## AU-010 · 발주 2026-07-23 20:21 → 정수 공장 (Director 세션 내 승인 — AskUserQuestion 선택)

목표: S-030~S-034 신규 기능의 무음 지점을 채워 게임플레이 전 구간이 청각 피드백을 갖는다.

배경 (코드 실측 2026-07-23):
- `DebtSettled` 이벤트 발행됨(`WorldEvents.cs:159`)이나 정산 요약음 부재 — 하루의 마침표가 무음.
- S-034 `SettleDeliveries`가 건별 `DeliveryCompleted`/`DeliveryFailed`를 같은 프레임에 N회 Raise
  → 기존 배선(sfx_delivery_ok·sfx_late_buzzer)이 같은 프레임 N중첩 (음량 스파이크).
- S-031 가구 배치(확정·R회전·ESC취소·집기)·벽지/바닥 순환·전화 받기/거절 — 전부 무음.

입력:
- 신규 생성 4종 (6세대 토이 톤 앵커 · GAME-SFX-RULES 준수 · 전건 --length 명시):
  - `sfx_settle_ok` (1.5s) — 정산 요약 성공 (전건 성공 시). 상행 계열.
  - `sfx_settle_bad` (1.5s) — 정산 요약 실패 포함 (FailCount>0). settle_ok와 같은 음색 계열, 하행 대비 (쌍 규칙 §2).
  - `sfx_furniture_place` (0.6s) — 가구 배치 확정. 나무 톡 놓기.
  - `sfx_ui_tick` (0.3s) — 공용 UI 틱. 연타 내성(dry) 필수.
- 후처리: 피크 -1dB → RMS -14dB (6세대 표준 · 비트크러시 없음). 앞 무음 트림.
- 반입: `Assets/_intake/ElevenLabs/SFX/` + `Assets/Audio/SFX/` + CREDITS append. BOM §8 신규 행은 관제 몫(R16 ③에 4종 합류 요청).

배선 설계:
- WorldAudioManager: [SerializeField] 4필드 + Instance API 4종(PlaySettleOkSfx/PlaySettleBadSfx/PlayFurniturePlaceSfx/PlayUiTickSfx)
  + **PlaySfx 동일 프레임 클립별 1회 가드** (정산 N중첩 수리 — 근본 원인 처방).
- SettlementView.Open: FailCount>0 ? SettleBad : SettleOk (판정 재료가 뷰에만 있음 — MinigameRhythmView 선례).
- HomeFurniturePlacer: 확정→FurniturePlace · R회전/ESC취소/집기→UiTick.
- PhoneView: 벽지/바닥 순환→UiTick · 전화 받기/거절→PlayPhoneToggleSfx(기존 API 재사용 — 신규 에셋 0).
- CoreSceneBuilder SetField 4건 + Core 재조립 + 씬 YAML guid 4/4 검증 (S-022 함정: 메뉴 경로 반환값 확인).

수용기준: 재컴파일 통과 · 콘솔 0 · EditMode 테스트 green · Play 실측(정산 성공/실패 각 발화 + 건별음 중첩 1회로 수렴
· 가구 확정/회전/취소/집기 · 벽지/바닥 틱 · 받기/거절 토글) · 클립 주입 4/4 · Director 청취 판정.

부수 발견 (수정 않음 — 관제 판단 요청): `SettleDeliveries` 실패 경로에서 `lateCount` 이중 증가 —
L129 직접 ++ 후 L130 Raise가 자기 구독 핸들러(L144 OnDeliveryFailed)를 타고 다시 ++.

실패 시: [BLOCKED].

### 결과 · 2026-07-23 20:45 (리드 24분 · 정수 공장)

- **신규 4종 생성·반입 완료** (6세대 토이 톤 · seed CREDITS 기재 · 후처리 트림→피크-1dB→RMS-14dB):
  settle_ok/bad는 같은 마림바 계열 상행/하행 쌍(규칙 §2). ui_tick은 API 하한 0.5s 생성 후 0.3s 트림.
  착지 `_intake/ElevenLabs/SFX/` + `Audio/SFX/` 양쪽 · CREDITS AU-010 절 append.
- **배선 8지점**: SettlementView(FailCount>0 ? Bad : Ok) · HomeFurniturePlacer 확정→Place, R회전/ESC취소/집기→UiTick
  · PhoneView 벽지/바닥 순환→UiTick, 받기→PhoneToggle(거절은 기존 OnToggle 폐음이 커버 — 이중 재생 회피).
- **동일 프레임 가드**: PlaySfx에 클립별 frameCount 기록 — Play 실측: 같은 프레임 DeliveryCompleted 3회+Failed 2회
  Raise → ok/buzzer 각 1회로 수렴(dict 프레임 일치 확인) · PlaySettleOkSfx 재호출 차단 확인.
- CoreSceneBuilder SetField 4건 + Core 재조립(ExecuteMenuItem 반환 True) → **씬 YAML guid 4/4 검증**.
- 검증: 재컴파일 통과 · EditMode 30/30 green · 콘솔 = 기존 S-023 워닝 1건뿐(범위 외) · Play 실측
  클립 주입 4/4(이름·길이 일치 1.48/1.48/0.55/0.30s) · Instance API 4종 발화 isPlaying=True.
- 감각 판정 잔여: 4종 청취·인게임 체감(가구 배치 마우스 흐름은 시뮬 불가 — S-031 선례) = Director 몫.
- 부수 발견(발주서 기재): SettleDeliveries lateCount 이중 증가 — 관제 판단 대기.

### 결과 2차 · 2026-07-23 20:55 (1차 청취 기각 → 재생성)

- Director 청취 판정: 1차 4종 기각 ("맥 빠짐"). 원인 진단 — 승격 19종은 짧은 명사구+음형 개수(two-note·three quick)
  +에너지 단어(cheerful·bright·bouncy·sparkle) 패턴인데, 1차는 장면 서술형+무기력 단어(satisfied·deflated·gentle placement).
- 2차: 승격 프롬프트 패턴 모사로 재작성 → 재생성 (seed CREDITS 2차 표 기재, 1차는 git 이력 보존).
- 음량 실측: 1차 furniture -16.5/tick -16.4dB → 2차 전종 -14.0~-15.4dB (승격 19종 -14.2~-15.5 대역 정합).
- 같은 파일명 교체(guid 불변 — 코드·씬 재작업 0) · 재임포트 · 콘솔 0.
- 잔여: Director 재청취 판정.

### 결과 3차 · 2026-07-23 21:03 (재청취 "똑같다" → 원인 규명 + 지목 2종 교체)

- Director 재청취 관찰 "걷는 소리·집으로 버튼 이전과 동일" → 해시 대조로 사실 확정:
  ① AU-010 신규 4종은 2차에서 실제 재생성됨(1차↔2차 MD5 전부 상이) ② footstep·scene_whoosh는
  **AU-010 스코프 밖이라 미교체** — PR #11 승격본 그대로 = "똑같다"는 정확한 관찰.
- 기각 범위 재확정 (Director 선택): 전량 재탐색 아님 — **인게임에서 거슬리는 것 지목 방식**.
- 지목 2종 재생성: 기존 프롬프트가 19종 중 최약체(soft·gentle·light 무기력 3연발)임을 확인 →
  에너지 패턴 재작성(bouncy hop·swooping sweep) → 후처리 → 동일 파일명 교체(guid 불변).
  RMS: footstep -15.6dB(원본 극저음량 gain x34.85) · whoosh -14.0dB.
- 재임포트 콘솔 0. 잔여: Director 재청취 (걷기·씬 전환 + 정산음 단독 확인 — whoosh와 겹쳐 들릴 수 있음).
  추가 지목 나오면 같은 절차로 즉시 교체.
---

## AU-011 · 발주 2026-07-23 20:59 → 정수 (구역 앰비언스 2종 + 지도 앱 SFX 3종)

> ⚠ 번호 재조정 (병합 시 공장): 원문은 AU-010으로 발주됐으나 공장 세션이 20:21에 같은 번호를 선점
> (Director 세션 내 승인 · origin push·PR #12 선행). S-028→S-029 선례대로 후발분을 AU-011로 재조정.

요구 (6세대 동숲 토이 톤 규격 — GAME-SFX-RULES·기존 후처리 체인 그대로):
- `amb_villatown` — 빌라촌 낮 골목 (새소리·먼 오토바이·생활 소음, 루프 60s±)
- `amb_foodalley` — 먹자골목 밤 (왁자지껄 웅성·지글지글, 루프 60s±)
- `sfx_map_pin` · `sfx_map_route` · `sfx_map_depart` — 지도 앱 (핀 탭·경로 표시·출발 확정, 0.2~0.6s)

수용기준: 파일명=bom_id · _intake→승격 해시 일치 · CREDITS 기록 · 배선은 S-035/036과 맞물려 정수 판단(앰비언스는 WorldAudioManager amb 채널 확장).

### AU-010 관제 검수 · 2026-07-23 21:21 (PR #12 머지)
- 검수: 경계(오디오+배선 5파일) ○ · intake↔승격 해시 23/23 ○ · 배선 패턴(Instance 명령·정산 상행/하행 분기) ○ · 테스트 30/30 승계 ○. 번호 충돌은 공장 선발(20:21) 유지·관제 후발(20:59)을 **AU-011로 재조정 수용** (S-029 선례).
- 정수 적발 결함 처리: `SettleDeliveries` lateCount 이중 가산(직접++ 후 Raise→자기 핸들러 재가산) — **이벤트 발행 전 cargo 제거로 핸들러 재진입 구조 차단** + 직접 카운트 유지. 회귀 테스트 1케이스 추가, **31/31 green**. (첫 시도인 "핸들러에 위임"은 EditMode에서 OnEnable 미실행이라 기각 — 실측으로 잡음.)
- 재조립·Core 클립 주입 확인(settleOk 1.48s). BOM §8 행 추가는 R16 ③에 4종 합류(신세대 확정 시 일괄).

### 결과 · 2026-07-23 22:14 (리드 대기분 제외 실작업 ~35분 · 정수 공장)

- **5종 생성·착지·배선 완료** (6세대 토이 톤 · seed CREDITS 기재 · `_intake`↔`Audio/SFX` 해시 5/5 일치).
- **발주 편차 — amb 루프 60s± → 5.0s 납품**: ① sound-generation API 실상한 22s ② 파이프라인 SFX 캡 5.0s
  (amb_night 승격 선례) ③ BGM 루트는 시티팝 스타일 앵커가 주입돼 환경음 불가(실측 — 아래 수리 참조).
  5s 루프 반복감은 Director 청취 판정 — 기각 시 파이프라인 캡 상향(5→22s) 재생성 후속 제안.
- **파이프라인 수리 1건**: `bom_audio.fallback()`이 BOM 미등재 `amb_*`를 bgm으로 오분류 —
  BGM 루프 규격(48s 요구)+음악 앵커 주입 사고. `amb_` 접두어 SFX 분류 추가(1줄+주석).
- 후처리(AU-009/010 체인): SFX 3종 = 트림→피크 -1dB→RMS -14dB (pin -17.7dB — 클립 가드 ≤1%로 -14 미달 기록 ·
  route -13.5 부스트 불요 · depart -14.0) / amb 2종 = 피크 -1dB만(루프 이음새 보존 선례).
- 배선:
  - **amb 채널 확장**(WorldAudioManager.UpdateAmbient): District 체류 중 구역 전용 amb가 시간대보다 우선
    (빌라촌→amb_villatown · 먹자골목→amb_foodalley — 구역감 목적, S-035 상수 참조) / 비District는 기존 규칙
    (저녁·밤=amb_night) / 타이틀 무음. `_inDistrict` 플래그 + GameStateSO 참조(빌더 주입).
  - **지도 SFX**: Instance API 3종(PlayMapPin/MapRoute/MapDepartSfx) — PhoneView 핀 탭=pin(+활성 핀은 경로가
    그려지므로 route 동반, 잠금 핀은 pin만)·출발=depart. S-036의 UiTick 임시분 교체.
  - CoreSceneBuilder SetField 6건(클립 5+GameState) + Core 재조립 — **씬 YAML guid 6/6 검증**.
- 검증: 컴파일 ○ · 콘솔 에러·워닝 0 · EditMode 31/31 · Play 실측 — amb 4분기: District(빌라촌)=amb_villatown ·
  District(먹자골목)=amb_foodalley(20:00 밤에도 구역 우선 유지) · Travel 낮=정지 · Travel 밤=amb_night /
  클립 주입 5/5 이름·길이 일치(0.48/0.48/0.60/5.00/5.00s) / PlayMapPinSfx 발화 isPlaying=True.
- 귀 판정 잔여 = Director 몫: 5종 청취 + amb 5s 루프 반복감 + 지도 앱 인게임 조작감.

---

## AU-012 · 발주 2026-07-24 14:27 → 정수 (앰비언스 재생성 — 30s+ × 베리에이션 3종 · D-068)

요구 (님 청취 판정 — "5초 루프 반복감 귀에 거슬림. 못해도 30초에 3가지 바리에이션 필요"):
- `amb_villatown_a/b/c` · `amb_foodalley_a/b/c` — 구역당 3종, **각 루프 ≥30초**
- API 상한(22s) 대응은 공장 재량: 세그먼트 크로스페이드 스티칭(예: 20s×2 → 이음새 무단차) 등 — 파이프라인 캡 5s는 앰비언스 한정 해제
- WorldAudioManager amb 채널: 구역 진입 시 3종 중 추첨, 루프 끝나면 다음 베리에이션 로테이션(같은 곡 연속 금지)

수용기준: 6파일 각 ≥30s·루프 이음새 클릭 노이즈 없음(파형 확인) · 로테이션 실측(연속 상이) · 해시·CREDITS·manifest. 구 amb 2종은 교체 은퇴.

---

## AU-018 · 발주 2026-07-25 01:20 → 정수님 (날씨 연동 오디오 + 액션 SFX 8종 + 플레이 실측 폴리싱)

> ⚠ 번호 안내: 정수님 전 PR의 S-050~S-054는 관제 대장과 번호가 겹쳐 **AU-013~AU-017**로 재조정됨
> ([orders/system.md](system.md) 재번호 주석 참조). 이번 발주부터 AU-018.

요구 (남규님 지시 2026-07-25):

**① 날씨별 앰비언스** — WeatherType 6종(Clear·Cloudy·Rain·Snow·Fog·Heat) 대응.
- 계약: `amb_weather_<type>` (예: amb_weather_rain). 30s+ 루프, D-068 베리에이션 정신 승계.
- 전 종 필수는 아님 — 소리 차이가 체감되는 것(Rain·Snow·Heat 우선)부터. Clear는 기존 구역 앰비언스 겸용 가능(정수님 판단).

**② 날씨별 BGM** — 날씨에 따른 곡/변주 분기.
- 슬롯 구조 확장(BgmLibrary에 날씨 축 추가 등)은 정수님 설계 재량. 최소선: 비/눈 오는 날 무드 변주 1종 이상.

**③ 액션 SFX** (bom_id 계약 · Audio/SFX/):
| bom_id | 내용 |
|---|---|
| `sfx_box_damage` | 박스 HP 닳을 때 (충격·구겨짐) |
| `sfx_box_roll` | 박스가 약하게 구를 때 (저속 굴림 루프 or 짧은 원샷) |
| `sfx_throw` | 던질 때 (기존 합성 placeholder 교체) |
| `sfx_footstep_snow` | 눈 밟는 소리 (적설 시 발소리 스왑 — WorldWeatherManager.HasSnowCover 게이트 활용) |
| `sfx_jump` | 점프 |
| `sfx_land` | 착지 |

**④ 플레이 실측 재량 발주** — 직접 플레이해서 (a) 소리가 **비어 있는** 상호작용에 추가,
(b) **어색한** 기존 소리 폴리싱. 발견 목록을 결과 보고에 명시 (무엇을 왜 바꿨는지).

수용기준: 라이선스 기록(CREDITS.md·manifest) 완비 · 후공정 실측(피크/RMS) 기록 · 배선 지점 명시
(코드 훅이 없는 소리는 훅 필요 위치를 보고에 — 배선은 협의) · 셀프검증 3종 · 브랜치→PR.

실패 시: 막히면 [BLOCKED] — 특히 ② BGM 구조 확장이 커지면 설계만 보고하고 멈춰도 됨.

---

## AU-020 · 발주 2026-07-28 01:35 → 정수님 (교통사고 SFX — 끼익!! 쿵!)

요구 (남규님 지시 S-066 ③): 차에 치일 때 **타이어 스키드(끼익) + 충돌(쿵)** 연속음 1클립.
- 계약: `sfx_car_crash` (wav · Audio/SFX/). 길이 1.0~1.5s 권장 — 스키드 0.5~0.8s → 즉시 임팩트.
- 톤: 기존 SFX 톤과 충돌 없게 — 다만 사고는 예외적으로 **비토이톤(--raw) 허용** (sfx_box_damage 선례).
- 훅은 기시공: WorldAudioManager `_sfxCarCrash` 소켓 — 반입 즉시 자동 배선(CoreSceneBuilder LoadSfx). 클립 없으면 무음.
- 후공정: 피크 -1dB. 라이선스 기록(CREDITS·manifest) 관례대로.

수용기준: District에서 차에 치일 때 1회 재생 · 셀프검증 · 브랜치→PR.

### 결과 (AU-020) · 2026-07-29 (정수 공장 · PR 예정 feature/jjs-sfx-car-crash)

**셀프검증** — 컴파일/임포트 OK · 콘솔 에러·워닝 **0** · 클립 로드 실증(아래).

- 생성: `--no-anchors` 비토이톤(충격음 vs 토이 앵커 충돌 — box_damage 선례). 스키드→임팩트 순서 프롬프트, 요청 1.4s.
  3 take(seed 411312833/923410764/1537929964) → **Director 청취 판정 take1 채택**(1.02s 표준). ⚠ SFX seed 미수용 → 로컬 기록.
- 후공정: stereo→mono → 트림(≤-40dBFS) → 피크 -1.0dB(트랜지언트 peak 한계) → 페이드(in 2ms/out 20ms). 최종 1.02s 모노 · rms -11.9dB.
- 임포트 실측: `sfx_car_crash.wav` len 1.02s · 1ch(forceMono) · 44100 · Vorbis q0.7 · DecompressOnLoad — SFX 규격 정합.
- 배선: 소켓 `_sfxCarCrash` 기시공(S-066 ③) → `TrafficCar` 치임 시 `PlayCarCrashSfx()`. CoreSceneBuilder `LoadSfx("sfx_car_crash")` 경로 resolve 확인(ok 1.02s·mono) → Core 재빌드 시 자동 배선.
- ⚠ 인게임 실발화(District 차 치임 1회 재생)는 소켓 배선이 CoreSceneBuilder에 있어 **Core 재빌드 후** 확인 필요 — 남규 실플레이 최종 권장.
- 라이선스: CREDITS.md + assets_manifest.md LICENSE 표 등재(2026-07-29). 잔여(관제): BOM §8 SFX 행 `sfx_car_crash` 추가.

---

### 결과 · ① 날씨 앰비언스 3종 · 2026-07-27 (정수 공장 · 리드 ~50분)

**셀프검증 3종 통과** — 컴파일 OK · 콘솔 에러·워닝 **0** · Play 실측 아래.

**생성·후공정 (실측치)**
- Rain·Snow·Heat 3종 신규. **사실적 환경음**이라 토이톤 앵커 제거(`--no-anchors`). API 22s 상한을
  넘기려 **22s×2 테이크 등파워 크로스페이드 스티칭 → 심리스 루프 랩**으로 **40s 루프** 제작
  (scratchpad `stitch_amb.py` · D-068 "≥30s·클립 내 무반복" 정신 승계).
- 후공정 = RMS -20dB 타겟 + 피크 -1dB 소프트리밋(3종 라우드니스 일관):
  rain peak -1.7/rms -22.5 · snow peak -1.3/rms -20.5 · heat peak -4.8/rms -20.9 dBFS.
- 루프 이음새 불연속(끝→처음) = 정상 인접 스텝보다 작음(rain -29.9 / snow -56.9 / heat -16.5dB vs 정상 -7.8dB) → 클릭 없음(파형 수치).
- **눈은 본래 조용** — 1차 "정적·먹먹" 태그가 API를 무음(RMS -53)으로 유도 → 바람 중심 태그 재생성(가청 확보).

**배선 (WorldAudioManager.UpdateAmbient 우선순위 확장)**
- 우선순위 **날씨(Rain·Snow·Heat) > 구역(빌라촌·먹자골목) > 시간대(저녁·밤 amb_night)**.
  Clear·Cloudy·Fog는 날씨 클립이 없어 자연히 구역/시간대로 폴백(발주 "Clear는 기존 구역 앰비언스 겸용" 충족).
- `WeatherChanged` 구독 추가(OnEnable/OnDisable 짝) → `_weather` 캐시 → UpdateAmbient.
- CoreSceneBuilder SetField 3건 + Core 재조립 — **씬 YAML guid 3/3 검증**(주입 유실 없음).

**Play 실측 (exec)**
- Rain→`amb_weather_rain` · Snow→`amb_weather_snow` · Heat→`amb_weather_heat` 각 `playing=True`.
- 구역=빌라촌 설정 시: Clear→`amb_villatown`(겸용) · Rain→`amb_weather_rain`(날씨 override) · Cloudy→`amb_villatown`(폴백) → AU-011 구역 동작 보존 확인.

**파이프라인 개선 2건 (관제 검토 요청)**
- `prompt_builder`: SFX 5.0s 캡을 `amb_*` 한정 22s로 상향(AU-012 의도 승계) · `compose_sfx`가 `--no-anchors` 존중(질감·앰비언스 재사용).
- `AudioImportPostprocessor`: `amb_*`(긴 루프)를 DecompressOnLoad→**CompressedInMemory**+저비트레이트(40s×3 RAM 낭비 방지). 기존 amb 3종(night/villa/food)도 동일 적용됨.

**남은 것**
- **① Director 청취 판정** — 3종 인게임 체감(Y키 날씨 순환) + 라우드니스(0.35 볼륨) 적정성. 거슬리면 태그/타겟 조정 재생성.
- **② 날씨 BGM** 미착수 — Director Suno 수동 예정. 필요 시 곡 목록만 제공(생성 금지).
- 잔여 ④ travel_loop·ping·loading_tick·done 보류 유지(연출 부재).
- BOM §8 SFX 행 추가(amb_weather 3종) = 관제 몫(정수는 CREDITS+manifest만 — AU-011 선례).

---

## AU-021 · sfx_fanfare — 개척 해금 팡파레 (발주 2026-07-28 23:23 · 관제)

- **용도**: 정산 화면에서 새 구역 해금/트럭 지급 라인이 찍히는 순간 (S-086 콘페티와 동시).
- **스펙**: 1.5~2.5초 승리 팡파레 — 밝은 브라스/신스 상행 3~5음, 끝에 반짝임. 기존 정산
  상행음(sfx_settle_ok)보다 화려하게. 파일명 = **sfx_fanfare** (Audio/SFX/ — 소켓 자동 스왑).
- 클립 도착 전엔 sfx_settle_ok 폴백으로 재생 중.

---

## AU-022 · sfx_thunder — 천둥 (발주 2026-07-29 00:28 · 관제)

- **용도**: 비·태풍 날씨 중 랜덤 간격 번개 섬광과 동시 재생 (S-088 ⑥).
- **스펙**: 1.5~3초 천둥 — 우르릉 낮은 럼블 + 크랙. 파일명 = **sfx_thunder** (Audio/SFX/ 자동 스왑).
- 클립 도착 전엔 무음(섬광만).

### 결과 (AU-022) · 2026-07-29 (정수 공장 · PR 예정 feature/jjs-sfx-thunder, base=car_crash 스택)

**셀프검증** — 컴파일/임포트 OK · 콘솔 에러·워닝 **0** · 클립 로드 실증.

- **방향 재협의**: 1차 초안(distant rolling rumble 계열) 3 take **Director 전량 기각** → 프롬프트 단계부터 재확인 요청 수용.
  용어 설명 후 **A안 "크랙 선행"** 채택 — 섬광 동기 재생이라 날카로운 어택이 선행해야 번쩍(시각)+쩍(청각)이 한 프레임에 인지(B 딥붐은 저역이라 작은 스피커 약함·시작 뭉근, C 롤링럼블은 날카로움 부재로 섬광과 어긋남).
- 생성: `--no-anchors` 비토이톤. A 프롬프트 3 take(seed 524933213/1946017365/2130492058) → **Director 청취 판정 take2 채택**(1.31s 펀치). ⚠ SFX seed 미수용 → 로컬 기록.
- 후공정: stereo→mono → 트림(≤-45dBFS) → 피크 -1.0dB(크랙 트랜지언트 peak 한계) → 페이드(in 1ms 어택 보존/out 40ms 럼블 꼬리). 최종 1.31s 모노 · rms -12.6dB.
- 임포트 실측: `sfx_thunder.wav` len 1.31s · 1ch(forceMono) · 44100 · Vorbis q0.7 · DecompressOnLoad — SFX 규격 정합.
- 배선: 소켓 `_sfxThunder` 기시공(S-088 ⑥) → `WorldWeatherManager.ThunderFlash()`에서 `PlayThunderSfx()`(섬광과 동시). CoreSceneBuilder `LoadSfx("sfx_thunder")` 경로 resolve 확인(ok 1.31s·mono) → Core 재빌드 시 자동 배선.
- ⚠ 인게임 실발화(비·태풍 중 섬광+천둥 동시)는 CoreSceneBuilder 배선이라 **Core 재빌드 후** 확인 필요 — 남규 실플레이 최종 권장.
- 라이선스: CREDITS.md + assets_manifest.md LICENSE 표 등재(2026-07-29). 잔여(관제): BOM §8 SFX 행 `sfx_thunder` 추가.

---

## AU-023 · 발주 2026-07-29 22:47 → 정수님 (엔딩 전용 BGM 1곡)

- **요구 (남규님)**: 엔딩 시퀀스 전용 BGM.
- 용도: 빚 청산 후 박말순+이웃들 마중 → 작별 → "늦지마→잊지마" 크레딧 (총 ~40초 연출, 루프 재생).
- 톤: 잔잔·따뜻한 이별+감사 — 낮곡(메이저)보다 느리고, 밤곡(마이너)보다 온기. 60~90초 루프.
- **파일명 = bgm_ending** (스왑 계약 — Audio/BGM/에 넣으면 관제가 라이브러리 Ending 슬롯 등재).
- 소켓은 S-107이 기시공 예정 — 클립 도착 즉시 엔딩에서 자동 재생됩니다.

### 결과 (AU-023 · 라이브러리 등재) · 2026-08-01 (정수 공장 · feature/jjs-au023-ending-slot, base=main · Director 지시)

- **잔여였던 "관제 라이브러리 Ending 슬롯 등재" 완료.** 곡 반입(`Fading Into Dawn.wav`)은 2db3acf로 main 도달,
  소켓·배선(S-107)도 main 도달했으나 **BgmLibrary Ending 슬롯이 비어 엔딩 무음**이었다(0801 실측).
- 파일명이 스왑계약 `bgm_ending`이 아닌 Suno 원제 `Fading Into Dawn`이라 자동 등재 불발 → **수동 등재**.
- 작업: `Assets/Data/BgmLibrary.asset`에 `{clip: Fading Into Dawn (guid 515450a4…), slot: Ending}` 1항목 추가
  (코드 변경 0 — SO 데이터 1줄. 에디터 API로 라이브 등재·저장).
- 검증(3종): 컴파일·콘솔 에러/워닝 0 · Play → `DialogueEnded`(BGM 해제) → `EndingStarted` →
  **`CurrentSlot=Ending, CurrentClip=Fading Into Dawn`** 실경로 확인 · `[EVENT] EndingStarted` 로그 발화.
- 이로써 엔딩 시퀀스(빚 청산 → 마중 → 크레딧)에서 엔딩곡 자동 재생. AU-023 종결.

## AU-021 · 발주 2026-07-29 00:40 → 정수 공장 (개척 해금 팡파레 SFX — S-086 소켓 충전 · Director 세션 내 승인)

요구 (남규님 지시 S-086 ②): 정산에서 개척 해금/트럭 지급 라인이 찍히는 순간의 **빵빠레** 1클립.
- 계약: `sfx_fanfare` (wav · Audio/SFX/). 길이 ~2.0s 권장 (상행 런 → 밝은 벨 착지).
- 톤: 큐트 토이톤 유지 — `sfx_settle_ok`(상행 축하음)의 **증폭판**(개척 정점). settle_ok보다 크고 화려하게.
  ⚠ 팡파레는 본질이 짧은 축하 멜로디 스팅 → GAME-SFX-RULES §3 "melody/jingle" 금지의 **의도적 예외**
  (`sfx_scene_whoosh`의 riser 예외 선례). 토이 마림바+벨 계열이라 팔레트 정합.
- 훅 기시공(S-086 · PR #21): WorldAudioManager `_sfxFanfare` 소켓 + `PlayFanfareSfx()`(도착 전 settle_ok
  폴백) + CoreSceneBuilder `LoadSfx("sfx_fanfare")` 배선. 반입 즉시 자동 배선(#21 머지 후).
- 후공정: 트림 → 피크 -1dB (트랜지언트라 peak 한계 예상, RMS는 SFX 타겟). 라이선스 CREDITS+manifest 관례대로.
- ⚠ **프롬프트는 ElevenLabs 전송 전 Director 검토 게이트** (남규님 지시 2026-07-29).

수용기준: 정산 개척 해금에서 1회 재생 · settle_ok보다 큰 축하감 · 셀프검증(임포트 에러 0·청취) · 브랜치→PR.

### 결과 (AU-021) · 2026-07-29 01:10 (정수 공장)

- 프롬프트: 대안 B(토이톤+칩튠 브라스 스탭) 승인 → API 450자 한계로 창작태그 트림(요소 전부 유지).
  전송 태그: `triumphant toy fanfare, quick rising marimba run into a sparkling bell chime and final ding, bright chiptune brass stab, celebratory grand yet cute`.
- 3 take 생성(seed 상이) → Director 청취 판정 **take1 채택**(차분·성김). take2(풀 2s·중밀도)·take3(짧고 조밀·펀치)는 미채택.
- 후공정: 트림 → peak -1dB 정규화(트랜지언트라 peak 한계 · RMS→-14 게인보다 peak→-1 게인이 작아 규칙상 peak 한계 — 단일 게인 무클립) → 8ms 페이드. 최종 0.94s 모노.
- 임포트 실측: `sfx_fanfare.wav` len 0.94s · ch 1(forceMono) · 44100 · Vorbis · DecompressOnLoad — SFX 임포터 규격 정합. 콘솔 에러/워닝 0.
- 라이선스: CREDITS.md + assets_manifest.md LICENSE 표 등재(2026-07-29).
- ⚠ 인게임 재생(PlayFanfareSfx)은 소켓이 S-086(PR #21)에 있어 이 브랜치(off main)에선 미검증 — #21 + AU-021 머지 후 CoreSceneBuilder `LoadSfx("sfx_fanfare")` 자동 배선 시 확인.
- 잔여(관제): BOM §8 SFX 행 `sfx_fanfare` 추가(정수는 CREDITS+manifest만 — AU-011 선례).

---

## AU-024 · 발주 2026-07-31 → 정수 공장 (자발 · 날씨 BGM 문서 Storm 보강 — AU-020 ② 갭)

- **배경**: `planning/audio-weather-bgm-songlist.md`(AU-020 ② 산출물, 2026-07-27)가 WeatherType 7종 중
  **Storm(태풍)만 누락.** 태풍은 어둡고 간헐비+강풍+천둥(AU-022) = 체감 최대 날씨라 문서 철학
  "체감 큰 날씨만"상 오히려 우선 대상인데 빠져 있었다.
- **작업**: 문서에 `bgm_storm` 항목(P2 상단 — Heat/Fog보다 우선 권장) + Suno 프롬프트 초안 1종 추가.
  통합설계의 날씨 집합에 Storm 포함. 문서 표기 AU-018 → AU-020 정정.
- **경계**: 문서만(planning/). BGM 생성 없음(D-055 — Director Suno 수동). 코드·배선 없음.
- **자발 4조건**: 스코프 내(음원 문서 보강) · 이 대장 기록 · [발주] 커밋 선행 · 감각판정(곡 채택)은 Director 몫 명시.

수용기준: Storm 항목·프롬프트가 기존 4곡과 동형(BPM·네거티브·GAME-BGM-RULES 준수) · 브랜치→PR.

### 결과 (AU-024) · 2026-07-31 (정수 공장 · feature/jjs-storm-bgm-doc, base=main)

- `planning/audio-weather-bgm-songlist.md` Storm 보강 4곳: P2 표에 `bgm_storm` 행(체감 최대·P2 최우선) +
  Suno 프롬프트 초안 1종(기존 4곡 **동형** — 78 BPM·네거티브 명시·GAME-BGM-RULES 준수) + 통합설계 날씨집합에
  Storm 포함 + Director 액션에 storm 추가. 프롬프트는 §룰 "no dramatic build-ups" 존중해 "dramatic" 긍정어
  대신 ominous/tense/foreboding로 긴장 표현, 긍정 금칙어(energetic·bright uplifting·catchy hook) 0.
- **발주 오기 정정(투명)**: 위 AU-024 발주 헤더의 "AU-020 ②"는 오기 — 원발주는 **AU-018 ②**(2026-07-25 날씨별 BGM).
  AU-020의 "②"는 car_crash 결과노트가 status로 재언급한 것일 뿐. 따라서 발주 항목 "문서 표기 AU-018→AU-020 정정"은
  **취소**(문서의 AU-018 ② 표기가 이미 정본). 실작업 = Storm 추가만.
- 셀프검증: 문서 산출물(코드·에셋 0) — 컴파일/콘솔/Play 3종 비대상. 렌더·규격 확인으로 갈음.
- 잔여: Director Suno 생성(필수 rain·snow → 권장 storm·heat·fog). 곡 확보 후 임포트·BgmLibrary 등재·WeatherChanged 배선은 별건(통합설계 §대로).

---

## AU-025 · 발주 2026-08-01 19:10 → 정수 공장 (비 날씨 BGM 낮/밤 분리)

요구 (남규 원문 요약): 곡 2개 확보 — 낮=`Rain on the Window.wav`(신규, `_audio_intake` 반입), 밤=`Neon Rain.wav`(기존). 비 오는 날 낮엔 Rain on the Window, 밤엔 Neon Rain이 나오게. Neon Rain은 비-밤 전용으로 분리.

- **배경**: 현재 날씨 BGM은 날씨당 1곡·phase-blind(`WorldAudioManager` 날씨 오버라이드가 시간대 슬롯을 통째로 덮어씀) → 비 오면 낮/밤 무관 Neon Rain 하나. `audio-weather-bgm-songlist.md` §확장 "날씨×시간대 분리"(뒤로 미룬 확장)를 **비(Rain)만** 켠다.
- **작업**: (1) `WorldAudioManager` `_bgmRain`→`_bgmRainDay`/`_bgmRainNight` 분리 + 날씨 BGM 선택을 `_phase` 참조(Evening/Night→Night, else→Day) + `OnDayPhaseChanged`에서도 날씨 BGM 재평가(비 오는 중 낮↔밤 전환 반영). (2) `CoreSceneBuilder` 배선 RainNight=`Neon Rain`/RainDay=`Rain on the Window`. (3) `Rain on the Window.wav` `_audio_intake`→`Assets/Audio/BGM/` 반입(.gitignore allowlist·CREDITS·assets_manifest 등재·루프 이음새 트림 확인).
- **경계**: 눈/폭염/안개는 낮밤 공용 1곡 유지(YAGNI). 씬 본문 미커밋(빌더 정본). 공장→PR까지, main 머지 관제 게이트.

수용기준: Rain 진입 시 Evening/Night면 Neon Rain, Morning/Day면 Rain on the Window 재생 · 비 오는 중 낮↔밤 넘어가면 크로스페이드로 교체 · Snow/Heat/Fog 동작 불변 · 컴파일·콘솔0·Play 실측.
MDA 판정 (D-070): 강화 — 기존 날씨 BGM 몰입을 시간대 축으로 심화(코어 "늦지마" 시간압박 무드 각인 보강). 곡 확보됨 = 저비용. 코어루프 불변.

### 결과 (AU-025) · 2026-08-01 (정수 공장 · feature/jjs-storm-bgm-doc, base=main)

- **코드** `WorldAudioManager`: `_bgmRain`→`_bgmRainDay`/`_bgmRainNight` 분리. 날씨 BGM 선택을 인라인 switch→`RefreshWeatherBgm()` 헬퍼로 추출, `_phase`(Evening·Night→밤곡, else→낮곡) 참조. `OnDayPhaseChanged`도 `RefreshWeatherBgm()` 호출 → 비 오는 중 낮↔밤 넘어가면 곡 교체. Snow/Heat/Fog는 단곡 유지(분기 없음).
- **배선** `CoreSceneBuilder`: `_bgmRainNight`=`Neon Rain`, `_bgmRainDay`=`Rain on the Window`. Core 재빌드 후 `Core.unity`에 guid 주입 확인(RainDay 583a926c·RainNight 5fcdc31e, 구 `_bgmRain` 고아 없음).
- **반입** `Rain on the Window.wav`: `_audio_intake`→`Assets/Audio/BGM/`. **루프 트림** — 아웃트로 페이드아웃 4.1s 검출·제거(152.9→148.6s) + 컷엣지 15ms 마이크로페이드. 꼬리 풀레벨 종료 확인(Neon Rain 기준과 동형, RMS 분석 근거). AudioImportPostprocessor 자동 설정: loadType CompressedInMemory·Vorbis **q0.26·모노**. `.gitignore` allowlist·CREDITS·assets_manifest 등재.
  > ⚠ **정정 (2026-08-05 리베이스)**: 최초 기술은 `q0.30·stereo`였다. main의 임포터가 S-136(WebGL 100MB 예산)으로 **BGM 전량 모노·q0.26**을 강제하게 바뀌었고, 커밋해 둔 `.meta`가 구 규칙 산물이라 Unity를 열면 자동으로 덮어써졌다(실측). 임포터 산출값으로 meta를 갱신하고 기술을 정정한다 — main의 기존 BGM(`Neon Rain`·`Neon Snowfall`)도 이미 모노·q0.26이다.
- **셀프검증 3종**: ① 컴파일 완료 ② 콘솔 에러/워닝 0(컴파일·Play 중) ③ Play 실측 exec — `RainDay=Rain on the Window · RainNight=Neon Rain · RainMorning=Rain on the Window · RainEvening=Neon Rain · Clear(evening)=Seoul_Afternoon_Stroll(밤슬롯 폴백)`. 낮/밤 분리·시간대 전환 교체·날씨 해제 폴백 모두 정상.
- 잔여: main 머지는 관제 게이트([[factory-no-merge]]). 눈/폭염/안개 낮밤 분리는 미착수(YAGNI — 곡 없음).

---

## AU-026 · 발주 2026-08-01 → 정수 공장 (눈 날씨 BGM 낮/밤 분리)

요구 (남규 원문): `Daylight Snowfall.wav`를 눈 내리는 날 **낮 전용** BGM으로 게임에 추가. 밤=`Neon Snowfall.wav`(기존). AU-025(비) 낮/밤 분리와 동형.

- **배경**: AU-025로 비(Rain)만 낮/밤 2곡 분리됨. 눈(Snow)은 아직 `_bgmSnow`(Neon Snowfall) 단곡·phase-blind → 눈 오면 낮/밤 무관 한 곡. `Daylight Snowfall.wav` 확보(`_audio_intake`) → 눈도 시간대 분리.
- **작업**: (1) `WorldAudioManager` `_bgmSnow`→`_bgmSnowDay`/`_bgmSnowNight` 분리 + `RefreshWeatherBgm()` Snow 케이스를 `_phase` 참조(Evening/Night→Night, else→Day). (2) `CoreSceneBuilder` 배선 SnowNight=`Neon Snowfall`/SnowDay=`Daylight Snowfall`. (3) `Daylight Snowfall.wav` `_audio_intake`→`Assets/Audio/BGM/` 반입(.gitignore allowlist·CREDITS·assets_manifest 등재·루프 꼬리 페이드 트림).
- **경계**: 비는 AU-025 그대로, 폭염/안개는 낮밤 공용 1곡 유지(YAGNI). 씬 본문 미커밋(빌더 정본). 공장→PR, main 머지 관제 게이트. AU-025 스택(base=`feature/jjs-au025-rain-bgm` — 같은 메서드 수정).

수용기준: Snow 진입 시 Evening/Night면 Neon Snowfall, Morning/Day면 Daylight Snowfall 재생 · 눈 오는 중 낮↔밤 넘어가면 크로스페이드 교체 · Rain/Heat/Fog 동작 불변 · 컴파일·콘솔0·Play 실측.
MDA 판정 (D-070): 강화 — AU-025와 동형, 날씨 몰입을 시간대 축으로 심화. 곡 확보됨 = 저비용. 코어루프 불변.

### 결과 (AU-026) · 2026-08-01 (정수 공장 · feature/jjs-au026-snow-day-bgm, base=feature/jjs-au025-rain-bgm 스택)

- **코드** `WorldAudioManager`: `_bgmSnow`→`_bgmSnowDay`/`_bgmSnowNight` 분리. `RefreshWeatherBgm()` Snow 케이스를 `night ? _bgmSnowNight : _bgmSnowDay`로(비와 동일 로직). `OnDayPhaseChanged` 재평가 경로는 AU-025가 이미 설치 → 눈 오는 중 낮↔밤 전환 시 곡 교체 자동 적용. Heat/Fog 단곡 유지.
- **배선** `CoreSceneBuilder`: `_bgmSnowNight`=`Neon Snowfall`, `_bgmSnowDay`=`Daylight Snowfall`. Core 재빌드 후 `Core.unity` guid 주입 확인(SnowDay cfdda866·SnowNight 4b92653c, 구 `_bgmSnow` 고아 없음, fileID 8300000 정상 참조).
- **반입** `Daylight Snowfall.wav`: `_audio_intake`→`Assets/Audio/BGM/`. **루프 트림** — 아웃트로 페이드아웃 3.5s 검출·제거(52.8→49.3s, RMS 분석: 49.3s 이후 단조 감쇠→무음) + 컷엣지 15ms 마이크로페이드. AudioImportPostprocessor 자동 설정: loadType CompressedInMemory·Vorbis **q0.26·모노**. `.gitignore` allowlist·CREDITS·assets_manifest 등재.
  > ⚠ **정정 (2026-08-05 리베이스)**: 최초 기술은 `q0.30·stereo`. AU-025와 동일 원인 — main 임포터가 S-136으로 BGM 전량 모노·q0.26을 강제하게 바뀌어 커밋해 둔 meta가 덮어써졌다(실측). 임포터 산출값으로 갱신.
- **셀프검증 3종**: ① 컴파일 완료 ② 콘솔 에러/워닝 0(빌드·Play 중) ③ Play 실측 exec — `SnowMorning=Daylight Snowfall · SnowDay=Daylight Snowfall · SnowEvening=Neon Snowfall · SnowNight=Neon Snowfall · RainDay=Rain on the Window · RainNight=Neon Rain · HeatDay=Midnight Heatwave`. 눈 낮/밤 분리·시간대 교체·비/폭염 회귀 불변 모두 정상.
- 잔여: main 머지는 관제 게이트([[factory-no-merge]]). PR base=AU-025 브랜치(스택) → #30 선머지 후 리베이스/머지. 폭염·안개 낮밤 분리 미착수(YAGNI — 곡 없음).
- **ACCEPT (2026-08-01 · Director)**: 인게임 Play 실측 — 낮/밤 눈 BGM 정상 재생 육안·청음 확인 → **통과**. 머지는 규율대로 관제 대기(공장 미머지). 관제 처리 순서: **PR #30(AU-025) 선머지 → #31(AU-026)** rebase/머지. 재검수 불요(사전 승인됨).


## AU-028 · 발주 2026-08-05 12:49 → 정수 공장 (튜토리얼 미션 성공 SFX)

> ⚠ **번호 정정 (2026-08-05 20:55)**: 최초 AU-025로 채번했으나 정수님이 PR#30에서
> AU-025(비 날씨 BGM 낮/밤 분리)를 2026-08-01에 **선점**하고 있었다. AU-026→AU-027 정정과
> 원인이 같다 — 그 건이 대장에 append되지 않아 다음 번호를 25로 읽은 것이다.
> **선발 유지·후발 재번호** 규칙대로 AU-028로 옮긴다. 코드 주석 참조 4곳도 함께 갱신했다.

요구 (남규님 원문): "sfx에는 튜토리얼 미션 성공 sfx 발주 넣어" (S-162 튜토리얼 미션 카드 UI 연계)

- 파일: `Assets/Audio/SFX/sfx_tutorial_step.wav` (파일명 = bom_id 스왑 계약)
- 쓰임: 튜토리얼 한 단계를 해냈을 때. 카드 배경이 초록으로 바뀌는 그 순간에 1회.
- 톤: **짧고 가볍게, 축하보다 확인에 가깝게** — 9단계 내내 반복되므로 화려하면 금방 피로해진다.
  기존 `sfx_fanfare`(개척 해금)와 겹치면 안 된다. 그건 큰 사건, 이건 작은 확인이다.
- 길이 0.3~0.6초 권장. 상승 2음 정도의 단순 모티프.
- 임포트 규격은 폴더 계약이 자동 처리(`AudioImportPostprocessor` — 모노 q0.40).

수용기준: 파일 존재 · 길이 1초 이내 · 팡파레와 구분되는 절제된 톤 · 라이선스/출처 기록.
연계: 배선은 관제가 S-162에서 처리(파일이 없으면 무음 폴백 — 소켓).

### 결과 (AU-028) · 2026-08-07 03:50 (정수 공장 · 리드 ~55분 실작업 · feature/jjs-au028-tutorial-sfx, base=main)

- **채택 태그**: `two-note ascending confirm chime, soft wooden marimba, crisp and short, bright and light`
  + SFX 토이톤 앵커(마림바·둥근 신스 플럭) · 요청 길이 0.5s.
- **10 take · Director 청취 2라운드**. 1차 3계열 6 take(A 마림바 확인음 / B 글로켄슈필 딩 / C 칩튠 블립)
  발신 → **A 계열 지목** → A 프롬프트를 고정하고 태그 변주 없이 4 take 추가 → **A6 채택**.
  AU-027과 같은 결론 — 계열이 정해지면 태그를 흔들기보다 같은 프롬프트로 take를 더 뽑는 쪽이 이긴다.
  (C 계열은 0.10~0.26s로 짧게 나와 사실상 클릭음이었다. 마림바가 2음 모티프에 가장 잘 붙는다.)
- **후공정**: 트림(≤-40dBFS) → 단일게인 min(RMS→-14dB, peak→-1dB) = **+4.0dB** → 8ms 페이드아웃 → 모노.
  **peak가 한계**(rms -16.1로 -14 미달) — 트랜지언트라 정상. 밀집음이 한계인 AU-027과 반대 사례.
- **임포트 실측**: len 0.259s · ch 1(forceMono) · 44100 · Vorbis · q0.40 · DecompressOnLoad — SFX 계약 정합.
- **소켓 4겹 사전 검증(AU-027 교훈 적용)**: 착수 전 전수 grep — ① `WorldAudioManager._sfxTutorialStep`
  ② `PlayTutorialStepSfx()` ③ `TutorialMissionCardView.OnStepCleared` 발화(초록 전환 직후 1회)
  ④ `CoreSceneBuilder.cs:228` `LoadSfx("sfx_tutorial_step")` 주입. **전부 실재** → 코드 변경 0줄, 파일만 반입.
- **셀프검증 3종**: ① 컴파일 완료 ② 콘솔 에러/워닝 0(리컴파일·Core 재빌드·Play 전 구간)
  ③ Play 실측 exec — `RaiseTutorialStepStarted` → `RaiseTutorialStepCleared` 발화 시
  `_sfxSource.isPlaying` **False→True**, 클립 `sfx_tutorial_step` 0.259s. 콘솔 `[EVENT] TutorialStepCleared` 확인.
  Core 재빌드 후 `Core.unity:1479`에 `_sfxTutorialStep: {fileID: 8300000, guid: ba60dcb4d286c8642bf5966cce1f95ce}` 실재.
  EditMode **46/46 통과**.
- **발주 사양 이탈 1건**: **길이 0.3~0.6초 미달** — 채택본 0.259s. 사양 정합 후보는 A2(0.37s)·A3(0.48s)였으나
  Director 청취에서 A6이 채택됐다. AU-027과 동일하게 발주 의도("짧고 가볍게, 축하보다 확인에 가깝게")가
  수치 사양보다 우선한 판정으로 기록한다. 수용기준의 "1초 이내"·"팡파레와 구분되는 절제된 톤"은 충족.
- ⚠ SFX는 API가 seed를 받지 않는다(`elevenlabs_client` SFX 경로 body = `{text, duration_seconds}`).
  gen.json의 seed는 클라이언트 기록일 뿐 복원 불가 — **로컬 wav가 정본**.
- 라이선스: `Assets/Audio/CREDITS.md` + `planning/assets_manifest.md` LICENSE 표 등재(2026-08-07).
- **잔여(관제)**: BOM §8 SFX 행 `sfx_tutorial_step` 추가 — 정수는 CREDITS+manifest만(AU-011 선례).
  미등재라 파이프라인 `intake`/`promote` 게이트가 막혀 수동 후공정 경로로 진행했다. AU-027 `sfx_level_up` 행도 여전히 미등재.

## AU-027 · 발주 2026-08-05 17:18 → 정수 공장 (레벨업 SFX — S-174 ③ 소켓 충전)

> ⚠ **번호 정정 (2026-08-05 18:40)**: 최초 AU-026으로 채번했으나 정수님이 PR#31에서
> AU-026(눈 날씨 BGM 낮/밤 분리)을 2026-08-01에 선점하고 있었다. 대장에 그 건이 append되지
> 않아 다음 번호를 26으로 읽은 것 — **선발 유지·후발 재번호** 규칙에 따라 AU-027로 옮긴다.
> 재발 방지: 채번 전에 대장뿐 아니라 **열린 PR 제목까지** 훑는다.

요구 (남규 원문): 레벨업시 발생하는 sfx 발주내줘

사양:
- 파일: `Assets/Audio/SFX/sfx_level_up.wav` (파일명 = bom_id · 스왑 계약)
- 길이 0.8~1.4초. 상승 3~4음 아르페지오 + 짧은 반짝임 꼬리. 팡파레(AU-021 개척 해금)보다
  **작고 짧게** — 레벨업은 정산 화면에서 자주 나므로 축포급이면 금방 물린다.
- 톤: 8비트/칩튠 계열(게임 전반 픽셀 룩과 정합). 라우드니스는 기존 SFX와 동급.
- 모노·48kHz. 임포트 설정은 `AudioImportPostprocessor`가 자동 적용(손대지 말 것).

배선: 관제가 `WorldAudioManager.PlayLevelUpSfx()` 소켓을 먼저 깔아 둔다 —
**클립이 없으면 무음 폴백**이라 파일이 도착하기 전에도 게임은 정상 동작한다.
파일만 위 경로에 넣고 PR을 열면 자동으로 울린다.

### 결과 (AU-027) · 2026-08-05 20:35 (정수 공장 · 리드 ~45분 실작업 · feature/jjs-au027-levelup-sfx, base=main)

- **채택 태그**: `short level up chime, three quick ascending notes, bright cheerful chiptune arpeggio, tiny sparkle tail`
  + SFX 토이톤 앵커(마림바·둥근 신스 플럭) · 요청 길이 0.9s.
- **16 take · Director 청취 2라운드**. 1차 7 take(1.2s·0.9s 혼합) → 후공정 4종 발신 → "B(0.59s)" 지목 →
  B 프롬프트를 기준선으로 고정하고 4계열 변주(동일/4음+벨꼬리/글로켄슈필/스케일런) 9 take 재생성 →
  **V02 채택** = V0 계열, 즉 **B와 완전 동일 프롬프트의 다른 take**. 태그 변주보다 take 뽑기가 이겼다.
- **후공정**: 트림(≤-40dBFS) → 단일게인 min(RMS→-14dB, peak→-1dB) = **-3.5dB(감쇠)** → 8ms 페이드아웃 → 모노.
  V02 원본이 peak 0.0dB로 붙어 있었고 밀집음이라 **RMS가 한계** — peak가 -1에 못 미치는 건 규칙상 정상
  (팡파레 AU-021은 반대로 트랜지언트라 peak 한계였다).
- **임포트 실측**: len 0.569s · ch 1(forceMono) · 44100 · Vorbis · q0.40 · DecompressOnLoad — SFX 계약 정합.
- **Play 실측**: `RaisePlayerLeveledUp(2)` 발화 → `_sfxSource.isPlaying` False→True · 콘솔 `[EVENT] PlayerLeveledUp → Lv.2` ·
  에러/워닝 0. 씬 재빌드 후 YAML에 클립 guid `8fa882c2ac232a5469e604a457bfd5d2` 실재 확인.
- **발주 사양 반증 2건**
  1. **"파일만 넣으면 자동으로 울린다" = 거짓**. 이벤트 체인(MasteryProgress→RaisePlayerLeveledUp→구독→
     PlaySfx)은 전부 실재했으나 `CoreSceneBuilder`에 `LoadSfx("sfx_level_up")` 주입 라인이 없어
     재빌드해도 `_sfxLevelUp`은 null 유지 = 영원히 무음이었다. 본 브랜치에서 1줄 추가(CoreSceneBuilder.cs:231).
     부기: 발주서가 지목한 `PlayLevelUpSfx()` 메서드도 실명은 `OnPlayerLeveledUp`.
  2. **길이 0.8~1.4초 미달** — 채택본 0.569s. 사양대로면 1차 take2(1.20s)였으나 Director 청취에서
     짧은 쪽이 채택됐다. 발주 의도("팡파레보다 작고 짧게")가 수치 사양보다 우선한 판정으로 기록한다.
- 라이선스: CREDITS.md + assets_manifest.md LICENSE 표 등재(2026-08-05).
- **잔여(관제)**: BOM §8 SFX 행 `sfx_level_up` 추가 — 정수는 CREDITS+manifest만(AU-011 선례).
  미등재라 파이프라인 `intake`/`promote` 게이트가 막혀 수동 후공정 경로로 진행했다.

## AU-029 · 발주 2026-08-08 03:20 → 정수 공장 (BOM §8 SFX 5행 등재 · stale 표기 수정)

요구 (남규님 지시): 개선 후보 정리 ③ — **BOM §8 미등재 5행 처리 위임 승인**.
종전까지 이 항목은 대장에 "잔여(관제)"로 기록돼 공장이 손댈 수 없었다. 본 발주로 위임이 성립한다.

배경 (실측): AU-020·021·022·027·028 결과 블록에 **"잔여(관제): BOM §8 SFX 행 추가"가 5회 반복
기입**돼 있고 전부 미처리다. 미등재의 실비용은 문서 위생이 아니라 공정이다 —
파이프라인 `intake`/`promote` 게이트가 bom_id를 못 찾아 막히고, 그래서 최근 SFX 5건이 전부
**수동 후공정 우회 경로**로 나갔다(AU-027·028 결과에 명시).

작업:
1. `planning/BOM.md` §8 SFX 표에 5행 추가 — `sfx_car_crash` · `sfx_thunder` · `sfx_fanfare` ·
   `sfx_tutorial_step` · `sfx_level_up`. 트리거 이벤트·용도·상태를 기존 행 형식에 맞춘다.
2. `BOM.md:289` stale 수정 — `sfx_fanfare / sfx_thunder`가 `🔶 소켓만`으로 남아 있다.
   두 클립 모두 main 도달 완료(AU-021·AU-022)이므로 실제 상태로 갱신.
3. 표기 근거는 각 AU 결과 블록의 실측값(길이·채널·포맷)과 `Assets/Audio/SFX/` 실파일.

수용기준: 5행이 §8 표에 존재 · 289행 stale 소멸 · 각 행의 bom_id가 실파일명과 정확히 일치 ·
`Assets/Audio/CREDITS.md`·`assets_manifest.md`와 3자 정합

MDA 판정 (D-070): **무관** — 재미 축 무기여. 공정 위생이다. 착수 근거는 위 "수동 우회 5회"라는
누적 낭비이며, 방치하면 다음 SFX도 같은 우회를 반복한다.

## AU-030 · 발주 2026-08-08 03:20 → 정수 공장 (WebGL 오디오 예산 재검토)

요구 (남규님 지시): 개선 후보 정리 ② 승인.

배경 (실측): 배포본 data **94.8MB** (S-136 · 2026-08-03 · Brotli+탄젠트 제거로 114.9→94.8 감축).
저장소 원본은 BGM 14곡 **277MB** · SFX **27MB**. 임포트는 `AudioImportPostprocessor`가
Vorbis 모노, `BGM_QUALITY=0.26` / `SFX_QUALITY=0.40`, BGM은 Compressed In Memory
(Streaming 금지 — D-040, WebGL 미지원).

조사·시공 3축:
1. **중복본 실사용 확인** — `Pixel_Night_Funk_Don-T-Late.wav`(36MB)와 `_NoVocal.wav`(36MB)가
   양쪽 다 `BgmLibrary` 슬롯에 물려 있는지. 한쪽만 쓰인다면 미사용본은 빌드에서 빠지는지 실측
   (SO 미참조 에셋은 빌드 제외되므로 **먼저 확인하고, 빠진다면 손대지 않는다**).
2. **루프 트림** — 곡별 실길이 측정. 인게임에서 루프로만 쓰이는 곡의 꼬리 여백은 이미
   AU-025/026에서 다룬 기법(stdlib `wave` — `audioop`은 py3.13 제거됨)으로 처리 가능.
   ⚠ 음악적 구간을 잘라내는 것은 **감각 판정**이므로 Director 청취 없이 확정하지 않는다.
3. **비트레이트 A/B** — `BGM_QUALITY` 0.26 → 0.22 청감 비교. 차이가 안 들리면 채택.

수용기준: 빌드 data 크기 **감축분을 실측 수치로 제시**(감축 전/후 MB) · 청감 열화 없음
(Director 판정 필요분은 후보만 제시하고 대기) · 임포트 콘솔 0 · WebGL 제약(Streaming 금지) 불변

⚠ 경계: **빌드·배포 자체는 관제 게이트**(gh-pages push 권한). 공장은 설정·에셋 변경과
에디터 로컬 빌드 실측까지. 재배포는 별건.

MDA 판정 (D-070): **무관** — 재미 3축 무기여. 다만 심사 동선이 웹 링크 1개라 로딩이 곧 첫 인상이고,
현재 배포본은 **S-136 이후 70커밋(S-137~S-205) 미반영 구본**이다. 재배포가 언젠가 필요하므로
그 전에 예산을 줄여 두는 순서다.

### 결과 (AU-029) · 2026-08-08 03:24 (정수 공장 · 리드 ~4분 · feature/jjs-s206-au029-au030, base=main)

수정 1파일 (`planning/BOM.md`). 코드·에셋 변경 0.

**① 5종 등재** — `§15.2 오디오 클립 5종 등재` 신설. bom_id·트리거 실코드 위치·소리·발주번호·상태 5열.
트리거는 문서를 믿지 않고 코드에서 직접 확인했다:

| bom_id | 실측 트리거 |
|---|---|
| sfx_car_crash | `TrafficCar.cs:48·81` → `PlayCarCrashSfx()` |
| sfx_thunder | `WorldWeatherManager.cs:992` → `PlayThunderSfx()` |
| sfx_fanfare | `SettlementView.cs:147` → `PlayFanfareSfx()` (클립 null이면 `sfx_settle_ok` 폴백) |
| sfx_level_up | `WorldEvents.PlayerLeveledUp` 구독 → `OnPlayerLeveledUp` (`WorldAudioManager.cs:231·436`) |
| sfx_tutorial_step | `TutorialMissionCardView.cs:79` → `PlayTutorialStepSfx()` |

소켓 주입은 5종 모두 `CoreSceneBuilder.cs`의 `LoadSfx(...)` 라인(221·222·228·251·252행)에 실재.
**§15.1은 트리거를 적지 않았는데 이번엔 적었다** — AU-027에서 "파일만 넣으면 울린다"가 거짓으로
드러난 이유가 정확히 이 빌더 주입 라인 누락이었고, 문서에 없으면 다음에도 같은 데서 막힌다.

**② stale 소멸** — §15 표 `sfx_fanfare / sfx_thunder` 행의 `🔶 소켓만` → `✅ 클립 도착·배선 완료 — 상세 §15.2`.
동결 문서 기존 행 수정이라 남규님 승인("①②③ 다 진행해줘", 2026-08-08)을 게이트로 삼았고,
그 근거를 §15.2 머리글에 남겼다. 훅 검사 결과 `BOM.md`는 헤더에 `frozen: true`가 없어
freeze-guard 자동 차단 대상은 아니다(규칙상 동결은 유효 — 그래서 승인을 근거로 적었다).

**발주 사양 이탈 1건 (의도적)** — 발주서 수용기준은 "5행이 **§8** 표에 존재"였으나 **§15 부록에 넣었다.**
§8 SFX 표는 v0.4 동결 대상이고, §15.1(2026-07-29)이 미등재 23종을 §8에 넣지 않고 부록에 모으며
"개별 행 분해는 불요"라는 선례를 세웠다. 동결 규칙과 선례가 내가 쓴 수용기준보다 우선한다고 판단했다.

**관찰 (실측 검증)**
- 3자 정합: 5종 전부 `Assets/Audio/SFX/<bom_id>.wav` 실파일 · `git ls-files` 등재 1건씩 ·
  `Assets/Audio/CREDITS.md` 기재 · `assets_manifest.md` LICENSE 표 기재 — **결손 0**.
- 파일 수 대조: §15.1 감사 시점 36종 → 현재 **41종**. 차이 5 = 이번 등재분과 정확히 일치
  (누락·유령 항목 없음).
- `.cs` 변경 0이라 pre-commit 컴파일 게이트 비대상. Unity 불요 작업.

**이 등재로 풀리는 것**: 파이프라인 `intake`/`promote` 게이트가 bom_id를 찾는다 —
AU-020·021·022·027·028이 5회 연속 수동 후공정으로 우회한 원인이 미등재였다.

### 결과 (AU-030) · 2026-08-08 03:53 (정수 공장 · 리드 ~33분 · feature/jjs-s206-au029-au030, base=main)

수정 2파일(`Assets/Data/BgmLibrary.asset` · `AudioImportPostprocessor.cs` — 후자는 **상수 원복 + 주석**).

**⚠ 발주 가설 2개가 실측에서 깨졌다. 순서대로 적는다.**

**① "미참조 6곡 100.9MB" — 오독이었다.** BgmLibrary에 없던 날씨 BGM 6곡(Rain on the Window·
Neon Rain·Neon Snowfall·Sodium Fog·Midnight Heatwave·Daylight Snowfall)은 사장 파일이 아니라
`Core.unity`의 `WorldAudioManager` **날씨 override 필드**(`_weatherBgm` 계열, AU-018 ②/AU-025)가
참조 중이었다. BgmLibrary만 보고 판정하면 안 된다 — 날씨 BGM은 라이브러리를 거치지 않는 별도 경로다.

**② "q0.26 → 0.22로 감축" — 0바이트다. 레버가 이미 바닥에 닿아 있었다.**
같은 곡(`Daylight Snowfall.wav`, 49.3s 모노)을 품질만 바꿔 강제 재임포트하며 임포트 산출물
(`AssetDatabaseExperimental.LookupArtifact` → `GetArtifactPaths` 실파일 크기)을 쟀다:

| quality | 0.05 | 0.22 | 0.26 | 0.35 | 0.50 | 0.70 | 0.90 |
|---|---|---|---|---|---|---|---|
| 산출 | 374KB | 374KB | 374KB | 374KB | 431KB | 490KB | 555KB |

**0.35 이하가 전부 동일 바이트 = Unity Vorbis 인코더의 하한 버킷**이고 현행 0.26이 이미 그 안이다.
14곡 전량 q0.22 재임포트 후 합계도 **11,255KB → 11,255KB로 불변**(1바이트도 안 줄었다).
→ **상수를 0.26으로 원복**하고, 위 측정표를 코드 주석에 박아 두었다. 다음 사람이 같은 삽질을
반복하지 않게 하는 것이 이번 건의 실질 산출물이다. (측정이 죽은 게 아니라는 것은 q0.90=555KB로 반증했다.)

**실제 감축분 — 미분류 보컬곡 제거 (Director 판정)**

- `Pixel_Night_Funk_Don-T-Late.wav`(195.6s)는 BgmLibrary 슬롯이 `Unsorted`였다.
  `WorldAudioManager.cs:296`이 추첨에서 제외하므로 **게임 진행 중 절대 재생되지 않으면서 빌드에는 실렸다**
  (코드 주석에도 그렇게 적혀 있다). 출처는 S-052 "타이틀곡 보컬제거본 교체(보컬본 보관)".
- 라이브러리 항목만 제거 — **wav 파일·CREDITS·manifest 기록은 그대로 남아** S-052의 보관 의도는 유지된다.
- 감축 **1,541KB**. 잔여 참조 0건 실측(`guid b0e4edd3…` 전 에셋·씬 검색) → 빌드에서 빠진다.

**오디오 예산 실측표 (임포트 산출물 기준)**

| 항목 | 전 | 후 |
|---|---|---|
| BGM 14곡 | 11,255 KB | **9,714 KB** (13곡분 적재) |
| SFX 41종 | 2,064 KB | 2,064 KB |
| 오디오 합계 | 13,319 KB (13.0MB) | **11,778 KB (11.5MB)** |

**③ 범위 밖 발견 — 오디오는 이미 작은 항목이다.** 배포 `WebGL.data.unityweb` 94.85MB 중
오디오는 13.0MB = **13.7%**뿐이다(발주서 추정 18.5MB도 과대였다). 이번 감축은 data의 1.6%.
**용량 문제의 본체는 오디오가 아니라 비오디오(메시·텍스처) 쪽**이다 — 별건 발주 대상으로 남긴다.
Director가 기각한 "루프 트림"도 최대치가 몇 MB라 같은 결론이었을 것이다.

**관찰 (Play 실측 · Core 씬)**
- BgmLibrary `entries` 8 → **7**: Night×3 · Day×2 · Title×1(NoVocal) · Ending×1 — `Unsorted` 슬롯 소멸
- Play 진입 → `_pools` **5 → 4개**(Unsorted 풀 자체가 안 생긴다) · 타이틀에서
  `active=Pixel_Night_Funk_Don-T-Late_NoVocal` `isPlaying=True` — 보컬제거본 정상 재생
- 컴파일 0에러 0워닝 · 콘솔 에러/워닝 **0** · EditMode **90/90** · 캡처 `Screenshots/au030_title_bgm.png`
- q0.22 프로브 후 14곡 전량 재임포트로 `.meta` 원복 확인 (`git diff Assets/Audio/` 변경 0)

## AU-031 · 발주 2026-08-08 04:14 → 정수 공장 (타이틀 BGM 프롬프트 4종 회수 — 워크트리 유실 직전)

요구 (남규님 지시): 브랜치 정리 중 발견 보고에 대해 **"프롬프트 8건 살려줘"**.

경위: 원격 브랜치 24개 정리(2026-08-08) 중 워크트리 `C:\Works\Game\Don-t-late-wt1`에
**미추적(untracked) 파일 11건**이 남아 있는 것을 발견했다. 그중 8건이 어느 브랜치에도,
main에도 없는 고유 산출물이다 — 커밋된 적이 없어 브랜치 삭제와 무관하게 **워크트리를 지우는 순간 소멸**한다.

대상 (2026-07-22 생성, `prompt_builder.py` 조립분):
`scripts/audio/prompts/bgm_title_{chip,day,night,rush}.md` + 각 `.plan.json` = 8파일

- 성격: 타이틀 BGM **4방향 탐색안**(칩튠·낮·밤·질주). main에 `bgm_title.md`/`.plan.json` 본편은 이미 있고
  이 4종은 그 변주다. BOM §8 `bgm_title` 슬롯은 **"0곡 — 공백(Director 보류)"** 상태라
  나중에 타이틀곡을 다시 굴릴 때 그대로 재사용된다.
- 커밋 관례 정합: main의 `bgm_day_loop`·`bgm_night_var`·`bgm_title`이 `.md`+`.plan.json` 쌍으로
  등재돼 있다 — 같은 형식이므로 신규 규칙이 필요 없다.

작업: 워크트리 → 본 저장소 `scripts/audio/prompts/`로 복사 후 커밋. 내용 수정 없음(자동 생성물 원형 보존).

수용기준: 8파일이 저장소에 존재 · 워크트리 원본과 바이트 동일 · 비밀정보 미포함 · 커밋 경계 준수

MDA 판정 (D-070): **무관** — 재미 축 무기여. 유실 방지다. 되돌릴 수 없는 삭제를 앞두고 있어
순서상 지금 해야 하는 일이며, 작업량은 복사 1회다.

### 결과 (AU-031) · 2026-08-08 04:18 (정수 공장 · 리드 ~4분 · feature/jjs-s206-au029-au030, base=main)

8파일 신규 등재. 내용 무수정(자동 생성물 원형 보존).

| 파일 | md | plan.json | 창작 태그 방향 |
|---|---|---|---|
| `bgm_title_chip` | 2,907B | 2,621B | 8-bit chiptune · square/triangle wave · bouncy and comedic |
| `bgm_title_day` | 3,108B | 2,577B | major key city pop · retro 80s FM 일렉피아노 · sunny Korean neighborhood morning |
| `bgm_title_night` | 3,013B | 2,228B | moody neon nightscape · 고깃집 골목 야간 배달 |
| `bgm_title_rush` | 3,048B | 2,391B | fast driving beat · rush hour chase · comedic tension |

**관찰 (실측)**
- 8파일 전부 워크트리 원본과 **`cmp` 바이트 동일** (위 표의 크기는 복사본 실측치)
- 비밀정보 스캔(`api_key|secret|token|sk-|xi-api`) **미검출** — 프롬프트 텍스트와 chunks 구조뿐
- `.plan.json` 4종 전부 `chunks` 키 보유 — main의 `bgm_title.plan.json`과 동형(파싱 성공)
- 커밋 관례 정합 확인: main의 `bgm_day_loop`·`bgm_night_var`·`bgm_title`이 이미 `.md`+`.plan.json`
  쌍으로 등재돼 있다. `.gitignore`에도 프롬프트 제외 규칙이 없다(154행이 오히려 "재현 정보는
  `scripts/audio/prompts/`가 보유"라고 명시).
- `.cs` 변경 0 → pre-commit 컴파일 게이트 비대상

**왜 유실 직전이었나 (재발 방지)**: 이 8건은 **어느 브랜치에도 커밋된 적이 없는 untracked 파일**이라
`git branch -d`의 안전장치도, `merge-base --is-ancestor` 검사도 잡아내지 못한다. 브랜치 정리에서
안전하다고 판정한 근거는 전부 *커밋된* 내용에 대한 것이었다.
→ **워크트리를 폐기하기 전에는 반드시 `git status --porcelain`으로 미추적 파일을 먼저 본다.**
   브랜치가 main에 다 들어갔다는 것과 워크트리 디렉토리가 비었다는 것은 완전히 다른 명제다.

**잔여**: 워크트리 `Don-t-late-wt1`의 나머지 미추적 3건은 회수 불요 —
`scripts/audio/rules/GAME-SFX-RULES.md`는 main에 이미 있고, gif 1건은 07-22 화면 녹화 잔재다.
워크트리 폐기 여부는 남규님 판단 대기.

---

## AU-032 · 발주 2026-08-08 → 정수 공장 (폭염·안개 날씨 BGM 낮/밤 분리)

요구 (남규님 원문): `HEAT 밤 곡 : Sunny Afternoon Drive.wav` / `FOG 낮 곡 : Pale White Haze.wav` ·
"HEAT 파일명이 밤/낮 반대로 되어있으면 파일명 수정".

- **배경**: AU-025(비)·AU-026(눈)으로 날씨×시간대 분리가 2종 완료. 폭염·안개는 아직
  `_bgmHeat`/`_bgmFog` 단곡·phase-blind → 낮이든 밤이든 한 곡. 곡 2종 확보(Director Suno) → 분리 가능.
- **확정 배정** (Director 세션 내 승인):

| 날씨 | 낮 | 밤 |
|---|---|---|
| Heat | `Heatwave Afternoon.wav` (기존 `Midnight Heatwave` 개명) | `Heatwave Night Drive.wav` (신곡 · 원제 `Sunny Afternoon Drive`) |
| Fog | `Pale White Haze.wav` (신곡 · 원제 유지) | `Sodium Fog.wav` (기존) |

- **개명 근거**: 신곡 원제가 역할과 반대다(`Sunny Afternoon Drive`가 **밤** 곡 — Director 청취 판정).
  분리하면 기존 `Midnight Heatwave`도 **낮** 곡이 되어 역시 반대가 된다. 이름 교환(덮어쓰기)·신곡만
  개명 대신 **충돌 없는 새 이름 2개**를 택했다(Director 3안 중 ⭐안). 원제는 `assets_manifest.md`에
  병기 보존 — 라이선스 추적선을 끊지 않는다. Fog 신곡은 원제가 역할과 정합해 개명 없음.

**작업**
1. `WorldAudioManager`: `_bgmHeat`→`_bgmHeatDay`/`_bgmHeatNight`, `_bgmFog`→`_bgmFogDay`/`_bgmFogNight`.
   `RefreshWeatherBgm()`의 Heat·Fog 케이스를 `night ? …Night : …Day`로 (Rain/Snow와 동형).
   재평가 경로(`OnDayPhaseChanged`→`RefreshWeatherBgm`)는 AU-025가 이미 설치 → 추가 배선 불요.
2. `CoreSceneBuilder` 배선 4행 교체.
3. 반입: 신곡 2곡 `Downloads`→`Assets/Audio/BGM/` (**꼬리 페이드 트림** — 실측상 머리는 풀레벨,
   꼬리만 페이드아웃: Sunny 86.1s 중 약 3s / Pale 149.6s 중 약 6s). 기존 `Midnight Heatwave.wav`는
   `git mv`로 개명(.meta 동반 — GUID 보존이 배선 생명줄).
4. `.gitignore` allowlist 갱신(신규 2 + 개명분) · `assets_manifest.md` 등재 · BOM §8 갱신.

**경계**: Storm BGM은 범위 밖(곡 없음 — AU-024 프롬프트 대기). 씬 본문 미커밋(빌더 정본).
공장→PR, main 머지는 관제 게이트. base=`feature/jjs-s206-au029-au030` 스택(PR #52 미머지).

**수용기준**: 컴파일 0에러·0워닝 · Heat/Fog 각각 낮·밤 곡이 갈려 재생(실측) · 날씨 유지 중 낮↔밤
전환 시 곡 교체 · 개명 후 GUID 유실 0(배선 살아있음) · 원제 추적 가능 · 예산 증가분 실측 기재.

MDA 판정 (D-070): **강화** — AU-025/026과 동형. 날씨 몰입을 시간대 축으로 심화. 곡 확보됨 = 저비용.
코어루프 불변. (AU-030 실측: 오디오는 빌드 data 94.85MB 중 11.5MB — 2곡 추가 여력 있음.)

### 결과 (AU-032) · 2026-08-09 (정수 공장 · feature/jjs-au032-heat-fog-bgm, base=main)

수정 6파일 + 신곡 2곡 반입 + 개명 1건(`git mv` — GUID 보존).

**곡 배정 (수용기준 대조 — Play 실측)**

Play 진입 → 날씨·시간대를 production 경로(`WorldWeatherManager.SetWeather` ·
`WorldEvents.RaiseDayPhaseChanged` → `WorldAudioManager.RefreshWeatherBgm`)로 구동하고
`_weatherBgm`·`_active.clip`을 프레임 분리 exec로 읽었다.

| # | 날씨 | phase | `_weatherBgm` | 실제 재생 `_active.clip` | isPlaying |
|---|---|---|---|---|---|
| ① | Heat | Day | Heatwave Afternoon | **Heatwave Afternoon** | True |
| ② | Heat | Night | Heatwave Night Drive | **Heatwave Night Drive** | True |
| ③ | Fog | Night | Sodium Fog | **Sodium Fog** | True |
| ④ | Fog | Day | Pale White Haze | **Pale White Haze** | True |
| ⑤ | Clear | Day | NULL | Seoul_Alley_Reflection(시간대 슬롯 복귀) | True |

- **날씨 유지 중 낮↔밤 교체 확인**(①→②) · **시간대 유지 중 날씨 교체 확인**(③→④) 둘 다 실측.
- Evening도 밤곡으로 갈린다(`night = Evening || Night` — Rain/Snow와 동형). Heat+Evening → Heatwave Night Drive 확인.
- ⑤ 날씨 해제 시 `_weatherBgm=null` → 시간대 슬롯 곡으로 재크로스페이드(기존 동작 무회귀).

**관측 함정 2건 (다음 사람 시간 절약)**
- `WorldDayNightManager.SetTime()`은 `minuteOfDay`만 바꾼다 — phase 전이는 다음 `Update()` 틱 몫이라
  **같은 exec 안에서 시각을 바꾸고 phase를 읽으면 이전 값이 나온다.** exec를 프레임 단위로 쪼개야 한다.
- 타이틀 화면에선 `introGraceActive` 때문에 `SkyMinute`가 정오로 고정돼 **시각을 밤으로 옮겨도 phase가 Day**다
  (S-009 설계). 또 `ApplySlot`의 `_titleScene` 게이트가 날씨 override보다 앞서서 **타이틀에선 날씨곡이 아예 안 걸린다.**
  → 위 표 ①~⑤는 `_titleScene=false`·`_bgmReleased=true`로 타이틀 게이트를 내린 뒤 측정한 값이다.

**개명 GUID 보존 (배선 생명줄)**

| | GUID |
|---|---|
| HEAD의 `Midnight Heatwave.wav.meta` | `bfe916a78f65a8543998866e8677eb14` |
| 개명 후 `Heatwave Afternoon.wav.meta` | `bfe916a78f65a8543998866e8677eb14` |

동일 → 참조 유실 0. Core 씬 재빌드 후 `_bgmHeatDay` = Heatwave Afternoon (guid `bfe916a7…`) 물림 확인.

**Core 씬 배선 실측** (`DontLate/Build/Core Scene` 재빌드 → SerializedObject 판독)

```
_bgmHeatDay   = Heatwave Afternoon    (bfe916a78f65a8543998866e8677eb14)
_bgmHeatNight = Heatwave Night Drive  (54b519d226e340e41821f7f1a55fb9cb)
_bgmFogDay    = Pale White Haze       (58a17dc48544deb42a6ab3e092f03d01)
_bgmFogNight  = Sodium Fog            (6f050b2da2446f845bf98afb50033317)
```
Rain·Snow 4필드도 동시 판독 — 무회귀 확인.

**트림 실측 (루프 이음새)**

| 곡 | 원본 | 트림후 | head 0.5s | body | tail 0.5s |
|---|---|---|---|---|---|
| Heatwave Night Drive | 86.1s | **82.5s** | -19.7dB | -19.8dB | -15.7dB |
| Pale White Haze | 149.6s | **143.5s** | -18.7dB | -14.1dB | -15.5dB |

양끝 RMS가 바디 레벨 이상 = 페이드 램프 잔존 0. 루프 이음새에서 음량이 꺼지지 않는다.
둘 다 48kHz 스테레오.

**예산 실측 (AU-030과 같은 방법 — `LookupArtifact` → `GetArtifactPaths` 실파일 바이트)**

| 곡 | 임포트 산출물 |
|---|---|
| Heatwave Night Drive (82.5s) | **628 KB** |
| Pale White Haze (143.5s) | **1,053 KB** |
| (참고) Heatwave Afternoon 472 · Sodium Fog 496 · Rain 876 · Neon Rain 1,009 · Daylight Snowfall 374 · Neon Snowfall 606 | |

이를 AU-030 실측표에 얹으면:

| 항목 | AU-030 후 | AU-032 후 |
|---|---|---|
| BGM(적재분) | 9,714 KB | **11,397 KB** |
| SFX 41종 | 2,064 KB | 2,064 KB |
| 오디오 합계 | 11,778 KB (11.5MB) | **13,461 KB (13.1MB)** |

**증가분 +1,681 KB (+1.64MB)**. BGM 디스크 전량은 16곡 12,938 KB이고 여기서 미적재 보컬본
(`Pixel_Night_Funk_Don-T-Late` 1,541 KB — AU-030에서 라이브러리 해제)을 뺀 값이 위 적재분이다.
빌드 data 94.85MB 기준 오디오 비중 13.7% → **약 13.9%**(data 추정 96.5MB). AU-030 결론
("용량 본체는 비오디오")은 그대로 유효 — 이번 증가는 data의 1.7%.

**셀프검증 3종 + α**
- `editor refresh --compile` → 컴파일 **0에러 0워닝**
- `console --type error,warning` → **0건** (Play 종료 후 재확인도 0건)
- `editor play --wait` → 위 ①~⑤ 실측
- EditMode **90/90 통과** (failed 0 · skipped 0 — AU-030 기준선 유지, 회귀 0)

**경계 준수**: 씬 본문 미커밋(빌더 정본) · 신곡 2곡은 `.gitignore` allowlist 등재 후 반입 ·
라이선스는 `CREDITS.md`+`assets_manifest.md` 양쪽 기록(원제 병기로 추적선 유지) · Storm은 범위 밖(곡 없음).

**잔여**: Storm BGM 1곡(AU-024 프롬프트 대기)이 유일한 날씨 BGM 공백. 이번 건으로
**곡이 있는 날씨 4종(비·눈·폭염·안개) 전부 낮/밤 분리 완료**.

---

## AU-033 · 발주 2026-08-09 → 정수 공장 (태풍 BGM = 비 곡 공유)

요구 (남규님 판단): "Storm 날씨에 비가 내리고 천둥이 치는 거면 Rain에 쓰이는 곡 그대로 써도 될 것 같다."
→ 코드 확인 후 **재사용안 채택**(4안 중 ⭐).

**코드 근거 (`WorldWeatherManager` 실측 — 발주 전 확인)**

| 요소 | Storm | Rain |
|---|---|---|
| 비 | 간헐 — 8~18초 켜짐/6~14초 꺼짐 (`StormRainCycle`, S-088 ⑤) | 계속 |
| 천둥번개 | 18~45초 간격 섬광 2연발 + `sfx_thunder` | **동일 코드 공유** (`stormy = Rain \|\| Storm`, S-088 ⑥) |
| 먹구름 | 8개 · `(0.30,0.31,0.36,0.92)` | 8개 · **동일 색** |
| 강풍 | `_windX` 좌/우 · 강수 기울기 · 공중 바람 줄기(S-089 ④) | 없음 |
| 스태미나 패널티 | 15%(HUD "강풍") | 0 |
| BGM | **없음 → 평상시 도시곡이 흐른다** | 낮/밤 2곡 |

- **천둥은 Storm 전용이 아니다** — Rain에서도 친다. 따라서 두 날씨의 음향적 차이는 실질 **강풍 하나**뿐이고,
  하늘색·구름·천둥·강수까지 같은 계열이다. 곡 공유 근거가 발주 가설보다 오히려 강하다.
- **현행이 어색하다**: Storm은 BGM 슬롯이 비어 시간대 슬롯이 그대로 이어진다 = 먹구름 가득에 천둥이 치는데
  **맑은 날 도시 산책곡**이 흐른다. Rain 곡을 물리면 무드가 맞고, Rain이 이미 낮/밤 2곡이라
  **Storm도 추가 곡 없이 낮/밤 분리**된다.

**작업**
1. `WorldAudioManager.RefreshWeatherBgm()`에 `WeatherType.Storm => night ? _bgmRainNight : _bgmRainDay` 1행 추가.
   XML 주석의 "없는 날씨(Storm 등)는 null" 문구를 현실에 맞게 갱신.
2. `planning/BOM.md` `bgm_weather_storm` 행: "0곡 — 공백" → 비 곡 공유로 갱신(전용곡 여지 명시).
3. `Assets/Audio/CREDITS.md` 날씨 BGM 절에 공유 사실 1줄(신규 파일·라이선스 변동 없음).

**경계**: 신곡 생성·반입 0 · 에셋 0 · 예산 증가 0 · 씬 본문 미커밋(빌더 정본이나 이번 건은 배선 변경 없음 —
`_bgmRainDay/Night`는 이미 물려 있다). Storm 전용곡은 **폐기 아님** — AU-024 프롬프트 초안은 살려두고,
전용곡이 생기면 같은 1행을 `_bgmStormDay/Night`로 교체하면 된다(소켓 구조).

**수용기준**: 컴파일 0에러 0워닝 · 콘솔 0건 · Play에서 Storm 낮 → 비 낮곡 / Storm 밤 → 비 밤곡 실측 ·
Storm 유지 중 낮↔밤 전환 시 곡 교체 · Rain↔Storm 전환 시 곡 유지(같은 곡이므로 크로스페이드 없음) 확인 ·
기존 4날씨 무회귀 · EditMode 90/90 유지.

MDA 판정 (D-070): **강화** — 날씨 BGM 공백 1건 소멸. 코드 1행·에셋 0·예산 0으로 태풍의 음향 무드가
평상시곡에서 우천곡으로 교정된다. 코어루프 불변.

### 결과 (AU-033) · 2026-08-09 (정수 공장 · 리드 ~15분 · feature/jjs-au033-storm-bgm, base=feature/jjs-au032-heat-fog-bgm)

수정 3파일 · **코드 1행 추가** · 신규 에셋 0 · 예산 증가 0.

**관찰 (Play 실측)** — 타이틀 게이트를 내리고(`_titleScene=false`·`_bgmReleased=true`) production 경로로 구동.

| # | 날씨/phase | 실제 재생 `_active.clip` | `_active.time` |
|---|---|---|---|
| ① | Storm / Day | **Rain on the Window** (비 낮곡) | 2.7s |
| ② | Storm / Night | **Neon Rain** (비 밤곡) | 2.8s |
| ③ | Rain / Night | Neon Rain | 7.9s |
| ④ | Storm / Night | Neon Rain | **12.9s** |
| ⑤ | Clear / Day | Sunlit_Seoul_Afternoon (시간대 슬롯 복귀) | 2.4s |
| ⑥ | Storm / Evening | Neon Rain (Evening=밤곡) | 2.5s |

- ①→② **태풍 유지 중 낮↔밤 전환 시 곡 교체** 확인.
- ③→④ **Rain↔Storm 전환에서 `time`이 7.9s→12.9s로 계속 흐른다** = 같은 클립이라 `wb != _weatherBgm`이
  거짓 → `ApplySlot` 미호출 → **재시작·크로스페이드 없이 이어진다**(수용기준 명시 항목). 날씨만 바뀌고
  음악은 끊기지 않는 것이 의도한 동작이다.
- ⑤ 날씨 해제 시 시간대 슬롯 복귀 — 무회귀.

**기존 4날씨 무회귀 (전수 재확인)**

| 날씨 | 낮 | 밤 |
|---|---|---|
| Rain | Rain on the Window | Neon Rain |
| Snow | Daylight Snowfall | Neon Snowfall |
| Heat | Heatwave Afternoon | Heatwave Night Drive |
| Fog | Pale White Haze | Sodium Fog |
| Cloudy | Seoul_Alley_Reflection (곡 없음 → 시간대 슬롯) | — |

**셀프검증 3종**
- `editor refresh --compile` → 컴파일 **0에러 0워닝**
- `console --type error,warning` → **0건** (Play 중·종료 후 모두)
- `editor play --wait` → 위 ①~⑥ + 무회귀 표 실측
- EditMode **90/90** (failed 0 · skipped 0)

**예산**: 신규 파일 0 → 오디오 합계 13,461 KB **불변**. AU-032 대비 증가분 0.

**경계 준수**: 씬 본문 미커밋 · 배선 변경 0(`_bgmRainDay/Night`는 이미 물려 있어 Core 재빌드 불요) ·
CREDITS 표에 추가되는 행 없음(신규 음원 0 = 라이선스 변동 0).

**잔여**: 날씨 BGM 공백 **0건**. Storm 전용곡은 폐기가 아니라 보류 — AU-024 프롬프트 초안이 유효하고,
곡이 생기면 `WorldAudioManager`의 Storm 1행을 `_bgmStormDay/Night`로 바꾸고 빌더에 2행 추가하면 된다.

## AU-034 · 발주 2026-08-10 00:54 → ClaudeCode (sfx_throw 재생성 — 바람 뺀 짧은 "톡")

요구 (남규 원문): "물건 던질 때 소리 — 쉭하는 이상한 소리가 나옴."

진단:
- 클립 결함이 아니라 **프롬프트 결함**. `scripts/audio/prompts/sfx_throw.md`의 창작 태그가
  `one quick soft airy toss swish, light` — `airy swish`가 그 "쉭"의 정체다. 의도대로 나온 소리.
- BOM §8의 `sfx_throw` 행에 **트리거·소리 칸이 미등재**라 규격 없이 스타일 문장만으로 조립됐다
  (gen 7회 재조립했으나 태그는 한 번도 안 바뀜).
- 배선 지점: `PlayerStatusManager.cs:508`(상자 던지기) · `:649`(드링크 던지기) →
  `WorldAudioManager.PlayThrowSfx()`. 파일명 스왑 계약이라 코드 변경 불요.

시공 (남규 판정 = 방향 ①):
- 창작 태그를 바람 제거형 짧은 타격음으로 교체 — 마림바 플럭 계열 "톡", 0.2~0.3s.
- BOM §8 `sfx_throw` 행의 트리거·소리 칸 등재(재발 방지).
- take 뽑기 후 Director 청취 → 채택본만 반입.

수용기준: 던지기 시 "쉭"(공기 스침) 성분 소멸 · 게임 토이톤(마림바·둥근 플럭)과 정합 ·
길이 0.3s 이하 · 연타해도 피로 없음 · 라이선스/출처 CREDITS.md 기록 · 콘솔 에러 0.
MDA 판정 (D-070): 강화 — 던지기는 S-016 ⑦로 이미 있는 동사이고, 소리가 세계 톤에서 튀면
"손맛"이 깎인다. 신규 기능 추가 없음.
