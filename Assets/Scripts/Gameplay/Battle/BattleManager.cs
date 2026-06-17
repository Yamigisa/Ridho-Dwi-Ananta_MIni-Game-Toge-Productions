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
    private UnitBattle currentActingUnit;
    public UnitBattle CurrentActingUnit => currentActingUnit;
    private UnitBattle selectedEnemyTarget;
    private bool isSelectingTarget;
    private bool targetConfirmed;
    private bool targetSelectionCanceled;
    private bool subscribedToDialogueEvents;

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
            battleHUD.ShowActionMenu();
        }
        else
        {
            currentState = BattleState.EnemyTurn;
            battleHUD.HideActionMenu();
            battleHUD.HideCancelButton();
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
        target.SetHP(target.CurrentHP - damage);
        target.PlayHurt();

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitDamageDealt,
            ("unit", attackerName),
            ("amount", damage.ToString()),
            ("target", targetName)
        );

        ClearTargetIndicator();

        if (!target.IsAlive)
        {
            target.PlayDie();

            yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
                DialogueManager.Instance.Messages.unitDie,
                ("unit", targetName)
            );

            enemyBattleUnits.Remove(target);
            turnOrderManager.RemoveUnit(target);
            target.OnSelected -= OnEnemyTargetSelected;
            Destroy(target.gameObject);

            if (enemyBattleUnits.Count == 0)
            {
                currentState = BattleState.Win;
                yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.victory);
                yield break;
            }
        }

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
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
        foreach (UnitBattle enemy in enemyBattleUnits)
        {
            if (enemy.IsAlive)
                return enemy;
        }

        return null;
    }

    private int CalculateAttackDamage(UnitBattle attacker, UnitBattle target)
    {
        return Mathf.Max(1, attacker.Attack - target.Defense);
    }

    private void HandlePopupVisibilityChanged(bool isVisible)
    {
        foreach (UnitBattle playerUnit in playerBattleUnits)
        {
            if (playerUnit != null)
                playerUnit.gameObject.SetActive(!isVisible);
        }
    }

    private void SubscribeDialogueEvents()
    {
        if (subscribedToDialogueEvents || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.OnPopupVisibilityChanged += HandlePopupVisibilityChanged;
        subscribedToDialogueEvents = true;
    }

    #endregion

    #region Pass

    private IEnumerator HandlePassSequence()
    {
        string unitName = currentActingUnit != null ? currentActingUnit.name : "Unit";
        int recoveredHP = currentActingUnit != null ? currentActingUnit.RecoverHPPercent(0.1f) : 0;
        int recoveredMP = currentActingUnit != null ? currentActingUnit.RecoverMPPercent(0.1f) : 0;

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitPass,
            ("unit", unitName),
            ("hp", recoveredHP.ToString()),
            ("mp", recoveredMP.ToString())
        );

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    #endregion

    #region Defend

    private IEnumerator HandleDefendSequence()
    {
        string unitName = currentActingUnit != null ? currentActingUnit.name : "Unit";
        int recoveredMP = 0;

        if (currentActingUnit != null)
        {
            recoveredMP = currentActingUnit.RecoverMPPercent(0.2f);
            currentActingUnit.StartGuard();
        }

        yield return DialogueManager.Instance.ShowFormattedPopupAndWait(
            DialogueManager.Instance.Messages.unitGuard,
            ("unit", unitName),
            ("mp", recoveredMP.ToString())
        );

        turnOrderManager.CompleteCurrentTurn();
        ProcessNextTurn();
    }

    #endregion

    #region Flee

    private IEnumerator HandleFleeSequence()
    {
        yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.fleeAttempt);

        bool success = Random.value <= CalculateFleeChance();

        if (success)
        {
            yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.escapeSuccess);

            SceneManager.LoadScene("Gameplay");
        }
        else
        {
            yield return DialogueManager.Instance.ShowPopupAndWait(DialogueManager.Instance.Messages.escapeFailed);

            yield return new WaitForSeconds(1f);

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
