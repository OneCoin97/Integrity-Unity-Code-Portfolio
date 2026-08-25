# Core Architecture

`Unit`은 모든 기능을 직접 구현하는 클래스가 아니라, 공용 데이터 관리 객체와 조립된 `UnitComponent`의 생명주기를 연결하는 진입점입니다. Awake에서 자식 컴포넌트를 한 번 수집해 타입 기반 Registry를 만들고, 각 컴포넌트에는 같은 공용 데이터 참조와 Unit 내부 이벤트를 전달합니다.

선택, 사망, 저장과 로드처럼 여러 컴포넌트가 함께 반응해야 하는 흐름은 `UnitController`가 조정하고 `UnitEventManager`가 구독된 함수를 실행합니다. 따라서 새 컴포넌트가 추가돼도 `Unit`에 해당 기능의 세부 호출을 계속 추가하지 않습니다.

## 메인 코드

1. `Unit.cs` — 컴포넌트 등록, 필수 구성, Unity 생명주기와 공용 관리 객체 소유
2. `UnitComponent.cs` — 읽기 전용 공용 참조와 UnitComponent 생명주기
3. `UnitController.cs` — 선택·사망·저장·로드처럼 여러 소유 객체가 참여하는 Unit 흐름 조정
4. `UnitEventManager.cs` — Unit 내부 이벤트 구독·호출·정리

## 보조 코드

- `UnitUtility.cs` — Rigidbody, Collider, 계층 오브젝트와 게임 모드처럼 Unity 오브젝트에 가까운 책임을 Unit 본체에서 분리

`Unit.cs`의 초기화와 Registry API를 먼저 확인한 뒤 `UnitComponent.cs`, `UnitController.cs`, `UnitEventManager.cs` 순서로 읽으면 기능 등록과 공통 사건 실행이 분리된 흐름을 파악할 수 있습니다.
