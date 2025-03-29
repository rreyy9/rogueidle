using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerState;

// Executes before PlayerController (-1) to ensure inputs are processed first
[DefaultExecutionOrder(-2)]
public class PlayerActionsInput : MonoBehaviour, PlayerControls.IPlayerActionMapActions
{
    private PlayerLocomotionInput _playerLocomotionInput;
    private PlayerState _playerState;
    public bool AttackPressed { get; private set; }
    public bool GatherPressed { get; private set; }

    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        _playerState = GetComponent<PlayerState>();
    }

    // Register input callbacks when component is enabled
    private void OnEnable()
    {
        // Check that input system is initialized
        if (PlayerInputManager.Instance?.PlayerControls == null)
        {
            Debug.LogError("Player controls is not initialized - cannot enable");
            return;
        }

        // Enable the third person action map and register this class as the callback handler
        PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Enable();
        PlayerInputManager.Instance.PlayerControls.PlayerActionMap.SetCallbacks(this);
    }

    // Unregister input callbacks when component is disabled
    private void OnDisable()
    {
        // Check that input system is initialized
        if (PlayerInputManager.Instance?.PlayerControls == null)
        {
            Debug.LogError("Player controls is not initialized - cannot enable");
            return;
        }

        // Disable the third person action map and remove callbacks
        PlayerInputManager.Instance.PlayerControls.PlayerActionMap.Disable();
        PlayerInputManager.Instance.PlayerControls.PlayerActionMap.RemoveCallbacks(this);
    }

    private void Update()
    {
        if (_playerLocomotionInput.MovementInput != Vector2.zero ||
            _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping ||
            _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling)
        {
            GatherPressed = false;
        }
    }

    public void SetGatherPressedFalse()
    {
        GatherPressed = false;
    }

    public void SetAttackPressedFalse()
    {
        AttackPressed = false;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        AttackPressed = true;
    }

    public void OnGather(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        GatherPressed = true;
    }
}