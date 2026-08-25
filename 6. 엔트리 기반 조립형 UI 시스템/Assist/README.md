# Assist

등록된 UIView 조회와 데이터 주입, 화면 스택 제어와 데이터 입력 형식을 담당하는 보조 계층입니다.

## 화면 관리

1. `UIViewModel.cs` — 키로 등록된 UIView를 조회하고 데이터와 Action을 주입하는 외부 접근 지점
2. `UIViewController.cs` — 활성 UIView 스택의 Push·Pop과 입력에 따른 닫기 흐름을 관리

## Data

- `Data\UIDataInspector.cs` — 텍스트·Sprite·색상·숫자와 추가 프리팹을 묶은 UIViewEntryInput 정의
- `Data\UIPrefab.cs` — 동적으로 생성되는 UI를 표현할 때 사용하는 프리팹 데이터
- `Data\SpriteEntry.cs` — Sprite와 표시 위치·크기 정보를 함께 전달하는 입력 객체

외부 시스템은 `UIViewModel`을 통해 화면을 요청하고, Main의 UIView와 Entry는 전달된 데이터의 표현에 집중하도록 구분했습니다.
