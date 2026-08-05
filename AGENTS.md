# Unity Project Agent Instructions

## Scope

- 사용자가 지정한 파일과 직접 관련된 C# 파일만 읽는다.
- 작업 시작 시 먼저 대상 파일과 변경 계획을 짧게 제시한다.
- 한 작업에서 처음 탐색하는 파일은 최대 5개로 제한한다.
- 프로젝트 전체를 재귀적으로 분석하지 않는다.
- 추가 파일이 필요하면 왜 필요한지 먼저 설명한다.

## Ignore

다음 경로는 탐색하거나 읽지 않는다.

- Library/
- Temp/
- Logs/
- Obj/
- Build/
- Builds/
- UserSettings/
- .git/
- Packages/PackageCache/

다음 파일은 사용자가 명시적으로 요청한 경우에만 읽거나 수정한다.

- *.unity
- *.prefab
- *.asset
- *.meta
- 대용량 JSON, CSV, 로그 파일
- Assets/Art/
- Assets/Models/
- Assets/Textures/

## Unity C# Rules

- 기존 public API를 임의로 변경하지 않는다.
- SerializeField 변수명을 임의로 변경하지 않는다.
- Inspector 참조가 끊길 수 있는 변경을 피한다.
- MonoBehaviour 생명주기와 실행 순서를 확인한다.
- Unity 버전에서 지원되지 않는 API를 사용하지 않는다.
- Editor 전용 코드는 Assets/Editor 또는 Editor 폴더 아래에 둔다.
- 런타임 코드에서 UnityEditor 네임스페이스를 사용하지 않는다.
- 요청하지 않은 리팩터링과 포맷 변경을 하지 않는다.

## Editing

- 필요한 최소 범위만 수정한다.
- 새 파일 생성보다 기존 구조 수정을 우선한다.
- 한 번에 관련 없는 여러 기능을 구현하지 않는다.
- 수정 후 git diff를 기준으로 변경 내용을 검토한다.
- 컴파일 오류 가능성과 Inspector 연결 영향을 보고한다.

## Token Usage

- 동일한 파일을 반복해서 읽지 않는다.
- 긴 파일은 필요한 클래스와 메서드 범위만 읽는다.
- 검색 결과를 무작정 전부 열지 않는다.
- 빌드 로그는 마지막 오류 주변만 읽는다.
- 해결에 필요하지 않은 문서와 에셋은 탐색하지 않는다.