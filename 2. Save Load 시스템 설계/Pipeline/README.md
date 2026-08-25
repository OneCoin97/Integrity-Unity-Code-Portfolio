# Pipeline

저장 요청의 직렬화와 파일 쓰기를 분리하고, 반복 요청을 후속 단계에서 최신 데이터로 병합하는 비동기 처리 구간입니다.

## 메인 코드

1. `SerializeStage.cs` — `Saver` 기반 중복 요청을 병합하고 `Saver.serialize()` 결과를 쓰기 단계로 전달. 현재 `JsonUtility` 사용을 고려해 메인 스레드에서 순차 실행
2. `WriteStage.cs` — 직렬화된 데이터를 경로별 최신 요청으로 병합하고 단일 Task 워커에서 비동기 파일 쓰기를 순차 처리
3. `SaveData.cs` — 경로와 직렬화 결과만 보관하며 temp 파일 기반의 최선 노력(best-effort) 교체를 수행. `null` 직렬화 결과는 기존 파일을 덮어쓰지 않고 저장 실패로 처리

## 보조 코드

- `SaveDataPool.cs` — 파이프라인에서 반복 생성되는 `SaveData`를 재사용하고 진행 중인 작업 수를 관리

읽는 순서는 `SerializeStage` → `WriteStage` → `SaveData`입니다. `SaverManager.saveAll()`은 모든 Saver의 요청을 등록한 뒤 직렬화 큐와 실행 중인 파일 쓰기가 모두 끝날 때까지 Awaitable로 대기합니다.
