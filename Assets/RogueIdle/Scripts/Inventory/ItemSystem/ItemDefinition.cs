using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic Properties")]
    public int id;
    public string itemName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    public GameObject prefab;

    [Header("Classification")]
    public ItemType itemType;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Stacking")]
    public int maxStackSize = 1;
    public bool isStackable => maxStackSize > 1;

    [Header("Economy")]
    public float baseValue = 1f;

    // Methods to handle basic item interactions
    public virtual void Use(GameObject user)
    {
        // Base implementation - override in derived classes for specific behavior
        Debug.Log($"{user.name} uses {itemName}");
    }

    public virtual string GetDescription()
    {
        return description;
    }

    public virtual string GetStats()
    {
        return $"Value: {baseValue}";
    }
}

// Enum for categorizing items
public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Material,
    QuestItem,
    Tool,
    Miscellaneous
}

// Enum for item rarity/quality
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}