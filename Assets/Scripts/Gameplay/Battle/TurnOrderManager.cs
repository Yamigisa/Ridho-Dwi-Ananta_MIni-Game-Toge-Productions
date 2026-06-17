using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnEntry
{
    public UnitBattle Unit;
    public float TurnValue;
    public TurnEntry(UnitBattle unit, float turnValue)
    {
        Unit = unit;
        TurnValue = turnValue;
    }
}

public class TurnOrderManager : MonoBehaviour
{
    [Header("Turn Order UI")]
    [SerializeField] private Transform uiContainer;
    [SerializeField] private TurnOrderEntry entryPrefab;
    [SerializeField] private int maxDisplayEntries = 6;

    private List<TurnOrderEntry> uiEntries = new();

    private const float ACTION_VALUE_BASE = 10000f;

    private List<TurnEntry> timeline = new();
    public IReadOnlyList<TurnEntry> Timeline => timeline;

    public void Initialize(List<UnitBattle> playerUnits, List<UnitBattle> enemyUnits)
    {
        timeline.Clear();

        foreach (var unit in playerUnits)
            timeline.Add(new TurnEntry(unit, ComputeInitialTurnValue(unit)));

        foreach (var unit in enemyUnits)
            timeline.Add(new TurnEntry(unit, ComputeInitialTurnValue(unit)));

        SortTimeline();

        BuildUI();
    }

    public UnitBattle PopNext()
    {
        if (timeline.Count == 0) return null;

        UnitBattle nextUnit = timeline[0].Unit;
        AdvanceFirstTurn();
        RefreshUI();

        return nextUnit;
    }

    public UnitBattle PeekNext()
    {
        if (timeline.Count == 0) return null;

        return timeline[0].Unit;
    }

    public void CompleteCurrentTurn()
    {
        if (timeline.Count == 0) return;

        AdvanceFirstTurn();
        RefreshUI();
    }

    private void AdvanceFirstTurn()
    {
        TurnEntry next = timeline[0];
        float advance = next.TurnValue;

        foreach (var entry in timeline)
            entry.TurnValue -= advance;

        next.TurnValue = ComputeTurnValue(next.Unit);

        SortTimeline();
    }

    public void RemoveUnit(UnitBattle unit)
    {
        timeline.RemoveAll(e => e.Unit == unit);
        RefreshUI();
    }

    private void BuildUI()
    {
        foreach (var entry in uiEntries)
            Destroy(entry.gameObject);
        uiEntries.Clear();

        for (int i = 0; i < maxDisplayEntries; i++)
        {
            TurnOrderEntry entry = Instantiate(entryPrefab, uiContainer);
            uiEntries.Add(entry);
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        List<TurnEntry> projected = ProjectTimeline(maxDisplayEntries);

        for (int i = 0; i < uiEntries.Count; i++)
        {
            if (i < projected.Count)
            {
                UnitBattle unit = projected[i].Unit;
                uiEntries[i].gameObject.SetActive(true);
                uiEntries[i].SetTurnOrderEntry(
                    unit.GetUnitBattleData().turnOrderIcon,
                    unit.name,
                    i + 1
                );
            }
            else
            {
                uiEntries[i].gameObject.SetActive(false);
            }
        }
    }

    private List<TurnEntry> ProjectTimeline(int count)
    {
        List<TurnEntry> simulated = timeline
            .Select(e => new TurnEntry(e.Unit, e.TurnValue))
            .ToList();

        List<TurnEntry> result = new();

        while (result.Count < count)
        {
            if (simulated.Count == 0) break;

            simulated = simulated.OrderBy(e => e.TurnValue).ToList();
            TurnEntry next = simulated[0];
            float advance = next.TurnValue;

            foreach (var entry in simulated)
                entry.TurnValue -= advance;

            next.TurnValue = ComputeTurnValue(next.Unit);
            result.Add(new TurnEntry(next.Unit, next.TurnValue));
        }

        return result;
    }

    private float ComputeInitialTurnValue(UnitBattle unit)
    {
        float jitter = Random.Range(0.9f, 1.1f);
        return ComputeTurnValue(unit) * jitter;
    }

    private float ComputeTurnValue(UnitBattle unit)
    {
        float speed = Mathf.Max(1f, unit.Speed);
        return ACTION_VALUE_BASE / speed;
    }

    private void SortTimeline()
    {
        timeline = timeline.OrderBy(e => e.TurnValue).ToList();
    }
}
