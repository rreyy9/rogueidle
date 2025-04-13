using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the player's inventory including items, equipment, and persistence.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    #region Variables

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20;

    [Header("Equipment Settings")]
    [SerializeField] private int equipmentSlots = 6; // Based on ArmorSlot enum

    [Header("Persistence")]
    [SerializeField] private string saveFileName = "player_inventory.dat";
    [SerializeField] private bool showDebugMessages = true;

    // Main inventory storage
    private ItemInstance[] _inventorySlots;

    // Equipment slots (mapped to ArmorSlot enum)
    private ItemInstance[] _equippedItems;

    // Events for UI updates
    public event Action<int> OnSlotChanged;
    public event Action<ArmorItem.ArmorSlot> OnEquipmentChanged;
    public event Action OnInventoryChanged;

    // Singleton instance
    public static InventoryManager Instance { get; private set; }

    #endregion

    #region Initialization

    private void Awake()
    {
        InitializeSingleton();
        InitializeInventory();
        LoadInventory();
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeInventory()
    {
        _inventorySlots = new ItemInstance[inventorySize];
        _equippedItems = new ItemInstance[equipmentSlots];
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    #endregion

    #region Core Inventory Methods

    /// <summary>
    /// Adds an item to the inventory
    /// </summary>
    /// <param name="itemToAdd">The item instance to add</param>
    /// <returns>True if successfully added, false if inventory is full</returns>
    public bool AddItem(ItemInstance itemToAdd)
    {
        if (itemToAdd == null || itemToAdd.quantity <= 0)
        {
            DebugLog("Attempted to add null or zero quantity item");
            return false;
        }

        // First try to stack with existing items
        if (TryStackItem(itemToAdd))
        {
            NotifyInventoryChanged();
            return true;
        }

        // If can't stack, find first empty slot
        int emptySlot = FindEmptySlot();
        if (emptySlot == -1)
        {
            DebugLog("Inventory is full");
            return false;
        }

        // Add to empty slot
        _inventorySlots[emptySlot] = itemToAdd;
        NotifySlotChanged(emptySlot);
        NotifyInventoryChanged();
        DebugLog($"Added {itemToAdd.quantity}x {itemToAdd.GetDisplayName()} to slot {emptySlot}");
        return true;
    }

    /// <summary>
    /// Adds an item to the inventory directly from an ItemDefinition
    /// </summary>
    /// <param name="itemDef">Item definition to add</param>
    /// <param name="quantity">Quantity to add</param>
    /// <returns>True if successfully added</returns>
    public bool AddItem(ItemDefinition itemDef, int quantity = 1)
    {
        if (itemDef == null || quantity <= 0)
        {
            DebugLog("Attempted to add null item definition or invalid quantity");
            return false;
        }

        ItemInstance newItem = new ItemInstance(itemDef, quantity);
        return AddItem(newItem);
    }

    /// <summary>
    /// Removes an item from a specific slot
    /// </summary>
    /// <param name="slotIndex">Slot to remove from</param>
    /// <param name="quantity">Amount to remove, defaults to all</param>
    /// <returns>Removed item instance, or null if slot was empty</returns>
    public ItemInstance RemoveItem(int slotIndex, int quantity = -1)
    {
        if (slotIndex < 0 || slotIndex >= _inventorySlots.Length)
            return null;

        ItemInstance slotItem = _inventorySlots[slotIndex];
        if (slotItem == null)
            return null;

        // Default to removing all
        if (quantity < 0 || quantity >= slotItem.quantity)
        {
            ItemInstance removedItem = slotItem;
            _inventorySlots[slotIndex] = null;
            NotifySlotChanged(slotIndex);
            NotifyInventoryChanged();
            DebugLog($"Removed all {removedItem.quantity}x {removedItem.GetDisplayName()} from slot {slotIndex}");
            return removedItem;
        }

        // Remove a portion of the stack
        ItemInstance partialStack = slotItem.Clone();
        partialStack.quantity = quantity;
        slotItem.quantity -= quantity;

        NotifySlotChanged(slotIndex);
        NotifyInventoryChanged();
        DebugLog($"Removed {quantity}x {partialStack.GetDisplayName()} from slot {slotIndex}");

        return partialStack;
    }

    /// <summary>
    /// Uses an item from a specific slot
    /// </summary>
    /// <param name="slotIndex">Slot to use item from</param>
    /// <param name="user">GameObject that will use the item</param>
    /// <returns>True if item was used</returns>
    public bool UseItem(int slotIndex, GameObject user)
    {
        if (slotIndex < 0 || slotIndex >= _inventorySlots.Length)
            return false;

        ItemInstance slotItem = _inventorySlots[slotIndex];
        if (slotItem == null)
            return false;

        // Use the item
        slotItem.Use(user);
        DebugLog($"Used {slotItem.GetDisplayName()} from slot {slotIndex}");

        // Check if the item was consumed
        if (slotItem.quantity <= 0)
        {
            _inventorySlots[slotIndex] = null;
            DebugLog($"Item was fully consumed and removed from slot {slotIndex}");
        }

        NotifySlotChanged(slotIndex);
        NotifyInventoryChanged();
        return true;
    }

    /// <summary>
    /// Equips an item from inventory to an equipment slot
    /// </summary>
    /// <param name="slotIndex">Inventory slot to equip from</param>
    /// <returns>True if successfully equipped</returns>
    public bool EquipItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _inventorySlots.Length)
            return false;

        ItemInstance itemToEquip = _inventorySlots[slotIndex];
        if (itemToEquip == null)
            return false;

        // Check if it's an equippable item
        if (itemToEquip.itemDefinition is ArmorItem armorItem)
        {
            // Get the appropriate equipment slot
            int equipSlotIndex = (int)armorItem.armorSlot;

            if (equipSlotIndex < 0 || equipSlotIndex >= _equippedItems.Length)
            {
                DebugLog($"Invalid equipment slot: {armorItem.armorSlot}");
                return false;
            }

            // Check if something is already equipped there
            ItemInstance currentEquipped = _equippedItems[equipSlotIndex];
            if (currentEquipped != null)
            {
                // Unequip the current item first
                currentEquipped.isEquipped = false;

                // Add it back to inventory
                if (!AddItem(currentEquipped))
                {
                    // If inventory is full, we can't swap
                    DebugLog("Cannot equip: inventory is full for item swap");
                    return false;
                }
            }

            // Equip the new item
            itemToEquip.isEquipped = true;
            _equippedItems[equipSlotIndex] = itemToEquip;
            _inventorySlots[slotIndex] = null; // Remove from inventory slot

            NotifySlotChanged(slotIndex);
            NotifyEquipmentChanged(armorItem.armorSlot);
            NotifyInventoryChanged();

            DebugLog($"Equipped {itemToEquip.GetDisplayName()} to {armorItem.armorSlot} slot");
            return true;
        }
        else if (itemToEquip.itemDefinition is WeaponItem)
        {
            // Handle weapon equipment (you might want to add a weapon slot in equipment)
            // For now, just mark it as equipped but keep it in inventory
            itemToEquip.isEquipped = true;
            NotifySlotChanged(slotIndex);
            DebugLog($"Equipped weapon {itemToEquip.GetDisplayName()}");
            return true;
        }

        DebugLog($"Item {itemToEquip.GetDisplayName()} is not equippable");
        return false;
    }

    /// <summary>
    /// Unequips an item from an equipment slot
    /// </summary>
    /// <param name="equipSlot">Equipment slot to unequip from</param>
    /// <returns>True if successfully unequipped</returns>
    public bool UnequipItem(ArmorItem.ArmorSlot equipSlot)
    {
        int slotIndex = (int)equipSlot;

        if (slotIndex < 0 || slotIndex >= _equippedItems.Length)
            return false;

        ItemInstance itemToUnequip = _equippedItems[slotIndex];
        if (itemToUnequip == null)
            return false;

        // Find a place in inventory
        if (!AddItem(itemToUnequip))
        {
            DebugLog("Cannot unequip: inventory is full");
            return false;
        }

        // Remove from equipment slot
        itemToUnequip.isEquipped = false;
        _equippedItems[slotIndex] = null;

        NotifyEquipmentChanged(equipSlot);
        NotifyInventoryChanged();

        DebugLog($"Unequipped {itemToUnequip.GetDisplayName()} from {equipSlot} slot");
        return true;
    }

    /// <summary>
    /// Transfers items between inventory slots
    /// </summary>
    /// <param name="fromSlot">Source slot</param>
    /// <param name="toSlot">Destination slot</param>
    /// <param name="quantity">Amount to move, -1 for all</param>
    /// <returns>True if transfer was successful</returns>
    public bool TransferItems(int fromSlot, int toSlot, int quantity = -1)
    {
        if (fromSlot < 0 || fromSlot >= _inventorySlots.Length ||
            toSlot < 0 || toSlot >= _inventorySlots.Length ||
            fromSlot == toSlot)
        {
            return false;
        }

        ItemInstance sourceItem = _inventorySlots[fromSlot];
        ItemInstance destItem = _inventorySlots[toSlot];

        if (sourceItem == null)
            return false;

        // Handle full stack transfer
        if (quantity < 0 || quantity >= sourceItem.quantity)
        {
            return HandleFullStackTransfer(fromSlot, toSlot, sourceItem, destItem);
        }
        else
        {
            return HandlePartialStackTransfer(fromSlot, toSlot, sourceItem, destItem, quantity);
        }
    }

    private bool HandleFullStackTransfer(int fromSlot, int toSlot, ItemInstance sourceItem, ItemInstance destItem)
    {
        // If destination is empty, simple move
        if (destItem == null)
        {
            _inventorySlots[toSlot] = sourceItem;
            _inventorySlots[fromSlot] = null;

            NotifySlotChanged(fromSlot);
            NotifySlotChanged(toSlot);
            NotifyInventoryChanged();

            return true;
        }

        // If destination has same item, try to stack
        if (destItem.itemDefinition == sourceItem.itemDefinition && destItem.CanStackWith(sourceItem))
        {
            int maxTransfer = destItem.GetMaxStackSize() - destItem.quantity;
            int amountToTransfer = Mathf.Min(maxTransfer, sourceItem.quantity);

            if (amountToTransfer > 0)
            {
                destItem.quantity += amountToTransfer;
                sourceItem.quantity -= amountToTransfer;

                // If source is empty after transfer, remove it
                if (sourceItem.quantity <= 0)
                    _inventorySlots[fromSlot] = null;

                NotifySlotChanged(fromSlot);
                NotifySlotChanged(toSlot);
                NotifyInventoryChanged();

                return true;
            }
        }

        // Items are different, swap them
        _inventorySlots[fromSlot] = destItem;
        _inventorySlots[toSlot] = sourceItem;

        NotifySlotChanged(fromSlot);
        NotifySlotChanged(toSlot);
        NotifyInventoryChanged();

        return true;
    }

    private bool HandlePartialStackTransfer(int fromSlot, int toSlot, ItemInstance sourceItem, ItemInstance destItem, int quantity)
    {
        if (quantity <= 0 || quantity > sourceItem.quantity)
            return false;

        // If destination is empty
        if (destItem == null)
        {
            // Create a new stack for destination
            ItemInstance newStack = sourceItem.Clone();
            newStack.quantity = quantity;
            _inventorySlots[toSlot] = newStack;

            // Reduce source stack
            sourceItem.quantity -= quantity;
            if (sourceItem.quantity <= 0)
                _inventorySlots[fromSlot] = null;

            NotifySlotChanged(fromSlot);
            NotifySlotChanged(toSlot);
            NotifyInventoryChanged();

            return true;
        }

        // If destination has same item and can stack
        if (destItem.itemDefinition == sourceItem.itemDefinition && destItem.CanStackWith(sourceItem))
        {
            int maxTransfer = destItem.GetMaxStackSize() - destItem.quantity;
            int amountToTransfer = Mathf.Min(maxTransfer, quantity);

            if (amountToTransfer > 0)
            {
                destItem.quantity += amountToTransfer;
                sourceItem.quantity -= amountToTransfer;

                // If source is empty after transfer, remove it
                if (sourceItem.quantity <= 0)
                    _inventorySlots[fromSlot] = null;

                NotifySlotChanged(fromSlot);
                NotifySlotChanged(toSlot);
                NotifyInventoryChanged();

                return true;
            }
        }

        // Cannot transfer (different items or destination stack is full)
        return false;
    }

    #endregion

    #region Stack Management

    /// <summary>
    /// Attempts to stack an item with existing stackable items
    /// </summary>
    /// <param name="itemToStack">Item to stack</param>
    /// <returns>True if completely stacked</returns>
    private bool TryStackItem(ItemInstance itemToStack)
    {
        // Only stackable items can be stacked
        if (!itemToStack.itemDefinition.isStackable)
            return false;

        int remainingQuantity = itemToStack.quantity;

        // Try to find existing stacks of the same item
        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            ItemInstance slotItem = _inventorySlots[i];

            // Skip empty slots or non-matching items
            if (slotItem == null || slotItem.itemDefinition != itemToStack.itemDefinition)
                continue;

            // Skip slots that can't stack with this item
            if (!slotItem.CanStackWith(itemToStack))
                continue;

            int maxAddAmount = slotItem.GetMaxStackSize() - slotItem.quantity;
            int amountToAdd = Mathf.Min(maxAddAmount, remainingQuantity);

            if (amountToAdd > 0)
            {
                // Add to this stack
                slotItem.quantity += amountToAdd;
                remainingQuantity -= amountToAdd;
                NotifySlotChanged(i);

                DebugLog($"Stacked {amountToAdd}x {itemToStack.GetDisplayName()} in slot {i}");

                // If we've stacked everything, we're done
                if (remainingQuantity <= 0)
                    return true;
            }
        }

        // Update the original item's quantity if we stacked some but not all
        if (remainingQuantity < itemToStack.quantity)
        {
            itemToStack.quantity = remainingQuantity;
            return false; // Still need to find a slot for the remainder
        }

        return false; // Could not stack at all
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Finds the first empty slot in the inventory
    /// </summary>
    /// <returns>Index of first empty slot, or -1 if inventory is full</returns>
    public int FindEmptySlot()
    {
        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            if (_inventorySlots[i] == null)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Gets an item from a specific slot without removing it
    /// </summary>
    /// <param name="slotIndex">Slot to check</param>
    /// <returns>The item in the specified slot, or null if empty</returns>
    public ItemInstance GetItemAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _inventorySlots.Length)
            return null;

        return _inventorySlots[slotIndex];
    }

    /// <summary>
    /// Gets an equipped item
    /// </summary>
    /// <param name="slot">Equipment slot to check</param>
    /// <returns>The equipped item, or null if nothing equipped</returns>
    public ItemInstance GetEquippedItem(ArmorItem.ArmorSlot slot)
    {
        int slotIndex = (int)slot;

        if (slotIndex < 0 || slotIndex >= _equippedItems.Length)
            return null;

        return _equippedItems[slotIndex];
    }

    /// <summary>
    /// Checks if the inventory has a specific item
    /// </summary>
    /// <param name="itemDef">Item definition to check for</param>
    /// <param name="quantity">Minimum quantity required</param>
    /// <returns>True if inventory contains the required quantity</returns>
    public bool HasItem(ItemDefinition itemDef, int quantity = 1)
    {
        if (itemDef == null || quantity <= 0)
            return false;

        int totalQuantity = 0;

        foreach (ItemInstance item in _inventorySlots)
        {
            if (item != null && item.itemDefinition == itemDef)
            {
                totalQuantity += item.quantity;
                if (totalQuantity >= quantity)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Counts the total quantity of a specific item in inventory
    /// </summary>
    /// <param name="itemDef">Item definition to count</param>
    /// <returns>Total quantity found</returns>
    public int GetItemCount(ItemDefinition itemDef)
    {
        if (itemDef == null)
            return 0;

        int totalQuantity = 0;

        foreach (ItemInstance item in _inventorySlots)
        {
            if (item != null && item.itemDefinition == itemDef)
                totalQuantity += item.quantity;
        }

        return totalQuantity;
    }

    /// <summary>
    /// Removes a specific quantity of an item type from inventory
    /// </summary>
    /// <param name="itemDef">Item definition to remove</param>
    /// <param name="quantity">Amount to remove</param>
    /// <returns>True if successfully removed</returns>
    public bool RemoveItem(ItemDefinition itemDef, int quantity)
    {
        if (itemDef == null || quantity <= 0)
            return false;

        // Check if we have enough
        if (!HasItem(itemDef, quantity))
            return false;

        int remainingToRemove = quantity;

        // Remove from stacks until we've removed enough
        for (int i = _inventorySlots.Length - 1; i >= 0; i--)
        {
            ItemInstance item = _inventorySlots[i];

            if (item != null && item.itemDefinition == itemDef)
            {
                if (item.quantity <= remainingToRemove)
                {
                    // Remove entire stack
                    remainingToRemove -= item.quantity;
                    _inventorySlots[i] = null;
                    NotifySlotChanged(i);
                }
                else
                {
                    // Remove partial stack
                    item.quantity -= remainingToRemove;
                    remainingToRemove = 0;
                    NotifySlotChanged(i);
                }

                if (remainingToRemove <= 0)
                    break;
            }
        }

        NotifyInventoryChanged();
        return true;
    }

    /// <summary>
    /// Clears the entire inventory
    /// </summary>
    public void ClearInventory()
    {
        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            _inventorySlots[i] = null;
            NotifySlotChanged(i);
        }

        for (int i = 0; i < _equippedItems.Length; i++)
        {
            _equippedItems[i] = null;
            NotifyEquipmentChanged((ArmorItem.ArmorSlot)i);
        }

        NotifyInventoryChanged();
        DebugLog("Inventory cleared");
    }

    #endregion

    #region Event Notification Methods

    public void NotifySlotChanged(int slotIndex)
    {
        OnSlotChanged?.Invoke(slotIndex);
        SaveInventory();
    }

    private void NotifyEquipmentChanged(ArmorItem.ArmorSlot slot)
    {
        OnEquipmentChanged?.Invoke(slot);
        SaveInventory();
    }

    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
        SaveInventory();
    }

    #endregion

    #region Persistence

    private void OnApplicationQuit()
    {
        SaveInventory();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveInventory();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugLog($"Scene loaded: {scene.name} - Inventory persists");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        SaveInventory();
        DebugLog($"Scene unloaded: {scene.name} - Inventory saved");
    }

    /// <summary>
    /// Saves the current inventory state to a file
    /// </summary>
    private bool SaveInventory()
    {
        try
        {
            string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            InventorySaveData saveData = new InventorySaveData();

            // Save inventory slots
            saveData.inventoryItems = new List<InventoryItemData>();
            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                ItemInstance item = _inventorySlots[i];
                if (item != null)
                {
                    InventoryItemData itemData = new InventoryItemData
                    {
                        slotIndex = i,
                        itemId = item.itemDefinition.id,
                        quantity = item.quantity,
                        durability = item.currentDurability,
                        isEquipped = item.isEquipped,
                        customName = item.customName,
                        hasCustomData = item.hasCustomData
                    };
                    saveData.inventoryItems.Add(itemData);
                }
            }

            // Save equipment slots
            saveData.equippedItems = new List<InventoryItemData>();
            for (int i = 0; i < _equippedItems.Length; i++)
            {
                ItemInstance item = _equippedItems[i];
                if (item != null)
                {
                    InventoryItemData itemData = new InventoryItemData
                    {
                        slotIndex = i,
                        itemId = item.itemDefinition.id,
                        quantity = item.quantity,
                        durability = item.currentDurability,
                        isEquipped = true,
                        customName = item.customName,
                        hasCustomData = item.hasCustomData
                    };
                    saveData.equippedItems.Add(itemData);
                }
            }

            // Write to file
            using (FileStream fileStream = new FileStream(savePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fileStream, saveData);
            }

            DebugLog($"Inventory saved to {savePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save inventory: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads inventory state from a file
    /// </summary>
    private bool LoadInventory()
    {
        string savePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(savePath))
        {
            DebugLog("No saved inventory found - starting with empty inventory");
            return false;
        }

        try
        {
            // Clear current inventory
            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                _inventorySlots[i] = null;
            }

            for (int i = 0; i < _equippedItems.Length; i++)
            {
                _equippedItems[i] = null;
            }

            // Read from file
            InventorySaveData saveData;
            using (FileStream fileStream = new FileStream(savePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                saveData = (InventorySaveData)formatter.Deserialize(fileStream);
            }

            // Create a dictionary of item definitions for quick lookup
            Dictionary<int, ItemDefinition> itemDefs = new Dictionary<int, ItemDefinition>();

            // Populate inventory slots
            foreach (InventoryItemData itemData in saveData.inventoryItems)
            {
                ItemDefinition itemDef = GetItemDefinitionById(itemData.itemId, itemDefs);

                if (itemDef != null && itemData.slotIndex >= 0 && itemData.slotIndex < _inventorySlots.Length)
                {
                    ItemInstance newItem = new ItemInstance(itemDef, itemData.quantity);
                    newItem.currentDurability = itemData.durability;
                    newItem.isEquipped = itemData.isEquipped;
                    newItem.customName = itemData.customName;
                    newItem.hasCustomData = itemData.hasCustomData;

                    _inventorySlots[itemData.slotIndex] = newItem;
                }
            }

            // Populate equipment slots
            foreach (InventoryItemData itemData in saveData.equippedItems)
            {
                ItemDefinition itemDef = GetItemDefinitionById(itemData.itemId, itemDefs);

                if (itemDef != null && itemData.slotIndex >= 0 && itemData.slotIndex < _equippedItems.Length)
                {
                    ItemInstance newItem = new ItemInstance(itemDef, itemData.quantity);
                    newItem.currentDurability = itemData.durability;
                    newItem.isEquipped = true;
                    newItem.customName = itemData.customName;
                    newItem.hasCustomData = itemData.hasCustomData;

                    _equippedItems[itemData.slotIndex] = newItem;
                }
            }

            // Trigger a general inventory changed event to update UI
            OnInventoryChanged?.Invoke();
            DebugLog("Inventory loaded successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Helper to find an item definition by ID
    /// </summary>
    private ItemDefinition GetItemDefinitionById(int id, Dictionary<int, ItemDefinition> cache)
    {
        // Check cache first
        if (cache.TryGetValue(id, out ItemDefinition cachedDef))
            return cachedDef;

        // Find item definition in your game's item database
        // This implementation depends on how you store/access all available item definitions
        // Option 1: Search through all Resources
        ItemDefinition[] allItems = Resources.LoadAll<ItemDefinition>("Items");
        foreach (ItemDefinition item in allItems)
        {
            if (item.id == id)
            {
                cache[id] = item;
                return item;
            }
        }

        // Option 2: If you have a centralized ItemDatabase, use that instead
        // ItemDefinition foundItem = ItemDatabase.Instance.GetItemById(id);
        // if (foundItem != null)
        //     cache[id] = foundItem;
        // return foundItem;

        Debug.LogWarning($"Could not find item definition with ID: {id}");
        return null;
    }

    // Helper for debug logging
    private void DebugLog(string message)
    {
        if (showDebugMessages)
            Debug.Log($"[Inventory] {message}");
    }

    #endregion
}

/// <summary>
/// Serializable class for saving inventory data
/// </summary>
[Serializable]
public class InventorySaveData
{
    public List<InventoryItemData> inventoryItems;
    public List<InventoryItemData> equippedItems;
}

/// <summary>
/// Serializable class for saving individual item data
/// </summary>
[Serializable]
public class InventoryItemData
{
    public int slotIndex;
    public int itemId;
    public int quantity;
    public float durability;
    public bool isEquipped;
    public string customName;
    public bool hasCustomData;
}