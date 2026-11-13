using Godot;

// The existing unlock state controls the music room and the right showroom together.
public partial class RoomDoorwayPresentation : Node
{
    public static void StyleLockedTitle(Label title)
    {
        title.AddThemeColorOverride("font_color", Colors.Black);
        title.AddThemeColorOverride("font_outline_color", Colors.White);
        title.AddThemeConstantOverride("outline_size", 6);
    }

    public static void AddLockedCurtain(ColorRect overlay)
    {
        overlay.Color = Colors.Black;
        var curtain = new TextureRect
        {
            Name = "LockedCurtain",
            Texture = GD.Load<Texture2D>("res://art/rooms/locked_curtain.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        overlay.AddChild(curtain);
        curtain.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var room = overlay.GetParent<Node2D>();
        float overlap = RoomFramePresentation.FloorSeamOverlap / room.GlobalScale.Y;
        if (room.Name == "Showroom") curtain.OffsetTop -= overlap;
        else curtain.OffsetBottom += overlap;
    }

    private Sprite2D _corridor = null!, _cashier = null!;
    private Texture2D _corridorOpen = null!, _corridorClosed = null!;
    private Texture2D _cashierOpen = null!, _cashierClosed = null!;

    public override void _Ready()
    {
        _corridor = GetNode<Sprite2D>("../World/Corridor/CorridorBackground");
        _cashier = GetNode<Sprite2D>("../World/TradingRoom/TradingRoomBackground");
        _corridorOpen = GD.Load<Texture2D>("res://art/rooms/upper_corridor.jpg");
        _corridorClosed = GD.Load<Texture2D>("res://art/rooms/upper_corridor_closed.jpg");
        _cashierOpen = GD.Load<Texture2D>("res://art/rooms/cashier.jpg");
        _cashierClosed = GD.Load<Texture2D>("res://art/rooms/cashier_closed.jpg");
    }

    public void Refresh(bool neighboringRoomsUnlocked)
    {
        GetNode<Node2D>("../World/Showroom/Showcases").Visible = neighboringRoomsUnlocked;
        _corridor.Texture = neighboringRoomsUnlocked ? _corridorOpen : _corridorClosed;
        _cashier.Texture = neighboringRoomsUnlocked ? _cashierOpen : _cashierClosed;
    }
}
