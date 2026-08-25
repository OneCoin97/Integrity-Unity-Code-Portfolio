using System.Collections;
using UnityEngine;


public class UVEFadeInOut : UIViewEffect
{
    [SerializeField] protected float inTime = 0.1f;
    [SerializeField] protected float outTime = 0.1f;
    [SerializeField] protected TransitionType transitionType = TransitionType.EaseInOut;
    [Tooltip("CustomCurve일 때 사용")]
    [SerializeField] protected AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    protected CanvasGroup canvasGroup;
    [SerializeField]private bool isBlink =false;


    private Coroutine coroutine;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    protected override void enableEffectDefault()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        if (isBlink)
        {
            coroutine = StartCoroutine(blinkIE());
        }
        else
        {
            coroutine = StartCoroutine(setAlphaIE(1, inTime));
        }
    }

    protected override void disableEffectDefault()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
        coroutine = StartCoroutine(setAlphaIE(0, outTime));
    }

    
    private IEnumerator blinkIE()
    {
        // 무한 반복: 0 ↔ 1 알파 전환
        while (true)
        {
            // 페이드 인
            yield return setAlphaIE(1f, getSafeDuration(inTime));

            // 페이드 아웃
            yield return setAlphaIE(0f, getSafeDuration(outTime));
        }
    }
    
    private float getSafeDuration(float v)
    {
        if (v < 0f) return 0f;
        if (v < 0.0001f) return 0f;
        return v;
    }
    
    protected IEnumerator setAlphaIE(float target,float duration)
    {
        isRunning = true;
        
        yield return waitNextFrameIE();
        
        float elapsed     = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < duration)
        {
            elapsed += getCurrentDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            t = TransitionUtility.applyEasing(t,transitionType,customCurve);     // 선택한 전환 곡선 적용
            canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, target, t);
            yield return waitNextFrameIE();
        }
        
        canvasGroup.alpha = target;
        
        isRunning = false;
    }
    
    
   
}

