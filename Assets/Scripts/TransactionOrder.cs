/// <summary>
/// A concrete implementation of a resource transaction. This object holds all
/// the state for a single transaction and communicates back to the ResourceManager.
/// </summary>
public class TransactionOrder : ITransactionOrder
{
    private readonly ResourceManager m_Manager;
    public IResourceProvider Source { get; }
    public IResourceReceiver Destination { get; }
    private readonly ResourceType m_ResourceType;
    private readonly int m_Amount;
    public bool IsCompletedOrCanceled { get; private set; }

    public ResourceType ResourceType => m_ResourceType;
    public int Amount => m_Amount;

    public TransactionOrder(ResourceManager manager, IResourceProvider source, IResourceReceiver destination, ResourceType resourceType, int amount)
    {
        m_Manager = manager;
        Source = source;
        Destination = destination;
        m_ResourceType = resourceType;
        m_Amount = amount;
        IsCompletedOrCanceled = false;
    }

    public void Complete()
    {
        if (IsCompletedOrCanceled) return;
        m_Manager.CompleteTransaction(this);
        IsCompletedOrCanceled = true;
    }

    public void Cancel()
    {
        if (IsCompletedOrCanceled) return;
        m_Manager.CancelTransaction(this);
        IsCompletedOrCanceled = true;
    }
}
