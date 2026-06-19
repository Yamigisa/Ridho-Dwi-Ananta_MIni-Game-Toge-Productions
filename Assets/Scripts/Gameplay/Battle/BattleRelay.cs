using System.Collections.Generic;

public static class BattleRelay
{
    private static readonly HashSet<string> defeatedEncounterIds = new HashSet<string>();

    public static List<UnitData> PlayerUnits { get; private set; }
    public static List<UnitData> EnemyUnits { get; private set; }
    public static string EnemyPartyName { get; private set; }
    public static string CurrentEncounterId { get; private set; }

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
            defeatedEncounterIds.Add(CurrentEncounterId);

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
}
