using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    public enum TargetType
    {
        Self,
        SingleAlly,
        AllAllies,
        SingleEnemy,
        AllEnemies
    }

    public enum EffectType
    {
        Damage,
        HealHP,
        HealMP,
        IncreaseAttack,
        IncreaseDefense,
        IncreaseSpeed,
        DecreaseAttack,
        DecreaseDefense,
        DecreaseSpeed
    }

    public enum EffectRecipient
    {
        Target,
        Caster
    }

    public enum ValueType
    {
        Flat,
        Percent
    }

    [Serializable]
    public class SkillEffect
    {
        [SerializeField] private EffectType effectType;
        [SerializeField] private EffectRecipient recipient = EffectRecipient.Target;
        [SerializeField] private ValueType valueType = ValueType.Flat;
        [SerializeField, Min(0f)] private float value;
        [SerializeField, Min(0)] private int duration;
        [SerializeField, Range(0f, 1f)] private float successChance = 1f;

        public EffectType Type => effectType;
        public EffectRecipient Recipient => recipient;
        public ValueType ValueType => valueType;
        public float Value => value;
        public int Duration => duration;
        public float SuccessChance => successChance;
    }

    [Header("Skill Info")]
    [SerializeField] private string skillName;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private AudioClip audioClip;

    [Header("Cost")]
    [SerializeField, Min(0)] private int hpCost;
    [SerializeField, Min(0)] private int mpCost;

    [Header("Targeting")]
    [SerializeField] private TargetType targetType = TargetType.SingleEnemy;

    [Header("Effects")]
    [SerializeField] private List<SkillEffect> effects = new List<SkillEffect>();

    public string SkillName => skillName;
    public string Description => description;
    public Sprite Icon => icon;
    public AudioClip AudioClip => audioClip;
    public int HPCost => hpCost;
    public int MPCost => mpCost;
    public TargetType Targeting => targetType;
    public IReadOnlyList<SkillEffect> Effects => effects;
}
