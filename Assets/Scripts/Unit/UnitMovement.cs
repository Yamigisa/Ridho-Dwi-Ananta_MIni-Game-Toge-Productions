using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    public float MoveSpeed => moveSpeed;

    private float stoppingDistance = 0.05f;

    public Vector2 MoveDirection { get; private set; }

    private Rigidbody2D unitRigidbody;

    private Vector2 destination;

    private bool isMoving;
    public bool IsMoving => isMoving;

    public Vector2 Destination => destination;

    private void Awake()
    {
        unitRigidbody = GetComponent<Rigidbody2D>();
        destination = unitRigidbody.position;
    }

    public void MoveInDirection(Vector2 direction)
    {
        MoveDirection = Vector2.ClampMagnitude(direction, 1f);
        isMoving = MoveDirection.sqrMagnitude > 0f;
        unitRigidbody.linearVelocity = MoveDirection * moveSpeed;
    }

    public void Stop()
    {
        destination = unitRigidbody.position;
        isMoving = false;
        MoveDirection = Vector2.zero;
        unitRigidbody.linearVelocity = Vector2.zero;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void Move(float deltaTime)
    {
        if (!isMoving)
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
        isMoving = false;
        MoveDirection = Vector2.zero;
        unitRigidbody.MovePosition(destination);
        unitRigidbody.linearVelocity = Vector2.zero;
    }
}
