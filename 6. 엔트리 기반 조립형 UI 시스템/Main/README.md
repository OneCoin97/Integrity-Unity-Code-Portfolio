# Main

표시 값과 동작을 타입별 Entry로 분리하고, UIView가 필요한 Entry와 화면 효과를 조립하는 핵심 코드입니다.

## Core

1. `UIEntry.cs` — 문자열·숫자·색상·Sprite·Action 등 입력 타입별 UI 처리 계약을 정의
2. `UIViewEntrySet.cs` — 자식 Entry를 타입별로 수집하고 입력 데이터를 대응하는 Entry에 분배
3. `UIView.cs` — 데이터 주입, 추가 프리팹 구성과 화면 활성·비활성 생명주기를 조정
4. `UIViewEffect.cs` — UIView의 열기·닫기 흐름에 결합되는 화면 효과의 공통 계약

## UIEntries

- `UIEntries\TextViewer.cs` — 문자열과 폰트를 TMP_Text에 반영
- `UIEntries\SpriteViewer.cs` — Sprite와 선택적인 크기·위치 값을 Image에 반영
- `UIEntries\Gauge.cs` — 숫자 입력을 즉시 또는 보간 방식으로 Gauge에 반영
- `UIEntries\ButtonActionBinder.cs` — Action 입력을 Button 이벤트에 연결
- `UIEntries\TextColorChanger.cs` — Color 입력을 텍스트 색상에 반영
- `UIEntries\NumberAction.cs` — 숫자 명령으로 위치·크기·알파 등 화면 값을 변경
- `UIEntries\HoverTransmitter.cs` — 포인터 진입·이탈 시 내부 UIView 데이터를 전달

## UIViewEffects

- `UIViewEffects\UVEFadeInOut.cs` — CanvasGroup 기반 화면 전환
- `UIViewEffects\UVESound.cs` — 화면 열기·닫기 시점의 UI 사운드 재생

`UIEntry` → `UIViewEntrySet` → `UIView` 순서로 보면 개별 표시 로직이 하나의 화면으로 조립되는 구조를 파악할 수 있습니다.
