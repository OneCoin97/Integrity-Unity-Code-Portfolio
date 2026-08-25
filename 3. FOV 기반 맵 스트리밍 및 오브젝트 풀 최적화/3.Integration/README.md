# Integration

FOV 계산 결과를 실제 블록 View와 연결하고, 스킬용 임시 FOV와 맵 전환 수명주기를 연동한 발췌 코드입니다.

## 연동 코드

1. `MapViewBlockStreamingExcerpt.cs` — 생성 좌표에는 블록을 배치하고 제거 좌표의 블록은 풀로 반환
2. `MapLifecycleStreamingExcerpt.cs` — 스킬 경로용 TemporaryAreaFov 등록·정리와 맵 전환 시 스트리밍·풀 초기화

두 파일은 실제 프로젝트에서 스트리밍과 직접 연결되는 부분만 발췌했으며, 전체 프로젝트 데이터 타입에 의존하므로 단독 컴파일을 목적으로 하지 않습니다.
