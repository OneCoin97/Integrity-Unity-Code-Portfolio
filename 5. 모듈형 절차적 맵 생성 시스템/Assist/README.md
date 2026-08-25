# Assist

레이아웃 생성 조건, 조립 가능한 맵 파츠와 최종 출력 데이터를 정의하는 보조 계층입니다.

## Layout

- `Layout\MapLayoutStageOption.cs` — 스테이지별 방 구성과 생성 조건을 담는 ScriptableObject
- `Layout\MapLayoutData.cs` — 생성기가 만들고 Builder가 소비하는 레이아웃 결과와 항목 데이터
- `Layout\RouteMask.cs` — 경로 탐색에서 사용하는 방향별 점유 마스크
- `Layout\MapLayoutGeneratorUtility.cs` — 난수, 대기 정보와 분기 횟수 등 생성 과정의 상태 객체

## Build

- `Build\MapPartSetSO.cs` — Road·Room 파츠 묶음과 시드 기반 선택을 관리
- `Build\RoadPartSO.cs` — 도로의 기본·분기·비밀 경로 파츠 데이터
- `Build\RoomPartSO.cs` — 방 본체, 출구와 회전 가능한 파츠 데이터
- `Build\RoomSecretExitSO.cs` — 비밀 방 출구 파츠 데이터

## Map

- `Map\Map.cs` — 조립 완료 후 외부 시스템으로 전달하는 최종 맵 데이터
- `Map\BlockMap.cs` — 좌표별 블록 정보와 병합·회전·복사를 관리하는 공간 데이터
- `Map\RoomDatas.cs` — 생성된 방들의 위치와 속성 데이터
- `Map\TriggerMap.cs` — 맵 파츠에 포함된 트리거 영역의 병합과 조회 데이터
- `Map\RotateHelper.cs` — 파츠 회전에 따른 좌표와 방향 변환
- `DirUtility.cs` — 생성 전반에서 공유하는 방향 정의와 변환

Layout은 생성 단계의 계약, Build는 조립 재료, Map은 외부로 전달되는 결과라는 기준으로 분리했습니다.
