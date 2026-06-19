using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBar : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemAmountText;
    [SerializeField] private Button itemBarButton;
    [SerializeField, Range(0f, 1f)] private float selectedColorMultiplier = 0.7f;

    private ItemData itemData;
    private Color originalColor;

    public ItemData ItemData => itemData;
    public event Action<ItemBar> Clicked;

    private void Awake()
    {
        originalColor = itemBarButton.targetGraphic.color;
        itemBarButton.onClick.AddListener(HandleClick);
    }

    public void InitializeItemBar(ItemData itemData)
    {
        this.itemData = itemData;

        itemImage.sprite = itemData.Icon;
        itemNameText.text = itemData.ItemName;
    }

    public void SetAmount(int amount)
    {
        itemAmountText.text = amount.ToString();
    }

    public void SetSelected(bool isSelected)
    {
        itemBarButton.targetGraphic.color = isSelected
            ? new Color(
                originalColor.r * selectedColorMultiplier,
                originalColor.g * selectedColorMultiplier,
                originalColor.b * selectedColorMultiplier,
                originalColor.a)
            : originalColor;
    }

    private void HandleClick()
    {
        Clicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        itemBarButton.onClick.RemoveListener(HandleClick);
        Clicked = null;
    }
}
