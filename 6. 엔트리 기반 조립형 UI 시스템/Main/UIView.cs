using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DDObjectPool;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Image)),RequireComponent(typeof(CanvasGroup)),RequireComponent(typeof(UVEFadeInOut))]
public class UIView : MonoBehaviour
{
    #region Variable

    #region Public
    
    public event Action enableEventStart;
    public event Action disableEventStart;
    public event Action enableEventEnd;
    public event Action disableEventEnd;
    
    public bool isActive { get; protected set; }
    public string getKey
    {
        get { return key; }
    }

    #endregion
    
    #region Inspector
    
    [Header("UIView")]
    [SerializeField] protected string key;
    [SerializeField] private bool isRealTime = false;
    [SerializeField] private bool active = true;
    public bool deactivateOnPressKey;
    
    [SerializeField] protected List<UIView> innerUIViews = new List<UIView>();
    
    #endregion
    
    protected List<UIViewEffect> uiViewEffects = new();
    protected LayoutGroup layoutGroup;
    protected RectTransform layoutGroupRect;
    protected CanvasGroup canvasGroup;
    
    public UIViewEntrySet entrySet { get; private set; }
    protected UIViewEntryInput currentData { get; private set; }
    
    private Coroutine activeCoroutine;
    private int previousShortage = -1;
    private UIViewModel registeredViewModel;
    
    protected readonly ObjectPoolManager<UIPrefab, int> objectPoolManager = new ObjectPoolManager<UIPrefab, int>();
    protected readonly List<UIPrefab> addedObjects = new List<UIPrefab>();
    
    #endregion

    protected virtual void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform poolParent;
        if (canvas != null)
        {
            poolParent = canvas.transform;
        }
        else
        {
            poolParent = transform;
        }

        entrySet = new UIViewEntrySet();
        entrySet.collectUIEntries(this);

        objectPoolManager.Initialize(poolParent, false, transform.parent);
       

        canvasGroup = GetComponent<CanvasGroup>();
        layoutGroup = GetComponentInChildren<LayoutGroup>();
        if(layoutGroup != null)
            layoutGroupRect = layoutGroup.GetComponent<RectTransform>();
       
        uiViewEffects = GetComponents<UIViewEffect>().ToList();
        registeredViewModel = UIViewModel.getInst;
        registeredViewModel.addUIView(key, this);

        isActive = active;
        setViewActive(active);
    }

    protected virtual void OnDestroy()
    {
        if (registeredViewModel != null)
        {
            registeredViewModel.removeUIView(key, this);
        }
    }
    
    #region Public API
    
    public virtual void applyInnerData(List<UIViewEntryInput> innerDataList)
    {
        if (currentData == null)
        {
            return;
        }

        currentData.ensureRuntimeData();
        currentData.innerDataList = innerDataList ?? new List<UIViewEntryInput>();
        entrySet.applyInnerData(currentData.innerDataList);

        if (currentData != null && currentData.uiPrefab != null)
        {
            int innerCount = currentData.uiPrefab.GetComponentsInChildren<UIInnerDataEntry>(true).Length;
            updateInnerIndex(innerCount, currentData.innerDataList);
        }
    }
    public virtual void applyActions(List<Action> actionList)
    {
        if (currentData == null)
        {
            return;
        }

        currentData.ensureRuntimeData();
        currentData.actionList = actionList ?? new List<Action>();
        entrySet.applyAction(currentData.actionList);
    }
    
    public void changeFont(FontSet fontSet)
    {
        foreach (var uiStringEntry in entrySet.textEntryList)
        {
            uiStringEntry.setFont(fontSet);
        }
    }

    public void setSpacing(float x,float y = 0)
    {
        if (layoutGroup != null)
        {
            switch (layoutGroup)
            {
                case HorizontalLayoutGroup horizontal:
                {
                    horizontal.spacing = x;
                }
                    break;
                case VerticalLayoutGroup vertical:
                {
                    vertical.spacing = x;
                }
                    break;
                case GridLayoutGroup grid:
                {
                    grid.spacing = new Vector2(x, y);
                }
                    break;
            }
        }
        
    }
    
    public virtual void makeUI(UIViewEntryInput entryInput)
    {
        makeUIDefault(entryInput);
    }

    public void updateUIValue(UIViewEntryInput entryInput)
    {
        if (entryInput == null) { Debug.Log("Null 입력"); return; }
        entryInput.ensureRuntimeData();
        currentData = entryInput;
        if (entryInput.uiPrefab != null)
        {
            int safety = 0;
            reset();
            previousShortage = -1;
            while (!hasEnoughSpace(entryInput) && safety++ < 100)
            {
                if (!makeAdditionalUI(entryInput.uiPrefab)) break;
            }

            if (safety >= 100)
            {
                Debug.LogWarning("반복 UI 생성이 안전 한도(100회)에 도달했습니다.", this);
            }
        }
        entrySet.applyData(entryInput);
        if (currentData != null && currentData.uiPrefab != null && entryInput.innerDataList.Count > 0)
        {
            int innerCount = currentData.uiPrefab.GetComponentsInChildren<UIInnerDataEntry>(true).Length;
            updateInnerIndex(innerCount, entryInput.innerDataList);
        }
        if (layoutGroupRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroupRect);
    }
    
    public virtual void enableUIView()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        
        UIViewController.getInst.pushUIView(this);
        try
        {
            enableEventStart?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        
        setViewActive(true);
        
        foreach (var uiViewEffect in uiViewEffects)
        {
            uiViewEffect?.enableEffect(isRealTime);
        }
        
        try
        {
            enableEventEnd?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        
        isActive = true;
    }

    public virtual void disableUIView()
    {
        UIViewController.getInst.removeUIView(this);
        
        if (isActive)
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }
            
            activeCoroutine = StartCoroutine(disableIE());
            
            try
            {
                disableEventStart?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        isActive = false;
    }
    #endregion

    #region Internal Helpers
    
    private void reset()
    {
        foreach (var obj in addedObjects) objectPoolManager.Destroy(obj);
        addedObjects.Clear();
        entrySet.collectUIEntries(this);
    }
    
    protected virtual void setViewActive(bool value)
    {
        foreach (Transform child in transform) child.gameObject.SetActive(value);
        canvasGroup.blocksRaycasts = value;
    }

    protected IEnumerator disableIE()
    {
        foreach (var uiView in innerUIViews)
        {
            uiView.disableUIView();
        }

        foreach (var effect in uiViewEffects)
        {
            effect?.disableEffect(isRealTime);
        }

        float ctime = 0;
        
        while (true)
        {
            bool end = true;

            foreach (var uiViewEffect in uiViewEffects)
            {
                end &= !uiViewEffect.isRunning;
                if (!end)
                {
                    break;
                }
            }

            ctime += isRealTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (ctime > 5 || end)
            {
                break;
            }

            if (isRealTime)
            {
                yield return new WaitForSecondsRealtime(0);
            }
            else
            {
                yield return null;
            }
        }

        setViewActive(false);
        
        try
        {
            disableEventEnd?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        
        foreach (var uiView in innerUIViews) // 이펙트 대기 중 다시 켜졌을 수 있어서 마지막에 한 번 더 비활성화
        {
            uiView.disableUIView();
        }
    }
    protected void makeUIDefault(UIViewEntryInput entryInput)
    {
        if (isActive)
        {
            foreach (var uiViewEffect in uiViewEffects)
            {
                uiViewEffect?.updateEffect(isRealTime);
            }
        }
        else
        {
            enableUIView();
        }
       
        updateUIValue(entryInput);
    }
    private void updateInnerIndex(int innerCount, List<UIViewEntryInput> innerDataList)
    {
        if (innerCount <= 0)
        {
            Debug.LogWarning("updateInnerIndex(): innerCount <= 0");
            return;
        }
        
        int count = 0;
        for (int i = 0; i < innerDataList.Count; i+=innerCount, count++)
        {
            innerDataList[i].innerIndex = count;
        }
    }
    protected bool hasEnoughSpace(UIViewEntryInput entryInput)
    {
        int shortage = 0;
        shortage += getShortage(entryInput.textCList, entrySet.textEntryList.Count);
        shortage += getShortage(entryInput.spriteList, entrySet.spriteEntryList.Count);
        shortage += getShortage(entryInput.colorList, entrySet.colorEntryList.Count);
        shortage += getShortage(entryInput.numberList, entrySet.numberEntryList.Count);

        if (hasEntrySlot<UIInnerDataEntry>(entryInput.uiPrefab))
        {
            shortage += getShortage(entryInput.innerDataList, entrySet.innerDataEntryList.Count);
        }

        if (hasEntrySlot<UIActionEntry>(entryInput.uiPrefab))
        {
            shortage += getShortage(entryInput.actionList, entrySet.buttonEntryList.Count);
        }

        if (shortage <= 0)
        {
            return true;
        }

        if (previousShortage == shortage)
        {
            Debug.LogWarning("추가 UI를 생성해도 엔트리 공간이 늘지 않습니다. UIPrefab 구성을 확인하세요.", this);
            return true;
        }

        previousShortage = shortage;
        return false;
    }

    private static int getShortage<T>(ICollection<T> data, int entryCount)
    {
        return data == null ? 0 : Mathf.Max(0, data.Count - entryCount);
    }

    private static bool hasEntrySlot<T>(UIPrefab prefab) where T : UIEntryBase
    {
        return prefab != null && prefab.GetComponentInChildren<T>(true) != null;
    }

    protected bool makeAdditionalUI(UIPrefab prefab)
    {
        if (layoutGroup == null)
        {
            Debug.Log("해당 UIView는 LayoutGroup이 없음");
            return false;
        }

        var ui = objectPoolManager.Instantiate(prefab);
        addedObjects.Add(ui);
        ui.transform.SetParent(layoutGroup.transform, false);
        entrySet.appendUIEntries(ui.transform,this);
        return true;
    }
    
    #endregion

}


