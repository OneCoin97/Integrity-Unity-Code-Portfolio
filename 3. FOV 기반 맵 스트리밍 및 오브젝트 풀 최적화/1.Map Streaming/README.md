# Map Streaming

FOV 중첩 후보를 거리로 선별하고 이전·현재 영역의 HashSet 차집합만 View에 반영하는 스트리밍 코드입니다.

## 메인 코드

1. 'FovManager.cs' — 거리 기반 중첩 후보 선별, FOV별 상태 관리와 블록 생성·제거 좌표 계산
2. 'MapFovUpdateManager.cs' — FixedUpdate 요청 큐, 즉시 갱신과 계산 결과 적용
3. 'TemporaryAreaFov.cs' — 스킬 경로와 범위를 기존 FOV 계산 흐름에 전달하는 데이터형 FOV

## 계약과 변경 데이터

- 'Fov.cs' — 위치·생성 범위·생명주기를 제공하는 스트리밍 입력 계약
- 'MapFormationMaterial.cs' — FOV 갱신 요청과 생성·제거 블록 목록
- 'MapFovTransition.cs' — FOV 제거 시 시각 전환이 필요한 컴포넌트의 선택적 연출 계약

'FovManager.setRange'의 변경 영역 계산을 먼저 확인한 뒤 'MapFovUpdateManager'의 큐·즉시 적용 경로를 보면 전체 흐름을 빠르게 파악할 수 있습니다.
