# Event Core

게임 시스템이 전달한 동기·비동기 함수를 이벤트와 상태별 채널에 등록하고 정해진 순서대로 실행하는 코어입니다.

## 메인 코드

1. 'GameManager.cs' — 이벤트 위치를 소유하고 등록·해제·실행 API만 제공하는 최상위 이벤트 오케스트라
2. 'GameEventSubscriptions.cs' — 구독 추가·삭제, 우선순위 삽입, 변경 시 실행 스냅샷 갱신과 순차 실행 관리
3. 'GameEventSubscription.cs' — Action 또는 Awaitable 콜백과 우선순위·순서·일회성 정보를 보관하는 구독 단위
4. 'GameModeEvent.cs' — 상태별 Enter·Exit 채널을 연결하고 'IGameModeEvent' 실행 계약만 상태 머신에 전달

'GameManager.cs'의 공개 API를 먼저 확인한 뒤 'GameEventSubscriptions.cs', 'GameEventSubscription.cs', 'GameModeEvent.cs' 순서로 보면 위치·컬렉션·구독 단위·상태 연결의 책임을 빠르게 파악할 수 있습니다.
