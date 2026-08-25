# 게임 런타임 코어 시스템

1,936줄의 기존 `GameManager`를 이벤트 코어와 책임별 관리자로 분리한 출시 후 리팩터링 사례입니다. 리팩터링 이전에는 게임 진행 데이터, 파티, 유닛 선택, 전투와 탐험 상태뿐 아니라 각 시스템의 함수 호출 순서까지 `GameManager`에 모여 있었습니다.

리팩터링 후에는 호출자가 게임에서 발생한 사건에 해당하는 이벤트만 실행하고, 실제 처리 함수와 조건은 각 책임을 가진 관리자가 구독하도록 변경했습니다. 데이터도 하나의 통합 묶음으로 저장하지 않고, 소유 관리자가 변경과 저장 및 전달을 함께 책임지도록 경계를 나눴습니다.

- [런타임 코어 시스템 상세 설계](https://app.notion.com/p/3b9650b00a5c8122b695f06089c4d138)
- [GameManager 리팩터링 전후 분석](https://app.notion.com/p/3b9650b00a5c81b4928bea0159225972)

## 전체 실행 흐름

1. 외부 시스템은 `GameManager`의 세부 함수를 직접 조합하지 않고 현재 사건에 해당하는 이벤트를 실행합니다.
2. `GameEventSubscriptions`는 각 시스템이 등록한 콜백을 위치와 우선순위에 맞춰 정렬하고, 실행 중 컬렉션 변경의 영향을 받지 않도록 스냅샷을 사용해 순차 실행합니다.
3. `GameDataManager`, `PartyManager`, `UnitSelectionManager`와 턴 관리자는 자신의 콜백 안에서 도메인 규칙과 데이터 변경을 처리합니다.
4. 변경된 코어 데이터는 Listener 계약으로 소비자에게 전달되며, 소비자는 데이터 관리자의 내부 구현을 알 필요가 없습니다.
5. Unity 생명주기나 여러 도메인의 협력이 필요한 흐름만 `Integration` 계층에서 연결합니다.

## 폴더 구성

- `0.Before Refactoring`: 리팩터링 이전 `GameManager` 원본
- `1.Event Core`: 이벤트 위치 관리, 구독 등록과 해제, 실행 스냅샷과 상태 이벤트 계약
- `2.Core Managers`: 게임 데이터, 파티, 선택과 전투 및 탐험 턴을 소유하는 책임별 관리자
- `3.Data Subscription`: 데이터별 Listener 계약과 공통 구독 생명주기
- `4.State Machine`: 전투와 탐험 상태 및 전이 조건. 상태는 `IGameModeEvent` 실행 계약만 전달받음
- `5.Integration`: 입력, Unity 생명주기와 여러 코어 관리자가 함께 필요한 흐름 연결

## 권장 확인 순서

먼저 `0.Before Refactoring/GameManager.cs`에서 분리 전 책임을 확인한 뒤, `1.Event Core/GameManager.cs`와 `GameEventSubscriptions.cs`를 비교하면 리팩터링의 중심을 파악할 수 있습니다. 이후 `2.Core Managers`와 `3.Data Subscription`을 보면 데이터 소유권과 전달 방식이 어떻게 분리됐는지 확인할 수 있습니다.
