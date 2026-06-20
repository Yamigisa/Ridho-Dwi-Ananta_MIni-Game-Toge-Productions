using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("General Info")]
    public string unitName;
    public int level;
    public Sprite icon;

    [Header("Battle Attributes Data")]
    public UnitBattleData battleData;

    [Header("Exploration Attributes Data")]
    public UnitExplorationData explorationData;

    [Header("Battle AI (leave empty for player-controlled units)")]
    public BattleAIProfile battleAIProfile;

    [Header("AI Exploration Data ((leave empty for player-controlled units)")]
    public UnitAIData aiData;

    [Header("Skills)")]
    [SerializeField] private List<SkillData> skills = new();
    public IReadOnlyList<SkillData> Skills => skills;
}
