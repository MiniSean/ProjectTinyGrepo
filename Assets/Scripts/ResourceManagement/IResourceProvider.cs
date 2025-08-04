/// <summary>
/// Defines the contract for any component that can provide resources.
/// </summary>
public interface IResourceProvider: IResourceTrader
{
    /// <summary>
    /// Checks if this provider can supply a certain amount of a resource.
    /// </summary>
    /// <param name="type">The type of resource to provide.</param>
    /// <param name="amount">The amount of the resource requested.</param>
    /// <returns>True if the resource can be provided, false otherwise.</returns>
    bool CanProvide(ResourceType type, int amount);
}
