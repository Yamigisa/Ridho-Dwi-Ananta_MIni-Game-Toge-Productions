using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BattleState
{
    Start,
    PlayerTurn,
    EnemyTurn,
    Win,
    Lose
}

public class BattleManager : MonoBehaviour
{
    [Header("Stations")]
    [SerializeField] private BattleStations battleStations;

    private List<UnitBattle> playerBattleUnits = new();
    private List<UnitBattle> enemyBattleUnits = new();


    [Header("Turn Order")]
    [SerializeField] private TurnOrderManager turnOrderManager;

    [Header("HUD")]
    [SerializeField] private BattleHUD battleHUD;
    [SerializeField] private SkillUI skillUI;

    [Header("Feedback")]
    [SerializeField] private float damagePopupDelay = 0.35f;

    private UnitBattle currentActingUnit;
    public UnitBattle CurrentActingUnit => currentActingUnit;
    private UnitBattle selectedEnemyTarget;
    private UnitBattle confirmedEnemyTarget;
    private UnitBattle selectedItemTarget;
    private ItemData itemBeingUsed;
    private SkillData skillBeingUsed;
    private bool isSelectingTarget;
    private bool isChoosingItem;
    private bool isSelectingItemTarget;
    private bool isChoosingSkill;
    private bool isSelectingSkillTarget;
    private bool isExecutingSkill;
    private bool targetConfirmed;
    private bool targetSelectionCanceled;
    private bool subscribedToDialogueEvents;
    private bool keepPlayerCardsVisibleDuringPopups;

    private BattleState currentState;
    public BattleState CurrentState => currentState;

    private void Awake()
    {
        if (skillUI == null)
            skillUI = FindFirstObjectByType<SkillUI>(FindObjectsInactive.Include);

        battleHUD.OnActionSelected += OnPlayerAction;
        battleHUD.OnCancelSelected += OnCancelAction;

        if (skillUI != null)
            skillUI.SkillSelected += OnBattleSkillSelected;

        SubscribeDialogueEvents();
    }

    private void Update()
    {
        if (!isChoosingItem &&
            !isSelectingItemTarget &&
            !isChoosingSkill &&
            !isSelectingSkillTarget)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            OnCancelAction();
    }

    private void OnDestroy()
    {
        battleHUD.OnActionSelected -= OnPlayerAction;
        battleHUD.OnCancelSelected -= OnCancelAction;

        if (skillUI != null)
            skillUI.SkillSelected -= OnBattleSkillSelected;

        if (Inventory.Instance != null)
        {
            Inventory.Instance.ItemUseRequested -= OnBattleItemSelected;
            Inventory.Instance.SetKeyboardToggleEnabled(true);
        }

        if (subscribedToDialogueEvents && DialogueManager.Instance != null)
            DialogueManager.Instance.OnPopupVisibilityChanged -= HandlePopupVisibilityChanged;

        foreach (UnitBattle enemy in enemyBattleUnits)
        {
            enemy.OnSelected -= OnEnemyTargetSelected;
            enemy.OnHoverEntered -= OnEnemyTargetHoverEntered;
        }

        foreach (UnitBattle player in playerBattleUnits)
        {
            player.OnSelected -= OnPlayerItemTargetSelected;
            player.OnHoverEntered -= OnPlayerItemTargetHoverEntered;
            player.OnHoverExited -= OnPlayerItemTargetHoverExited;
        }
    }

    private void Start()
    {
        if (BattleRelay.PlayerUnits == null || BattleRelay.EnemyUnits == null) return;

        SubscribeDialogueEvents();
        battleHUD.HideActionMenu();

        playerBattleUnits = battleStations.SpawnPlayerUnits(BattleRelay.PlayerUnits);
        enemyBattleUnits = battleStations.SpawnEnemyUnits(BattleRelay.EnemyUnits);
        SubscribeEnemyTargetCallbacks();
        SubscribePlayerItemTargetCallbacks();
        SubscribeInventoryEvents();

        turnOrderManager.Initialize(playerBattleUnits, enemyBattleUnits);

        StartCoroutine(StartBattleSequence());
    }

    private IEnumerator StartBattleSequence()
    {
        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.enemyAppeared,
            ("enemy", BattleRelay.EnemyPartyName)
        );

        BattleRelay.Clear();

        ProcessNextTurn();
    }

    #region Turn Order

    public void ProcessNextTurn()
    {
        ClearPlayerTurnIndicators();

        UnitBattle next = turnOrderManager.PeekNext();
        if (next == null) return;

        if (playerBattleUnits.Contains(next))
        {
            currentState = BattleState.PlayerTurn;
            currentActingUnit = next;
            currentActingUnit.ClearGuard();
            currentActingUnit.SetTargeted(true);
            battleHUD.HideCancelButton();

            if (currentActingUnit.BattleAIProfile == null)
                battleHUD.ShowActionMenu();
            else
                StartCoroutine(HandleAITurnSequence(currentActingUnit, playerBattleUnits, enemyBattleUnits));
        }
        else
        {
            currentState = BattleState.EnemyTurn;
            currentActingUnit = next;
            currentActingUnit.ClearGuard();
            battleHUD.HideActionMenu();
            battleHUD.HideCancelButton();
            StartCoroutine(HandleAITurnSequence(currentActingUnit, enemyBattleUnits, playerBattleUnits));
        }
    }

    public void OnPlayerAction(BattleAction action)
    {
        battleHUD.HideActionMenu();

        switch (action)
        {
            case BattleAction.Attack:
                StartCoroutine(HandleAttackSequence());
                break;
            case BattleAction.Defend:
                ClearPlayerTurnIndicators();
                StartCoroutine(HandleDefendSequence());
                break;
            case BattleAction.Item:
                BeginBattleItemSelection();
                break;
            case BattleAction.Skill:
                BeginBattleSkillSelection();
                break;
            case BattleAction.Pass:
                ClearPlayerTurnIndicators();
                StartCoroutine(HandlePassSequence());
                break;
            case BattleAction.Flee:
                ClearPlayerTurnIndicators();
                StartCoroutine(HandleFleeSequence());
                break;
        }
    }
    #endregion

    #region AI

    private IEnumerator HandleAITurnSequence(UnitBattle actingUnit, List<UnitBattle> allies, List<UnitBattle> opponents)
    {
        if (actingUnit == null || !actingUnit.IsAlive)
        {
            turnOrderManager.CompleteCurrentTurn();
            ProcessNextTurn();
            yield break;
        }

        BattleAIProfile profile = actingUnit.BattleAIProfile;
        if (profile != null && profile.ThinkDelay > 0f)
            yield return new WaitForSeconds(profile.ThinkDelay);
        else
            yield return new WaitForSeconds(0.5f);

        BattleAIIntent intent = profile != null
            ? profile.ChooseIntent(actingUnit, allies, opponents)
            : CreateDefaultAIIntent(opponents);

        yield return ExecuteAIIntent(actingUnit, intent, allies, opponents);
    }

    private BattleAIIntent CreateDefaultAIIntent(List<UnitBattle> opponents)
    {
        UnitBattle target = GetFirstAliveUnit(opponents);
        return target != null
            ? new BattleAIIntent(BattleAIAction.Attack, target)
            : new BattleAIIntent(BattleAIAction.Pass);
    }

    private IEnumerator ExecuteAIIntent(UnitBattle actingUnit, BattleAIIntent intent, List<UnitBattle> allies, List<UnitBattle> opponents)
    {
        switch (intent.Action)
        {
            case BattleAIAction.Attack:
                if (intent.Target != null)
                    yield return ExecuteAttackSequence(actingUnit, intent.Target);
                break;
            case BattleAIAction.Skill:
                yield return ExecuteSkillSequence(actingUnit, intent.Target);
                break;
            case BattleAIAction.Item:
                yield return ExecuteItemSequence(actingUnit);
                break;
            case BattleAIAction.Defend:
                yield return ExecuteDefendSequence(actingUnit);
                break;
            case BattleAIAction.Pass:
                yield return ExecutePassSequence(actingUnit);
                break;
            case BattleAIAction.Flee:
                yield return ExecuteAIFleeSequence(actingUnit, allies, opponents);
                yield break;
        }

        if (IsBattleOver())
            yield break;

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    private IEnumerator ExecuteAIFleeSequence(UnitBattle actingUnit, List<UnitBattle> allies, List<UnitBattle> opponents)
    {
        DialogueManager.Instance.BeginPopupSequence();

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitFleeAttempt,
            ("unit", actingUnit.name)
        );

        bool success = actingUnit.BattleAIProfile != null && actingUnit.BattleAIProfile.RollFleeSuccess(actingUnit, opponents);

        if (success)
        {
            yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
                DialogueManager.Instance.Messages.unitFleeSuccess,
                ("unit", actingUnit.name)
            );

            if (enemyBattleUnits.Contains(actingUnit))
            {
                actingUnit.OnSelected -= OnEnemyTargetSelected;
                actingUnit.OnHoverEntered -= OnEnemyTargetHoverEntered;
            }

            allies.Remove(actingUnit);
            turnOrderManager.RemoveUnit(actingUnit);

            Destroy(actingUnit.gameObject);

            if (enemyBattleUnits.Count == 0)
            {
                currentState = BattleState.Win;
                yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.victory);
                DialogueManager.Instance.EndPopupSequence();
                BattleRelay.MarkCurrentEncounterDefeated();
                SceneManager.LoadScene("Gameplay");
                yield break;
            }

            if (playerBattleUnits.Count == 0)
            {
                currentState = BattleState.Lose;
                yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.defeat);
                DialogueManager.Instance.EndPopupSequence();
                yield break;
            }
        }
        else
        {
            yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
                DialogueManager.Instance.Messages.unitFleeFailed,
                ("unit", actingUnit.name)
            );

            turnOrderManager.CompleteCurrentTurn();
        }

        DialogueManager.Instance.EndPopupSequence();
        ProcessNextTurn();
    }

    #endregion

    #region Attack

    private IEnumerator HandleAttackSequence()
    {
        UnitBattle attacker = currentActingUnit;
        UnitBattle target = GetFirstAliveEnemy();

        if (attacker == null || target == null)
            yield break;

        yield return WaitForEnemyTargetSelection(target);

        if (targetSelectionCanceled)
        {
            ClearTargetIndicator();
            battleHUD.ShowActionMenu();
            yield break;
        }

        target = confirmedEnemyTarget;
        StopEnemyTargetSelection();

        if (target == null || !target.IsAlive)
        {
            ClearTargetIndicator();
            yield break;
        }

        yield return ExecuteAttackSequence(attacker, target);

        ClearTargetIndicator();
        if (IsBattleOver())
            yield break;

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    private IEnumerator ExecuteAttackSequence(UnitBattle attacker, UnitBattle target)
    {
        bool previousKeepPlayerCardsVisible = keepPlayerCardsVisibleDuringPopups;
        keepPlayerCardsVisibleDuringPopups = previousKeepPlayerCardsVisible || playerBattleUnits.Contains(target);
        RefreshPlayerCardsForPopupState();

        string attackerName = attacker.name;
        string targetName = target.name;

        yield return DialogueManager.Instance.ShowFormattedPopupForSeconds(
            1f,
            DialogueManager.Instance.Messages.unitAttack,
            ("unit", attackerName),
            ("target", targetName)
        );

        attacker.PlayAttack();
        yield return new WaitForSeconds(0.5f);

        int damage = CalculateAttackDamage(attacker, target);
        target.PlayHurt();
        yield return target.SetHPAnimated(target.CurrentHP - damage);

        if (damagePopupDelay > 0f)
            yield return new WaitForSeconds(damagePopupDelay);

        DialogueManager.Instance.BeginPopupSequence();
        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitDamageDealt,
            ("unit", attackerName),
            ("amount", damage.ToString()),
            ("target", targetName)
        );

        if (!target.IsAlive)
        {
            target.PlayDie();

            yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
                DialogueManager.Instance.Messages.unitDie,
                ("unit", targetName)
            );

            RemoveDefeatedUnit(target);
            turnOrderManager.RemoveUnit(target);
            Destroy(target.gameObject);

            if (enemyBattleUnits.Count == 0)
            {
                currentState = BattleState.Win;
                yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.victory);
                DialogueManager.Instance.EndPopupSequence();
                keepPlayerCardsVisibleDuringPopups = previousKeepPlayerCardsVisible;
                RefreshPlayerCardsForPopupState();
                BattleRelay.MarkCurrentEncounterDefeated();
                SceneManager.LoadScene("Gameplay");
                yield break;
            }

            if (playerBattleUnits.Count == 0)
            {
                currentState = BattleState.Lose;
                yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.defeat);
                DialogueManager.Instance.EndPopupSequence();
                keepPlayerCardsVisibleDuringPopups = previousKeepPlayerCardsVisible;
                RefreshPlayerCardsForPopupState();
                yield break;
            }
        }

        DialogueManager.Instance.EndPopupSequence();
        keepPlayerCardsVisibleDuringPopups = previousKeepPlayerCardsVisible;
        RefreshPlayerCardsForPopupState();
    }

    private void RemoveDefeatedUnit(UnitBattle unit)
    {
        if (enemyBattleUnits.Remove(unit))
        {
            unit.OnSelected -= OnEnemyTargetSelected;
            unit.OnHoverEntered -= OnEnemyTargetHoverEntered;
        }
        else if (playerBattleUnits.Remove(unit))
        {
            unit.OnSelected -= OnPlayerItemTargetSelected;
            unit.OnHoverEntered -= OnPlayerItemTargetHoverEntered;
            unit.OnHoverExited -= OnPlayerItemTargetHoverExited;
        }
    }

    private bool IsBattleOver()
    {
        return currentState == BattleState.Win || currentState == BattleState.Lose;
    }

    private IEnumerator WaitForEnemyTargetSelection(UnitBattle defaultTarget)
    {
        isSelectingTarget = true;
        targetConfirmed = false;
        targetSelectionCanceled = false;
        selectedEnemyTarget = defaultTarget;
        confirmedEnemyTarget = null;

        SetEnemyTargetingEnabled(true);
        SetTargetIndicator(defaultTarget);
        battleHUD.ShowCancelButton();

        while (!targetConfirmed && !targetSelectionCanceled)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                CancelEnemyTargetSelection();

            yield return null;
        }
    }

    private void SubscribeEnemyTargetCallbacks()
    {
        foreach (UnitBattle enemy in enemyBattleUnits)
        {
            enemy.OnSelected += OnEnemyTargetSelected;
            enemy.OnHoverEntered += OnEnemyTargetHoverEntered;
        }
    }

    private void SubscribePlayerItemTargetCallbacks()
    {
        foreach (UnitBattle player in playerBattleUnits)
        {
            player.OnSelected += OnPlayerItemTargetSelected;
            player.OnHoverEntered += OnPlayerItemTargetHoverEntered;
            player.OnHoverExited += OnPlayerItemTargetHoverExited;
        }
    }

    private void OnEnemyTargetSelected(UnitBattle enemy)
    {
        if (isSelectingSkillTarget &&
            skillBeingUsed != null &&
            skillBeingUsed.Targeting == SkillData.TargetType.SingleEnemy)
        {
            ConfirmSkillTarget(enemy);
            return;
        }

        if (!isSelectingTarget || targetConfirmed || enemy == null || !enemy.IsAlive)
            return;

        selectedEnemyTarget = enemy;
        confirmedEnemyTarget = enemy;
        SetTargetIndicator(enemy);
        targetConfirmed = true;
    }

    private void OnEnemyTargetHoverEntered(UnitBattle enemy)
    {
        if (isSelectingSkillTarget &&
            skillBeingUsed != null &&
            skillBeingUsed.Targeting == SkillData.TargetType.SingleEnemy)
        {
            if (enemy != null && enemy.IsAlive)
                SetTargetIndicator(enemy);
            return;
        }

        if (!isSelectingTarget || targetConfirmed || enemy == null || !enemy.IsAlive)
            return;

        selectedEnemyTarget = enemy;
        SetTargetIndicator(enemy);
    }

    private void SetEnemyTargetingEnabled(bool isEnabled)
    {
        foreach (UnitBattle enemy in enemyBattleUnits)
            enemy.SetTargetable(isEnabled);
    }

    private void SetTargetIndicator(UnitBattle target)
    {
        foreach (UnitBattle enemy in enemyBattleUnits)
            enemy.SetTargeted(enemy == target);
    }

    private void ClearPlayerTurnIndicators()
    {
        foreach (UnitBattle playerUnit in playerBattleUnits)
            playerUnit.SetTargeted(false);
    }

    private void StopEnemyTargetSelection()
    {
        isSelectingTarget = false;
        targetConfirmed = false;
        SetEnemyTargetingEnabled(false);
        battleHUD.HideCancelButton();
    }

    private void CancelEnemyTargetSelection()
    {
        targetSelectionCanceled = true;
        StopEnemyTargetSelection();
    }

    private void OnCancelAction()
    {
        if (isChoosingSkill)
        {
            CancelSkillSelectionToActionMenu();
            return;
        }

        if (isSelectingSkillTarget)
        {
            ReturnToSkillUI();
            return;
        }

        if (isChoosingItem)
        {
            CancelItemSelectionToActionMenu();
            return;
        }

        if (isSelectingItemTarget)
        {
            ReturnToItemInventory();
            return;
        }

        if (!isSelectingTarget)
            return;

        CancelEnemyTargetSelection();
    }

    private void ClearTargetIndicator()
    {
        selectedEnemyTarget = null;
        confirmedEnemyTarget = null;
        SetTargetIndicator(null);
    }

    private UnitBattle GetFirstAliveEnemy()
    {
        return GetFirstAliveUnit(enemyBattleUnits);
    }

    private UnitBattle GetFirstAliveUnit(List<UnitBattle> units)
    {
        foreach (UnitBattle unit in units)
        {
            if (unit != null && unit.IsAlive)
                return unit;
        }

        return null;
    }

    private int CalculateAttackDamage(UnitBattle attacker, UnitBattle target)
    {
        return Mathf.Max(0, attacker.Attack - target.Defense);
    }

    private void HandlePopupVisibilityChanged(bool isVisible)
    {
        RefreshPlayerCardsForPopupState(isVisible);
    }

    private void RefreshPlayerCardsForPopupState()
    {
        RefreshPlayerCardsForPopupState(false);
    }

    private void RefreshPlayerCardsForPopupState(bool popupVisible)
    {
        foreach (UnitBattle playerUnit in playerBattleUnits)
        {
            if (playerUnit != null)
                playerUnit.gameObject.SetActive(ShouldShowPlayerCardsDuringPopup(popupVisible));
        }
    }

    private bool ShouldShowPlayerCardsDuringPopup(bool popupVisible)
    {
        return !popupVisible || currentState == BattleState.PlayerTurn || keepPlayerCardsVisibleDuringPopups;
    }

    private void SubscribeDialogueEvents()
    {
        if (subscribedToDialogueEvents || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.OnPopupVisibilityChanged += HandlePopupVisibilityChanged;
        subscribedToDialogueEvents = true;
    }

    #endregion

    #region Item & Skill

    private void SubscribeInventoryEvents()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("BattleManager could not find the persistent Inventory.");
            return;
        }

        Inventory.Instance.ItemUseRequested -= OnBattleItemSelected;
        Inventory.Instance.ItemUseRequested += OnBattleItemSelected;
        Inventory.Instance.SetKeyboardToggleEnabled(false);
        Inventory.Instance.CloseItemPanel();
    }

    private void BeginBattleItemSelection()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("Cannot use an item because no Inventory exists.");
            battleHUD.ShowActionMenu();
            return;
        }

        itemBeingUsed = null;
        selectedItemTarget = null;
        isChoosingItem = true;
        isSelectingItemTarget = false;

        Inventory.Instance.OpenItemPanel();
        battleHUD.ShowCancelButton();
    }

    private void OnBattleItemSelected(ItemData itemData)
    {
        if (!isChoosingItem || itemData == null || currentState != BattleState.PlayerTurn)
            return;

        itemBeingUsed = itemData;
        isChoosingItem = false;
        isSelectingItemTarget = true;
        selectedItemTarget = null;

        Inventory.Instance.CloseItemPanel();
        ClearPlayerTurnIndicators();
        SetPlayerItemTargetingEnabled(true);
        battleHUD.ShowCancelButton();
    }

    private void OnPlayerItemTargetSelected(UnitBattle target)
    {
        if (isSelectingSkillTarget &&
            skillBeingUsed != null &&
            skillBeingUsed.Targeting == SkillData.TargetType.SingleAlly)
        {
            ConfirmSkillTarget(target);
            return;
        }

        if (!isSelectingItemTarget || target == null || !target.IsAlive || itemBeingUsed == null)
            return;

        if (!itemBeingUsed.Use(target))
        {
            ShowItemCannotUsePopup(target, itemBeingUsed);
            return;
        }

        ItemData usedItem = itemBeingUsed;
        StopPlayerItemTargeting();
        Inventory.Instance.RemoveItem(usedItem, 1);
        Inventory.Instance.ClearItemSelection();
        StartCoroutine(CompletePlayerItemUse(target, usedItem));
    }

    private void OnPlayerItemTargetHoverEntered(UnitBattle target)
    {
        if (isSelectingSkillTarget &&
            skillBeingUsed != null &&
            skillBeingUsed.Targeting == SkillData.TargetType.SingleAlly)
        {
            if (target != null && target.IsAlive)
                SetPlayerItemTargetIndicator(target);
            return;
        }

        if (!isSelectingItemTarget || target == null || !target.IsAlive)
            return;

        selectedItemTarget = target;
        SetPlayerItemTargetIndicator(target);
        target.ShowItemPreview(itemBeingUsed);
    }

    private void OnPlayerItemTargetHoverExited(UnitBattle target)
    {
        if (isSelectingSkillTarget &&
            skillBeingUsed != null &&
            skillBeingUsed.Targeting == SkillData.TargetType.SingleAlly)
        {
            SetPlayerItemTargetIndicator(null);
            return;
        }

        if (!isSelectingItemTarget || selectedItemTarget != target)
            return;

        target.ClearItemPreview();
        selectedItemTarget = null;
        SetPlayerItemTargetIndicator(null);
    }

    private IEnumerator CompletePlayerItemUse(UnitBattle target, ItemData usedItem)
    {
        string unitName = target.UnitData != null
            ? target.UnitData.unitName
            : target.name;

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.itemUsed,
            ("unit", unitName),
            ("item", usedItem.ItemName)
        );

        ClearPlayerTurnIndicators();
        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    private void SetPlayerItemTargetingEnabled(bool isEnabled)
    {
        foreach (UnitBattle player in playerBattleUnits)
            player.SetTargetable(isEnabled && player.IsAlive);
    }

    private void SetPlayerItemTargetIndicator(UnitBattle target)
    {
        foreach (UnitBattle player in playerBattleUnits)
        {
            if (player != target)
                player.ClearItemPreview();

            player.SetTargeted(player == target, false);
        }
    }

    private void ShowItemCannotUsePopup(UnitBattle target, ItemData item)
    {
        if (DialogueManager.Instance == null ||
            DialogueManager.Instance.Messages == null)
            return;

        string unitName = target.UnitData != null
            ? target.UnitData.unitName
            : target.name;

        DialogueManager.Instance.ShowFormattedPopup(
            DialogueManager.Instance.Messages.itemCannotUse,
            ("unit", unitName),
            ("item", item.ItemName));
    }

    private void StopPlayerItemTargeting()
    {
        isSelectingItemTarget = false;
        isChoosingItem = false;
        SetPlayerItemTargetingEnabled(false);
        SetPlayerItemTargetIndicator(null);
        selectedItemTarget = null;
        itemBeingUsed = null;
        battleHUD.HideCancelButton();
        Inventory.Instance?.CloseItemPanel();
    }

    private void ReturnToItemInventory()
    {
        SetPlayerItemTargetingEnabled(false);
        SetPlayerItemTargetIndicator(null);
        selectedItemTarget = null;
        itemBeingUsed = null;
        isSelectingItemTarget = false;
        isChoosingItem = true;

        Inventory.Instance?.OpenItemPanel();
        battleHUD.ShowCancelButton();
    }

    private void CancelItemSelectionToActionMenu()
    {
        isChoosingItem = false;
        isSelectingItemTarget = false;
        itemBeingUsed = null;
        selectedItemTarget = null;

        Inventory.Instance?.CloseItemPanel();
        Inventory.Instance?.ClearItemSelection();
        SetPlayerItemTargetingEnabled(false);
        ClearPlayerTurnIndicators();

        if (currentActingUnit != null)
            currentActingUnit.SetTargeted(true);

        battleHUD.HideCancelButton();
        battleHUD.ShowActionMenu();
    }

    private void BeginBattleSkillSelection()
    {
        if (skillUI == null)
        {
            Debug.LogWarning("BattleManager could not find a SkillUI in the Battle scene.");
            battleHUD.ShowActionMenu();
            return;
        }

        isChoosingSkill = true;
        isSelectingSkillTarget = false;
        skillBeingUsed = null;
        skillUI.OpenSkillUI(currentActingUnit != null ? currentActingUnit.UnitData : null);
        battleHUD.ShowCancelButton();
    }

    private void OnBattleSkillSelected(SkillData skill)
    {
        if (!isChoosingSkill ||
            isExecutingSkill ||
            skill == null ||
            currentActingUnit == null ||
            currentState != BattleState.PlayerTurn)
            return;

        if (!currentActingUnit.CanPaySkillCost(skill))
        {
            DialogueManager.Instance?.ShowFormattedPopup(
                DialogueManager.Instance.Messages.skillCannotUse,
                ("unit", currentActingUnit.name),
                ("skill", skill.SkillName));
            return;
        }

        skillBeingUsed = skill;
        isChoosingSkill = false;
        skillUI.CloseSkillUI();

        switch (skill.Targeting)
        {
            case SkillData.TargetType.Self:
                StartCoroutine(ExecutePlayerSkill(skill, new List<UnitBattle> { currentActingUnit }));
                break;
            case SkillData.TargetType.AllAllies:
                StartCoroutine(ExecutePlayerSkill(skill, GetLivingUnits(playerBattleUnits)));
                break;
            case SkillData.TargetType.AllEnemies:
                StartCoroutine(ExecutePlayerSkill(skill, GetLivingUnits(enemyBattleUnits)));
                break;
            case SkillData.TargetType.SingleAlly:
                BeginSkillTargetSelection(true);
                break;
            case SkillData.TargetType.SingleEnemy:
                BeginSkillTargetSelection(false);
                break;
        }
    }

    private void BeginSkillTargetSelection(bool targetsAllies)
    {
        isSelectingSkillTarget = true;
        ClearPlayerTurnIndicators();
        ClearTargetIndicator();

        if (targetsAllies)
            SetPlayerItemTargetingEnabled(true);
        else
            SetEnemyTargetingEnabled(true);

        battleHUD.ShowCancelButton();
    }

    private void ConfirmSkillTarget(UnitBattle target)
    {
        if (!isSelectingSkillTarget ||
            isExecutingSkill ||
            target == null ||
            !target.IsAlive ||
            skillBeingUsed == null)
            return;

        SkillData confirmedSkill = skillBeingUsed;
        StopSkillTargetSelection();
        StartCoroutine(ExecutePlayerSkill(confirmedSkill, new List<UnitBattle> { target }));
    }

    private void StopSkillTargetSelection()
    {
        isSelectingSkillTarget = false;
        SetEnemyTargetingEnabled(false);
        SetPlayerItemTargetingEnabled(false);
        SetTargetIndicator(null);
        SetPlayerItemTargetIndicator(null);
        battleHUD.HideCancelButton();
    }

    private void ReturnToSkillUI()
    {
        StopSkillTargetSelection();
        skillBeingUsed = null;
        isChoosingSkill = true;
        skillUI?.OpenSkillUI(currentActingUnit != null ? currentActingUnit.UnitData : null);
        battleHUD.ShowCancelButton();
    }

    public void CloseSkillUI()
    {
        skillUI?.CloseSkillUI();
    }

    private void CancelSkillSelectionToActionMenu()
    {
        isChoosingSkill = false;
        isSelectingSkillTarget = false;
        skillBeingUsed = null;
        CloseSkillUI();
        SetEnemyTargetingEnabled(false);
        SetPlayerItemTargetingEnabled(false);
        ClearTargetIndicator();
        ClearPlayerTurnIndicators();

        if (currentActingUnit != null)
            currentActingUnit.SetTargeted(true);

        battleHUD.HideCancelButton();
        battleHUD.ShowActionMenu();
    }

    private IEnumerator ExecutePlayerSkill(SkillData skill, List<UnitBattle> targets)
    {
        if (skill == null ||
            currentActingUnit == null ||
            targets == null ||
            targets.Count == 0 ||
            !currentActingUnit.CanPaySkillCost(skill))
        {
            CancelSkillSelectionToActionMenu();
            yield break;
        }

        isExecutingSkill = true;
        UnitBattle caster = currentActingUnit;
        caster.PaySkillCost(skill);

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitSkill,
            ("unit", caster.name),
            ("skill", skill.SkillName));

        bool hasDamageEffect = HasEffect(skill, SkillData.EffectType.Damage);
        if (hasDamageEffect)
            caster.PlayAttack();

        yield return ApplySkillEffects(skill, caster, targets);
        yield return RemoveUnitsDefeatedBySkill();

        isExecutingSkill = false;
        skillBeingUsed = null;
        ClearTargetIndicator();
        ClearPlayerTurnIndicators();

        if (IsBattleOver())
            yield break;

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    private IEnumerator ApplySkillEffects(
        SkillData skill,
        UnitBattle caster,
        List<UnitBattle> targets)
    {
        foreach (SkillData.SkillEffect effect in skill.Effects)
        {
            if (effect == null ||
                effect.Recipient != SkillData.EffectRecipient.Caster)
                continue;

            if (Random.value > effect.SuccessChance)
            {
                yield return ShowSkillMissPopup(caster, skill, caster);
                continue;
            }

            yield return ApplySkillEffect(effect, caster);
        }

        foreach (UnitBattle target in targets)
        {
            if (target == null || !target.IsAlive)
                continue;

            foreach (SkillData.SkillEffect effect in skill.Effects)
            {
                if (effect == null ||
                    effect.Recipient != SkillData.EffectRecipient.Target)
                    continue;

                if (Random.value > effect.SuccessChance)
                {
                    yield return ShowSkillMissPopup(caster, skill, target);
                    continue;
                }

                yield return ApplySkillEffect(effect, target);
            }
        }
    }

    private IEnumerator ShowSkillMissPopup(
        UnitBattle caster,
        SkillData skill,
        UnitBattle target)
    {
        string casterName = caster != null ? caster.name : "Unit";
        string targetName = target != null ? target.name : "target";

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.skillMiss,
            ("unit", casterName),
            ("skill", skill != null ? skill.SkillName : "skill"),
            ("target", targetName));
    }

    private IEnumerator ApplySkillEffect(SkillData.SkillEffect effect, UnitBattle target)
    {
        int amount = GetSkillEffectAmount(effect, target);

        switch (effect.Type)
        {
            case SkillData.EffectType.Damage:
                int damage = effect.ValueType == SkillData.ValueType.Flat
                    ? Mathf.Max(0, amount - target.Defense)
                    : amount;
                target.PlayHurt();
                yield return target.SetHPAnimated(target.CurrentHP - damage);
                break;
            case SkillData.EffectType.HealHP:
                yield return target.SetHPAnimated(target.CurrentHP + amount);
                break;
            case SkillData.EffectType.HealMP:
                yield return target.SetMPAnimated(target.CurrentMP + amount);
                break;
            case SkillData.EffectType.IncreaseAttack:
                target.IncreaseAttack(amount);
                break;
            case SkillData.EffectType.IncreaseDefense:
                target.IncreaseDefense(amount);
                break;
            case SkillData.EffectType.IncreaseSpeed:
                target.IncreaseSpeed(amount);
                break;
            case SkillData.EffectType.DecreaseAttack:
                target.DecreaseAttack(amount);
                break;
            case SkillData.EffectType.DecreaseDefense:
                target.DecreaseDefense(amount);
                break;
            case SkillData.EffectType.DecreaseSpeed:
                target.DecreaseSpeed(amount);
                break;
        }
    }

    private int GetSkillEffectAmount(SkillData.SkillEffect effect, UnitBattle target)
    {
        if (effect.ValueType == SkillData.ValueType.Flat)
            return Mathf.RoundToInt(effect.Value);

        float baseValue;
        switch (effect.Type)
        {
            case SkillData.EffectType.Damage:
            case SkillData.EffectType.HealHP:
                baseValue = target.MaxHP;
                break;
            case SkillData.EffectType.HealMP:
                baseValue = target.MaxMP;
                break;
            case SkillData.EffectType.IncreaseAttack:
            case SkillData.EffectType.DecreaseAttack:
                baseValue = target.Attack;
                break;
            case SkillData.EffectType.IncreaseDefense:
            case SkillData.EffectType.DecreaseDefense:
                baseValue = target.BaseDefense;
                break;
            case SkillData.EffectType.IncreaseSpeed:
            case SkillData.EffectType.DecreaseSpeed:
                baseValue = target.Speed;
                break;
            default:
                baseValue = 0f;
                break;
        }

        return Mathf.CeilToInt(baseValue * effect.Value / 100f);
    }

    private IEnumerator RemoveUnitsDefeatedBySkill()
    {
        List<UnitBattle> defeatedUnits = new();
        defeatedUnits.AddRange(playerBattleUnits.FindAll(unit => unit != null && !unit.IsAlive));
        defeatedUnits.AddRange(enemyBattleUnits.FindAll(unit => unit != null && !unit.IsAlive));

        foreach (UnitBattle defeated in defeatedUnits)
        {
            string unitName = defeated.name;
            defeated.PlayDie();

            yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
                DialogueManager.Instance.Messages.unitDie,
                ("unit", unitName));

            RemoveDefeatedUnit(defeated);
            turnOrderManager.RemoveUnit(defeated);
            Destroy(defeated.gameObject);
        }

        if (enemyBattleUnits.Count == 0)
        {
            currentState = BattleState.Win;
            yield return DialogueManager.Instance.ShowPopupAndWait(
                DialogueManager.Instance.Messages.victory);
            BattleRelay.MarkCurrentEncounterDefeated();
            SceneManager.LoadScene("Gameplay");
        }
        else if (playerBattleUnits.Count == 0)
        {
            currentState = BattleState.Lose;
            yield return DialogueManager.Instance.ShowPopupAndWait(
                DialogueManager.Instance.Messages.defeat);
        }
    }

    private List<UnitBattle> GetLivingUnits(List<UnitBattle> units)
    {
        return units.FindAll(unit => unit != null && unit.IsAlive);
    }

    private bool HasEffect(SkillData skill, SkillData.EffectType effectType)
    {
        foreach (SkillData.SkillEffect effect in skill.Effects)
        {
            if (effect != null && effect.Type == effectType)
                return true;
        }

        return false;
    }

    private IEnumerator ExecuteItemSequence(UnitBattle actingUnit)
    {
        string unitName = actingUnit != null ? actingUnit.name : "Unit";

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitItem,
            ("unit", unitName)
        );
    }

    private IEnumerator ExecuteSkillSequence(UnitBattle actingUnit, UnitBattle target)
    {
        string unitName = actingUnit != null ? actingUnit.name : "Unit";

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitSkill,
            ("unit", unitName),
            ("skill", "a skill")
        );
    }

    #endregion

    #region Pass

    private IEnumerator HandlePassSequence()
    {
        yield return ExecutePassSequence(currentActingUnit);

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    private IEnumerator ExecutePassSequence(UnitBattle actingUnit)
    {
        string unitName = actingUnit != null ? actingUnit.name : "Unit";
        int recoveredHP = actingUnit != null ? actingUnit.RecoverHPPercent(0.1f) : 0;
        int recoveredMP = actingUnit != null ? actingUnit.RecoverMPPercent(0.1f) : 0;

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitPass,
            ("unit", unitName),
            ("hp", recoveredHP.ToString()),
            ("mp", recoveredMP.ToString())
        );
    }

    #endregion

    #region Defend

    private IEnumerator HandleDefendSequence()
    {
        yield return ExecuteDefendSequence(currentActingUnit);

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    private IEnumerator ExecuteDefendSequence(UnitBattle actingUnit)
    {
        string unitName = actingUnit != null ? actingUnit.name : "Unit";
        int recoveredMP = 0;

        if (actingUnit != null)
        {
            recoveredMP = actingUnit.RecoverMPPercent(0.2f);
            actingUnit.StartGuard();
        }

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitGuard,
            ("unit", unitName),
            ("mp", recoveredMP.ToString())
        );
    }

    #endregion

    #region Flee

    private IEnumerator HandleFleeSequence()
    {
        DialogueManager.Instance.BeginPopupSequence();

        yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.fleeAttempt);

        bool success = Random.value <= CalculateFleeChance();

        if (success)
        {
            yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.escapeSuccess);

            DialogueManager.Instance.EndPopupSequence();
            BattleRelay.ClearCurrentEncounter();
            SceneManager.LoadScene("Gameplay");
        }
        else
        {
            yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.escapeFailed);

            yield return new WaitForSeconds(1f);

            DialogueManager.Instance.EndPopupSequence();
            turnOrderManager.CompleteCurrentTurn();
            ProcessNextTurn();
        }
    }

    private float CalculateFleeChance()
    {
        if (playerBattleUnits.Count == 0 || enemyBattleUnits.Count == 0)
            return 1f;

        float playerAvgSpeed = 0f;
        foreach (var unit in playerBattleUnits)
            playerAvgSpeed += unit.Speed;
        playerAvgSpeed /= playerBattleUnits.Count;

        float enemyAvgSpeed = 0f;
        foreach (var unit in enemyBattleUnits)
            enemyAvgSpeed += unit.Speed;
        enemyAvgSpeed /= enemyBattleUnits.Count;

        float speedRatio = playerAvgSpeed / Mathf.Max(1f, enemyAvgSpeed);
        float chance = 0.5f * speedRatio;

        return Mathf.Clamp(chance, 0.1f, 0.95f);
    }
    #endregion
}
