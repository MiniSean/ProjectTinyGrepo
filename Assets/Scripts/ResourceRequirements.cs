using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A concrete implementation of the <see cref="IResourceRequirement"/> interface.
/// This class represents a single, specific resource cost for an action, such as an upgrade.
/// It is context-aware, meaning it holds a reference to the <see cref="IResourceTrader"/>
/// whose inventory is being checked against the requirement.
/// </summary>
public class ResourceRequirement : IResourceRequirement
{
    private readonly IResourceTrader _contextualTrader;

    /// <inheritdoc/>
    public ResourceType ResourceType { get; }

    /// <inheritdoc/>
    public int TotalAmount { get; }

    /// <summary>
    /// Calculates the amount of this resource that is still needed to fulfill the requirement.
    /// The calculation is performed by querying the central <see cref="ResourceManager"/> for the
    /// trader's current inventory of the specified resource type.
    /// The mathematical formulation is:
    /// AmountMissing = max(0, TotalAmount - CurrentAmount)
    /// </summary>
    public int AmountMissing
    {
        get
        {
            // Query the singleton ResourceManager for the current inventory of the trader.
            int currentAmount = ResourceManager.Instance.GetResourceAmount(_contextualTrader, ResourceType);
            return Math.Max(0, TotalAmount - currentAmount);
        }
    }

    /// <summary>
    /// Determines if this specific requirement has been fulfilled.
    /// A requirement is considered fulfilled if the amount missing is zero.
    /// This corresponds to the logical condition: CurrentAmount >= TotalAmount.
    /// </summary>
    public bool IsFulfilled => AmountMissing <= 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRequirement"/> class.
    /// </summary>
    /// <param name="trader">The entity whose inventory will be checked. This provides the necessary context.</param>
    /// <param name="resourceType">The type of resource required.</param>
    /// <param name="totalAmount">The total amount of the resource required.</param>
    public ResourceRequirement(IResourceTrader trader, ResourceType resourceType, int totalAmount)
    {
        _contextualTrader = trader ?? throw new ArgumentNullException(nameof(trader), "A trader context must be provided to a resource requirement.");
        ResourceType = resourceType;
        TotalAmount = totalAmount;
    }
}


/// <summary>
/// A concrete implementation of the <see cref="IResourceRequirements"/> interface.
/// This class represents the complete set of resource costs for a specific action,
/// such as a single level upgrade. It aggregates multiple <see cref="IResourceRequirement"/> objects.
/// </summary>
public class ResourceRequirements : IResourceRequirements
{
    /// <inheritdoc/>
    public List<IResourceRequirement> Requirements { get; }

    // Note: The 'IsFulfilled' property is provided by the default interface implementation
    // in IResourceRequirements. It automatically evaluates to:
    // ∀r ∈ Requirements : r.IsFulfilled
    // which is implemented in C# as: Requirements.All(req => req.IsFulfilled)

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRequirements"/> class.
    /// </summary>
    /// <param name="trader">The entity whose inventory will be checked. This context is passed down to each individual requirement.</param>
    /// <param name="requiredResources">A dictionary mapping each required <see cref="ResourceType"/> to its total required amount.</param>
    public ResourceRequirements(IResourceTrader trader, Dictionary<ResourceType, int> requiredResources)
    {
        Requirements = new List<IResourceRequirement>();
        if (requiredResources != null)
        {
            foreach (var entry in requiredResources)
            {
                // For each entry in the dictionary, create a new context-aware ResourceRequirement.
                Requirements.Add(new ResourceRequirement(trader, entry.Key, entry.Value));
            }
        }
    }
}
