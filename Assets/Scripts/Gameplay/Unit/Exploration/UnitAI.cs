using UnityEngine;

[RequireComponent(
    typeof(UnitMovement),
    typeof(BoxCollider2D),
    typeof(UnitExploration))]
[DisallowMultipleComponent]
public class UnitAI : MonoBehaviour
{
    private UnitExploration unitExploration;
    private UnitMovement movement;
    private UnitAIData aiData;

    private Vector2 wanderOrigin;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isWandering;
    private bool hasWanderTarget;
    private Transform playerTarget;
    private bool isChasing;

    private BoxCollider2D detectionCollider;

    private void Awake()
    {
        unitExploration = GetComponent<UnitExploration>();
        movement = GetComponent<UnitMovement>();
        detectionCollider = GetComponent<BoxCollider2D>();
        detectionCollider.isTrigger = true;
    }

    private void Start()
    {
        aiData = unitExploration.GetAIData();
        enabled = aiData != null;

        if (enabled)
            Initialize(aiData);
    }

    private void Update()
    {
        if (GameplayState.BlocksWorldSimulation)
        {
            movement.Stop();
            return;
        }

        if (isChasing)
        {
            if (!IsPlayerInRange())
                StopChasing();
            else
                HandleChase();
        }
        else
        {
            DetectPlayer();
            HandleWander();
        }
    }

    private void DetectPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, Vector2.one * aiData.detectRange, 0f);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            Vector2 directionToPlayer = (hit.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(transform.up, directionToPlayer);

            if (angle <= aiData.detectAngle / 2f)
                StartChasing(hit.transform);
        }
    }

    private bool IsPlayerInRange()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, Vector2.one * aiData.detectRange, 0f);
        foreach (Collider2D hit in hits)
            if (hit.CompareTag("Player")) return true;

        return false;
    }

    public void Initialize(UnitAIData data)
    {
        aiData = data;
        wanderOrigin = transform.position;
        wanderTimer = aiData.wanderInterval;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameplayState.BlocksWorldSimulation ||
            !other.CompareTag("Player"))
            return;

        Vector2 directionToPlayer = (other.transform.position - transform.position).normalized;
        float angle = Vector2.Angle(transform.up, directionToPlayer);

        if (angle <= aiData.detectAngle / 2f)
            StartChasing(other.transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            StopChasing();
    }

    private void HandleChase()
    {
        if (playerTarget == null)
        {
            StopChasing();
            return;
        }

        MoveToward(playerTarget.position);
    }

    private void HandleWander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            wanderTimer = aiData.wanderInterval + Random.Range(0f, aiData.continueWander);

            if (isWandering)
            {
                movement.Stop();
                isWandering = false;
                hasWanderTarget = false;
            }
            else
            {
                Vector2 randomOffset = Random.insideUnitCircle * aiData.wanderRange;
                wanderTarget = wanderOrigin + randomOffset;
                hasWanderTarget = true;
                MoveToward(wanderTarget);
                isWandering = true;
            }
        }

        if (isWandering && hasWanderTarget)
        {
            MoveToward(wanderTarget);

            Vector2 toDestination = wanderTarget - (Vector2)transform.position;
            if (toDestination.sqrMagnitude < 0.1f)
            {
                movement.Stop();
                isWandering = false;
                hasWanderTarget = false;
                wanderTimer = aiData.wanderInterval;
            }
        }
    }

    private void MoveToward(Vector2 target)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        movement.MoveInDirection(direction);
    }

    public void StartChasing(Transform target)
    {
        if (GameplayState.BlocksWorldSimulation)
        {
            movement.Stop();
            return;
        }

        playerTarget = target;
        isChasing = true;
        isWandering = false;
        hasWanderTarget = false;
        movement.Stop();
    }

    public void StopChasing()
    {
        playerTarget = null;
        isChasing = false;
        isWandering = false;
        hasWanderTarget = false;
        wanderTimer = aiData.wanderInterval;
        movement.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        if (aiData == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aiData.detectRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(wanderOrigin == Vector2.zero ? transform.position : wanderOrigin, aiData.wanderRange);
    }
}
