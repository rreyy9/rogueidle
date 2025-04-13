using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HotbarSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int slotCount = 6;

    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;

    // Store references to slot UI elements
    private List<HotbarSlotUI> slots = new List<HotbarSlotUI>();

    // Store which inventory slot is linked to each hotbar slot
    private Dictionary<int, int> hotbarToInventoryMap = new Dictionary<int, int>();

    // Currently selected slot index
    private int selectedSlotIndex = 0;

    private void Start()
    {
        // Find inventory manager if not assigned
        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>();

        // Initialize hotbar
        InitializeHotbar();

        // Setup subscriptions to inventory changes
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged += RefreshHotbar;
            inventoryManager.OnSlotChanged += OnInventorySlotChanged;
        }
    }

    private void OnDestroy()
    {
        // Clean up subscriptions
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshHotbar;
            inventoryManager.OnSlotChanged -= OnInventorySlotChanged;
        }
    }

    private void InitializeHotbar()
    {
        // Clear any existing slots
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        // Initialize mapping dictionary with empty values
        for (int i = 0; i < slotCount; i++)
        {
            hotbarToInventoryMap[i] = -1; // -1 means no item assigned
        }

        // Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            HotbarSlotUI slot = slotObj.GetComponent<HotbarSlotUI>();

            if (slot != null)
            {
                // Set up the slot
                slot.SetIndex(i);
                slot.SetKeyText((i + 1).ToString());
                slot.OnSlotClicked = OnSlotClicked;

                slots.Add(slot);
            }
        }

        // Select the first slot by default
        SelectSlot(0);
    }

    // Refresh the hotbar after inventory changes
    private void RefreshHotbar()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            UpdateSlot(i);
        }
    }

    // Update a specific slot
    private void UpdateSlot(int hotbarIndex)
    {
        if (hotbarIndex >= 0 && hotbarIndex < slots.Count)
        {
            // Get the mapped inventory slot
            if (hotbarToInventoryMap.TryGetValue(hotbarIndex, out int inventorySlot) && inventorySlot >= 0)
            {
                // Get the item from inventory
                ItemInstance item = inventoryManager.GetItemAt(inventorySlot);

                if (item != null)
                {
                    // Update the slot with item data
                    slots[hotbarIndex].SetItem(item);
                }
                else
                {
                    // Inventory slot is now empty
                    slots[hotbarIndex].ClearSlot();
                    hotbarToInventoryMap[hotbarIndex] = -1;
                }
            }
            else
            {
                // No item assigned to this slot
                slots[hotbarIndex].ClearSlot();
            }
        }
    }

    // Called when an inventory slot changes
    private void OnInventorySlotChanged(int inventorySlot)
    {
        // Find any hotbar slots that reference this inventory slot
        foreach (var pair in hotbarToInventoryMap)
        {
            if (pair.Value == inventorySlot)
            {
                UpdateSlot(pair.Key);
            }
        }
    }

    // Handle slot selection
    private void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return;

        // Deselect previous slot
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
            slots[selectedSlotIndex].SetSelected(false);

        // Select new slot
        selectedSlotIndex = index;
        slots[selectedSlotIndex].SetSelected(true);
    }

    // Handle slot clicks
    private void OnSlotClicked(int slotIndex)
    {
        SelectSlot(slotIndex);
    }

    // Public method to assign an inventory item to the hotbar
    public void AssignItemToHotbar(int inventorySlot, int HotbarSlotUI)
    {
        if (HotbarSlotUI < 0 || HotbarSlotUI >= slotCount || inventorySlot < 0)
            return;

        // Get the item
        ItemInstance item = inventoryManager.GetItemAt(inventorySlot);

        if (item != null)
        {
            // Map this hotbar slot to the inventory slot
            hotbarToInventoryMap[HotbarSlotUI] = inventorySlot;

            // Update the visuals
            UpdateSlot(HotbarSlotUI);
        }
    }

    // Get an item instance from a hotbar slot
    public ItemInstance GetItemFromHotbarSlotUI(int HotbarSlotUI)
    {
        if (HotbarSlotUI < 0 || HotbarSlotUI >= slotCount)
            return null;

        if (hotbarToInventoryMap.TryGetValue(HotbarSlotUI, out int inventorySlot) && inventorySlot >= 0)
        {
            return inventoryManager.GetItemAt(inventorySlot);
        }

        return null;
    }

    // Get the inventory slot index associated with a hotbar slot
    public int GetInventorySlotFromHotbar(int HotbarSlotUI)
    {
        if (HotbarSlotUI < 0 || HotbarSlotUI >= slotCount)
            return -1;

        if (hotbarToInventoryMap.TryGetValue(HotbarSlotUI, out int inventorySlot))
        {
            return inventorySlot;
        }

        return -1;
    }

    // Clear a hotbar slot mapping
    public void ClearHotbarSlotUI(int HotbarSlotUI)
    {
        if (HotbarSlotUI < 0 || HotbarSlotUI >= slotCount)
            return;

        hotbarToInventoryMap[HotbarSlotUI] = -1;
        UpdateSlot(HotbarSlotUI);
    }

    // Public method to get the currently selected item
    public ItemInstance GetSelectedItem()
    {
        if (hotbarToInventoryMap.TryGetValue(selectedSlotIndex, out int inventorySlot) && inventorySlot >= 0)
        {
            return inventoryManager.GetItemAt(inventorySlot);
        }

        return null;
    }

    // Public method to use the currently selected item
    public void UseSelectedItem()
    {
        if (hotbarToInventoryMap.TryGetValue(selectedSlotIndex, out int inventorySlot) && inventorySlot >= 0)
        {
            // Use the item in the inventory
            if (inventoryManager != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    inventoryManager.UseItem(inventorySlot, player);
                }
            }
        }
    }
}