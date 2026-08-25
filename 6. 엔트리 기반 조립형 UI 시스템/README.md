# 엔트리 기반 조립형 UI 시스템

표시할 값과 동작을 UIEntry 계층으로 분리하고 UIView가 필요한 엔트리와 화면 효과를 조립하는 UI 시스템입니다.

- [상세 설계 및 적용 사례](https://app.notion.com/p/ef6650b00a5c83849672017b765e5981)
- 'Main': Entry, View, 화면 효과와 구체 UI 구현
- 'Assist': ViewModel, Controller와 UI 데이터 보조 계층
- 'Legacy': 실제 프로젝트 연결에 필요한 기존 연동 코드
- 'Main/UIView.cs': 엔트리 조립, 데이터 주입과 화면 생명주기를 연결하는 메인 코드
