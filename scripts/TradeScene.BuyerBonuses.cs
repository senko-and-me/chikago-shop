using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Trading;
using Godot;

// A crafted object's quality belongs to the particular copy, not its catalogue kind.
// Parallel grade lists follow inventory/showcase transfers and are stored in saves.
public partial class TradeScene
{
    public const double BuyerWaitDuration = ChikagoShop.Domain.Economy.RunRewardRules.BuyerWaitDuration;

    private readonly List<string> _storedGrades = new();
    private readonly List<string> _checkpointGrades = new();
    private readonly string[] _showcaseGrades = new string[ShowcaseCapacity];
    private readonly string[] _checkpointShowcaseGrades = new string[ShowcaseCapacity];
    private string _activeTradeGrade = "D";
    private double _buyerWaitRemaining = BuyerWaitDuration;
    private double _buyerWaitInitial = BuyerWaitDuration;
    private int _pendingCraftStorageIndex = -1;
    private int _lastShownSpeedBonus = -1;
    private double _buyerWaitAutosave;
    private bool _waitingBuyer;
    private int _waitingBuyerItem = -1;
    private int _previousBuyerItem = -1;
    private bool _preferDifferentFirstRequest;
    private BuyerWaitWidget _buyerWaitWidget = null!;

    public double BuyerWaitInitial => _buyerWaitInitial;
    public double BuyerWaitRemaining => _buyerWaitRemaining;
    internal string BuyerClockText => TradeActive || _waitingBuyer
        ? _buyerWaitRemaining.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo(GameLocalization.Language))
        : "—";

    internal void PrepareNewDayBuyer()
    {
        if (TradeActive) return;
        HideWaitingBuyer();
        _preferDifferentFirstRequest = true;
    }
    public string ActiveTradeQualityRank => _activeTradeGrade;

    public string GetStoredItemQualityRank(int inventoryPosition) =>
        inventoryPosition >= 0 && inventoryPosition < _storedGrades.Count
            ? NormalizeRank(_storedGrades[inventoryPosition]) : "D";

    private static string NormalizeRank(string rank) => rank is "S" or "A" or "B" or "C" or "D" ? rank : "D";

    public static int QualityBonus(string rank) => ChikagoShop.Domain.Economy.RunRewardRules.QualityBonus(rank);

    // Five equal time bands, with no speed tip at zero.
    public static int SpeedBonus(double seconds, double initialSeconds = BuyerWaitDuration) =>
        ChikagoShop.Domain.Economy.RunRewardRules.SpeedBonus(seconds, initialSeconds);

    public void FinalizeLatestCraftQuality(string rank)
    {
        if (_pendingCraftStorageIndex < 0 || _pendingCraftStorageIndex >= _storedGrades.Count)
            return;
        _storedGrades[_pendingCraftStorageIndex] = NormalizeRank(rank);
        _pendingCraftStorageIndex = -1;
        _inventoryUI.UpdateUI(_storedItems);
        SaveProgress();
    }

    private void InitializeBuyerWaitWidget()
    {
        _buyerWaitWidget = new BuyerWaitWidget
        {
            Name = "BuyerWaitingThought",
            Position = new Vector2(1120, 294),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(205, 155)
        };
        GetNode<Node2D>("World/TradingRoom").AddChild(_buyerWaitWidget);
        _buyerWaitWidget.Hide();
    }

    private void ResetBuyerWait()
    {
        var economy = DebugBalanceSettings.Economy;
        _buyerWaitInitial = _rng.RandiRange(economy.BuyerWaitMinSeconds, economy.BuyerWaitMaxSeconds) + DayUpgrades.WaitExtension;
        _buyerWaitRemaining = _buyerWaitInitial;
        _buyerWaitAutosave = 0;
        _lastShownSpeedBonus = -1;
    }

    private void LimitTrainingInitialOffer()
    {
        int basePrice = GetItemPrice(ItemIndex);
        int ceiling = TradeOfferRules.Price(basePrice, TradeOfferRules.MaxPercent);
        if (TradeOfferRules.Price(basePrice, Percentage) < ceiling) return;
        // Preserve the ordinary price range and leave room for a higher rounded price.
        for (int percent = TradeOfferRules.MaxPercent - 1; percent >= TradeOfferRules.MinPercent; percent--)
        {
            if (TradeOfferRules.Price(basePrice, percent) >= ceiling) continue;
            Percentage = percent;
            break;
        }
    }

    internal void PrepareTutorialReroll()
    {
        int basePrice = GetItemPrice(ItemIndex);
        int ceiling = TradeOfferRules.Price(basePrice, TradeOfferRules.MaxPercent);
        if (OfferedPrice < ceiling && _buyerWaitRemaining > 0)
        {
            RefreshTutorialTradeActions();
            return;
        }
        if (_buyerWaitRemaining <= 0)
        {
            _buyerWaitRemaining = _buyerWaitInitial;
            UpdateBuyerWaitWidget();
        }
        LimitTrainingInitialOffer();
        RefreshOffer();
        SaveProgress();
    }

    internal void BeginTutorialTimedSale()
    {
        _buyerWaitRemaining = _buyerWaitInitial;
        _buyerWaitAutosave = 0;
        UpdateBuyerWaitWidget();
        RefreshOffer();
        SaveProgress();
    }

    internal void ShortenTutorialLateSale()
    {
        const double previewSeconds = 2;
        if (_buyerWaitRemaining <= previewSeconds) return;

        _buyerWaitRemaining = previewSeconds;
        UpdateBuyerWaitWidget();
        RefreshOffer();
        SaveProgress();
    }

    private void UpdateBuyerWaitWidget() =>
        _buyerWaitWidget?.SetState(TradeActive ? ItemIndex : _waitingBuyerItem, _buyerWaitRemaining, _buyerWaitInitial);

    private void TickBuyerWait(double delta)
    {
        if ((!TradeActive && !_waitingBuyer) || _dayCycle.Session.IsClosed || _tutorial.FreezeTrainingBuyer)
            return;

        // The timer belongs to the customer and follows them into the offer.
        bool wasExpired = BuyerPatienceRules.IsExpired(_buyerWaitRemaining);
        _buyerWaitRemaining = Math.Max(_tutorial.HoldTrainingBuyer ? 1 : 0, _buyerWaitRemaining - delta);
        bool justExpired = !wasExpired && BuyerPatienceRules.IsExpired(_buyerWaitRemaining);
        UpdateBuyerWaitWidget();
        if (TradeActive)
        {
            int speedNow = SpeedBonus(_buyerWaitRemaining, _buyerWaitInitial);
            if (justExpired) RefreshOffer();
            else if (_lastShownSpeedBonus != speedNow) RefreshTotalOfferDisplay();
        }
        _buyerWaitAutosave += delta;
        if (justExpired || _buyerWaitAutosave >= 1.0)
        {
            _buyerWaitAutosave = 0;
            SaveProgress();
        }
    }

    private void StartWaitingBuyer(int collectedSlot = -1)
    {
        if (_waitingBuyer || TradeActive || !_dayCycle.CanReceiveBuyers || !CanSupplyWaitingBuyer()) return;

        _returnedOfferPercent = null;
        _waitingBuyerItem = ChooseBuyerRequest();
        _previousBuyerItem = _waitingBuyerItem;
        _preferDifferentFirstRequest = false;
        CustomerIndex = _rng.RandiRange(0, _customer.GetAppearanceCount() - 1);
        _waitingBuyer = true;
        ResetBuyerWait();
        ShowWaitingBuyer();
        BuyerArrived?.Invoke();
        if (!_waitingBuyer || TradeActive) return;

        if (collectedSlot >= 0 && _showcaseItems[collectedSlot] == _waitingBuyerItem)
        {
            OfferCollectedDisplay(collectedSlot, _waitingBuyerItem);
            return;
        }

        if (!_tutorial.IsActive)
            for (int slot = 0; slot < _showcaseItems.Length; slot++)
            {
                if (slot == _pendingCraftSlot || _showcaseItems[slot] != _waitingBuyerItem) continue;
                OfferDisplay(slot);
                return;
            }

        SaveProgress();
    }

    private int ChooseBuyerRequest()
    {
        if (_tutorial.IsActive && FirstDisplayedItem("atelier") is int slot && slot >= 0)
            return _showcaseItems[slot];

        // Prefer goods the player can sell immediately. At an empty counter, ask for
        // a catalogue item the player can afford to craft in an open workshop.
        int[] stocked = DisplayedRequestItems().Distinct().ToArray();
        int[] unlocked = Enumerable.Range(0, Items.Count).Where(IsAvailableBuyerRequest).ToArray();
        int[] craftable = Score >= CurrentCraftCost
            ? unlocked.Where(item => HasFreeDisplay(Items[item].WorkshopId)).ToArray() : [];
        int[] candidates = stocked.Length > 0 ? stocked : craftable.Length > 0 ? craftable : unlocked;
        int[] alternatives = candidates.Where(item => item != _previousBuyerItem).ToArray();
        if (alternatives.Length == 0 && _preferDifferentFirstRequest)
            alternatives = craftable.Where(item => item != _previousBuyerItem).ToArray();
        if (alternatives.Length > 0) candidates = alternatives;

        string workshop = MusicWorkshopUnlocked && _rng.Randf() < 0.5f ? "music" : "atelier";
        int[] preferred = candidates.Where(item => Items[item].WorkshopId == workshop).ToArray();
        if (preferred.Length > 0) candidates = preferred;
        return candidates[_rng.RandiRange(0, candidates.Length - 1)];
    }

    private IEnumerable<int> DisplayedRequestItems() =>
        _showcaseItems.Where((item, slot) => slot != _pendingCraftSlot && IsAvailableBuyerRequest(item));

    private bool IsAvailableBuyerRequest(int item) =>
        item >= 0 && item < Items.Count && (MusicWorkshopUnlocked || Items[item].WorkshopId == "atelier");

    private int RestoreWaitingBuyerRequest(string kind, bool music)
    {
        int item = WorkshopItemCatalog.IndexOf(kind);
        if (IsAvailableBuyerRequest(item)) return item;

        string workshop = music && MusicWorkshopUnlocked ? "music" : "atelier";
        int slot = FirstDisplayedItem(workshop);
        return slot >= 0 ? _showcaseItems[slot] : ChooseBuyerRequest();
    }

    private void ReplaceUnavailableExpiredBuyer()
    {
        if (!_waitingBuyer || !_dayCycle.CanReceiveBuyers || _tutorial.IsActive ||
            !BuyerPatienceRules.IsExpired(_buyerWaitRemaining)) return;

        int item = _waitingBuyerItem;
        if (Array.IndexOf(_showcaseItems, item) >= 0 ||
            (HasFreeDisplay(Items[item].WorkshopId) && Score >= CurrentCraftCost) ||
            !DisplayedRequestItems().Any()) return;

        // A full workshop or an unaffordable request must not trap all the remaining stock.
        HideWaitingBuyer();
        StartWaitingBuyer();
    }

    private void ShowWaitingBuyer()
    {
        if (!_waitingBuyer || !_dayCycle.CanReceiveBuyers) return;
        if (DismissBuyerWithoutSupply()) return;
        _customer.SetAppearance(CustomerIndex);
        _customer.Visible = true;
        _world.ItemVisible = false;
        _tradePanel.Visible = false;
        UpdateBuyerWaitWidget();
    }

    private void HideWaitingBuyer()
    {
        _waitingBuyer = false;
        _waitingBuyerItem = -1;
        _buyerWaitWidget?.EndTrade();
        if (!TradeActive) _customer.Visible = false;
    }

    private bool DeferTutorialBuyerArrival()
    {
        if (_tutorial?.DefersBuyerArrival != true) return false;
        if (_waitingBuyer)
        {
            HideWaitingBuyer();
            SaveProgress();
        }
        return true;
    }

    private bool CanSupplyWaitingBuyer() => BuyerAvailabilityRules.CanWait(DayClosed);

    private bool DismissBuyerWithoutSupply()
    {
        if (CanSupplyWaitingBuyer()) return false;
        HideWaitingBuyer();
        SaveProgress();
        return true;
    }

    private void SynchronizeBuyerPatienceWithDay()
    {
        _buyerWaitRemaining = BuyerPatienceRules.RemainingAfterDayClosure(_buyerWaitRemaining, DayClosed);
        if (TradeActive && DayClosed)
            UpdateBuyerWaitWidget();
    }

    private void RefreshClosedBuyerOffer()
    {
        if (TradeActive && DayClosed) RefreshOffer();
    }

    private bool CanRerollCurrentBuyer => BuyerPatienceRules.CanReroll(
        TradeActive, DayClosed, RerollsRemaining, _buyerWaitRemaining);

    private int CardSaleBonus => DayUpgrades.SaleBonus(Items[ItemIndex].WorkshopId, _activeTradeGrade, _buyerWaitRemaining);

    private int CurrentBuyerPayout => BuyerPatienceRules.TotalPayout(
        OfferedPrice, _buyerWaitRemaining, SpeedBonus(_buyerWaitRemaining, _buyerWaitInitial), QualityBonus(_activeTradeGrade) + CardSaleBonus);

    private void RefreshTotalOfferDisplay()
    {
        if (!TradeActive || ItemIndex < 0) return;
        int speed = SpeedBonus(_buyerWaitRemaining, _buyerWaitInitial);
        int grade = QualityBonus(_activeTradeGrade);
        _lastShownSpeedBonus = speed;
        int basePrice = GetItemPrice(ItemIndex);
        int bonus = CurrentBuyerPayout - OfferedPrice;
        string offer = GameLocalization.Format("Получим: {0}$", OfferedPrice);
        _offerLabel.Text = bonus != 0 ? $"{offer} {ColoredAmount(bonus)}" : offer;

        string breakdown = GameLocalization.Format("Предложение: {0}\nСкорость: {1}\nКачество: {2}",
            SignedAmount(OfferedPrice - basePrice), SignedAmount(speed), SignedAmount(grade));
        if (CardSaleBonus != 0) breakdown += "\n" + GameLocalization.Format("Карточка: {0}", SignedAmount(CardSaleBonus));
        breakdown += "\n" + GameLocalization.Format("Итого: {0}$", CurrentBuyerPayout);
        _percentLabel.TooltipText = _offerLabel.TooltipText = breakdown;
    }

    internal static string ColoredAmount(int value) => value == 0 ? GameLocalization.Format("{0}$", 0) :
        $"[color={(value > 0 ? "#268d48" : "#c32938")}]{SignedAmount(value)}[/color]";

    internal static string SignedAmount(int value) =>
        GameLocalization.Format(value < 0 ? "−{0}$" : "+{0}$", Math.Abs(value));

    #nullable enable annotations
    private void LoadLegacyGradeArrays(string[]? storedGrades, string[]? showcaseGrades)
    {
        _storedGrades.Clear();
        for (int i = 0; i < _storedItems.Count; i++)
            _storedGrades.Add(storedGrades != null && i < storedGrades.Length ? NormalizeRank(storedGrades[i]) : "D");
        for (int i = 0; i < ShowcaseCapacity; i++)
            _showcaseGrades[i] = showcaseGrades != null && i < showcaseGrades.Length ? NormalizeRank(showcaseGrades[i]) : "D";
    }
    #nullable restore annotations
}
