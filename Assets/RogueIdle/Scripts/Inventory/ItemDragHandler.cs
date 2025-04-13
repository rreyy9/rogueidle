using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Handles dragging items between inventory and hotbar slots.
/// Add this to your Canvas object.
/// </summary>
public class ItemDragHandler : MonoBehaviour
{
    [SerializeField] private GameObject dragIconPrefab;
    [SerializeField] private Canvas parentCanvas;

    private InventoryManager inventoryManager;
    private HotbarSystem hotbarSystem;

    // Dragging state
    private GameObject dragIconInstance;
    private RectTransform dragRectTransform;
    private Image dragIconImage;

    private int sourceSlotIndex = -1;
    private bool draggingFromInventory = false;
    private bool isDragging = false;

    private void Awake()
    {
        // Find references
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        hotbarSystem = FindFirstObjectByType<HotbarSystem>();

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        // Create drag icon object
        CreateDragIcon();
    }

    private void CreateDragIcon()
    {
        // Create a simple icon that follows the cursor when dragging
        dragIconInstance = Instantiate(dragIconPrefab, parentCanvas.transform);
        dragRectTransform = dragIconInstance.GetComponent<RectTransform>();
        dragIconImage = dragIconInstance.GetComponent<Image>();

        // Start hidden
        dragIconInstance.SetActive(false);
    }

    private void Update()
    {
        if (isDragging && dragIconInstance != null)
        {
            // Update icon position to follow mouse
            dragRectTransform.position = Input.mousePosition;

            // Check for mouse release
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
    }

    /// <summary>
    /// Start dragging an item from an inventory slot
    /// </summary>
    public void BeginDragFromInventory(int slotIndex)
    {
        ItemInstance item = inventoryManager.GetItemAt(slotIndex);

        if (item != null)
        {
            sourceSlotIndex = slotIndex;
            draggingFromInventory = true;
            BeginDrag(item.itemDefinition.icon);
        }
    }

    /// <summary>
    /// Start dragging an item from a hotbar slot
    /// </summary>
    public void BeginDragFromHotbar(int slotIndex)
    {
        // Get the item via the hotbar
        ItemInstance item = hotbarSystem.GetItemFromHotbarSlotUI(slotIndex);

        if (item != null)
        {
            sourceSlotIndex = slotIndex;
            draggingFromInventory = false;
            BeginDrag(item.itemDefinition.icon);
        }
    }

    private void BeginDrag(Sprite icon)
    {
        isDragging = true;

        // Set up drag icon
        dragIconImage.sprite = icon;
        dragIconImage.color = new Color(1, 1, 1, 0.8f);
        dragIconImage.raycastTarget = false; // Prevent icon from blocking raycasts

        // Position at mouse and show
        dragRectTransform.position = Input.mousePosition;
        dragIconInstance.SetActive(true);
    }

    private void EndDrag()
    {
        isDragging = false;
        dragIconInstance.SetActive(false);

        // Find what's under the pointer
        GameObject target = FindObjectUnderPointer();

        if (target != null)
        {
            // If dragging from inventory to hotbar
            if (draggingFromInventory)
            {
                HotbarSlotUI HotbarSlotUI = target.GetComponent<HotbarSlotUI>();
                if (HotbarSlotUI != null)
                {
                    // Assign item to hotbar
                    int hotbarIndex = HotbarSlotUI.GetSlotIndex();
                    hotbarSystem.AssignItemToHotbar(sourceSlotIndex, hotbarIndex);
                }
                else
                {
                    // Could be inventory slot (inventory to inventory)
                    HandleInventoryToInventory(target);
                }
            }
            // If dragging from hotbar to inventory or to another hotbar slot
            else
            {
                HotbarSlotUI HotbarSlotUI = target.GetComponent<HotbarSlotUI>();
                if (HotbarSlotUI != null && HotbarSlotUI.GetSlotIndex() != sourceSlotIndex)
                {
                    // Move within hotbar
                    int sourceInventorySlot = hotbarSystem.GetInventorySlotFromHotbar(sourceSlotIndex);
                    if (sourceInventorySlot >= 0)
                    {
                        hotbarSystem.AssignItemToHotbar(sourceInventorySlot, HotbarSlotUI.GetSlotIndex());
                        hotbarSystem.ClearHotbarSlotUI(sourceSlotIndex);
                    }
                }
                else
                {
                    // Could be dropping hotbar item back into inventory
                    // This might just clear the hotbar slot if you want to implement it
                    hotbarSystem.ClearHotbarSlotUI(sourceSlotIndex);
                }
            }
        }

        // Reset state
        sourceSlotIndex = -1;
    }

    private void HandleInventoryToInventory(GameObject target)
    {
        // This requires your inventory UI to have exposed components
        // Here's a simplified example - you'll need to adapt this to your UI structure
        InventorySlotUI inventorySlot = target.GetComponent<InventorySlotUI>();
        if (inventorySlot != null && inventorySlot.GetSlotIndex() != sourceSlotIndex)
        {
            // Move within inventory
            inventoryManager.TransferItems(sourceSlotIndex, inventorySlot.GetSlotIndex());
        }
    }

    private GameObject FindObjectUnderPointer()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject != dragIconInstance)
            {
                return result.gameObject;
            }
        }

        return null;
    }
}