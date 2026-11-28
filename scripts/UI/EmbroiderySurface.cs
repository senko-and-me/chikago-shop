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
    private const float SidePadding = 40;
    private const float QualityTrackWidth = 72;
    private const float FabricFramePadding = 7;
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
    // Only the painted interior fills. The end caps and letters stay visible at every score.
    private static readonly Rect2 RankFillArtwork = new(200, 100, 190, 1800);
    private static readonly int[] PaintedRankDividers = [0, 427, 799, 1201, 1575, 2000];

    public EmbroideryGame Game { get; set; } = null!;

    public Rect2 PanelRect
    {
        get
        {
            Vector2 margin = new(36, 16);
            Vector2 fitted = _authoredPanelSize.Min((Size - 2 * margin).Max(Vector2.One));
            Vector2 position = ((Size - fitted) / 2 + new Vector2(0, _authoredPanelOffset.Y))
                .Clamp(margin, (Size - fitted - margin).Max(margin));
            return new Rect2(position, fitted);
        }
    }

    public Rect2 FabricRect => new(
        PanelRect.Position + new Vector2(SidePadding + FabricFramePadding, 112),
        PanelRect.Size - new Vector2(2 * SidePadding + QualityTrackWidth + 24 + 2 * FabricFramePadding, 202));

    public Rect2 QualityTrackRect => new(
        new Vector2(PanelRect.End.X - SidePadding - QualityTrackWidth, FabricRect.Position.Y + 28),
        new Vector2(QualityTrackWidth, FabricRect.Size.Y - 66));

    internal Rect2 MeterTrackRect => new(
        QualityTrackRect.Position + QualityTrackRect.Size * RankFillArtwork.Position / new Vector2(400, 2000),
        QualityTrackRect.Size * RankFillArtwork.Size / new Vector2(400, 2000));

    internal float MeterFill => Game.ActiveSession.Quality / 100f;

    private static Rect2 BottomPortion(Rect2 rect, float fraction) => new(
        rect.Position.X, rect.End.Y - rect.Size.Y * fraction, rect.Size.X, rect.Size.Y * fraction);

    private void DrawQualityBands(Rect2 track)
    {
        Rect2 fillTrack = MeterTrackRect;
        // Keep the hand-drawn letters and linework, snapping each divider to its score.
        for (int band = 0; band < PaintedRankDividers.Length - 1; band++)
        {
            float top = band == 0 ? track.Position.Y :
                fillTrack.Position.Y + fillTrack.Size.Y * band * CraftQualityRank.BandSize / 100f;
            int nextBand = band + 1;
            float bottom = nextBand == PaintedRankDividers.Length - 1 ? track.End.Y :
                fillTrack.Position.Y + fillTrack.Size.Y * nextBand * CraftQualityRank.BandSize / 100f;
            var bandRect = new Rect2(track.Position.X, top, track.Size.X, bottom - top);
            var bandArt = new Rect2(0, PaintedRankDividers[band], _rankEmpty.GetWidth(),
                PaintedRankDividers[nextBand] - PaintedRankDividers[band]);
            DrawTextureRectRegion(_rankEmpty, bandRect, bandArt);
        }
    }

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
        DrawStyleBox(_fabricFrame, fabric.Grow(FabricFramePadding));

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

        Rect2 track = QualityTrackRect;
        float fill = MeterFill;
        if (_rankEmpty != null && _rankFilled != null)
        {
            DrawQualityBands(track);
            if (fill > 0f)
                DrawTextureRectRegion(_rankFilled, BottomPortion(MeterTrackRect, fill), BottomPortion(RankFillArtwork, fill));
        }
        else
        {
            DrawRect(track, Colors.White);
            if (fill > 0f) DrawRect(BottomPortion(track, fill), Colors.Black);
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
