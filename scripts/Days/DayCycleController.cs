using System;
using ChikagoShop.Domain.Days;
using Godot;

// Godot presentation/orchestration; clock and closing policies live in the domain.
public sealed class DayCycleController
{
    public DaySession Session { get; } = new();
    private readonly TradeScene _game;
    private readonly Label _timer;
    private int _displayedSeconds = -1;

    public DayCycleController(TradeScene game)
    {
        _game = game;
        _timer = new Label
        {
            Name = "DayTimer", HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _timer.AddThemeFontSizeOverride("font_size", 36);
        _timer.AddThemeColorOverride("font_color", Colors.White);
        _timer.AddThemeColorOverride("font_outline_color", Colors.Black);
        _timer.AddThemeConstantOverride("outline_size", 8);
        game.GetNode<Control>("HUD/Root").AddChild(_timer);
        _timer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _timer.OffsetLeft = -100;
        _timer.OffsetRight = 100;
        _timer.OffsetTop = 20;
        _timer.OffsetBottom = 72;
        RefreshTimer();
    }

    public bool CanReceiveBuyers => Session.CanReceiveBuyers && !_game.Failed && !_game.Completed;

    public void Tick(double delta)
    {
        bool crafting = _game.CraftMiniGameOpen;
        if ((_game.GetTree().Paused && !crafting) || _game.Failed || _game.Completed) return;
        bool closed = Session.Tick(delta);
        bool secondChanged = RefreshTimer();
        if (closed) _game.SynchronizeDayState();
        else if (secondChanged) _game.PersistProgress();
    }

    public void EndDay()
    {
        if (_game.Failed || _game.Completed) return;
        switch (Session.RequestEnd(_game.TradeActive))
        {
            case EndDayAction.Closed:
                RefreshTimer();
                _game.SynchronizeDayState();
                break;
            case EndDayAction.Advance:
                Reset();
                _game.AdvanceToNextDay();
                if (_game.Failed || _game.Completed)
                {
                    Session.Close();
                    RefreshTimer();
                    _game.SynchronizeDayState();
                }
                break;
        }
    }

    public void Reset()
    {
        Session.Reset();
        RefreshTimer();
    }

    public void Restore(double? remaining, bool closed)
    {
        Session.Restore(remaining, closed);
        RefreshTimer();
    }

    public void UpdateEndButton(Button button)
    {
        button.Text = Session.IsClosed ? "Начать следующий день" : "Завершить день";
        button.Disabled = _game.Failed || _game.Completed || (Session.IsClosed && _game.TradeActive);
        button.TooltipText = Session.IsClosed && _game.TradeActive ? "Сначала завершите сделку" : "";
    }

    public string DisplayStatus(string status) => GameLocalization.Status(
        Session.IsClosed && !_game.Failed && !_game.Completed
            ? _game.TradeActive ? "День завершён. Закончите текущую сделку." : "День завершён. Можно начать следующий день."
            : status);

    private bool RefreshTimer()
    {
        int seconds = (int)Math.Ceiling(Session.RemainingSeconds);
        if (seconds == _displayedSeconds) return false;
        _displayedSeconds = seconds;
        _timer.Text = FormattableString.Invariant($"{seconds / 60:00}:{seconds % 60:00}");
        return true;
    }
}
