using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMovement : MonoBehaviour
{
    [Header("IF Unit has no unit data, set default move speed")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.25f;
    [SerializeField, Min(0f)] private float acceleration = 18f;
    [SerializeField, Min(0f)] private float deceleration = 26f;
    [SerializeField, Range(0f, 1f)] private float inputDeadZone = 0.1f;
    [SerializeField, Min(0f)] private float stopSnapSpeed = 0.04f;
    [SerializeField, Min(1f)] private float sprintMultiplier = 1.6f;

    public float MoveSpeed => moveSpeed;
    public float SprintMultiplier => sprintMultiplier;
    public bool IsSprinting { get; private set; }

    private float stoppingDistance = 0.05f;

    public Vector2 MoveDirection { get; private set; }

    private Rigidbody2D unitRigidbody;

    private Vector2 destination;
    private Vector2 finalDestination;

    private bool isMoving;
    public bool IsMoving => isMoving;
    public bool IsMovingToDestination { get; private set; }

    public Vector2 Destination => destination;

    private Vector2 requestedMoveDirection;

    public enum MovementOrder
    {
        Direct,
        XThenY,
        YThenX
    }

    private MovementOrder movementOrder;
    private bool isOnSecondMovement;

    private void Awake()
    {
        unitRigidbody = GetComponent<Rigidbody2D>();
        destination = unitRigidbody.position;
        finalDestination = destination;
    }

    private void FixedUpdate()
    {
        if (IsMovingToDestination)
            AdvanceDestinationMovement(Time.fixedDeltaTime);
        else
            AdvanceFreeMovement(Time.fixedDeltaTime);
    }

    public void MoveInDirection(Vector2 direction)
    {
        if (IsMovingToDestination)
            return;

        requestedMoveDirection = GetProcessedInputDirection(direction);
    }

    public void Stop()
    {
        destination = unitRigidbody.position;
        finalDestination = destination;
        isMoving = false;
        IsMovingToDestination = false;
        isOnSecondMovement = false;
        MoveDirection = Vector2.zero;
        requestedMoveDirection = Vector2.zero;
        unitRigidbody.linearVelocity = Vector2.zero;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void SetSprintMultiplier(float multiplier)
    {
        sprintMultiplier = Mathf.Max(1f, multiplier);
    }

    public void Sprint()
    {
        SetSprinting(true);
    }

    public void StopSprinting()
    {
        SetSprinting(false);
    }

    public void SetSprinting(bool sprinting)
    {
        IsSprinting = sprinting;
    }

    public void SetMovementFeel(
        float acceleration,
        float deceleration,
        float inputDeadZone,
        float stopSnapSpeed)
    {
        this.acceleration = Mathf.Max(0f, acceleration);
        this.deceleration = Mathf.Max(0f, deceleration);
        this.inputDeadZone = Mathf.Clamp01(inputDeadZone);
        this.stopSnapSpeed = Mathf.Max(0f, stopSnapSpeed);
    }

    private Vector2 GetProcessedInputDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= inputDeadZone * inputDeadZone)
            return Vector2.zero;

        return Vector2.ClampMagnitude(direction, 1f);
    }

    private void AdvanceFreeMovement(float deltaTime)
    {
        Vector2 currentVelocity = unitRigidbody.linearVelocity;
        Vector2 targetVelocity = requestedMoveDirection * GetCurrentMoveSpeed();
        float rate = requestedMoveDirection.sqrMagnitude > 0f
            ? acceleration
            : deceleration;

        Vector2 nextVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            rate * deltaTime);

        if (requestedMoveDirection.sqrMagnitude <= 0f &&
            nextVelocity.sqrMagnitude <= stopSnapSpeed * stopSnapSpeed)
        {
            nextVelocity = Vector2.zero;
        }

        unitRigidbody.linearVelocity = nextVelocity;

        isMoving = nextVelocity.sqrMagnitude > stopSnapSpeed * stopSnapSpeed;
        MoveDirection = isMoving
            ? nextVelocity.normalized
            : Vector2.zero;
    }

    private void AdvanceDestinationMovement(float deltaTime)
    {
        if (!IsMovingToDestination)
            return;

        Vector2 currentPosition = unitRigidbody.position;
        Vector2 toDestination = destination - currentPosition;
        MoveDirection = toDestination.normalized;

        if (toDestination.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            Arrive();
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            destination,
            GetCurrentMoveSpeed() * deltaTime
        );

        unitRigidbody.MovePosition(nextPosition);
    }

    private float GetCurrentMoveSpeed()
    {
        return moveSpeed * (IsSprinting ? sprintMultiplier : 1f);
    }

    private void Arrive()
    {
        if (!isOnSecondMovement && movementOrder != MovementOrder.Direct)
        {
            isOnSecondMovement = true;
            destination = finalDestination;
            return;
        }

        isMoving = false;
        IsMovingToDestination = false;
        isOnSecondMovement = false;
        MoveDirection = Vector2.zero;
        requestedMoveDirection = Vector2.zero;
        unitRigidbody.MovePosition(destination);
        unitRigidbody.linearVelocity = Vector2.zero;
    }

    public void MoveTo(Vector2 target)
    {
        BeginMovement(target, target, MovementOrder.Direct);
    }

    public void Move(float x, float y)
    {
        Vector2 target = unitRigidbody.position + new Vector2(x, y);
        BeginMovement(target, target, MovementOrder.Direct);
    }

    public void MoveXThenY(float x, float y)
    {
        Vector2 start = unitRigidbody.position;
        Vector2 target = start + new Vector2(x, y);
        Vector2 firstDestination = new Vector2(target.x, start.y);

        BeginMovement(firstDestination, target, MovementOrder.XThenY);
    }

    public void MoveYThenX(float x, float y)
    {
        Vector2 start = unitRigidbody.position;
        Vector2 target = start + new Vector2(x, y);
        Vector2 firstDestination = new Vector2(start.x, target.y);

        BeginMovement(firstDestination, target, MovementOrder.YThenX);
    }

    public void BeginMovement(
        Vector2 firstDestination,
        Vector2 target,
        MovementOrder order)
    {
        destination = firstDestination;
        finalDestination = target;
        movementOrder = order;
        isOnSecondMovement = false;
        isMoving = true;
        IsMovingToDestination = true;
        requestedMoveDirection = Vector2.zero;
        unitRigidbody.linearVelocity = Vector2.zero;
    }
}
