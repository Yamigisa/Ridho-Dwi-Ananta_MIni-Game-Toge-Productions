using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Units/Battle Data")]
public class UnitBattleData : ScriptableObject
{
    [System.Serializable]
    public class ItemDrop
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;

        public ItemData Item => item;
        public int Amount => Mathf.Max(1, amount);
        public float DropChance => Mathf.Clamp01(dropChance);
    }

    [Header("Portrait for player card UI")]
    public Sprite portrait;        // for player card UI
    [Header("Sprite for enemy battle (Can leave empty if there is animator)")]
    public Sprite battleSprite;    // for enemy sprite renderer

    [Header("Sprite for turn order")]
    public Sprite turnOrderIcon;   // for turn order display

    [Header("Base Stats")]
    public int baseHP = 15;
    public int baseMP = 50;
    public int baseAttack = 5;
    public int baseDefense = 0;
    public int baseSpeed = 5;

    [Header("Leveling")]
    [Min(1f)] public float statMultiplierPerLevel = 1.1f;
    [Min(1)] public int baseExpToNextLevel = 3;
    [Min(0)] public int expDropAfterDefeat = 0;

    [Header("Item Drops")]
    public List<ItemDrop> itemDrops = new();

    [Header("Optional Animator")]
    public RuntimeAnimatorController battleAnimator;
}
