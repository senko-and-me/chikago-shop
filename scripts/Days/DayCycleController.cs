
using System;
using ChikagoShop.Domain.Days;
using Godot;

public sealed class DayCycleController
{
    private static readonly int TotalDays = EconomyConfigProvider.Load().Days.TotalDays;
	public DaySession Session { get; } = new();
	public bool Started { get; private set; }
	public DayUpgradeSession Upgrades { get; } = new();
	public int CheckpointAtelierLevel { get; private set; }
	public int CheckpointMusicLevel { get; private set; }
	public int CheckpointAtelierPriceLevel { get; private set; }
	public int CheckpointMusicPriceLevel { get; private set; }
	private readonly TradeScene _game;
	private readonly Label _timer;
	private readonly TextureRect _clock;
	private readonly Button _startButton;
	private readonly Texture2D _clockActive;
	private readonly Texture2D _clockExpired;
	private readonly Control _inventoryStrip;
	private int _displayedSeconds = -1;
	private readonly DayTransitionCard _card;
	private readonly DayNightBackground _dayNight;
	private readonly DayAnnouncementOverlay _announcements;
	private double ExpectedDuration => WorkshopItemCatalog.DayDurationSeconds
		+ (Upgrades.Selected == DayUpgrade.Time ? 30 : 0);
	public bool TransitionOpen => _card.IsOpen;

	public DayCycleController(TradeScene game)
	{
		_game = game;
		_card = new DayTransitionCard();
		_card.Chosen += ChooseUpgrade;
		_card.Skipped += SkipUpgrade;
		game.AddChild(_card);
		_timer = new Label
		{
			Name = "DayTimer", HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_timer.AddThemeColorOverride("font_color", Colors.Black);
		_timer.AddThemeConstantOverride("outline_size", 0);
		game.GetNode<Control>("HUD/Root").AddChild(_timer);
		_timer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
		_clockActive = GD.Load<Texture2D>("res://art/ui/day_timer_empty.png");
		_clockExpired = GD.Load<Texture2D>("res://art/ui/day_timer_expired.png");
		_clock = new TextureRect
		{
			Name = "Clock", Texture = _clockActive,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore, ShowBehindParent = true
		};
		_timer.AddChild(_clock);
		MonochromePresentation.PreserveClock(_clock);
		_clock.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_startButton = new Button
		{
			Name = "StartButton", Text = "Начать", Flat = true,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		_startButton.AddThemeColorOverride("font_color", Colors.Black);
		_startButton.AddThemeColorOverride("font_hover_color", Colors.Black);
		_startButton.AddThemeColorOverride("font_focus_color", Colors.Black);
		_startButton.AddToGroup(ButtonFeedbackController.LocalFeedbackGroup);
		foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
			_startButton.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
		_startButton.MouseEntered += UpdateClockFeedback;
		_startButton.MouseExited += UpdateClockFeedback;
		_startButton.FocusEntered += UpdateClockFeedback;
		_startButton.FocusExited += UpdateClockFeedback;
		_startButton.Pressed += StartDay;
		_timer.AddChild(_startButton);
		_startButton.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_inventoryStrip = game.GetNode<Control>("HUD/InventoryUI/ScrollContainer");
		_timer.GetParent<Control>().Resized += LayoutTimer;
		_inventoryStrip.Resized += LayoutTimer;
		_dayNight = new DayNightBackground(game);
		_announcements = new DayAnnouncementOverlay();
		game.AddChild(_announcements);
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public bool CanReceiveBuyers => Started && Session.CanReceiveBuyers && !_game.Failed && !_game.Completed;

	public void StartDay()
	{
		if (Started || Session.IsClosed || Upgrades.Pending || _game.GetTree().Paused || _game.Failed || _game.Completed) return;
		Started = true;
		_announcements.Observe(Session.RemainingSeconds, Session.Duration, false, _game.CurrentDay, TotalDays);
		RefreshTimer();
		_game.SynchronizeDayState();
	}

	public void AddDebugTime()
	{
		if (!OS.IsDebugBuild() || _game.Failed || _game.Completed || Upgrades.Pending) return;
		Session.AddTimeForTesting(30);
		_announcements.Observe(Session.RemainingSeconds, Session.Duration, Session.IsClosed, _game.CurrentDay, TotalDays);
		RefreshTimer();
		UpdateDayNightBackground();
		_game.SynchronizeDayState();
	}

	public void Tick(double delta)
	{
		bool crafting = _game.CraftMiniGameOpen;
		if (!Started || Upgrades.Pending || (_game.GetTree().Paused && !crafting) || _game.Failed || _game.Completed) return;
		bool closed = Session.Tick(delta);
		_announcements.Observe(Session.RemainingSeconds, Session.Duration, Session.IsClosed, _game.CurrentDay, TotalDays);
		UpdateDayNightBackground();
		bool secondChanged = RefreshTimer();
		if (closed) _game.SynchronizeDayState();
		else if (secondChanged) _game.PersistProgress();
	}

	public void EndDay()
	{
		if (_game.Failed || _game.Completed || TransitionOpen || Upgrades.Pending) return;
		if (!Started && !Session.IsClosed) { StartDay(); return; }
		switch (Session.RequestEnd(_game.TradeActive))
		{
			case EndDayAction.Closed:
				_announcements.Observe(Session.RemainingSeconds, Session.Duration, Session.IsClosed, _game.CurrentDay, TotalDays);
				RefreshTimer();
				UpdateDayNightBackground();
				_game.SynchronizeDayState();
				break;
			case EndDayAction.Advance:
				int previousDay = _game.CurrentDay;
				Reset(preserveCategories: true);
				if (previousDay < 7) Upgrades.Offer(previousDay + 1);
				_game.AdvanceToNextDay();
				if (_game.Failed || _game.Completed)
				{
					Upgrades.Reset();
					Session.Close();
					RefreshTimer();
					UpdateDayNightBackground();
					_game.SynchronizeDayState();
				}
				else _card.Open(previousDay, _game.CurrentDay, Upgrades.Options, _game.Score);
				break;
		}
	}

	public void Reset(bool preserveCategories = false)
	{
		_card.Close();
		Upgrades.Reset(preserveCategories);
		Session.Reset(ExpectedDuration);
		Started = false;
		_announcements.BeginDay();
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public void Restore(double? remaining, bool closed, bool pending = false, DayUpgrade selected = DayUpgrade.None,
		int day = 2, int atelierLevel = 0, int musicLevel = 0, int checkpointAtelierLevel = 0, int checkpointMusicLevel = 0,
		int atelierPriceLevel = 0, int musicPriceLevel = 0, int checkpointAtelierPriceLevel = 0, int checkpointMusicPriceLevel = 0,
		bool started = true)
	{
		Upgrades.Restore(pending, selected, day, atelierLevel, musicLevel, atelierPriceLevel, musicPriceLevel);
		CheckpointAtelierLevel = Math.Clamp(checkpointAtelierLevel, 0, 6);
		CheckpointMusicLevel = Math.Clamp(checkpointMusicLevel, 0, 6);
		CheckpointAtelierPriceLevel = Math.Clamp(checkpointAtelierPriceLevel, 0, 6);
		CheckpointMusicPriceLevel = Math.Clamp(checkpointMusicPriceLevel, 0, 6);
		Session.Restore(remaining, closed, ExpectedDuration);
		Started = started || Session.IsClosed;
		if (Started) _announcements.Restore(Session.RemainingSeconds, Session.Duration);
		else _announcements.BeginDay();
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public void ResumeUpgradeChoice()
	{
		if (Upgrades.Pending && !_game.Failed && !_game.Completed && !Session.IsClosed && _game.CurrentDay is >= 2 and <= 7)
			_card.Open(_game.CurrentDay - 1, _game.CurrentDay, Upgrades.Options, _game.Score);
	}

	public void CaptureCheckpointUpgrades()
	{
		CheckpointAtelierLevel = Upgrades.AtelierLevel;
		CheckpointMusicLevel = Upgrades.MusicLevel;
		CheckpointAtelierPriceLevel = Upgrades.AtelierPriceLevel;
		CheckpointMusicPriceLevel = Upgrades.MusicPriceLevel;
	}

	public void RestoreCheckpointUpgrades() => Upgrades.Restore(false, DayUpgrade.None, _game.CurrentDay,
		_game.CurrentDay > 1 ? CheckpointAtelierLevel : 0, _game.CurrentDay > 1 ? CheckpointMusicLevel : 0,
		_game.CurrentDay > 1 ? CheckpointAtelierPriceLevel : 0, _game.CurrentDay > 1 ? CheckpointMusicPriceLevel : 0);

	private void ChooseUpgrade(DayUpgrade upgrade)
	{
		if (!Upgrades.TryPurchase(upgrade, _game.Score, out int balance)) return;
		_game.Score = balance;
		switch (upgrade)
		{
			case DayUpgrade.Cash: _game.Score += Upgrades.CashReward; break;
			case DayUpgrade.Energy: _game.EnergyRemaining++; break;
			case DayUpgrade.Time: Session.ConfigureDuration(ExpectedDuration); break;
		}
		RefreshTimer();
		UpdateDayNightBackground();
		_game.RefreshDayPresentation();
		_card.Close();
	}

	private void SkipUpgrade()
	{
		if (!Upgrades.TrySkip()) return;
		_game.RefreshDayPresentation();
		_card.Close();
	}

	public void SynchronizeRoomDuration()
	{
		Session.ConfigureDuration(ExpectedDuration);
		RefreshTimer();
		UpdateDayNightBackground();
	}

	private void UpdateDayNightBackground() =>
		_dayNight.Update(Session.RemainingSeconds, Session.Duration);

	public void UpdateEndButton(Button button)
	{
		RefreshTimer();
		button.Text = Session.IsClosed ? "Начать следующий день" : Started ? "Завершить день" : "Начать";
		button.Disabled = _game.Failed || _game.Completed || (Session.IsClosed && _game.TradeActive);
		button.TooltipText = Session.IsClosed && _game.TradeActive ? "Сначала завершите сделку" : "";
	}

	public string DisplayStatus(string status) => GameLocalization.Status(
		Session.IsClosed && !_game.Failed && !_game.Completed
			? _game.TradeActive ? "День завершён. Закончите текущую сделку." : "День завершён. Можно начать следующий день."
			: status);

	private bool RefreshTimer()
	{
		LayoutTimer();
		int seconds = (int)Math.Ceiling(Session.RemainingSeconds);
		_startButton.Visible = !Started && !Session.IsClosed && !_game.Failed && !_game.Completed;
		_startButton.Disabled = Upgrades.Pending;
		UpdateClockFeedback();
		_timer.Text = Started || Session.IsClosed ? seconds.ToString("00", System.Globalization.CultureInfo.InvariantCulture) : "";
		bool changed = seconds != _displayedSeconds;
		_displayedSeconds = seconds;
		_clock.Texture = seconds == 0 ? _clockExpired : _clockActive;
		return changed;
	}

	private void UpdateClockFeedback()
	{
		bool active = _startButton.Visible && !_startButton.Disabled &&
			(_startButton.IsHovered() || _startButton.HasFocus());
		_clock.SelfModulate = active ? new Color(0.9f, 0.9f, 0.9f) : Colors.White;
	}

	private void LayoutTimer()
	{
		// Six inventory slots leave less room at narrow window sizes.
		float inventoryLeft = _inventoryStrip.GetGlobalRect().Position.X;
		float size = Mathf.Min(Mathf.Clamp(inventoryLeft - 24, 64, 180) * 1.15f, Mathf.Max(64, inventoryLeft - 12));
		_timer.OffsetLeft = 6;
		_timer.OffsetRight = 6 + size;
		_timer.OffsetTop = -6 - size;
		_timer.OffsetBottom = -6;
		_startButton?.AddThemeFontSizeOverride("font_size", Math.Max(16, (int)(size * 0.16f)));
		int fontSize = Math.Max(18, (int)(size * 0.22f));
		if (_timer.GetThemeFontSize("font_size") != fontSize) _timer.AddThemeFontSizeOverride("font_size", fontSize);
	}
}
