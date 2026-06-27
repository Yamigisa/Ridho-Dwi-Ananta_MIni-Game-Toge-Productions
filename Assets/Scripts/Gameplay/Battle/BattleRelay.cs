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
    private static string playerReturnScene;
    private static Vector3 playerReturnPosition;
    private static bool restorePlayerPositionAfterBattle;
    public static event Action<string> EncounterDefeated;
    public static event Action<UnitData> UnitDefeated;

    public static void Set(
        UnitBattleParty player,
        UnitBattleParty enemy,
        string encounterId,
        string returnScene,
        Vector3 returnPosition)
    {
        PlayerUnits = new List<UnitData>(player.Units);
        EnemyUnits = new List<UnitData>(enemy.Units);
        EnemyPartyName = enemy.PartyName;
        CurrentEncounterId = encounterId;
        playerReturnScene = returnScene;
        playerReturnPosition = returnPosition;
        restorePlayerPositionAfterBattle = false;
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
            restorePlayerPositionAfterBattle = true;
            EncounterDefeated?.Invoke(CurrentEncounterId);
        }

        CurrentEncounterId = null;
    }

    public static void ClearCurrentEncounter()
    {
        CurrentEncounterId = null;
        restorePlayerPositionAfterBattle = false;
    }

    public static bool TryConsumePlayerReturnPosition(
        string sceneName,
        out Vector3 returnPosition)
    {
        if (restorePlayerPositionAfterBattle &&
            string.Equals(
                playerReturnScene,
                sceneName,
                StringComparison.Ordinal))
        {
            returnPosition = playerReturnPosition;
            restorePlayerPositionAfterBattle = false;
            playerReturnScene = null;
            playerReturnPosition = default;
            return true;
        }

        returnPosition = default;
        return false;
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
        UnitDefeated?.Invoke(unitData);
    }

    private static string GetUnitKey(UnitData unitData)
    {
        return unitData.name.Trim();
    }
}
