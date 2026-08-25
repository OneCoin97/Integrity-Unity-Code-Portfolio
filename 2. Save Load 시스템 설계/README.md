# Save / Load 시스템 설계

빈번한 저장 요청을 최신 상태로 통합하고, 직렬화와 파일 쓰기를 분리해 프레임 부담과 중복 I/O를 줄인 저장 시스템입니다.

- [Save / Load 시스템 상세 설계](https://app.notion.com/p/453650b00a5c82c19d9a810bd2b78fcd)
- [대용량 맵 저장 적용 사례](https://app.notion.com/p/635650b00a5c8260bb6f017a625af705)
- `Core`: Saver 등록·순서·경로와 Awaitable 기반 전체 Save / Load 흐름
- `Pipeline`: 요청 병합, 직렬화, 비동기 파일 쓰기와 temp 파일 교체
- `SaveLoadUsageExample.cs`: 등록, 순서 설정, Awaitable Load와 전체 Save 완료 대기 예시
- `SerializationContractExample.cs`: 직렬화 계약으로 `JsonUtility`를 대체한 뒤 현재 `SerializeStage` 워커를 백그라운드 스레드로 전환하는 확장 예시

현재는 실제 규모와 유지보수 비용을 고려해 `JsonUtility`를 메인 스레드에서 사용합니다. 직렬화가 실제 병목이 되면 `ISaveSerializable` 계약으로 Unity API 의존을 분리하고, 불변 스냅샷 또는 동기화된 데이터만 읽도록 한 뒤 직렬화 워커를 `Task.Run` 기반으로 옮길 수 있습니다.
