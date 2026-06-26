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
        movement.SetMoveSpeed(data.explorationData.moveSpeed);
        unitAnimator?.ApplyUnitDataAnimatorController(data);
    }

    public UnitData GetUnitData() => unitData;
    public UnitMovement GetMovement() => movement;
    public UnitAIData GetAIData() => unitData?.aiData;
}
