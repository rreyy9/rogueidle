using UnityEngine;
using static PlayerState;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float locomotionBlendSpeed = 4f;

    private PlayerLocomotionInput _playerLocomotionInput;
    private PlayerState _playerState;
    private PlayerController _playerController;

    private static int inputXHash = Animator.StringToHash("inputX");
    private static int inputYHash = Animator.StringToHash("inputY");
    private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
    private static int isGroundedHash = Animator.StringToHash("isGrounded");
    private static int isIdlingHash = Animator.StringToHash("isIdling");
    private static int isFallingHash = Animator.StringToHash("isFalling");
    private static int isJumpingHash = Animator.StringToHash("isJumping");
    private static int isRotatingToTargetHash = Animator.StringToHash("isRotatingToTarget");
    private static int rotationMismatchHash = Animator.StringToHash("rotationMismatch");

    private Vector3 _currentBlendInput = Vector3.zero;

    public float _sprintMaxBlendValue = 1.5f;
    public float _runMaxBlendValue = 1.0f;
    public float _walkMaxBlendValue = 0.5f;

    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        _playerState = GetComponent<PlayerState>();
        _playerController = GetComponent<PlayerController>();
    }
    private void Update()
    {
        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        bool isIdling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Idling;
        bool isRunning = _playerState.CurrentPlayerMovementState == PlayerMovementState.Running;
        bool isSprinting = _playerState.CurrentPlayerMovementState == PlayerMovementState.Sprinting;
        bool isJumping = _playerState.CurrentPlayerMovementState == PlayerMovementState.Jumping;
        bool isFalling = _playerState.CurrentPlayerMovementState == PlayerMovementState.Falling;
        bool isGrounded = _playerState.InGroundedState();

        bool isRunBlendValue = isRunning || isJumping || isFalling;
        Vector2 inputTarget = isSprinting ? _playerLocomotionInput.MovementInput * _sprintMaxBlendValue :
                              isRunBlendValue ? _playerLocomotionInput.MovementInput * _runMaxBlendValue : _playerLocomotionInput.MovementInput * _walkMaxBlendValue;

        _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);

        _animator.SetBool(isGroundedHash, isGrounded);
        _animator.SetBool(isIdlingHash, isIdling);
        _animator.SetBool(isFallingHash, isFalling);
        _animator.SetBool(isJumpingHash, isJumping);
        _animator.SetBool(isRotatingToTargetHash, _playerController.IsRotatingToTarget);

        _animator.SetFloat(inputXHash, _currentBlendInput.x);
        _animator.SetFloat(inputYHash, _currentBlendInput.y);
        _animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);
        _animator.SetFloat(rotationMismatchHash, _playerController.RotationMismatch);
    }
}
