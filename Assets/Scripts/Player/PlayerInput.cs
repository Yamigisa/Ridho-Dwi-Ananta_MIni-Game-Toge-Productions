using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UnitMovement))]
public class PlayerInput : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;

    private UnitMovement movement;
    private Vector2 moveInput;

    private void Awake()
    {
        movement = GetComponent<UnitMovement>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();

        movement.Stop();
    }

    private void Update()
    {
        if (moveAction == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = moveAction.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        movement.MoveInDirection(moveInput);
    }
}
