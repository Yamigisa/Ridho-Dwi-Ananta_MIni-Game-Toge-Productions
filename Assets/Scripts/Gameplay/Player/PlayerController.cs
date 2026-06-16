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
        movement.MoveInDirection(input.MoveInput);
    }

    private void Update()
    {
        if (input.InteractPressed)
            interactor.TryInteract();
    }

    private void OnDisable()
    {
        movement.Stop();
    }
}