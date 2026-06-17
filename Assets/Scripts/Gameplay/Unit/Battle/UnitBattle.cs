using TMPro;
using System;
using System.Collections;
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

    [Header("Selection Highlight")]
    [SerializeField] private Color targetedColor = Color.yellow;
    [SerializeField] private float targetedScaleMultiplier = 1.12f;

    [Header("Stat Bar Animation")]
    [SerializeField] private float hpBarAnimationDuration = 0.5f;
    [SerializeField] private float mpBarAnimationDuration = 0.35f;

    private UnitBattleData unitBattleData;
    private UnitData unitData;
    private Animator animator;
    private Color defaultSpriteColor = Color.white;
    private Color defaultUIImageColor = Color.white;
    private Vector3 defaultScale;
    private Coroutine hpBarCoroutine;
    private Coroutine mpBarCoroutine;

    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public int MaxHP { get; private set; }
    public int MaxMP { get; private set; }
    public int Attack { get; private set; }
    public int BaseDefense { get; private set; }
    public int Defense => IsGuarding ? BaseDefense * 2 : BaseDefense;
    public int Speed { get; private set; }
    public int Level { get; private set; }
    public UnitData UnitData => unitData;
    public BattleAIProfile BattleAIProfile => unitData != null ? unitData.battleAIProfile : null;
    public bool IsGuarding { get; private set; }
    public bool IsAlive => CurrentHP > 0;
    public bool IsTargetable { get; private set; }
    public event Action<UnitBattle> OnSelected;

    private void Awake()
    {
        TryGetComponent(out animator);
        defaultScale = transform.localScale;

        if (hpSlider != null)
            hpSlider.interactable = false;

        if (mpSlider != null)
            mpSlider.interactable = false;

        if (spriteRenderer != null)
            defaultSpriteColor = spriteRenderer.color;

        if (uiImage != null)
            defaultUIImageColor = uiImage.color;
    }

    public void InitializeUnitBattle(UnitData unitData)
    {
        if (unitData == null)
        {
            Debug.LogWarning($"{name}: UnitData is null, skipping initialization.");
            return;
        }

        this.unitData = unitData;
        unitBattleData = unitData.battleData;
        Level = unitData.level;

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
        SetSliderImmediate(hpSlider, GetHPPercent());
    }

    public void SetMP(int value)
    {
        CurrentMP = Mathf.Clamp(value, 0, MaxMP);
        if (mpText != null) mpText.text = $"{CurrentMP} / {MaxMP}";
        SetSliderImmediate(mpSlider, GetMPPercent());
    }

    public IEnumerator SetHPAnimated(int value)
    {
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        if (hpText != null) hpText.text = $"{CurrentHP} / {MaxHP}";

        yield return AnimateSlider(hpSlider, GetHPPercent(), hpBarAnimationDuration, true);
    }

    public IEnumerator SetMPAnimated(int value)
    {
        CurrentMP = Mathf.Clamp(value, 0, MaxMP);
        if (mpText != null) mpText.text = $"{CurrentMP} / {MaxMP}";

        yield return AnimateSlider(mpSlider, GetMPPercent(), mpBarAnimationDuration, false);
    }

    public int RecoverHPPercent(float percent)
    {
        int before = CurrentHP;
        int amount = MaxHP > 0 ? Mathf.CeilToInt(MaxHP * percent) : 0;
        SetHP(CurrentHP + amount);
        return CurrentHP - before;
    }

    public IEnumerator RecoverHPPercentAnimated(float percent, Action<int> onRecovered = null)
    {
        int before = CurrentHP;
        int amount = MaxHP > 0 ? Mathf.CeilToInt(MaxHP * percent) : 0;
        yield return SetHPAnimated(CurrentHP + amount);
        onRecovered?.Invoke(CurrentHP - before);
    }

    public int RecoverMPPercent(float percent)
    {
        int before = CurrentMP;
        int amount = MaxMP > 0 ? Mathf.CeilToInt(MaxMP * percent) : 0;
        SetMP(CurrentMP + amount);
        return CurrentMP - before;
    }

    public IEnumerator RecoverMPPercentAnimated(float percent, Action<int> onRecovered = null)
    {
        int before = CurrentMP;
        int amount = MaxMP > 0 ? Mathf.CeilToInt(MaxMP * percent) : 0;
        yield return SetMPAnimated(CurrentMP + amount);
        onRecovered?.Invoke(CurrentMP - before);
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

        if (spriteRenderer != null)
        {
            if (unitBattleData != null && unitBattleData.battleSprite != null)
                spriteRenderer.sprite = unitBattleData.battleSprite;

            EnsureSelectionCollider();
        }

        SetHP(CurrentHP);
        SetMP(CurrentMP);
    }

    public UnitBattleData GetUnitBattleData() => unitBattleData;

    public void SetTargetable(bool isTargetable)
    {
        IsTargetable = isTargetable && IsAlive;
    }

    public void SetTargeted(bool isTargeted)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = isTargeted ? targetedColor : defaultSpriteColor;

        if (uiImage != null)
            uiImage.color = isTargeted ? targetedColor : defaultUIImageColor;

        transform.localScale = isTargeted ? defaultScale * targetedScaleMultiplier : defaultScale;
    }

    private void OnMouseDown()
    {
        if (!IsTargetable)
            return;

        OnSelected?.Invoke(this);
    }

    private void EnsureSelectionCollider()
    {
        if (spriteRenderer == null || GetComponent<Collider2D>() != null)
            return;

        BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    public void PlayAttack() => animator?.SetTrigger("Attack");
    public void PlayHurt() => animator?.SetTrigger("Hurt");
    public void PlayDie() => animator?.SetTrigger("Die");

    private float GetHPPercent()
    {
        return MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
    }

    private float GetMPPercent()
    {
        return MaxMP > 0 ? (float)CurrentMP / MaxMP : 0f;
    }

    private void SetSliderImmediate(Slider slider, float value)
    {
        if (slider != null)
            slider.value = value;
    }

    private IEnumerator AnimateSlider(Slider slider, float targetValue, float duration, bool isHP)
    {
        if (slider == null)
            yield break;

        Coroutine activeCoroutine = isHP ? hpBarCoroutine : mpBarCoroutine;
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        IEnumerator routine = AnimateSliderValue(slider, targetValue, duration, isHP);
        if (isHP)
            hpBarCoroutine = StartCoroutine(routine);
        else
            mpBarCoroutine = StartCoroutine(routine);

        if (isHP)
        {
            yield return hpBarCoroutine;
            hpBarCoroutine = null;
        }
        else
        {
            yield return mpBarCoroutine;
            mpBarCoroutine = null;
        }
    }

    private IEnumerator AnimateSliderValue(Slider slider, float targetValue, float duration, bool isHP)
    {
        float startValue = slider.value;

        if (duration <= 0f || Mathf.Approximately(startValue, targetValue))
        {
            slider.value = targetValue;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            slider.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        slider.value = targetValue;
    }
}
