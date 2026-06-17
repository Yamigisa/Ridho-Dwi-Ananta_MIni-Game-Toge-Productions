using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitBattle : MonoBehaviour
{
    [Header("Unit Battle UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider mpSlider;

    [Header("For Player Unit ONLY")]
    [SerializeField] private Image uiImage;

    [Header("For Enemy Unit ONLY")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private UnitBattleData unitBattleData;
    private Animator animator;

    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public int MaxHP { get; private set; }
    public int MaxMP { get; private set; }
    public int Attack { get; private set; }
    public int BaseDefense { get; private set; }
    public int Defense => IsGuarding ? BaseDefense * 2 : BaseDefense;
    public int Speed { get; private set; }
    public bool IsGuarding { get; private set; }

    private void Awake()
    {
        TryGetComponent(out animator);
    }

    public void InitializeUnitBattle(UnitData unitData)
    {
        if (unitData == null)
        {
            Debug.LogWarning($"{name}: UnitData is null, skipping initialization.");
            return;
        }

        unitBattleData = unitData.battleData;

        if (unitBattleData == null)
        {
            Debug.LogWarning($"{name}: UnitBattleData is null, skipping stat setup.");
        }
        else
        {
            MaxHP = unitBattleData.baseHP;
            MaxMP = unitBattleData.baseMP;
            CurrentHP = MaxHP;
            CurrentMP = MaxMP;
            Attack = unitBattleData.baseAttack;
            BaseDefense = unitBattleData.baseDefense;
            Speed = unitBattleData.baseSpeed;

            if (animator != null && unitBattleData.battleAnimator != null)
                animator.runtimeAnimatorController = unitBattleData.battleAnimator;
        }

        RefreshUI(unitData);
    }

    public void SetHP(int value)
    {
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        if (hpText != null) hpText.text = $"{CurrentHP} / {MaxHP}";
        if (hpSlider != null) hpSlider.value = MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
    }

    public void SetMP(int value)
    {
        CurrentMP = Mathf.Clamp(value, 0, MaxMP);
        if (mpText != null) mpText.text = $"{CurrentMP} / {MaxMP}";
        if (mpSlider != null) mpSlider.value = MaxMP > 0 ? (float)CurrentMP / MaxMP : 0f;
    }

    public int RecoverHPPercent(float percent)
    {
        int before = CurrentHP;
        int amount = MaxHP > 0 ? Mathf.CeilToInt(MaxHP * percent) : 0;
        SetHP(CurrentHP + amount);
        return CurrentHP - before;
    }

    public int RecoverMPPercent(float percent)
    {
        int before = CurrentMP;
        int amount = MaxMP > 0 ? Mathf.CeilToInt(MaxMP * percent) : 0;
        SetMP(CurrentMP + amount);
        return CurrentMP - before;
    }

    public void StartGuard()
    {
        IsGuarding = true;
    }

    public void ClearGuard()
    {
        IsGuarding = false;
    }

    private void RefreshUI(UnitData unitData)
    {
        if (nameText != null) nameText.text = unitData != null ? unitData.unitName : string.Empty;
        if (levelText != null) levelText.text = unitData != null ? $"Lv. {unitData.level}" : string.Empty;

        if (uiImage != null && unitData != null && unitData.icon != null)
            uiImage.sprite = unitData.icon;

        if (spriteRenderer != null && unitBattleData != null && unitBattleData.battleSprite != null)
            spriteRenderer.sprite = unitBattleData.battleSprite;

        SetHP(CurrentHP);
        SetMP(CurrentMP);
    }

    public UnitBattleData GetUnitBattleData() => unitBattleData;

    public void PlayAttack() => animator?.SetTrigger("Attack");
    public void PlayHurt() => animator?.SetTrigger("Hurt");
    public void PlayDie() => animator?.SetTrigger("Die");
}
