using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("General Info")]
    public string unitName;
    public int level;
    public Sprite icon;
    public UnitBattleData battleData;
    public UnitExplorationData explorationData;

    [Header("Battle AI (leave empty for player-controlled units)")]
    public BattleAIProfile battleAIProfile;

    [Header("AI Exploration Data ((leave empty for player-controlled units)")]
    public UnitAIData aiData;
}
