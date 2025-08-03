using UnityEngine;

/// <summary>
/// A data class representing a potential camera anchor point.
/// It contains a central position and a bounding box that defines its area of influence.
/// </summary>
[System.Serializable]
public class SnapAnchor
{
    [Tooltip("The central position of this anchor point in world space.")]
    public Vector3 position;

    [Tooltip("The dimensions of the bounding box, centered on the position.")]
    public Vector3 boundingBox;

    [Tooltip("The target rotation around the Y-axis for the camera at this anchor.")]
    public float yRotation;

    /// <summary>
    /// Checks if a given world space vector is within the bounding box of this anchor.
    /// </summary>
    /// <param name="v">The world space vector to check.</param>
    /// <returns>True if the vector is inside the bounding box, false otherwise.</returns>
    public bool IsWithin(Vector3 v)
    {
        // To check if a point is inside an Oriented Bounding Box (OBB), we transform
        // the point into the local coordinate space of the box.

        // Create the inverse rotation quaternion from the anchor's Y rotation.
        // This will effectively "un-rotate" the world space point back to the anchor's local orientation.
        Quaternion inverseRotation = Quaternion.Euler(0, -this.yRotation, 0);

        // Translate the point 'v' so it is relative to the anchor's position.
        Vector3 localPoint = v - this.position;

        // Apply the inverse rotation to the translated point.
        // The point is now in the anchor's local space, where the bounding box is axis-aligned.
        localPoint = inverseRotation * localPoint;

        // Calculate the half-dimensions of the bounding box for the AABB check.
        Vector3 halfBox = this.boundingBox / 2;

        // Perform a simple AABB check in the anchor's local space.
        // We use the absolute value of the local point's coordinates for a clean check against the origin.
        return (Mathf.Abs(localPoint.x) <= halfBox.x) &&
               (Mathf.Abs(localPoint.y) <= halfBox.y) &&
               (Mathf.Abs(localPoint.z) <= halfBox.z);
    }
}