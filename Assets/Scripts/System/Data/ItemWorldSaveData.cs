using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ItemWorld))]
[DisallowMultipleComponent]
public class ItemWorldSaveData : MonoBehaviour
{
    [Header("Persistence")]
    [Tooltip("Unique ID for this specific item instance. Generate once and leave it alone.")]
    [SerializeField] private string itemInstanceId;

    private const string PlayerPrefsKey = "ItemWorldSaveData.PickedUpIds";

    private static HashSet<string> pickedUpIds;
    private static bool isLoaded;

    private ItemWorld itemWorld;

    private void Awake()
    {
        itemWorld = GetComponent<ItemWorld>();
        itemWorld.OnPickedUp += MarkAsPickedUp;

        EnsureLoaded();

        if (string.IsNullOrEmpty(itemInstanceId))
        {
            Debug.LogWarning(
                $"{name} has no itemInstanceId set. Right-click the component header and choose 'Generate ID'.",
                this
            );
            return;
        }

        if (pickedUpIds.Contains(itemInstanceId))
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (itemWorld != null)
            ItemWorldPickupEvents.OnAnyItemPickedUp += HandleAnyItemPickedUp;
    }

    private void OnDisable()
    {
        ItemWorldPickupEvents.OnAnyItemPickedUp -= HandleAnyItemPickedUp;
    }

    private void HandleAnyItemPickedUp(ItemWorld pickedUp)
    {
        if (pickedUp != itemWorld)
            return;

        MarkAsPickedUp();
    }

    private void MarkAsPickedUp()
    {
        if (string.IsNullOrEmpty(itemInstanceId))
            return;

        EnsureLoaded();

        if (pickedUpIds.Add(itemInstanceId))
            Persist();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        pickedUpIds = new HashSet<string>();

        if (SaveDataTransaction.HasKey(PlayerPrefsKey))
        {
            string json =
                SaveDataTransaction.GetString(PlayerPrefsKey);
            SavedIds saved = JsonUtility.FromJson<SavedIds>(json);

            if (saved?.ids != null)
            {
                foreach (string id in saved.ids)
                {
                    if (!string.IsNullOrEmpty(id))
                        pickedUpIds.Add(id);
                }
            }
        }

        isLoaded = true;
    }

    private static void Persist()
    {
        SavedIds saved = new() { ids = new List<string>(pickedUpIds) };
        SaveDataTransaction.SetString(
            PlayerPrefsKey,
            JsonUtility.ToJson(saved)
        );
        SaveDataTransaction.Save();
    }

    public static void ClearAllPickupData()
    {
        SaveDataTransaction.DeleteKey(PlayerPrefsKey);
        SaveDataTransaction.Save();
        pickedUpIds?.Clear();
        isLoaded = false;
    }

    [Serializable]
    private class SavedIds
    {
        public List<string> ids = new();
    }

#if UNITY_EDITOR
    [ContextMenu("Generate ID")]
    private void GenerateId()
    {
        itemInstanceId = Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

public static class ItemWorldPickupEvents
{
    public static event Action<ItemWorld> OnAnyItemPickedUp;

    public static void RaisePickedUp(ItemWorld item) => OnAnyItemPickedUp?.Invoke(item);
}
