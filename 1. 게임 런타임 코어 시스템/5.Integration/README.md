# Integration

순수 C# 코어 관리자만으로 끝낼 수 없는 Unity 생명주기와 둘 이상의 도메인이 함께 필요한 런타임 흐름을 연결합니다.

## 메인 코드

1. 'GameSessionManager.cs' — 턴 종료 입력, 스테이지 이동과 전투 종료처럼 여러 관리자와 Unity 객체가 함께 필요한 세션 흐름 조정
2. 'GameInputSubscriber.cs' — Unity Input System 입력을 GameManager 이벤트와 선택 관리자 API에 연결
3. 'PartyRecoveryManager.cs' — 전투 종료·휴식 이벤트를 구독해 파티 회복, 효과와 저장 처리를 연결

도메인 원본 데이터와 규칙은 Core Managers에 유지하고, 외부 시스템과의 복합 연결만 이 경계에 배치했습니다.
