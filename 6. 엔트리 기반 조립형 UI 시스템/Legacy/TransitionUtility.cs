using UnityEngine;

public static class TransitionUtility
{
    public static float applyEasing(float t,TransitionType transitionType,AnimationCurve customCurve = null)
    {
        switch (transitionType)
        {
            case TransitionType.SmoothStep:
                return t * t * (3f - 2f * t);                 // SmoothStep
            case TransitionType.EaseIn:
                return t * t;                                 // Quadratic In
            case TransitionType.EaseOut:
                return 1f - (1f - t) * (1f - t);              // Quadratic Out
            case TransitionType.EaseInOut:
                return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
            case TransitionType.CustomCurve:
                return customCurve != null ? customCurve.Evaluate(t) : t;
            case TransitionType.Linear:
            default:
                return t;
        }
    }
}

public enum TransitionType
{
    Linear,
    SmoothStep,
    EaseIn,
    EaseOut,
    EaseInOut,
    CustomCurve
}