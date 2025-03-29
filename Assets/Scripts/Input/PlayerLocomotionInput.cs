using UnityEngine;
using UnityEngine.InputSystem;

// Executes before PlayerController (-1) to ensure inputs are processed first
[DefaultExecutionOrder(-2)]
public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions
{
    [SerializeField] private bool holdToSprint = true;  // Toggle between hold and toggle sprint modes

    // Public properties to access input states - read-only outside this class
    public Vector2 MovementInput { get; private set; }  // WASD or analog stick input
    public Vector2 LookInput { get; private set; }      // Mouse or right stick movement
    public bool JumpPressed { get; private set; }       // Jump button pressed
    public bool SprintToggledOn { get; private set; }   // Is sprint currently active
    public bool WalkToggledOn { get; private set; }     // Is walk currently active

    // Register input callbacks when component is enabled
    private void OnEnable()
    {
        // Check that input system is initialized
        if (PlayerInputManager.Instance?.PlayerControls == null)
        {
            Debug.LogError("Player controls is not initialized - cannot enable");
            return;
        }

        // Enable the locomotion action map and register this class as the callback handler
        PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.Enable();
        PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.SetCallbacks(this);
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

        // Disable the locomotion action map and remove callbacks
        PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.Disable();
        PlayerInputManager.Instance.PlayerControls.PlayerLocomotionMap.RemoveCallbacks(this);
    }

    // Reset single-frame input flags at the end of the frame
    private void LateUpdate()
    {
        JumpPressed = false;  // Reset jump flag so it only triggers once per press
    }

    // Callback for movement input (WASD/Left stick)
    public void OnMovement(InputAction.CallbackContext context)
    {
        // Read the 2D vector input (x,y) for movement
        MovementInput = context.ReadValue<Vector2>();
        print(MovementInput);
    }

    // Callback for look/camera input (Mouse/Right stick)
    public void OnLook(InputAction.CallbackContext context)
    {
        // Read the 2D vector input for camera rotation
        LookInput = context.ReadValue<Vector2>();
    }

    // Callback for sprint toggle input
    public void OnToggleSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Button pressed - handle both toggle and hold modes
            SprintToggledOn = holdToSprint || !SprintToggledOn;
        }
        else if (context.canceled)
        {
            // Button released - only relevant in hold mode
            SprintToggledOn = !holdToSprint && SprintToggledOn;
        }
    }

    // Callback for jump input
    public void OnJump(InputAction.CallbackContext context)
    {
        // Only trigger on initial press, not hold or release
        if (!context.performed)
            return;

        JumpPressed = true;
    }

    // Callback for walk toggle input
    public void OnToggleWalk(InputAction.CallbackContext context)
    {
        // Only trigger on initial press, not hold or release
        if (!context.performed)
            return;

        // Toggle walk state
        WalkToggledOn = !WalkToggledOn;
    }
}