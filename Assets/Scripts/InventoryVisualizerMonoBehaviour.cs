using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Visualizes a resource collector's inventory on a dynamic UI bar.
/// </summary>
public class InventoryVisualizerMonoBehaviour : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The specific resource collector this UI should display.")]
    public ResourceCollectorMonoBehaviour targetCollector;

    [Header("UI Setup")]
    [Tooltip("The parent RectTransform that holds the bar segments.")]
    public RectTransform barContainer;
    [Tooltip("The UI Image prefab used to represent a single segment of the bar.")]
    public GameObject segmentPrefab;
    [Tooltip("The data asset that maps resource types to colors.")]
    public ResourceColorMap colorMap;

    private void OnEnable()
    {
        // Subscribe to the inventory changed event when this component is enabled.
        ResourceManager.Instance.OnInventoryChanged += HandleInventoryChange;
        // Perform an initial draw.
        RedrawUI();
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when the component is disabled to prevent memory leaks.
        ResourceManager.Instance.OnInventoryChanged -= HandleInventoryChange;
    }

    /// <summary>
    /// The event handler that is called by the ResourceManager.
    /// </summary>
    private void HandleInventoryChange(IResourceReceiver collector)
    {
        // Only redraw the UI if the change affects our target collector.
        if (System.Object.ReferenceEquals(collector, targetCollector))
        {
            RedrawUI();
        }
    }

    /// <summary>
    /// Clears and redraws the entire inventory bar based on the collector's current state.
    /// </summary>
    private void RedrawUI()
    {
        if (targetCollector == null || barContainer == null || segmentPrefab == null || colorMap == null) return;

        // Clear any previously instantiated segment objects.
        foreach (Transform child in barContainer)
        {
            Destroy(child.gameObject);
        }

        // Get the capacity directly from the target collector.
        int totalCapacity = targetCollector.Capacity;
        if (totalCapacity <= 0) return; // Avoid division by zero if capacity is not set.

        // Get all possible resource types and sort them alphabetically to ensure a consistent order.
        var sortedResourceTypes = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().OrderBy(t => t.ToString());

        foreach (ResourceType type in sortedResourceTypes)
        {
            // Get current and allocated amounts from the manager.
            int currentAmount = ResourceManager.Instance.GetResourceAmount(targetCollector, type);
            int allocatedAmount = ResourceManager.Instance.GetAllocatedAmount(targetCollector, type);

            // Create the segment for the solid, confirmed resources.
            if (currentAmount > 0)
            {
                CreateSegment(currentAmount, colorMap.GetColor(type), totalCapacity);
            }

            // Create the semi-transparent segment for allocated resources.
            if (allocatedAmount > 0)
            {
                Color allocatedColor = colorMap.GetColor(type);
                allocatedColor.a = 0.5f; // Set alpha to 50%
                CreateSegment(allocatedAmount, allocatedColor, totalCapacity);
            }
        }
    }

    /// <summary>
    /// Instantiates and configures a single UI segment for the bar.
    /// </summary>
    private void CreateSegment(int amount, Color color, int capacity)
    {
        GameObject segmentGO = Instantiate(segmentPrefab, barContainer);
        segmentGO.GetComponent<Image>().color = color;

        // Set the width of the segment proportionally to the total capacity.
        LayoutElement layoutElement = segmentGO.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            // Calculate the width as a fraction of the container's total width.
            // We use preferredWidth to set a fixed size within the Horizontal Layout Group.
            float containerWidth = barContainer.rect.width;
            float segmentWidth = ((float)amount / capacity) * containerWidth;
            layoutElement.preferredWidth = segmentWidth;
        }
    }
}
