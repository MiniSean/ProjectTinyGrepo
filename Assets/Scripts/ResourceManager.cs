using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// A singleton that manages all resource inventories and transactions in the game.
/// </summary>
public class ResourceManager
{
    // Singleton instance
    private static ResourceManager _instance;
    public static ResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ResourceManager();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Event that is invoked whenever any collector's inventory or allocation changes.
    /// Listeners can subscribe to this to update UI or other game logic.
    /// </summary>
    public event Action<IResourceCollector> OnInventoryChanged;

    // Main inventory: Stores the actual resources held by each collector.
    private readonly Dictionary<IResourceCollector, Dictionary<ResourceType, int>> _inventories = new Dictionary<IResourceCollector, Dictionary<ResourceType, int>>();

    // Allocated resources: Stores resources that are "in transit" during an extraction.
    private readonly Dictionary<IResourceCollector, Dictionary<ResourceType, int>> _allocated = new Dictionary<IResourceCollector, Dictionary<ResourceType, int>>();

    // List of transition orders
    private readonly List<ITransactionOrder> _activeTransactions = new List<ITransactionOrder>();

    // Private constructor to enforce the singleton pattern.
    private ResourceManager() { }

    /// <summary>
    /// A universal method to request any resource transaction between a provider and a collector.
    /// </summary>
    public ITransactionOrder RequestTransaction(IResourceProvider source, IResourceCollector destination, ResourceType type, int amount)
    {
        if (!source.CanProvide(type, amount))
        {
            Debug.Log("ResourceManager: Transaction denied. Source cannot provide the requested amount.");
            return null;
        }

        // Check if the destination has capacity.
        int destHeld = GetTotalResourceAmount(destination);
        int destAllocated = GetTotalAllocatedAmount(destination);
        if (destHeld + destAllocated + amount > destination.Capacity)
        {
            Debug.Log("ResourceManager: Transaction denied. Destination has insufficient capacity.");
            return null;
        }

        // (Optional) Add checks for deposit-specific rules, e.g., if the deposit accepts this type.

        // All checks passed, create and approve the transaction.
        var transaction = new TransactionOrder(this, source, destination, type, amount);
        _activeTransactions.Add(transaction);
        AllocateResource(destination, type, amount);
        Debug.Log($"ResourceManager: Transaction approved from {source} to {destination}. Allocating {amount} {type}.");
        return transaction;
    }

    internal void CompleteTransaction(ITransactionOrder transaction)
    {
        if (!_activeTransactions.Contains(transaction)) return;

        DeallocateResource(transaction.Destination, transaction.ResourceType, transaction.Amount);
        AddResource(transaction.Destination, transaction.ResourceType, transaction.Amount);
        _activeTransactions.Remove(transaction);
        Debug.Log($"ResourceManager: Transaction complete. {transaction.Destination} received {transaction.Amount} {transaction.ResourceType}. Total: {GetResourceAmount(transaction.Destination, transaction.ResourceType)}");
    }

    internal void CancelTransaction(ITransactionOrder transaction)
    {
        if (!_activeTransactions.Contains(transaction)) return;

        DeallocateResource(transaction.Destination, transaction.ResourceType, transaction.Amount);
        _activeTransactions.Remove(transaction);
        Debug.Log($"ResourceManager: Transaction for {transaction.Amount} {transaction.ResourceType} by {transaction.Destination} was cancelled.");
    }

    private void AllocateResource(IResourceCollector destination, ResourceType type, int amount)
    {
        if (!_allocated.ContainsKey(destination)) _allocated[destination] = new Dictionary<ResourceType, int>();
        if (!_allocated[destination].ContainsKey(type)) _allocated[destination][type] = 0;
        _allocated[destination][type] += amount;
        // Invoke the event to notify listeners of the change.
        OnInventoryChanged?.Invoke(destination);
    }

    private void DeallocateResource(IResourceCollector destination, ResourceType type, int amount)
    {
        if (_allocated.ContainsKey(destination) && _allocated[destination].ContainsKey(type))
        {
            _allocated[destination][type] -= amount;
            // Invoke the event to notify listeners of the change.
            OnInventoryChanged?.Invoke(destination);
        }
    }

    public void RemoveResource(IResourceCollector collector, ResourceType type, int amount)
    {
        if (_inventories.ContainsKey(collector) && _inventories[collector].ContainsKey(type))
        {
            int currentAmount = _inventories[collector][type];
            if (amount > currentAmount)
            {
                Debug.LogWarning($"ResourceManager: Attempted to remove {amount} of {type}, but collector only has {currentAmount}. Clamping amount.");
                amount = currentAmount; // Clamp the amount to prevent negative inventory.
            }

            _inventories[collector][type] -= amount;
            OnInventoryChanged?.Invoke(collector);
        }
    }
    
    private void AddResource(IResourceCollector collector, ResourceType type, int amount)
    {
        if (!_inventories.ContainsKey(collector)) _inventories[collector] = new Dictionary<ResourceType, int>();
        if (!_inventories[collector].ContainsKey(type)) _inventories[collector][type] = 0;
        _inventories[collector][type] += amount;
        // Invoke the event to notify listeners of the change.
        OnInventoryChanged?.Invoke(collector);
    }

    public int GetResourceAmount(IResourceCollector collector, ResourceType type)
    {
        if (_inventories.ContainsKey(collector) && _inventories[collector].ContainsKey(type))
        {
            return _inventories[collector][type];
        }
        return 0;
    }

    public int GetAllocatedAmount(IResourceCollector collector, ResourceType type)
    {
        if (_allocated.ContainsKey(collector) && _allocated[collector].ContainsKey(type))
        {
            return _allocated[collector][type];
        }
        return 0;
    }
    
    public int GetTotalResourceAmount(IResourceCollector collector)
    {
        if (_inventories.ContainsKey(collector))
        {
            return _inventories[collector].Values.Sum();
        }
        return 0;
    }

    public int GetTotalAllocatedAmount(IResourceCollector collector)
    {
        if (_allocated.ContainsKey(collector))
        {
            return _allocated[collector].Values.Sum();
        }
        return 0;
    }
}
