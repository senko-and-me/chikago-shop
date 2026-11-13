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
                expected.Size += new Vector2(0, 10);
                if (entry.Item3.Position.Y == 1297) expected.Position -= new Vector2(0, 10);
                Check(worldBounds.Position.DistanceTo(expected.Position) < 0.02f && worldBounds.Size.DistanceTo(expected.Size) < 0.02f,
                    "Room art must cover ten world pixels of the floor gap while retaining the outside building edges");
                Check(new Rect2(roomFrame.Position, roomFrame.Size * roomFrame.Scale).Encloses(localBounds) &&
                    roomFrame.ZIndex > 100 && !roomFrame.DrawCenter && roomFrame.MouseFilter == Control.MouseFilterEnum.Ignore,
                    "Each room must have an input-transparent frame around its art and above its locked curtain");
                await CheckRenderedRoomStroke(roomFrame, localBounds);
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
            game.NewGame();
            game.GetNode<CanvasLayer>("HUD").Show();
            camera.Position = new Vector2(3520, 1400);
            camera.Zoom = Vector2.One * 0.7f;
            using (var closeup = await CapturePresentation("room-frame-alignment")) { }
        }
        finally
        {
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task CheckRenderedRoomStroke(NinePatchRect original, Rect2 room)
    {
        // Render the actual nine-patch in isolation: measure painted pixels, not the Control's bounds.
        var viewport = new SubViewport { Size = new Vector2I(512, 384), TransparentBg = true,
            World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        Root.AddChild(viewport);
        try
        {
            var copy = (NinePatchRect)original.Duplicate();
            const float scale = 0.2f;
            Vector2 start = new(48, 48);
            copy.Position = start + (original.Position - room.Position) * scale;
            copy.Scale = original.Scale * scale;
            viewport.AddChild(copy);
            for (int i = 0; i < 3; i++) await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var pixels = viewport.GetTexture().GetImage();
            Vector2 end = start + room.Size * scale;
            for (int y = 0; y < pixels.GetHeight(); y++)
            for (int x = 0; x < pixels.GetWidth(); x++)
            {
                Color pixel = pixels.GetPixel(x, y);
                Check(pixel.A < 0.1f || (pixel.R < 0.01f && pixel.G < 0.01f && pixel.B < 0.01f),
                    "The rendered frame must contain no opaque white flecks at its joints");
            }
            foreach (var edge in new[] { (true, start.X), (true, end.X), (false, start.Y), (false, end.Y) })
            {
                float mass = 0, weighted = 0;
                int from = (int)(edge.Item1 ? start.Y + room.Size.Y * scale / 4 : start.X + room.Size.X * scale / 4);
                int to = (int)(edge.Item1 ? end.Y - room.Size.Y * scale / 4 : end.X - room.Size.X * scale / 4);
                for (int across = Mathf.FloorToInt(edge.Item2 - 8); across <= Mathf.CeilToInt(edge.Item2 + 8); across++)
                for (int along = from; along < to; along++)
                {
                    float alpha = pixels.GetPixel(edge.Item1 ? across : along, edge.Item1 ? along : across).A;
                    mass += alpha;
                    weighted += (across + 0.5f) * alpha;
                }
                Check(mass > 0 && Mathf.Abs(weighted / mass - edge.Item2) < 0.8f,
                    "The painted stroke must be centered on each room edge, with no inset or displaced outline");
                if (!edge.Item1)
                    Check(mass / (to - from) < 4.0f,
                        "Horizontal strokes must stay thinner than four pixels in the reference render");
                int first = Mathf.CeilToInt(edge.Item1 ? start.Y : start.X);
                int last = Mathf.FloorToInt(edge.Item1 ? end.Y : end.X);
                for (int along = first; along <= last; along++)
                    Check(pixels.GetPixel(edge.Item1 ? Mathf.FloorToInt(edge.Item2) : along,
                            edge.Item1 ? along : Mathf.FloorToInt(edge.Item2)).A > 0.5f,
                        $"The black seam must remain continuous through room corners without transparent pinholes: {original.GetParent().Name} edge {edge}, along {along}, end {end}");
            }
        }
        finally { viewport.QueueFree(); }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
