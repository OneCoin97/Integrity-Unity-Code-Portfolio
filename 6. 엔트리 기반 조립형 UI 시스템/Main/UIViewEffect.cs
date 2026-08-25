using System;
using System.Collections;
using UnityEngine;


[RequireComponent(typeof(UIView))]
public abstract class UIViewEffect : MonoBehaviour
{
    public bool isRunning { get; protected set; }
    protected bool isRealTime;
    public bool useUpdateEffect;
    protected UIView uiView;
    private readonly WaitForSecondsRealtime nextRealtimeFrame = new WaitForSecondsRealtime(0f);
    private readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
    private readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    // 프레임 넘기기
    protected IEnumerator waitNextFrameIE()
    {
        if (isRealTime)
        {
            yield return nextRealtimeFrame;
        }
        else
        {
            yield return null;
        }
    }

    // 지정된 시간 대기
    protected IEnumerator waitSecondsIE(float seconds)
    {
        if (isRealTime)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    // EndOfFrame
    protected IEnumerator waitForEndOfFrameIE()
    {
        // EndOfFrame은 timeScale 영향 안 받음. 그대로 리턴
        yield return waitForEndOfFrame;
    }

    // FixedUpdate 대기
    protected IEnumerator waitForFixedUpdateIE()
    {
        // FixedUpdate는 timeScale 영향 받음. Realtime 대응 없음
        yield return waitForFixedUpdate;
    }

    // deltaTime 가져오기
    protected float getCurrentDeltaTime()
    {
        if (isRealTime)
        {
            return Time.unscaledDeltaTime; // 타임스케일 무시
        }
        else
        {
            return Time.deltaTime; // 타임스케일 적용
        }
    }

    // 현재 시각(누적)
    protected float getCurrentTime()
    {
        if (isRealTime)
        {
            return Time.realtimeSinceStartup; // 타임스케일 무시 누적 시간
        }
        else
        {
            return Time.time; // 타임스케일 적용 누적 시간
        }
    }
    
    protected virtual void Awake()
    {
        uiView = GetComponent<UIView>();
        isRunning = false;
    }

    public virtual void updateEffect(bool isRealTime)
    {
        if (useUpdateEffect)
        {
            enableEffect(isRealTime);
        }
    }

    public virtual void enableEffect(bool isRealTime)
    {
        this.isRealTime = isRealTime;
        enableEffectDefault();
    }
    public virtual void disableEffect(bool isRealTime)
    {
        this.isRealTime = isRealTime;
        disableEffectDefault();
    }



    protected abstract void enableEffectDefault();
    protected abstract void disableEffectDefault();
  
}
