using System;
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

    [Header("Skills")]
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

            SortSkills(allSkills);
            return allSkills;
        }
    }

    public IReadOnlyList<SkillData> AddedSkills => addedSkills;

#if UNITY_EDITOR
    private void OnValidate()
    {
        SortSkills(skills);
    }
#endif

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

    private static void SortSkills(List<SkillData> skillList)
    {
        if (skillList == null || skillList.Count <= 1)
            return;

        skillList.Sort(CompareSkills);
    }

    private static int CompareSkills(SkillData left, SkillData right)
    {
        if (ReferenceEquals(left, right))
            return 0;

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        int nameComparison = string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);

        return nameComparison != 0
            ? nameComparison
            : string.Compare(left.SkillName, right.SkillName, StringComparison.OrdinalIgnoreCase);
    }
}
