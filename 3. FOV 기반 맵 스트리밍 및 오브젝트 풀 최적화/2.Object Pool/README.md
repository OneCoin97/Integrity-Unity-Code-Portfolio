# Object Pool

블록 인스턴스와 고빈도 FOV 계산에서 사용하는 임시 객체를 재사용해 Instantiate / Destroy와 GC 할당을 줄이는 코드입니다.

FOV 갱신은 유닛 이동 중 Unity 메인 스레드에서 반복적으로 실행됩니다. 순간적인 GC 발생이 프레임 끊김으로 이어질 수 있어 블록뿐 아니라 계산용 HashSet과 FovStreamingState도 풀링해 관리 힙 할당을 줄였습니다.

## 블록 풀

1. `ObjectPoolManager.cs` — 프리팹별 재사용 Queue와 수량을 관리하고, 한도를 넘으면 오래 사용하지 않은 풀부터 정리합니다. 맵 전환 전에 예약된 지연 반환이 새로운 풀에 섞이는 것도 차단합니다.

## 계산 메모리 재사용

- `HashSetPool.cs` — 생성·제거·유지 영역 계산에 사용하는 임시 HashSet 재사용
- `ClassPool.cs` — FOV별 위치와 점유 좌표를 보관하는 FovStreamingState 재사용
