using UnityEngine;

/// <summary>
/// A MonoBehaviour wrapper for a SnapAnchor data object. This allows for
/// the visual placement and configuration of custom anchor points in the scene.
/// </summary>
public class SnapAnchorMonoBehaviour : MonoBehaviour
{
    [Tooltip("The dimensions of the bounding box, centered on this object's position.")]
    public Vector3 boundingBox = new Vector3(5f, 2f, 5f);

    // This field is managed by the custom editor script to reflect the transform's state.
    [SerializeField]
    private Vector3 position;
    [SerializeField]
    private float yRotation;

    /// <summary>
    /// Public property to get a SnapAnchor instance based on this component's data.
    /// </summary>
    public SnapAnchor Anchor
    {
        get
        {
            return new SnapAnchor
            {
                // Always use the transform's current position.
                position = this.transform.position,
                boundingBox = this.boundingBox,
                yRotation = this.transform.eulerAngles.y
            };
        }
    }

    /// <summary>
    /// A static, reusable method to draw a standardized gizmo for any SnapAnchor.
    /// </summary>
    /// <param name="anchor">The SnapAnchor data to visualize.</param>
    /// <param name="color">The primary color for the gizmo.</param>
    public static void DrawGizmo(SnapAnchor anchor, Color color)
    {
        if (anchor == null) return;

        // --- Rotated Bounding Box Drawing ---
        // Store the original Gizmos matrix to restore it later.
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Create a new transformation matrix for the rotated cube.
        // This matrix combines the anchor's position and its Y-axis rotation.
        Gizmos.matrix = Matrix4x4.TRS(anchor.position, Quaternion.Euler(0, anchor.yRotation, 0), Vector3.one);

        // Set the color and draw the wire cube at the new matrix's origin.
        // The matrix handles the transformation into world space.
        Gizmos.color = color;
        Gizmos.DrawWireCube(Vector3.zero, anchor.boundingBox);

        // Restore the original matrix so as not to affect other Gizmo drawings.
        Gizmos.matrix = oldMatrix;
        // ------------------------------------

        // Draw a sphere at the center with a slightly brighter version of the color.
        Gizmos.color = color * 1.5f; // Make the center sphere brighter
        Gizmos.DrawSphere(anchor.position, 0.2f);
    }

    // This object draws its own gizmo representation in the scene view.
    private void OnDrawGizmos()
    {
        // Call the static drawing method to visualize this anchor.
        DrawGizmo(this.Anchor, Color.magenta);
    }
}
