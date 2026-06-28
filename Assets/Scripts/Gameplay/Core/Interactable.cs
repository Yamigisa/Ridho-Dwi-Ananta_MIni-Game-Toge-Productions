using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class Interactable : MonoBehaviour
{
    [SerializeField] private BoxCollider2D interactionCollider;

    [Header("Interaction Settings")]
    [SerializeField] private string interactionPrompt = "Interact";
    [SerializeField] private Vector2 interactionRange = new Vector2(1.5f, 1.5f);
    [SerializeField] private Vector2 interactionOffset;

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject> onPlayerEnterRange;
    [SerializeField] private UnityEvent<GameObject> onPlayerExitRange;
    [SerializeField] private UnityEvent<GameObject> onInteract;

    private bool canInteract = false;
    private GameObject playerInRange;
    private bool wasGameplayInputLocked;
    public event Action<GameObject> Interacted;

    protected bool CanInteract => canInteract;
    public string InteractionPrompt => interactionPrompt;
    public Vector2 InteractionRange => interactionRange;
    public Vector2 InteractionCenter => (Vector2)transform.position + interactionOffset;

    protected virtual void Awake()
    {
        interactionCollider = GetComponent<BoxCollider2D>();
        ApplyColliderSettings();
        wasGameplayInputLocked =
            GameplayState.BlocksPlayerInput;
    }

    private void Update()
    {
        bool isLocked = GameplayState.BlocksPlayerInput;
        if (isLocked == wasGameplayInputLocked)
            return;

        wasGameplayInputLocked = isLocked;

        if (playerInRange == null)
            return;

        if (isLocked)
            onPlayerExitRange?.Invoke(playerInRange);
        else
            onPlayerEnterRange?.Invoke(playerInRange);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            playerInRange = other.gameObject;

            if (other.TryGetComponent<PlayerInteractor>(out var interactor))
                interactor.OnEnterRange(this);

            if (!GameplayState.BlocksPlayerInput)
                onPlayerEnterRange?.Invoke(other.gameObject);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            playerInRange = null;
            if (other.TryGetComponent<PlayerInteractor>(out var interactor))
                interactor.OnExitRange(this);

            onPlayerExitRange?.Invoke(other.gameObject);
        }
    }

    public virtual void Interact(GameObject interactor)
    {
        if (!canInteract ||
            GameplayState.BlocksPlayerInput)
            return;

        onInteract?.Invoke(interactor);
        Interacted?.Invoke(interactor);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(InteractionCenter, interactionRange);
    }

    protected virtual void OnValidate()
    {
        interactionRange.x = Mathf.Max(0.01f, interactionRange.x);
        interactionRange.y = Mathf.Max(0.01f, interactionRange.y);
        interactionCollider = GetComponent<BoxCollider2D>();
        ApplyColliderSettings();
    }

    private void ApplyColliderSettings()
    {
        if (interactionCollider == null)
            return;

        interactionCollider.isTrigger = true;
        interactionCollider.size = interactionRange;
        interactionCollider.offset = interactionOffset;
    }
}
