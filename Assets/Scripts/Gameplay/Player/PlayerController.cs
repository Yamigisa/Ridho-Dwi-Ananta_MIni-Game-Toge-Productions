using System;
using UnityEngine;

[RequireComponent(typeof(UnitMovement), typeof(PlayerInteractor))]
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Tooltip(
        "Optional component implementing IPlayerInputSource. " +
        "When empty, one is discovered on this GameObject.")]
    private MonoBehaviour inputSource;

    private IPlayerInputSource input;
    private UnitMovement movement;
    private PlayerInteractor interactor;
    private UnitPartyUI unitPartyUI;

    private void Awake()
    {
        input = ResolveInputSource();
        movement = GetComponent<UnitMovement>();
        interactor = GetComponent<PlayerInteractor>();
        unitPartyUI = FindFirstObjectByType<UnitPartyUI>(FindObjectsInactive.Include);
    }

    private void FixedUpdate()
    {
        if (InputIsLocked())
        {
            movement.SetSprinting(false);

            if (!movement.IsMovingToDestination)
                movement.Stop();

            return;
        }

        movement.SetSprinting(input.SprintHeld);
        movement.MoveInDirection(input.MoveInput);
    }

    private void Update()
    {
        if (GameplayInputIsLocked())
            return;

        HandleMenuInput();

        if (MenuIsOpen())
            return;

        if (input.InteractPressed)
            interactor.TryInteract();
    }

    private bool InputIsLocked()
    {
        return GameplayInputIsLocked() || MenuIsOpen();
    }

    private bool GameplayInputIsLocked()
    {
        return GameplayState.BlocksPlayerInput;
    }

    private bool MenuIsOpen()
    {
        return Inventory.Instance != null && Inventory.Instance.IsOpen ||
               GetUnitPartyUI() != null && unitPartyUI.IsOpen;
    }

    private void HandleMenuInput()
    {
        if (input.InventoryPressed)
            ToggleInventory();

        if (input.PartyPressed)
            TogglePartyUI();
    }

    private void ToggleInventory()
    {
        if (Inventory.Instance == null)
            return;

        UnitPartyUI partyUI = GetUnitPartyUI();
        if (partyUI != null && partyUI.IsOpen)
            partyUI.ClosePartyUI();

        Inventory.Instance.ToggleItemPanel();
    }

    private void TogglePartyUI()
    {
        UnitPartyUI partyUI = GetUnitPartyUI();
        if (partyUI == null)
            return;

        partyUI.TogglePartyUI();
    }

    private UnitPartyUI GetUnitPartyUI()
    {
        if (unitPartyUI == null)
            unitPartyUI = FindFirstObjectByType<UnitPartyUI>(FindObjectsInactive.Include);

        return unitPartyUI;
    }

    private IPlayerInputSource ResolveInputSource()
    {
        if (inputSource is IPlayerInputSource configuredInput)
            return configuredInput;

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour is IPlayerInputSource discoveredInput)
                return discoveredInput;
        }

        throw new InvalidOperationException(
            $"{name} requires a component implementing " +
            $"{nameof(IPlayerInputSource)}.");
    }

    private void OnDisable()
    {
        movement.SetSprinting(false);
        movement.Stop();
    }
}
