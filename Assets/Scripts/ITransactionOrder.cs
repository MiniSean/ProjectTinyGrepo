/// <summary>
/// Defines the contract for a resource transaction.
/// Provides methods to complete or cancel the transaction.
/// </summary>
public interface ITransactionOrder
{
    /// <summary>
    /// The collector who initiated this transaction.
    /// </summary>
    IResourceCollector Collector { get; }

    // /// <summary>
    // /// The Provider who provides for this transaction.
    // /// </summary>
    // IResourceExtraction Provider { get; }

    /// <summary>
    /// Boolean whether transaction is completed or canceled.
    /// </summary>
    public bool IsCompletedOrCanceled { get; }

    /// <summary>
    /// Finalizes the transaction, moving resources from allocated to inventory.
    /// </summary>
    void Complete();

    /// <summary>
    /// Cancels the transaction, releasing any allocated resources.
    /// </summary>
    void Cancel();
}
