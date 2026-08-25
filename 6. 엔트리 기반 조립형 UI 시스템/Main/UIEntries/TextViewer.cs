using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
public class TextViewer : UIStringEntry
{
    protected TMP_Text text;
    
    protected virtual void Awake()
    {
        text = GetComponent<TMP_Text>();
        RectTransform rectTransform = GetComponent<RectTransform>();
        
        if(rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public override void setFont(FontSet fontSet)
    {
        text.font = fontSet.getFont(fontType);
    }

    
    protected override void processData()
    {
        text.text = data;
    }
}
