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
        var hint = _priceHints[index];
        string reason = !HasFreeDisplay(workshop) ? "Нет свободных витрин" :
            Score < WorkshopItemCatalog.CraftCost(workshop) ? "Недостаточно денег" : "";
        hint.GetNode<Label>("Label").Text = GameLocalization.Text(reason);
        hint.Visible = reason.Length > 0 && !_rarityPanels[index].Visible;
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
        if (!_waitingBuyer || item != _waitingBuyerItem) return;

        OfferDisplay(slot);
    }

    internal void OfferCollectedCraft(int slot, int item)
    {
        if (TradeActive || GetTree().Paused || DayTransitionOpen || !_dayCycle.CanReceiveBuyers ||
            _showcaseItems[slot] != item) return;
        RefreshBuyers();
        if (!_waitingBuyer || item != _waitingBuyerItem) return;

        string workshop = Items[item].WorkshopId;
        Vector2 counter = GetNode<Node2D>("World/TradingRoom/Item").GlobalPosition;
        int nearest = slot;
        float distance = _showcaseSlots[slot].GetGlobalRect().GetCenter().DistanceSquaredTo(counter);
        for (int candidate = 0; candidate < ShowcaseCapacity; candidate++)
        {
            if (_showcaseItems[candidate] >= 0 || DisplayWorkshop(candidate) != workshop) continue;
            float candidateDistance = _showcaseSlots[candidate].GetGlobalRect().GetCenter().DistanceSquaredTo(counter);
            if (candidateDistance >= distance) continue;
            nearest = candidate;
            distance = candidateDistance;
        }
        if (nearest != slot)
        {
            PutDisplay(nearest, new DisplayStock(item, _showcaseGrades[slot], _showcaseRerolls[slot]),
                _showcaseItems, _showcaseGrades, _showcaseRerolls);
            ClearDisplay(slot, _showcaseItems, _showcaseGrades, _showcaseRerolls);
        }
        OfferDisplay(nearest);
    }

    private void OfferDisplay(int slot)
    {
        _activeShowcaseIndex = slot;
        ItemIndex = _showcaseItems[slot];
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
        _waitingBuyerItem = ItemIndex;
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
