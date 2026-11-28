using Godot;
using ChikagoShop.Domain.Crafting;

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
    private Vector2 _authoredPanelSize;
    private Vector2 _authoredPanelOffset;

    public override void _Ready()
    {
        var authoredPanel = GetNode<Control>("Panel");
        _authoredPanelSize = authoredPanel.Size;
        _authoredPanelOffset = authoredPanel.Position + authoredPanel.Size / 2 - Size / 2;
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
    private static readonly int[] EmbroideryRankThresholds = [0, 40, 60, 75, 90, 100];
    private static readonly int[] PolishingRankThresholds = [0, 42, 63, 78, 92, 100];

    // The artwork has five equally spaced grade bands, unlike the quality thresholds.
    // Map each actual grade interval onto its painted band without changing the score.
    internal static float QualityToRankFill(int quality, bool polishing)
    {
        int[] thresholds = polishing ? PolishingRankThresholds : EmbroideryRankThresholds;
        quality = Mathf.Clamp(quality, 0, 100);
        for (int tier = 0; tier < 5; tier++)
            if (quality < thresholds[tier + 1] || tier == 4)
                return (tier + (quality - thresholds[tier]) / (float)(thresholds[tier + 1] - thresholds[tier])) / 5f;
        return 1;
    }

    public EmbroideryGame Game { get; set; } = null!;

    public Rect2 PanelRect
    {
        get
        {
            Vector2 fitted = _authoredPanelSize.Min((Size - Vector2.One * 32).Max(Vector2.One));
            Vector2 position = ((Size - fitted) / 2 + _authoredPanelOffset)
                .Clamp(Vector2.One * 16, (Size - fitted - Vector2.One * 16).Max(Vector2.One * 16));
            return new Rect2(position, fitted);
        }
    }

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
                {
                    SoundEffects.Instance?.PlayStitch();
                    if (Game.Session.Complete) Game.RestoreNeedleCursor();
                }
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
        float fill = QualityToRankFill(sessionQuality.Quality, Game.IsPolishing);

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
