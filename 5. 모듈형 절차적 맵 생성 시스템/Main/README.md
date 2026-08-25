# Main

스테이지 조건으로 레이아웃을 생성하고 Road·Room 파츠를 실제 맵 데이터로 조립하는 핵심 코드입니다.

## Layout

1. 'Layout\MapLayoutGenerator.cs' — 스테이지 옵션과 시드를 적용해 방 구성과 연결 정보를 가진 MapLayoutData를 생성
2. 'Layout\RouteSolver.cs' — 분기와 마스크 조건을 검사하며 각 레이아웃 항목을 연결할 경로를 탐색

## Build

1. 'Build\MapBuilder.cs' — MapLayoutData를 순회하며 도로와 방 배치를 조정하고 최종 Map을 반환
2. 'Build\BlockMapManager.cs' — 생성 과정의 블록 점유 상태와 배치 가능 여부를 관리
3. 'Build\RoadBuilder.cs' — 직선·분기·비밀 경로에 맞는 Road 파츠를 배치
4. 'Build\RoomBuilder.cs' — 일반·전투·비밀 방 조건에 맞는 Room 파츠를 배치

'MapLayoutGenerator' → 'RouteSolver' → 'MapBuilder' 순서로 보면 레이아웃 계산과 실제 파츠 조립이 분리된 구조를 빠르게 파악할 수 있습니다.
