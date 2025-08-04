/// <summary>
/// Defines the contract for any component that can receive resources.
/// </summary>
public interface IResourceReceiver: IResourceTrader
{
    /// <summary>
    /// The total resource capacity of this receiver.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Checks if this receiver can store a certain amount of a resource.
    /// </summary>
    /// <param name="type">The type of resource to receive.</param>
    /// <param name="amount">The amount of the resource requested.</param>
    /// <returns>True if the resource can be received, false otherwise.</returns>
    bool CanReceive(ResourceType type, int amount);
}
