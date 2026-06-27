using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitBattle : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum BattleStat
    {
        Attack,
        Defense,
        Speed
    }

    public enum RecoveryStat
    {
        HP,
        MP
    }

    private sealed class TemporaryStatIncrease
    {
        public BattleStat stat;
        public int amount;
        public int remainingTurns;
        public bool skipNextTurnEnd;
    }

    private sealed class RecurringRecovery
    {
        public RecoveryStat stat;
        public int amount;
        public int remainingTurns;
    }

    [Header("Unit Battle UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider mpSlider;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("For Player Unit ONLY")]
    [SerializeField] private Image uiImage;
    [SerializeField] private Image selectionBackground;

    [Header("For Enemy Unit ONLY")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Selection Highlight")]
    [SerializeField] private Color targetedColor = Color.yellow;
    [SerializeField] private float targetedScaleMultiplier = 1.12f;

    [Header("Item Preview")]
    [SerializeField] private Color hpGhostColor = new Color(0.3f, 1f, 0.45f, 0.65f);
    [SerializeField] private Color mpGhostColor = new Color(0.3f, 0.7f, 1f, 0.65f);

    [Header("Stat Bar Animation")]
    [SerializeField] private float hpBarAnimationDuration = 0.5f;
    [SerializeField] private float mpBarAnimationDuration = 0.35f;

    [Header("Damage Feedback")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.05f, 0.05f, 0.45f);
    [SerializeField] private Vector2 damageShakeStrength = new Vector2(10f, 5f);
    [SerializeField] private Vector2 enemyDamageShakeStrength = new Vector2(0.15f, 0.08f);
    [SerializeField, Min(1)] private int damageShakeVibrato = 18;
    [SerializeField, Range(0f, 180f)] private float damageShakeRandomness = 45f;
    [SerializeField, Min(1)] private int damageFlashCount = 3;

    private UnitBattleData unitBattleData;
    private UnitData unitData;
    private UnitRuntimeState.State runtimeState;
    private Animator animator;
    private UnitAnimator unitAnimator;
    private Color defaultSpriteColor = Color.white;
    private Color defaultSelectionBackgroundColor = Color.white;
    private Vector3 defaultScale;
    private Coroutine hpBarCoroutine;
    private Coroutine mpBarCoroutine;
    private RectTransform cardRectTransform;
    private Image damageFlashOverlay;
    private Sequence damageFeedbackSequence;
    private Image hpGhostFill;
    private Image mpGhostFill;
    private Vector2 cardPositionBeforeShake;
    private bool hasDamageFeedbackPosition;
    private Vector3 enemyPositionBeforeShake;
    private Color enemyColorBeforeFlash;
    private bool hasEnemyDamageFeedbackState;
    private int baseAttack;
    private int baseDefense;
    private int baseSpeed;
    private readonly List<TemporaryStatIncrease> temporaryStatIncreases = new();
    private readonly List<RecurringRecovery> recurringRecoveries = new();

    public int CurrentHP { get; private set; }
    public int CurrentMP { get; private set; }
    public int MaxHP { get; private set; }
    public int MaxMP { get; private set; }
    public int Attack => Mathf.Max(0, baseAttack + GetTemporaryStatIncrease(BattleStat.Attack));
    public int BaseDefense => Mathf.Max(0, baseDefense + GetTemporaryStatIncrease(BattleStat.Defense));
    public int Defense => IsGuarding ? BaseDefense * 2 : BaseDefense;
    public int Speed => Mathf.Max(0, baseSpeed + GetTemporaryStatIncrease(BattleStat.Speed));
    public int Level { get; private set; }
    public UnitData UnitData => unitData;
    public BattleAIProfile BattleAIProfile => unitData != null ? unitData.battleAIProfile : null;
    public bool IsGuarding { get; private set; }
    public bool IsAlive => CurrentHP > 0;
    public bool IsTargetable { get; private set; }
    public event Action<UnitBattle> OnSelected;
    public event Action<UnitBattle> OnHoverEntered;
    public event Action<UnitBattle> OnHoverExited;

    private void Awake()
    {
        TryGetComponent(out animator);
        TryGetComponent(out unitAnimator);
        defaultScale = transform.localScale;

        if (hpSlider != null)
            hpSlider.interactable = false;

        if (mpSlider != null)
            mpSlider.interactable = false;

        if (expSlider != null)
            expSlider.interactable = false;

        if (spriteRenderer != null)
            defaultSpriteColor = spriteRenderer.color;

        if (uiImage != null)
        {
            cardRectTransform = transform as RectTransform;
            CreateDamageFlashOverlay();
        }

        if (selectionBackground != null)
            defaultSelectionBackgroundColor =
                selectionBackground.color;
    }

    private void OnDisable()
    {
        ResetDamageFeedback();
        ClearItemPreview();
    }

    public void InitializeUnitBattle(UnitData unitData, bool usePersistentState = false)
    {
        temporaryStatIncreases.Clear();
        recurringRecoveries.Clear();

        this.unitData = unitData;
        unitBattleData = unitData.battleData;
        Level = Mathf.Max(0, unitData.level);
        if (usePersistentState)
        {
            runtimeState = UnitRuntimeState.GetOrCreate(unitData);
            Level = runtimeState.level;
            MaxHP = runtimeState.maxHP;
            MaxMP = runtimeState.maxMP;
            CurrentHP = Mathf.Clamp(runtimeState.currentHP, 0, MaxHP);
            CurrentMP = Mathf.Clamp(runtimeState.currentMP, 0, MaxMP);
            baseAttack = runtimeState.attack;
            baseDefense = runtimeState.defense;
            baseSpeed = runtimeState.speed;
        }
        else
        {
            runtimeState = null;
            MaxHP = UnitRuntimeState.CalculateScaledHP(unitData);
            MaxMP = UnitRuntimeState.CalculateScaledMP(unitData);
            CurrentHP = MaxHP;
            CurrentMP = MaxMP;
            baseAttack = UnitRuntimeState.CalculateScaledAttack(unitData);
            baseDefense = UnitRuntimeState.CalculateScaledDefense(unitData);
            baseSpeed = UnitRuntimeState.CalculateScaledSpeed(unitData);
        }

        if (unitAnimator != null)
            unitAnimator.ApplyBattleAnimatorController(unitData);
        else if (animator != null && unitBattleData.battleAnimator != null)
            animator.runtimeAnimatorController = unitBattleData.battleAnimator;

        RefreshUI(unitData);
    }

    public void SetHP(int value)
    {
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        if (runtimeState != null)
            runtimeState.currentHP = CurrentHP;

        if (hpText != null) hpText.text = $"{CurrentHP} / {MaxHP}";
        SetSliderImmediate(hpSlider, GetHPPercent());
        RefreshExperienceUI();
    }

    public void SetMP(int value)
    {
        CurrentMP = Mathf.Clamp(value, 0, MaxMP);
        if (runtimeState != null)
            runtimeState.currentMP = CurrentMP;

        if (mpText != null) mpText.text = $"{CurrentMP} / {MaxMP}";
        SetSliderImmediate(mpSlider, GetMPPercent());
        RefreshExperienceUI();
    }

    public int HealHP(int amount)
    {
        int before = CurrentHP;
        SetHP(CurrentHP + Mathf.Max(0, amount));
        return CurrentHP - before;
    }

    public int HealMP(int amount)
    {
        int before = CurrentMP;
        SetMP(CurrentMP + Mathf.Max(0, amount));
        return CurrentMP - before;
    }

    public void IncreaseAttack(int amount)
    {
        baseAttack += Mathf.Max(0, amount);
        if (runtimeState != null)
            runtimeState.attack = baseAttack;
    }

    public void IncreaseDefense(int amount)
    {
        baseDefense += Mathf.Max(0, amount);
        if (runtimeState != null)
            runtimeState.defense = baseDefense;
    }

    public void IncreaseSpeed(int amount)
    {
        baseSpeed += Mathf.Max(0, amount);
        if (runtimeState != null)
            runtimeState.speed = baseSpeed;
    }

    public void DecreaseAttack(int amount)
    {
        baseAttack = Mathf.Max(0, baseAttack - Mathf.Max(0, amount));
        if (runtimeState != null)
            runtimeState.attack = baseAttack;
    }

    public void DecreaseDefense(int amount)
    {
        baseDefense = Mathf.Max(0, baseDefense - Mathf.Max(0, amount));
        if (runtimeState != null)
            runtimeState.defense = baseDefense;
    }

    public void DecreaseSpeed(int amount)
    {
        baseSpeed = Mathf.Max(0, baseSpeed - Mathf.Max(0, amount));
        if (runtimeState != null)
            runtimeState.speed = baseSpeed;
    }

    public void AddTemporaryStatIncrease(
        BattleStat stat,
        int amount,
        int duration,
        bool appliedDuringOwnTurn)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
            return;

        temporaryStatIncreases.Add(new TemporaryStatIncrease
        {
            stat = stat,
            amount = amount,
            remainingTurns = Mathf.Max(0, duration),
            skipNextTurnEnd = duration > 0 && appliedDuringOwnTurn
        });
    }

    public void AdvanceTemporaryStatDurations()
    {
        for (int i = temporaryStatIncreases.Count - 1; i >= 0; i--)
        {
            TemporaryStatIncrease increase = temporaryStatIncreases[i];

            if (increase.remainingTurns == 0)
                continue;

            if (increase.skipNextTurnEnd)
            {
                increase.skipNextTurnEnd = false;
                continue;
            }

            increase.remainingTurns--;
            if (increase.remainingTurns == 0)
                temporaryStatIncreases.RemoveAt(i);
        }
    }

    public void AddRecurringRecovery(
        RecoveryStat stat,
        int amount,
        int duration)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
            return;

        recurringRecoveries.Add(new RecurringRecovery
        {
            stat = stat,
            amount = amount,
            remainingTurns = Mathf.Max(0, duration)
        });
    }

    public void ApplyRecurringRecoveryAtTurnStart()
    {
        for (int i = recurringRecoveries.Count - 1; i >= 0; i--)
        {
            RecurringRecovery recovery = recurringRecoveries[i];

            if (recovery.stat == RecoveryStat.HP)
                HealHP(recovery.amount);
            else
                HealMP(recovery.amount);

            if (recovery.remainingTurns == 0)
                continue;

            recovery.remainingTurns--;
            if (recovery.remainingTurns == 0)
                recurringRecoveries.RemoveAt(i);
        }
    }

    private int GetTemporaryStatIncrease(BattleStat stat)
    {
        int total = 0;

        for (int i = 0; i < temporaryStatIncreases.Count; i++)
        {
            TemporaryStatIncrease increase = temporaryStatIncreases[i];
            if (increase.stat == stat)
                total += increase.amount;
        }

        return total;
    }

    public bool CanPaySkillCost(SkillData skill)
    {
        return skill != null &&
               CurrentHP > skill.HPCost &&
               CurrentMP >= skill.MPCost;
    }

    public void PaySkillCost(SkillData skill)
    {
        if (skill == null)
            return;

        SetHP(CurrentHP - skill.HPCost);
        SetMP(CurrentMP - skill.MPCost);
    }

    public IEnumerator SetHPAnimated(int value)
    {
        int previousHP = CurrentHP;
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        if (runtimeState != null)
            runtimeState.currentHP = CurrentHP;

        if (hpText != null) hpText.text = $"{CurrentHP} / {MaxHP}";

        if (CurrentHP < previousHP)
            PlayDamageFeedback();

        yield return AnimateSlider(hpSlider, GetHPPercent(), hpBarAnimationDuration, true);
    }

    public IEnumerator SetMPAnimated(int value)
    {
        CurrentMP = Mathf.Clamp(value, 0, MaxMP);
        if (runtimeState != null)
            runtimeState.currentMP = CurrentMP;

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
        if (levelText != null) levelText.text = unitData != null ? $"Lv. {Level}" : string.Empty;

        if (uiImage != null)
        {
            Sprite portrait = unitBattleData != null && unitBattleData.portrait != null
                ? unitBattleData.portrait
                : unitData != null
                    ? unitData.icon
                    : null;

            if (portrait != null)
                uiImage.sprite = portrait;
        }

        if (spriteRenderer != null)
        {
            if (unitBattleData != null && unitBattleData.battleSprite != null)
                spriteRenderer.sprite = unitBattleData.battleSprite;

            EnsureSelectionCollider();
        }

        SetHP(CurrentHP);
        SetMP(CurrentMP);
        RefreshExperienceUI();
    }

    public UnitBattleData GetUnitBattleData() => unitBattleData;
    public int GetExpReward() => unitBattleData != null ? Mathf.Max(0, unitBattleData.expDropAfterDefeat) : 0;

    public List<ItemDropReward> RollItemDrops()
    {
        List<ItemDropReward> rewards = new();
        if (unitBattleData == null || unitBattleData.itemDrops == null)
            return rewards;

        foreach (UnitBattleData.ItemDrop drop in unitBattleData.itemDrops)
        {
            if (drop == null || drop.Item == null || drop.Amount <= 0)
                continue;

            if (UnityEngine.Random.value > drop.DropChance)
                continue;

            rewards.Add(new ItemDropReward(drop.Item, drop.Amount));
        }

        return rewards;
    }

    public void SetTargetable(bool isTargetable)
    {
        IsTargetable = isTargetable && IsAlive;
    }

    public void SetTargeted(bool isTargeted)
    {
        SetTargeted(isTargeted, true);
    }

    public void SetTargeted(bool isTargeted, bool changeScale)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = isTargeted ? targetedColor : defaultSpriteColor;

        if (selectionBackground != null)
            selectionBackground.color = isTargeted
                ? targetedColor
                : defaultSelectionBackgroundColor;

        transform.localScale = isTargeted && changeScale
            ? defaultScale * targetedScaleMultiplier
            : defaultScale;
    }

    public void ShowItemPreview(ItemData itemData)
    {
        ClearItemPreview();

        if (itemData == null || !IsAlive)
            return;

        int projectedHP = itemData.GetProjectedHP(this);
        int projectedMP = itemData.GetProjectedMP(this);

        if (projectedHP > CurrentHP && MaxHP > 0)
        {
            hpGhostFill = CreateGhostFill(
                hpSlider,
                GetHPPercent(),
                (float)projectedHP / MaxHP,
                hpGhostColor,
                "HP Ghost Fill");
        }

        if (projectedMP > CurrentMP && MaxMP > 0)
        {
            mpGhostFill = CreateGhostFill(
                mpSlider,
                GetMPPercent(),
                (float)projectedMP / MaxMP,
                mpGhostColor,
                "MP Ghost Fill");
        }
    }

    public void ClearItemPreview()
    {
        if (hpGhostFill != null)
            Destroy(hpGhostFill.gameObject);

        if (mpGhostFill != null)
            Destroy(mpGhostFill.gameObject);

        hpGhostFill = null;
        mpGhostFill = null;
    }

    private void OnMouseDown()
    {
        HandleSelectionClick();
    }

    private void OnMouseEnter()
    {
        HandleHoverEntered();
    }

    private void OnMouseExit()
    {
        HandleHoverExited();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        HandleSelectionClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        HandleHoverEntered();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HandleHoverExited();
    }

    private void HandleSelectionClick()
    {
        if (DialogueManager.IsGameplayInputLocked)
            return;

        if (IsTargetable)
            OnSelected?.Invoke(this);
    }

    private void HandleHoverEntered()
    {
        if (DialogueManager.IsGameplayInputLocked)
            return;

        if (IsTargetable)
            OnHoverEntered?.Invoke(this);
    }

    private void HandleHoverExited()
    {
        if (DialogueManager.IsGameplayInputLocked)
            return;

        if (IsTargetable)
            OnHoverExited?.Invoke(this);
    }

    private void EnsureSelectionCollider()
    {
        if (spriteRenderer == null || GetComponent<Collider2D>() != null)
            return;

        BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    public void PlayAttack()
    {
        if (unitAnimator != null)
            unitAnimator.PlayAttack();
        else
            animator?.SetTrigger("Attack");
    }

    public void PlayHurt()
    {
        if (unitAnimator != null)
            unitAnimator.PlayHurt();
        else
            animator?.SetTrigger("Hurt");
    }

    public void PlayDie()
    {
        if (unitAnimator != null)
            unitAnimator.PlayDeath();
        else
            animator?.SetTrigger("Death");
    }

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

    private void RefreshExperienceUI()
    {
        if (expSlider == null && expText == null)
            return;

        int currentExp = unitData != null ? UnitRuntimeState.GetExperience(unitData) : 0;
        int requiredExp = unitData != null ? UnitRuntimeState.GetExperienceForNextLevel(unitData) : 0;

        if (expSlider != null)
            expSlider.value = requiredExp > 0 ? Mathf.Clamp01((float)currentExp / requiredExp) : 0f;

        if (expText != null)
            expText.text = requiredExp > 0
                ? $"{currentExp} / {requiredExp} EXP"
                : string.Empty;
    }

    private Image CreateGhostFill(
        Slider slider,
        float currentPercent,
        float projectedPercent,
        Color color,
        string objectName)
    {
        if (slider == null || slider.fillRect == null || slider.fillRect.parent == null)
            return null;

        GameObject ghostObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
        ghostRect.SetParent(slider.fillRect.parent, false);
        ghostRect.anchorMin = new Vector2(Mathf.Clamp01(currentPercent), 0f);
        ghostRect.anchorMax = new Vector2(Mathf.Clamp01(projectedPercent), 1f);
        ghostRect.offsetMin = Vector2.zero;
        ghostRect.offsetMax = Vector2.zero;
        ghostRect.SetAsLastSibling();

        Image ghostImage = ghostObject.GetComponent<Image>();
        ghostImage.color = color;
        ghostImage.raycastTarget = false;

        return ghostImage;
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

    private void CreateDamageFlashOverlay()
    {
        if (cardRectTransform == null || damageFlashOverlay != null)
            return;

        GameObject overlayObject = new GameObject(
            "Damage Flash",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.SetParent(cardRectTransform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.SetAsLastSibling();

        damageFlashOverlay = overlayObject.GetComponent<Image>();
        damageFlashOverlay.raycastTarget = false;
        SetDamageFlashAlpha(0f);
    }

    private void PlayDamageFeedback()
    {
        if (cardRectTransform == null && spriteRenderer == null)
            return;

        ResetDamageFeedback();

        float duration = Mathf.Max(0.01f, hpBarAnimationDuration);
        int flashLoops = Mathf.Max(1, damageFlashCount) * 2;
        float flashHalfDuration = duration / flashLoops;

        damageFeedbackSequence = DOTween.Sequence()
            .SetTarget(this);

        if (cardRectTransform != null)
        {
            CreateDamageFlashOverlay();
            cardPositionBeforeShake = cardRectTransform.anchoredPosition;
            hasDamageFeedbackPosition = true;
            SetDamageFlashAlpha(0f);

            damageFeedbackSequence
                .Join(cardRectTransform.DOShakeAnchorPos(
                    duration,
                    damageShakeStrength,
                    damageShakeVibrato,
                    damageShakeRandomness,
                    false,
                    true,
                    ShakeRandomnessMode.Harmonic
                ))
                .Join(damageFlashOverlay
                    .DOFade(damageFlashColor.a, flashHalfDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLoops(flashLoops, LoopType.Yoyo));
        }

        if (spriteRenderer != null)
        {
            enemyPositionBeforeShake = transform.localPosition;
            enemyColorBeforeFlash = spriteRenderer.color;
            hasEnemyDamageFeedbackState = true;

            Color enemyFlashColor = damageFlashColor;
            enemyFlashColor.a = enemyColorBeforeFlash.a;

            damageFeedbackSequence
                .Join(transform.DOShakePosition(
                    duration,
                    new Vector3(enemyDamageShakeStrength.x, enemyDamageShakeStrength.y, 0f),
                    damageShakeVibrato,
                    damageShakeRandomness,
                    false,
                    true,
                    ShakeRandomnessMode.Harmonic
                ))
                .Join(spriteRenderer
                    .DOColor(enemyFlashColor, flashHalfDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLoops(flashLoops, LoopType.Yoyo));
        }

        damageFeedbackSequence.OnComplete(FinishDamageFeedback);
    }

    private void FinishDamageFeedback()
    {
        if (cardRectTransform != null && hasDamageFeedbackPosition)
            cardRectTransform.anchoredPosition = cardPositionBeforeShake;

        if (spriteRenderer != null && hasEnemyDamageFeedbackState)
        {
            transform.localPosition = enemyPositionBeforeShake;
            spriteRenderer.color = enemyColorBeforeFlash;
        }

        hasDamageFeedbackPosition = false;
        hasEnemyDamageFeedbackState = false;
        SetDamageFlashAlpha(0f);
        damageFeedbackSequence = null;
    }

    private void SetDamageFlashAlpha(float alpha)
    {
        if (damageFlashOverlay == null)
            return;

        Color color = damageFlashColor;
        color.a = alpha;
        damageFlashOverlay.color = color;
    }

    private void ResetDamageFeedback()
    {
        if (damageFeedbackSequence != null && damageFeedbackSequence.IsActive())
            damageFeedbackSequence.Kill();

        damageFeedbackSequence = null;

        if (cardRectTransform != null && hasDamageFeedbackPosition)
            cardRectTransform.anchoredPosition = cardPositionBeforeShake;

        if (spriteRenderer != null && hasEnemyDamageFeedbackState)
        {
            transform.localPosition = enemyPositionBeforeShake;
            spriteRenderer.color = enemyColorBeforeFlash;
        }

        hasDamageFeedbackPosition = false;
        hasEnemyDamageFeedbackState = false;
        SetDamageFlashAlpha(0f);
    }
}

public readonly struct ItemDropReward
{
    public ItemDropReward(ItemData item, int amount)
    {
        Item = item;
        Amount = Mathf.Max(1, amount);
    }

    public ItemData Item { get; }
    public int Amount { get; }
}

public static class UnitRuntimeState
{
    public sealed class State
    {
        public int level;
        public int experience;
        public int currentHP;
        public int currentMP;
        public int maxHP;
        public int maxMP;
        public int attack;
        public int defense;
        public int speed;
    }

    private static readonly Dictionary<UnitData, State> states =
        new Dictionary<UnitData, State>();

    public static State GetOrCreate(UnitData unitData)
    {
        if (!states.TryGetValue(unitData, out State state))
        {
            state = CreateInitialState(unitData);
            states.Add(unitData, state);
        }

        EnsureScaledStats(unitData, state);
        return state;
    }

    public static int GetExperience(UnitData unitData)
    {
        return unitData != null ? GetOrCreate(unitData).experience : 0;
    }

    public static int GetExperienceForNextLevel(UnitData unitData)
    {
        if (unitData == null)
            return 0;

        State state = GetOrCreate(unitData);
        return GetExperienceRequiredForLevel(unitData, state.level + 1);
    }

    public static int AddExperience(UnitData unitData, int amount, out int previousLevel, out int newLevel)
    {
        previousLevel = 0;
        newLevel = 0;

        if (unitData == null || amount <= 0)
            return 0;

        State state = GetOrCreate(unitData);
        previousLevel = state.level;
        state.experience += amount;

        while (state.experience >= GetExperienceRequiredForLevel(unitData, state.level + 1))
        {
            int requiredExp = GetExperienceRequiredForLevel(unitData, state.level + 1);
            state.experience -= requiredExp;
            state.level++;
            ApplyScaledStats(unitData, state, true);
        }

        newLevel = state.level;
        return state.experience;
    }

    public static int CalculateScaledHP(UnitData unitData)
    {
        return CalculateScaledStat(unitData?.battleData?.baseHP ?? 0, GetInitialLevel(unitData), unitData?.battleData);
    }

    public static int CalculateScaledMP(UnitData unitData)
    {
        return CalculateScaledStat(unitData?.battleData?.baseMP ?? 0, GetInitialLevel(unitData), unitData?.battleData);
    }

    public static int CalculateScaledAttack(UnitData unitData)
    {
        return CalculateScaledStat(unitData?.battleData?.baseAttack ?? 0, GetInitialLevel(unitData), unitData?.battleData);
    }

    public static int CalculateScaledDefense(UnitData unitData)
    {
        return CalculateScaledStat(unitData?.battleData?.baseDefense ?? 0, GetInitialLevel(unitData), unitData?.battleData);
    }

    public static int CalculateScaledSpeed(UnitData unitData)
    {
        return CalculateScaledStat(unitData?.battleData?.baseSpeed ?? 0, GetInitialLevel(unitData), unitData?.battleData);
    }

    private static State CreateInitialState(UnitData unitData)
    {
        State state = new State
        {
            level = GetInitialLevel(unitData),
            experience = 0
        };

        ApplyScaledStats(unitData, state, false);
        state.currentHP = state.maxHP;
        state.currentMP = state.maxMP;
        return state;
    }

    private static void EnsureScaledStats(UnitData unitData, State state)
    {
        if (state == null)
            return;

        if (state.maxHP <= 0 && state.maxMP <= 0)
            ApplyScaledStats(unitData, state, false);
    }

    private static void ApplyScaledStats(UnitData unitData, State state, bool preserveCurrentHPMP)
    {
        if (state == null)
            return;

        int previousMaxHP = state.maxHP;
        int previousMaxMP = state.maxMP;
        UnitBattleData battleData = unitData != null ? unitData.battleData : null;

        state.maxHP = CalculateScaledStat(battleData != null ? battleData.baseHP : 0, state.level, battleData);
        state.maxMP = CalculateScaledStat(battleData != null ? battleData.baseMP : 0, state.level, battleData);
        state.attack = CalculateScaledStat(battleData != null ? battleData.baseAttack : 0, state.level, battleData);
        state.defense = CalculateScaledStat(battleData != null ? battleData.baseDefense : 0, state.level, battleData);
        state.speed = CalculateScaledStat(battleData != null ? battleData.baseSpeed : 0, state.level, battleData);

        if (!preserveCurrentHPMP)
            return;

        state.currentHP = Mathf.Clamp(
            state.currentHP + Mathf.Max(0, state.maxHP - previousMaxHP),
            0,
            state.maxHP);
        state.currentMP = Mathf.Clamp(
            state.currentMP + Mathf.Max(0, state.maxMP - previousMaxMP),
            0,
            state.maxMP);
    }

    private static int CalculateScaledStat(int baseValue, int level, UnitBattleData battleData)
    {
        if (baseValue <= 0)
            return 0;

        float multiplier = battleData != null ? Mathf.Max(1f, battleData.statMultiplierPerLevel) : 1f;
        int scaledValue = Mathf.RoundToInt(baseValue * Mathf.Pow(multiplier, Mathf.Max(0, level)));
        return Mathf.Max(1, scaledValue);
    }

    private static int GetExperienceRequiredForLevel(UnitData unitData, int targetLevel)
    {
        UnitBattleData battleData = unitData != null ? unitData.battleData : null;
        int baseExp = battleData != null ? Mathf.Max(1, battleData.baseExpToNextLevel) : 3;
        targetLevel = Mathf.Max(1, targetLevel);

        return Mathf.Max(1, Mathf.CeilToInt(baseExp * targetLevel * Mathf.Log(targetLevel + 1)));
    }

    private static int GetInitialLevel(UnitData unitData)
    {
        return unitData != null ? Mathf.Max(0, unitData.level) : 0;
    }

    public static void Clear()
    {
        states.Clear();
    }
}
