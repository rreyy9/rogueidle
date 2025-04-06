using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Executes before PlayerController (-1) to ensure inputs are processed first
[DefaultExecutionOrder(-2)]
public class ThirdPersonInput : MonoBehaviour, PlayerControls.IThirdPersonMapActions
{
    public Vector2 ScrollInput { get; private set; }  // Mouse wheel or equivalent input

    [SerializeField] private CinemachineCamera _virtualCamera;  // Reference to the camera system
    [SerializeField] private float _cameraZoomSpeed;            // How fast the camera zooms in/out
    [SerializeField] private float _cameraMinZoom;              // Closest camera distance allowed
    [SerializeField] private float _cameraMaxZoom;              // Furthest camera distance allowed

    private CinemachineThirdPersonFollow _thirdPersonFollow;    // Reference to Cinemachine follow component

    // Initialize component references
    private void Awake()
    {
        _thirdPersonFollow = _virtualCamera.GetComponent<CinemachineThirdPersonFollow>();
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
        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Enable();
        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.SetCallbacks(this);
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
        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Disable();
        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.RemoveCallbacks(this);
    }

    // Apply zoom changes to camera every frame
    private void Update()
    {
        // Adjust camera distance based on scroll input and clamp between min/max values
        _thirdPersonFollow.CameraDistance = Mathf.Clamp(_thirdPersonFollow.CameraDistance + ScrollInput.y, _cameraMinZoom, _cameraMaxZoom);
    }

    // Reset single-frame input flags at the end of the frame
    private void LateUpdate()
    {
        ScrollInput = Vector2.zero;  // Reset scroll input so it doesn't continue zooming
    }

    // Callback for camera zoom/scroll input
    public void OnScrollCamera(InputAction.CallbackContext context)
    {
        // Only process on initial scroll, not continuously
        if (!context.performed)
            return;

        // Get scroll wheel input and apply zoom speed multiplier
        // Negative multiplier because scrolling up (positive Y) typically zooms in (smaller distance)
        Vector2 scrollInput = context.ReadValue<Vector2>();
        ScrollInput = -1f * scrollInput.normalized * _cameraZoomSpeed;
    }
}