using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Panel")]
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Item Bar UI")]
    [SerializeField] private ItemBar itemBarPrefab;
    [SerializeField] private RectTransform itemBarContent;

    private readonly List<InventoryItem> items = new List<InventoryItem>();
    private bool allowKeyboardToggle = true;
    private ItemBar selectedItemBar;

    public static Inventory Instance { get; private set; }
    public event Action<ItemData> ItemUseRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Canvas inventoryCanvas = GetComponentInChildren<Canvas>();
        if (inventoryCanvas != null)
        {
            inventoryCanvas.overrideSorting = true;
            inventoryCanvas.sortingOrder = 100;
        }

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
        if (!allowKeyboardToggle)
            return;

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

    public void SetKeyboardToggleEnabled(bool isEnabled)
    {
        allowKeyboardToggle = isEnabled;
    }

    public void ClearItemSelection()
    {
        if (selectedItemBar != null)
            selectedItemBar.SetSelected(false);

        selectedItemBar = null;
        SetItemDescription(null);
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

        if (selectedItemBar == inventoryItem.itemBar)
            selectedItemBar = null;

        Destroy(inventoryItem.itemBar.gameObject);
    }

    private void AddItemBar(InventoryItem inventoryItem)
    {
        ItemBar itemBar = Instantiate(itemBarPrefab, itemBarContent);
        inventoryItem.itemBar = itemBar;

        itemBar.InitializeItemBar(inventoryItem.itemData);
        itemBar.Clicked += HandleItemBarClicked;
        itemBar.HoverEntered += HandleItemBarHoverEntered;
        itemBar.HoverExited += HandleItemBarHoverExited;
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

    private void HandleItemBarHoverEntered(ItemBar hoveredItemBar)
    {
        if (hoveredItemBar == null)
            return;

        if (selectedItemBar != null && selectedItemBar != hoveredItemBar)
            selectedItemBar.SetSelected(false);

        selectedItemBar = hoveredItemBar;
        selectedItemBar.SetSelected(true);
        SetItemDescription(selectedItemBar.ItemData);
    }

    private void HandleItemBarHoverExited(ItemBar hoveredItemBar)
    {
        if (selectedItemBar != hoveredItemBar)
            return;

        ClearItemSelection();
    }

    private void SetItemDescription(ItemData itemData)
    {
        if (itemDescriptionText != null)
            itemDescriptionText.text = itemData != null ? itemData.Description : string.Empty;
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
