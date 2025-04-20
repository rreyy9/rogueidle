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
    public bool GatherPressed { get; set; }
    private bool _eKeyPressed = false; // Track if E is pressed, separate from animation

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
            Debug.LogError("Player controls is not initialized - cannot disable");
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

    private void LateUpdate()
    {
        // Make sure other input flags are reset properly too
        // This ensures the input is only processed once
        IsGatherKeyPressedThisFrame(); // Reset the flag
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
        if (context.performed)
        {
            _eKeyPressed = true; // E was just pressed
        }
        else if (context.canceled)
        {
            _eKeyPressed = false; // E was released
        }
    }

    public bool WasGatherKeyPressed()
    {
        if (_eKeyPressed)
        {
            _eKeyPressed = false; // Reset after checking
            return true;
        }
        return false;
    }

    public bool IsGatherKeyPressedThisFrame()
    {
        bool result = _eKeyPressed;
        _eKeyPressed = false; // Reset after checking
        return result;
    }
}