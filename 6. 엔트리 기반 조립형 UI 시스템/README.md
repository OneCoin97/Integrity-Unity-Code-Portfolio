# 엔트리 기반 조립형 UI 시스템

UI마다 텍스트, 이미지, 게이지와 버튼을 전용 스크립트에서 직접 연결하면 비슷한 화면이 늘어날수록 데이터 전달 코드와 화면별 관리 코드가 반복됩니다. 이를 줄이기 위해 각 UI 요소가 자신의 값과 동작을 처리하는 `UIEntry` 구조를 만들었습니다.

예를 들어 `TMP_Text`에는 `TextViewer` Entry를 붙입니다. 이 Entry는 문자열을 전달받으면 자신이 관리하는 `TMP_Text.text`를 변경합니다. 같은 방식으로 이미지에는 Sprite를 처리하는 Entry, 게이지에는 숫자를 처리하는 Entry, 버튼에는 Action을 처리하는 Entry를 배치합니다. 하나의 화면은 이렇게 필요한 Entry들을 `UIView` 아래에 조합해 구성합니다.

화면을 사용하는 외부 시스템은 `TMP_Text`나 `Image` 같은 UI 컴포넌트를 직접 찾고 변경하지 않습니다. `UIViewEntryInput`에 화면에 표시할 데이터만 담아 전달하면 `UIViewEntrySet`이 타입과 배치 순서에 맞는 Entry로 데이터를 나눠주고, 각 Entry가 자신이 담당하는 UI에 값을 적용합니다.

- [상세 설계 및 적용 사례](https://app.notion.com/p/ef6650b00a5c83849672017b765e5981)

## 데이터 전달 흐름

1. 외부 시스템이 `UIViewModel`에 동작시킬 UI인 `UIView`의 고유 키와 `UIViewEntryInput`을 전달합니다.
2. `UIViewModel`은 고유 키에 해당하는 `UIView`를 찾아 데이터를 전달합니다.
3. `UIView`는 자식의 Entry를 `UIViewEntrySet`으로 수집하고, 전달받은 데이터를 타입과 등록 순서에 맞는 Entry로 분배합니다.
4. `UIEntry<T>` 구현은 자신에게 전달된 값만 실제 Unity UI 컴포넌트에 반영합니다.
5. 화면 활성화와 비활성화 시점에는 `UIViewEffect`를 통해 Fade와 Sound 같은 연출을 함께 실행합니다.

정적으로 준비한 번역 문장과 Sprite뿐 아니라 런타임에 계산한 유닛 스탯, 게이지 값과 버튼 Action도 같은 입력 구조로 전달할 수 있습니다. 새로운 표현이 필요할 때는 `UIView`의 분기문을 늘리지 않고 해당 데이터 타입을 처리하는 Entry를 추가합니다.

현재 데이터와 Entry의 연결은 타입별 등록 순서를 기준으로 하므로, 같은 타입의 값이 여러 개라면 입력 순서와 화면에 배치한 Entry 순서가 일치해야 합니다.

## 폴더 구성

- `Main`: Entry 계약, Entry 수집 및 데이터 분배, 화면 생명주기와 구체 UI 표현
- `Assist`: 외부 접근용 ViewModel, 화면 스택 Controller와 UI 입력 데이터
- `Legacy`: 오브젝트 풀, 사운드, 폰트처럼 실제 프로젝트 기능과 연결되는 최소 연동 코드

`Main/UIEntry.cs`, `UIViewEntrySet.cs`, `UIView.cs` 순서로 읽으면 값 하나의 표현이 전체 화면으로 조립되는 과정을 파악할 수 있습니다. 이후 `Assist/UIViewModel.cs`에서 외부 시스템이 이 구조를 어떻게 호출하는지 확인하는 것을 권장합니다.
