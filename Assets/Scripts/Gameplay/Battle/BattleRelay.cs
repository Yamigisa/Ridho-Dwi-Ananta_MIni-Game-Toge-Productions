using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleRelay
{
    private static readonly HashSet<string> defeatedEncounterIds = new HashSet<string>();
    private static readonly Dictionary<string, int> defeatedUnitCounts =
        new Dictionary<string, int>();
    private const string DefeatedUnitCountPrefix =
        "BattleRelay.DefeatedUnitCount.";

    public static List<UnitData> PlayerUnits { get; private set; }
    public static List<UnitData> EnemyUnits { get; private set; }
    public static string EnemyPartyName { get; private set; }
    public static string CurrentEncounterId { get; private set; }
    public static event Action<string> EncounterDefeated;

    public static void Set(UnitBattleParty player, UnitBattleParty enemy, string encounterId)
    {
        PlayerUnits = new List<UnitData>(player.Units);
        EnemyUnits = new List<UnitData>(enemy.Units);
        EnemyPartyName = enemy.PartyName;
        CurrentEncounterId = encounterId;
    }

    public static void Clear()
    {
        PlayerUnits = null;
        EnemyUnits = null;
        EnemyPartyName = null;
    }

    public static void MarkCurrentEncounterDefeated()
    {
        if (!string.IsNullOrEmpty(CurrentEncounterId))
        {
            defeatedEncounterIds.Add(CurrentEncounterId);
            EncounterDefeated?.Invoke(CurrentEncounterId);
        }

        CurrentEncounterId = null;
    }

    public static void ClearCurrentEncounter()
    {
        CurrentEncounterId = null;
    }

    public static bool IsEncounterDefeated(string encounterId)
    {
        return !string.IsNullOrEmpty(encounterId) &&
               defeatedEncounterIds.Contains(encounterId);
    }

    public static void RestoreDefeatedEncounter(string encounterId)
    {
        if (!string.IsNullOrEmpty(encounterId))
            defeatedEncounterIds.Add(encounterId);
    }

    public static int GetDefeatedUnitCount(UnitData unitData)
    {
        if (unitData == null)
            return 0;

        string unitKey = GetUnitKey(unitData);
        if (!defeatedUnitCounts.TryGetValue(unitKey, out int count))
        {
            count = SaveDataTransaction.GetInt(
                DefeatedUnitCountPrefix + unitKey,
                0
            );
            defeatedUnitCounts[unitKey] = count;
        }

        return count;
    }

    public static void MarkUnitDefeated(UnitData unitData)
    {
        if (unitData == null)
            return;

        string unitKey = GetUnitKey(unitData);
        int count = GetDefeatedUnitCount(unitData) + 1;
        defeatedUnitCounts[unitKey] = count;
        SaveDataTransaction.SetInt(
            DefeatedUnitCountPrefix + unitKey,
            count
        );

        SaveDataTransaction.Save();
    }

    private static string GetUnitKey(UnitData unitData)
    {
        return unitData.name.Trim();
    }
}
