using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    Start,
    PlayerTurn,
    EnemyTurn,
    Win,
    Lose
}

public class BattleManager : MonoBehaviour
{
    [Header("Battle Station")]
    [SerializeField] private List<Transform> playerBattleStation;
    [SerializeField] private List<Transform> enemyBattleStation;

    [Header("Setup")]
    [SerializeField] private UnitBattle battleUnitPrefab;

    private List<UnitBattle> playerBattleUnits = new();
    private List<UnitBattle> enemyBattleUnits = new();

    private void Start()
    {
        if (BattleRelay.PlayerUnits != null && BattleRelay.EnemyUnits != null)
        {
            SpawnUnits(BattleRelay.PlayerUnits, playerBattleStation, playerBattleUnits);
            SpawnUnits(BattleRelay.EnemyUnits, enemyBattleStation, enemyBattleUnits);

            DialogueManager.Instance.ShowFormattedPopup(
                DialogueManager.Instance.Messages.enemyAppeared,
                ("enemy", BattleRelay.EnemyPartyName)
            );

            BattleRelay.Clear();
        }
    }

    #region Initialize Battle & Spawning Units
    public void InitializeBattle(UnitBattleParty playerParty, UnitBattleParty enemyParty)
    {
        SpawnUnits(playerParty.Units, playerBattleStation, playerBattleUnits);
        SpawnUnits(enemyParty.Units, enemyBattleStation, enemyBattleUnits);

        DialogueManager.Instance.ShowFormattedPopup(
          DialogueManager.Instance.Messages.enemyAppeared,
          ("enemy", enemyParty.PartyName)
      );
    }

    private UnitBattle SpawnUnit(UnitData unitData, Transform station)
    {
        UnitBattle unitBattle = Instantiate(battleUnitPrefab, station.position, station.rotation);
        unitBattle.name = unitData.unitName;
        unitBattle.InitializeUnitBattle(unitData.battleData);
        return unitBattle;
    }

    private void SpawnUnits(List<UnitData> unitData, List<Transform> stations, List<UnitBattle> result)
    {
        int unitCount = Mathf.Min(unitData.Count, stations.Count);
        List<int> indices = GetStationIndices(unitCount, stations.Count);

        for (int i = 0; i < unitCount; i++)
            result.Add(SpawnUnit(unitData[i], stations[indices[i]]));
    }

    private List<int> GetStationIndices(int unitCount, int maxStations)
    {
        List<int> indices = new();

        switch (unitCount)
        {
            case 1:
                indices.Add(maxStations / 2); // middle
                break;
            case 2:
                indices.Add(0);               // first
                indices.Add(maxStations - 1); // last
                break;
            default:
                for (int i = 0; i < unitCount; i++)
                    indices.Add(i);           // fill naturally
                break;
        }

        return indices;
    }

    #endregion
}
