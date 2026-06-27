using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Quest_", menuName = "Quests/Quest")]
public class QuestSO : ScriptableObject
{
    public enum RequirementType
    {
        GetItem,
        KillMonster
    }

    [Serializable]
    public class DialogueOption
    {
        public DialogueTrigger.DialogueStage stage;
        public string flowchartBlockId;
    }

    [Header("Quest")]
    [Tooltip("Stable ID, defaults to asset name. Used for save data / lookups.")]
    public string questId;
    public string displayName;
    [TextArea(2, 5)]
    [Tooltip("Objective text shown in the quest tracker. Leave empty to generate it from the requirement.")]
    public string description;
    [Tooltip("Optional cutscene ID that starts this quest when the cutscene finishes.")]
    public string startAfterCutsceneId;
    [Tooltip("Optional Fungus block name that starts this quest when the block finishes.")]
    public string startAfterFlowchartBlockName;

    [Header("Target (optional)")]
    [Tooltip("Drag the unit prefab here if this quest is tied to a specific NPC/monster. Leave empty for a standalone quest with no world target.")]
    public GameObject targetGameObject;

    [Header("Dialogue")]
    public List<DialogueOption> dialogueOptions = new();

    [Header("Requirement")]
    public RequirementType requirementType;

    [Header("Item Requirement - Empty if monsters")]
    public ItemData requiredItem;
    [FormerlySerializedAs("requiredAmount")]
    [Min(1)] public int requiredItemAmount = 1;

    [Header("Monster Requirement - Empty if item")]
    public UnitData requiredMonster;
    [Min(1)] public int requiredMonsterAmount = 1;

    public bool HasTarget => targetGameObject != null;

    public GameObject ResolveRuntimeTarget()
    {
        if (targetGameObject == null)
            return null;

        if (targetGameObject.scene.IsValid())
            return targetGameObject;

        UnitExploration configuredUnit =
            targetGameObject.GetComponent<UnitExploration>();
        UnitData targetUnitData = configuredUnit?.GetUnitData();

        if (targetUnitData == null)
        {
            Debug.LogWarning(
                $"Quest '{questId}' target prefab '{targetGameObject.name}' has no UnitExploration with UnitData.",
                this
            );
            return null;
        }

        UnitExploration[] runtimeUnits =
            FindObjectsByType<UnitExploration>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (UnitExploration runtimeUnit in runtimeUnits)
        {
            if (runtimeUnit != null &&
                runtimeUnit.gameObject.scene.IsValid() &&
                runtimeUnit.GetUnitData() == targetUnitData)
            {
                return runtimeUnit.gameObject;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(questId))
            questId = name;
    }
#endif
}
