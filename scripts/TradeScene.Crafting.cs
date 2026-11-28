using Godot;
using ChikagoShop.Domain.Persistence;

// Inventory/save adapter for CraftCoordinator. Existing inventory representation stays compatible.
public partial class TradeScene
{
    public CraftCoordinator Crafting { get; private set; }
    internal bool CraftClockRunning => CraftMiniGameOpen && !Crafting.Covered;

    internal int CurrentCraftCost => DayUpgrades.CraftPrice(DebugBalanceSettings.Economy.CraftCost);

    internal int BeginSelectedCraft(int index)
    {
        int position = FreeDisplay(WorkshopItemCatalog.Items[index].WorkshopId, _showcaseItems, MusicWorkshopUnlocked);
        int cost = CurrentCraftCost;
        if (position < 0 || DayClosed || !DayStarted || Score < cost) return -1;
        Score -= cost;
        _pendingCraftSlot = position;
        PutDisplay(position, new DisplayStock(index, "D", GetItemRerollLimit(index)), _showcaseItems, _showcaseGrades, _showcaseRerolls);
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
