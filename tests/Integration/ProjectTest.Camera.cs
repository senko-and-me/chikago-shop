using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestRoomTitles()
    {
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        var entries = new[] { "Atelier", "MusicWorkshop", "ShowroomLeft", "Showroom" }.Select(room =>
        {
            var node = game.GetNode("World/" + room);
            var plate = node.GetNodeOrNull<Control>("Header") ?? node.GetNode<Control>("ShowroomHeader");
            var title = node.GetNodeOrNull<Label>("NameLabel") ?? node.GetNode<Label>("ShowroomLabel");
            var subtitle = node.GetNodeOrNull<Control>("HintLabel") ?? node.GetNode<Control>("ShowroomHint");
            return (Room: room, Plate: plate, Title: title, Subtitle: subtitle,
                Width: plate.Size.X * plate.Scale.X, Center: plate.Position + plate.Size * plate.Scale / 2);
        }).ToArray();
        Root.AddChild(game);
        game.SetProcess(false);
        var camera = game.GetNode<WorldCamera>("Camera2D");
        camera.SetProcess(false);
        var ui = game.GetNode<ZoomResponsiveUi>("ZoomResponsiveUi");
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        try
        {
            await Frame();
            game.EndDay();
            game.Score = 10000;
            game.TryUnlockMusicWorkshop();
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720), new Vector2I(1000, 800) })
            foreach (string language in new[] { "ru", "en" })
            {
                Root.Size = size;
                TranslationServer.SetLocale(language);
                await Frame();
                camera.ResetToOverview();
                await Frame();
                Check(entries.All(entry => !entry.Plate.Visible && !entry.Title.Visible && !entry.Subtitle.Visible),
                    "The real maximum overview must hide all room headings, plates and subtitles in every viewport");
                if (size.X == 1280 && language == "ru") using (await CapturePresentation("room-titles-overview")) { }
                camera.Zoom = Vector2.One * camera.OverviewZoom * 1.2f;
                ui.UpdateForZoom(camera.Zoom.X);
                Check(entries.All(entry => !entry.Title.Visible && Mathf.IsZeroApprox(entry.Title.Modulate.A)),
                    "Titles must stay hidden through twenty percent zoom above overview");
                camera.Zoom = Vector2.One * camera.OverviewZoom * 1.4f;
                ui.UpdateForZoom(camera.Zoom.X);
                Check(entries.All(entry => entry.Title.Visible && entry.Title.Modulate.A > 0 && entry.Title.Modulate.A < 1 && !entry.Subtitle.Visible),
                    "Only titles and plates must smoothly fade in between the relative zoom thresholds");
                foreach (var entry in entries)
                {
                    var frame = game.GetNode<Control>($"World/{entry.Room}/RoomFrame");
                    Rect2 bounds = frame.GetGlobalTransform() * new Rect2(Vector2.Zero, frame.Size);
                    camera.FocusOn(bounds.GetCenter(), camera.CloseupSize, 0.01);
                    await ToSignal(CreateTimer(0.06), SceneTreeTimer.SignalName.Timeout);
                    await Frame();
                    Rect2 plateRect = entry.Plate.GetTransform() * new Rect2(Vector2.Zero, entry.Plate.Size);
                    Rect2 titleRect = entry.Title.GetTransform() * new Rect2(Vector2.Zero, entry.Title.Size);
                    float textWidth = entry.Title.GetThemeFont("font").GetStringSize(entry.Title.Tr(entry.Title.Text),
                        fontSize: entry.Title.GetThemeFontSize("font_size")).X;
                    Check(entry.Title.Visible && Mathf.IsEqualApprox(entry.Title.Modulate.A, 1) && !entry.Subtitle.Visible &&
                        plateRect.Size.X <= entry.Width * 0.66f && plateRect.Size.Y <= 161 &&
                        plateRect.GetCenter().DistanceTo(entry.Center) < 0.1f && plateRect.Encloses(titleRect) && textWidth <= entry.Title.Size.X,
                        $"The compact {entry.Room} plate must retain its center and fit its {language} title without a subtitle at {size}");
                    if (size.X == 1280)
                        using (await CapturePresentation($"room-title-{entry.Room}-{language}")) { }
                }
                game.RefreshDayPresentation();
                await Frame();
                Check(entries.All(entry => !entry.Subtitle.Visible), "Game state refresh must not reveal removed subtitles");
            }
        }
        finally
        {
            camera.CancelFocusTransition();
            game.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(originalLocale);
            await Frame();
        }
        GD.Print("ROOM_TITLE_CHECKS_PASS: compact centered titles, no subtitles, overview/fade/closeup, RU/EN and three viewports");
    }


    private async Task TestPauseHotkey()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        try
        {
            Check(!game.HasNode("PauseMenu/Root/PauseButton"), "The on-screen pause button must be removed entirely");
            var overlay = game.GetNode<Control>("PauseMenu/Root/Overlay");
            Check(!Paused && !overlay.Visible, "Gameplay must start unpaused");
            await PressEscapeForTest();
            Check(Paused && overlay.Visible, "Escape must open pause without a screen button");
            double remaining = game.DayRemainingSeconds;
            game._Process(5);
            Check(game.DayRemainingSeconds == remaining, "The timer must remain frozen while paused");
            Root.PushInput(new InputEventKey { Pressed = true, Echo = true, Keycode = Key.Escape }, true);
            Check(Paused && overlay.Visible, "Holding Escape must not repeatedly toggle pause");
            const string content = "PauseMenu/Root/Overlay/Center/Panel/Content/";
            await ClickControl(game, content + "Menu/SettingsButton");
            Check(Paused && game.GetNode<Control>(content + "Settings").Visible, "Settings must remain in pause");
            await PressEscapeForTest();
            Check(Paused && game.GetNode<Control>(content + "Menu").Visible,
                "Escape from settings must return to the pause menu");
            await PressEscapeForTest();
            Check(!Paused && !overlay.Visible, "Escape from the pause menu must resume gameplay");
            await PressEscapeForTest();
            await ClickControl(game, content + "Menu/ResumeButton");
            Check(!Paused && !overlay.Visible, "The Resume menu action must still work");

            if (!game.DayStarted) game.EndDay();
            var crafting = game.GetNode<EmbroideryGame>("EmbroideryGame");
            crafting.Open();
            Check(crafting.IsOpen, "Crafting fixture must open");
            await PressEscapeForTest();
            Check(crafting.IsOpen && Paused && overlay.Visible, "Escape must pause without cancelling craft");
            await PressEscapeForTest();
            Check(crafting.IsOpen && Paused && !overlay.Visible, "Escape must return to the same craft screen");
            crafting.Close();
            game.DayUpgrades.Offer(2);
            var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
            card.Open(1, 2, game.DayUpgrades.Options, game.Score);
            await WaitForDayCardAnimation(card);
            await PressEscapeForTest();
            await WaitForDayCardAnimation(card);
            Check(!card.IsOpen && !Paused && !overlay.Visible,
                "Escape skipping day cards must not also open pause");
        }
        finally
        {
            game.QueueFree();
            Paused = false;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestStandaloneCamera()
    {
        // Deliberately contains no TradeScene: navigation must work with just bounds and a viewport.
        var scene = new Node2D();
        var boundsSource = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://art/rooms/room_layout.jpg"),
            Centered = false, Position = new Vector2(100, 200), Scale = Vector2.One * 4
        };
        scene.AddChild(boundsSource);
        var camera = new WorldCamera
        {
            BoundsSource = boundsSource, Zoom = Vector2.One * 0.7f,
            Position = (boundsSource.Transform * boundsSource.GetRect()).GetCenter()
        };
        scene.AddChild(camera);
        var layer = new CanvasLayer();
        scene.AddChild(layer);
        var button = new Button { Position = new Vector2(12, 12), Size = new Vector2(160, 64), Text = "UI" };
        layer.AddChild(button);
        Root.AddChild(scene);
        camera.SetProcess(false);
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            void Move(Vector2 point, Vector2 relative, MouseButtonMask mask = 0) =>
                Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                    Relative = relative, ButtonMask = mask }, true);
            void Left(Vector2 point, bool pressed) => Root.PushInput(new InputEventMouseButton
            {
                Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
                Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0
            }, true);

            Vector2 pointer = Root.GetVisibleRect().Size * new Vector2(0.6f, 0.55f);
            Vector2 screenOffset = pointer - Root.GetVisibleRect().Size / 2;
            Vector2 anchoredWorldPoint = camera.Position + screenOffset / camera.Zoom;
            float zoomBefore = camera.Zoom.X;
            Root.PushInput(new InputEventMouseButton { Position = pointer, GlobalPosition = pointer,
                ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
            Check(camera.Zoom.X == zoomBefore, "Standalone camera wheel input must preserve smooth zoom");
            camera._Process(1d / 60d);
            Check(camera.Zoom.X > zoomBefore &&
                  (camera.Position + screenOffset / camera.Zoom).DistanceTo(anchoredWorldPoint) < 0.01f,
                "The extracted camera must zoom toward the world point under the cursor");

            Vector2 positionBefore = camera.Position;
            Vector2 uiPoint = button.GetGlobalRect().GetCenter();
            Move(uiPoint, Vector2.Zero);
            Left(uiPoint, true);
            Move(uiPoint + new Vector2(40, 0), new Vector2(40, 0), MouseButtonMask.Left);
            Left(uiPoint + new Vector2(40, 0), false);
            Check(camera.Position == positionBefore, "A GUI click/drag must not start world-camera panning");
            Move(pointer, Vector2.Zero);
            Left(pointer, true);
            Move(pointer + new Vector2(40, 0), new Vector2(40, 0), MouseButtonMask.Left);
            Check(camera.Position.X < positionBefore.X, "Dragging empty world space must still pan the standalone camera");

            Paused = true;
            Vector2 pausedPosition = camera.Position;
            Vector2 pausedZoom = camera.Zoom;
            camera._Process(1);
            camera._Input(new InputEventMouseMotion { Relative = new Vector2(100, 100) });
            Check(camera.Position == pausedPosition && camera.Zoom == pausedZoom,
                "Pause must freeze the independent camera's motion and zoom");
            Paused = false;
            Move(pointer + new Vector2(60, 0), new Vector2(20, 0), MouseButtonMask.Left);
            Check(camera.Position == pausedPosition, "Resuming must not revive a mouse drag that began before pause");
            Left(pointer + new Vector2(60, 0), false);
        }
        finally
        {
            Paused = false;
            scene.QueueFree();
        }
        await ToSignal(this, SignalName.ProcessFrame);
        Vector2I originalSize = Root.Size;
        Root.Size = new Vector2I(originalSize.X - 20, originalSize.Y);
        await ToSignal(this, SignalName.ProcessFrame);
        Root.Size = originalSize;
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Standalone camera, zoom anchor, GUI input isolation and pause lifecycle checks passed");
    }

}
