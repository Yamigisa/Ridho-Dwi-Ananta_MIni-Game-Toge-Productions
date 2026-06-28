using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
[DisallowMultipleComponent]
public class InventorySaveData : MonoBehaviour
{
    [Serializable]
    private class SavedItem
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    private class SavedInventory
    {
        public List<SavedItem> items = new();
    }

    private const string PlayerPrefsKey = "InventorySaveData.Items";

    private Inventory inventory;
    private bool isRestoring;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.InventoryChanged += Save;
    }

    private void Start()
    {
        if (Inventory.Instance == inventory)
            Load();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.InventoryChanged -= Save;
    }

    public void Save()
    {
        if (isRestoring ||
            inventory == null ||
            Inventory.Instance != inventory)
        {
            return;
        }

        SavedInventory savedInventory = new();

        foreach (InventoryItem inventoryItem in inventory.Items)
        {
            if (inventoryItem?.itemData == null ||
                inventoryItem.amount <= 0)
            {
                continue;
            }

            savedInventory.items.Add(new SavedItem
            {
                itemId = GetItemId(inventoryItem.itemData),
                amount = inventoryItem.amount
            });
        }

        SaveDataTransaction.SetString(
            PlayerPrefsKey,
            JsonUtility.ToJson(savedInventory)
        );
        SaveDataTransaction.Save();
    }

    public void Load()
    {
        if (inventory == null ||
            Inventory.Instance != inventory ||
            !SaveDataTransaction.HasKey(PlayerPrefsKey))
        {
            return;
        }

        SavedInventory savedInventory = JsonUtility.FromJson<SavedInventory>(
            SaveDataTransaction.GetString(PlayerPrefsKey)
        );

        if (savedInventory?.items == null)
            return;

        Dictionary<string, ItemData> loadedItems = BuildLoadedItemLookup();

        isRestoring = true;

        try
        {
            foreach (SavedItem savedItem in savedInventory.items)
            {
                if (savedItem == null ||
                    savedItem.amount <= 0 ||
                    !loadedItems.TryGetValue(
                        savedItem.itemId,
                        out ItemData itemData))
                {
                    continue;
                }

                inventory.RestoreItem(itemData, savedItem.amount);
            }
        }
        finally
        {
            isRestoring = false;
        }
    }

    public void DeleteSave()
    {
        SaveDataTransaction.DeleteKey(PlayerPrefsKey);
        SaveDataTransaction.Save();
    }

    private static Dictionary<string, ItemData> BuildLoadedItemLookup()
    {
        Dictionary<string, ItemData> itemsById = new();
        ItemData[] loadedItems =
            Resources.FindObjectsOfTypeAll<ItemData>();

        foreach (ItemData itemData in loadedItems)
        {
            if (itemData == null)
                continue;

            itemsById[GetItemId(itemData)] = itemData;
        }

        return itemsById;
    }

    private static string GetItemId(ItemData itemData)
    {
        return itemData.name.Trim();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}
