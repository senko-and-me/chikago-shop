using Godot;

// Frame the existing room art in its local space, including above the locked curtains.
public partial class RoomFramePresentation : Node
{
    public const float FloorSeamOverlap = 10f;
    private static readonly Vector2 ArtScale = new(0.4f, 0.2f);

    public override void _Ready()
    {
        var source = GD.Load<Texture2D>("res://art/ui/camera_frame.png");
        using var image = source.GetImage();
        Rect2I region = image.GetUsedRect();
        var texture = new AtlasTexture { Atlas = source, Region = region };
        // Place the middle of each painted stroke on the room edge, not its outer texture boundary.
        Vector4 inset = new(StrokeCenter(image, region, true, false), StrokeCenter(image, region, false, false),
            StrokeCenter(image, region, true, true), StrokeCenter(image, region, false, true));
        inset *= new Vector4(ArtScale.X, ArtScale.Y, ArtScale.X, ArtScale.Y);
        foreach (var entry in new[] {
            ("Atelier", "Background"), ("MusicWorkshop", "Background"),
            ("Showroom", "ShowroomBackground"), ("ShowroomLeft", "ShowroomBackground"),
            ("TradingRoom", "TradingRoomBackground"), ("Corridor", "CorridorBackground") })
        {
            var room = GetNode<Node2D>("../World/" + entry.Item1);
            var background = room.GetNode<Sprite2D>(entry.Item2);
            // Fill part of the existing floor gap with room art. Only the background stretches;
            // furniture, interaction areas and the outer building silhouette stay in place.
            bool lowerFloor = entry.Item1 is "Showroom" or "ShowroomLeft" or "TradingRoom";
            float overlap = FloorSeamOverlap / room.GlobalScale.Y;
            float height = (background.Transform * background.GetRect()).Size.Y;
            background.Scale *= new Vector2(1, (height + overlap) / height);
            if (lowerFloor) background.Position -= new Vector2(0, overlap);
            Rect2 bounds = background.Transform * background.GetRect();
            Vector2 topLeft = new(inset.X, inset.Y);
            Vector2 bottomRight = new(inset.Z, inset.W);
            var border = CreateBorder(bounds.Size + topLeft + bottomRight, texture);
            border.Name = "RoomFrame";
            border.Position = bounds.Position - topLeft;
            border.ZIndex = 101;
            // A narrow continuous core bridges pinholes where the hand-drawn art meets the outline.
            var seal = new Panel
            {
                Name = "SeamSeal", ShowBehindParent = true,
                Position = (topLeft - new Vector2(4, 5)) / ArtScale,
                Size = (bounds.Size + new Vector2(8, 10)) / ArtScale,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            var style = new StyleBoxFlat { BgColor = Colors.Black, DrawCenter = false };
            style.BorderColor = Colors.Black;
            style.BorderWidthLeft = style.BorderWidthRight = 20;
            style.BorderWidthTop = style.BorderWidthBottom = 50;
            seal.AddThemeStyleboxOverride("panel", style);
            border.AddChild(seal);
            room.AddChild(border);
        }
    }

    private static float StrokeCenter(Image image, Rect2I region, bool vertical, bool farEdge)
    {
        float weighted = 0, total = 0;
        int length = vertical ? region.Size.Y : region.Size.X;
        for (int depth = 0; depth < 96; depth++)
        for (int along = length / 4; along < length * 3 / 4; along += 4)
        {
            int x = vertical ? (farEdge ? region.End.X - 1 - depth : region.Position.X + depth) : region.Position.X + along;
            int y = vertical ? region.Position.Y + along : (farEdge ? region.End.Y - 1 - depth : region.Position.Y + depth);
            float alpha = image.GetPixel(x, y).A;
            weighted += (depth + 0.5f) * alpha;
            total += alpha;
        }
        return total > 0 ? weighted / total : 0;
    }

    private static NinePatchRect CreateBorder(Vector2 size, Texture2D texture)
    {
        return new NinePatchRect
        {
            Texture = texture,
            DrawCenter = false,
            PatchMarginLeft = 96, PatchMarginTop = 96,
            PatchMarginRight = 96, PatchMarginBottom = 96,
            Scale = ArtScale,
            Size = size / ArtScale,
            // Retain the supplied silhouette, but never paint white flecks over a black joint.
            SelfModulate = Colors.Black,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
    }
}
