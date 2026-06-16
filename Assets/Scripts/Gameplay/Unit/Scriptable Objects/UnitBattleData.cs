using UnityEngine;

[CreateAssetMenu(menuName = "Units/Battle Data")]
public class UnitBattleData : ScriptableObject
{
    [Header("Base Stats")]
    public int baseHP = 15;
    public int baseMP = 50;
    public int baseAttack = 5;
    public int baseDefense = 0;
    public int baseSpeed = 5;

    [Header("Optional Animator")]
    public RuntimeAnimatorController battleAnimator;
}