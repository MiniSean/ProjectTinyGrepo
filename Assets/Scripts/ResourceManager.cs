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
    public event Action<IResourceTrader> OnInventoryChanged;

    // Main inventory: Stores the actual resources held by each collector.
    private readonly Dictionary<IResourceTrader, Dictionary<ResourceType, int>> _inventories = new Dictionary<IResourceTrader, Dictionary<ResourceType, int>>();

    // Allocated resources: Stores resources that are "in transit" during an extraction.
    private readonly Dictionary<IResourceTrader, Dictionary<ResourceType, int>> _allocated = new Dictionary<IResourceTrader, Dictionary<ResourceType, int>>();

    // List of transition orders
    private readonly List<ITransactionOrder> _activeTransactions = new List<ITransactionOrder>();

    // Private constructor to enforce the singleton pattern.
    private ResourceManager() { }

    /// <summary>
    /// A universal method to request any resource transaction between a provider and a collector.
    /// </summary>
    public ITransactionOrder RequestTransaction(IResourceProvider source, IResourceReceiver destination, ResourceType type, int amount)
    {
        if (!source.CanProvide(type, amount))
        {
            Debug.Log("ResourceManager: Transaction denied. Source cannot provide the requested amount.");
            return null;
        }

        // Check if the destination has capacity.
        if (!HasCapacity(destination, type, amount))
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
        RemoveResource(transaction.Source, transaction.ResourceType, transaction.Amount);
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

    private void AllocateResource(IResourceReceiver trader, ResourceType type, int amount)
    {
        if (!_allocated.ContainsKey(trader)) _allocated[trader] = new Dictionary<ResourceType, int>();
        if (!_allocated[trader].ContainsKey(type)) _allocated[trader][type] = 0;
        _allocated[trader][type] += amount;
        // Invoke the event to notify listeners of the change.
        OnInventoryChanged?.Invoke(trader);
    }

    private void DeallocateResource(IResourceReceiver destination, ResourceType type, int amount)
    {
        if (_allocated.ContainsKey(destination) && _allocated[destination].ContainsKey(type))
        {
            _allocated[destination][type] -= amount;
            // Invoke the event to notify listeners of the change.
            OnInventoryChanged?.Invoke(destination);
        }
    }

    public void RemoveResource(IResourceProvider trader, ResourceType type, int amount)
    {
        if (_inventories.ContainsKey(trader) && _inventories[trader].ContainsKey(type))
        {
            int currentAmount = _inventories[trader][type];
            if (amount > currentAmount)
            {
                Debug.LogWarning($"ResourceManager: Attempted to remove {amount} of {type}, but collector only has {currentAmount}.");
            }

            _inventories[trader][type] -= amount;
            OnInventoryChanged?.Invoke(trader);
        }
    }

    private void AddResource(IResourceReceiver trader, ResourceType type, int amount)
    {
        if (!_inventories.ContainsKey(trader)) _inventories[trader] = new Dictionary<ResourceType, int>();
        if (!_inventories[trader].ContainsKey(type)) _inventories[trader][type] = 0;
        _inventories[trader][type] += amount;
        // Invoke the event to notify listeners of the change.
        OnInventoryChanged?.Invoke(trader);
    }

    public int GetResourceAmount(IResourceTrader trader, ResourceType type)
    {
        if (_inventories.ContainsKey(trader) && _inventories[trader].ContainsKey(type))
        {
            return _inventories[trader][type];
        }
        return 0;
    }

    public int GetAllocatedAmount(IResourceReceiver trader, ResourceType type)
    {
        if (_allocated.ContainsKey(trader) && _allocated[trader].ContainsKey(type))
        {
            return _allocated[trader][type];
        }
        return 0;
    }

    public int GetTotalResourceAmount(IResourceTrader trader)
    {
        if (_inventories.ContainsKey(trader))
        {
            return _inventories[trader].Values.Sum();
        }
        return 0;
    }

    public int GetTotalAllocatedAmount(IResourceReceiver trader)
    {
        if (_allocated.ContainsKey(trader))
        {
            return _allocated[trader].Values.Sum();
        }
        return 0;
    }

    public bool HasCapacity(IResourceReceiver trader, ResourceType type, int amount)
    {
        int destHeld = GetTotalResourceAmount(trader);
        int destAllocated = GetTotalAllocatedAmount(trader);
        return destHeld + destAllocated + amount <= trader.Capacity;
    }
}
