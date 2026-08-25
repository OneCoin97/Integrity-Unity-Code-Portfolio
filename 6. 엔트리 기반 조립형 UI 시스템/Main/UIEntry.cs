using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public abstract class UIEntryBase : MonoBehaviour
{
    [SerializeField, Min(1)] private int entrySlotCount = 1;

    public virtual int getEntrySlotCount()
    {
        return Mathf.Max(1, entrySlotCount);
    }
}

public abstract class UIEntry<T> : UIEntryBase
{
    protected T data;
    
    public T getData
    {
        get { return data; }
    }
   
    public virtual void setData(T data)
    {
        this.data = data;
        
        if (data != null)
        {
            processData();
        }
        else
        {
            processNullData();
        }
    }

    public virtual void setSlotData(int slotIndex, T data)
    {
        if (slotIndex == 0)
        {
            setData(data);
        }
    }

    protected abstract void processData();

    protected virtual void processNullData()
    {
        
    }
}

[RequireComponent(typeof(TMP_Text))]
public abstract class UIStringEntry : UIEntry<string>
{
    [SerializeField]
    protected FontType fontType;
    
    public abstract void setFont(FontSet fontSet);

}

public abstract class UIActionEntry : UIEntry<Action>
{
    
}

public abstract class UINumberEntry : UIEntry<float>
{
    
}

public abstract class UIEffectEntry : UIEntry<int>
{
    
}

public abstract class UIColorEntry : UIEntry<Color>
{
  
}

public abstract class UISpriteEntry : UIEntry<SpriteEntry>
{

}

/// <summary>
/// 다른 UIView에 전달할 데이터를 보관하는 엔트리.
/// 대상 UIView가 사용할 UIViewEntryInput을 전달하는 역할을 한다.
/// </summary>
public abstract class UIInnerDataEntry : UIEntry<UIViewEntryInput>
{
    public UIView owner;
    public string key = ""; // 전달할 uiview의 키
}
