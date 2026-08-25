# Core Architecture

## 메인 코드

1. 'Unit.cs' — 컴포넌트 등록, 필수 구성, Unity 생명주기와 공용 관리 객체 소유
2. 'UnitComponent.cs' — 읽기 전용 공용 참조와 UnitComponent 생명주기
3. 'UnitController.cs' — 선택·사망·저장·로드처럼 여러 소유 객체가 참여하는 Unit 흐름 조정
4. 'UnitEventManager.cs' — Unit 내부 이벤트 구독·호출·정리

## 보조 코드

* 'UnitUtility.cs' — Rigidbody, Collider, 계층 오브젝트와 게임 모드처럼 Unity 오브젝트에 가까운 책임을 Unit 본체에서 분리

