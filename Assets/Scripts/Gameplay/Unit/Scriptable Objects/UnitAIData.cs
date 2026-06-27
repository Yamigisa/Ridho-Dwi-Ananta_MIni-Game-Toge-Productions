using UnityEngine;

[CreateAssetMenu(menuName = "Units/AI Data")]
public class UnitAIData : ScriptableObject
{
    [Header("Move")]
    public float wanderRange = 10f;
    public float wanderInterval = 5f;
    public float continueWander = 1.5f;

    [Header("Vision")]
    public float detectRange = 2.5f;
    public float detectAngle = 360f;
}
