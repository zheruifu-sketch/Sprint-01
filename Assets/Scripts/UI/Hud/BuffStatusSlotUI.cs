using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BuffStatusSlotUI : MonoBehaviour
{
    [Header("References")]
    [LabelText("图标图片")]
    [SerializeField] private Image iconImage;
    [LabelText("进度圆环")]
    [SerializeField] private Image ringImage;

    public Image IconImage => iconImage;
    public Image RingImage => ringImage;

    private void Reset()
    {
        AutoBind();
    }

    private void Awake()
    {
        AutoBind();
    }

    public void Apply(Color color, float normalizedRemaining)
    {
        Apply(null, color, normalizedRemaining);
    }

    public void Apply(Sprite iconSprite, Color color, float normalizedRemaining)
    {
        if (iconImage != null)
        {
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
            }

            iconImage.color = color;
        }

        if (ringImage != null)
        {
            ringImage.color = color;
            ringImage.fillAmount = Mathf.Clamp01(normalizedRemaining);
        }
    }

    private void AutoBind()
    {
        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");
            iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }

        if (ringImage == null)
        {
            Transform ringTransform = transform.Find("Ring");
            ringImage = ringTransform != null ? ringTransform.GetComponent<Image>() : null;
        }
    }
}
