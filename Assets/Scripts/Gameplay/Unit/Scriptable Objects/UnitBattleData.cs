using UnityEngine;

[CreateAssetMenu(menuName = "Units/Battle Data")]
public class UnitBattleData : ScriptableObject
{
    [Header("Portrait for player card UI")]
    public Sprite portrait;        // for player card UI
    [Header("Portrait for player card UI")]
    public Sprite battleSprite;    // for enemy sprite renderer
    public Sprite turnOrderIcon;   // for turn order display

    [Header("Base Stats")]
    public int baseHP = 15;
    public int baseMP = 50;
    public int baseAttack = 5;
    public int baseDefense = 0;
    public int baseSpeed = 5;

    [Header("Optional Animator")]
    public RuntimeAnimatorController battleAnimator;
}