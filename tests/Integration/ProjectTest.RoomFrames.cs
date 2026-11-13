using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestRoomFrames()
    {
        Check(DisplayServer.GetName() != "headless", "Camera frame checks require rendering");
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var camera = game.GetNode<Camera2D>("Camera2D");
        camera.SetProcess(false);
        try
        {
            Check(!game.HasNode("ViewportFrame"), "The screen border must be absent while room frames remain");
            foreach (var entry in new[] {
                ("Atelier", "Background", new Rect2(1125, 819, 827, 447)),
                ("MusicWorkshop", "Background", new Rect2(2573, 819, 819, 447)),
                ("ShowroomLeft", "ShowroomBackground", new Rect2(1125, 1297, 827, 434)),
                ("Showroom", "ShowroomBackground", new Rect2(2573, 1297, 819, 434)),
                ("Corridor", "CorridorBackground", new Rect2(1963, 819, 599, 447)),
                ("TradingRoom", "TradingRoomBackground", new Rect2(1963, 1297, 599, 434)) })
            {
                var room = game.GetNode<Node2D>("World/" + entry.Item1);
                var art = room.GetNode<Sprite2D>(entry.Item2);
                var roomFrame = room.GetNode<NinePatchRect>("RoomFrame");
                Rect2 localBounds = art.Transform * art.GetRect();
                Rect2 worldBounds = art.GlobalTransform * art.GetRect();
                Rect2 expected = new(entry.Item3.Position * (5760f / 4560), entry.Item3.Size * (5760f / 4560));
                Check(worldBounds.Position.DistanceTo(expected.Position) < 0.02f && worldBounds.Size.DistanceTo(expected.Size) < 0.02f,
                    "Every room must align to its opening in the supplied day/night layout");
                Check(roomFrame.Position == localBounds.Position &&
                    (roomFrame.Size * roomFrame.Scale).IsEqualApprox(localBounds.Size) &&
                    roomFrame.ZIndex > 100 && !roomFrame.DrawCenter && roomFrame.MouseFilter == Control.MouseFilterEnum.Ignore,
                    "Each room must have an input-transparent frame around its art and above its locked curtain");
            }
            var layout = game.GetNode<Sprite2D>("World/RoomLayout");
            foreach (string period in new[] { "Day", "Night" })
            {
                var background = game.GetNode<Sprite2D>("World/TimeOfDay" + period);
                Check(background.Texture.ResourcePath == "res://art/rooms/" + period.ToLowerInvariant() + "_time.png" &&
                    (background.GlobalTransform * background.GetRect()).IsEqualApprox(layout.GlobalTransform * layout.GetRect()),
                    "Both supplied backgrounds must cover the same world bounds without changing camera limits");
            }
            Root.Size = new Vector2I(1280, 720);
            game.GetNode<CanvasLayer>("HUD").Hide();
            camera.Position = new Vector2(2880, 1620);
            camera.Zoom = Vector2.One * (1280f / 5760);
            game.Score = 1000;
            game.TryUnlockMusicWorkshop();
            using (var day = await CapturePresentation("room-layout-day")) { }
            game.EndDay(); // Start, then run to the night endpoint.
            game._Process(game.DayRemainingSeconds);
            game.GetNode<DayAnnouncementOverlay>("DayAnnouncements").Dismiss();
            using (var night = await CapturePresentation("room-layout-night")) { }
        }
        finally
        {
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
