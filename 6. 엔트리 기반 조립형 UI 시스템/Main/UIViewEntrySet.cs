using System;
using System.Collections.Generic;
using UnityEngine;

public class UIViewEntrySet
{
    public List<UIStringEntry> textEntryList = new List<UIStringEntry>();
    public List<UISpriteEntry> spriteEntryList = new List<UISpriteEntry>();
    public List<UIInnerDataEntry> innerDataEntryList = new List<UIInnerDataEntry>();
    public List<UIActionEntry> buttonEntryList = new List<UIActionEntry>();
    public List<UIColorEntry> colorEntryList = new List<UIColorEntry>();
    public List<UINumberEntry> numberEntryList = new List<UINumberEntry>();
    public List<UIEffectEntry> effectEntryList = new List<UIEffectEntry>();

    private readonly List<UIEntryBase> allEntryList = new List<UIEntryBase>();
    
    public void collectUIEntries(UIView owner)
    {
        textEntryList.Clear();
        spriteEntryList.Clear();
        innerDataEntryList.Clear();
        buttonEntryList.Clear();
        colorEntryList.Clear();
        numberEntryList.Clear();
        effectEntryList.Clear();
        allEntryList.Clear();

        Transform target = owner.transform;
        appendUIEntries(target,owner);
    }
    
    public void appendUIEntries(Transform target,UIView uiView)
    {
        target.GetComponentsInChildren(true, allEntryList);

        for (int i = 0; i < allEntryList.Count; i++)
        {
            UIEntryBase entryBase = allEntryList[i];

            switch (entryBase)
            {
                case UIStringEntry textEntry:
                    addEntrySlots(textEntryList, textEntry);
                    break;

                case UISpriteEntry spriteEntry:
                    addEntrySlots(spriteEntryList, spriteEntry);
                    break;

                case UIInnerDataEntry innerEntry:
                    innerEntry.owner = uiView;
                    addEntrySlots(innerDataEntryList, innerEntry);
                    break;

                case UIActionEntry buttonEntry:
                    addEntrySlots(buttonEntryList, buttonEntry);
                    break;

                case UIColorEntry colorEntry:
                    addEntrySlots(colorEntryList, colorEntry);
                    break;

                case UINumberEntry numberEntry:
                    addEntrySlots(numberEntryList, numberEntry);
                    break;

                case UIEffectEntry effectEntry:
                    addEntrySlots(effectEntryList, effectEntry);
                    break;
            }
        }
    }

    private void addEntrySlots<T>(List<T> entryList, T entry) where T : UIEntryBase
    {
        if (entry == null)
        {
            return;
        }

        int count = entry.getEntrySlotCount();
        for (int i = 0; i < count; i++)
        {
            entryList.Add(entry);
        }
    }

    public void applyData(UIViewEntryInput input)
    {
        if (input == null)
        {
            return;
        }

        List<StringCapsule> textList = input.textCList;
        List<SpriteEntry> spriteList = input.spriteList;
        List<Color> colorList = input.colorList;
        List<float> numberList = input.numberList;

        if (textList != null)
        {
            applyTextEntries(textList);
        }

        if (spriteList != null)
        {
            applyEntryData(spriteEntryList, spriteList);
        }

        if (colorList != null)
        {
            applyEntryData(colorEntryList, colorList);
        }

        if (numberList != null)
        {
            applyEntryData(numberEntryList, numberList);
        }

        input.ensureRuntimeData();
        if (input.innerDataList != null)
        {
            applyEntryData(innerDataEntryList, input.innerDataList);
        }

        if (input.actionList != null)
        {
            applyEntryData(buttonEntryList, input.actionList);
        }
    }

    public void applyAction(List<Action> input)
    {
        if (input == null)
        {
            return;
        }

        applyEntryData(buttonEntryList, input);
    }

    public void applyInnerData(List<UIViewEntryInput> input)
    {
        if (input == null)
        {
            return;
        }

        applyEntryData(innerDataEntryList, input);
    }
    
    public void applyColor(List<Color> colors)
    {
        if (colors == null)
        {
            return;
        }

        applyEntryData(colorEntryList, colors);
    }
    
    public void setColor(int index, Color color)
    {
        if (index < 0 || index >= colorEntryList.Count)
        {
            return;
        }

        UIColorEntry entry = colorEntryList[index];
        entry?.setSlotData(getSlotIndexAt(colorEntryList, index), color);
    }

    
    public void setEffect(int index, int num)
    {
        if (index < 0 || index >= effectEntryList.Count)
        {
            return;
        }

        UIEffectEntry entry = effectEntryList[index];
        entry?.setSlotData(getSlotIndexAt(effectEntryList, index), num);
    }

    public void setNumber(int index, float number)
    {
        if (index < 0 || index >= numberEntryList.Count)
        {
            return;
        }

        UINumberEntry entry = numberEntryList[index];
        entry?.setSlotData(getSlotIndexAt(numberEntryList, index), number);
    }

    public void setNumbers(List<float> numbers)
    {
        if (numbers == null)
        {
            return;
        }

        applyEntryData(numberEntryList, numbers);
    }

    private void applyEntryData<TEntry, TData>(List<TEntry> entries, List<TData> input)
        where TEntry : UIEntry<TData>
    {
        TEntry previousEntry = null;
        int slotIndex = 0;

        for (int i = 0; i < entries.Count && i < input.Count; i++)
        {
            TEntry entry = entries[i];
            if (entry == null)
            {
                previousEntry = null;
                slotIndex = 0;
                continue;
            }

            slotIndex = entry == previousEntry ? slotIndex + 1 : 0;
            entry.setSlotData(slotIndex, input[i]);
            previousEntry = entry;
        }
    }

    private void applyTextEntries(List<StringCapsule> input)
    {
        UIStringEntry previousEntry = null;
        int slotIndex = 0;

        for (int i = 0; i < textEntryList.Count && i < input.Count; i++)
        {
            UIStringEntry entry = textEntryList[i];
            if (entry == null)
            {
                previousEntry = null;
                slotIndex = 0;
                continue;
            }

            slotIndex = entry == previousEntry ? slotIndex + 1 : 0;
            entry.setSlotData(slotIndex, input[i]);
            previousEntry = entry;
        }
    }

    private int getSlotIndexAt<T>(List<T> entries, int index) where T : UIEntryBase
    {
        if (entries == null || index < 0 || index >= entries.Count || entries[index] == null)
        {
            return 0;
        }

        T entry = entries[index];
        int slotIndex = 0;

        for (int i = index - 1; i >= 0; i--)
        {
            if (entries[i] != entry)
            {
                break;
            }

            slotIndex++;
        }

        return slotIndex;
    }
}
