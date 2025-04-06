using UnityEngine;

// Tracks and manages the current movement state of the player
public class PlayerState : MonoBehaviour
{
    // Current movement state with SerializeField attribute to make it visible in inspector
    // but with private setter to ensure only this class can change it
    [field: SerializeField] public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idling;

    // Sets the player's current movement state
    // Used by PlayerController to update state based on physics and input
    public void SetPlayerMovementState(PlayerMovementState playerMovementState)
    {
        CurrentPlayerMovementState = playerMovementState;
    }

    // Checks if the player is in any grounded state (not jumping or falling)
    // Used frequently for movement and physics calculations
    public bool InGroundedState()
    {
        return IsStateGroundedState(CurrentPlayerMovementState);
    }

    // Checks if a specific movement state is considered "grounded"
    // This allows checking both current and past states
    public bool IsStateGroundedState(PlayerMovementState movementState)
    {
        // All states except Jumping and Falling are considered grounded
        return movementState == PlayerMovementState.Idling ||
                movementState == PlayerMovementState.Walking ||
                movementState == PlayerMovementState.Running ||
                movementState == PlayerMovementState.Sprinting;
    }

    // Enum defining all possible player movement states
    // Used throughout the system to determine appropriate physics, animations, and controls
    public enum PlayerMovementState
    {
        Idling = 0,     // Standing still
        Walking = 1,    // Moving slowly (toggled or while moving sideways/backwards)
        Running = 2,    // Standard movement speed
        Sprinting = 3,  // Fast movement (toggled)
        Jumping = 4,    // Moving upward in the air
        Falling = 5,    // Moving downward in the air
        Strafing = 6,   // Moving sideways (currently unused but defined for future use)
    }
}