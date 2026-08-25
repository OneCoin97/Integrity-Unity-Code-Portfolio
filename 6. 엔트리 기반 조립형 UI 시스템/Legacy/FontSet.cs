using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "FontSet", menuName = "UI/FontSet", order = 0)]
public class FontSet : ScriptableObject
{
    public TMP_FontAsset normal;
    public List<FontData> datas = new List<FontData>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        Array enumValues = Enum.GetValues(typeof(FontType));
        int enumCount = enumValues.Length;

        // 리스트 크기 보정
        if (datas.Count != enumCount)
        {
            datas.Clear();
            for (int i = 0; i < enumCount; i++)
            {
                FontType type = (FontType)enumValues.GetValue(i);
                datas.Add(new FontData { type = type, font = null });
            }
        }
        else
        {
            // enum 순서 바뀌었을 경우 type 보정
            for (int i = 0; i < enumCount; i++)
            {
                datas[i].type = (FontType)enumValues.GetValue(i);
            }
        }
    }
#endif

    public TMP_FontAsset getFont(FontType fontType)
    {
        foreach (FontData data in datas)
        {
            if (data != null && data.type == fontType)
            {
                return data.font != null ? data.font : normal;
            }
        }

        return normal;
    }
}

[Serializable]
public class FontData
{
    public FontType type;
    public TMP_FontAsset font;
}

public enum FontType
{
    Normal,
    OutLine,
}
