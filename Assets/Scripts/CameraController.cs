using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CameraController : MonoBehaviour
{
    [Header("Target Tracking")]
    [Tooltip("The player or object the camera should track.")]
    public Transform playerTransform;

    [Header("Anchor Logic")]
    [Tooltip("The spacing of the underlying mathematical grid used for default anchor points.")]
    public Vector3 gridSpacing = new Vector3(6f, 0f, 6f);

    [Tooltip("The default bounding box size for anchors created on the mathematical grid.")]
    public Vector3 defaultBoundingBox = new Vector3(8f, 5f, 8f);

    [Tooltip("Custom snap-anchor points.")]
    public List<SnapAnchorMonoBehaviour> customAnchors;

    [Header("Camera Positioning")]
    [Tooltip("The distance the camera maintains from the anchor's center along its view direction.")]
    public float cameraDistance = 15f;

    [Tooltip("How quickly the camera moves to its new target position. Higher is faster.")]
    public float interpolationSpeed = 5f;

    [Tooltip("How quickly the camera adjusts its size to fit the new anchor. Higher is faster.")]
    public float sizeInterpolationSpeed = 5f;

    [Tooltip("How quickly the camera rotates to its new target orientation. Higher is faster.")]
    public float rotationInterpolationSpeed = 5f;

    // Private fields for managing state
    private Camera m_Camera;
    private SnapAnchorManager anchorManager = new SnapAnchorManager();
    private SnapAnchor m_CurrentAnchor;
    private List<SnapAnchor> m_NeighborAnchors;
    private Vector3 m_TargetCameraPosition;
    private float m_TargetOrthographicSize;
    private float m_TargetYRotation;

    private void Awake()
    {
        // Cache the camera component for performance.
        m_Camera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (playerTransform == null) return;

        // Perform an initial setup to prevent camera snapping on the first frame.
        UpdateAnchorManagerFromMonoBehaviours();
        m_CurrentAnchor = anchorManager.GetClosestAnchor(playerTransform.position);

        if (m_CurrentAnchor != null)
        {
            // Set initial targets for position, rotation, and size.
            m_TargetYRotation = m_CurrentAnchor.yRotation;
            m_TargetCameraPosition = CalculateTargetPosition(m_CurrentAnchor);
            m_TargetOrthographicSize = CalculateOrthographicSize(m_CurrentAnchor.boundingBox);

            // Snap the camera directly to its starting state.
            transform.position = m_TargetCameraPosition;
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, m_TargetYRotation, transform.eulerAngles.z);
            m_Camera.orthographicSize = m_TargetOrthographicSize;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null || anchorManager == null)
        {
            return;
        }

        // Ensure the manager is using the latest data from the scene.
        UpdateAnchorManagerFromMonoBehaviours();

        // Only search for a new anchor if the player is no longer within the current one.
        if (m_CurrentAnchor == null || !m_CurrentAnchor.IsWithin(playerTransform.position))
        {
            // Determine the correct new anchor for the player's current position.
            SnapAnchor newAnchor = anchorManager.GetClosestAnchor(playerTransform.position);

            // Check if a valid anchor was found and if it's different from the current one.
            if (newAnchor != null && (m_CurrentAnchor == null || m_CurrentAnchor.position != newAnchor.position))
            {
                m_CurrentAnchor = newAnchor;
                // A new anchor is active, so we calculate a new target position for the camera.
                m_TargetCameraPosition = CalculateTargetPosition(m_CurrentAnchor);
                m_TargetOrthographicSize = CalculateOrthographicSize(m_CurrentAnchor.boundingBox);
                m_TargetYRotation = m_CurrentAnchor.yRotation;
            }
        }

        // Find neighbors for gizmo visualization
        if (m_CurrentAnchor != null)
        {
            var closestAnchors = anchorManager.GetNClosestAnchors(m_CurrentAnchor.position, 5);
            m_NeighborAnchors = closestAnchors.Skip(1).ToList();
        }

        // Every frame, smoothly interpolate the camera's position towards its target.
        transform.position = Vector3.Lerp(transform.position, m_TargetCameraPosition, Time.deltaTime * interpolationSpeed);
        m_Camera.orthographicSize = Mathf.Lerp(m_Camera.orthographicSize, m_TargetOrthographicSize, Time.deltaTime * sizeInterpolationSpeed);
        // Smoothly interpolate the Y-axis rotation.
        float currentYRotation = Mathf.LerpAngle(transform.eulerAngles.y, m_TargetYRotation, Time.deltaTime * rotationInterpolationSpeed);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, currentYRotation, transform.eulerAngles.z);
    }

    /// <summary>
    /// Calculates the target position for the camera based on an anchor's data.
    /// </summary>
    private Vector3 CalculateTargetPosition(SnapAnchor anchor)
    {
        // The direction is based on the anchor's target rotation, not the camera's current rotation.
        Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x, anchor.yRotation, transform.eulerAngles.z);
        Vector3 direction = targetRotation * Vector3.forward;
        // Vector3 direction = transform.forward;
        return anchor.position - (direction * cameraDistance);
    }
    
    /// <summary>
    /// Calculates the required orthographic size to fit a given bounding box.
    /// </summary>
    private float CalculateOrthographicSize(Vector3 boundingBox)
    {
        // The orthographic size is half the vertical viewing height.
        float sizeForHeight = boundingBox.z / 2f;
        // The required height to fit the width depends on the camera's aspect ratio.
        float sizeForWidth = (boundingBox.x / m_Camera.aspect) / 2f;

        // Return the larger of the two values to ensure the entire box is visible.
        return Mathf.Max(sizeForHeight, sizeForWidth);
    }

    /// <summary>
    /// Synchronizes the SnapAnchorManager's internal list with the MonoBehaviour components assigned in the inspector.
    /// </summary>
    private void UpdateAnchorManagerFromMonoBehaviours()
    {
        if (anchorManager == null) return;

        // Clear the manager's current list of custom anchors.
        anchorManager.customAnchors.Clear();

        // If there are any assigned MonoBehaviour anchors, add their data to the manager's list.
        if (customAnchors != null)
        {
            anchorManager.defaultBoundingBox = defaultBoundingBox;
            anchorManager.gridSpacing = gridSpacing;

            foreach (var anchorMB in customAnchors)
            {
                if (anchorMB != null)
                {
                    // Add the pure data object from the MonoBehaviour wrapper.
                    anchorManager.customAnchors.Add(anchorMB.Anchor);
                }
            }
        }
    }

    /// <summary>
    /// Draws visual helpers in the Scene view to make setup easier.
    /// This will only be drawn when the camera object is selected.
    /// </summary>
    private void OnDrawGizmos()
    {
        SnapAnchor anchorToDraw = null;
        List<SnapAnchor> neighborsToDraw = null;

        // If the game is running, use the anchors calculated in LateUpdate.
        if (Application.isPlaying)
        {
            anchorToDraw = m_CurrentAnchor;
            neighborsToDraw = m_NeighborAnchors;
        }
        // If we are in the editor and not playing...
        else
        {
            // ...calculate the anchors directly for editor visualization.
            if (playerTransform != null && anchorManager != null)
            {
                // Ensure the manager is using the latest data from the scene.
                UpdateAnchorManagerFromMonoBehaviours();
                anchorToDraw = anchorManager.GetClosestAnchor(playerTransform.position);
                var closestAnchors = anchorManager.GetNClosestAnchors(anchorToDraw.position, 5);
                neighborsToDraw = closestAnchors.Skip(1).ToList();
            }
        }

        // --- Draw Neighbor Anchors ---
        if (neighborsToDraw != null)
        {
            foreach (var neighbor in neighborsToDraw)
            {
                // Use the static gizmo drawing method for consistency.
                SnapAnchorMonoBehaviour.DrawGizmo(neighbor, Color.grey);
            }
        }

        // --- Draw Current Anchor ---
        if (anchorToDraw != null)
        {
            // Use the static gizmo drawing method for consistency.
            SnapAnchorMonoBehaviour.DrawGizmo(anchorToDraw, Color.cyan);
        }
    }
}
