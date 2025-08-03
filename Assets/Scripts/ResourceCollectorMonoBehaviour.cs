using UnityEngine;

/// <summary>
/// An implementation of the IResourceCollector interface. This component
/// allows the player to collect resources from extraction points.
/// </summary>
public class ResourceCollectorMonoBehaviour : MonoBehaviour, IResourceCollector
{
    [Header("Collector Settings")]
    [Tooltip("The total amount of resources this collector can hold.")]
    public int capacity = 10;

    // Interface implementation for Capacity.
    public int Capacity => capacity;

    private ITransactionOrder m_ActiveTransaction;

    /// <summary>
    /// Called when this collector enters the radius of a resource node.
    /// </summary>
    public bool OnExtractionOpportunity(IResourceExtraction resourceNode)
    {
        if (m_ActiveTransaction == null || m_ActiveTransaction.IsCompletedOrCanceled)
        {
            Debug.Log($"'{name}' has an opportunity to extract from an {resourceNode.Type} node.");

            // Request a transaction from the global manager.
            m_ActiveTransaction = ResourceManager.Instance.RequestExtraction(this, resourceNode, 1);
            bool wasApproved = m_ActiveTransaction != null;

            if (wasApproved)
            {
                // If approved, tell the visual component on the node to start its cooldown.
                // We need to cast the interface back to its MonoBehaviour type to access the method.
                if (resourceNode is ResourceExtractionMonoBehaviour nodeBehaviour)
                {
                    nodeBehaviour.BeginVisualCooldown(m_ActiveTransaction);
                }
            }

            return wasApproved;
        }
        return false;
    }

    /// <summary>
    /// Called when this collector exits the radius of a resource node.
    /// </summary>
    public void OnExtractionExit()
    {
        if (m_ActiveTransaction != null)
        {
            Debug.Log($"'{name}' opportunity lost. Cancelling active transaction.");
            // Cancel the active transaction. The ResourceManager will handle de-allocation.
            m_ActiveTransaction.Cancel();
            m_ActiveTransaction = null;
        }
    }
}
