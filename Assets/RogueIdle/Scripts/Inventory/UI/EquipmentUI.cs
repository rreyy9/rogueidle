using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EquipmentUI : MonoBehaviour
{
    [Header("Equipment Panel")]
    [SerializeField] private GameObject equipmentPanel;

    [Header("Equipment Slots")]
    [SerializeField] private EquipmentSlotUI headSlot;
    [SerializeField] private EquipmentSlotUI chestSlot;
    [SerializeField] private EquipmentSlotUI handsSlot;
    [SerializeField] private EquipmentSlotUI legsSlot;
    [SerializeField] private EquipmentSlotUI feetSlot;
    [SerializeField] private EquipmentSlotUI weaponSlot;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI totalArmorText;
    [SerializeField] private TextMeshProUGUI weaponDamageText;
    [SerializeField] private TextMeshProUGUI equippedItemsText;

    private Dictionary<ArmorItem.ArmorSlot, EquipmentSlotUI> equipmentSlots = new Dictionary<ArmorItem.ArmorSlot, EquipmentSlotUI>();
    private InventoryManager inventoryManager;

    private void Awake()
    {
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("No InventoryManager found in the scene!");
        }
    }

    private void Start()
    {
        // Map equipment slots
        InitializeEquipmentSlots();

        // Update UI
        RefreshEquipment();

        // Subscribe to equipment change events
        inventoryManager.OnEquipmentChanged += OnEquipmentChanged;
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }

    private void InitializeEquipmentSlots()
    {
        equipmentSlots.Clear();

        equipmentSlots.Add(ArmorItem.ArmorSlot.Head, headSlot);
        equipmentSlots.Add(ArmorItem.ArmorSlot.Chest, chestSlot);
        equipmentSlots.Add(ArmorItem.ArmorSlot.Hands, handsSlot);
        equipmentSlots.Add(ArmorItem.ArmorSlot.Legs, legsSlot);
        equipmentSlots.Add(ArmorItem.ArmorSlot.Feet, feetSlot);

        // Initialize slots with their equipment type
        foreach (var pair in equipmentSlots)
        {
            pair.Value.SetSlotType(pair.Key);
        }

        // Weapon slot is handled separately
        weaponSlot.SetSlotType(ArmorItem.ArmorSlot.Shield); // Using Shield for Weapon slot for now
    }

    private void OnEquipmentChanged(ArmorItem.ArmorSlot slot)
    {
        // Update the specific slot that changed
        UpdateEquipmentSlot(slot);

        // Update overall stats display
        UpdateStatsDisplay();
    }

    private void RefreshEquipment()
    {
        // Update all equipment slots
        foreach (ArmorItem.ArmorSlot slot in System.Enum.GetValues(typeof(ArmorItem.ArmorSlot)))
        {
            UpdateEquipmentSlot(slot);
        }

        // Update overall stats display
        UpdateStatsDisplay();
    }

    private void UpdateEquipmentSlot(ArmorItem.ArmorSlot slot)
    {
        if (equipmentSlots.TryGetValue(slot, out EquipmentSlotUI slotUI))
        {
            ItemInstance equippedItem = inventoryManager.GetEquippedItem(slot);
            slotUI.UpdateSlot(equippedItem);
        }
    }

    private void UpdateStatsDisplay()
    {
        // Calculate total armor value
        float totalArmor = 0f;

        // Calculate weapon damage
        float weaponDamage = 0f;

        // Count equipped items
        int equippedCount = 0;
        int maxEquipSlots = equipmentSlots.Count + 1; // +1 for weapon

        // Check all equipment slots
        foreach (ArmorItem.ArmorSlot slot in System.Enum.GetValues(typeof(ArmorItem.ArmorSlot)))
        {
            ItemInstance item = inventoryManager.GetEquippedItem(slot);

            if (item != null)
            {
                equippedCount++;

                if (item.itemDefinition is ArmorItem armorItem)
                {
                    totalArmor += armorItem.armorValue;
                }
                else if (item.itemDefinition is WeaponItem weaponItem)
                {
                    weaponDamage = weaponItem.damage;
                }
            }
        }

        // Update text displays
        totalArmorText.text = $"Total Armor: {totalArmor}";
        weaponDamageText.text = $"Weapon Damage: {weaponDamage}";
        equippedItemsText.text = $"Equipped Items: {equippedCount}/{maxEquipSlots}";
    }
}