using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SplitStackUI : MonoBehaviour
{
    [SerializeField] private GameObject splitStackPanel;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [SerializeField] private Image originalStackIcon;
    [SerializeField] private TextMeshProUGUI originalStackText;
    [SerializeField] private Image newStackIcon;
    [SerializeField] private TextMeshProUGUI newStackText;

    private int originalSlotIndex;
    private ItemInstance originalItem;
    private int splitAmount = 1;

    private void Awake()
    {
        // Hide panel initially
        splitStackPanel.SetActive(false);

        // Set up button listeners
        confirmButton.onClick.AddListener(ConfirmSplit);
        cancelButton.onClick.AddListener(CancelSplit);

        // Set up slider listener
        amountSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    public void ShowSplitStackPanel(int slotIndex)
    {
        originalSlotIndex = slotIndex;
        originalItem = InventoryManager.Instance.GetItemAt(slotIndex);

        if (originalItem == null || originalItem.quantity <= 1)
        {
            // Can't split a stack of 1 or empty slot
            return;
        }

        // Configure slider
        amountSlider.minValue = 1;
        amountSlider.maxValue = originalItem.quantity - 1; // At least 1 stays in original stack
        amountSlider.value = 1; // Default to 1

        // Set up icons
        originalStackIcon.sprite = originalItem.itemDefinition.icon;
        newStackIcon.sprite = originalItem.itemDefinition.icon;

        // Update text
        UpdateStackText();

        // Show panel
        splitStackPanel.SetActive(true);
    }

    private void OnSliderValueChanged(float value)
    {
        splitAmount = Mathf.RoundToInt(value);
        UpdateStackText();
    }

    private void UpdateStackText()
    {
        amountText.text = $"Amount: {splitAmount}";
        originalStackText.text = (originalItem.quantity - splitAmount).ToString();
        newStackText.text = splitAmount.ToString();
    }

    private void ConfirmSplit()
    {
        if (originalItem != null && splitAmount > 0)
        {
            // Find empty slot
            int emptySlot = InventoryManager.Instance.FindEmptySlot();

            if (emptySlot != -1)
            {
                // Create new item instance for the split portion
                ItemInstance newStack = originalItem.Clone();
                newStack.quantity = splitAmount;

                // Reduce original stack
                originalItem.quantity -= splitAmount;

                // Add new stack to inventory
                InventoryManager.Instance.GetItemAt(emptySlot);

                // Update UI
                InventoryManager.Instance.NotifySlotChanged(originalSlotIndex);
                InventoryManager.Instance.NotifySlotChanged(emptySlot);
                InventoryManager.Instance.NotifyInventoryChanged();
            }
        }

        // Hide panel
        splitStackPanel.SetActive(false);
    }

    private void CancelSplit()
    {
        // Just hide the panel
        splitStackPanel.SetActive(false);
    }
}