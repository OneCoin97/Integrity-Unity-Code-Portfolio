# UnitComponent Examples

Unit이 필수 구성으로 보장하는 전투 컴포넌트 4종입니다. 스탯·상태·자원·타깃 기록을 변경 이유와 데이터 책임에 따라 분리하고, 공용 관리 객체의 동일 참조를 사용하도록 구성했습니다.

여기서 `Flash` 스탯과 상태는 스킬 실행처럼 특정 상황에서 즉시 반영하며 파일에는 저장하지 않는 런타임 값입니다.

## 메인 코드

1. `UnitCombatStats.cs` — 영구·임시·Flash 스탯과 시간제 스탯의 적용·해제 관리
2. `UnitCombatStatuses.cs` — UnitState 적용·해제와 상태별 지속 턴 관리
3. `UnitCombatResources.cs` — HP·Stamina, 피해·회복 계산, FloatingText와 경험치 처리
4. `UnitCombatState.cs` — 타깃·타게터 관계와 이전·현재 턴 대상 기록 관리
