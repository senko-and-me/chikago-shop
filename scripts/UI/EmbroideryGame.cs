using System;
using ChikagoShop.Domain.Crafting;
using Godot;

// Locks world interaction while the separate day clock continues. Acquisition commits on Start.
public partial class EmbroideryGame : CanvasLayer
{
    public EmbroiderySession Session { get; private set; } = new();
    public PolishingSession Polishing { get; private set; } = null!;
    public bool IsPolishing { get; }
    public ICraftSession ActiveSession => IsPolishing ? Polishing : Session;
    private PolishingSurface _polishingSurface = null!;
    public bool IsOpen => _surface != null && _surface.Visible;
    private EmbroiderySurface _surface = null!;
    private Label _title = null!, _hint = null!, _quality = null!, _dayTimer = null!;
    private readonly Control[] _statCards = new Control[3];
    private readonly Label[] _statValues = new Label[3];
    private readonly Label[] _statHeadings = new Label[3];
    private readonly Label[] _ranks = new Label[5];
    private Button _action = null!, _cancel = null!;
    private readonly Action _acquire;
    private bool _ownsPause;

    public EmbroideryGame(Action acquire, bool polishing = false) { _acquire = acquire; IsPolishing = polishing; }

    public override void _Ready()
    {
        Name = IsPolishing ? "PolishingGame" : "EmbroideryGame";
        Layer = 110;
        ProcessMode = ProcessModeEnum.Always;
        _surface = new EmbroiderySurface { Name = "Surface", Game = this, Visible = false };
        AddChild(_surface);
        _surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        if (IsPolishing)
        {
            _polishingSurface = new PolishingSurface { Name = "Polishing", MouseFilter = Control.MouseFilterEnum.Stop, ClipContents = true };
            _surface.AddChild(_polishingSurface);
        }
        _title = AddLabel("Title", 28);
        _dayTimer = AddLabel("DayTimer", 24);
        string[] statNames = ["Time", "Stitches", "Misses"];
        string[] statTitles = ["ВРЕМЯ", "СТЕЖКИ", "ПРОМАХИ"];
        for (int i = 0; i < _statCards.Length; i++)
        {
            var card = new Panel { Name = "Stat" + statNames[i], MouseFilter = Control.MouseFilterEnum.Ignore };
            card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color("171d1e"), BorderColor = new Color("5e594a"),
                BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7
            });
            _surface.AddChild(card);
            _statCards[i] = card;
            var heading = new Label { Name = "Heading", Text = statTitles[i], Position = new Vector2(14, 7),
                Size = new Vector2(76, 18), MouseFilter = Control.MouseFilterEnum.Ignore };
            heading.AddThemeFontSizeOverride("font_size", 12);
            heading.AddThemeColorOverride("font_color", new Color("c9ae7f"));
            card.AddChild(heading);
            _statHeadings[i] = heading;
            var value = new Label { Name = "Value", Position = new Vector2(14, 25), Size = new Vector2(76, 28),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            value.AddThemeFontSizeOverride("font_size", 22);
            value.AddThemeColorOverride("font_color", new Color("fff1d8"));
            card.AddChild(value);
            _statValues[i] = value;
        }
        _hint = AddLabel("Hint", 18);
        _quality = AddLabel("Quality", 25);
        for (int i = 0; i < _ranks.Length; i++)
            _ranks[i] = AddLabel("Rank" + "SABCD"[i], 24);
        _action = new Button { Name = "Action", MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        _cancel = new Button { Name = "Cancel", Text = "Назад" };
        _surface.AddChild(_action); _surface.AddChild(_cancel);
        foreach (var button in new[] { _action, _cancel })
        {
            button.AddThemeFontSizeOverride("font_size", 20);
            var style = new StyleBoxFlat { BgColor = new Color("aa824a"), CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 };
            button.AddThemeStyleboxOverride("normal", style);
            button.AddThemeColorOverride("font_color", new Color("191d20"));
        }
        _action.Pressed += Activate;
        _cancel.Pressed += Close;
    }

    private Label AddLabel(string name, int size)
    {
        var label = new Label { Name = name, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeColorOverride("font_color", new Color("f5e8d0"));
        label.AddThemeFontSizeOverride("font_size", size);
        _surface.AddChild(label);
        return label;
    }

    public void Open()
    {
        var game = GetParent<TradeScene>();
        if (IsOpen || GetTree().Paused || game.Failed || game.Completed || game.EnergyRemaining <= 0 || game.Score < (IsPolishing ? 140 : 90) || (IsPolishing && !game.MusicWorkshopUnlocked)) return;
        game.PersistProgress();
        if (IsPolishing)
        {
            string[] instruments = ["violin", "trumpet", "sax", "guitar"];
            Polishing = _polishingSurface.Begin(instruments[GD.Randi() % 4]);
        }
        else Session = new EmbroiderySession((int)(GD.Randi() % 4));
        _ownsPause = true;
        GetTree().Paused = true;
        _surface.Show();
        Refresh();
        _action.GrabFocus();
    }

    public void Activate()
    {
        if (!IsOpen) return;
        if (ActiveSession.State == EmbroideryState.Ready)
        {
            if (GetParent<TradeScene>().DayClosed) { Close(); return; }
            ActiveSession.Start();
            // Persist the paid batch now; closing the day cannot erase a started craft.
            _acquire();
        }
        else if (ActiveSession.State == EmbroideryState.Finished)
        {
            Close();
            return;
        }
        Refresh();
    }

    private void Close()
    {
        if (!IsOpen) return;
        if (ActiveSession.State == EmbroideryState.Playing) return;
        _surface.Hide();
        if (_ownsPause) GetTree().Paused = false;
        _ownsPause = false;
    }

    public override void _ExitTree()
    {
        if (_ownsPause) GetTree().Paused = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsOpen) return;
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            if (ActiveSession.State == EmbroideryState.Ready) Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!IsOpen) return;
        var game = GetParent<TradeScene>();
        game.TickCraftDay(delta);
        if (game.DayClosed && ActiveSession.State == EmbroideryState.Ready) { Close(); return; }
        ActiveSession.Tick(delta);
        Refresh();
    }

    private void Refresh()
    {
        bool ready = ActiveSession.State == EmbroideryState.Ready, playing = ActiveSession.State == EmbroideryState.Playing;
        _title.Text = GameLocalization.Text(ready || playing ? (IsPolishing ? "Полировка" : "Вышивка") : ActiveSession.Complete ? "Готово" : "Время вышло");
        _statValues[0].Text = FormattableString.Invariant($"{ActiveSession.RemainingSeconds:0.0}");
        _statValues[1].Text = IsPolishing ? $"{Math.Round(Polishing.Progress * 100)}%" : $"{Session.Stitches} / {Session.Points.Count}";
        _statValues[2].Text = ActiveSession.Misses.ToString();
        _statHeadings[0].Text = GameLocalization.Text("ВРЕМЯ");
        _statHeadings[1].Text = GameLocalization.Text(IsPolishing ? "ОЧИЩЕНО" : "СТЕЖКИ");
        _statHeadings[2].Text = GameLocalization.Text("ПРОМАХИ");
        _hint.Text = ready ? GameLocalization.Text(IsPolishing ? "Зажми и протирай туда-сюда" : "Нажимай на светящиеся точки") : "";
        _quality.Text = $"{ActiveSession.Quality}%";
        _action.Text = GameLocalization.Text(ready ? "Начать" : "Забрать");
        _action.Visible = !playing;
        _cancel.Visible = ready;
        Rect2 panel = _surface.PanelRect;
        _dayTimer.Text = GetParent().GetNode<Label>("HUD/Root/DayTimer").Text;
        Place(_title, new Rect2(panel.Position + new Vector2(120, 14), new Vector2(panel.Size.X - 240, 40)));
        Place(_dayTimer, new Rect2(new Vector2(panel.End.X - 112, panel.Position.Y + 14), new Vector2(88, 40)));
        for (int i = 0; i < _statCards.Length; i++)
            Place(_statCards[i], new Rect2(new Vector2(panel.GetCenter().X - 166 + i * 114, panel.Position.Y + 62),
                new Vector2(104, 60)));
        Rect2 track = _surface.QualityTrackRect;
        Place(_quality, new Rect2(new Vector2(panel.End.X - 104, track.End.Y + 10), new Vector2(88, 36)));
        for (int i = 0; i < _ranks.Length; i++)
        {
            Label label = _ranks[i];
            label.Text = "SABCD"[i].ToString();
            bool active = label.Text == ActiveSession.Rank;
            label.AddThemeColorOverride("font_color", new Color(active ? "ffe0a0" : "a99b83"));
            label.AddThemeColorOverride("font_outline_color", new Color(active ? "79522e" : "252a2b"));
            label.AddThemeConstantOverride("outline_size", active ? 4 : 0);
            Place(label, new Rect2(new Vector2(track.Position.X - 36, track.Position.Y + track.Size.Y * i / 5),
                new Vector2(28, track.Size.Y / 5)));
        }
        Place(_hint, new Rect2(panel.Position + new Vector2(16, panel.Size.Y - 100), new Vector2(panel.Size.X - 32, 30)));
        Place(_action, new Rect2(new Vector2(panel.GetCenter().X + 4, panel.End.Y - 58), new Vector2(144, 42)));
        Place(_cancel, new Rect2(new Vector2(panel.GetCenter().X - 148, panel.End.Y - 58), new Vector2(144, 42)));
        if (!ready) _action.Position = new Vector2(panel.GetCenter().X - 72, _action.Position.Y);
        if (IsPolishing)
        {
            Place(_polishingSurface, _surface.FabricRect);
            _polishingSurface.RefreshArtwork();
        }
        _surface.QueueRedraw();
    }

    private static void Place(Control control, Rect2 rect) { control.Position = rect.Position; control.Size = rect.Size; }
}

public partial class EmbroiderySurface : Control
{
    // Muted fabric/dye hues: burgundy at the bottom, sage at the top.
    private readonly GradientTexture2D _qualityGradient = new()
    {
        Width = 1, Height = 256, FillFrom = new Vector2(0, 1), FillTo = Vector2.Zero,
        Gradient = new Gradient
        {
            Offsets = [0f, 0.25f, 0.5f, 0.75f, 1f],
            Colors = [new Color("883f49"), new Color("ad6949"), new Color("c19155"),
                new Color("cfb66e"), new Color("b9c79a")]
        }
    };
    private readonly StyleBoxFlat _panelStyle = new() { BgColor = new Color("252a2b"), BorderColor = new Color("8e7953"),
        BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12 };
    private Vector2 _missPoint;
    private ulong _missAt;
    public EmbroideryGame Game { get; set; } = null!;
    public Rect2 PanelRect => new((Size - new Vector2(Mathf.Min(900, Size.X - 32), Mathf.Min(600, Size.Y - 32))) / 2,
        new Vector2(Mathf.Min(900, Size.X - 32), Mathf.Min(600, Size.Y - 32)));
    public Rect2 FabricRect => new(PanelRect.Position + new Vector2(24, 140), PanelRect.Size - new Vector2(136, 254));
    public Rect2 QualityTrackRect => new(new Vector2(PanelRect.End.X - 54, FabricRect.Position.Y),
        new Vector2(26, FabricRect.Size.Y));
    public Vector2 ScreenPoint(int index)
    {
        var p = Game.Session.Points[index];
        return GlobalPosition + FabricRect.Position + new Vector2((float)p.X / 840, (float)p.Y / 480) * FabricRect.Size;
    }

    public override void _GuiInput(InputEvent @event)
    {
        Vector2? point = @event switch
        {
            InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse => mouse.Position,
            InputEventScreenTouch { Pressed: true } touch => touch.Position,
            _ => null
        };
        // Touch is handled directly; suppress the synthetic mouse duplicate.
        if (@event is InputEventMouseButton { Device: -1 }) return;
        if (!Game.IsPolishing && point.HasValue && FabricRect.HasPoint(point.Value))
        {
            Vector2 logical = (point.Value - FabricRect.Position) / FabricRect.Size * new Vector2(840,480);
            if (Game.Session.State == EmbroideryState.Playing && !Game.Session.Hit(logical.X, logical.Y))
            {
                _missPoint = point.Value;
                _missAt = Time.GetTicksMsec();
            }
            QueueRedraw();
        }
        AcceptEvent();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.02f,0.025f,0.03f,0.9f));
        DrawStyleBox(_panelStyle, PanelRect);
        Rect2 fabric = FabricRect;
        DrawRect(fabric.Grow(7), new Color("7e5634"));
        DrawRect(fabric, new Color("d4bd96"));
        for (float x=fabric.Position.X;x<fabric.End.X;x+=6) DrawLine(new Vector2(x,fabric.Position.Y),new Vector2(x,fabric.End.Y),new Color(0.43f,0.3f,0.17f,0.12f));
        for (float y=fabric.Position.Y;y<fabric.End.Y;y+=6) DrawLine(new Vector2(fabric.Position.X,y),new Vector2(fabric.End.X,y),new Color(0.43f,0.3f,0.17f,0.12f));
        if (!Game.IsPolishing)
        {
        DrawSetTransformMatrix(Transform2D.Identity.Scaled(fabric.Size / new Vector2(840,480)).Translated(fabric.GetCenter()));
        var hoop = new Vector2[97];
        for (int i=0;i<hoop.Length;i++) { float angle=i*Mathf.Tau/96; hoop[i]=new Vector2(Mathf.Cos(angle)*348,Mathf.Sin(angle)*212); }
        DrawPolyline(hoop,new Color("7b4d2b"),14,true); DrawPolyline(hoop,new Color("e7b97b"),4,true);
        DrawSetTransform(Vector2.Zero);
        var session = Game.Session;
        for (int i=1;i<session.Points.Count;i++) DrawDashedLine(ScreenPoint(i-1)-GlobalPosition,ScreenPoint(i)-GlobalPosition,new Color(0.3f,0.24f,0.17f,0.3f),1,5);
        for (int i=1;i<session.Stitches;i++)
        {
            DrawLine(ScreenPoint(i-1)-GlobalPosition,ScreenPoint(i)-GlobalPosition,new Color("501b2c"),6,true);
            DrawLine(ScreenPoint(i-1)-GlobalPosition,ScreenPoint(i)-GlobalPosition,new Color("b5374a"),3,true);
        }
        for (int i=0;i<session.Points.Count;i++)
        {
            Vector2 p=ScreenPoint(i)-GlobalPosition;
            bool active=i==session.Stitches && session.State==EmbroideryState.Playing;
            DrawCircle(p,active?10:4,new Color(active?"ffedab":i<session.Stitches?"a12f43":"977d5b"));
            if(active) DrawArc(p,18+Mathf.Sin(Time.GetTicksMsec()/150f)*2,0,Mathf.Tau,40,new Color("fff7d8"),2,true);
        }
        }
        var activeSession = Game.ActiveSession;
        Rect2 track = QualityTrackRect;
        DrawRect(track.Grow(3), new Color("c9ae7f"));
        DrawTextureRect(_qualityGradient, track, false);
        float empty = track.Size.Y * (100 - activeSession.Quality) / 100;
        if (empty > 0) DrawRect(new Rect2(track.Position, new Vector2(track.Size.X, empty)), new Color("252a2b"));
        for (int i = 1; i < 5; i++)
        {
            float y = track.Position.Y + track.Size.Y * i / 5;
            DrawLine(new Vector2(track.Position.X, y), new Vector2(track.End.X, y), new Color("79522e"), 1);
        }
        if (!Game.IsPolishing && Game.Session.Misses > 0 && Time.GetTicksMsec() - _missAt < 350)
        {
            DrawLine(_missPoint-new Vector2(7,7),_missPoint+new Vector2(7,7),new Color("b32137"),3,true);
            DrawLine(_missPoint-new Vector2(-7,7),_missPoint+new Vector2(-7,7),new Color("b32137"),3,true);
        }
    }
}
