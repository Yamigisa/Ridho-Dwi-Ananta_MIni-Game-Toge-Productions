using UnityEngine;

public class UnitBattle : MonoBehaviour
{
    private Animator animator;
    private int currentHP;

    private UnitBattleData unitBattleData;

    public UnitBattleData UnitBattleData => unitBattleData;
    public int CurrentHP => currentHP;
    public int Speed => unitBattleData != null ? Mathf.Max(1, unitBattleData.baseSpeed) : 1;
    public bool IsAlive => currentHP > 0;

    private void Awake()
    {
        TryGetComponent(out animator);
    }

    public void InitializeUnitBattle(UnitBattleData battleData)
    {
        unitBattleData = battleData;
        currentHP = battleData.baseHP;

        if (animator != null && battleData.battleAnimator != null)
            animator.runtimeAnimatorController = battleData.battleAnimator;
    }

    public void PlayAttack() => animator?.SetTrigger("Attack");
    public void PlayHurt() => animator?.SetTrigger("Hurt");
    public void PlayDie() => animator?.SetTrigger("Die");
}
