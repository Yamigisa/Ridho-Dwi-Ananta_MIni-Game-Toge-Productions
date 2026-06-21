using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
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
    public event Action<GameObject> Interacted;

    public string InteractionPrompt => interactionPrompt;
    public Vector2 InteractionRange => interactionRange;
    public Vector2 InteractionCenter => (Vector2)transform.position + interactionOffset;

    private void Awake()
    {
        interactionCollider = GetComponent<BoxCollider2D>();
        ApplyColliderSettings();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;

            if (other.TryGetComponent<PlayerInteractor>(out var interactor))
                interactor.OnEnterRange(this);

            onPlayerEnterRange?.Invoke(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            if (other.TryGetComponent<PlayerInteractor>(out var interactor))
                interactor.OnExitRange(this);

            onPlayerExitRange?.Invoke(other.gameObject);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (!canInteract)
            return;

        onInteract?.Invoke(interactor);
        Interacted?.Invoke(interactor);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(InteractionCenter, interactionRange);
    }

    private void OnValidate()
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
