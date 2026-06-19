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

    public bool AddUnit(UnitData unit)
    {
        if (unit == null || units.Contains(unit))
            return false;

        units.Add(unit);
        PartyChanged?.Invoke();
        return true;
    }

    public bool RemoveUnit(UnitData unit)
    {
        if (unit == null || !units.Remove(unit))
            return false;

        PartyChanged?.Invoke();
        return true;
    }
}
