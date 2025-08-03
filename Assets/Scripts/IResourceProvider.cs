/// <summary>
/// Defines the contract for any component that can provide resources.
/// </summary>
public interface IResourceProvider
{
    /// <summary>
    /// Checks if this provider can supply a certain amount of a resource.
    /// </summary>
    /// <param name="type">The type of resource to provide.</param>
    /// <param name="amount">The amount of the resource requested.</param>
    /// <returns>True if the resource can be provided, false otherwise.</returns>
    bool CanProvide(ResourceType type, int amount);

    /// <summary>
    /// Called by the ResourceManager to debit the provided resources from this provider's inventory.
    /// </summary>
    /// <param name="type">The type of resource to fulfill.</param>
    /// <param name="amount">The amount of the resource to fulfill.</param>
    void FulfillProvide(ResourceType type, int amount);
}
