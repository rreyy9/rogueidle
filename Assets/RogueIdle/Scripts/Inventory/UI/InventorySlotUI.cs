using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject highlightOverlay;

    private int slotIndex;
    private ItemInstance currentItem;

    // Events
    public event Action<int> OnSlotClicked;
    public static event Action<InventorySlotUI> OnBeginDragEvent;
    public static event Action<InventorySlotUI> OnEndDragEvent;
    public static event Action<InventorySlotUI, InventorySlotUI> OnDropEvent;

    public static InventorySlotUI draggingSlot;

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    public void UpdateSlot(ItemInstance item)
    {
        currentItem = item;

        if (item != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = item.itemDefinition.icon;

            // Show quantity text only for stackable items with quantity > 1
            if (item.quantity > 1)
            {
                quantityText.gameObject.SetActive(true);
                quantityText.text = item.quantity.ToString();
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Empty slot
            itemIcon.gameObject.SetActive(false);
            quantityText.gameObject.SetActive(false);
        }

        // Reset highlight
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightOverlay != null)
        {
            highlightOverlay.SetActive(highlighted);
        }
    }

    // Handle clicking on slot
    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(slotIndex);
    }

    // Handle drag and drop functionality
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            draggingSlot = this;
            OnBeginDragEvent?.Invoke(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Handle visualization of dragged item
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingSlot == this)
        {
            draggingSlot = null;
            OnEndDragEvent?.Invoke(this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggingSlot != null && draggingSlot != this)
        {
            OnDropEvent?.Invoke(draggingSlot, this);
        }
    }
}