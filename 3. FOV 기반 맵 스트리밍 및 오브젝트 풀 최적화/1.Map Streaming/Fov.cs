using UnityEngine;

/// <summary>
/// 해당 인터페이스는 시야를 관리하며 블럭을 생성시키는(시야를 밝히는) 모든 유닛에게 상속됨
/// </summary>


public interface Fov 
{
    // 구현체 제공 값.
    // 현재 위치와 맵 생성 범위는 각 FOV 구현체가 자신의 상태에 맞게 제공·변경한다.
    Vector3 getCurrentPosition();
    public int creationRange { get; set; } // 해당 FOV의 맵 생성 범위

    // 구현체가 보관하는 생명주기 상태값.
    // 값의 변경과 평가는 MapFovUpdateManager만 담당한다.
    // 영구 파괴 명령(isDestroy == true)이 처리된 뒤 들어오는 FOV 갱신을 차단하기 위해
    // FOV 객체의 수명에 맞춰 저장한다. 구현체에서는 이 값을 직접 판단하거나 변경하지 않는다.
    // 일시적인 맵 제거(isDestroy == false)는 종료 상태로 취급하지 않는다.
    public bool isEnd { get; set; }
}
