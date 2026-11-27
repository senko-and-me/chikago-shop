using System;
using Godot;

// One selected attempt owns both the debit and the result. UI cannot roll a different reward.
public sealed class CraftCoordinator
{
    private readonly TradeScene _game;
    public CraftCatalog Catalog { get; }
    public bool IsCatalogOpen => Catalog.IsOpen;
    public string Workshop { get; private set; }
    public int SelectedIndex { get; private set; } = -1;
    private int _paidPosition = -1;
    private bool _started;
    private bool _finished;

    public CraftCoordinator(TradeScene game)
    {
        _game = game;
        Catalog = GD.Load<PackedScene>("res://scenes/UI/CraftCatalog.tscn").Instantiate<CraftCatalog>();
        Catalog.Coordinator = this;
        game.AddChild(Catalog);
        game.NewGameStarted += Reset;
    }

    private EmbroideryGame Host(string workshop) => _game.GetNode<EmbroideryGame>(
        workshop == "music" ? "PolishingGame" : "EmbroideryGame");

    public string UnavailableReason(int index)
    {
        if (index < 0 || index >= WorkshopItemCatalog.Items.Count) return "Предмет недоступен";
        var item = WorkshopItemCatalog.Items[index];
        if (_game.Failed || _game.Completed || _game.DayClosed || !_game.DayStarted) return "Магазин закрыт";
        if (item.WorkshopId == "music" && !_game.MusicWorkshopUnlocked) return "Мастерская закрыта";
        if (_game.Score < WorkshopItemCatalog.ItemCraftCost(index)) return "Недостаточно денег";
        if (!_game.HasFreeDisplay(item.WorkshopId)) return "Нет свободных витрин";
        return "";
    }

    private bool Covered => _game.GetNode<PauseMenu>("PauseMenu").IsOpen ||
        _game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsSkipConfirmationOpen;

    public void OpenCatalog(string workshop)
    {
        if (workshop is not ("atelier" or "music") || !_game.RequireStartedDay() || Covered ||
            _game.GetTree().Paused || _game.CraftMiniGameOpen || IsCatalogOpen ||
            _game.Failed || _game.Completed || _game.DayClosed || _game.DayTransitionOpen ||
            (workshop == "music" && !_game.MusicWorkshopUnlocked)) return;
        Workshop = workshop;
        SelectedIndex = -1;
        Catalog.Open(workshop);
    }

    public bool Select(string kind)
    {
        int index = WorkshopItemCatalog.IndexOf(kind);
        if (!IsCatalogOpen || Covered || index < 0 ||
            WorkshopItemCatalog.Items[index].WorkshopId != Workshop || UnavailableReason(index) != "") return false;
        SelectedIndex = index;
        _started = _finished = false;
        _paidPosition = -1;
        Catalog.HideForSelection();
        Host(Workshop).OpenItem(index);
        if (!Host(Workshop).IsOpen) { ReturnToCatalog(); return false; }
        return true;
    }

    public bool TryStart(int index)
    {
        if (_started || _finished || Covered || SelectedIndex != index || index < 0 ||
            UnavailableReason(index) != "" || !Host(Workshop).IsReadyToStart) return false;
        // State flips before notifications/save so duplicate input cannot re-enter the debit.
        _started = true;
        _paidPosition = _game.BeginSelectedCraft(index);
        if (_paidPosition >= 0) return true;
        _started = false;
        return false;
    }

    public void Complete(string rank)
    {
        if (!_started || _finished || _paidPosition < 0 ||
            Host(Workshop).ActiveSession.State != ChikagoShop.Domain.Crafting.EmbroideryState.Finished) return;
        _finished = true;
        _game.CompleteSelectedCraft(_paidPosition, SelectedIndex, rank);
    }

    public double Duration(string workshop) => _game.DayUpgrades.CraftDuration(workshop,
        DebugBalanceSettings.Number(workshop == "music" ? "craft.music.seconds" : "craft.atelier.seconds"));

    public void ReturnToCatalog()
    {
        if (_started && !_finished || Covered) return;
        SelectedIndex = -1;
        _started = _finished = false;
        _paidPosition = -1;
        Catalog.Open(Workshop);
    }

    public void CancelPreparation()
    {
        SelectedIndex = -1;
        _paidPosition = -1;
        _started = _finished = false;
    }

    private void Reset()
    {
        Catalog.CloseForReset();
        CancelPreparation();
        Workshop = null;
    }
}
