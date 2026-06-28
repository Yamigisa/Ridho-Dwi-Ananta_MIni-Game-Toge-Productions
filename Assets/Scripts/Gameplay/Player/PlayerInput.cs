using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference inventoryAction;
    [SerializeField] private InputActionReference partyAction;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private InputActionReference sprintAction;

    public static event Action PauseRequested;

    public Vector2 MoveInput { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool InventoryPressed { get; private set; }
    public bool PartyPressed { get; private set; }
    public bool SprintHeld { get; private set; }

    private InputAction move;
    private InputAction interact;
    private InputAction inventory;
    private InputAction party;
    private InputAction pause;
    private InputAction sprint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeStatics()
    {
        PauseRequested = null;
    }

    private void OnEnable()
    {
        move = ResolveAction(moveAction, "Player/Move");
        interact = ResolveAction(interactAction, "Player/Interact");
        inventory = ResolveAction(inventoryAction, "Player/Inventory");
        party = ResolveAction(partyAction, "Player/Party");
        pause = ResolveAction(pauseAction, "Player/Pause");
        sprint = ResolveAction(sprintAction, "Player/Sprint");

        move?.Enable();
        interact?.Enable();
        inventory?.Enable();
        party?.Enable();
        pause?.Enable();
        sprint?.Enable();
    }

    private void OnDisable()
    {
        move?.Disable();
        interact?.Disable();
        inventory?.Disable();
        party?.Disable();
        pause?.Disable();
        sprint?.Disable();
    }

    private void Update()
    {
        if (pause?.WasPressedThisFrame() ?? false)
            PauseRequested?.Invoke();

        if (DialogueManager.IsGameplayInputLocked ||
            GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            MoveInput = Vector2.zero;
            InteractPressed = false;
            InventoryPressed = false;
            PartyPressed = false;
            SprintHeld = false;
            return;
        }

        MoveInput = move?.ReadValue<Vector2>() ?? Vector2.zero;
        InteractPressed = interact?.WasPressedThisFrame() ?? false;
        InventoryPressed = inventory?.WasPressedThisFrame() ?? false;
        PartyPressed = party?.WasPressedThisFrame() ?? false;
        SprintHeld = sprint?.IsPressed() ?? false;
    }

    private InputAction ResolveAction(InputActionReference actionReference, string fallbackPath)
    {
        if (actionReference != null && actionReference.action != null)
            return actionReference.action;

        InputActionAsset asset = GetInputActionAsset();

        return asset != null
            ? asset.FindAction(fallbackPath, false)
            : null;
    }

    private InputActionAsset GetInputActionAsset()
    {
        return moveAction?.action?.actionMap?.asset ??
               interactAction?.action?.actionMap?.asset ??
               inventoryAction?.action?.actionMap?.asset ??
               partyAction?.action?.actionMap?.asset ??
               pauseAction?.action?.actionMap?.asset ??
               sprintAction?.action?.actionMap?.asset;
    }
}
