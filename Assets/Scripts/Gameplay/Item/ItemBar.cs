using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
    public event Action<ItemBar> HoverEntered;
    public event Action<ItemBar> HoverExited;

    private void Awake()
    {
        originalColor = itemBarButton.targetGraphic.color;
        itemBarButton.onClick.AddListener(HandleClick);
        AudioManager.Instance?.RegisterButton(itemBarButton);
    }

    public void InitializeItemBar(ItemData itemData)
    {
        this.itemData = itemData;

        itemImage.sprite = itemData.Icon;
        itemNameText.text = itemData.ItemName;
        itemBarButton.interactable = !itemData.IsQuestItem;
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
        if (DialogueManager.IsGameplayInputLocked)
            return;

        Clicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DialogueManager.IsGameplayInputLocked)
            return;

        HoverEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (DialogueManager.IsGameplayInputLocked)
            return;

        HoverExited?.Invoke(this);
    }

    private void OnDestroy()
    {
        itemBarButton.onClick.RemoveListener(HandleClick);
        Clicked = null;
        HoverEntered = null;
        HoverExited = null;
    }
}
