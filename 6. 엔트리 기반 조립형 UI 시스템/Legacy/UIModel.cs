using System;
using System.Collections.Generic;
using UnityEngine;

public enum UIType
{
    Main,
    Icon,
    Popup,
    Loading,
    Notice,
    CheckPopup
}

[Serializable]
public sealed class UIDataGroup
{
    public UIType type;
    public List<UIDataInspector> entries = new List<UIDataInspector>();
}

/// <summary>
/// 에디터에서 등록한 UI 데이터를 타입과 에셋 이름으로 조회한다.
/// 원본 ScriptableObject의 런타임 변형을 막기 위해 입력 데이터의 복사본을 반환한다.
/// </summary>
public class UIModel : MonoBehaviour
{
    [SerializeField] private List<UIDataGroup> dataGroups = new List<UIDataGroup>();

    private readonly Dictionary<UIType, Dictionary<string, UIDataInspector>> dataMap
        = new Dictionary<UIType, Dictionary<string, UIDataInspector>>();

    private void Awake()
    {
        rebuildDataMap();
    }

    public UIViewEntryInput getUIData(UIType type, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (!dataMap.TryGetValue(type, out Dictionary<string, UIDataInspector> typeMap)
            || !typeMap.TryGetValue(key, out UIDataInspector dataAsset)
            || dataAsset == null)
        {
            return null;
        }

        return dataAsset.data?.deepCopy();
    }

    public void rebuildDataMap()
    {
        dataMap.Clear();

        foreach (UIDataGroup group in dataGroups)
        {
            if (group == null)
            {
                continue;
            }

            if (!dataMap.TryGetValue(group.type, out Dictionary<string, UIDataInspector> typeMap))
            {
                typeMap = new Dictionary<string, UIDataInspector>(StringComparer.Ordinal);
                dataMap.Add(group.type, typeMap);
            }

            foreach (UIDataInspector entry in group.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.name))
                {
                    continue;
                }

                if (!typeMap.TryAdd(entry.name, entry))
                {
                    Debug.LogWarning($"중복된 UI 데이터 키를 건너뜁니다: {group.type}/{entry.name}", this);
                }
            }
        }
    }
}
