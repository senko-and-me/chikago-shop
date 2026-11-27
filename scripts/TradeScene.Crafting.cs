using Godot;
using ChikagoShop.Domain.Persistence;

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
        int position = FreeDisplay(WorkshopItemCatalog.Items[index].WorkshopId, _showcaseItems, MusicWorkshopUnlocked);
        if (position < 0) return -1;
        Score -= WorkshopItemCatalog.ItemCraftCost(index);
        _pendingCraftSlot = position;
        PutDisplay(position, new DisplayStock(index, "D", -1), _showcaseItems, _showcaseGrades, _showcaseRerolls);
        RefreshCraftInventory();
        return position;
    }

    internal void CompleteSelectedCraft(int position, int expectedItem, string rank)
    {
        if (position < 0 || position >= ShowcaseCapacity || _showcaseItems[position] != expectedItem || _pendingCraftSlot != position) return;
        _showcaseGrades[position] = NormalizeRank(rank);
        _pendingCraftSlot = -1;
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
