using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitMovement))]
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

    private readonly HashSet<int> availableParameters = new();

    private UnitMovement movement;
    private Vector2 lastMoveDirection;

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
        movement = GetComponent<UnitMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        lastMoveDirection =
            startingDirection.sqrMagnitude > 0f
                ? startingDirection.normalized
                : Vector2.down;

        CacheParameterHashes();
        CacheAnimatorParameters();
        UpdateFacingParameters(lastMoveDirection);
    }

    private void Update()
    {
        if (animator == null)
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
        SetTrigger(attack);
    }

    public void PlayHurt()
    {
        SetTrigger(hurt);
    }

    public void PlayDeath()
    {
        SetTrigger(death);
    }

    public void PlayInteract()
    {
        SetTrigger(interact);
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
            availableParameters.Add(parameter.nameHash);
    }

    private void UpdateFacingParameters(Vector2 direction)
    {
        SetFloat(lastMoveX, direction.x);
        SetFloat(lastMoveY, direction.y);
    }

    private void SetFloat(int parameter, float value)
    {
        if (animator != null && availableParameters.Contains(parameter))
            animator.SetFloat(parameter, value);
    }

    private void SetBool(int parameter, bool value)
    {
        if (animator != null && availableParameters.Contains(parameter))
            animator.SetBool(parameter, value);
    }

    private void SetTrigger(int parameter)
    {
        if (animator != null && availableParameters.Contains(parameter))
            animator.SetTrigger(parameter);
    }
}
