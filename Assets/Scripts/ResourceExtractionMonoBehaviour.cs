using UnityEngine;

/// <summary>
/// Manages a single resource extraction point. Implements the IResourceExtraction interface
/// and controls a visual effect to show extraction progress when told to by a collector.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ResourceExtractionMonoBehaviour : MonoBehaviour, IResourceExtraction, IInteractionHandler
{
    [Header("Extraction Settings")]
    [Tooltip("The type of resource this node provides.")]
    public ResourceType resourceType = ResourceType.Stone;

    [Tooltip("Can resources be extracted from this point?")]
    public bool isExtractionPossible = true;

    // The active transaction for this extraction point.
    private ITransactionOrder m_ActiveTransaction;

    #region IResourceExtraction Implementation

    public ResourceType Type => resourceType;
    public bool IsExtractionAllowed => isExtractionPossible;

    public bool CanProvide(ResourceType type, int amount)
    {
        // This node provides a specific type and is assumed to be infinite.
        return isExtractionPossible && type == this.Type;
    }

    #endregion

    #region IInteractionHandler Implemetnation
    /// <summary>
    /// Called when an IResourceProvider enters the trigger volume.
    /// return: true if transaction is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionStart(IResourceTrader interactor)
    {
        // This handler expects the interactor to be a provider.
        IResourceReceiver receiver = interactor as IResourceReceiver;
        if (receiver == null || m_ActiveTransaction != null || !isExtractionPossible) return false;

        // Request a transaction from this extraction point (the provider) to the receiver.
        m_ActiveTransaction = ResourceManager.Instance.RequestTransaction(this, receiver, this.Type, 1);
        
        // Return true if the transaction was approved by the manager.
        return m_ActiveTransaction != null;
    }

    /// <summary>
    /// Called when interaction coroutine finishes.
    /// return: true if transaction completion is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionComplete(IResourceTrader interactor)
    {
        IResourceReceiver receiver = interactor as IResourceReceiver;
        if (m_ActiveTransaction == null || receiver != m_ActiveTransaction.Destination) return false;

        m_ActiveTransaction?.Complete();
        m_ActiveTransaction = null;
        return true;
    }

    /// <summary>
    /// Called when an IResourceProvider exits the trigger volume.
    /// return: true if transaction cancellation is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionCancel(IResourceTrader interactor)
    {
        IResourceReceiver receiver = interactor as IResourceReceiver;
        if (m_ActiveTransaction != null && receiver == m_ActiveTransaction.Destination)
        {
            m_ActiveTransaction.Cancel();
            m_ActiveTransaction = null;
            return true;
        }
        return false;
    }
    #endregion
}