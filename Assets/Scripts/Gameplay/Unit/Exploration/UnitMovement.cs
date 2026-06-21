using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMovement : MonoBehaviour
{
    [Header("IF Unit has no unit data, set default move speed")]
    [SerializeField, Min(0f)] private float moveSpeed = 10f;
    public float MoveSpeed => moveSpeed;

    private float stoppingDistance = 0.05f;

    public Vector2 MoveDirection { get; private set; }

    private Rigidbody2D unitRigidbody;

    private Vector2 destination;
    private Vector2 finalDestination;

    private bool isMoving;
    public bool IsMoving => isMoving;
    public bool IsMovingToDestination { get; private set; }

    public Vector2 Destination => destination;

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
        AdvanceDestinationMovement(Time.fixedDeltaTime);
    }

    public void MoveInDirection(Vector2 direction)
    {
        if (IsMovingToDestination)
            return;

        MoveDirection = Vector2.ClampMagnitude(direction, 1f);
        isMoving = MoveDirection.sqrMagnitude > 0f;
        unitRigidbody.linearVelocity = MoveDirection * moveSpeed;
    }

    public void Stop()
    {
        destination = unitRigidbody.position;
        finalDestination = destination;
        isMoving = false;
        IsMovingToDestination = false;
        isOnSecondMovement = false;
        MoveDirection = Vector2.zero;
        unitRigidbody.linearVelocity = Vector2.zero;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
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
            moveSpeed * deltaTime
        );

        unitRigidbody.MovePosition(nextPosition);
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
        unitRigidbody.linearVelocity = Vector2.zero;
    }
}
