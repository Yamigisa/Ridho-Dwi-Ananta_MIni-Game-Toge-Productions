using UnityEngine;

[RequireComponent(typeof(PlayerInput), typeof(UnitMovement), typeof(PlayerInteractor))]
public class PlayerController : MonoBehaviour
{
    private PlayerInput input;
    private UnitMovement movement;
    private PlayerInteractor interactor;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        movement = GetComponent<UnitMovement>();
        interactor = GetComponent<PlayerInteractor>();
    }

    private void FixedUpdate()
    {
        if (InputIsLocked())
        {
            if (!movement.IsMovingToDestination)
                movement.Stop();

            return;
        }

        movement.MoveInDirection(input.MoveInput);
    }

    private void Update()
    {
        if (InputIsLocked())
            return;

        if (input.InteractPressed)
            interactor.TryInteract();
    }

    private bool InputIsLocked()
    {
        bool dialogueIsPlaying =
            DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialoguePlaying;

        bool cutsceneIsPlaying =
            NewTimelineManager.IsAnyCutscenePlaying;

        return dialogueIsPlaying || cutsceneIsPlaying;
    }

    private void OnDisable()
    {
        movement.Stop();
    }
}
