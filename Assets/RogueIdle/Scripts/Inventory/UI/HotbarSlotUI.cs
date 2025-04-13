using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class HotbarSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private Image selectionIndicator;
    [SerializeField] private Image highlightImage;

    private int slotIndex;
    private bool hasItem = false;

    // References
    private ItemDragHandler dragHandler;

    // Delegate for click events
    public Action<int> OnSlotClicked;

    private void Awake()
    {
        // Find drag handler
        dragHandler = FindFirstObjectByType<ItemDragHandler>();

        // Ensure highlight is off initially
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    public void SetIndex(int index)
    {
        slotIndex = index;
    }

    public void SetKeyText(string text)
    {
        if (keyText != null)
            keyText.text = text;
    }

    public void SetItem(ItemInstance item)
    {
        if (item == null)
        {
            ClearSlot();
            return;
        }

        // Set the icon
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);

            if (item.itemDefinition.icon != null)
            {
                itemIcon.sprite = item.itemDefinition.icon;
                itemIcon.color = Color.white;
            }
            else
            {
                // If no icon, use a default color
                itemIcon.sprite = null;
                itemIcon.color = Color.white;
            }
        }

        // Show quantity for stackable items
        if (quantityText != null)
        {
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

        hasItem = true;
    }

    public void ClearSlot()
    {
        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);

        if (quantityText != null)
            quantityText.gameObject.SetActive(false);

        hasItem = false;
    }

    public void SetSelected(bool selected)
    {
        if (selectionIndicator != null)
            selectionIndicator.gameObject.SetActive(selected);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(highlighted);
    }

    // Click handling
    public void OnPointerClick(PointerEventData eventData)
    {
        // Left click = select
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSlotClicked?.Invoke(slotIndex);
        }
        // Right click = clear
        else if (eventData.button == PointerEventData.InputButton.Right && hasItem)
        {
            HotbarSystem hotbarUI = FindFirstObjectByType<HotbarSystem>();
            if (hotbarUI != null)
            {
                hotbarUI.ClearHotbarSlotUI(slotIndex);
            }
        }
    }

    // Drag and drop handling
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (hasItem && dragHandler != null)
        {
            dragHandler.BeginDragFromHotbar(slotIndex);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // The drag handler handles the visuals
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // The drag handler handles the drop logic
    }

    public void OnDrop(PointerEventData eventData)
    {
        // This is handled by the drag handler's EndDrag method
        // which will call the appropriate SimpleHotbarUI methods
    }
}