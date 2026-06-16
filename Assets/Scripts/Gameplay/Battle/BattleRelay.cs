using System.Collections.Generic;

public static class BattleRelay
{
    public static List<UnitData> PlayerUnits { get; private set; }
    public static List<UnitData> EnemyUnits { get; private set; }
    public static string EnemyPartyName { get; private set; }

    public static void Set(UnitBattleParty player, UnitBattleParty enemy)
    {
        PlayerUnits = new List<UnitData>(player.Units);
        EnemyUnits = new List<UnitData>(enemy.Units);
        EnemyPartyName = enemy.PartyName;
    }

    public static void Clear()
    {
        PlayerUnits = null;
        EnemyUnits = null;
        EnemyPartyName = null;
    }
}