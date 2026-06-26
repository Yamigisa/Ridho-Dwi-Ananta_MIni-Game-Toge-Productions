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

    [Header("Optional Exploration Animator")]
    public RuntimeAnimatorController explorationAnimator;

    [Header("Battle AI (leave empty for player-controlled units)")]
    public BattleAIProfile battleAIProfile;

    [Header("AI Exploration Data ((leave empty for player-controlled units)")]
    public UnitAIData aiData;

    [Header("Skills)")]
    [SerializeField] private List<SkillData> skills = new();
    private readonly List<SkillData> addedSkills = new();

    public IReadOnlyList<SkillData> Skills
    {
        get
        {
            List<SkillData> allSkills = new(skills);

            foreach (SkillData skill in addedSkills)
            {
                if (skill != null && !allSkills.Contains(skill))
                    allSkills.Add(skill);
            }

            return allSkills;
        }
    }

    public IReadOnlyList<SkillData> AddedSkills => addedSkills;

    public bool AddSkill(SkillData skill)
    {
        if (skill == null || skills.Contains(skill) || addedSkills.Contains(skill))
            return false;

        addedSkills.Add(skill);
        return true;
    }

    public bool RemoveAddedSkill(SkillData skill)
    {
        return skill != null && addedSkills.Remove(skill);
    }
}
