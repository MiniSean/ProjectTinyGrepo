using UnityEngine;

/// <summary>
/// An implementation of the IResourceCollector interface. This component
/// allows the player to collect resources from extraction points.
/// </summary>
public class ResourceCollectorMonoBehaviour : MonoBehaviour, IResourceReceiver, IResourceProvider
{
    [Header("Collector Settings")]
    [Tooltip("The total amount of resources this collector can hold.")]
    public int capacity = 10;

    // Interface implementation for Capacity.
    public int Capacity => capacity;

    public bool CanReceive(ResourceType type, int amount)
    {
        return ResourceManager.Instance.HasCapacity(this, type, amount);
    }

    public bool CanProvide(ResourceType type, int amount)
    {
        // Check the actual inventory to see if we have enough resources.
        return ResourceManager.Instance.GetResourceAmount(this, type) >= amount;
    }
}
