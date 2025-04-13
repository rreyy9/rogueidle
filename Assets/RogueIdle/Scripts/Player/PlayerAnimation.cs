using System.Linq;
using UnityEngine;
using static PlayerState;

// Handles updating animation parameters based on player state
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator;             // Reference to the player model's animator
    [SerializeField] private float locomotionBlendSpeed = 4f; // How quickly animation transitions between states

    private PlayerLocomotionInput _playerLocomotionInput;    // Reference to input component
    private PlayerState _playerState;                         // Reference to state component
    private PlayerController _playerController;               // Reference to controller component
    private PlayerActionsInput _playerActionsInput;

    // Hash IDs for animator parameters - improves performance by avoiding string lookups
    private static int inputXHash = Animator.StringToHash("inputX");                  // Horizontal input
    private static int inputYHash = Animator.StringToHash("inputY");                  // Vertical input
    private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");  // Overall input strength
    private static int isGroundedHash = Animator.StringToHash("isGrounded");          // Is player on ground
    private static int isIdlingHash = Animator.StringToHash("isIdling");              // Is player standing still
    private static int isFallingHash = Animator.StringToHash("isFalling");            // Is player falling
    private static int isJumpingHash = Animator.StringToHash("isJumping");            // Is player jumping

    //Camera
    private static int isRotatingToTargetHash = Animator.StringToHash("isRotatingToTarget");  // Is player rotating to target
    private static int rotationMismatchHash = Animator.StringToHash("rotationMismatch");      // Angle difference with camera

    //Actions
    private static int isAttackingHash = Animator.StringToHash("isAttacking");
    private static int isGatheringHash = Animator.StringToHash("isGathering");
    private static int isPlayingActionHash = Animator.StringToHash("isPlayingAction");
    private int[] actionHashes;

    private Vector3 _currentBlendInput = Vector3.zero;  // Smoothed input values for animation blending

    // Maximum blend values for different movement states
    public float _sprintMaxBlendValue = 1.5f;  // Sprinting animation intensity
    public float _runMaxBlendValue = 1.0f;     // Running animation intensity 
    public float _walkMaxBlendValue = 0.5f;    // Walking animation intensity

    // Initialize component references
    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        _playerState = GetComponent<PlayerState>();
        _playerController = GetComponent<PlayerController>();
        _playerActionsInput = GetComponent<PlayerActionsInput>();

        actionHashes = new int[] { isGatheringHash };
    }

    // Update animation parameters every frame
    private void Update()
    {
        UpdateAnimationState();
    }

    // Updates all animation parameters based on current player state and input
    private void UpdateAnimationState()
    {
        // Get current player states
        bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
        bool isRunning = _playerState.CurrentPlayerMovementState == PlayerMovementState.Running;
        bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
        bool isJumping = _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping;
        bool isFalling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;
        bool isGrounded = _playerState.InGroundedState();
        bool isPlayingAction = actionHashes.Any(hash => _animator.GetBool(hash));

        // Determine movement blend values based on state
        bool isRunBlendValue = isRunning || isJumping || isFalling;

        // Calculate target input values with appropriate intensity multiplier
        Vector2 inputTarget = isSprinting ? _playerLocomotionInput.MovementInput * _sprintMaxBlendValue :
                              isRunBlendValue ? _playerLocomotionInput.MovementInput * _runMaxBlendValue :
                              _playerLocomotionInput.MovementInput * _walkMaxBlendValue;

        // Smoothly blend between current and target input values
        _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);

        // Set boolean parameters for state machine transitions
        _animator.SetBool(isGroundedHash, isGrounded);
        _animator.SetBool(isIdlingHash, isIdling);
        _animator.SetBool(isFallingHash, isFalling);
        _animator.SetBool(isJumpingHash, isJumping);
        _animator.SetBool(isRotatingToTargetHash, _playerController.IsRotatingToTarget);
        _animator.SetBool(isAttackingHash, _playerActionsInput.AttackPressed);
        _animator.SetBool(isGatheringHash, _playerActionsInput.GatherPressed);
        _animator.SetBool(isPlayingActionHash, isPlayingAction);

        // Set float parameters for blend trees
        _animator.SetFloat(inputXHash, _currentBlendInput.x);          // Horizontal input
        _animator.SetFloat(inputYHash, _currentBlendInput.y);          // Vertical input  
        _animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);  // Overall input strength
        _animator.SetFloat(rotationMismatchHash, _playerController.RotationMismatch);  // Rotation difference
    }
}