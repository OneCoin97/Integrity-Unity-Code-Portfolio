using System.Collections;

/// <summary>
/// FOV가 제거되거나 다시 활성화될 때 실행할 시각 전환 계약입니다.
/// MapFovUpdateManager는 구체 컴포넌트를 알지 않고 이 인터페이스만 호출합니다.
/// </summary>
public interface MapFovTransition
{
    IEnumerator turnOn();
    IEnumerator turnOff();
}
