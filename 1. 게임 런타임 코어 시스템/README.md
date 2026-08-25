# 게임 런타임 코어 시스템

1,936줄의 기존 `GameManager`를 이벤트 코어와 책임별 관리자로 분리한 출시 후 리팩터링 사례입니다. 리팩터링 이전에는 게임 진행 데이터, 파티, 유닛 선택, 전투와 탐험 상태뿐 아니라 각 시스템의 함수 호출 순서까지 `GameManager`에 모여 있었습니다.

리팩터링 후에는 호출자가 게임에서 발생한 사건에 해당하는 이벤트만 실행하고, 실제 처리 함수와 조건은 각 책임을 가진 관리자가 구독하도록 변경했습니다. 데이터도 하나의 통합 묶음으로 저장하지 않고, 소유 관리자가 변경과 저장 및 전달을 함께 책임지도록 경계를 나눴습니다.

- [런타임 코어 시스템 상세 설계](https://app.notion.com/p/3b9650b00a5c8122b695f06089c4d138)
- [GameManager 리팩터링 전후 분석](https://app.notion.com/p/3b9650b00a5c81b4928bea0159225972)

## 작동 방식

이 런타임 코어는 **게임 이벤트 구독**과 **코어 데이터 구독**이라는 두 구조로 동작합니다.

`GameManager`는 타이틀 진입, 전투 시작과 종료, 스테이지 이동처럼 게임 전체에 영향을 주는 사건을 이벤트로 정의하고, 콜백 등록과 실행 순서 관리만 담당합니다. `GameDataManager`, `PartyManager`, `UnitSelectionManager`, `CombatTurnManager`, `AdventureTurnManager`를 비롯한 각 시스템은 필요한 이벤트에 자신의 처리 함수를 직접 등록합니다. 따라서 `GameManager`는 개별 시스템의 내부 동작을 알지 않고, 이벤트가 실행되면 미리 등록된 함수들을 정해진 순서대로 호출합니다.

예를 들어 전투가 끝나면 호출부는 `CombatEnd` 이벤트를 실행합니다. `GameSessionManager`는 이 이벤트에서 화면 전환과 전투 종료 준비를 처리하고, `PartyRecoveryManager`는 회복 예약과 저장을 처리합니다. 두 시스템은 서로를 직접 호출하지 않으며, 각자 `CombatEnd`에 등록한 함수만 책임집니다.

### 이벤트 실행 흐름

1. 각 시스템이 초기화될 때 자신이 반응할 이벤트, 실행 함수, 위치와 우선순위를 `GameManager`에 등록합니다.
2. 게임에서 사건이 발생하면 해당하는 이벤트를 한 번 실행합니다.
3. `GameEventSubscriptions`가 등록된 동기 및 비동기 함수를 우선순위에 맞춰 정렬하고 순차 실행합니다. 실행 중 구독 목록이 바뀌어도 현재 흐름에 영향을 주지 않도록 실행 스냅샷을 사용합니다.
4. 호출된 관리자는 자신이 소유한 규칙과 데이터를 처리합니다. 데이터가 변경되면 Listener를 통해 필요한 소비자에게 새 상태를 전달합니다.
5. Unity 생명주기나 여러 관리자를 함께 조정해야 하는 작업만 `Integration` 계층에서 연결합니다.

### 코어 데이터 전달

게임 이벤트가 **언제 동작할지**를 연결한다면, 코어 데이터 구독은 **현재 상태가 무엇인지**를 전달합니다. `GameDataManager`, `PartyManager`, `UnitSelectionManager`, `CombatTurnManager`, `AdventureTurnManager`가 각각 자신의 원본 데이터를 소유하고 변경하며, 상태가 바뀌면 데이터별 Listener에 새 값을 전달합니다. 데이터를 사용하는 쪽은 각 관리자의 저장 방식이나 내부 변경 과정을 직접 알지 않고 전달받은 상태로 자신의 기능을 처리합니다.

## 폴더 구성

- `0.Before Refactoring`: 리팩터링 이전 `GameManager` 원본
- `1.Event Core`: 이벤트 위치 관리, 구독 등록과 해제, 실행 스냅샷과 상태 이벤트 계약
- `2.Core Managers`: 게임 데이터, 파티, 선택과 전투 및 탐험 턴을 소유하는 책임별 관리자
- `3.Data Subscription`: 데이터별 Listener 계약과 공통 구독 생명주기
- `4.State Machine`: 전투와 탐험 상태 및 전이 조건. 상태는 `IGameModeEvent` 실행 계약만 전달받음
- `5.Integration`: 입력, Unity 생명주기와 여러 코어 관리자가 함께 필요한 흐름 연결

## 권장 확인 순서

먼저 `0.Before Refactoring/GameManager.cs`에서 분리 전 책임을 확인한 뒤, `1.Event Core/GameManager.cs`와 `GameEventSubscriptions.cs`를 비교하면 리팩터링의 중심을 파악할 수 있습니다. 이후 `2.Core Managers`와 `3.Data Subscription`을 보면 데이터 소유권과 전달 방식이 어떻게 분리됐는지 확인할 수 있습니다.
