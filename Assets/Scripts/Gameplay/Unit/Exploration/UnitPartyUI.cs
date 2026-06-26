using UnityEngine;

public class UnitPartyUI : MonoBehaviour
{
    [Header("Unit Party UI")]
    [SerializeField] private GameObject unitPartyUIPanel;
    [SerializeField] private UnitBattle unitPartyPrefab;
    [SerializeField] private RectTransform content;
    [SerializeField] private UnitBattleParty playerUnits;

    private UnitBattle[] spawnedUnits;
    private UnitBattle selectedUnit;
    private ItemData itemBeingUsed;

    public bool IsOpen => unitPartyUIPanel != null && unitPartyUIPanel.activeSelf;

    private void Start()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("UnitPartyUI could not find the Inventory.");
            return;
        }

        Inventory.Instance.ItemUseRequested += BeginItemUse;
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.ItemUseRequested -= BeginItemUse;
    }

    public void InitializeParty()
    {
        SpawnPartyUnits();
    }

    private void SpawnPartyUnits()
    {
        spawnedUnits = new UnitBattle[playerUnits.Units.Count];

        for (int i = 0; i < playerUnits.Units.Count; i++)
        {
            UnitBattle unitParty = Instantiate(unitPartyPrefab, content);
            unitParty.InitializeUnitBattle(playerUnits.Units[i], true);
            unitParty.SetTargetable(true);
            unitParty.OnSelected += HandleUnitClicked;
            unitParty.OnHoverEntered += HandleUnitHoverEntered;
            unitParty.OnHoverExited += HandleUnitHoverExited;

            spawnedUnits[i] = unitParty;
        }
    }

    public void OpenPartyUI()
    {
        if (IsOpen)
            ClosePartyUI();

        Inventory.Instance?.CloseItemPanel();
        InitializeParty();
        if (unitPartyUIPanel != null)
            unitPartyUIPanel.SetActive(true);
    }

    public void ClosePartyUI()
    {
        if (unitPartyUIPanel != null)
            unitPartyUIPanel.SetActive(false);

        if (spawnedUnits != null)
        {
            foreach (UnitBattle unit in spawnedUnits)
            {
                if (unit != null)
                {
                    unit.OnSelected -= HandleUnitClicked;
                    unit.OnHoverEntered -= HandleUnitHoverEntered;
                    unit.OnHoverExited -= HandleUnitHoverExited;
                }
            }
        }

        if (content != null)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        spawnedUnits = null;
        selectedUnit = null;
        itemBeingUsed = null;
    }

    public void TogglePartyUI()
    {
        if (IsOpen)
            ClosePartyUI();
        else
            OpenPartyUI();
    }

    public void BeginItemUse(ItemData itemData)
    {
        itemBeingUsed = itemData;
        OpenPartyUI();
    }

    private void HandleUnitClicked(UnitBattle clickedUnit)
    {
        if (itemBeingUsed == null || clickedUnit == null)
            return;

        ApplyItemTo(clickedUnit);
    }

    private void HandleUnitHoverEntered(UnitBattle hoveredUnit)
    {
        if (itemBeingUsed == null || hoveredUnit == null)
            return;

        if (selectedUnit != null)
            selectedUnit.SetTargeted(false, false);

        selectedUnit = hoveredUnit;
        selectedUnit.SetTargeted(true, false);
        selectedUnit.ShowItemPreview(itemBeingUsed);
    }

    private void HandleUnitHoverExited(UnitBattle hoveredUnit)
    {
        if (selectedUnit != hoveredUnit)
            return;

        selectedUnit.ClearItemPreview();
        selectedUnit.SetTargeted(false, false);
        selectedUnit = null;
    }

    private void ApplyItemTo(UnitBattle target)
    {
        if (!itemBeingUsed.Use(target))
        {
            ShowItemCannotUsePopup(target, itemBeingUsed);
            return;
        }

        Inventory.Instance.RemoveItem(itemBeingUsed, 1);
        Inventory.Instance.ClearItemSelection();
        ShowItemUsedPopup(target, itemBeingUsed);

        ClosePartyUI();
        Inventory.Instance.OpenItemPanel();
    }

    private void ShowItemUsedPopup(UnitBattle target, ItemData usedItem)
    {
        if (DialogueManager.Instance == null ||
            DialogueManager.Instance.Messages == null)
        {
            Debug.LogWarning("Cannot show item-used popup because DialogueManager is unavailable.");
            return;
        }

        string unitName = target.UnitData != null
            ? target.UnitData.unitName
            : target.name;

        DialogueManager.Instance.ShowFormattedPopup(
            DialogueManager.Instance.Messages.itemUsed,
            ("unit", unitName),
            ("item", usedItem.ItemName));
    }

    private void ShowItemCannotUsePopup(UnitBattle target, ItemData item)
    {
        if (DialogueManager.Instance == null ||
            DialogueManager.Instance.Messages == null)
            return;

        string unitName = target.UnitData != null
            ? target.UnitData.unitName
            : target.name;

        DialogueManager.Instance.ShowFormattedPopup(
            DialogueManager.Instance.Messages.itemCannotUse,
            ("unit", unitName),
            ("item", item.ItemName));
    }

}
