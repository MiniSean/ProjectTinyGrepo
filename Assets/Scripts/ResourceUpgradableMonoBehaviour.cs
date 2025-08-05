using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

#region Serializable Data Structures for Unity Inspector

/// <summary>
/// A serializable, data-only class representing a single resource cost.
/// This is used for configuration in the Unity Inspector.
/// </summary>
[Serializable]
public class UpgradeCost
{
    public ResourceType ResourceType;
    [Min(1)]
    public int Amount;
}

/// <summary>
/// A serializable, data-only class representing the resource costs for a single level.
/// This is used for configuration in the Unity Inspector.
/// </summary>
[Serializable]
public class LevelRequirements
{
    public List<UpgradeCost> Costs = new List<UpgradeCost>();
}

#endregion

/// <summary>
/// A MonoBehaviour that implements the IResourceUpgradable interface, allowing
/// any GameObject to become an upgradable entity driven by resource collection.
/// Upgrade costs for each level are configured via the Unity Inspector.
/// </summary>
public class ResourceUpgradableMonoBehaviour : MonoBehaviour, IResourceUpgradable, IInteractionHandler
{
    [Tooltip("The list of resource requirements for each level. Index 0 corresponds to the upgrade TO level 1, etc.")]
    [SerializeField]
    private List<LevelRequirements> _upgradeLevels = new List<LevelRequirements>();

    [Tooltip("The current level of this object. Can be set to a starting value.")]
    [SerializeField]
    private int _currentLevel = 0;

    /// <summary>
    /// Public event invoked when the component is successfully upgraded.
    /// </summary>
    public event Action<int> OnUpgraded;

    #region IResourceUpgradable Implementation

    public int CurrentLevel => _currentLevel;
    public int MaxLevel => _upgradeLevels.Count;

    // Note: Capacity, CanReceive, and CanUpgrade are all handled by the
    // default implementations in the IResourceUpgradable interface.

    /// <summary>
    /// Constructs the logical, context-aware resource requirements for the next level
    /// based on the data configured in the Inspector.
    /// </summary>
    public IResourceRequirements GetUpgradeRequirements()
    {
        // Check if we are already at max level.
        if (CurrentLevel >= MaxLevel)
        {
            // Return an empty set of requirements if max level is reached.
            return new ResourceRequirements(this, new Dictionary<ResourceType, int>());
        }

        // Get the serialized cost data for the next level.
        LevelRequirements nextLevelData = _upgradeLevels[CurrentLevel];
        var costs = nextLevelData.Costs.ToDictionary(cost => cost.ResourceType, cost => cost.Amount);

        // Create the logical, context-aware requirements object.
        return new ResourceRequirements(this, costs);
    }

    /// <summary>
    /// Executes the upgrade if all conditions are met.
    /// </summary>
    public void Upgrade()
    {
        // Use the default interface method to check if upgrading is possible.
        if (!((IUpgradable)this).CanUpgrade())
        {
            Debug.LogWarning($"Upgrade attempt failed on {gameObject.name}. Conditions not met.", this);
            return;
        }

        IResourceRequirements requirements = GetUpgradeRequirements();

        // This component is now responsible for orchestrating the consumption.
        // It iterates through the requirements and tells the ResourceManager to remove each one.
        foreach (var req in requirements.Requirements)
        {
            ResourceManager.Instance.RemoveResource(this, req.ResourceType, req.TotalAmount);
        }

        _currentLevel++;
        Debug.Log($"{gameObject.name} successfully upgraded to Level {CurrentLevel}!", this);
        OnUpgraded?.Invoke(_currentLevel);
    }

    #endregion

    private ITransactionOrder m_ActiveTransaction;

    /// <summary>
    /// Called when an IResourceProvider enters the trigger volume.
    /// return: true if transaction is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionStart(IResourceProvider provider)
    {
        if (provider == null || m_ActiveTransaction != null) return false;

        IResourceRequirements _resourceRequirements = GetUpgradeRequirements();
        foreach (IResourceRequirement resourceRequirement in _resourceRequirements.Requirements)
        {
            if (ResourceManager.Instance.GetResourceAmount(provider, resourceRequirement.ResourceType) > 0 && !resourceRequirement.IsFulfilled)
            {
                m_ActiveTransaction = ResourceManager.Instance.RequestTransaction(provider, this, resourceRequirement.ResourceType, 1);
                bool wasApproved = m_ActiveTransaction != null;
                if (wasApproved)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Called when interaction coroutine finishes.
    /// return: true if transaction completion is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionComplete(IResourceProvider provider)
    {
        if (m_ActiveTransaction == null || provider != m_ActiveTransaction.Source) return false;
        // The cycle is complete. Finalize the transaction.
        m_ActiveTransaction?.Complete();
        m_ActiveTransaction = null;

        if (GetUpgradeRequirements().IsFulfilled)
        {
            Upgrade();
        }

        return true;
    }

    /// <summary>
    /// Called when an IResourceProvider exits the trigger volume.
    /// return: true if transaction cancellation is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionCancel(IResourceProvider provider)
    {
        if (m_ActiveTransaction != null && provider == m_ActiveTransaction.Source)
        {
            m_ActiveTransaction.Cancel();
            m_ActiveTransaction = null;
            return true;
        }
        return false;
    }
}
