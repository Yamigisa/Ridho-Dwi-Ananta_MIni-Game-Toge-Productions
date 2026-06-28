using System.Collections.Generic;
using UnityEngine;

public enum BattleAIAction
{
    Attack,
    Skill,
    Item,
    Defend,
    Pass,
    Flee
}

public enum BattleAITargetMode
{
    FirstAlive,
    RandomAlive,
    LowestHP
}

public class BattleAIIntent
{
    public BattleAIAction Action { get; }
    public UnitBattle Target { get; }

    public BattleAIIntent(BattleAIAction action, UnitBattle target = null)
    {
        Action = action;
        Target = target;
    }
}

[CreateAssetMenu(menuName = "Units/Battle AI Profile")]
public class BattleAIProfile : ScriptableObject
{
    [Header("Timing")]
    [SerializeField] private float thinkDelay = 0.5f;
    public float ThinkDelay => thinkDelay;

    [Header("Actions")]
    [Tooltip("Relative chance to attack. 0 disables attacking. Compared against the other action weights.")]
    [Range(0f, 10f)]
    [SerializeField] private float attackWeight = 1f;

    [Tooltip("Relative chance to use a skill. 0 disables skill selection.")]
    [Range(0f, 10f)]
    [SerializeField] private float skillWeight = 0f;

    [Tooltip("Relative chance to use an item. 0 disables item selection.")]
    [Range(0f, 10f)]
    [SerializeField] private float itemWeight = 0f;

    [Tooltip("Relative chance to defend. 0 disables defending. Compared against the other action weights.")]
    [Range(0f, 10f)]
    [SerializeField] private float defendWeight = 0f;

    [Tooltip("Relative chance to pass. 0 disables passing. Compared against the other action weights.")]
    [Range(0f, 10f)]
    [SerializeField] private float passWeight = 0f;

    [Header("Targeting")]
    [SerializeField] private BattleAITargetMode targetMode = BattleAITargetMode.RandomAlive;

    [Header("Flee")]
    [SerializeField] private bool canFlee = false;
    [SerializeField] private float fleeHpPercentThreshold = 0.3f;
    [SerializeField] private int fleeLevelGapThreshold = 2;
    [SerializeField] private float baseFleeChance = 0.5f;
    [SerializeField] private float fleeChancePerExtraLevel = 0.1f;
    [SerializeField] private float maxFleeChance = 0.9f;

    public BattleAIIntent ChooseIntent(UnitBattle self, IReadOnlyList<UnitBattle> allies, IReadOnlyList<UnitBattle> opponents)
    {
        if (self == null)
            return new BattleAIIntent(BattleAIAction.Pass);

        if (ShouldTryFlee(self, opponents))
            return new BattleAIIntent(BattleAIAction.Flee);

        BattleAIAction action = ChooseWeightedAction();
        if (action == BattleAIAction.Skill)
            return new BattleAIIntent(action);

        if (action == BattleAIAction.Attack)
        {
            UnitBattle target = ChooseTarget(opponents);
            if (target == null)
                return new BattleAIIntent(BattleAIAction.Pass);

            return new BattleAIIntent(action, target);
        }

        return new BattleAIIntent(action);
    }

    public UnitBattle ChooseLivingTarget(
        IReadOnlyList<UnitBattle> candidates)
    {
        return ChooseTarget(candidates);
    }

    public bool RollFleeSuccess(UnitBattle self, IReadOnlyList<UnitBattle> opponents)
    {
        int levelGap = Mathf.Max(0, GetAverageLevel(opponents) - self.Level - fleeLevelGapThreshold);
        float chance = Mathf.Clamp(baseFleeChance + levelGap * fleeChancePerExtraLevel, 0f, maxFleeChance);
        return Random.value <= chance;
    }

    private BattleAIAction ChooseWeightedAction()
    {
        float attack = Mathf.Max(0f, attackWeight);
        float skill = Mathf.Max(0f, skillWeight);
        float item = Mathf.Max(0f, itemWeight);
        float defend = Mathf.Max(0f, defendWeight);
        float pass = Mathf.Max(0f, passWeight);
        float total = attack + skill + item + defend + pass;

        if (total <= 0f)
            return BattleAIAction.Attack;

        float roll = Random.value * total;
        if (roll < attack)
            return BattleAIAction.Attack;

        roll -= attack;
        if (roll < skill)
            return BattleAIAction.Skill;

        roll -= skill;
        if (roll < item)
            return BattleAIAction.Item;

        roll -= item;
        if (roll < defend)
            return BattleAIAction.Defend;

        return BattleAIAction.Pass;
    }

    private bool ShouldTryFlee(UnitBattle self, IReadOnlyList<UnitBattle> opponents)
    {
        if (!canFlee || self.MaxHP <= 0)
            return false;

        float hpPercent = (float)self.CurrentHP / self.MaxHP;
        if (hpPercent > fleeHpPercentThreshold)
            return false;

        return GetAverageLevel(opponents) >= self.Level + fleeLevelGapThreshold;
    }

    private UnitBattle ChooseTarget(IReadOnlyList<UnitBattle> opponents)
    {
        List<UnitBattle> living = GetLivingUnits(opponents);
        if (living.Count == 0)
            return null;

        switch (targetMode)
        {
            case BattleAITargetMode.FirstAlive:
                return living[0];
            case BattleAITargetMode.LowestHP:
                UnitBattle lowest = living[0];
                foreach (UnitBattle unit in living)
                {
                    if (unit.CurrentHP < lowest.CurrentHP)
                        lowest = unit;
                }
                return lowest;
            case BattleAITargetMode.RandomAlive:
            default:
                return living[Random.Range(0, living.Count)];
        }
    }

    private List<UnitBattle> GetLivingUnits(IReadOnlyList<UnitBattle> units)
    {
        List<UnitBattle> living = new();
        foreach (UnitBattle unit in units)
        {
            if (unit != null && unit.IsAlive)
                living.Add(unit);
        }

        return living;
    }

    private int GetAverageLevel(IReadOnlyList<UnitBattle> units)
    {
        int total = 0;
        int count = 0;

        foreach (UnitBattle unit in units)
        {
            if (unit == null || !unit.IsAlive)
                continue;

            total += unit.Level;
            count++;
        }

        return count > 0 ? Mathf.RoundToInt((float)total / count) : 0;
    }
}

/// <summary>
/// Stateless battle formulas. Keeping calculations outside BattleManager makes
/// them reusable by player actions, AI, previews, and automated tests.
/// </summary>
public static class BattleRules
{
    public static int CalculateAttackDamage(
        UnitBattle attacker,
        UnitBattle target)
    {
        return Mathf.Max(0, attacker.Attack - target.Defense);
    }

    public static float CalculateFleeChance(
        IReadOnlyList<UnitBattle> playerUnits,
        IReadOnlyList<UnitBattle> enemyUnits)
    {
        if (playerUnits.Count == 0 || enemyUnits.Count == 0)
            return 1f;

        float playerAverageSpeed = CalculateAverageSpeed(playerUnits);
        float enemyAverageSpeed = CalculateAverageSpeed(enemyUnits);
        float speedRatio =
            playerAverageSpeed / Mathf.Max(1f, enemyAverageSpeed);

        return Mathf.Clamp(0.5f * speedRatio, 0.1f, 0.95f);
    }

    private static float CalculateAverageSpeed(
        IReadOnlyList<UnitBattle> units)
    {
        float total = 0f;
        int count = 0;

        foreach (UnitBattle unit in units)
        {
            if (unit == null || !unit.IsAlive)
                continue;

            total += unit.Speed;
            count++;
        }

        return count > 0 ? total / count : 0f;
    }
}
