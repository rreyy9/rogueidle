using UnityEngine;

// Static utility class for helper methods related to CharacterController
// Can be used from anywhere without needing an instance
public static class CharacterControllerUtils
{
    // Gets the normal vector of the surface below the character controller
    // Used for slope detection and wall sliding physics
    public static Vector3 GetNormalWithSphereCast(CharacterController characterController, LayerMask layerMask = default)
    {
        // Default normal is straight up (flat ground)
        Vector3 normal = Vector3.up;

        // Calculate the center of the character controller
        Vector3 center = characterController.transform.position + characterController.center;

        // Calculate how far to cast the sphere
        // Uses height/2 + stepOffset + a small buffer to detect ground reliably
        float distance = characterController.height / 2f + characterController.stepOffset + 0.01f;

        // Cast a sphere downward to detect ground
        RaycastHit hit;
        if (Physics.SphereCast(center, characterController.radius, Vector3.down, out hit, distance, layerMask))
        {
            // If ground is detected, get its surface normal
            normal = hit.normal;
        }

        // Return the normal vector (either detected surface or default up vector)
        return normal;
    }
}