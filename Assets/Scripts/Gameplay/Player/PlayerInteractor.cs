using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Prompt")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string promptFormat = "Press E to {0}";

    private readonly List<Interactable> interactablesInRange = new();
    private Interactable currentTarget;
    private bool wasInteractionLocked;

    private void Awake()
    {
        if (promptText == null)
        {
            Debug.LogWarning(
                "PlayerInteractor has no interaction prompt text assigned.",
                this
            );
            return;
        }

        HidePrompt();
    }

    private void Update()
    {
        bool isLocked = IsInteractionLocked();

        if (isLocked == wasInteractionLocked)
            return;

        wasInteractionLocked = isLocked;

        if (isLocked)
            HidePrompt();
        else
            RefreshPrompt();
    }

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
        if (IsInteractionLocked())
            return;

        currentTarget?.Interact(gameObject);
    }

    private void RefreshTarget()
    {
        interactablesInRange.RemoveAll(interactable => interactable == null);

        currentTarget = interactablesInRange
            .OrderBy(i => (i.InteractionCenter - (Vector2)transform.position).sqrMagnitude)
            .FirstOrDefault();

        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (promptText == null)
            return;

        if (currentTarget == null || IsInteractionLocked())
        {
            HidePrompt();
            return;
        }

        promptText.text = string.Format(
            promptFormat,
            currentTarget.InteractionPrompt
        );
        promptText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private bool IsInteractionLocked()
    {
        bool dialogueIsPlaying =
            DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialoguePlaying;

        bool cutsceneIsPlaying =
            TimelineManager.Instance != null &&
            TimelineManager.Instance.IsCutscenePlaying;

        return dialogueIsPlaying || cutsceneIsPlaying;
    }
}
