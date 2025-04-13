using UnityEngine;

// Specialized item type for weapons
[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Items/Weapon")]
public class WeaponItem : ItemDefinition
{
    [Header("Weapon Properties")]
    public float damage;
    public float attackSpeed;
    public float durability = 100f;
    public WeaponType weaponType;
    public AudioClip attackSound;

    // Weapon-specific stats for display
    public override string GetStats()
    {
        return base.GetStats() + $"\nDamage: {damage}\nAttack Speed: {attackSpeed}/s\nDurability: {durability}%";
    }

    // Specialized use implementation for weapons
    public override void Use(GameObject user)
    {
        // Could equip the weapon instead of using it directly
        Debug.Log($"{user.name} equipped {itemName}");

        // Example to notify a weapon manager or equipment system
        if (user.TryGetComponent(out PlayerActionsInput playerActions))
        {
            // Implementation would depend on your combat system
            Debug.Log($"Ready to attack with {itemName}");
        }
    }

    public enum WeaponType
    {
        Sword,
        Axe,
        Bow,
        Staff,
        Dagger,
        Mace,
        Spear
    }
}

// Specialized item type for consumables (potions, food, etc.)
[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Items/Consumable")]
public class ConsumableItem : ItemDefinition
{
    [Header("Consumable Properties")]
    public float healthValue;
    public float energyValue;
    public float duration;
    public ParticleSystem useEffect;

    // Consumable-specific stats display
    public override string GetStats()
    {
        string stats = base.GetStats();

        if (healthValue != 0)
            stats += $"\nHealth: +{healthValue}";

        if (energyValue != 0)
            stats += $"\nEnergy: +{energyValue}";

        if (duration > 0)
            stats += $"\nDuration: {duration}s";

        return stats;
    }

    // Specialized use implementation for consumables
    public override void Use(GameObject user)
    {
        Debug.Log($"{user.name} consumed {itemName}");

        // Apply effects - example implementation
        // In a real implementation, you would hook this up to your character stats system
        if (user.TryGetComponent(out PlayerState playerState))
        {
            // Apply health, energy effects, etc.
            Debug.Log($"Applied effects from {itemName}");
        }
    }
}

// Specialized item type for armor/equipment
[CreateAssetMenu(fileName = "New Armor", menuName = "Inventory/Items/Armor")]
public class ArmorItem : ItemDefinition
{
    [Header("Armor Properties")]
    public float armorValue;
    public float durability = 100f;
    public ArmorType armorType;
    public ArmorSlot armorSlot;

    // Armor-specific stats
    public override string GetStats()
    {
        return base.GetStats() + $"\nArmor: {armorValue}\nDurability: {durability}%";
    }

    // Specialized use implementation for armor
    public override void Use(GameObject user)
    {
        Debug.Log($"{user.name} equipped {itemName}");

        // Example equip logic
        if (user.TryGetComponent(out PlayerState playerState))
        {
            // Implementation would integrate with your equipment system
            Debug.Log($"Equipped {itemName} to {armorSlot} slot");
        }
    }

    public enum ArmorType
    {
        Light,
        Medium,
        Heavy,
        Magical
    }

    public enum ArmorSlot
    {
        Head,
        Chest,
        Legs,
        Feet,
        Hands,
        Shield
    }
}