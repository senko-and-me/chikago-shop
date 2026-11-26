
using System;
using ChikagoShop.Domain.Days;
using Godot;

public sealed class DayCycleController
{
	private static int TotalDays => EconomyConfigProvider.Load().Days.TotalDays;
	public DaySession Session { get; } = new(5, 3630);
	public bool Started { get; private set; }
	private bool _advanceWhenClosed;
	private Action _tutorialUpgradeFinished;
	public DayUpgradeSession Upgrades { get; } = new();
	public int CheckpointAtelierLevel { get; private set; }
	public int CheckpointMusicLevel { get; private set; }
	public int CheckpointAtelierPriceLevel { get; private set; }
	public int CheckpointMusicPriceLevel { get; private set; }
	private readonly TradeScene _game;
	private readonly Label _timer;
	private readonly CanvasLayer _clockLayer;
	private readonly TextureRect _clock;
	private readonly Button _startButton;
	private readonly Button _clockAction;
	private readonly Label _endCaption;
	private readonly Texture2D _clockActive;
	private readonly Texture2D _clockExpired;
	private readonly Control _inventoryStrip;
	private int _displayedSeconds = -1;
	public const double TutorialDuration = 180;
	private double? _restoredTutorialRemaining;
	private bool TutorialActive => _game.GetNodeOrNull<FirstTradeTutorial>("FirstTradeTutorial")?.IsActive == true;
	private readonly DayTransitionCard _card;
	private readonly MafiaQuotaNotice _mafia;
	private readonly DayNightBackground _dayNight;
	private readonly DayAnnouncementOverlay _announcements;
	private readonly DayStartGate _actionGate;
	private double ExpectedDuration => TutorialActive ? TutorialDuration : WorkshopItemCatalog.DayDurationSeconds
		+ (_game.MusicWorkshopUnlocked ? 30 : 0);
	public bool TransitionOpen => _card.IsOpen || _mafia.IsOpen;

	public DayCycleController(TradeScene game)
	{
		_game = game;
		_clockLayer = new CanvasLayer { Name = "DayClock", Layer = game.GetNode<CanvasLayer>("HUD").Layer + 1 };
		game.GetNode("HUD").AddChild(_clockLayer);
		var clockRoot = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
		_clockLayer.AddChild(clockRoot);
		clockRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		Upgrades.Rules = DebugBalanceSettings.CardRules;
		_card = GD.Load<PackedScene>("res://scenes/UI/DayTransitionCard.tscn").Instantiate<DayTransitionCard>();
		_card.Chosen += ChooseUpgrade;
		_card.Skipped += SkipUpgrade;
		game.AddChild(_card);
		_mafia = new MafiaQuotaNotice();
		_mafia.Dismissed += () =>
		{
			_game.AcknowledgeMafiaNotice();
			ResumeUpgradeChoice();
		};
		_mafia.RestartRequested += _game.NewGame;
		_mafia.CheckpointRequested += _game.ReturnToCheckpoint;
		_mafia.MainMenuRequested += () =>
		{
			_game.GetTree().Paused = false;
			Error error = _game.GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
			if (error != Error.Ok) GD.PushError($"Не удалось открыть главное меню: {error}");
		};
		game.AddChild(_mafia);
		_timer = new Label
		{
			Name = "DayTimer", HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_timer.AddThemeColorOverride("font_color", Colors.Black);
		_timer.AddThemeConstantOverride("outline_size", 0);
		clockRoot.AddChild(_timer);
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
		_clock.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ButtonDropShadow.Attach(_clock, ButtonDropShadow.Direction.RightAndBottom, offsetAdjustment: new Vector2(-3, 0));
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
		_clockAction = game.GetNode<Button>("HUD/Root/ClockActionButton");
		_clockAction.Reparent(clockRoot);
		_clockAction.Pressed += game.EndDay;
		_clockAction.MouseEntered += UpdateClockFeedback;
		_clockAction.MouseExited += UpdateClockFeedback;
		_clockAction.FocusEntered += UpdateClockFeedback;
		_clockAction.FocusExited += UpdateClockFeedback;
		// Releasing focus on mouse-down cancels the pending BaseButton click before mouse-up.
		_clockAction.Pressed += () => _clockAction.CallDeferred(Control.MethodName.ReleaseFocus);
		_startButton.Pressed += StartDay;
		_timer.AddChild(_startButton);
		_startButton.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_endCaption = new Label
		{
			Name = "EndDayCaption", Visible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
		};
		_endCaption.AddThemeFontOverride("font", _startButton.GetThemeFont("font"));
		_endCaption.AddThemeColorOverride("font_color", Colors.Black);
		_endCaption.AddThemeConstantOverride("outline_size", 0);
		_timer.AddChild(_endCaption);
		_endCaption.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_inventoryStrip = game.GetNode<Control>("HUD/InventoryUI/ScrollContainer");
		_timer.GetParent<Control>().Resized += LayoutTimer;
		_inventoryStrip.Resized += LayoutTimer;
		_dayNight = new DayNightBackground(game);
		_announcements = new DayAnnouncementOverlay();
		game.AddChild(_announcements);
		_actionGate = new DayStartGate { Name = "DayStartGate" };
		game.AddChild(_actionGate);
		Session.Reset(ExpectedDuration);
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public bool CanReceiveBuyers => Started && Session.CanReceiveBuyers && !Upgrades.Pending && !_game.Failed && !_game.Completed;
	public bool RequireStartedDay() => _actionGate.AllowAction();
	public void StartDay()
	{
		if (Started || Session.IsClosed || Upgrades.Pending || _game.QuotaVisitPending || _game.GetTree().Paused || _game.Failed || _game.Completed) return;
		Started = true;
		_actionGate.Dismiss();
		_game.RecalculateInvisibleHand();
		RefreshTimer();
		_game.SynchronizeDayState();
	}

	public void AddDebugTime()
	{
		if (!OS.IsDebugBuild() || _game.Failed || _game.Completed || Upgrades.Pending) return;
		Session.AddTimeForTesting(30);
		RefreshTimer();
		UpdateDayNightBackground();
		_game.SynchronizeDayState();
	}

	public void Tick(double delta)
	{
		bool crafting = _game.CraftMiniGameOpen;
		if (!Started || Upgrades.Pending || (_game.GetTree().Paused && !crafting) || _game.Failed || _game.Completed) return;
		// Expiry closes trading immediately, then advances once the current interaction is finished.
		// Manual closure follows the same transition; never put cards over a craft window.
		if (_advanceWhenClosed && Session.IsClosed && !_game.TradeActive && !crafting)
		{
			EndDay();
			return;
		}
		bool closed;
		if (TutorialActive)
		{
			Session.TickRepeating(delta);
			closed = false;
		}
		else closed = Session.Tick(delta);
		if (closed) _advanceWhenClosed = true;
		UpdateDayNightBackground();
		bool secondChanged = RefreshTimer();
		if (closed) _game.SynchronizeDayState();
		else if (secondChanged) _game.PersistProgress();
	}

	public void EndDay()
	{
		if (_game.Failed || _game.Completed || TransitionOpen || Upgrades.Pending || _game.QuotaVisitPending || _game.CraftMiniGameOpen) return;
		if (!Started && !Session.IsClosed) { StartDay(); return; }
		if (TutorialActive) return;
		switch (Session.RequestEnd(_game.TradeActive))
		{
			case EndDayAction.Closed:
				_advanceWhenClosed = true;
				RefreshTimer();
				UpdateDayNightBackground();
				_game.SynchronizeDayState();
				break;
			case EndDayAction.Advance:
				int previousDay = _game.CurrentDay;
				Reset(preserveCategories: true);
				if (previousDay < TotalDays) Upgrades.Offer(previousDay + 1, _game.MusicWorkshopUnlocked);
				_game.AdvanceToNextDay();
				if (_game.Failed || _game.Completed)
				{
					Upgrades.Reset();
					Session.Close();
					RefreshTimer();
					UpdateDayNightBackground();
					_game.SynchronizeDayState();
				}
				ResumeUpgradeChoice();
				break;
		}
	}

	public void Reset(bool preserveCategories = false)
	{
		_tutorialUpgradeFinished = null;
		_restoredTutorialRemaining = null;
		_actionGate.Dismiss();
		_advanceWhenClosed = false;
		_card.Close();
		_mafia.CloseSilently();
		Upgrades.Reset(preserveCategories);
		Session.Reset(ExpectedDuration);
		Started = false;
		_announcements.Dismiss();
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public void Restore(double? remaining, bool closed, bool pending = false, DayUpgrade selected = DayUpgrade.None,
		int day = 2, int atelierLevel = 0, int musicLevel = 0, int checkpointAtelierLevel = 0, int checkpointMusicLevel = 0,
		int atelierPriceLevel = 0, int musicPriceLevel = 0, int checkpointAtelierPriceLevel = 0, int checkpointMusicPriceLevel = 0,
		bool started = true)
	{
		Upgrades.Restore(pending, selected, day, atelierLevel, musicLevel, atelierPriceLevel, musicPriceLevel,
			_game.MusicWorkshopUnlocked);
		CheckpointAtelierLevel = Math.Clamp(checkpointAtelierLevel, 0, 6);
		CheckpointMusicLevel = Math.Clamp(checkpointMusicLevel, 0, 6);
		CheckpointAtelierPriceLevel = Math.Clamp(checkpointAtelierPriceLevel, 0, 6);
		CheckpointMusicPriceLevel = Math.Clamp(checkpointMusicPriceLevel, 0, 6);
		_restoredTutorialRemaining = remaining;
		Session.Restore(remaining, closed, ExpectedDuration);
		// Recover saves made in the closed-day state, including before this fix.
		_advanceWhenClosed = Session.IsClosed;
		Started = started || Session.IsClosed;
		_announcements.Dismiss();
		RefreshTimer();
		UpdateDayNightBackground();
	}

	private void ShowMafiaNotice() => _mafia.Open(
		_game.CurrentDay >= TotalDays || _game.QuotaStage == 2
			? _game.SecondQuota : _game.FirstQuota,
		_game.Score, !_game.Failed, _game.Completed);

	public void ResumeUpgradeChoice()
	{
		UpdateDayNightBackground();
		if (_game.MafiaNoticePending || _game.Failed || _game.Completed)
		{
			// Old saves may already have dismissed the former text-only notice.
			// Still show an ending screen so the player can restart or return to menu.
			ShowMafiaNotice();
			return;
		}
		if (_game.QuotaVisitPending)
		{
			_game.ResumeQuotaVisit();
			return;
		}
		if (Upgrades.Pending && !Session.IsClosed && _game.CurrentDay >= 2 && _game.CurrentDay <= TotalDays &&
            !_game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").HasPendingUpgradeLesson())
			_card.Open(_game.CurrentDay - 1, _game.CurrentDay, Upgrades.Options, _game.Score);
	}

	public void FinishQuotaVisit() =>
		_announcements.ShowReward(GameLocalization.Format("Получено {0}$", TradeScene.QuotaGift), ResumeUpgradeChoice);

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
		}
		if (FinishTutorialUpgradeChoice()) return;
		RefreshTimer();
		UpdateDayNightBackground();
		_game.RefreshDayPresentation();
		_card.Close();
	}

	private void SkipUpgrade()
	{
		if (!Upgrades.TrySkip()) return;
		if (FinishTutorialUpgradeChoice()) return;
		_game.RefreshDayPresentation();
		_card.Close();
	}

	public bool OpenTutorialUpgradeChoice(Action finished)
	{
		if (!TutorialActive || _game.TradeActive || TransitionOpen || _game.GetTree().Paused) return false;
		_tutorialUpgradeFinished = finished;
		// Reconstruct the same practice hand when a pending choice is loaded.
		Upgrades.Offer(_game.CurrentDay + 1, _game.MusicWorkshopUnlocked, training: true);
		_game.PersistProgress();
		_card.Open(_game.CurrentDay, _game.CurrentDay + 1, Upgrades.Options, _game.Score);
		return _card.IsOpen;
	}

	private bool FinishTutorialUpgradeChoice()
	{
		var finished = _tutorialUpgradeFinished;
		if (finished == null) return false;
		_tutorialUpgradeFinished = null;
		_card.Close();
		Session.Reset(ExpectedDuration);
		Started = false;
		_advanceWhenClosed = false;
		_game.AdvanceTrainingDay();
		RefreshTimer();
		UpdateDayNightBackground();
		finished();
		return true;
	}

	public void SynchronizeTutorialClock(bool restart)
	{
		if (!TutorialActive) return;
		double remaining = TutorialDuration;
		if (!restart && _restoredTutorialRemaining is double saved && double.IsFinite(saved) && saved > 0)
			remaining = Math.Min(saved, TutorialDuration);
		else if (!restart && Session.Duration == TutorialDuration && !Session.IsClosed)
			remaining = Session.RemainingSeconds;
		Session.Restore(remaining, false, TutorialDuration);
		_restoredTutorialRemaining = null;
		_advanceWhenClosed = false;
		_announcements.Dismiss();
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public void SynchronizeRoomDuration()
	{
		Session.ConfigureDuration(ExpectedDuration);
		RefreshTimer();
		UpdateDayNightBackground();
	}

	public void ApplyDebugBalance()
	{
		Upgrades.Rules = DebugBalanceSettings.CardRules;
		SynchronizeRoomDuration();
	}

	private void UpdateDayNightBackground() =>
		_dayNight.Update(_game.QuotaVisitPending ? 0 : Session.RemainingSeconds, Session.Duration);

	public void UpdateEndButton(Button button)
	{
		RefreshTimer();
		button.Text = Session.IsClosed ? "Начать следующий день" : Started ? "Завершить день" : "Начать";
		button.Disabled = _game.Failed || _game.Completed || (Session.IsClosed && _game.TradeActive);
		button.TooltipText = !TutorialActive && Session.IsClosed && _game.TradeActive ? "Сначала завершите сделку" : "";
	}

	public string DisplayStatus(string status) => GameLocalization.Status(
		Session.IsClosed && !_game.Failed && !_game.Completed
			? _game.TradeActive ? "День завершён. Закончите текущую сделку." : "День завершён. Можно начать следующий день."
			: status);

	private bool RefreshTimer()
	{
		_clockLayer.Layer = _game.CraftMiniGameOpen ? 111 : _game.GetNode<CanvasLayer>("HUD").Layer + 1;
		LayoutTimer();
		int seconds = (int)Math.Ceiling(Session.RemainingSeconds);
		_startButton.Visible = !Started && !Session.IsClosed && !_game.Failed && !_game.Completed;
		_startButton.Disabled = Upgrades.Pending;
		_clockAction.Visible = (Started || Session.IsClosed) && !_game.Failed && !_game.Completed;
		_clockAction.Disabled = _game.CraftMiniGameOpen || Upgrades.Pending || Session.IsClosed && _game.TradeActive;
		_clockAction.TooltipText = !TutorialActive && Session.IsClosed ? "Начать следующий день" : "";
		UpdateClockFeedback();
		bool changed = seconds != _displayedSeconds;
		_displayedSeconds = seconds;
		_clock.Texture = seconds == 0 ? _clockExpired : _clockActive;
		return changed;
	}

	private void UpdateClockFeedback()
	{
		bool active = _startButton.Visible && !_startButton.Disabled &&
			(_startButton.IsHovered() || _startButton.HasFocus()) ||
			_clockAction.Visible && !_clockAction.Disabled && (_clockAction.IsHovered() || _clockAction.HasFocus());
		_clock.SelfModulate = active ? new Color(0.9f, 0.9f, 0.9f) : Colors.White;
		if (_endCaption == null) return;
		bool showEnd = !TutorialActive && _clockAction.Visible && !_clockAction.Disabled && Started &&
			!Session.IsClosed && Session.RemainingSeconds > 0 &&
			(_clockAction.IsHovered() || _clockAction.HasFocus());
		_endCaption.Visible = showEnd;
		string caption = GameLocalization.Text("Завершить день");
		_endCaption.Text = caption.Replace(" ", "\n");
		int baseSize = _startButton.GetThemeFontSize("font_size");
		float wordWidth = 1;
		foreach (string word in caption.Split(' '))
			wordWidth = Mathf.Max(wordWidth, _startButton.GetThemeFont("font").GetStringSize(word, fontSize: baseSize).X);
		// The painted clock face occupies only the middle of its transparent texture.
		int fittedSize = Math.Max(8, Math.Min(baseSize, (int)(baseSize * _timer.Size.X * 0.46f / wordWidth)));
		_endCaption.AddThemeFontSizeOverride("font_size", fittedSize);
		_timer.Text = showEnd || !Started && !Session.IsClosed ? "" :
			((int)Math.Ceiling(Session.RemainingSeconds)).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
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
		_clockAction.Position = _timer.Position;
		_clockAction.Size = _timer.Size;
		if (_endCaption != null)
		{
			_endCaption.OffsetLeft = size * 0.24f;
			_endCaption.OffsetRight = -size * 0.24f;
			_endCaption.OffsetTop = size * 0.24f;
			_endCaption.OffsetBottom = -size * 0.24f;
		}
		_startButton?.AddThemeFontSizeOverride("font_size", Math.Max(16, (int)(size * 0.16f)));
		int fontSize = Math.Max(18, (int)(size * 0.22f));
		if (_timer.GetThemeFontSize("font_size") != fontSize) _timer.AddThemeFontSizeOverride("font_size", fontSize);
	}
}
