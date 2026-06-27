using UnityEngine;

[CreateAssetMenu(menuName = "Units/Exploration Data")]
public class UnitExplorationData : ScriptableObject
{
    [Header("Animation")]
    public RuntimeAnimatorController explorationAnimator;

    [Header("Exploration Stats")]
    [Min(0f)] public float moveSpeed = 2.25f;
    [Min(1f)] public float sprintMultiplier = 1.6f;

    [Header("Movement Feel")]
    [Min(0f)] public float acceleration = 18f;
    [Min(0f)] public float deceleration = 26f;
    [Range(0f, 1f)] public float inputDeadZone = 0.1f;
    [Min(0f)] public float stopSnapSpeed = 0.04f;
}
