using System;
using UnityEngine;

public class ItemWorld : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int itemAmount = 1;

    public event Action OnPickedUp;

    private void Start()
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = itemData.Icon;
    }
    public void Pickup()
    {
        if (itemData == null || Inventory.Instance == null)
            return;

        if (!Inventory.Instance.TryPickUpItem(itemData, itemAmount))
            return;

        OnPickedUp?.Invoke();
        ItemWorldPickupEvents.RaisePickedUp(this);

        if (DialogueManager.Instance != null &&
            DialogueManager.Instance.Messages != null)
        {
            DialogueManager.Instance.ShowFormattedPopup(
                DialogueManager.Instance.Messages.itemPickedUp,
                ("item", itemData.ItemName)
            );
        }

        Destroy(gameObject);
    }
}
