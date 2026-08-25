
using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextColorChanger : UIColorEntry
{
    private TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    protected override void processData()
    {
       
        text.color = data;
    }
}
