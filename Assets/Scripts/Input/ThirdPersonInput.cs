using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-2)]
public class ThirdPersonInput : MonoBehaviour, PlayerControls.IThirdPersonMapActions
{
    public Vector2 ScrollInput { get; private set; }

    [SerializeField] private CinemachineCamera _virtualCamera;
    [SerializeField] private float _cameraZoomSpeed;
    [SerializeField] private float _cameraMinZoom;
    [SerializeField] private float _cameraMaxZoom;

    private CinemachineThirdPersonFollow _thirdPersonFollow;

    private void Awake()
    {
        _thirdPersonFollow = _virtualCamera.GetComponent<CinemachineThirdPersonFollow>();
    }

    private void OnEnable()
    {
        if (PlayerInputManager.Instance?.PlayerControls == null)
        {
            Debug.LogError("Player controls is not initialized - cannot enable");
            return;
        }

        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Enable();
        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.SetCallbacks(this);
    }

    private void OnDisable()
    {
        if (PlayerInputManager.Instance?.PlayerControls == null)
        {
            Debug.LogError("Player controls is not initialized - cannot enable");
            return;
        }

        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.Disable();
        PlayerInputManager.Instance.PlayerControls.ThirdPersonMap.RemoveCallbacks(this);
    }

    private void Update()
    {
        _thirdPersonFollow.CameraDistance = Mathf.Clamp(_thirdPersonFollow.CameraDistance + ScrollInput.y, _cameraMinZoom, _cameraMaxZoom);
    }

    private void LateUpdate()
    {
        ScrollInput = Vector2.zero;
    }

    public void OnScrollCamera(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Vector2 scrollInput = context.ReadValue<Vector2>();
        ScrollInput = -1f * scrollInput.normalized * _cameraZoomSpeed;
    }
}
