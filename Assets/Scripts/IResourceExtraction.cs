/// <summary>
/// Defines the contract for any component that acts as a source of resources.
/// </summary>
public interface IResourceExtraction: IResourceProvider
{
    /// <summary>
    /// The type of resource this node provides.
    /// </summary>
    ResourceType Type { get; }

    /// <summary>
    /// Is this node currently active and available for extraction?
    /// </summary>
    bool IsExtractionAllowed { get; }
}
