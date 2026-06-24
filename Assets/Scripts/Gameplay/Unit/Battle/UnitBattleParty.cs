using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitBattleParty : MonoBehaviour
{
    [SerializeField] private string partyName;
    [SerializeField] private List<UnitData> units = new List<UnitData>();

    public string PartyName => partyName;
    public IReadOnlyList<UnitData> Units => units;
    public event Action PartyChanged;

    public void AddUnit(UnitData unit)
    {
        if (unit == null || units.Contains(unit))
            return;

        units.Add(unit);
        PartyChanged?.Invoke();
        ShowUnitJoinedPopup(unit);
    }

    public void AddParty(UnitData unit)
    {
        AddUnit(unit);
    }

    public void AddToPlayerParty(UnitData unit)
    {
        UnitBattleParty playerParty = GetPlayerParty();

        if (playerParty == null)
        {
            Debug.LogWarning("No active player UnitBattleParty found.");
            return;
        }

        playerParty.AddUnit(unit);
    }

    public void RemoveUnit(UnitData unit)
    {
        if (unit == null || !units.Remove(unit))
            return;

        PartyChanged?.Invoke();
    }

    public void SetUnits(IEnumerable<UnitData> newUnits)
    {
        units.Clear();

        foreach (UnitData unit in newUnits)
        {
            if (unit != null && !units.Contains(unit))
                units.Add(unit);
        }

        PartyChanged?.Invoke();
    }

    private static void ShowUnitJoinedPopup(UnitData unit)
    {
        if (DialogueManager.Instance == null ||
            DialogueManager.Instance.Messages == null)
        {
            return;
        }

        string unitName = !string.IsNullOrWhiteSpace(unit.unitName)
            ? unit.unitName
            : unit.name;

        DialogueManager.Instance.ShowFormattedPopup(
            DialogueManager.Instance.Messages.unitJoinedParty,
            ("unit", unitName));
    }

    private static UnitBattleParty GetPlayerParty()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null
            ? player.GetComponentInParent<UnitBattleParty>()
            : null;
    }
}
