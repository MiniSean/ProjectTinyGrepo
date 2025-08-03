using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A ScriptableObject that creates a reusable data asset for mapping
/// ResourceType enums to specific colors.
/// To use this, right-click in your Project window and select Gameplay > Resource Color Map to create the data asset
/// </summary>
[CreateAssetMenu(fileName = "ResourceColorMap", menuName = "Gameplay/Resource Color Map")]
public class ResourceColorMap : ScriptableObject
{
    [System.Serializable]
    public struct ColorMapping
    {
        public ResourceType type;
        public Color color;
    }

    public List<ColorMapping> colorMappings;

    /// <summary>
    /// Gets the color associated with a specific resource type.
    /// </summary>
    /// <param name="type">The resource type to look up.</param>
    /// <returns>The corresponding color, or white if not found.</returns>
    public Color GetColor(ResourceType type)
    {
        foreach (var mapping in colorMappings)
        {
            if (mapping.type == type)
            {
                return mapping.color;
            }
        }
        return Color.white; // Default color
    }
}
