# Data Subscription

코어 관리자의 내부 구현을 직접 호출하지 않고 필요한 상태만 전달받기 위한 데이터 구독 계약입니다.

## 메인 코드

1. 'GameDataListener.cs' — 진행·게임 모드·파티·선택·전투 턴 데이터별 Listener와 전체 계약 정의
2. 'GMSubscriber.cs' — 전체 Listener를 구현하고 Unity 객체의 구독 생명주기와 최신 데이터 캐시를 공통 처리하는 기반 클래스

일부 데이터만 필요한 클래스는 개별 Listener를 구현하고, 여러 코어 데이터가 필요한 Unity 시스템은 'GMSubscriber.cs'를 상속해 자신의 내부 로직에 집중합니다.
