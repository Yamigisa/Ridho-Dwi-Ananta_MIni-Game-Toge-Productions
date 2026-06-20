using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    public enum Category
    {
        Unknown
    }

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

        public Attribute Attribute => attribute;
        public ValueType ValueType => valueType;
        public float Value => value;
    }

    [Header("Item Info")]
    [SerializeField] private string itemName;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private Category category = Category.Unknown;

    [SerializeField] private int maxStacks = 99;
    [Header("Attributes")]
    [SerializeField] private List<AttributeValue> attributes = new List<AttributeValue>();

    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public Category ItemCategory => category;
    public int MaxStacks => maxStacks;
    public IReadOnlyList<AttributeValue> Attributes => attributes;

    public bool CanUse(UnitBattle target)
    {
        if (target == null || !target.IsAlive)
            return false;

        for (int i = 0; i < attributes.Count; i++)
        {
            AttributeValue attribute = attributes[i];
            if (attribute == null || attribute.Value <= 0f)
                continue;

            switch (attribute.Attribute)
            {
                case Attribute.HealHP when target.CurrentHP < target.MaxHP:
                case Attribute.HealMP when target.CurrentMP < target.MaxMP:
                case Attribute.IncreaseAttack:
                case Attribute.IncreaseDefense:
                case Attribute.IncreaseSpeed:
                    return true;
            }
        }

        return false;
    }

    public bool Use(UnitBattle target)
    {
        if (!CanUse(target))
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
                    break;
                case Attribute.HealMP:
                    target.HealMP(amount);
                    break;
                case Attribute.IncreaseAttack:
                    target.IncreaseAttack(amount);
                    break;
                case Attribute.IncreaseDefense:
                    target.IncreaseDefense(amount);
                    break;
                case Attribute.IncreaseSpeed:
                    target.IncreaseSpeed(amount);
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
}
