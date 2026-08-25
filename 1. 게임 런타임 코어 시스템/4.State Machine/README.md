# State Machine

전투와 탐험 상태의 실행 루프와 상태별 전이 판단을 분리한 상태 머신 코드입니다.

## 공통 구조

1. `GameMode.cs` — Enter 실행, 프레임별 전이 판단, Exit 실행과 다음 상태 적용 순서를 제공하는 제네릭 상태 기반 클래스

## 상태 구현

- `BraveTurn.cs` — 아군의 Delay·Ready·Move·Skill·Wait 상태와 다음 전이 조건
- `EnemyTurn.cs` — 적의 Delay·Ready·Move·Skill·Wait 상태와 다음 전이 조건
- `MoveMode.cs` — 탐험의 Move·Skill·Load 상태와 다음 전이 조건

상태 객체는 `IGameModeEvent`만 전달받아 Enter·Exit 이벤트를 실행하며 구독 등록·해제 구현에는 의존하지 않습니다.
