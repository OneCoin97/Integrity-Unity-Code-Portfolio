# Core Managers

게임 진행에 필요한 원본 데이터와 도메인 규칙을 데이터·파티·선택·전투·탐험 책임으로 분리한 관리자입니다.

## 책임별 관리자

1. 'GameDataManager.cs' — 스테이지·게임 모드·엔딩 상태의 저장·복원과 데이터 변경 전파
2. 'PartyManager.cs' — 아군·적 파티 원본과 파티 변경 및 유닛 사망 흐름 관리
3. 'UnitSelectionManager.cs' — 현재·이전 선택 유닛, 선택 잠금·가능 조건과 선택 데이터 저장·전파
4. 'CombatTurnManager.cs' — 전투 턴 상태 머신, 턴 종료 요청, 턴 카운터와 영구 전투 횟수 관리
5. 'AdventureTurnManager.cs' — 탐험 상태 머신과 탐험 모드 실행·중단 및 타이머 관리

각 관리자는 자신의 데이터 변경을 내부에서 결정하고 Listener에는 조회에 필요한 상태만 전달합니다. 'UnitSelectionManager.cs'는 파티와 전투 턴 데이터를 Listener 계약으로 받아 선택 도메인의 판단에 사용합니다.
