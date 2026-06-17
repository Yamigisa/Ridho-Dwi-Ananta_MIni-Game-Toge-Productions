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

    [Header("Feedback")]
    [SerializeField] private float damagePopupDelay = 0.35f;

    private UnitBattle currentActingUnit;
    public UnitBattle CurrentActingUnit => currentActingUnit;
    private UnitBattle selectedEnemyTarget;
    private bool isSelectingTarget;
    private bool targetConfirmed;
    private bool targetSelectionCanceled;
    private bool subscribedToDialogueEvents;
    private bool keepPlayerCardsVisibleDuringPopups;

    private BattleState currentState;
    public BattleState CurrentState => currentState;

    private void Awake()
    {
        battleHUD.OnActionSelected += OnPlayerAction;
        battleHUD.OnCancelSelected += OnCancelAction;
        SubscribeDialogueEvents();
    }

    private void OnDestroy()
    {
        battleHUD.OnActionSelected -= OnPlayerAction;
        battleHUD.OnCancelSelected -= OnCancelAction;

        if (subscribedToDialogueEvents && DialogueManager.Instance != null)
            DialogueManager.Instance.OnPopupVisibilityChanged -= HandlePopupVisibilityChanged;

        foreach (UnitBattle enemy in enemyBattleUnits)
            enemy.OnSelected -= OnEnemyTargetSelected;
    }

    private void Start()
    {
        if (BattleRelay.PlayerUnits == null || BattleRelay.EnemyUnits == null) return;

        SubscribeDialogueEvents();
        battleHUD.HideActionMenu();

        playerBattleUnits = battleStations.SpawnPlayerUnits(BattleRelay.PlayerUnits);
        enemyBattleUnits = battleStations.SpawnEnemyUnits(BattleRelay.EnemyUnits);
        SubscribeEnemyTargetCallbacks();

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
                // open item submenu
                turnOrderManager.CompleteCurrentTurn();
                ProcessNextTurn();
                break;
            case BattleAction.Skill:
                // open skill submenu
                turnOrderManager.CompleteCurrentTurn();
                ProcessNextTurn();
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
                actingUnit.OnSelected -= OnEnemyTargetSelected;

            allies.Remove(actingUnit);
            turnOrderManager.RemoveUnit(actingUnit);

            Destroy(actingUnit.gameObject);

            if (enemyBattleUnits.Count == 0)
            {
                currentState = BattleState.Win;
                yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.victory);
                DialogueManager.Instance.EndPopupSequence();
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

        target = selectedEnemyTarget;
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
            unit.OnSelected -= OnEnemyTargetSelected;
        else
            playerBattleUnits.Remove(unit);
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
            enemy.OnSelected += OnEnemyTargetSelected;
    }

    private void OnEnemyTargetSelected(UnitBattle enemy)
    {
        if (!isSelectingTarget || enemy == null || !enemy.IsAlive)
            return;

        if (selectedEnemyTarget == enemy)
        {
            targetConfirmed = true;
            return;
        }

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
        if (!isSelectingTarget)
            return;

        CancelEnemyTargetSelection();
    }

    private void ClearTargetIndicator()
    {
        selectedEnemyTarget = null;
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
        return Mathf.Max(1, attacker.Attack - target.Defense);
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
        string targetName = target != null ? target.name : "target";

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitSkill,
            ("unit", unitName),
            ("target", targetName)
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
