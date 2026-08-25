using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스를 올리면 데이터를 key 값과 매칭되는 UIView 에 전송하고,
/// moveMode 가 true 면 요소 오른쪽에 고정 위치로 UIView 를 소환.
/// </summary>

public class HoverTransmitter : UIInnerDataEntry, IPointerEnterHandler, IPointerExitHandler
{
    public bool moveMode;                         // 고정 소환 여부
    public Vector2 offset = new Vector2(20f, 0f); // 오른쪽 20px

    UIView currentView;
    Canvas rootCanvas;

    private bool isHover;

    private void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(data == null || data.dummy) return;

        data.viewKey = key;
        currentView = UIViewModel.getInst.makeUI(data); // UIView 반환

        if(moveMode && currentView != null) placeNextToSelf();
        isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;
        UIViewModel.getInst.setUIActive(key, false);
        currentView = null;
    }

    void placeNextToSelf()
    {
        if (currentView == null || rootCanvas == null) return;

        RectTransform selfRect = GetComponent<RectTransform>();
        RectTransform viewRect = currentView.GetComponent<RectTransform>();
        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();

        // ① 레이아웃 강제 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewRect);

        // ② ‘오른쪽 중앙’ 월드 좌표 → 스크린 좌표
        Vector3[] selfCorners = new Vector3[4];
        selfRect.GetWorldCorners(selfCorners); // 0:BL,1:TL,2:TR,3:BR
        Vector3 rightCenterWorld = (selfCorners[2] + selfCorners[3]) * 0.5f;

        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        Vector2 baseScreenPos = RectTransformUtility.WorldToScreenPoint(cam, rightCenterWorld);
        Vector2 screenPos = baseScreenPos + offset; // 기본 위치(오른쪽)

        // ③ 뷰 크기(픽셀) & 피봇 오프셋
        Vector2 viewSize = viewRect.rect.size * rootCanvas.scaleFactor;
        Vector2 pivotOffset = new Vector2(
            viewSize.x * viewRect.pivot.x,
            viewSize.y * viewRect.pivot.y
        );

        float canvasW = canvasRect.rect.width;
        float canvasH = canvasRect.rect.height;

        // ④ 가로 오버플로 체크
        float rightEdge = screenPos.x + viewSize.x - pivotOffset.x;
        float leftEdge = screenPos.x - pivotOffset.x;

        if (rightEdge > canvasW)
        {
            screenPos.x -= rightEdge - canvasW;
        }
        else if (leftEdge < 0f)
        {
            screenPos.x += -leftEdge;
        }

        // ⑤ 세로 오버플로 체크
        float topEdge = screenPos.y + viewSize.y - pivotOffset.y;
        float bottomEdge = screenPos.y - pivotOffset.y;

        if (topEdge > canvasH)
        {
            screenPos.y -= topEdge - canvasH;
        }
        else if (bottomEdge < 0f)
        {
            screenPos.y += -bottomEdge;
        }

        // ⑥ 스크린 → 캔버스 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, cam, out Vector2 localPos);

        // ⑦ 적용(pivot 그대로)
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.zero;
        viewRect.anchoredPosition = localPos;
    }


    protected override void processData()
    {
        if (isHover)
        {
            if(data == null) return;
        
            data.viewKey = key;
            currentView = UIViewModel.getInst.makeUI(data); // UIView 반환
        }
    }
}