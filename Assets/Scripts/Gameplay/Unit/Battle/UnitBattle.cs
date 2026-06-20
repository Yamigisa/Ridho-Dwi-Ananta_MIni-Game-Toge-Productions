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
    [Header("Unit Battle UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider mpSlider;

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
    public event Action<UnitBattle> OnHoverEntered;
    public event Action<UnitBattle> OnHoverExited;

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
        {
            cardRectTransform = transform as RectTransform;
            CreateDamageFlashOverlay();
        }

        if (selectionBackground != null)
            defaultSelectionBackgroundColor = selectionBackground.color;
    }

    private void OnDisable()
    {
        ResetDamageFeedback();
        ClearItemPreview();
    }

    public void InitializeUnitBattle(UnitData unitData, bool usePersistentState = false)
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

            if (usePersistentState)
            {
                runtimeState = UnitRuntimeState.GetOrCreate(unitData);
                CurrentHP = Mathf.Clamp(runtimeState.currentHP, 0, MaxHP);
                CurrentMP = Mathf.Clamp(runtimeState.currentMP, 0, MaxMP);
                Attack = runtimeState.attack;
                BaseDefense = runtimeState.defense;
                Speed = runtimeState.speed;
            }
            else
            {
                runtimeState = null;
                CurrentHP = MaxHP;
                CurrentMP = MaxMP;
                Attack = unitBattleData.baseAttack;
                BaseDefense = unitBattleData.baseDefense;
                Speed = unitBattleData.baseSpeed;
            }

            if (animator != null && unitBattleData.battleAnimator != null)
                animator.runtimeAnimatorController = unitBattleData.battleAnimator;
        }

        RefreshUI(unitData);
    }

    public void SetHP(int value)
    {
        CurrentHP = Mathf.Clamp(value, 0, MaxHP);
        if (runtimeState != null)
            runtimeState.currentHP = CurrentHP;

        if (hpText != null) hpText.text = $"{CurrentHP} / {MaxHP}";
        SetSliderImmediate(hpSlider, GetHPPercent());
    }

    public void SetMP(int value)
    {
        CurrentMP = Mathf.Clamp(value, 0, MaxMP);
        if (runtimeState != null)
            runtimeState.currentMP = CurrentMP;

        if (mpText != null) mpText.text = $"{CurrentMP} / {MaxMP}";
        SetSliderImmediate(mpSlider, GetMPPercent());
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
        Attack += Mathf.Max(0, amount);
        if (runtimeState != null)
            runtimeState.attack = Attack;
    }

    public void IncreaseDefense(int amount)
    {
        BaseDefense += Mathf.Max(0, amount);
        if (runtimeState != null)
            runtimeState.defense = BaseDefense;
    }

    public void IncreaseSpeed(int amount)
    {
        Speed += Mathf.Max(0, amount);
        if (runtimeState != null)
            runtimeState.speed = Speed;
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
        if (IsTargetable)
            OnSelected?.Invoke(this);
    }

    private void HandleHoverEntered()
    {
        if (IsTargetable)
            OnHoverEntered?.Invoke(this);
    }

    private void HandleHoverExited()
    {
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

public static class UnitRuntimeState
{
    public sealed class State
    {
        public int currentHP;
        public int currentMP;
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
            UnitBattleData battleData = unitData.battleData;
            state = new State
            {
                currentHP = battleData != null ? battleData.baseHP : 0,
                currentMP = battleData != null ? battleData.baseMP : 0,
                attack = battleData != null ? battleData.baseAttack : 0,
                defense = battleData != null ? battleData.baseDefense : 0,
                speed = battleData != null ? battleData.baseSpeed : 0
            };

            states.Add(unitData, state);
        }

        return state;
    }

    public static void Clear()
    {
        states.Clear();
    }
}
