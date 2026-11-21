using System;
using System.Collections.Generic;
using ChikagoShop.Domain.Trading;
using Godot;

// A crafted object's quality belongs to the particular copy, not its catalogue kind.
// Parallel grade lists follow inventory/showcase transfers and are stored in saves.
public partial class TradeScene
{
    public const double BuyerWaitDuration = 20.0;
    private const int MaxSpeedBonus = 5;
    private readonly List<string> _storedGrades = new();
    private readonly List<string> _checkpointGrades = new();
    private readonly string[] _showcaseGrades = new string[ShowcaseCapacity];
    private readonly string[] _checkpointShowcaseGrades = new string[ShowcaseCapacity];
    private string _activeTradeGrade = "D";
    private double _buyerWaitRemaining = BuyerWaitDuration;
    private int _pendingCraftStorageIndex = -1;
    private int _lastShownSpeedBonus = -1;
    private double _buyerWaitAutosave;
    private bool _waitingBuyer;
    private bool _waitingBuyerMusic;
    private BuyerWaitWidget _buyerWaitWidget = null!;

    public double BuyerWaitRemaining => _buyerWaitRemaining;
    public string ActiveTradeQualityRank => _activeTradeGrade;

    public string GetStoredItemQualityRank(int inventoryPosition) =>
        inventoryPosition >= 0 && inventoryPosition < _storedGrades.Count
            ? NormalizeRank(_storedGrades[inventoryPosition]) : "D";

    private static string NormalizeRank(string rank) => rank is "S" or "A" or "B" or "C" or "D" ? rank : "D";

    public static int QualityBonus(string rank) => NormalizeRank(rank) switch
    {
        "S" => 5, "A" => 4, "B" => 3, "C" => 2, _ => 0
    };

    // Five equal time bands, with no speed tip at zero.
    public static int SpeedBonus(double seconds) => seconds <= 0 ? 0 :
        (int)Math.Ceiling(Math.Min(BuyerWaitDuration, seconds) / (BuyerWaitDuration / MaxSpeedBonus));

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
        _buyerWaitRemaining = BuyerWaitDuration;
        _buyerWaitAutosave = 0;
        _lastShownSpeedBonus = -1;
    }

    private void TickBuyerWait(double delta)
    {
        if ((!TradeActive && !_waitingBuyer) || GetTree().Paused || _dayCycle.Session.IsClosed)
            return;

        // The timer belongs to the customer and follows them into the offer.
        bool wasExpired = BuyerPatienceRules.IsExpired(_buyerWaitRemaining);
        _buyerWaitRemaining = Math.Max(0, _buyerWaitRemaining - delta);
        bool justExpired = !wasExpired && BuyerPatienceRules.IsExpired(_buyerWaitRemaining);
        if (_buyerWaitWidget != null)
            _buyerWaitWidget.SetState(_waitingBuyer ? _waitingBuyerMusic :
                Items[ItemIndex].WorkshopId == "music", _buyerWaitRemaining);
        if (TradeActive)
        {
            int speedNow = SpeedBonus(_buyerWaitRemaining);
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

    private void StartWaitingBuyer()
    {
        if (_waitingBuyer || TradeActive || !_dayCycle.CanReceiveBuyers || !CanSupplyWaitingBuyer()) return;

        // Each new customer independently chooses a desired category, 50/50.
        // Before the music workshop is unlocked, only atelier requests are playable.
        // Do not bias the choice toward the current showcase inventory.
        _waitingBuyerMusic = MusicWorkshopUnlocked && _rng.Randf() < 0.5f;
        CustomerIndex = _rng.RandiRange(0, _customer.GetAppearanceCount() - 1);
        _waitingBuyer = true;
        ResetBuyerWait();
        ShowWaitingBuyer();
        BuyerArrived?.Invoke();
        SaveProgress();
    }

    private void ShowWaitingBuyer()
    {
        if (!_waitingBuyer || !_dayCycle.CanReceiveBuyers) return;
        if (DismissBuyerWithoutSupply()) return;
        _customer.SetAppearance(CustomerIndex);
        _customer.Visible = true;
        _world.ItemVisible = false;
        _tradePanel.Visible = false;
        _buyerWaitWidget.SetState(_waitingBuyerMusic, _buyerWaitRemaining);
    }

    private void HideWaitingBuyer()
    {
        _waitingBuyer = false;
        _buyerWaitWidget?.EndTrade();
        if (!TradeActive) _customer.Visible = false;
    }

    private bool TryServeWaitingBuyer()
    {
        if (!_waitingBuyer || !_dayCycle.CanReceiveBuyers) return false;
        if (DismissBuyerWithoutSupply()) return false;
        var matching = new List<int>();
        for (int i = 0; i < ShowcaseCapacity; i++)
        {
            int index = _showcaseItems[i];
            if (index >= 0 && (Items[index].WorkshopId == "music") == _waitingBuyerMusic)
                matching.Add(i);
        }
        if (matching.Count == 0) return false;

        _activeShowcaseIndex = matching[_rng.RandiRange(0, matching.Count - 1)];
        ItemIndex = _showcaseItems[_activeShowcaseIndex];
        _activeTradeGrade = NormalizeRank(_showcaseGrades[_activeShowcaseIndex]);
        _showcaseItems[_activeShowcaseIndex] = -1;
        _showcaseGrades[_activeShowcaseIndex] = "D";
        RefreshShowcases();
        StartBuyerOffer();
        return true;
    }

    private bool CanSupplyWaitingBuyer() => BuyerAvailabilityRules.CanWait(
        EnergyRemaining, _storedItems.Count > 0, Array.Exists(_showcaseItems, index => index >= 0));

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
            _buyerWaitWidget?.SetState(Items[ItemIndex].WorkshopId == "music", _buyerWaitRemaining);
    }

    private void RefreshClosedBuyerOffer()
    {
        if (TradeActive && DayClosed) RefreshOffer();
    }

    private bool CanRerollCurrentBuyer => BuyerPatienceRules.CanReroll(
        TradeActive, DayClosed, RerollsRemaining, _buyerWaitRemaining);

    private int CurrentBuyerPayout => BuyerPatienceRules.TotalPayout(
        OfferedPrice, _buyerWaitRemaining, SpeedBonus(_buyerWaitRemaining), QualityBonus(_activeTradeGrade));

    private void RefreshTotalOfferDisplay()
    {
        if (!TradeActive || ItemIndex < 0) return;
        int speed = SpeedBonus(_buyerWaitRemaining);
        int grade = BuyerPatienceRules.IsExpired(_buyerWaitRemaining) ? 0 : QualityBonus(_activeTradeGrade);
        _lastShownSpeedBonus = speed;
        _offerLabel.Text = GameLocalization.Format("Получим: {0}$", CurrentBuyerPayout);
        _offerLabel.TooltipText = GameLocalization.Language == "en"
            ? $"Offer {OfferedPrice}$ + speed {speed}$ + quality {_activeTradeGrade} {grade}$"
            : $"Цена {OfferedPrice}$ + скорость {speed}$ + качество {_activeTradeGrade} {grade}$";
    }

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
