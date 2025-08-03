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
    private readonly List<TransactionOrder> _activeTransactions = new List<TransactionOrder>();

    // Private constructor to enforce the singleton pattern.
    private ResourceManager() { }

    /// <summary>
    /// A collector requests to start an extraction process.
    /// </summary>
    /// <param name="collector">The collector initiating the request.</param>
    /// <param name="node">The resource node to extract from.</param>
    /// <param name="amount">The amount of resource to be extracted.</param>
    /// <returns>True if the transaction is approved, false otherwise.</returns>
    public ITransactionOrder RequestExtraction(IResourceCollector collector, IResourceExtraction node, int amount)
    {
        if (!node.IsExtractionAllowed)
        {
            Debug.Log("ResourceManager: Transaction denied. Node is not active.");
            return null;
        }

        // Placeholder for future capacity checks.
        // int currentAmount = GetResourceAmount(collector, node.Type);
        // int allocatedAmount = GetAllocatedAmount(collector, node.Type);
        // if (currentAmount + allocatedAmount + amount > collector.Capacity) return false;

        // --- Capacity Check Logic ---
        int currentHeldAmount = GetTotalResourceAmount(collector);
        int currentAllocatedAmount = GetTotalAllocatedAmount(collector);

        if (currentHeldAmount + currentAllocatedAmount + amount > collector.Capacity)
        {
            Debug.Log($"ResourceManager: Transaction denied. Collector '{collector}' has insufficient capacity.");
            return null;
        }
        // --------------------------

        // Allocate the resource.
        var transaction = new TransactionOrder(this, collector, node.Type, amount);
        _activeTransactions.Add(transaction);
        AllocateResource(collector, node.Type, amount);
        Debug.Log($"ResourceManager: Transaction approved and created for {collector}. Allocating {amount} {node.Type}.");
        return transaction;
    }

    internal void CompleteTransaction(TransactionOrder transaction, IResourceCollector collector, ResourceType type, int amount)
    {
        if (!_activeTransactions.Contains(transaction)) return;

        DeallocateResource(collector, type, amount);
        AddResource(collector, type, amount);
        _activeTransactions.Remove(transaction);
        Debug.Log($"ResourceManager: Transaction complete. {collector} received {amount} {type}. Total: {GetResourceAmount(collector, type)}");
    }

    internal void CancelTransaction(TransactionOrder transaction, IResourceCollector collector, ResourceType type, int amount)
    {
        if (!_activeTransactions.Contains(transaction)) return;

        DeallocateResource(collector, type, amount);
        _activeTransactions.Remove(transaction);
        Debug.Log($"ResourceManager: Transaction for {amount} {type} by {collector} was cancelled.");
    }

    private void AllocateResource(IResourceCollector collector, ResourceType type, int amount)
    {
        if (!_allocated.ContainsKey(collector)) _allocated[collector] = new Dictionary<ResourceType, int>();
        if (!_allocated[collector].ContainsKey(type)) _allocated[collector][type] = 0;
        _allocated[collector][type] += amount;
        // Invoke the event to notify listeners of the change.
        OnInventoryChanged?.Invoke(collector);
    }

    private void DeallocateResource(IResourceCollector collector, ResourceType type, int amount)
    {
        if (_allocated.ContainsKey(collector) && _allocated[collector].ContainsKey(type))
        {
            _allocated[collector][type] -= amount;
            // Invoke the event to notify listeners of the change.
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
