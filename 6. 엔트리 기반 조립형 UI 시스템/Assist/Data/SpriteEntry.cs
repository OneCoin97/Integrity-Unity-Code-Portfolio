using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpriteEntry : MonoBehaviour
{
    [HideInInspector] public Image image;


#if UNITY_EDITOR
    private void OnValidate()
    {
        image = GetComponent<Image>(); // ✅ 에디터에서 자동으로 Image 할당
    }
#endif
}