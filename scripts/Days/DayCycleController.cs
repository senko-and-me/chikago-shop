
using System;
using ChikagoShop.Domain.Days;
using Godot;

public sealed class DayCycleController
{
	public DaySession Session { get; } = new();
	public DayUpgradeSession Upgrades { get; } = new();
	public int CheckpointAtelierLevel { get; private set; }
	public int CheckpointMusicLevel { get; private set; }
	public int CheckpointAtelierPriceLevel { get; private set; }
	public int CheckpointMusicPriceLevel { get; private set; }
	private readonly TradeScene _game;
	private readonly Label _timer;
	private int _displayedSeconds = -1;
	private readonly DayTransitionCard _card;
	private readonly DayNightBackground _dayNight;
	private double ExpectedDuration => (_game.MusicWorkshopUnlocked
		? DaySession.TwoRoomDurationSeconds : DaySession.DurationSeconds)
		+ (Upgrades.Selected == DayUpgrade.Time ? 30 : 0);
	public bool TransitionOpen => _card.IsOpen;

	public DayCycleController(TradeScene game)
	{
		_game = game;
		_card = new DayTransitionCard();
		_card.Chosen += ChooseUpgrade;
		game.AddChild(_card);
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
		_dayNight = new DayNightBackground(game);
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public bool CanReceiveBuyers => Session.CanReceiveBuyers && !_game.Failed && !_game.Completed;

	public void Tick(double delta)
	{
		bool crafting = _game.CraftMiniGameOpen;
		if (Upgrades.Pending || (_game.GetTree().Paused && !crafting) || _game.Failed || _game.Completed) return;
		bool closed = Session.Tick(delta);
		UpdateDayNightBackground();
		bool secondChanged = RefreshTimer();
		if (closed) _game.SynchronizeDayState();
		else if (secondChanged) _game.PersistProgress();
	}

	public void EndDay()
	{
		if (_game.Failed || _game.Completed || TransitionOpen || Upgrades.Pending) return;
		switch (Session.RequestEnd(_game.TradeActive))
		{
			case EndDayAction.Closed:
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
				else _card.Open(previousDay, _game.CurrentDay, Upgrades.Options);
				break;
		}
	}

	public void Reset(bool preserveCategories = false)
	{
		_card.Close();
		Upgrades.Reset(preserveCategories);
		Session.Reset(ExpectedDuration);
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public void Restore(double? remaining, bool closed, bool pending = false, DayUpgrade selected = DayUpgrade.None,
		int day = 2, int atelierLevel = 0, int musicLevel = 0, int checkpointAtelierLevel = 0, int checkpointMusicLevel = 0,
		int atelierPriceLevel = 0, int musicPriceLevel = 0, int checkpointAtelierPriceLevel = 0, int checkpointMusicPriceLevel = 0)
	{
		Upgrades.Restore(pending, selected, day, atelierLevel, musicLevel, atelierPriceLevel, musicPriceLevel);
		CheckpointAtelierLevel = Math.Clamp(checkpointAtelierLevel, 0, 6);
		CheckpointMusicLevel = Math.Clamp(checkpointMusicLevel, 0, 6);
		CheckpointAtelierPriceLevel = Math.Clamp(checkpointAtelierPriceLevel, 0, 6);
		CheckpointMusicPriceLevel = Math.Clamp(checkpointMusicPriceLevel, 0, 6);
		Session.Restore(remaining, closed, ExpectedDuration);
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public void ResumeUpgradeChoice()
	{
		if (Upgrades.Pending && !_game.Failed && !_game.Completed && !Session.IsClosed && _game.CurrentDay is >= 2 and <= 7)
			_card.Open(_game.CurrentDay - 1, _game.CurrentDay, Upgrades.Options);
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
		if (!Upgrades.TryChoose(upgrade)) return;
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
