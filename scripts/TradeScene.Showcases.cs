using System;
using System.Collections.Generic;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class TradeScene
{
    private int _pendingCraftSlot = -1;
    private int? _returnedOfferPercent;
    private readonly int[] _showcaseRerolls = new int[ShowcaseCapacity];
    private readonly int[] _checkpointShowcaseRerolls = new int[ShowcaseCapacity];
    private readonly List<DisplayStock> _deferredStock = new();
    private readonly List<DisplayStock> _checkpointStock = new();
    public DisplayStock[] DeferredItems => _deferredStock.ToArray();
    public string GetShowcaseQualityRank(int slot) => slot >= 0 && slot < ShowcaseCapacity ? NormalizeRank(_showcaseGrades[slot]) : "D";
    internal ItemInfoPopup ShowcasePopup { get; private set; } = null!;

    private static string DisplayWorkshop(int slot) => slot < InitialShowcaseCapacity ? "atelier" : "music";

    private static int FreeDisplay(string workshop, int[] items, bool musicOpen)
    {
        if (workshop == "music" && !musicOpen) return -1;
        for (int i = 0; i < items.Length; i++)
            if (DisplayWorkshop(i) == workshop && items[i] < 0) return i;
        return -1;
    }

    internal bool HasFreeDisplay(string workshop) => FreeDisplay(workshop, _showcaseItems, MusicWorkshopUnlocked) >= 0;

    private void CaptureDisplayCheckpoint()
    {
        _checkpointStock.Clear();
        _checkpointStock.AddRange(_deferredStock);
        Array.Copy(_showcaseRerolls, _checkpointShowcaseRerolls, ShowcaseCapacity);
    }

    private void RestoreDisplayCheckpoint()
    {
        _pendingCraftSlot = -1;
        _returnedOfferPercent = null;
        _deferredStock.Clear();
        _deferredStock.AddRange(_checkpointStock);
        Array.Copy(_checkpointShowcaseRerolls, _showcaseRerolls, ShowcaseCapacity);
    }

    private void ResetDisplayStock()
    {
        _pendingCraftSlot = -1;
        _returnedOfferPercent = null;
        _deferredStock.Clear();
        _checkpointStock.Clear();
        Array.Fill(_showcaseRerolls, -1);
        Array.Fill(_checkpointShowcaseRerolls, -1);
    }

    internal bool CanBeginCraft(string workshop) =>
        !Failed && !Completed && !DayClosed && !QuotaVisitPending && !DayTransitionOpen &&
        Score >= WorkshopItemCatalog.CraftCost(workshop) &&
        FreeDisplay(workshop, _showcaseItems, MusicWorkshopUnlocked) >= 0;

    private void RefreshCraftPriceHint(int index, string workshop)
    {
        var label = _priceHints[index].GetNode<Label>("Label");
        label.Text = FreeDisplay(workshop, _showcaseItems, MusicWorkshopUnlocked) < 0
            ? GameLocalization.Text("Нет свободных витрин")
            : GameLocalization.Format("Цена создания: {0}$", WorkshopItemCatalog.CraftCost(workshop));
        int deferred = _deferredStock.FindAll(stock => Items[stock.Item].WorkshopId == workshop).Count;
        if (deferred > 0)
            label.Text += "\n" + GameLocalization.Format("Товаров ждут свободной витрины: {0}", deferred);
    }

    private void InitializeShowcasePopup()
    {
        var layer = new CanvasLayer { Name = "ShowcaseDescriptions", Layer = 60 };
        AddChild(layer);
        ShowcasePopup = new ItemInfoPopup { Name = "ItemInfo", Tutorial = _tutorial };
        layer.AddChild(ShowcasePopup);

        for (int i = 0; i < _showcaseSlots.Length; i++)
        {
            int slot = i;
            var button = _showcaseSlots[slot];
            button.MouseEntered += () =>
            {
                if (DescribeDisplay(slot) == null) return;
                ShowcasePopup.ShowFor(button, () => DescribeDisplay(slot));
                _tutorial.OnShowcaseInspected(slot);
            };
            button.MouseExited += () => ShowcasePopup.HideFor(button);
        }
    }

    private string DescribeDisplay(int slot)
    {
        int item = _showcaseItems[slot];
        if (item < 0 || slot == _pendingCraftSlot) return null;
        string grade = NormalizeRank(_showcaseGrades[slot]);
        string info = GameLocalization.Format("{0}\nКачество: {1}\nНадбавка за качество: +{2}$\nСтоимость: {3}$",
            GetItemName(item), grade, QualityBonus(grade), GetItemPrice(item));
        return slot == _activeShowcaseIndex && TradeActive
            ? info + "\n" + GameLocalization.Text("Нажмите, чтобы вернуть товар на витрину") : info;
    }

    internal int FirstDisplayedItem(string workshop)
    {
        for (int slot = 0; slot < _showcaseItems.Length; slot++)
            if (slot != _pendingCraftSlot && _showcaseItems[slot] is int item && item >= 0 &&
                Items[item].WorkshopId == workshop) return slot;
        return -1;
    }

    internal Control DisplayControl(int slot) => _showcaseSlots[slot];

    public void OnShowcasePressed(int slot)
    {
        if (slot < 0 || slot >= ShowcaseCapacity || slot == _pendingCraftSlot ||
            GetTree().Paused || DayTransitionOpen || !RequireStartedDay()) return;

        if (TradeActive)
        {
            if (_activeShowcaseIndex == slot) ReturnItemToShowcase();
            return;
        }

        int item = _showcaseItems[slot];
        if (item < 0 || !_tutorial.CanSelectDisplay(slot) || !_dayCycle.CanReceiveBuyers) return;
        RefreshBuyers();
        if (!_waitingBuyer || (Items[item].WorkshopId == "music") != _waitingBuyerMusic) return;

        _activeShowcaseIndex = slot;
        ItemIndex = item;
        _activeTradeGrade = NormalizeRank(_showcaseGrades[slot]);
        ShowcasePopup.HideInfo();
        StartBuyerOffer();
    }

    public void ReturnItemToShowcase()
    {
        if (!TradeActive || GetTree().Paused || _tutorial.BlocksSaleActions) return;
        int slot = _activeShowcaseIndex;
        if (slot < 0)
        {
            var stock = new DisplayStock(ItemIndex, _activeTradeGrade, RerollsRemaining);
            _deferredStock.Add(stock);
            FillDeferredDisplays();
        }
        else _showcaseRerolls[slot] = RerollsRemaining;

        _returnedOfferPercent = Percentage;
        TradeActive = false;
        _activeShowcaseIndex = -1;
        _waitingBuyer = !DayClosed;
        _waitingBuyerMusic = Items[ItemIndex].WorkshopId == "music";
        ItemIndex = -1;
        _world.ItemVisible = false;
        if (_waitingBuyer) ShowWaitingBuyer();
        else FinishTrade();
        RefreshItemButtons();
        SaveProgress();
    }

    private void ReleaseSoldDisplay()
    {
        if (_activeShowcaseIndex >= 0) ClearDisplay(_activeShowcaseIndex, _showcaseItems, _showcaseGrades, _showcaseRerolls);
        _activeShowcaseIndex = -1;
        FillDeferredDisplays();
    }

    private void FillDeferredDisplays() =>
        FillDeferredDisplays(_deferredStock, _showcaseItems, _showcaseGrades, _showcaseRerolls, MusicWorkshopUnlocked);

    private static void FillDeferredDisplays(List<DisplayStock> source, int[] items, string[] grades, int[] rerolls, bool musicOpen)
    {
        for (int i = 0; i < source.Count;)
        {
            var stock = source[i];
            int slot = FreeDisplay(Items[stock.Item].WorkshopId, items, musicOpen);
            if (slot < 0)
            {
                i++;
                continue;
            }
            PutDisplay(slot, stock, items, grades, rerolls);
            source.RemoveAt(i);
        }
    }

    private static void PutDisplay(int slot, DisplayStock stock, int[] items, string[] grades, int[] rerolls)
    {
        items[slot] = stock.Item;
        grades[slot] = NormalizeRank(stock.Grade);
        rerolls[slot] = stock.Rerolls;
    }

    private static void ClearDisplay(int slot, int[] items, string[] grades, int[] rerolls) =>
        PutDisplay(slot, new DisplayStock(-1, "D", -1), items, grades, rerolls);
}
