using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

/// <summary>
/// A simple character controller for WASD movement in the X-Z plane.
/// This version uses the new Unity Input System.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))] // Ensure the PlayerInput component exists
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The movement speed of the player in meters per second.")]
    public float moveSpeed = 5f;

    [Header("Dependencies")]
    [Tooltip("(Optional) Assign an arbitrary transform to make movement relative to its local coordinate space.")]
    public Transform relativeTransform;

    private Rigidbody m_Rigidbody;
    private Vector2 m_MoveInput; // We now use a Vector2, as this is what the Input Action provides.

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used here to cache component references.
    /// </summary>
    private void Awake()
    {
        // Cache the Rigidbody component for performance and reliability.
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// This method is called by the PlayerInput component whenever the "Move" action is triggered.
    /// This event-driven approach is more efficient than polling for input every frame in Update().
    /// </summary>
    /// <param name="value">The InputValue containing the Vector2 from the input device.</param>
    public void OnMove(InputValue value)
    {
        // Read the Vector2 value from the input action and store it.
        m_MoveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// FixedUpdate is called every fixed framerate frame.
    /// All physics-related calculations and movements should be done here.
    /// </summary>
    private void FixedUpdate()
    {
        // Convert the 2D move input into a 3D vector for movement in the X-Z plane.
        Vector3 moveInput3D = new Vector3(m_MoveInput.x, 0f, m_MoveInput.y);
        Vector3 moveDirection = moveInput3D;

        // If a relative transform has been assigned, adjust the movement direction
        // to be relative to its orientation on the X-Z plane.
        if (relativeTransform != null)
        {
            Vector3 relativeForward = Vector3.Scale(relativeTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 relativeRight = relativeTransform.right;

            // Calculate the final movement direction by combining the transform's orientation
            // with the player's input.
            moveDirection = (relativeForward * moveInput3D.z) + (relativeRight * moveInput3D.x);
        }
        
        // Apply the movement to the Rigidbody.
        m_Rigidbody.MovePosition(m_Rigidbody.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
    }
}
