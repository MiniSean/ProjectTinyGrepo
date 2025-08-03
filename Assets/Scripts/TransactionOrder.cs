/// <summary>
/// A concrete implementation of a resource transaction. This object holds all
/// the state for a single transaction and communicates back to the ResourceManager.
/// </summary>
public class TransactionOrder : ITransactionOrder
{
    private readonly ResourceManager m_Manager;
    public readonly IResourceCollector m_Collector;
    private readonly ResourceType m_ResourceType;
    private readonly int m_Amount;
    public bool IsCompletedOrCanceled { get; private set; }

    public IResourceCollector Collector => m_Collector;

    public TransactionOrder(ResourceManager manager, IResourceCollector collector, ResourceType resourceType, int amount)
    {
        m_Manager = manager;
        m_Collector = collector;
        m_ResourceType = resourceType;
        m_Amount = amount;
        IsCompletedOrCanceled = false;
    }

    public void Complete()
    {
        if (IsCompletedOrCanceled) return;
        m_Manager.CompleteTransaction(this, m_Collector, m_ResourceType, m_Amount);
        IsCompletedOrCanceled = true;
    }

    public void Cancel()
    {
        if (IsCompletedOrCanceled) return;
        m_Manager.CancelTransaction(this, m_Collector, m_ResourceType, m_Amount);
        IsCompletedOrCanceled = true;
    }
}
