using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class NumberAction : UIEffectEntry
{
    private Image image;

    [Header("Common")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private float defaultDuration = 0.25f;

    [Header("Blink")]
    [SerializeField] private float blinkOffTime = 0.08f;
    [SerializeField] private float blinkOnTime = 0.12f;
    [SerializeField] private bool reverse = false;
                

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Pulse")]
    [SerializeField] private float pulseScaleMul = 1.15f;
    [SerializeField] private float pulseUpTime = 0.08f;
    [SerializeField] private float pulseDownTime = 0.12f;

    [Header("Shake")]
    [SerializeField] private RectTransform targetRect = null;
    [SerializeField] private float shakeMagnitude = 8f;
    [SerializeField] private int shakeVibrato = 9; // 진동 횟수
    [SerializeField] private float shakeDuration = 0.18f;

    [Header("Fade")]
    [SerializeField] private float fadeOutTime = 0.12f;
    [SerializeField] private float fadeInTime = 0.12f;

    private Coroutine running;
    private RectTransform cachedRect;
    
    private Color originalColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (targetRect == null)
        {
            cachedRect = GetComponent<RectTransform>();
        }
        else
        {
            cachedRect = targetRect;
        }
        originalColor = image.color;
    }

    protected override void processData()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }

        switch (data)
        {
            case 0:
                // 아무 것도 안 함
                break;

            case 1:
                running = StartCoroutine(coBlinkOnce());
                break;

            case 2:
                running = StartCoroutine(coFlashColorOnce());
                break;

            case 3:
                running = StartCoroutine(coPulseScaleOnce());
                break;

            case 4:
                running = StartCoroutine(coShakeOnce());
                break;

            case 5:
                running = StartCoroutine(coFadeInOutOnce());
                break;

            default:
                // 정의되지 않은 값이면 기본적으로 한 번 깜빡임
                running = StartCoroutine(coBlinkOnce());
                break;
        }
    }

    private IEnumerator coBlinkOnce()
    {
        if (image == null) yield break;
      

        if (reverse)
        {
            yield return setImageAlpha(1, blinkOnTime);
            yield return setImageAlpha(0, blinkOffTime);
        }
        else
        {
            yield return setImageAlpha(0, blinkOffTime);
            yield return setImageAlpha(1, blinkOnTime);
        }


        image.color = originalColor;
        running = null;
    }

    private IEnumerator coFlashColorOnce()
    {
        if (image == null) yield break;

        Color original = image.color;
        float t = 0f;
        float dur = flashDuration > 0f ? flashDuration : defaultDuration;

        // 절반 동안 flashColor로 보간
        while (t < dur * 0.5f)
        {
            t += deltaTime();
            float p = Mathf.Clamp01(t / (dur * 0.5f));
            image.color = Color.Lerp(original, flashColor, p);
            yield return null;
        }

        // 나머지 절반 동안 원래 색으로 보간
        t = 0f;
        while (t < dur * 0.5f)
        {
            t += deltaTime();
            float p = Mathf.Clamp01(t / (dur * 0.5f));
            image.color = Color.Lerp(flashColor, original, p);
            yield return null;
        }

        image.color = original;
        running = null;
    }

    private IEnumerator coPulseScaleOnce()
    {
        if (cachedRect == null) yield break;

        Vector3 originalScale = cachedRect.localScale;
        Vector3 targetScale = originalScale * pulseScaleMul;

        float tUp = 0f;
        float upTime = pulseUpTime > 0f ? pulseUpTime : defaultDuration * 0.5f;

        while (tUp < upTime)
        {
            tUp += deltaTime();
            float p = Mathf.Clamp01(tUp / upTime);
            cachedRect.localScale = Vector3.LerpUnclamped(originalScale, targetScale, easeOutQuad(p));
            yield return null;
        }

        float tDown = 0f;
        float downTime = pulseDownTime > 0f ? pulseDownTime : defaultDuration * 0.5f;

        while (tDown < downTime)
        {
            tDown += deltaTime();
            float p = Mathf.Clamp01(tDown / downTime);
            cachedRect.localScale = Vector3.LerpUnclamped(targetScale, originalScale, easeInQuad(p));
            yield return null;
        }

        cachedRect.localScale = originalScale;
        running = null;
    }

    private IEnumerator coShakeOnce()
    {
        if (cachedRect == null) yield break;

        Vector2 originalPos = cachedRect.anchoredPosition;
        float dur = shakeDuration > 0f ? shakeDuration : defaultDuration;
        int vib = shakeVibrato > 0 ? shakeVibrato : 6;

        float elapsed = 0f;
        int step = 0;

        while (elapsed < dur)
        {
            elapsed += deltaTime();
            step += 1;

            float decay = 1f - (elapsed / dur);
            float angle = (float)step * 36.0f; // 회전하면서 진동 방향 변경
            float rad = angle * Mathf.Deg2Rad;

            float offsetX = Mathf.Cos(rad) * shakeMagnitude * decay;
            float offsetY = Mathf.Sin(rad) * shakeMagnitude * decay;

            cachedRect.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);

            // 진동 횟수 조절 (대략적으로 vib에 수렴하도록)
            if (step >= vib) step = 0;

            yield return null;
        }

        cachedRect.anchoredPosition = originalPos;
        running = null;
    }

    private IEnumerator coFadeInOutOnce()
    {
        if (image == null) yield break;

        Color original = image.color;

        // 페이드 아웃
        float tOut = 0f;
        float outTime = fadeOutTime > 0f ? fadeOutTime : defaultDuration * 0.5f;

        while (tOut < outTime)
        {
            tOut += deltaTime();
            float p = Mathf.Clamp01(tOut / outTime);
            setImageAlpha(1f - p);
            yield return null;
        }

        // 페이드 인
        float tIn = 0f;
        float inTime = fadeInTime > 0f ? fadeInTime : defaultDuration * 0.5f;

        while (tIn < inTime)
        {
            tIn += deltaTime();
            float p = Mathf.Clamp01(tIn / inTime);
            setImageAlpha(p);
            yield return null;
        }

        image.color = original;
        running = null;
    }

    private Coroutine alphaRoutine;

    private IEnumerator setImageAlpha(float targetAlpha, float duration = 0.25f)
    {
        if (image == null) yield break;

        duration = Mathf.Max(0.01f, duration);
        Color c = image.color;
        float startAlpha = c.a;
        float time = 0f;

        while (time < duration)
        {
            time += deltaTime();
            float t = time / duration;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            image.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        image.color = c;
        alphaRoutine = null;
    }

    public void ChangeAlphaSmooth(float alpha, float duration = 0.25f)
    {
        if (alphaRoutine != null)
            StopCoroutine(alphaRoutine);

        alphaRoutine = StartCoroutine(setImageAlpha(alpha, duration));
    }


    private float deltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private float easeOutQuad(float x)
    {
        return 1f - (1f - x) * (1f - x);
    }

    private float easeInQuad(float x)
    {
        return x * x;
    }
}
