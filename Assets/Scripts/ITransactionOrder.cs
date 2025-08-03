/// <summary>
/// Defines the contract for a resource transaction.
/// Provides methods to complete or cancel the transaction and exposes all participants and details.
/// </summary>
public interface ITransactionOrder
{
    /// <summary>
    /// The provider that is the source of the resources for this transaction.
    /// </summary>
    IResourceProvider Source { get; }

    /// <summary>
    /// The collector that is the destination of the resources for this transaction.
    /// </summary>
    IResourceReceiver Destination { get; }

    /// <summary>
    /// The type of resource being transferred.
    /// </summary>
    ResourceType ResourceType { get; }

    /// <summary>
    /// The amount of resource being transferred.
    /// </summary>
    int Amount { get; }

    /// <summary>
    /// A flag indicating if the transaction has already been completed or canceled.
    /// </summary>
    bool IsCompletedOrCanceled { get; }

    /// <summary>
    /// Finalizes the transaction, moving resources from allocated to inventory.
    /// </summary>
    void Complete();

    /// <summary>
    /// Cancels the transaction, releasing any allocated resources.
    /// </summary>
    void Cancel();
}
