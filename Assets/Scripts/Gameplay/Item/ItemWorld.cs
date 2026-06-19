using UnityEngine;

public class ItemWorld : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int itemAmount = 1;

    private void Start()
    {
        spriteRenderer.sprite = itemData.Icon;
    }
    public void Pickup()
    {
        Inventory.Instance.PickUpItem(itemData, itemAmount);
        Destroy(gameObject);
    }
}
