using UnityEngine;
using static PlayerState;

// This execution order ensures PlayerController runs before most other scripts
// but after PlayerLocomotionInput & PlayerInputManager
[DefaultExecutionOrder(-1)]
public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CharacterController _characterController;  // Unity's built-in character movement component
    [SerializeField] private Camera _playerCamera;                      // Reference to the player's camera
    public float RotationMismatch { get; private set; } = 0f;           // Angle difference between camera and player facing direction
    public bool IsRotatingToTarget { get; private set; } = false;       // Indicates if player is currently rotating to match camera direction

    [Header("Base Movement")]
    public float walkAcceleration = 0.15f;        // How quickly player accelerates to walk speed
    public float walkSpeed = 2f;                  // Maximum walking speed
    public float runAcceleration = 50f;           // How quickly player accelerates to run speed
    public float runSpeed = 6f;                   // Maximum running speed
    public float drag = 20f;                      // Resistance applied to movement (slows player down when not actively moving)
    public float movingThreshold = 0.01f;         // Minimum speed to be considered "moving"
    public float sprintAcceleration = 0.5f;       // How quickly player accelerates to sprint speed
    public float sprintSpeed = 9f;                // Maximum sprinting speed
    public float gravity = 25f;                   // Gravity force applied to player
    public float jumpSpeed = 1.0f;                // Initial upward velocity when jumping
    public float inAirAcceleration = 0.15f;       // How quickly player accelerates while airborne
    public float terminalVelocity = 50f;          // Maximum falling speed

    [Header("Animation")]
    public float playerModelRotationSpeed = 10f;  // How quickly player model rotates to face movement direction
    public float rotatToTargetTime = 0.25f;       // Time taken to rotate player to target direction

    [Header("Camera Settings")]
    public float lookSenseH = 0.1f;               // Horizontal mouse sensitivity
    public float lookSenseV = 0.1f;               // Vertical mouse sensitivity
    public float lookLimitV = 89f;                // Maximum vertical camera angle (prevents camera flipping)

    [Header("Environment Details")]
    [SerializeField] private LayerMask _groundLayers;  // Layers that are considered "ground" for collision detection

    private PlayerLocomotionInput _playerLocomotionInput;  // Component handling player input
    private PlayerState _playerState;                      // Component tracking player state (running, walking, etc.)
    private Vector2 _cameraRotation = Vector2.zero;        // Current camera rotation angles
    private Vector2 _playerTargetRotation = Vector2.zero;  // Target rotation for player model

    private bool _jumpedLastFrame = false;         // Tracks if player jumped in the previous frame
    private bool _isRotatingClockwise = false;     // Direction of rotation when aligning player with camera
    private float _rotatingToTargetTimer = 0f;     // Timer for rotation to target
    private float _verticalVelocity = 0f;          // Current vertical velocity (for jumping/falling)
    private float _antiBump;                       // Prevents "bumping" on slopes
    private float _stepOffset;                     // Original step offset value

    private PlayerMovementState _lastMovementState = PlayerMovementState.Falling;  // Previous movement state

    // Initialize components and variables when object is created
    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        _playerState = GetComponent<PlayerState>();

        _antiBump = sprintSpeed;  // Anti-bump set to sprint speed to handle slope movement
        _stepOffset = _characterController.stepOffset;  // Store original step offset
    }

    // Main update loop - handles all movement processing
    private void Update()
    {
        UpdateMovementState();     // Determine current movement state (walking, running, etc.)
        HandleVerticalMovement();  // Process vertical movement (jumping, falling)
        HandleLateralMovement();   // Process horizontal movement
    }

    // Determines the current movement state based on player input and physics
    private void UpdateMovementState()
    {
        // Store the previous movement state for transition handling
        _lastMovementState = _playerState.CurrentPlayerMovementState;

        // Get current movement conditions
        bool canRun = CanRun();  // Check if player can run (based on input direction)
        bool isMovementInput = _playerLocomotionInput.MovementInput != Vector2.zero;  // Is player pressing movement keys
        bool isMovingLaterally = IsMovingLaterally();  // Is player actually moving horizontally
        bool isSprinting = _playerLocomotionInput.SprintToggledOn && isMovingLaterally;  // Is sprint active and moving
        bool isWalking = (isMovingLaterally && !canRun) || _playerLocomotionInput.WalkToggledOn;  // Is walk toggled or sideways movement
        bool isGrounded = IsGrounded();  // Is player touching the ground

        // Determine lateral movement state using conditional (ternary) operator chain
        PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking :
                                          isSprinting ? PlayerMovementState.Sprinting :
                                          isMovingLaterally || isMovementInput ? PlayerMovementState.Running : PlayerMovementState.Idling;

        // Set the movement state
        _playerState.SetPlayerMovementState(lateralState);

        // Handle airborne states (jumping or falling)
        if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y > 0f)
        {
            // Moving upward in the air = jumping
            _playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
            _jumpedLastFrame = false;
            _characterController.stepOffset = 0f;  // Disable step offset while in air to prevent floating
        }
        else if ((!isGrounded || _jumpedLastFrame) && _characterController.velocity.y <= 0f)
        {
            // Moving downward in the air = falling
            _playerState.SetPlayerMovementState(PlayerMovementState.Falling);
            _jumpedLastFrame = false;
            _characterController.stepOffset = 0f;  // Disable step offset while in air
        }
        else
        {
            // Restore step offset when grounded
            _characterController.stepOffset = _stepOffset;
        }
    }

    // Handles jumping and falling (vertical movement)
    private void HandleVerticalMovement()
    {
        bool isGrounded = _playerState.InGroundedState();

        // Apply gravity
        _verticalVelocity -= gravity * Time.deltaTime;

        // If grounded and moving down, apply a small downward force
        // This helps stick to the ground and detect slopes
        if (isGrounded && _verticalVelocity < 0)
            _verticalVelocity = -_antiBump;

        // Handle jump input
        if (_playerLocomotionInput.JumpPressed && isGrounded)
        {
            // Apply jump velocity using physics formula v = sqrt(2 * h * g)
            // Modified to use jumpSpeed as height multiplier
            _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
            _jumpedLastFrame = true;
        }

        // Handle transitions to ground state
        if (_playerState.IsStateGroundedState(_lastMovementState) && isGrounded)
        {
            // Apply a small upward force to help with step handling
            _verticalVelocity += _antiBump;
        }

        // Cap vertical velocity at terminal velocity
        if (Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity))
        {
            _verticalVelocity = -1 * Mathf.Abs(terminalVelocity);
        }
    }

    // Handles all horizontal movement based on player input
    private void HandleLateralMovement()
    {
        // Get current movement state
        bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
        bool isGrounded = _playerState.InGroundedState();
        bool isWalking = _playerState.CurrentPlayerMovementState == PlayerMovementState.Walking;

        // Determine acceleration and speed limits based on movement state
        float lateralAcceleration = !isGrounded ? inAirAcceleration :  // Lower acceleration in air
                                   isWalking ? walkAcceleration :       // Walking acceleration
                                   isSprinting ? sprintAcceleration : runAcceleration;  // Sprint or run acceleration

        float clampLateralAcceleration = !isGrounded ? sprintSpeed :  // Use sprint speed cap in air
                                        isWalking ? walkSpeed :        // Walk speed cap
                                        isSprinting ? sprintSpeed : runSpeed;  // Sprint or run speed cap

        // Calculate movement direction relative to camera orientation
        Vector3 cameraForwardXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
        Vector3 cameraRightXZ = new Vector3(_playerCamera.transform.right.x, 0f, _playerCamera.transform.right.z).normalized;
        Vector3 movementDirection = cameraRightXZ * _playerLocomotionInput.MovementInput.x + cameraForwardXZ * _playerLocomotionInput.MovementInput.y;

        // Calculate movement delta based on input, acceleration and time
        Vector3 movementDelta = movementDirection * lateralAcceleration * Time.deltaTime;
        Vector3 newVelocity = _characterController.velocity + movementDelta;

        // Apply drag to slow player down when not actively moving
        Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;

        // Clamp horizontal speed to max allowed for current movement state
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampLateralAcceleration);

        // Add vertical component back to velocity
        newVelocity.y += _verticalVelocity;

        // Handle wall sliding when in air
        newVelocity = !isGrounded ? HandleSteepWalls(newVelocity) : newVelocity;

        // Apply the calculated movement
        _characterController.Move(newVelocity * Time.deltaTime);
    }

    // Handles sliding down steep surfaces (prevents sticking to walls)
    private Vector3 HandleSteepWalls(Vector3 velocity)
    {
        // Get the normal of the surface below player
        Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
        float angle = Vector3.Angle(normal, Vector3.up);
        bool validAngle = angle <= _characterController.slopeLimit;

        // If slope is too steep and player is falling, project velocity along the slope
        if (!validAngle && _verticalVelocity < 0f)
            velocity = Vector3.ProjectOnPlane(velocity, normal);

        return velocity;
    }

    // Handle camera rotation after other updates
    private void LateUpdate()
    {
        UpdateCameraRotation();
    }

    // Updates camera rotation based on mouse input and handles player model rotation
    private void UpdateCameraRotation()
    {
        // Update camera rotation based on mouse input
        _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x;
        _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y, -lookLimitV, lookLimitV);

        // Update player target rotation
        _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _playerLocomotionInput.LookInput.x;

        // Rotation handling parameters
        float rotationTolerance = 90f;  // How much mismatch to allow before auto-rotating
        bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
        IsRotatingToTarget = _rotatingToTargetTimer > 0;

        // Handle player model rotation based on state
        if (!isIdling)
        {
            // When moving, rotate player to match movement direction
            RotatePlayerToTarget();
        }
        else if (Mathf.Abs(RotationMismatch) > rotationTolerance || IsRotatingToTarget)
        {
            // When idle and player is not facing camera direction, rotate to align with camera
            UpdateIdleRotation(rotationTolerance);
        }

        // Apply rotation to camera
        _playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);

        // Calculate the angle difference between camera and player facing direction
        Vector3 camForwardProjectedXZ = new Vector3(_playerCamera.transform.forward.x, 0f, _playerCamera.transform.forward.z).normalized;
        Vector3 crossProduct = Vector3.Cross(transform.forward, camForwardProjectedXZ);
        float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
        RotationMismatch = sign * Vector3.Angle(transform.forward, camForwardProjectedXZ);
    }

    // Handles player rotation when idle to match camera direction
    private void UpdateIdleRotation(float rotationTolerance)
    {
        // Start new rotation if mismatch is beyond tolerance
        if (Mathf.Abs(RotationMismatch) > rotationTolerance)
        {
            _rotatingToTargetTimer = rotatToTargetTime;
            _isRotatingClockwise = RotationMismatch > rotationTolerance;
        }
        _rotatingToTargetTimer -= Time.deltaTime;

        // Rotate player if rotation hasn't completed
        if (_isRotatingClockwise && RotationMismatch > 0f ||
            !_isRotatingClockwise && RotationMismatch < 0f)
        {
            RotatePlayerToTarget();
        }
    }

    // Rotates player model to target rotation (camera direction)
    private void RotatePlayerToTarget()
    {
        Quaternion targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);
    }

    // Checks if player is moving horizontally
    private bool IsMovingLaterally()
    {
        Vector3 lateralVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
        return lateralVelocity.magnitude > movingThreshold;
    }

    // Checks if player is touching the ground
    private bool IsGrounded()
    {
        // Use different ground detection based on current state to prevent jitter
        bool grounded = _playerState.InGroundedState() ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();
        return grounded;
    }

    // Ground check when already in a grounded state
    private bool IsGroundedWhileGrounded()
    {
        // Create a sphere at the player's feet
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _characterController.radius, transform.position.z);

        // Check if sphere overlaps with ground layers
        bool grounded = Physics.CheckSphere(spherePosition, _characterController.radius, _groundLayers, QueryTriggerInteraction.Ignore);
        return grounded;
    }

    // Ground check when in an airborne state (jumping/falling)
    private bool IsGroundedWhileAirborne()
    {
        // Get the normal of the surface below
        Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(_characterController, _groundLayers);
        float angle = Vector3.Angle(normal, Vector3.up);
        print(angle);
        bool validAngle = angle <= _characterController.slopeLimit;

        // Only consider grounded if touching valid slope
        return _characterController.isGrounded && validAngle;
    }

    // Determines if player can run based on input direction
    private bool CanRun()
    {
        // Player can only run when moving forward or at a 45 degree angle
        // This prevents running when moving directly sideways or backward
        return _playerLocomotionInput.MovementInput.y >= Mathf.Abs(_playerLocomotionInput.MovementInput.x);
    }
}