using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Image))]
public class SpriteViewer : UISpriteEntry
{
    protected Image image;
    public bool sizeMode;
    public bool posMode;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    protected override void processData()
    {
        image.sprite = data.image.sprite;
        image.color = data.image.color;
        image.material = data.image.material;
        if (sizeMode)
        {
            RectTransform prefabRect = data.GetComponent<RectTransform>();

            if (prefabRect != null)
                image.rectTransform.sizeDelta = prefabRect.sizeDelta;
        }

        if (posMode)
        {
            RectTransform prefabRect = data.GetComponent<RectTransform>();

            if (prefabRect != null)
                image.rectTransform.localPosition = prefabRect.localPosition;
        }
    }

    protected override void processNullData()
    {
        image.color = new Color(0, 0, 0, 0);
    }
}