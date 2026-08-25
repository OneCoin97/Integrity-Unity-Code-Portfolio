using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIDataInspector", menuName = "UIDataInspector", order = 1)]
public class UIDataInspector : ScriptableObject
{
    public UIViewEntryInput data = new UIViewEntryInput();
    
    public UIDataInspector deepCopy()
    {
        UIDataInspector result = CreateInstance<UIDataInspector>();
        result.name = this.name;
        result.data = data.deepCopy();
        
        return result;
    }
}
[Serializable]
public struct StringCapsule
{
    [TextArea] 
    public string text;

    public StringCapsule(string value)
    {
        text = value;
    }
    
    // += 연산자 (실제 string과 연동되게)
    public static StringCapsule operator +(StringCapsule lhs, string rhs)
    {
        return new StringCapsule(lhs.text + rhs);
    }

    // string + StringCapsule
    public static StringCapsule operator +(string lhs, StringCapsule rhs)
    {
        return new StringCapsule(lhs + rhs.text);
    }

    // == 연산자
    public static bool operator ==(StringCapsule lhs, StringCapsule rhs)
    {
        return lhs.text == rhs.text;
    }

    public static bool operator !=(StringCapsule lhs, StringCapsule rhs)
    {
        return !(lhs == rhs);
    }

    // string과의 == 비교
    public static bool operator ==(StringCapsule lhs, string rhs)
    {
        return lhs.text == rhs;
    }

    public static bool operator !=(StringCapsule lhs, string rhs)
    {
        return !(lhs == rhs);
    }

    // string으로의 암시적 변환
    public static implicit operator string(StringCapsule capsule)
    {
        return capsule.text;
    }

    // string에서의 암시적 변환
    public static implicit operator StringCapsule(string str)
    {
        return new StringCapsule(str);
    }

    // Equals / GetHashCode는 struct의 == 연산자 쓸 때 권장됨
    public override bool Equals(object obj)
    {
        if (!(obj is StringCapsule)) return false;
        return this == (StringCapsule)obj;
    }

    public override int GetHashCode()
    {
        return text != null ? text.GetHashCode() : 0;
    }

    public override string ToString()
    {
        return text;
    }
}
// -----------------------------------------------------------------------------
// UIViewEntryInput
//   - 하나의 ViewEntry(Data + Prefab)에 대한 입력 버퍼 역할
//   - addData / removeData 로 부분 삽입·삭제 지원
//   - "프리팹에 존재하는 컴포넌트 종류 수" 에 맞춰 각 리스트를 동기화
// -----------------------------------------------------------------------------
[Serializable]
public class UIViewEntryInput
{
    public int innerIndex;
    public string viewKey;
    public List<StringCapsule> textCList = new List<StringCapsule>();
    public List<SpriteEntry> spriteList = new();
    public List<Color> colorList = new();
    public List<float> numberList = new();
    [NonSerialized] public List<UIViewEntryInput> innerDataList = new();
    [NonSerialized] public List<Action> actionList = new();
    public bool dummy;
    
    public UIPrefab uiPrefab;

    public UIViewEntryInput(bool dummy)
    {
        this.dummy = dummy;
    }
    public UIViewEntryInput()
    {
        
    }
    public UIViewEntryInput(UIPrefab uiPrefab)
    {
        this.uiPrefab = uiPrefab;
    }

    public void ensureRuntimeData()
    {
        innerDataList ??= new List<UIViewEntryInput>();
        actionList ??= new List<Action>();
    }
    
    public void addData(UIViewEntryInput newInput)
    {
        if (newInput == null) return;

        ensureRuntimeData();
        newInput.ensureRuntimeData();

        if (newInput.textCList != null) textCList.AddRange(newInput.textCList);
        if (newInput.spriteList != null) spriteList.AddRange(newInput.spriteList);
        if (newInput.colorList != null) colorList.AddRange(newInput.colorList);
        if (newInput.numberList != null) numberList.AddRange(newInput.numberList);
        innerDataList.AddRange(newInput.innerDataList);
        actionList.AddRange(newInput.actionList);
    }
    
    public UIViewEntryInput deepCopy()
    {
        return new UIViewEntryInput
        {
            innerIndex = innerIndex,
            viewKey = viewKey,
            textCList = textCList != null ? new List<StringCapsule>(textCList) : new List<StringCapsule>(),
            spriteList = spriteList != null ? new List<SpriteEntry>(spriteList) : new List<SpriteEntry>(),
            colorList = colorList != null ? new List<Color>(colorList) : new List<Color>(),
            numberList = numberList != null ? new List<float>(numberList) : new List<float>(),
            innerDataList = innerDataList != null ? new List<UIViewEntryInput>(innerDataList) : new List<UIViewEntryInput>(),
            actionList = actionList != null ? new List<Action>(actionList) : new List<Action>(),
            uiPrefab = uiPrefab,
            dummy = dummy,
        };
    }
}
