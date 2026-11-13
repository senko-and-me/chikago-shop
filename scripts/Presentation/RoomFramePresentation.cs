using Godot;

// Frame the existing room art in its local space, including above the locked curtains.
public partial class RoomFramePresentation : Node
{
    public override void _Ready()
    {
        foreach (var entry in new[] {
            ("Atelier", "Background"), ("MusicWorkshop", "Background"),
            ("Showroom", "ShowroomBackground"), ("ShowroomLeft", "ShowroomBackground"),
            ("TradingRoom", "TradingRoomBackground"), ("Corridor", "CorridorBackground") })
        {
            var room = GetNode<Node2D>("../World/" + entry.Item1);
            var background = room.GetNode<Sprite2D>(entry.Item2);
            Rect2 bounds = background.Transform * background.GetRect();
            var border = CreateBorder(bounds.Size);
            border.Name = "RoomFrame";
            border.Position = bounds.Position;
            border.ZIndex = 101;
            room.AddChild(border);
        }
    }

    private static NinePatchRect CreateBorder(Vector2 size)
    {
        const float artScale = 0.4f;
        var source = GD.Load<Texture2D>("res://art/ui/camera_frame.png");
        using var image = source.GetImage();
        return new NinePatchRect
        {
            Texture = new AtlasTexture { Atlas = source, Region = image.GetUsedRect() },
            DrawCenter = false,
            PatchMarginLeft = 96, PatchMarginTop = 96,
            PatchMarginRight = 96, PatchMarginBottom = 96,
            Scale = Vector2.One * artScale,
            Size = size / artScale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
    }
}
