using UnityEngine;

// Executes before all other input scripts to initialize the input system first
[DefaultExecutionOrder(-3)]
public class PlayerInputManager : MonoBehaviour
{
    // Singleton instance accessible globally
    public static PlayerInputManager Instance;

    // Reference to the generated input controls class (from Input System package)
    public PlayerControls PlayerControls { get; private set; }

    // Initialize the singleton pattern
    private void Awake()
    {
        // If another instance exists, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Set this as the singleton instance
        Instance = this;

        // Keep this GameObject between scene loads
        DontDestroyOnLoad(gameObject);
    }

    // Initialize the input system when component is enabled
    private void OnEnable()
    {
        // Create a new instance of the input controls
        PlayerControls = new PlayerControls();

        // Enable all input actions
        PlayerControls.Enable();
    }

    // Disable the input system when component is disabled
    private void OnDisable()
    {
        // Disable all input actions
        PlayerControls.Disable();
    }
}