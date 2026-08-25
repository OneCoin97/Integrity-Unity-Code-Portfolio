using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UIModel))]
public class UIViewModel : MonoBehaviour
{
    public static UIViewModel getInst
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIViewModel>();
            }
            return instance;
        }
    }
    
    private static UIViewModel instance=null;
    private readonly Dictionary<string, UIView> uiViews = new Dictionary<string, UIView>();
    public UIModel model { get; private set; }
    
    private void Awake()
    {
        model = GetComponent<UIModel>();
    }

    public void changeFont(FontSet fontSet)
    {
        foreach (var uiView in uiViews.Values)
        {
            uiView?.changeFont(fontSet);
        }
    }
    
    #region Main
    
    public UIView getUIView(string key)
    {
        if (key != null)
        {
            uiViews.TryGetValue(key, out UIView result);
            
            return result;
        }

        return null;
    }
    
    public UIView makeUI(string dataKey, UIViewEntryInput input)
    {
        UIViewEntryInput entryInput = model.getUIData(UIType.Main, dataKey);

        if (entryInput != null)
        {
            if (entryInput.viewKey.Equals(""))
            {
                entryInput.viewKey= dataKey;
            }
            
            entryInput.addData(input);
            
            return makeUI(entryInput);
        }

        if (input != null)
        {
            input.viewKey = dataKey;
            return makeUI(input);
        }

        return null;
    }
    
    public UIView makeUI(UIViewEntryInput input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.viewKey))
        {
            return null;
        }

        UIView uiView = getUIView(input.viewKey);
        if (uiView != null)
        {
            uiView.makeUI(input);
        }

        return uiView;
    }
    

    #endregion
    
    #region Control
    
    public void disableAllUI()
    {
        foreach (var uiView in uiViews.Values)
        {
            uiView?.disableUIView();
        }
    }
    
    public void addUIView(string key,UIView uiView)
    {
        if (uiView == null || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!uiViews.TryAdd(key, uiView))
        {
            Debug.LogWarning($"중복된 UIView 키를 건너뜁니다: {key}", uiView);
        }
    }

    public void removeUIView(string key, UIView uiView)
    {
        if (string.IsNullOrWhiteSpace(key) || uiView == null)
        {
            return;
        }

        if (uiViews.TryGetValue(key, out UIView current) && current == uiView)
        {
            uiViews.Remove(key);
        }
    }

    #endregion

    #region SetSomething
   

     public void setEffect(string key, int index, int num)
    {
        UIView uiView = getUIView(key);
        if (uiView != null)
        {
            uiView.entrySet.setEffect(index,num);
        }
    }

    public void setNumber(string key, int index, float num)
    {
        UIView uiView = getUIView(key);
        if (uiView != null)
        {
            uiView.entrySet.setNumber(index,num);
        }
    }

    public void setNumber(string key, List<float> numbers)
    {
        UIView uiView = getUIView(key);
        if (uiView != null)
        {
            if (numbers != null)
            {
                uiView.entrySet.setNumbers(numbers);
            }
        }
    }


    public void setColor(string key, List<(int, Color)> colorData)
    {
        UIView uiView = getUIView(key);
        if (uiView != null)
        {
            if (colorData != null)
            {
                foreach (var valueTuple in colorData)
                {
                    uiView.entrySet.setColor(valueTuple.Item1,valueTuple.Item2);
                }
            }
        }
    }
    
    public void setUIActive(string key, bool value)
    {
        UIView uiView = getUIView(key);
        if (uiView != null)
        {
            if (value)
            {
                uiView.enableUIView();

            }
            else
            {
                uiView.disableUIView();
            }
            
        }
    }

    #endregion
}
