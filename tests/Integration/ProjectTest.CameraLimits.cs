using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestCameraReferenceLimits()
    {
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var camera = game.GetNode<WorldCamera>("Camera2D");
        camera.SetProcess(false);
        var layout = game.GetNode<Sprite2D>("World/RoomLayout");
        Rect2 world = layout.GlobalTransform * layout.GetRect();
        Vector2 factor = world.Size / new Vector2(4560, 2565);
        Rect2 allowed = new(world.Position + new Vector2(429, 563) * factor, new Vector2(3662, 1440) * factor);
        Rect2 View() => new(camera.Position - Root.GetVisibleRect().Size / camera.Zoom / 2,
            Root.GetVisibleRect().Size / camera.Zoom);
        void CheckInside() => Check(allowed.Grow(0.05f).Encloses(View()), "The entire visible viewport must stay inside the reference movement range");
        void CheckStartingView()
        {
            Vector2 widest = View().Size / factor;
            Check(widest.X <= 2560.1f && widest.Y <= 1440.1f &&
                (Mathf.Abs(widest.X - 2560) < 0.1f || Mathf.Abs(widest.Y - 1440) < 0.1f),
                "Game entry and NewGame must immediately use the maximum allowed overview for the current window");
            Vector2 halfView = View().Size / 2;
            Vector2 center = world.GetCenter();
            Vector2 expected = new(
                halfView.X >= allowed.Size.X / 2 ? allowed.GetCenter().X :
                    Mathf.Clamp(center.X, allowed.Position.X + halfView.X, allowed.End.X - halfView.X),
                halfView.Y >= allowed.Size.Y / 2 ? allowed.GetCenter().Y :
                    Mathf.Clamp(center.Y, allowed.Position.Y + halfView.Y, allowed.End.Y - halfView.Y));
            Check(camera.Position.DistanceTo(expected) < 0.02f,
                "The starting overview must be centered on the world scene within the navigation bounds");
            CheckInside();
        }
        void Wheel(MouseButton direction)
        {
            for (int i = 0; i < 100; i++)
                camera._Input(new InputEventMouseButton { ButtonIndex = direction, Pressed = true,
                    Position = Root.GetVisibleRect().Size / 2 });
            camera._Process(1);
        }
        try
        {
            CheckStartingView();
            Check(camera.ReferenceSize == new Vector2(4560, 2565) &&
                camera.NavigationRegion == new Rect2(429, 563, 3662, 1440) &&
                camera.OverviewSize == new Vector2(2560, 1440) && camera.CloseupSize == new Vector2(1036, 583),
                "Scene limits must use the measured red rectangles from all three user references");
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(1000, 800) })
            {
                Root.Size = size;
                await ToSignal(this, SignalName.ProcessFrame);
                game.NewGame();
                CheckStartingView();
                camera.ClampToBounds();
                Wheel(MouseButton.WheelDown);
                CheckInside();
                Vector2 widest = View().Size / factor;
                Check(widest.X <= 2560.1f && widest.Y <= 1440.1f &&
                    (Mathf.Abs(widest.X - 2560) < 0.1f || Mathf.Abs(widest.Y - 1440) < 0.1f),
                    "Maximum zoom-out must fit the 2560x1440 overview on both window shapes");
                camera.Position = allowed.GetCenter();
                if (size.X == 1280)
                    using (var image = await CapturePresentation("camera-overview")) { }
                Wheel(MouseButton.WheelUp);
                Vector2 closest = View().Size / factor;
                Check(closest.X >= 1035.9f && closest.Y >= 582.9f &&
                    (Mathf.Abs(closest.X - 1036) < 0.1f || Mathf.Abs(closest.Y - 583) < 0.1f),
                    "Maximum zoom-in must not crop tighter than the minimum reference area");
                foreach (Vector2 corner in new[] { new Vector2(-100000, -100000), new Vector2(100000, -100000),
                    new Vector2(-100000, 100000), new Vector2(100000, 100000) })
                {
                    camera.Position = corner;
                    camera.ClampToBounds();
                    CheckInside();
                    Vector2 expected = new(corner.X < 0 ? allowed.Position.X : allowed.End.X,
                        corner.Y < 0 ? allowed.Position.Y : allowed.End.Y);
                    Vector2 edge = new(corner.X < 0 ? View().Position.X : View().End.X,
                        corner.Y < 0 ? View().Position.Y : View().End.Y);
                    Check(edge.DistanceTo(expected) < 0.05f, "Panning must stop at each of the four reference boundaries");
                }
                camera.Position = world.Position + new Vector2(2272, 1516.5f) * factor;
                camera.ClampToBounds();
                if (size.X == 1280)
                    using (var image = await CapturePresentation("camera-closeup")) { }
                Wheel(MouseButton.WheelDown);
                camera.Position = allowed.GetCenter();
                Vector2 pointer = Root.GetVisibleRect().Size * new Vector2(0.55f, 0.5f);
                Vector2 offset = pointer - Root.GetVisibleRect().Size / 2;
                Vector2 anchor = camera.Position + offset / camera.Zoom;
                camera._Input(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true, Position = pointer });
                camera._Process(1d / 60);
                Check((camera.Position + offset / camera.Zoom).DistanceTo(anchor) < 0.02f,
                    "Reference limits must preserve smooth zoom toward the cursor away from the boundaries");
                CheckInside();
                // Restart after zooming and panning: no pending zoom may pull the overview away again.
                camera._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
                game.NewGame();
                camera._Input(new InputEventMouseMotion { Relative = new Vector2(200, 100) });
                camera._Process(1d / 60);
                CheckStartingView();
                if (size.X == 1280)
                    using (var image = await CapturePresentation("camera-start-overview")) { }
            }
        }
        finally
        {
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
