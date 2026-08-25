# Save / Load 시스템 설계

로그라이크 플레이 중에는 이동, 전투, 유닛 상태와 맵 변경처럼 서로 다른 시스템에서 저장 요청이 연속으로 발생합니다. 요청마다 즉시 파일을 쓰면 같은 데이터를 반복 저장해 프레임과 디스크 I/O에 부담을 주고, 각 시스템이 저장 순서와 파일 처리까지 알아야 하는 문제가 생깁니다.

이 시스템은 데이터별 저장 책임은 `Saver`로 나누되, 요청은 하나의 파이프라인으로 모읍니다. 아직 처리되지 않은 같은 대상의 요청은 최신 상태로 통합하고, 직렬화가 끝난 데이터만 단일 쓰기 워커에 전달해 파일 접근이 동시에 겹치지 않도록 구성했습니다.

- [Save / Load 시스템 상세 설계](https://app.notion.com/p/453650b00a5c82c19d9a810bd2b78fcd)
- [대용량 맵 저장 적용 사례](https://app.notion.com/p/635650b00a5c8260bb6f017a625af705)

## 저장 흐름

1. 각 시스템은 자신의 데이터를 다루는 `SaverForData`를 만들고 `SaverManager`에 경로와 실행 순서를 등록합니다.
2. 저장 요청이 들어오면 `SerializeStage`가 같은 `Saver`의 대기 요청을 하나로 합치고, 현재 데이터를 직렬화합니다.
3. `WriteStage`는 `saveCycle` 동안 들어온 요청을 경로별 최신 결과로 합친 뒤, 대기 중인 전체 항목을 하나의 배치로 꺼냅니다. 단일 Task 워커는 배치에 포함된 파일을 하나씩 비동기로 기록합니다.
4. `SaveData`는 기존 파일을 바로 덮어쓰지 않고 임시 파일에 먼저 기록한 다음 교체를 시도합니다. 직렬화에 실패해 `null`이 전달되면 기존 저장 파일을 유지합니다.
5. `SaverManager.saveAll()`은 모든 요청을 등록한 뒤 직렬화 큐와 진행 중인 파일 쓰기가 끝날 때까지 Awaitable로 대기합니다.

로드는 등록된 실행 순서에 따라 각 `Saver`를 순차 처리합니다. 따라서 데이터 의존성이 있는 복원 단계도 호출부에 순서를 흩어 놓지 않고 한곳에서 관리할 수 있습니다.

## 폴더 구성

- `Core`: `Saver` 등록, 경로 중복 검사, 실행 순서와 Awaitable 기반 전체 Save / Load 흐름
- `Pipeline`: 요청 병합, 직렬화, 비동기 파일 쓰기와 임시 파일 교체
- `SaveLoadUsageExample.cs`: Saver 등록과 순서 설정, Load 및 전체 Save 완료 대기 예시
- `SerializationContractExample.cs`: Unity 직렬화 의존을 계약으로 분리해 직렬화 단계까지 백그라운드로 옮길 수 있는 확장 예시

## 현재 선택과 확장 지점

현재는 실제 데이터 규모와 유지보수 비용을 고려해 Unity의 `JsonUtility`를 메인 스레드에서 사용하고, 파일 쓰기만 백그라운드에서 수행합니다. 직렬화가 실제 병목이 되면 `ISaveSerializable` 계약으로 Unity API 의존을 분리하고, 불변 스냅샷 또는 동기화된 데이터만 읽도록 한 뒤 `SerializeStage`도 `Task.Run` 기반 워커로 옮길 수 있습니다.

처음에는 `SaveLoadUsageExample.cs`로 외부 사용 방식을 확인한 뒤, `Core/SaverForData.cs`와 `Core/SaverManager.cs`, `Pipeline/SerializeStage.cs`, `Pipeline/WriteStage.cs` 순서로 읽는 것을 권장합니다.
