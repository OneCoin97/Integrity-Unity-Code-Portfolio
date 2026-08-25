# Data Management

직렬화용 데이터와 실제 변경 API·저장 책임을 분리한 코드입니다.

## 관리 객체

1. `UnitCombatAttributes.cs` — 전투 스탯·HP·Stamina·상태의 조회와 변경 API를 제공하고 변경된 영역을 독립 저장
2. `UnitCombatHistory.cs` — 전투 기록과 타깃 기록을 하나의 API로 연결하면서 실제 저장은 기록 종류별로 분리
3. `UnitIdentity.cs` — 유닛 신원 데이터의 조회·변경·저장 관리
4. `UnitTransform.cs` — 위치·회전·가시 상태의 변경과 저장 관리

## 저장 데이터

- `UnitCombatData.cs` — 영구·임시·Flash 전투 스탯, HP·Stamina와 상태 값
- `UnitIdentityData.cs` — 이름·진영·클래스 등 유닛 신원 값
- `UnitTransformData.cs` — 위치·회전·가시 상태 값
- `CombatHistoryData.cs` — 피해·회복·이동과 턴 단위 기록
- `TargetHistoryData.cs` — 타깃·타게터와 이전 턴 관계 기록
- `UnitRuntimeData.cs` — UnitComponent들이 공유하는 실행 중 상태이며 파일 저장 대상에서는 제외

관리 객체를 먼저 확인한 뒤 대응하는 저장 데이터 파일을 보면 데이터 소유권, 변경 API와 저장 경계를 빠르게 파악할 수 있습니다.
