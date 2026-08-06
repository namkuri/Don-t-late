<p align="center">
  <img src="./docs/images/dontlate-readme-banner.png" alt="늦지마 캐릭터 배너" width="100%" />
</p>

<h1 align="center">늦지마</h1>

<p align="center">
  <b>시간에 쫓기며 동네 곳곳의 택배를 배달하는 지각 압박 배송 생존 게임</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Engine-Unity-222222?style=flat-square&logo=unity&logoColor=white" alt="Unity" />
  <img src="https://img.shields.io/badge/Platform-PC%20%7C%20WebGL-5C8DFF?style=flat-square" alt="Platform" />
  <img src="https://img.shields.io/badge/Status-In%20Development-F2B84B?style=flat-square" alt="Development Status" />
</p>

📦 게임 소개

늦지마는 제한 시간 안에 배송지를 확인하고, 택배를 챙겨 동네 곳곳에 배달하는 캐주얼 배송 게임입니다.

플레이어는 좁은 골목과 언덕길, 아파트와 상가를 오가며 배송을 수행합니다.무작정 달리는 것만으로는 부족합니다. 남은 시간, 이동 경로, 들고 있는 택배와 주민들의 요청을 함께 판단해야 합니다.

빨리 가야 한다. 하지만 아무 상자나 집으면 안 된다.

🎮 핵심 플레이

송장과 배송지 정보를 확인해 올바른 택배를 선택합니다.

제한 시간 안에 목적지까지 이동합니다.

배송 순서와 이동 동선을 최적화합니다.

스태미나와 시간을 관리하며 골목의 장애물을 돌파합니다.

동네 주민들과 상호작용하고 각자의 이야기를 발견합니다.

배송 보상으로 빚을 갚고 다음 배달을 준비합니다.

🏘️ 주요 지역

지역

특징

아파트

복도, 엘리베이터, 반복되는 동과 호수

빌라촌

좁은 골목과 헷갈리는 주소

먹자골목

간판과 행인이 많은 번화가

언덕주택가

긴 오르막과 복잡한 배송 동선

👥 등장인물

동네에는 플레이어의 배송을 도와주거나, 잔소리하거나, 새로운 정보를 알려주는 주민들이 등장합니다.

베테랑 기사와 배송 동료

꽃집 주인과 편의점 직원

동네 사정을 훤히 아는 주민들

까칠하지만 정 많은 할머니

배송길을 따라다니는 삼색 고양이

각 NPC는 독립적인 대사와 관계도를 가지며, 플레이 과정에서 동네의 분위기와 이야기를 채웁니다.

✨ 게임 특징

시간 압박과 경로 선택

배송마다 제한 시간이 존재합니다. 빠른 길이 항상 안전한 길은 아니며, 어떤 주문을 먼저 처리할지에 따라 전체 결과가 달라집니다.

한국 동네 기반의 공간

복도식 아파트, 빌라촌, 먹자골목, 언덕길처럼 익숙한 한국 주거 공간을 배경으로 구성했습니다.

픽셀 아트와 3D 공간의 결합

픽셀 감성의 캐릭터와 UI를 3D 공간에 배치해, 평면적인 친근함과 입체적인 이동감을 함께 표현합니다.

주민 중심의 옴니버스 이야기

하나의 거대한 서사보다, 배송 중 만나는 사람들의 짧고 선명한 에피소드가 쌓여 동네 전체의 이야기가 됩니다.

🕹️ 조작법

입력

동작

WASD / 방향키

이동

E

상호작용

Tab

휴대폰 및 배송 정보

Shift

달리기

Esc

메뉴

실제 키 설정은 빌드 버전에 따라 변경될 수 있습니다.

🌐 WebGL 플레이

브라우저에서 별도 설치 없이 플레이할 수 있습니다.

<p align="center">
  <a href="https://YOUR_WEBGL_URL" target="_blank">
    <img src="https://img.shields.io/badge/▶%20WEBGL로%20플레이-2F80ED?style=for-the-badge&logo=unity&logoColor=white" alt="WebGL로 플레이" />
  </a>
</p>

<p align="center">
  <sub>Chrome 또는 Edge 최신 버전, 데스크톱 환경을 권장합니다.</sub>
</p>

https://YOUR_WEBGL_URL을 실제 WebGL 배포 주소로 교체하세요.

🛠️ 개발 환경

Unity

C#

TextMesh Pro

Git / GitHub

WebGL Build

Blender 및 이미지 생성 도구를 활용한 아트 파이프라인

📁 프로젝트 구조

Assets/
├─ Art/
│  ├─ Characters/
│  ├─ Environment/
│  ├─ Props/
│  └─ UI/
├─ Scenes/
├─ Scripts/
│  ├─ Core/
│  ├─ Player/
│  ├─ Delivery/
│  ├─ NPC/
│  └─ UI/
└─ Resources/

🚧 개발 상태

현재 개발 중인 프로젝트입니다.

기본 이동 및 상호작용

배송 정보 UI

주요 NPC 및 캐릭터 아트

기본 지역 구성

배송 루프 밸런싱

지역별 이벤트 확장

사운드 및 연출 보강

WebGL 최적화

데모 빌드 공개

📸 스크린샷

<!-- 실제 이미지 경로로 교체 -->
![게임 화면](./docs/images/screenshot-01.png)
![배송 UI](./docs/images/screenshot-02.png)

🤝 기여

버그 제보와 개선 의견은 GitHub Issues를 통해 남겨주세요.

프로젝트 구조나 에셋 규칙을 변경하는 PR은 먼저 Issue에서 방향을 공유해 주세요.

📜 라이선스

코드와 게임 에셋의 라이선스는 서로 다를 수 있습니다.외부 에셋과 폰트는 각 원저작자의 라이선스를 따릅니다.
