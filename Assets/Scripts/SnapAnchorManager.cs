using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages a collection of SnapAnchors and determines the closest anchor
/// to a given world position, considering both a mathematical grid and custom overrides.
/// This is a plain C# class, not a MonoBehaviour, designed to be used as a data structure.
/// </summary>
[System.Serializable]
public class SnapAnchorManager
{
    [Tooltip("The spacing of the underlying mathematical grid used for default anchor points.")]
    public Vector3 gridSpacing = new Vector3(20f, 0f, 11.25f);

    [Tooltip("The normal vector of the plane on which the grid lies. Typically (0, 1, 0) for an X-Z plane.")]
    private Vector3 gridPlaneNormal = Vector3.up;

    [Tooltip("The default bounding box size for anchors created on the mathematical grid.")]
    public Vector3 defaultBoundingBox = new Vector3(18f, 20f, 10f);

    [Tooltip("A list of custom SnapAnchor points that override the mathematical grid.")]
    public List<SnapAnchor> customAnchors = new List<SnapAnchor>();

    /// <summary>
    /// Determines the single most relevant SnapAnchor for a given world position.
    /// It prioritizes the closest custom anchor over the calculated grid position.
    /// </summary>
    /// <param name="worldPosition">The world position to find the closest anchor for.</param>
    /// <returns>The closest SnapAnchor, either a custom one or a new one based on the grid.</returns>
    public SnapAnchor GetClosestAnchor(Vector3 worldPosition)
    {
        // This can now be implemented by calling the new, more general method.
        List<SnapAnchor> closest = GetNClosestAnchors(worldPosition, 1);
        return closest.Count > 0 ? closest[0] : null;
    }

    /// <summary>
    /// Finds the N closest SnapAnchors to a given world position.
    /// </summary>
    /// <param name="worldPosition">The world position to query from.</param>
    /// <param name="count">The number of closest anchors to return.</param>
    /// <returns>A list of SnapAnchors sorted by distance, with a maximum length of N.</returns>
    public List<SnapAnchor> GetNClosestAnchors(Vector3 worldPosition, int count)
    {
        // Candidate Gathering
        var allPotentialAnchors = new List<SnapAnchor>();

        // Add all predefined custom anchors to the list of candidates.
        allPotentialAnchors.AddRange(customAnchors);

        // Determine the central grid point and create a 3x3 neighborhood around it.
        // This ensures we have a good set of grid-based candidates to compare against.
        Vector3 centralGridPoint = new Vector3(
            Mathf.Round(worldPosition.x / gridSpacing.x) * gridSpacing.x,
            0,
            Mathf.Round(worldPosition.z / gridSpacing.z) * gridSpacing.z
        );

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector3 gridPos = centralGridPoint + new Vector3(x * gridSpacing.x, 0, z * gridSpacing.z);
                allPotentialAnchors.Add(new SnapAnchor
                {
                    position = gridPos,
                    boundingBox = defaultBoundingBox
                });
            }
        }

        // Calculate distances and Sort
        // Use LINQ to order the list of all potential anchors by their squared distance
        // to the worldPosition. This is more efficient than calculating the true distance.
        var sortedAnchors = allPotentialAnchors.OrderBy(anchor => (anchor.position - worldPosition).sqrMagnitude);

        // Selection
        // Take the top 'count' anchors from the sorted list and return them as a new list.
        return sortedAnchors.Take(count).ToList();
    }
}
