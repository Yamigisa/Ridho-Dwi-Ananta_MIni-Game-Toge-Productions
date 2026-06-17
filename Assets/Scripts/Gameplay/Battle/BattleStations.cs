using System.Collections.Generic;
using UnityEngine;

public class BattleStations : MonoBehaviour
{
    [Header("Player Stations (UI)")]
    [SerializeField] private List<RectTransform> playerStations;

    [Header("Enemy Stations (World Space)")]
    [SerializeField] private List<Transform> enemyStations;

    [Header("Prefabs")]
    [SerializeField] private UnitBattle playerUnitBattlePrefab;
    [SerializeField] private UnitBattle enemyUnitBattlePrefab;

    public List<UnitBattle> SpawnPlayerUnits(List<UnitData> unitData)
        => SpawnUIUnits(unitData, playerStations, playerUnitBattlePrefab);

    public List<UnitBattle> SpawnEnemyUnits(List<UnitData> unitData)
        => SpawnWorldUnits(unitData, enemyStations, enemyUnitBattlePrefab);

    private List<UnitBattle> SpawnUIUnits(List<UnitData> unitData, List<RectTransform> stations, UnitBattle prefab)
    {
        List<UnitBattle> result = new();
        int unitCount = Mathf.Min(unitData.Count, stations.Count);
        List<int> indices = GetStationIndices(unitCount, stations.Count);

        for (int i = 0; i < unitCount; i++)
        {
            UnitBattle unitBattle = Instantiate(prefab, stations[indices[i]]);
            unitBattle.name = unitData[i].unitName;
            unitBattle.InitializeUnitBattle(unitData[i]);
            result.Add(unitBattle);
        }

        return result;
    }

    private List<UnitBattle> SpawnWorldUnits(List<UnitData> unitData, List<Transform> stations, UnitBattle prefab)
    {
        List<UnitBattle> result = new();
        int unitCount = Mathf.Min(unitData.Count, stations.Count);
        List<int> indices = GetStationIndices(unitCount, stations.Count);

        for (int i = 0; i < unitCount; i++)
        {
            Transform station = stations[indices[i]];
            UnitBattle unitBattle = Instantiate(prefab, station.position, station.rotation, station);
            unitBattle.name = unitData[i].unitName;
            unitBattle.InitializeUnitBattle(unitData[i]);
            result.Add(unitBattle);
        }

        return result;
    }

    private List<int> GetStationIndices(int unitCount, int maxStations)
    {
        List<int> indices = new();

        for (int i = 0; i < unitCount && i < maxStations; i++)
            indices.Add(i);

        return indices;
    }
}
