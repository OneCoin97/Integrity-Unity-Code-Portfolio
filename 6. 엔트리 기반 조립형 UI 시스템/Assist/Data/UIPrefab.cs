using DDObjectPool;
using UnityEngine;

public class UIPrefab : ObjectPoolEntry<int>
{
    private RectTransform rectTransform;
    private CanvasGroup   canvasGroup;
    private float         baseAlpha = 1f;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) rectTransform = gameObject.AddComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        baseAlpha       = canvasGroup != null ? canvasGroup.alpha : 1f;
    }

    public override void initializeObject()
    {
        canvasGroup.alpha = baseAlpha;
    }

}