using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Gauge : UINumberEntry
{
    private Image image;
    public float maxValue = 100f;

    [Header("Transition Option")]
    public bool immediate = false;
    public float lerpTime = 0.2f;
    public TransitionType transitionType = TransitionType.Linear;

    [Header("Custom Curve")]
    public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Coroutine lerpCoroutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.type = Image.Type.Filled;
    }

    protected override void processData()
    {
        float targetValue = data / maxValue;
        targetValue = Mathf.Clamp01(targetValue);

        if (immediate)
        {
            image.fillAmount = targetValue;
            return;
        }

        if (lerpCoroutine != null)
        {
            StopCoroutine(lerpCoroutine);
        }

        lerpCoroutine = StartCoroutine(fillLerp(targetValue));
    }

    private IEnumerator fillLerp(float targetValue)
    {
        float startValue = image.fillAmount;
        float time = 0f;

        while (time < lerpTime)
        {
            time += Time.deltaTime;

            float t = time / lerpTime;
            t = TransitionUtility.applyEasing(t,transitionType,customCurve);

            image.fillAmount = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        image.fillAmount = targetValue;
        lerpCoroutine = null;
    }

 
}
