using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unifies the concepts of being upgradable and receiving resources.
/// This interface describes an entity whose level can be increased by
/// fulfilling specific resource requirements for each level.
/// </summary>
public interface IResourceUpgradable : IUpgradable, IResourceReceiver
{
    /// <summary>
    /// Gets the specific resource requirements needed for the NEXT level upgrade.
    /// </summary>
    /// <returns>An IResourceRequirements object detailing the costs.</returns>
    IResourceRequirements GetUpgradeRequirements();

    /// <summary>
    /// Provides a default implementation for the CanUpgrade check.
    /// An entity can upgrade if it's not at max level AND all resource
    /// requirements for the next level are met.
    /// This is the mathematical conjunction: CanUpgrade ⇔ (Level < MaxLevel) ∧ (RequirementsFulfilled)
    /// </summary>
    bool IUpgradable.CanUpgrade()
    {
        if (CurrentLevel >= MaxLevel)
        {
            return false;
        }
        IResourceRequirements requirements = GetUpgradeRequirements();
        return requirements.IsFulfilled;
    }

    /// <summary>
    /// Provides a default implementation for the total resource capacity.
    /// The capacity is dynamically defined as the sum of the total amounts
    /// of all resources required for the next level upgrade.
    /// Capacity = Σ(r.TotalAmount) for all r in Requirements.
    /// </summary>
    int IResourceReceiver.Capacity => GetUpgradeRequirements()?.Requirements?.Sum(req => req.TotalAmount) ?? 0;

    /// <summary>
    /// Provides a default implementation for the CanReceive check.
    /// An upgradable entity can only receive a resource if that specific resource
    /// is required for the next upgrade, and only up to the amount that is missing.
    /// </summary>
    /// <param name="type">The type of resource to receive.</param>
    /// <param name="amount">The amount of the resource requested.</param>
    /// <returns>True if the resource type is needed and the amount does not exceed the missing quantity.</returns>
    bool IResourceReceiver.CanReceive(ResourceType type, int amount)
    {
        // Get the requirements for the next level.
        IResourceRequirements requirements = GetUpgradeRequirements();
        if (requirements?.Requirements == null)
        {
            // If there are no requirements, it cannot receive any resources for an upgrade.
            return false;
        }

        // Find the specific requirement for the resource type in question.
        var specificRequirement = requirements.Requirements.FirstOrDefault(req => req.ResourceType == type);

        // If this resource type is not required for the upgrade, it cannot be received.
        if (specificRequirement == null)
        {
            return false;
        }

        // The entity can receive the resource if the requested amount is less than or equal to what is still missing.
        // This effectively defines the "capacity" of the entity by its immediate needs.
        return amount <= specificRequirement.AmountMissing;
    }
}

/// <summary>
/// Interface for an object that can be upgraded.
/// </summary>
public interface IUpgradable
{
    int CurrentLevel { get; }
    int MaxLevel { get; }
    bool CanUpgrade();
    void Upgrade();
}

/// <summary>
/// Interface for defining specific resource requirement.
/// </summary>
public interface IResourceRequirement
{
    ResourceType ResourceType { get; }
    int TotalAmount { get; }
    int AmountMissing { get; }
    bool IsFulfilled { get; }
}

public interface IResourceRequirements
{
    List<IResourceRequirement> Requirements { get; }

    bool IsFulfilled => Requirements.All(req => req.IsFulfilled);
}

