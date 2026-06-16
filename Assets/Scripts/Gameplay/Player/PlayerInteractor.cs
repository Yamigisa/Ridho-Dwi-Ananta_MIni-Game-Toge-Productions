using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    private readonly List<Interactable> interactablesInRange = new();
    private Interactable currentTarget;

    public void OnEnterRange(Interactable interactable)
    {
        if (!interactablesInRange.Contains(interactable))
            interactablesInRange.Add(interactable);

        RefreshTarget();
    }

    public void OnExitRange(Interactable interactable)
    {
        interactablesInRange.Remove(interactable);
        RefreshTarget();
    }

    public void TryInteract()
    {
        currentTarget?.Interact(gameObject);
    }

    private void RefreshTarget()
    {
        currentTarget = interactablesInRange
            .OrderBy(i => (i.InteractionCenter - (Vector2)transform.position).sqrMagnitude)
            .FirstOrDefault();
    }
}
