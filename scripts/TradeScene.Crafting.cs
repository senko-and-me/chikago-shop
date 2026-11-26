using Godot;

// Inventory/save adapter for CraftCoordinator. Existing inventory representation stays compatible.
public partial class TradeScene
{
    public CraftCoordinator Crafting { get; private set; }
    public bool CraftClockRunning =>
        ((_embroidery?.IsOpen == true && _embroidery.ActiveSession.State == ChikagoShop.Domain.Crafting.EmbroideryState.Playing) ||
         (_polishing?.IsOpen == true && _polishing.ActiveSession.State == ChikagoShop.Domain.Crafting.EmbroideryState.Playing)) &&
        !GetNode<PauseMenu>("PauseMenu").IsOpen && !_tutorial.IsSkipConfirmationOpen;

    internal int BeginSelectedCraft(int index)
    {
        if (Crafting.UnavailableReason(index) != "") return -1;
        Score -= WorkshopItemCatalog.ItemCraftCost(index);
        int position = _storedItems.Count;
        _storedItems.Add(index);
        _storedGrades.Add("D");
        _pendingCraftStorageIndex = position;
        RefreshCraftInventory();
        return position;
    }

    internal void CompleteSelectedCraft(int position, int expectedItem, string rank)
    {
        if (position < 0 || position >= _storedItems.Count || _storedItems[position] != expectedItem) return;
        _storedGrades[position] = NormalizeRank(rank);
        _pendingCraftStorageIndex = -1;
        RefreshCraftInventory();
    }

    private void RefreshCraftInventory()
    {
        RefreshScore();
        RefreshProgress();
        RefreshItemButtons();
        _inventoryUI.UpdateUI(_storedItems);
        SaveProgress();
    }
}
