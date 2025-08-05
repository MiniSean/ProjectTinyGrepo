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
    [Tooltip("The visual representation for this level.")]
    public GameObject VisualPrefab;
    [Tooltip("The resource costs required to upgrade TO this level.")]
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

    [Header("Visuals")]
    [Tooltip("The parent transform under which the level visuals will be instantiated.")]
    [SerializeField]
    private Transform _visualsParent;
    [Tooltip("A particle effect prefab to instantiate when the building upgrades.")]
    [SerializeField]
    private ParticleSystem _upgradeEffectPrefab;

    #region Events
    /// <summary>
    /// Public event invoked when the component is successfully upgraded. The int is the new level.
    /// </summary>
    public event Action<int> OnUpgraded;
    /// <summary>
    /// Public event invoked when the underlying resource amounts for this component's requirements have changed.
    /// </summary>
    public event Action OnRequirementsUpdated;
    #endregion

    private ITransactionOrder m_ActiveTransaction;
    private GameObject _currentVisualInstance;

    #region Unity Lifecycle
    private void OnEnable()
    {
        // Subscribe to the global resource manager to know when any inventory changes.
        ResourceManager.Instance.OnInventoryChanged += HandleInventoryChange;
        // Set the initial visual state when the component is enabled.
        UpdateVisuals(playEffect: false);
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks.
        ResourceManager.Instance.OnInventoryChanged -= HandleInventoryChange;
    }
    #endregion

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

        // Update the visual representation to match the new level.
        UpdateVisuals(playEffect: true);
        // Invoke the level up event.
        OnUpgraded?.Invoke(_currentLevel);
        // An upgrade also implies requirements have changed for the next level.
        OnRequirementsUpdated?.Invoke();
    }

    #endregion

    #region IInteractionHandler Implemetnation
    /// <summary>
    /// Called when an IResourceProvider enters the trigger volume.
    /// return: true if transaction is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionStart(IResourceTrader interactor)
    {
        // This handler expects the interactor to be a provider.
        IResourceProvider provider = interactor as IResourceProvider;
        if (provider == null || m_ActiveTransaction != null) return false;

        IResourceRequirements _resourceRequirements = GetUpgradeRequirements();
        foreach (IResourceRequirement resourceRequirement in _resourceRequirements.Requirements)
        {
            if (ResourceManager.Instance.GetResourceAmount(interactor, resourceRequirement.ResourceType) > 0 && !resourceRequirement.IsFulfilled)
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
    public bool AttemptTransactionComplete(IResourceTrader interactor)
    {
        IResourceProvider provider = interactor as IResourceProvider;
        if (m_ActiveTransaction == null || provider != m_ActiveTransaction.Source) return false;

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
    public bool AttemptTransactionCancel(IResourceTrader interactor)
    {
        IResourceProvider provider = interactor as IResourceProvider;
        if (m_ActiveTransaction != null && provider == m_ActiveTransaction.Source)
        {
            m_ActiveTransaction.Cancel();
            m_ActiveTransaction = null;
            return true;
        }
        return false;
    }
    #endregion

    /// <summary>
    /// Destroys the current visual instance, plays an optional effect, and instantiates the correct visual for the current level.
    /// </summary>
    /// <param name="playEffect">Should the upgrade particle effect be played?</param>
    private void UpdateVisuals(bool playEffect)
    {
        Debug.Log("Updating visuals");
        // Ensure we have a parent to instantiate under.
        if (_visualsParent == null)
        {
            Debug.LogError("Visuals Parent transform is not assigned!", this);
            return;
        }

        // Destroy the previous visual if it exists.
        if (_currentVisualInstance != null)
        {
            Destroy(_currentVisualInstance);
        }

        // Play the upgrade effect if it's assigned and requested.
        if (playEffect && _upgradeEffectPrefab != null)
        {
            ParticleSystem effectInstance = Instantiate(_upgradeEffectPrefab, _visualsParent.position, _visualsParent.rotation);
            // Automatically destroy the particle system GameObject after its duration to prevent scene clutter.
            Destroy(effectInstance.gameObject, effectInstance.main.duration);
        }

        // Check if the current level is valid.
        if (_currentLevel < 0 || _currentLevel >= _upgradeLevels.Count)
        {
            Debug.LogWarning($"Current level ({_currentLevel}) is out of bounds for the defined visuals.", this);
            return;
        }

        // Get the prefab for the current level.
        GameObject prefabToInstantiate = _upgradeLevels[_currentLevel].VisualPrefab;

        if (prefabToInstantiate != null)
        {
            // Instantiate the new visual and store a reference to it.
            _currentVisualInstance = Instantiate(prefabToInstantiate, _visualsParent.position, _visualsParent.rotation, _visualsParent);
        }
    }

    /// <summary>
    /// Handles the OnInventoryChanged event from the ResourceManager.
    /// </summary>
    private void HandleInventoryChange(IResourceTrader changedTrader)
    {
        // If the inventory change affects this specific component,
        // broadcast our own, more specific event.
        if ((System.Object)changedTrader == this)
        {
            OnRequirementsUpdated?.Invoke();
        }
    }
}
