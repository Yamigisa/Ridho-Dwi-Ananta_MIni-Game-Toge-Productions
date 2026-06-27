using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{

    public enum Attribute
    {
        HealHP,
        HealMP,
        IncreaseAttack,
        IncreaseDefense,
        IncreaseSpeed
    }

    public enum ValueType
    {
        Flat,
        Percent
    }

    [Serializable]
    public class AttributeValue
    {
        [SerializeField] private Attribute attribute;
        [SerializeField] private ValueType valueType;
        [SerializeField, Min(0f)] private float value;
        [SerializeField, Tooltip("For HP/MP recovery, repeat this effect at the start of the target's turns during battle.")]
        private bool repeatEachTurnInBattle;
        [SerializeField, Min(0), Tooltip("Used by stat boosts and repeating recovery. Set to 0 to last for the entire battle.")]
        private int duration;

        public Attribute Attribute => attribute;
        public ValueType ValueType => valueType;
        public float Value => value;
        public bool RepeatEachTurnInBattle => repeatEachTurnInBattle;
        public int Duration => duration;
    }

    [Header("Item Info")]
    [SerializeField] private string itemName;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite icon;

    [SerializeField] private int maxStacks = 99;

    [Header("Usage")]
    [SerializeField, Tooltip("Quest items can be collected and counted, but cannot be used from the inventory.")]
    private bool questItem;

    [Header("Attributes")]
    [SerializeField] private List<AttributeValue> attributes = new List<AttributeValue>();

    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;

    public int MaxStacks => maxStacks;
    public bool IsQuestItem => questItem;
    public IReadOnlyList<AttributeValue> Attributes => attributes;
    public bool IsBattleOnly
    {
        get
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                AttributeValue attribute = attributes[i];
                if (attribute != null &&
                    (IsStatIncrease(attribute.Attribute) ||
                     attribute.RepeatEachTurnInBattle))
                    return true;
            }

            return false;
        }
    }

    public bool CanUse(UnitBattle target, bool isInBattle)
    {
        if (target == null ||
            !target.IsAlive ||
            questItem ||
            (!isInBattle && IsBattleOnly))
            return false;

        for (int i = 0; i < attributes.Count; i++)
        {
            AttributeValue attribute = attributes[i];
            if (attribute == null || attribute.Value <= 0f)
                continue;

            switch (attribute.Attribute)
            {
                case Attribute.HealHP when
                    target.CurrentHP < target.MaxHP ||
                    (isInBattle && attribute.RepeatEachTurnInBattle):
                case Attribute.HealMP when
                    target.CurrentMP < target.MaxMP ||
                    (isInBattle && attribute.RepeatEachTurnInBattle):
                case Attribute.IncreaseAttack:
                case Attribute.IncreaseDefense:
                case Attribute.IncreaseSpeed:
                    return true;
            }
        }

        return false;
    }

    public bool Use(
        UnitBattle target,
        bool isInBattle,
        UnitBattle actingUnit = null)
    {
        if (!CanUse(target, isInBattle))
            return false;

        for (int i = 0; i < attributes.Count; i++)
        {
            AttributeValue attribute = attributes[i];
            if (attribute == null || attribute.Value <= 0f)
                continue;

            int amount = GetAmount(attribute, target);

            switch (attribute.Attribute)
            {
                case Attribute.HealHP:
                    target.HealHP(amount);
                    if (isInBattle && attribute.RepeatEachTurnInBattle)
                    {
                        target.AddRecurringRecovery(
                            UnitBattle.RecoveryStat.HP,
                            amount,
                            attribute.Duration);
                    }
                    break;
                case Attribute.HealMP:
                    target.HealMP(amount);
                    if (isInBattle && attribute.RepeatEachTurnInBattle)
                    {
                        target.AddRecurringRecovery(
                            UnitBattle.RecoveryStat.MP,
                            amount,
                            attribute.Duration);
                    }
                    break;
                case Attribute.IncreaseAttack:
                    target.AddTemporaryStatIncrease(
                        UnitBattle.BattleStat.Attack,
                        amount,
                        attribute.Duration,
                        target == actingUnit);
                    break;
                case Attribute.IncreaseDefense:
                    target.AddTemporaryStatIncrease(
                        UnitBattle.BattleStat.Defense,
                        amount,
                        attribute.Duration,
                        target == actingUnit);
                    break;
                case Attribute.IncreaseSpeed:
                    target.AddTemporaryStatIncrease(
                        UnitBattle.BattleStat.Speed,
                        amount,
                        attribute.Duration,
                        target == actingUnit);
                    break;
            }
        }

        return true;
    }

    public int GetProjectedHP(UnitBattle target)
    {
        if (target == null)
            return 0;

        int projectedHP = target.CurrentHP;
        for (int i = 0; i < attributes.Count; i++)
        {
            AttributeValue attribute = attributes[i];
            if (attribute == null ||
                attribute.Attribute != Attribute.HealHP ||
                attribute.Value <= 0f)
                continue;

            projectedHP += GetAmount(attribute, target);
        }

        return Mathf.Clamp(projectedHP, 0, target.MaxHP);
    }

    public int GetProjectedMP(UnitBattle target)
    {
        if (target == null)
            return 0;

        int projectedMP = target.CurrentMP;
        for (int i = 0; i < attributes.Count; i++)
        {
            AttributeValue attribute = attributes[i];
            if (attribute == null ||
                attribute.Attribute != Attribute.HealMP ||
                attribute.Value <= 0f)
                continue;

            projectedMP += GetAmount(attribute, target);
        }

        return Mathf.Clamp(projectedMP, 0, target.MaxMP);
    }

    private static int GetAmount(AttributeValue attribute, UnitBattle target)
    {
        if (attribute.ValueType == ValueType.Flat)
            return Mathf.RoundToInt(attribute.Value);

        float baseValue;
        switch (attribute.Attribute)
        {
            case Attribute.HealHP:
                baseValue = target.MaxHP;
                break;
            case Attribute.HealMP:
                baseValue = target.MaxMP;
                break;
            case Attribute.IncreaseAttack:
                baseValue = target.Attack;
                break;
            case Attribute.IncreaseDefense:
                baseValue = target.BaseDefense;
                break;
            case Attribute.IncreaseSpeed:
                baseValue = target.Speed;
                break;
            default:
                baseValue = 0f;
                break;
        }

        return Mathf.CeilToInt(baseValue * attribute.Value / 100f);
    }

    private static bool IsStatIncrease(Attribute attribute)
    {
        return attribute == Attribute.IncreaseAttack ||
               attribute == Attribute.IncreaseDefense ||
               attribute == Attribute.IncreaseSpeed;
    }
}
