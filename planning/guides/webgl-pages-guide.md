# 가이드 — WebGL 빌드 → GitHub Pages 관통 (M0-03 · 남규 작업분)

> 목표: 빈(또는 그레이박스) 씬을 웹 링크로 띄우고 **타 브라우저에서 열림 확인** = M0-03 done.
> ⚠ 실측(2026-07-20): **이 PC에 WebGL 빌드 모듈이 없다** — 0단계 없이는 시작 불가.

## 0. WebGL 모듈 설치 (선행 필수)
Unity Hub → Installs → **6000.5.3f1** 톱니 → Add modules → **Web Build Support** 체크 → 설치
→ 에디터 재시작. (수 GB — 네트워크 시간 감안)

## 1. 플랫폼 전환
File → **Build Profiles** → **Web** 선택 → **Switch Platform** (첫 전환은 수 분 — 에셋 재임포트)

## 2. 씬 목록
Scene List에 씬 1개면 관통 충분. `Greybox.unity`를 넣으면 코어루프까지 웹에서 확인 가능
(관통이 목적이면 SampleScene도 무방).

## 3. 압축 설정 — Pages 최대 함정 ⚠
Player Settings → Publishing Settings:
- **Compression Format = Brotli**
- **Decompression Fallback = ON** ← GitHub Pages는 Content-Encoding 헤더를 안 보내므로
  이걸 안 켜면 **무한 로딩 + 콘솔 `.br` 파싱 에러**. 실패 원인 1위.

## 4. 빌드
Build → 폴더 `Builds/Web` (git 추적 밖 — 빌드 산출물은 메인 브랜치 커밋 금지)

## 5. 배포 — 관통 최단 경로 (권장)
1. GitHub에서 새 **공개** 레포 생성: `dontlate-web`
2. 웹 UI "uploading an existing file" → `Builds/Web` **내용물** 전부 드래그
   (index.html이 레포 **루트**에 오게 — 폴더째 올리면 404)
3. 레포 Settings → **Pages** → Source: Deploy from a branch → `main` / `root` → Save
4. 1~3분 후 `https://<계정>.github.io/dontlate-web/`
- 본 레포 gh-pages 통합·자동화(Actions)는 본선용 — M0-07 훅 작업 때 함께.

## 6. 판정 (= M0-03 전환 조건)
- **타 브라우저**(폰·엣지 등 빌드한 PC 아닌 환경 포함)에서 링크 열림
- Unity 로딩바 → 씬 표시. 첫 로드 수십 초는 정상(압축 해제).

## 7. 흔한 실패 3종
| 증상 | 원인 | 처방 |
|---|---|---|
| 무한 로딩, 콘솔 `.br` 에러 | Decompression Fallback OFF | §3 다시 |
| 404 | index.html이 루트 아님 / Pages 소스 오설정 | §5-2·3 다시 |
| 로딩 후 보라/검정 화면 | 셰이더·URP 문제 | 콘솔 로그 들고 관제 호출 |
