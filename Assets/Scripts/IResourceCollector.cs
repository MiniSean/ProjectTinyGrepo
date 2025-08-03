/// <summary>
/// Defines the contract for any component that can collect resources.
/// It provides a standardized way for resource points to communicate with collectors.
/// </summary>
public interface IResourceCollector
{
    /// <summary>
    /// The total resource capacity of this collector.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Notifies the collector that it is within range of a resource node.
    /// The collector is then responsible for initiating a transaction with the ResourceManager.
    /// </summary>
    /// <param name="resourceNode">A reference to the resource point that can be extracted from.</param>
    /// <returns>True if a new extraction transaction was successfully requested and approved.</returns>
    bool OnExtractionOpportunity(IResourceExtraction resourceNode);

    /// <summary>
    /// Notifies the collector that it has left the range of a resource node.
    /// </summary>
    void OnExtractionExit();
}