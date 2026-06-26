using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Starting Direction")]
    [SerializeField] private Vector2 startingDirection = Vector2.down;

    [Header("Locomotion Parameters")]
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string lastMoveXParameter = "LastMoveX";
    [SerializeField] private string lastMoveYParameter = "LastMoveY";
    [SerializeField] private string isMovingParameter = "IsMoving";

    [Header("Action Trigger Parameters")]
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string hurtParameter = "Hurt";
    [SerializeField] private string deathParameter = "Death";
    [SerializeField] private string interactParameter = "Interact";

    private readonly Dictionary<int, AnimatorControllerParameterType> availableParameters = new();

    private UnitMovement movement;
    private UnitExploration unitExploration;
    private UnitBattle unitBattle;
    private Vector2 lastMoveDirection;
    private Coroutine attackBoolPulse;
    private Coroutine hurtBoolPulse;
    private Coroutine deathBoolPulse;
    private Coroutine interactBoolPulse;

    private int moveX;
    private int moveY;
    private int lastMoveX;
    private int lastMoveY;
    private int isMoving;
    private int attack;
    private int hurt;
    private int death;
    private int interact;

    private void Awake()
    {
        TryGetComponent(out movement);
        TryGetComponent(out unitExploration);
        TryGetComponent(out unitBattle);

        EnsureAnimator();

        lastMoveDirection =
            startingDirection.sqrMagnitude > 0f
                ? startingDirection.normalized
                : Vector2.down;

        CacheParameterHashes();
        ApplyUnitDataAnimatorController(unitExploration != null ? unitExploration.GetUnitData() : null);
        CacheAnimatorParameters();
        UpdateFacingParameters(lastMoveDirection);
    }

    private void Start()
    {
        ApplyBattleAnimatorController(unitBattle != null ? unitBattle.UnitData : null);
    }

    public void ApplyUnitDataAnimatorController(UnitData unitData)
    {
        ApplyAnimatorController(unitData != null ? unitData.explorationAnimator : null, false);
    }

    public void ApplyBattleAnimatorController(UnitData unitData)
    {
        ApplyAnimatorController(unitData != null ? unitData.battleData?.battleAnimator : null, true);
    }

    private void Update()
    {
        if (animator == null || movement == null)
            return;

        Vector2 moveDirection = movement.MoveDirection;
        bool isMoving = movement.IsMoving && moveDirection.sqrMagnitude > 0f;

        SetBool(this.isMoving, isMoving);

        if (!isMoving)
            return;

        moveDirection.Normalize();
        lastMoveDirection = moveDirection;

        SetFloat(moveX, moveDirection.x);
        SetFloat(moveY, moveDirection.y);
        UpdateFacingParameters(lastMoveDirection);
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
            return;

        lastMoveDirection = direction.normalized;
        UpdateFacingParameters(lastMoveDirection);
    }

    public void FaceUp()
    {
        SetFacingDirection(Vector2.up);
    }

    public void FaceDown()
    {
        SetFacingDirection(Vector2.down);
    }

    public void FaceLeft()
    {
        SetFacingDirection(Vector2.left);
    }

    public void FaceRight()
    {
        SetFacingDirection(Vector2.right);
    }

    public void PlayAttack()
    {
        SetActionTrigger(attack, ref attackBoolPulse);
    }

    public void PlayHurt()
    {
        SetActionTrigger(hurt, ref hurtBoolPulse);
    }

    public void PlayDeath()
    {
        SetActionTrigger(death, ref deathBoolPulse);
    }

    public void PlayDie()
    {
        PlayDeath();
    }

    public void PlayInteract()
    {
        SetActionTrigger(interact, ref interactBoolPulse);
    }

    private void EnsureAnimator()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void ApplyAnimatorController(RuntimeAnimatorController controller, bool replaceExisting)
    {
        if (controller == null)
            return;

        EnsureAnimator();

        if (animator == null)
            return;

        if (!replaceExisting && animator.runtimeAnimatorController != null)
            return;

        if (animator.runtimeAnimatorController != controller)
            animator.runtimeAnimatorController = controller;

        CacheAnimatorParameters();
        UpdateFacingParameters(lastMoveDirection);
    }

    private void CacheParameterHashes()
    {
        moveX = Animator.StringToHash(moveXParameter);
        moveY = Animator.StringToHash(moveYParameter);
        lastMoveX = Animator.StringToHash(lastMoveXParameter);
        lastMoveY = Animator.StringToHash(lastMoveYParameter);
        isMoving = Animator.StringToHash(isMovingParameter);
        attack = Animator.StringToHash(attackParameter);
        hurt = Animator.StringToHash(hurtParameter);
        death = Animator.StringToHash(deathParameter);
        interact = Animator.StringToHash(interactParameter);
    }

    private void CacheAnimatorParameters()
    {
        availableParameters.Clear();

        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            availableParameters[parameter.nameHash] = parameter.type;
    }

    private void UpdateFacingParameters(Vector2 direction)
    {
        SetFloat(lastMoveX, direction.x);
        SetFloat(lastMoveY, direction.y);
    }

    private void SetFloat(int parameter, float value)
    {
        if (animator != null &&
            availableParameters.TryGetValue(parameter, out AnimatorControllerParameterType parameterType) &&
            parameterType == AnimatorControllerParameterType.Float)
            animator.SetFloat(parameter, value);
    }

    private void SetBool(int parameter, bool value)
    {
        if (animator != null &&
            availableParameters.TryGetValue(parameter, out AnimatorControllerParameterType parameterType) &&
            parameterType == AnimatorControllerParameterType.Bool)
            animator.SetBool(parameter, value);
    }

    private void SetActionTrigger(int parameter, ref Coroutine boolPulse)
    {
        if (animator == null ||
            !availableParameters.TryGetValue(parameter, out AnimatorControllerParameterType parameterType))
            return;

        if (parameterType == AnimatorControllerParameterType.Trigger)
        {
            animator.SetTrigger(parameter);
            return;
        }

        if (parameterType == AnimatorControllerParameterType.Bool)
        {
            if (boolPulse != null)
                StopCoroutine(boolPulse);

            boolPulse = StartCoroutine(PulseBool(parameter));
        }
    }

    private IEnumerator PulseBool(int parameter)
    {
        animator.SetBool(parameter, true);
        yield return null;

        if (animator != null)
            animator.SetBool(parameter, false);
    }
}
