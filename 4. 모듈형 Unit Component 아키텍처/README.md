# 모듈형 Unit Component 아키텍처

Unit 기능을 독립 컴포넌트로 조립하고 공용 데이터의 소유권과 변경 API를 분리한 게임플레이 아키텍처입니다.

- [상세 설계 및 리팩터링 결과](https://app.notion.com/p/3b9650b00a5c81ab9751f4184331e2ad)
- '1.Core Architecture': 타입 기반 Component Registry, 공용 참조, Unit 흐름 조정과 내부 이벤트
- '2.Data Management': 직렬화 데이터, 변경 API와 영역별 저장
- '3.UnitComponent Examples': Stats·Statuses·Resources·State로 분리한 전투 컴포넌트 4종
