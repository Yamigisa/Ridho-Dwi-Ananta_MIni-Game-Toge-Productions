using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("General Info")]
    public string unitName;
    public Sprite icon;
    public UnitBattleData battleData;
    public UnitExplorationData explorationData;

    [Header("Leave empty for player units")]
    public UnitAIData aiData;
}
