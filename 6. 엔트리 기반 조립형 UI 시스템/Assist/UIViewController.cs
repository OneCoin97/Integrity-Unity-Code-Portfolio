using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIViewController : MonoBehaviour
{
    public static UIViewController getInst
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIViewController>();
            }
            return instance;
        }
    }
    
    private static UIViewController instance=null;
    
    private readonly LinkedList<UIView> uiStack = new LinkedList<UIView>();
    private readonly Dictionary<UIView, LinkedListNode<UIView>> nodeMap = new Dictionary<UIView, LinkedListNode<UIView>>();

    public bool hasViewStack()
    {
        if (uiStack.Count == 0) return false;
        
        foreach (UIView view in uiStack)
        {
            if (view != null && view.deactivateOnPressKey)
            {
                return true;
            }
        }

        return false;
    }
    public void pushUIView(UIView view)
    {
        if (view == null) return;

        /* 이미 스택 안에 있으면 먼저 제거 */
        if (nodeMap.TryGetValue(view, out LinkedListNode<UIView> oldNode))
        {
            uiStack.Remove(oldNode);
        }

        /* 맨 뒤(Top)로 추가 */
        LinkedListNode<UIView> newNode = uiStack.AddLast(view);
        nodeMap[view] = newNode;
    }

    public void popUIView()
    {
        if (uiStack.Count == 0) return;

        LinkedListNode<UIView> last = uiStack.Last;
        uiStack.RemoveLast();
        nodeMap.Remove(last.Value);
    }

    public void removeUIView(UIView view)
    {
        if (view == null) return;

        if (nodeMap.TryGetValue(view, out LinkedListNode<UIView> node))
        {
            uiStack.Remove(node);
            nodeMap.Remove(view);
        }
    }

    public UIView peekUIView()
    {
        return uiStack.Count > 0 ? uiStack.Last.Value : null;
    }

    public IEnumerator disableUIViewOnPressKeyIE()
    {
        yield return null;
        if (uiStack.Count == 0) yield break;

        LinkedListNode<UIView> last = uiStack.Last;

        while (last != null)
        {
            UIView view = last.Value;

            if (view == null)
            {
                last = last.Previous;
                continue;
            }

            if (view.deactivateOnPressKey)
            {
                view.disableUIView(); 
                break;
            }

            last = last.Previous;
        }
    }

    public void disableUIViewOnPressKey()
    {
        StartCoroutine(disableUIViewOnPressKeyIE());
    }
}
