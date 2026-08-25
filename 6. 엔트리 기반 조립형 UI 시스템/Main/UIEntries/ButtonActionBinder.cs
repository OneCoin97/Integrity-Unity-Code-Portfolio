using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonActionBinder : UIActionEntry
{
    protected Button button;

    protected void Awake()
    {
        button = GetComponent<Button>();
    }
    
    protected override void processData()
   {
       UnityAction unityAction = new UnityAction(data);
    
       button.onClick = new Button.ButtonClickedEvent();
       button.onClick.AddListener(unityAction);

   }
   

}
