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

    private BattleState currentState;
    public BattleState CurrentState => currentState;

    private void Awake()
    {
        battleHUD.OnActionSelected += OnPlayerAction;
    }

    private void OnDestroy()
    {
        battleHUD.OnActionSelected -= OnPlayerAction;
    }

    private void Start()
    {
        if (BattleRelay.PlayerUnits == null || BattleRelay.EnemyUnits == null) return;

        battleHUD.HideActionMenu();

        playerBattleUnits = battleStations.SpawnPlayerUnits(BattleRelay.PlayerUnits);
        enemyBattleUnits = battleStations.SpawnEnemyUnits(BattleRelay.EnemyUnits);

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
        UnitBattle next = turnOrderManager.PeekNext();
        if (next == null) return;

        if (playerBattleUnits.Contains(next))
        {
            currentState = BattleState.PlayerTurn;
            currentActingUnit = next;
            currentActingUnit.ClearGuard();
            battleHUD.ShowActionMenu();
        }
        else
        {
            currentState = BattleState.EnemyTurn;
            battleHUD.HideActionMenu();
        }
    }

    public void OnPlayerAction(BattleAction action)
    {
        battleHUD.HideActionMenu();

        switch (action)
        {
            case BattleAction.Attack:
                // handle attack
                turnOrderManager.CompleteCurrentTurn();
                ProcessNextTurn();
                break;
            case BattleAction.Defend:
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
                StartCoroutine(HandlePassSequence());
                break;
            case BattleAction.Flee:
                StartCoroutine(HandleFleeSequence());
                break;
        }
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
