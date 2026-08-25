# Core

데이터별 Saver와 전체 Save / Load 흐름을 관리하는 핵심 코드입니다.

## 메인 코드

1. `SaverForData.cs` — MonoBehaviour에 종속되지 않는 데이터별 Saver. `CancellationToken` 기반 Awaitable Load 훅과 Save 전후 훅을 제공. `AfterSave`는 파일 쓰기 완료가 아니라 저장 요청이 파이프라인에 정상 등록된 직후를 의미
2. `Saver.cs` — 파일 경로와 실행 순서를 관리하고 Save, Load, Reset, Serialize 구현을 연결하는 추상 기반 클래스
3. `SaverManager.cs` — `HashSet` 기반 경로 중복 검사, `List.Sort()` 기반 지연 정렬, 순차 Awaitable Load, Awaitable 전체 Save와 비동기 파이프라인을 조정

SaverManager는 등록된 Saver를 계속 참조하므로, 씬 전환이나 GameObject 파괴로 수명이 끝나는 객체는 `OnDestroy()`에서 반드시 `removeSaver()`를 호출해야 합니다.

`SaverForData` → `Saver` → `SaverManager` 순서로 보면 개별 데이터 요청이 전체 저장 흐름에 합류하는 과정을 파악할 수 있습니다.
