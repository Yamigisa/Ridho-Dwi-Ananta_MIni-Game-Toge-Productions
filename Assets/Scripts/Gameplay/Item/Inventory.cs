using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Panel")]
    [SerializeField] private GameObject itemPanel;

    [Header("Item Bar UI")]
    [SerializeField] private ItemBar itemBarPrefab;
    [SerializeField] private RectTransform itemBarContent;

    private readonly List<InventoryItem> items = new List<InventoryItem>();

    public static Inventory Instance { get; private set; }
    public event Action<ItemData> ItemUseRequested;

    private void Awake()
    {
        Instance = this;
        CloseItemPanel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ItemUseRequested = null;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.iKey.wasPressedThisFrame)
            return;

        if (itemPanel != null && itemPanel.activeSelf)
            CloseItemPanel();
        else
            OpenItemPanel();
    }

    public void OpenItemPanel()
    {
        if (itemPanel != null)
            itemPanel.SetActive(true);
    }

    public void CloseItemPanel()
    {
        if (itemPanel != null)
            itemPanel.SetActive(false);
    }

    public void PickUpItem(ItemData itemData, int amount = 1)
    {
        InventoryItem inventoryItem = FindItem(itemData);
        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem(itemData, amount);
            items.Add(inventoryItem);
            AddItemBar(inventoryItem);
        }
        else
        {
            inventoryItem.amount += amount;
            inventoryItem.itemBar.SetAmount(inventoryItem.amount);
        }
    }

    public void RemoveItem(ItemData itemData, int amount = 1)
    {
        InventoryItem inventoryItem = FindItem(itemData);
        if (inventoryItem == null)
            return;

        inventoryItem.amount = Mathf.Max(0, inventoryItem.amount - amount);
        if (inventoryItem.amount > 0)
        {
            inventoryItem.itemBar.SetAmount(inventoryItem.amount);
            return;
        }

        items.Remove(inventoryItem);
        Destroy(inventoryItem.itemBar.gameObject);
    }

    private void AddItemBar(InventoryItem inventoryItem)
    {
        ItemBar itemBar = Instantiate(itemBarPrefab, itemBarContent);
        inventoryItem.itemBar = itemBar;

        itemBar.InitializeItemBar(inventoryItem.itemData);
        itemBar.Clicked += HandleItemBarClicked;
        itemBar.SetAmount(inventoryItem.amount);
    }

    private InventoryItem FindItem(ItemData itemData)
    {
        return items.Find(item => item.itemData == itemData);
    }

    private void HandleItemBarClicked(ItemBar clickedItemBar)
    {
        InventoryItem clickedItem = FindItem(clickedItemBar.ItemData);
        if (clickedItem == null || clickedItem.amount <= 0)
            return;

        ItemUseRequested?.Invoke(clickedItem.itemData);
    }
}

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int amount;
    [System.NonSerialized] public ItemBar itemBar;

    public InventoryItem(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }
}
