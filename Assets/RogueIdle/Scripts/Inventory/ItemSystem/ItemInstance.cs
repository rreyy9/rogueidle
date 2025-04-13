using UnityEngine;
using System;

// Represents an instance of an item in the inventory 
[System.Serializable]
public class ItemInstance
{
    // Reference to the core item definition
    public ItemDefinition itemDefinition;

    // Core instance properties
    public int quantity = 1;
    public bool isEquipped = false;

    // Instance-specific properties that may vary
    public float currentDurability = 100f;

    // Optional custom name (for named/unique items)
    public string customName = "";

    public bool hasCustomData = false;

    // Track when the item was acquired
    public DateTime acquiredDateTime;

    // Constructor with required definition
    public ItemInstance(ItemDefinition definition, int amount = 1)
    {
        if (definition == null)
            throw new ArgumentNullException("Item definition cannot be null");

        itemDefinition = definition;
        quantity = Mathf.Clamp(amount, 1, definition.maxStackSize);
        acquiredDateTime = DateTime.Now;

        // Initialize type-specific instance properties
        if (definition is WeaponItem weaponItem)
        {
            currentDurability = weaponItem.durability;
        }
        else if (definition is ArmorItem armorItem)
        {
            currentDurability = armorItem.durability;
        }
    }

    // Get the actual name, accounting for custom names
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(customName))
            return customName;
        return itemDefinition.itemName;
    }

    // Get max stack size from definition
    public int GetMaxStackSize()
    {
        return itemDefinition.maxStackSize;
    }

    // Check if this item can stack with another instance
    public bool CanStackWith(ItemInstance other)
    {
        // Must have same definition
        if (other.itemDefinition != itemDefinition)
            return false;

        // Must not have custom data/modifications
        if (hasCustomData || other.hasCustomData)
            return false;

        // Check if stack size would be valid
        return quantity < itemDefinition.maxStackSize;
    }

    // Use the item
    public void Use(GameObject user)
    {
        itemDefinition.Use(user);

        // For consumables, reduce quantity
        if (itemDefinition is ConsumableItem)
        {
            quantity--;
        }

        // For durability-based items, reduce durability
        if (itemDefinition is WeaponItem || itemDefinition is ArmorItem)
        {
            // Example implementation - you may want more sophisticated durability
            currentDurability = Mathf.Max(0, currentDurability - 1);
        }
    }

    // Get a cloned copy of this instance
    public ItemInstance Clone()
    {
        ItemInstance clone = new ItemInstance(itemDefinition, quantity);
        clone.customName = customName;
        clone.currentDurability = currentDurability;
        clone.hasCustomData = hasCustomData;
        clone.isEquipped = isEquipped;
        clone.acquiredDateTime = acquiredDateTime;
        return clone;
    }
}