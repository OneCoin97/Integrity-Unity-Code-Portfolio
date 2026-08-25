# 게임 런타임 코어 시스템

1,936줄의 기존 GameManager를 이벤트 코어와 책임별 관리자로 분리한 출시 후 리팩터링 사례입니다.

- [런타임 코어 시스템 상세 설계](https://app.notion.com/p/3b9650b00a5c8122b695f06089c4d138)
- [GameManager 리팩터링 전후 분석](https://app.notion.com/p/3b9650b00a5c81b4928bea0159225972)
- '0.Before Refactoring': 리팩터링 이전 GameManager 원본
- '1.Event Core': 이벤트 위치 관리, 구독 등록·해제, 변경 시 갱신되는 실행 스냅샷과 상태 이벤트 실행 계약
- '2.Core Managers': 데이터, 파티, 선택과 턴 책임별 관리자
- '3.Data Subscription': 데이터별 Listener와 공통 구독 생명주기
- '4.State Machine': 전투·탐험 상태와 전이 조건 ('IGameModeEvent' 실행 계약만 전달받아 연결)
- '5.Integration': 입력 및 Unity 생명주기와 코어 시스템 연결
