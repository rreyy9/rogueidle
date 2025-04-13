using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject emptySlotIcon;
    [SerializeField] private GameObject highlightOverlay;

    private ArmorItem.ArmorSlot slotType;
    private ItemInstance equippedItem;

    // Events
    public event Action<EquipmentSlotUI> OnSlotClicked;

    public void SetSlotType(ArmorItem.ArmorSlot type)
    {
        slotType = type;
    }

    public ArmorItem.ArmorSlot GetSlotType()
    {
        return slotType;
    }

    public void UpdateSlot(ItemInstance item)
    {
        equippedItem = item;

        if (item != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = item.itemDefinition.icon;

            if (emptySlotIcon != null)
            {
                emptySlotIcon.SetActive(false);
            }
        }
        else
        {
            // Empty slot
            itemIcon.gameObject.SetActive(false);

            if (emptySlotIcon != null)
            {
                emptySlotIcon.SetActive(true);
            }
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

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(this);

        // If there's an item equipped and right click
        if (equippedItem != null && eventData.button == PointerEventData.InputButton.Right)
        {
            // Unequip the item
            InventoryManager.Instance.UnequipItem(slotType);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Get the dragged inventory slot
        InventorySlotUI draggedSlot = InventorySlotUI.draggingSlot;

        if (draggedSlot != null)
        {
            // Get the item in the dragged slot
            ItemInstance draggedItem = InventoryManager.Instance.GetItemAt(draggedSlot.GetSlotIndex());

            if (draggedItem != null)
            {
                // Check if the item can be equipped in this slot
                if (draggedItem.itemDefinition is ArmorItem armorItem && armorItem.armorSlot == slotType)
                {
                    // Equip the item
                    InventoryManager.Instance.EquipItem(draggedSlot.GetSlotIndex());
                }
                else if (draggedItem.itemDefinition is WeaponItem && slotType == ArmorItem.ArmorSlot.Shield)
                {
                    // Equip the weapon (assuming Shield slot is used for weapons too)
                    InventoryManager.Instance.EquipItem(draggedSlot.GetSlotIndex());
                }
            }
        }
    }
}