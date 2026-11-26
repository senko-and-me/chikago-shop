
using System;
using ChikagoShop.Domain.Crafting;
using Godot;

public partial class EmbroideryGame : CanvasLayer
{
	private readonly MiniGameVariantPicker _variants = new(4);
	public EmbroiderySession Session { get; private set; } = new();
	public PolishingSession Polishing { get; private set; } = null!;
	public bool IsPolishing { get; }
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
	private readonly Action _acquire;
	private bool _ownsPause;
	private bool? _primaryAction;
    private Texture2D _paperButton = null!, _inkButton = null!;
    private StyleBoxTexture _focusFrame = null!;
	public int? PreparedItemIndex { get; private set; }
	private string _preparedInstrument;
	private bool _preparedItemClaimed;
	private bool _qualityRecorded;
	private TradeScene _game = null!;
    private PauseMenu _pause = null!;
    private FirstTradeTutorial _tutorial = null!;
    private bool Covered => _pause.IsOpen || _tutorial.IsSkipConfirmationOpen;

	public EmbroideryGame(Action acquire, bool polishing = false)
	{
		_acquire = acquire;
		IsPolishing = polishing;
	}

	public override void _Ready()
	{
		Name = IsPolishing ? "PolishingGame" : "EmbroideryGame";
		Layer = 110;
		ProcessMode = ProcessModeEnum.Always;
		_game = GetParent<TradeScene>();
        _pause = _game.GetNode<PauseMenu>("PauseMenu");
        _tutorial = _game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
		_game.NewGameStarted += ResetPreparedItem;

		_surface = new EmbroiderySurface
		{
			Name = "Surface",
			Game = this,
			Visible = false
		};
		AddChild(_surface);
		_surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _surface.Theme = GD.Load<Theme>("res://themes/game_theme.tres");
        _paperButton = GD.Load<Texture2D>("res://art/ui/panel_small.png");
        _inkButton = LoadArtwork("res://art/ui/panel_inverted.png");
        _focusFrame = CreateArtworkFrame();
        _focusFrame.ModulateColor = new Color("727272");
        foreach (var side in new[] { Side.Left, Side.Top, Side.Right, Side.Bottom })
            _focusFrame.SetExpandMargin(side, 2);

		if (IsPolishing)
		{
			_polishingSurface = new PolishingSurface
			{
				Name = "Polishing",
				MouseFilter = Control.MouseFilterEnum.Stop,
				ClipContents = true
			};
			_surface.AddChild(_polishingSurface);
		}

		_title = AddLabel("Title", 28);
		_qualityHeading = AddLabel("QualityHeading", 14);
		_title.HorizontalAlignment = HorizontalAlignment.Left;

		string[] statNames = ["Time", "Stitches", "Misses"];
		string[] statTitles = ["ВРЕМЯ", "СТЕЖКИ", "ПРОМАХИ"];

		for (int i = 0; i < _statCards.Length; i++)
		{
			var card = new Panel
			{
				Name = "Stat" + statNames[i],
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
            card.AddThemeStyleboxOverride("panel", new StyleBoxTexture { Texture = _paperButton });
			_surface.AddChild(card);
			_statCards[i] = card;

			var heading = new Label
			{
				Name = "Heading",
				Text = statTitles[i],
				Position = new Vector2(14, 7),
				Size = new Vector2(76, 18),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			heading.AddThemeFontSizeOverride("font_size", 14);
			heading.AddThemeColorOverride("font_color", Colors.Black);
			heading.AddThemeConstantOverride("outline_size", 0);
			card.AddChild(heading);
			_statHeadings[i] = heading;

			var value = new Label
			{
				Name = "Value",
				Position = new Vector2(14, 25),
				Size = new Vector2(76, 28),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			value.AddThemeFontSizeOverride("font_size", 22);
			value.AddThemeColorOverride("font_color", Colors.Black);
			value.AddThemeConstantOverride("outline_size", 0);
			card.AddChild(value);
			_statValues[i] = value;
		}

		_hint = AddLabel("Hint", 16);
		_quality = AddLabel("Quality", 25);
		_action = new Button
		{
			Name = "Action",
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		_cancel = new Button { Name = "Cancel", Text = "Назад" };
		_surface.AddChild(_action);
		_surface.AddChild(_cancel);

        StyleButton(_cancel, primary: false);

		_action.Pressed += Activate;
		_cancel.Pressed += Close;

		if (IsPolishing)
		{
			// Use the cloth while playing; ready/result buttons keep the native UI pointer.
			_polishingSurface.DrawClothAtPointer = false;
			// Sprite2D uses an explicit scale calculated from the source artwork.
			// TextureRect's old centering could render the native 2048px texture
            // despite a 32x32 minimum size; that caused the giant cloth bug.
            Texture2D cloth = GD.Load<Texture2D>("res://art/minigames/polishing_cloth.png");
            float clothScale = PolishingCursorSize / Mathf.Max(cloth.GetWidth(), cloth.GetHeight());
            _polishingCursor = new Sprite2D
            {
                Name = "PolishingCursor",
                Texture = cloth,
                Centered = true,
                Scale = Vector2.One * clothScale,
                ZIndex = 100,
                Visible = false
            };
            _surface.AddChild(_polishingCursor);
        }
    }

    private Label AddLabel(string name, int size)
    {
        var label = new Label
        {
            Name = name,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 0);
        label.AddThemeFontSizeOverride("font_size", size);
        _surface.AddChild(label);
        return label;
    }

    public void Open()
    {
        var game = GetParent<TradeScene>();
        if (!game.RequireStartedDay()) return;

        if (IsOpen || GetTree().Paused || game.Failed || game.Completed ||
            game.EnergyRemaining <= 0 ||
            game.Score < WorkshopItemCatalog.CraftCost(IsPolishing ? "music" : "atelier") ||
            (IsPolishing && !game.MusicWorkshopUnlocked))
            return;

        game.PersistProgress();
        _qualityRecorded = false;

        if (IsPolishing)
        {
            if (!PreparedItemIndex.HasValue || _preparedItemClaimed)
            {
                var prepared = game.PreparePolishingItem();
                PreparedItemIndex = prepared.ItemIndex;
                _preparedInstrument = prepared.InstrumentKind;
                _preparedItemClaimed = false;
            }
            Polishing = _polishingSurface.Begin(_preparedInstrument,
                DebugBalanceSettings.Number("craft.music.seconds"));
        }
        else
        {
            Session = new EmbroiderySession(_variants.Next(Math.Min(GD.Randf(), 0.99999994f)),
                DebugBalanceSettings.Number("craft.atelier.seconds"));
        }

        _ownsPause = true;
        GetTree().Paused = true;
        _surface.Show();
        if (IsPolishing)
        {
            _previousCursorMode = Input.MouseMode;
            _polishingCursorHidden = true;
        }
        game.RefreshDayPresentation();
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

            ActiveSession.Start();
            _acquire();
            _preparedItemClaimed = true;
            Started?.Invoke();
        }
        else if (ActiveSession.State == EmbroideryState.Finished)
        {
            Close();
            return;
        }

        Refresh();
    }

    public void FinishForTesting(bool successful)
    {
        if (!OS.IsDebugBuild() || !IsOpen || Covered || ActiveSession.State == EmbroideryState.Finished) return;
        if (ActiveSession.State == EmbroideryState.Ready) Activate();
        if (!IsOpen) return;
        ActiveSession.FinishForTesting(successful);
        CommitQualityIfFinished();
        Refresh();
    }

    internal void Close()
    {
        if (!IsOpen || ActiveSession.State == EmbroideryState.Playing || Covered) return;
        Dismiss();
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

    private void ResetPreparedItem()
    {
        PreparedItemIndex = null;
        _preparedInstrument = null;
        _preparedItemClaimed = false;
        _qualityRecorded = false;
    }

    public override void _Process(double delta)
    {
        if (!IsOpen)
            return;

        if (Covered) return;

        _game.TickCraftDay(delta);

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
        _game.FinalizeLatestCraftQuality(ActiveSession.Rank);
    }

    private void Refresh()
    {
        bool ready = ActiveSession.State == EmbroideryState.Ready;
        bool playing = ActiveSession.State == EmbroideryState.Playing;

        _title.Text = GameLocalization.Text(
            ready || playing
                ? (IsPolishing ? "Полировка" : "Вышивка")
                : ActiveSession.Complete ? "Готово" : "Время вышло");

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
        StyleAction(ready);

        Rect2 panel = _surface.PanelRect;
        _qualityHeading.Text = GameLocalization.Text("КАЧЕСТВО");
        Place(_title, new Rect2(panel.Position + new Vector2(24, 24),
            new Vector2(panel.Size.X - 410, 60)));
        float statsX = panel.End.X - 364;
        for (int i = 0; i < _statCards.Length; i++)
            Place(_statCards[i], new Rect2(new Vector2(statsX + i * 116, panel.Position.Y + 30),
                new Vector2(104, 60)));

        Rect2 track = _surface.QualityTrackRect;
        Place(_qualityHeading, new Rect2(new Vector2(track.Position.X - 8, _surface.FabricRect.Position.Y),
            new Vector2(88, 20)));
        Place(_quality, new Rect2(new Vector2(track.Position.X - 8, track.End.Y + 8),
            new Vector2(88, 30)));
        Place(_hint, new Rect2(new Vector2(panel.Position.X + 24, panel.End.Y - (playing ? 67 : 82)),
            new Vector2(panel.Size.X - 48, playing ? 40 : 24)));
        float buttonY = panel.End.Y - (ready ? (IsPolishing ? 54 : 65) : 78);
        Place(_action, new Rect2(new Vector2(panel.GetCenter().X - 150, buttonY),
            new Vector2(144, 40)));
        Place(_cancel, new Rect2(new Vector2(panel.GetCenter().X + 6, buttonY),
            new Vector2(144, 40)));
        if (!ready)
            _action.Position = new Vector2(panel.GetCenter().X - 72, _action.Position.Y);

        if (IsPolishing)
        {
            Place(_polishingSurface, _surface.FabricRect);
            _polishingSurface.RefreshArtwork();
            RefreshPolishingCursor();
        }

        _surface.QueueRedraw();
    }

    private static AtlasTexture LoadArtwork(string path)
    {
        var texture = GD.Load<Texture2D>(path);
        using var image = texture.GetImage();
        return new AtlasTexture { Atlas = texture, Region = image.GetUsedRect(), FilterClip = true };
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

    private void StyleAction(bool primary)
    {
        if (_primaryAction == primary) return;
        _primaryAction = primary;
        StyleButton(_action, primary);
    }

    private void StyleButton(Button button, bool primary)
    {
        Texture2D face = primary ? _inkButton : _paperButton;
        foreach (var (state, tint) in new[] { ("normal", 1f), ("hover", 0.88f), ("pressed", 0.72f),
            ("hover_pressed", 0.72f), ("disabled", 0.60f) })
        {
            button.AddThemeStyleboxOverride(state, new StyleBoxTexture
            {
                Texture = face, ModulateColor = new Color(tint, tint, tint)
            });
        }
        button.AddThemeStyleboxOverride("focus", _focusFrame);
        foreach (string color in new[] { "font_color", "font_hover_color", "font_pressed_color",
            "font_hover_pressed_color", "font_focus_color", "font_disabled_color" })
            button.AddThemeColorOverride(color, primary ? Colors.White : Colors.Black);
        button.AddThemeFontSizeOverride("font_size", 20);
        button.AddThemeConstantOverride("outline_size", 0);
    }

}

public partial class EmbroiderySurface : Control
{
    private Texture2D _rankEmpty;
    private Texture2D _rankFilled;
    private Texture2D _hoop;
    private Texture2D _stitch;
    private static readonly Rect2 StitchSegment = new(886, 403, 76, 174);
    private static readonly Rect2 StitchPoint = new(886, 318, 76, 68);
    private const float StitchWidth = 4;
    private const float StitchLength = 9;
    private const float StitchSpacing = 12;
    private const float PointSize = 6;
    private Texture2D _panel;
    private StyleBoxTexture _fabricFrame;

    public override void _Ready()
    {
        _panel = GD.Load<Texture2D>("res://art/ui/panel_large.png");
        _stitch = GD.Load<Texture2D>("res://art/minigames/embroidery_stitch.png");
        _fabricFrame = EmbroideryGame.CreateArtworkFrame();
        const string emptyPath = "res://art/minigames/rank_empty.png";
        const string filledPath = "res://art/minigames/rank_full.png";
        const string hoopPath = "res://art/minigames/embroidery_hoop.png";

        if (ResourceLoader.Exists(emptyPath))
            _rankEmpty = GD.Load<Texture2D>(emptyPath);

        if (ResourceLoader.Exists(filledPath))
            _rankFilled = GD.Load<Texture2D>(filledPath);

        if (ResourceLoader.Exists(hoopPath))
            _hoop = GD.Load<Texture2D>(hoopPath);
    }

    private Vector2 _missPoint;
    private ulong _missAt;

    public EmbroideryGame Game { get; set; } = null!;

    public Rect2 PanelRect => new(
        (Size - new Vector2(
            Mathf.Min(900, Size.X - 32),
            Mathf.Min(600, Size.Y - 32))) / 2,
        new Vector2(
            Mathf.Min(900, Size.X - 32),
            Mathf.Min(600, Size.Y - 32)));

    public Rect2 FabricRect => new(
        PanelRect.Position + new Vector2(24, 112),
        PanelRect.Size - new Vector2(144, 202));

    public Rect2 QualityTrackRect => new(
        new Vector2(PanelRect.End.X - 96, FabricRect.Position.Y + 28),
        new Vector2(72, FabricRect.Size.Y - 66));

    public Vector2 ScreenPoint(int index)
    {
        var p = Game.Session.Points[index];
        return GlobalPosition + FabricRect.Position +
            new Vector2((float)p.X / 840, (float)p.Y / 480) *
            FabricRect.Size;
    }

    public override void _GuiInput(InputEvent @event)
    {
        Vector2? point = @event switch
        {
            InputEventMouseButton
                { Pressed: true, ButtonIndex: MouseButton.Left } mouse
                => mouse.Position,
            InputEventScreenTouch { Pressed: true } touch
                => touch.Position,
            _ => null
        };

        if (@event is InputEventMouseButton { Device: -1 })
            return;

        if (!Game.IsPolishing &&
            point.HasValue &&
            FabricRect.HasPoint(point.Value))
        {
            Vector2 logical =
                (point.Value - FabricRect.Position) /
                FabricRect.Size * new Vector2(840, 480);

            if (Game.Session.State == EmbroideryState.Playing)
            {
                if (Game.Session.Hit(logical.X, logical.Y))
                    SoundEffects.Instance?.PlayStitch();
                else
                {
                    _missPoint = point.Value;
                    _missAt = Time.GetTicksMsec();
                }
            }

            QueueRedraw();
        }

        AcceptEvent();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size),
            new Color(0.025f, 0.025f, 0.025f, 0.9f));

        DrawTextureRect(_panel, PanelRect, false);

        Rect2 fabric = FabricRect;
        DrawRect(fabric, new Color("c0c0c0"));
        DrawStyleBox(_fabricFrame, fabric.Grow(7));

        for (float x = fabric.Position.X; x < fabric.End.X; x += 6)
        {
            DrawLine(
                new Vector2(x, fabric.Position.Y),
                new Vector2(x, fabric.End.Y),
                new Color(0.32f, 0.32f, 0.32f, 0.12f));
        }

        for (float y = fabric.Position.Y; y < fabric.End.Y; y += 6)
        {
            DrawLine(
                new Vector2(fabric.Position.X, y),
                new Vector2(fabric.End.X, y),
                new Color(0.32f, 0.32f, 0.32f, 0.12f));
        }

        if (!Game.IsPolishing)
        {
            if (_hoop != null)
                DrawTextureRect(_hoop, fabric, false);

            var session = Game.Session;

            for (int i = 1; i < session.Points.Count; i++)
            {
                DrawDashedLine(
                    ScreenPoint(i - 1) - GlobalPosition,
                    ScreenPoint(i) - GlobalPosition,
                    new Color(0.25f, 0.25f, 0.25f, 0.3f),
                    1,
                    5);
            }

            for (int i = 1; i < session.Stitches; i++)
            {
                Vector2 from = ScreenPoint(i - 1) - GlobalPosition;
                Vector2 segment = ScreenPoint(i) - GlobalPosition - from;
                float length = segment.Length();
                DrawSetTransform(from, segment.Angle() - Mathf.Pi / 2);
                int count = Mathf.Max(0, Mathf.FloorToInt((length - PointSize - StitchLength) / StitchSpacing) + 1);
                float offset = (length - (count - 1) * StitchSpacing - StitchLength) / 2;
                for (int tile = 0; tile < count; tile++)
                    DrawTextureRectRegion(_stitch,
                        new Rect2(-StitchWidth / 2, offset + tile * StitchSpacing, StitchWidth, StitchLength),
                        StitchSegment);
                DrawSetTransform(Vector2.Zero);
            }

            for (int i = 0; i < session.Points.Count; i++)
            {
                Vector2 point = ScreenPoint(i) - GlobalPosition;
                if (i < session.Stitches)
                {
                    Vector2 size = StitchPoint.Size * (PointSize / StitchPoint.Size.X);
                    DrawTextureRectRegion(_stitch, new Rect2(point - size / 2, size), StitchPoint);
                }
                else
                    DrawCircle(point, 4, new Color("808080"));
            }

            // Draw the target last so nearby points and stitches cannot cover it.
            // Black/white contrast survives the final monochrome presentation pass.
            if (session.State == EmbroideryState.Playing && session.Stitches < session.Points.Count)
            {
                Vector2 point = ScreenPoint(session.Stitches) - GlobalPosition;
                float radius = 19 + Mathf.Sin(Time.GetTicksMsec() / 150f) * 2;
                DrawArc(point, radius, 0, Mathf.Tau, 64, Colors.White, 7, true);
                DrawArc(point, radius, 0, Mathf.Tau, 64, Colors.Black, 3, true);
                DrawCircle(point, 13, Colors.White);
                DrawCircle(point, 10, Colors.Black);
                DrawCircle(point, 3, Colors.White);
            }
        }

        var sessionQuality = Game.ActiveSession;
        Rect2 track = QualityTrackRect;
        float fill = Mathf.Clamp(
            sessionQuality.Quality / 100f, 0f, 1f);

        if (_rankEmpty != null && _rankFilled != null)
        {
            DrawTextureRect(_rankEmpty, track, false);

            if (fill > 0f)
            {
                Rect2 target = new Rect2(
                    track.Position.X,
                    track.End.Y - track.Size.Y * fill,
                    track.Size.X,
                    track.Size.Y * fill);

                Rect2 source = new Rect2(
                    0,
                    _rankFilled.GetHeight() * (1f - fill),
                    _rankFilled.GetWidth(),
                    _rankFilled.GetHeight() * fill);

                DrawTextureRectRegion(
                    _rankFilled, target, source);
            }
        }
        else
        {
            DrawRect(track, Colors.White);
            DrawRect(
                new Rect2(
                    track.Position.X,
                    track.End.Y - track.Size.Y * fill,
                    track.Size.X,
                    track.Size.Y * fill),
                Colors.Black);
            DrawRect(track, Colors.Black, false, 2);
        }

        if (!Game.IsPolishing &&
            Game.Session.Misses > 0 &&
            Time.GetTicksMsec() - _missAt < 350)
        {
            DrawLine(
                _missPoint - new Vector2(7, 7),
                _missPoint + new Vector2(7, 7),
                new Color("444444"),
                3,
                true);

            DrawLine(
                _missPoint - new Vector2(-7, 7),
                _missPoint + new Vector2(-7, 7),
                new Color("444444"),
                3,
                true);
        }
    }
}
