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
