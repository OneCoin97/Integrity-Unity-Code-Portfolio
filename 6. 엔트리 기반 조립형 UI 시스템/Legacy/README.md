# Legacy

엔트리 기반 UI가 실제 프로젝트의 데이터, 풀, 사운드와 폰트 기능에 연결되는 데 필요한 연동 코드입니다.

## 연관 코드

- 'UIModel.cs' — UIType과 키를 기준으로 저장된 UIViewEntryInput을 조회
- 'ObjectPoolManager.cs' — UIView가 동적으로 추가하는 UIPrefab의 생성과 반환을 관리
- 'SoundController.cs' — UIViewEffect에서 사용하는 UI 사운드 재생 인터페이스
- 'FontSet.cs' — UIStringEntry가 사용하는 타입별 TMP 폰트 데이터
- 'TransitionUtility.cs' — Gauge와 화면 효과가 공유하는 보간 방식

핵심 조립 구조는 'Main'에 있으며, 이 폴더는 포트폴리오 코드가 외부 기능과 연결되는 경계를 보여줍니다.
