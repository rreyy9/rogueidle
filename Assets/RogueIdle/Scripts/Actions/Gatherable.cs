using System.Collections;
using UnityEngine;

public class Gatherable : MonoBehaviour
{
    private bool playerInRange = false;
    private GameObject player;
    private PlayerActionsInput playerInput;
    private Animator playerAnimator; // Reference to player's animator
    private bool isGathering = false; // Flag to track gathering state

    private void Start()
    {
        // Try to find the player in the scene
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerActionsInput>();
            playerAnimator = player.GetComponentInChildren<Animator>(); // Get animator from player or its children
        }
    }

    private void Update()
    {
        // Check if player is already gathering
        if (playerAnimator != null)
        {
            // Get the value from the animator - use the same parameter name that's in PlayerAnimation.cs
            isGathering = playerAnimator.GetBool("isGathering");
        }

        // Only allow gathering if not already in progress
        if (playerInRange && playerInput != null && !isGathering)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                playerInput.GatherPressed = true;
                //Debug.Log("Gathering from bush!");

                // Optional: Disable further gathering for this bush
                StartCoroutine(DisableGatheringTemporarily());
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            //Debug.Log("Player can now gather from this bush (press E)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            //Debug.Log("Player left gathering range");
        }
    }

    // Optional: Coroutine to disable gathering for a time after successful gather

    private IEnumerator DisableGatheringTemporarily()
    {
        // Disable the collider temporarily so player can't gather again
        Collider bushCollider = GetComponent<Collider>();
        if (bushCollider != null)
            bushCollider.enabled = false;

        // Wait for animation to complete (adjust time as needed)
        yield return new WaitForSeconds(2.233f);

        // Re-enable collider
        if (bushCollider != null)
            bushCollider.enabled = true;
    }
}