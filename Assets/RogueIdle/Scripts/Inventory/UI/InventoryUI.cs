using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Panel")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform inventorySlotGrid;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private int slotCount = 20;

    [Header("Category Filters")]
    [SerializeField] private Button allButton;
    [SerializeField] private Button weaponsButton;
    [SerializeField] private Button armorButton;
    [SerializeField] private Button consumablesButton;
    [SerializeField] private Button materialsButton;

    [Header("Item Details Panel")]
    [SerializeField] private GameObject itemDetailsPanel;
    [SerializeField] private Image itemDetailIcon;
    [SerializeField] private TextMeshProUGUI itemDetailName;
    [SerializeField] private TextMeshProUGUI itemDetailType;
    [SerializeField] private TextMeshProUGUI itemDetailStats;
    [SerializeField] private TextMeshProUGUI itemDetailValue;
    [SerializeField] private Button useButton;
    [SerializeField] private Button dropButton;

    private List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private InventoryManager inventoryManager;
    private ItemType currentFilter = ItemType.Miscellaneous; // Using -1 to indicate "All"

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
        // Initialize inventory slots
        InitializeInventorySlots();

        // Set up button listeners
        SetupButtonListeners();

        // Hide item details by default
        itemDetailsPanel.SetActive(false);

        // Update UI to reflect current inventory state
        RefreshInventory();

        // Subscribe to inventory events
        inventoryManager.OnInventoryChanged += RefreshInventory;
        inventoryManager.OnSlotChanged += RefreshSlot;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshInventory;
            inventoryManager.OnSlotChanged -= RefreshSlot;
        }
    }

    private void InitializeInventorySlots()
    {
        // Clear existing slots if any
        foreach (Transform child in inventorySlotGrid)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        // Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = Instantiate(inventorySlotPrefab, inventorySlotGrid);
            InventorySlotUI slot = slotObject.GetComponent<InventorySlotUI>();

            if (slot != null)
            {
                slot.SetSlotIndex(i);
                slot.OnSlotClicked += ShowItemDetails;
                slots.Add(slot);
            }
        }
    }

    private void SetupButtonListeners()
    {
        allButton.onClick.AddListener(() => FilterInventory(ItemType.Miscellaneous, true));
        weaponsButton.onClick.AddListener(() => FilterInventory(ItemType.Weapon));
        armorButton.onClick.AddListener(() => FilterInventory(ItemType.Armor));
        consumablesButton.onClick.AddListener(() => FilterInventory(ItemType.Consumable));
        materialsButton.onClick.AddListener(() => FilterInventory(ItemType.Material));

        useButton.onClick.AddListener(UseSelectedItem);
        dropButton.onClick.AddListener(DropSelectedItem);
    }

    private void FilterInventory(ItemType filterType, bool showAll = false)
    {
        currentFilter = filterType;
        RefreshInventory();
    }

    private void RefreshInventory()
    {
        // Update all slots based on inventory state
        for (int i = 0; i < slots.Count; i++)
        {
            RefreshSlot(i);
        }
    }

    private void RefreshSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        ItemInstance item = inventoryManager.GetItemAt(slotIndex);
        slots[slotIndex].UpdateSlot(item);

        // Apply filtering
        bool shouldShow = currentFilter == ItemType.Miscellaneous ||
                          (item != null && item.itemDefinition.itemType == currentFilter);

        slots[slotIndex].gameObject.SetActive(shouldShow);
    }

    private void ShowItemDetails(int slotIndex)
    {
        ItemInstance item = inventoryManager.GetItemAt(slotIndex);

        if (item != null)
        {
            // Show and update details panel
            itemDetailsPanel.SetActive(true);
            itemDetailIcon.sprite = item.itemDefinition.icon;
            itemDetailName.text = item.GetDisplayName();

            // Set type text based on item rarity and type
            string rarityText = item.itemDefinition.rarity.ToString();
            string typeText = item.itemDefinition.itemType.ToString();
            itemDetailType.text = $"{rarityText} {typeText}";

            // Set stats
            itemDetailStats.text = item.itemDefinition.GetStats();

            // Set value
            itemDetailValue.text = $"Value: {item.itemDefinition.baseValue} gold";

            // Enable use button based on item type
            useButton.interactable = item.itemDefinition.itemType == ItemType.Consumable ||
                                     item.itemDefinition.itemType == ItemType.Weapon ||
                                     item.itemDefinition.itemType == ItemType.Armor;
        }
        else
        {
            itemDetailsPanel.SetActive(false);
        }
    }

    private void UseSelectedItem()
    {
        // Implementation depends on your item usage system
    }

    private void DropSelectedItem()
    {
        // Implementation depends on your item dropping system
    }
}