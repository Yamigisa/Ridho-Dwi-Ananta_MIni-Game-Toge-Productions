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

    [Header("Battle AI (leave empty for player-controlled units)")]
    public BattleAIProfile battleAIProfile;

    [Header("AI Exploration Data ((leave empty for player-controlled units)")]
    public UnitAIData aiData;

    [Header("Skills")]
    [SerializeField] private List<SkillData> skills = new();

    private static readonly Dictionary<UnitData, List<SkillData>>
        runtimeSkills = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeSkills()
    {
        runtimeSkills.Clear();
    }

    public IReadOnlyList<SkillData> Skills
    {
        get
        {
            List<SkillData> allSkills = new(skills);

            foreach (SkillData skill in GetRuntimeSkills())
            {
                if (skill != null && !allSkills.Contains(skill))
                    allSkills.Add(skill);
            }

            SortSkills(allSkills);
            return allSkills;
        }
    }

    public IReadOnlyList<SkillData> AddedSkills => GetRuntimeSkills();

#if UNITY_EDITOR
    private void OnValidate()
    {
        SortSkills(skills);
    }
#endif

    public bool AddSkill(SkillData skill)
    {
        List<SkillData> addedSkills = GetRuntimeSkills();
        if (skill == null ||
            skills.Contains(skill) ||
            addedSkills.Contains(skill))
            return false;

        addedSkills.Add(skill);
        return true;
    }

    public bool RemoveAddedSkill(SkillData skill)
    {
        return skill != null && GetRuntimeSkills().Remove(skill);
    }

    public void ClearAddedSkills()
    {
        runtimeSkills.Remove(this);
    }

    private List<SkillData> GetRuntimeSkills()
    {
        if (!runtimeSkills.TryGetValue(this, out List<SkillData> addedSkills))
        {
            addedSkills = new List<SkillData>();
            runtimeSkills.Add(this, addedSkills);
        }

        return addedSkills;
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
