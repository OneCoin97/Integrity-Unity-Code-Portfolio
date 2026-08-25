# 모듈형 절차적 맵 생성 시스템

제약 조건을 만족하는 LayoutData를 생성하고 미리 제작한 Road·Room 파츠를 조립하는 Data-Driven 절차적 맵 생성 시스템입니다.

- [상세 설계 및 측정 결과](https://app.notion.com/p/5b4650b00a5c8274979281dbfa7f5094)
- 'Main': 레이아웃 생성, 경로 탐색과 맵 파츠 조립
- 'Assist': 생성 옵션, 레이아웃과 맵 출력 데이터
- 'Legacy': 출력 데이터가 참조하는 최소 공용 계약
- 'MapGenerationUsageExample.cs': 시드와 스테이지 옵션 기반 생성 예시
