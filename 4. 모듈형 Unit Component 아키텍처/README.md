# 모듈형 Unit Component 아키텍처

게임 캐릭터인 Unit에는 이동, 스킬, 감지, 연출처럼 유닛 종류마다 다른 기능과 전투 스탯, 상태, 자원처럼 공통으로 필요한 기능이 함께 존재합니다. 모든 기능을 `Unit`에 계속 추가하면 클래스가 커지고, 반대로 컴포넌트마다 데이터를 따로 가지면 같은 값의 동기화와 저장 시점을 관리하기 어려워집니다.

이 구조는 기능을 `UnitComponent`로 분리해 필요한 컴포넌트를 조립하고, 여러 컴포넌트가 함께 사용하는 데이터는 관리 객체의 동일 참조로 공유합니다. 기본 전투 동작에 필요한 Stats, Statuses, Resources, State 4종은 필수 구성으로 보장하고, 그 밖의 이동, 스킬과 표현 기능은 유닛 역할에 맞춰 선택적으로 추가할 수 있습니다.

- [상세 설계 및 리팩터링 결과](https://app.notion.com/p/3b9650b00a5c81ab9751f4184331e2ad)

## 구성과 데이터 흐름

1. `Unit`은 Awake에서 자식의 `UnitComponent`를 한 번 수집해 실제 타입을 키로 사용하는 Registry에 등록합니다.
2. 외부 시스템은 반복적으로 `GetComponent`를 호출하거나 `Unit`에 컴포넌트별 프로퍼티를 계속 추가하지 않고, 타입 기반 API로 필요한 기능을 조회합니다.
3. `UnitIdentity`, `UnitTransform`, `UnitCombatAttributes`와 `UnitCombatHistory`는 각 데이터의 조회, 변경과 저장 API를 함께 소유합니다.
4. 모든 `UnitComponent`는 이 관리 객체들의 동일 참조를 전달받아 데이터 복사와 재동기화 없이 자신의 기능 구현에 집중합니다.
5. 선택, 사망, 저장과 로드처럼 분리된 여러 기능이 함께 반응해야 하는 사건은 `UnitController`와 `UnitEventManager`가 내부 이벤트로 연결합니다.

데이터는 참조로 공유하지만 아무 클래스에서나 직접 수정하도록 공개하지 않습니다. 특히 변경과 동시에 저장해야 하는 값은 소유 관리 객체의 API를 통해서만 바꾸도록 해 데이터 변경 지점과 저장 책임을 일치시켰습니다. `UnitRuntimeData`처럼 현재 세션에서만 필요한 값은 별도로 두고 파일 저장 대상에서 제외했습니다.

## 폴더 구성

- `1.Core Architecture`: 타입 기반 Component Registry, 공용 참조 전달, Unit 흐름 조정과 내부 이벤트
- `2.Data Management`: 직렬화 데이터와 이를 소유하는 변경 및 저장 API
- `3.UnitComponent Examples`: Stats, Statuses, Resources, State로 나눈 기본 전투 컴포넌트 4종

`1.Core Architecture/Unit.cs`와 `UnitComponent.cs`로 등록과 공용 참조 전달을 먼저 확인한 뒤, `2.Data Management/UnitCombatAttributes.cs`와 `3.UnitComponent Examples`를 비교하면 데이터 소유 객체와 기능 컴포넌트의 책임 차이를 이해하기 쉽습니다.
