using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference interactAction;

    public Vector2 MoveInput { get; private set; }
    public bool InteractPressed { get; private set; }

    private void OnEnable()
    {
        moveAction?.action?.Enable();
        interactAction?.action?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action?.Disable();
        interactAction?.action?.Disable();
    }

    private void Update()
    {
        MoveInput = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        InteractPressed = interactAction?.action?.WasPressedThisFrame() ?? false;
    }
}