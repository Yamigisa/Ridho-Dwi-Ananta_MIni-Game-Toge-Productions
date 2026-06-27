using UnityEngine;

[RequireComponent(typeof(UnitMovement))]
public class UnitExploration : MonoBehaviour
{
    [SerializeField] private UnitData unitData;
    private UnitMovement movement;
    private UnitAnimator unitAnimator;

    private void Awake()
    {
        movement = GetComponent<UnitMovement>();
        unitAnimator = GetComponent<UnitAnimator>();
    }

    private void Start()
    {
        if (unitData != null)
            Initialize(unitData);
    }

    public void Initialize(UnitData data)
    {
        unitData = data;

        if (data.explorationData != null)
        {
            movement.SetMoveSpeed(data.explorationData.moveSpeed);
            movement.SetSprintMultiplier(data.explorationData.sprintMultiplier);
            movement.SetMovementFeel(
                data.explorationData.acceleration,
                data.explorationData.deceleration,
                data.explorationData.inputDeadZone,
                data.explorationData.stopSnapSpeed);
        }

        unitAnimator?.ApplyExplorationAnimatorController(data.explorationData);
    }

    public UnitData GetUnitData() => unitData;
    public UnitExplorationData GetExplorationData() => unitData?.explorationData;
    public UnitMovement GetMovement() => movement;
    public UnitAIData GetAIData() => unitData?.aiData;
}
