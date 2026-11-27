
using System;
using ChikagoShop.Domain.Crafting;
using Godot;

public partial class EmbroideryGame : CanvasLayer
{
	public EmbroiderySession Session { get; private set; } = new();
	public PolishingSession Polishing { get; private set; } = null!;
	[Export] public bool IsPolishing { get; set; }
	public ICraftSession ActiveSession => IsPolishing ? Polishing : Session;
	private PolishingSurface _polishingSurface = null!;
	private Sprite2D _polishingCursor = null!;
	// Small pointer-sized cloth; the texture is always shown during polishing.
	private const float PolishingCursorSize = 112f; // Longest edge in viewport pixels.
	private Input.MouseModeEnum _previousCursorMode = Input.MouseModeEnum.Visible;
	private bool _polishingCursorHidden;
	public bool IsOpen => _surface != null && _surface.Visible;
	internal bool IsReadyToStart => IsOpen && ActiveSession.State == EmbroideryState.Ready;
	internal Button StartButton => _action;
	internal event Action Started;
	private EmbroiderySurface _surface = null!;
	private Label _title = null!, _hint = null!, _quality = null!;
	private Label _qualityHeading = null!;
	private readonly Control[] _statCards = new Control[3];
	private readonly Label[] _statValues = new Label[3];
	private readonly Label[] _statHeadings = new Label[3];
	private Button _action = null!, _cancel = null!;
	private bool _ownsPause;
	public int? PreparedItemIndex { get; private set; }
	private bool _preparedItemClaimed;
	private bool _qualityRecorded;
	private TradeScene _game = null!;
	private PauseMenu _pause = null!;
	private FirstTradeTutorial _tutorial = null!;
	private bool Covered => _pause.IsOpen || _tutorial.IsSkipConfirmationOpen;
	internal bool PolishingInputAllowed => IsPolishing && IsOpen && !Covered && ActiveSession.State == EmbroideryState.Playing;

	public override void _Ready()
	{
		_game = GetParent<TradeScene>();
		_pause = _game.GetNode<PauseMenu>("PauseMenu");
		_tutorial = _game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
		_game.NewGameStarted += ResetPreparedItem;
		_surface = GetNode<EmbroiderySurface>("Surface");
		_surface.Game = this;
		_title = _surface.GetNode<Label>("Panel/Title");
		_hint = _surface.GetNode<Label>("Panel/Hint");
		_quality = _surface.GetNode<Label>("Panel/Quality");
		_qualityHeading = _surface.GetNode<Label>("Panel/QualityHeading");
		string[] names = ["Time", "Stitches", "Misses"];
		for (int i = 0; i < names.Length; i++)
		{
			_statCards[i] = _surface.GetNode<Control>("Panel/Stat" + names[i]);
			_statHeadings[i] = _statCards[i].GetNode<Label>("Heading");
			_statValues[i] = _statCards[i].GetNode<Label>("Value");
		}
		_action = _surface.GetNode<Button>("Panel/Buttons/Action");
		_cancel = _surface.GetNode<Button>("Panel/Buttons/Cancel");
		_action.Pressed += Activate;
		_cancel.Pressed += Close;
		_polishingSurface = _surface.GetNode<PolishingSurface>("Polishing");
		_polishingSurface.Visible = IsPolishing;
		_polishingSurface.DrawClothAtPointer = false;
		_polishingCursor = _surface.GetNode<Sprite2D>("PolishingCursor");
		Texture2D cloth = _polishingCursor.Texture;
		_polishingCursor.Scale = Vector2.One * PolishingCursorSize / Mathf.Max(cloth.GetWidth(), cloth.GetHeight());
	}

	public void Open() => _game.Crafting.OpenCatalog(IsPolishing ? "music" : "atelier");

	public void OpenItem(int index)
	{
		string workshop = IsPolishing ? "music" : "atelier";
		if (IsOpen || index < 0 || index >= WorkshopItemCatalog.Items.Count ||
			WorkshopItemCatalog.Items[index].WorkshopId != workshop ||
			_game.Crafting.SelectedIndex != index) return;
		PreparedItemIndex = index;
		_preparedItemClaimed = false;
		_qualityRecorded = false;
		string kind = TradeScene.GetItemKind(index);
		double duration = _game.Crafting.Duration(IsPolishing ? "music" : "atelier");
		if (IsPolishing) Polishing = _polishingSurface.Begin(kind, duration);
		else Session = new EmbroiderySession(kind, duration);
		_ownsPause = true;
		GetTree().Paused = true;
		_surface.Show();
		if (IsPolishing)
		{
			_previousCursorMode = Input.MouseMode;
			_polishingCursorHidden = true;
		}
		_game.RefreshDayPresentation();
		Refresh();
		_action.GrabFocus();
	}

	public void Activate()
	{
		if (!IsOpen || Covered)
			return;

		if (ActiveSession.State == EmbroideryState.Ready)
		{
			if (GetParent<TradeScene>().DayClosed)
			{
				Close();
				return;
			}

			if (!PreparedItemIndex.HasValue || !_game.Crafting.TryStart(PreparedItemIndex.Value)) return;
			ActiveSession.Start();
			_preparedItemClaimed = true;
			Started?.Invoke();
		}
		else if (ActiveSession.State == EmbroideryState.Finished)
		{
			string rank = ActiveSession.Rank;
			bool collected = _preparedItemClaimed && _qualityRecorded;
			Close();
			if (collected) SoundEffects.Instance?.PlayCraftResult(rank);
			return;
		}

		Refresh();
	}

	public void FinishForTesting(bool successful)
	{
		if (!OS.IsDebugBuild() || !IsOpen || Covered || ActiveSession.State == EmbroideryState.Finished) return;
		if (ActiveSession.State == EmbroideryState.Ready) Activate();
		if (!IsOpen || ActiveSession.State != EmbroideryState.Playing) return;
		ActiveSession.FinishForTesting(successful);
		CommitQualityIfFinished();
		Refresh();
	}

	internal void Close()
	{
		if (!IsOpen || ActiveSession.State == EmbroideryState.Playing || Covered) return;
		bool ready = ActiveSession.State == EmbroideryState.Ready;
		Dismiss();
		if (ready) _game.Crafting.ReturnToCatalog();
	}

	internal void Dismiss()
	{
		if (!IsOpen) return;
		_surface.Hide();
		RestoreMouseCursor();

		if (_ownsPause && !_pause.IsOpen)
			GetTree().Paused = false;

		_ownsPause = false;
		if (_game.DayClosed) _game.SynchronizeDayState();
		else _game.RefreshDayPresentation();
	}

	internal void SuspendPolishingInput()
	{
		if (!IsPolishing || !IsOpen) return;
		_polishingSurface.EndPointer();
		_polishingCursor.Hide();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void RestoreMouseCursor()
	{
		if (!_polishingCursorHidden) return;
		_polishingCursorHidden = false;
		if (GodotObject.IsInstanceValid(_polishingCursor)) _polishingCursor.Hide();
		Input.MouseMode = _previousCursorMode;
	}

	private void RefreshPolishingCursor()
	{
		if (!IsPolishing || !_polishingCursorHidden) return;
		bool playing = ActiveSession.State == EmbroideryState.Playing;
		_polishingCursor.Position = _surface.GetLocalMousePosition();
		_polishingCursor.Visible = playing;
		Input.MouseModeEnum mode = playing ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
		if (Input.MouseMode != mode) Input.MouseMode = mode;
	}

	public override void _ExitTree()
	{
		RestoreMouseCursor();
		if (GodotObject.IsInstanceValid(_game))
			_game.NewGameStarted -= ResetPreparedItem;
		if (_ownsPause)
			GetTree().Paused = false;
	}

	public void Reset()
	{
		if (_surface != null) _surface.Hide();
		RestoreMouseCursor();
		if (_ownsPause && !_pause.IsOpen) GetTree().Paused = false;
		_ownsPause = false;
		ResetPreparedItem();
	}

	private void ResetPreparedItem()
	{
		if (_surface != null && _surface.Visible)
		{
			_surface.Hide();
			RestoreMouseCursor();
			if (_ownsPause && !_pause.IsOpen) GetTree().Paused = false;
			_ownsPause = false;
		}
		PreparedItemIndex = null;
		_preparedItemClaimed = false;
		_qualityRecorded = false;
	}

	public override void _Process(double delta)
	{
		if (!IsOpen)
			return;

		if (Covered) return;

		if (ActiveSession.State == EmbroideryState.Playing) _game.TickCraftDay(delta);

		if (_game.DayClosed && ActiveSession.State == EmbroideryState.Ready)
		{
			Close();
			return;
		}

		ActiveSession.Tick(delta);
		CommitQualityIfFinished();
		Refresh();
	}

	private void CommitQualityIfFinished()
	{
		if (_qualityRecorded || !_preparedItemClaimed || ActiveSession.State != EmbroideryState.Finished)
			return;
		_qualityRecorded = true;
		_game.Crafting.Complete(ActiveSession.Rank);
	}

	private void Refresh()
	{
		bool ready = ActiveSession.State == EmbroideryState.Ready;
		bool playing = ActiveSession.State == EmbroideryState.Playing;

		_title.Text = GameLocalization.Text(
			ready || playing
				? (IsPolishing ? "Полировка" : "Вышивка")
				: ActiveSession.Complete ? "Готово" : "Время вышло");
		if ((ready || playing) && PreparedItemIndex.HasValue)
			_title.Text += ": " + TradeScene.GetItemName(PreparedItemIndex.Value);

		_statValues[0].Text =
			FormattableString.Invariant($"{ActiveSession.RemainingSeconds:0.0}");

		_statValues[1].Text = IsPolishing
			? $"{Math.Round(Polishing.Progress * 100)}%"
			: $"{Session.Stitches} / {Session.Points.Count}";

		_statValues[2].Text = ActiveSession.Misses.ToString();
		_statHeadings[0].Text = GameLocalization.Text("ВРЕМЯ");
		_statHeadings[1].Text =
			GameLocalization.Text(IsPolishing ? "ОЧИЩЕНО" : "СТЕЖКИ");
		_statHeadings[2].Text = GameLocalization.Text("ПРОМАХИ");

		string instruction = IsPolishing && (ready || playing) ? "Зажми и протирай туда-сюда" :
			!IsPolishing && _tutorial.IsActive && playing ? "Нажимайте на подсвечивающиеся точки по очереди." :
			!IsPolishing && playing ? "Нажимай на светящиеся точки" : "";
		_hint.Text = GameLocalization.Text(instruction);

		_quality.Text = $"{ActiveSession.Quality}%";
		_action.Text = GameLocalization.Text(ready ? "Начать" : "Забрать");
		_action.Visible = !playing;
		_cancel.Visible = ready;
		_qualityHeading.Text = GameLocalization.Text("КАЧЕСТВО");
		Control panel = _surface.GetNode<Control>("Panel");
		Rect2 panelRect = _surface.PanelRect;
		panel.Position = panelRect.Position;
		panel.Size = panelRect.Size;

		if (IsPolishing)
		{
			Place(_polishingSurface, _surface.FabricRect);
			_polishingSurface.RefreshArtwork();
			RefreshPolishingCursor();
		}

		_surface.QueueRedraw();
	}

	internal static StyleBoxTexture CreateArtworkFrame()
	{
		Texture2D texture = GD.Load<Texture2D>("res://art/ui/camera_frame.png");
		using var image = texture.GetImage();
		using var cropped = image.GetRegion(image.GetUsedRect());
		cropped.Resize(200, 200, Image.Interpolation.Lanczos);
		var frame = new StyleBoxTexture { Texture = ImageTexture.CreateFromImage(cropped), DrawCenter = false };
		foreach (var side in new[] { Side.Left, Side.Top, Side.Right, Side.Bottom })
			frame.SetTextureMargin(side, 16);
		return frame;
	}

	private static void Place(Control control, Rect2 rect)
	{
		control.Position = rect.Position;
		control.Size = rect.Size;
	}

}
