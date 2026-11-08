using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
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

            var rarity = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
            rarity.Show();
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(rarity.Popup.IsVisibleInTree() && Root.GetVisibleRect().Encloses(ScreenBounds(rarity.Popup)),
                "Rarity popup placement must work after removing the pause-button obstacle");
            rarity.Hide();
            var crafting = game.GetNode<EmbroideryGame>("EmbroideryGame");
            crafting.Open();
            Check(crafting.IsOpen, "Crafting fixture must open");
            await PressEscapeForTest();
            Check(!crafting.IsOpen && !Paused && !overlay.Visible,
                "Escape cancelling crafting must not also open pause");
            game.DayUpgrades.Offer(2);
            var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
            card.Open(1, 2, game.DayUpgrades.Options, game.Score);
            await PressEscapeForTest();
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

    private async Task TestPauseAndZoomUi()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        var ui = game.GetNode<ZoomResponsiveUi>("ZoomResponsiveUi");
        var panel = game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
        var hud = game.GetNode<Control>("HUD/Root/ProgressPanel");
        var headers = new[] { "Atelier/Header", "Atelier/NameLabel", "Atelier/HintLabel",
            "MusicWorkshop/Header", "MusicWorkshop/NameLabel", "MusicWorkshop/HintLabel",
            "Showroom/ShowroomHeader", "Showroom/ShowroomLabel", "Showroom/ShowroomHint",
            "ShowroomLeft/ShowroomHeader", "ShowroomLeft/ShowroomLabel", "ShowroomLeft/ShowroomHint"
            }.Select(path => game.GetNode<CanvasItem>("World/" + path)).ToArray();
        Check(!game.HasNode("World/TradingRoom/SalesRoomLabel"), "The trading room heading must be removed");
        ui.UpdateForZoom(0.23f);
        Check(headers.All(header => !header.Visible && Mathf.IsZeroApprox(header.Modulate.A)),
            "All room titles, descriptions and backplates must disappear at zoom 0.23");
        Check(panel.Scale.X > 1 && panel.Scale.X <= 1.2f && hud.Scale == Vector2.One,
            "Small world UI must grow moderately on zoom-out while HUD stays fixed");
        ui.UpdateForZoom(0.29f);
        Check(headers.All(header => header.Visible && header.Modulate.A > 0 && header.Modulate.A < 1),
            "Room headings must fade in gradually above 0.23");
        ui.UpdateForZoom(0.35f);
        Check(headers.All(header => header.Visible && Mathf.IsEqualApprox(header.Modulate.A, 1)),
            "Room headings must reach full opacity at 0.35");
        ui.UpdateForZoom(0.1f);
        Check(Mathf.IsEqualApprox(panel.Scale.X, 1.2f), "Trade panel zoom compensation must be capped");
        foreach (float zoom in new[] { 1f, 1.8f })
        {
            ui.UpdateForZoom(zoom);
            Check(panel.Scale.X <= 1 && panel.Scale.X * zoom <= 1.08f,
                "Zooming in must keep the trade panel's screen size bounded without cumulative growth");
        }
        foreach (float zoom in new[] { 0.23f, 0.3f, 0.5f, 1f, 1.8f })
        {
            ui.UpdateForZoom(zoom);
            foreach (string room in new[] { "Atelier", "MusicWorkshop" })
            {
                var button = game.GetNode<Control>($"World/{room}/GetItemButton");
                var hint = button.GetNode<Control>("PriceHint");
                Rect2 rect = hint.GetTransform() * new Rect2(Vector2.Zero, hint.Size);
                Check(Mathf.Abs(-rect.End.Y * zoom - 8) < 0.1f &&
                      Mathf.Abs(rect.GetCenter().X - button.Size.X / 2) < 0.1f,
                    "Price hints must remain centered above both buttons with an eight-screen-pixel gap at every zoom");
            }
        }
        Check(TradeScene.GetItemBasePrice(0) == 30 && TradeScene.GetItemBasePrice(1) == 50 &&
              TradeScene.GetItemBasePrice(2) == 45 && TradeScene.GetItemBasePrice(3) == 75,
            "Inventory prices must match the current trade catalog");

        CraftAtelier(game);
        game.SelectInventoryItem(0);
        game.PlaceItemInShowcase(0);
        double countdown = game.BuyerCountdown;
        Root.PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
        Check(Paused && game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
            "Escape must open a modal pause instead of quitting");
        var camera = game.GetNode<Camera2D>("Camera2D");
        Vector2 zoomBefore = camera.Zoom;
        int score = game.Score;
        int day = game.CurrentDay;
        // This HUD position lies outside the centered menu; the modal shade must intercept it.
        await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
        Vector2 position = camera.Position;
        Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
        Root.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = false }, true);
        Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
        Root.PushInput(new InputEventMouseMotion { Relative = new Vector2(100, 100) }, true);
        game._Process(5);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(Paused && !game.TradeActive && game.BuyerCountdown == countdown && game.Score == score && game.CurrentDay == day &&
              camera.Zoom == zoomBefore && camera.Position == position,
            "Pause must freeze buyer time, purchases, camera and game hotkeys");
        const string content = "PauseMenu/Root/Overlay/Center/Panel/Content/";
        await ClickControl(game, content + "Menu/SettingsButton");
        Check(Paused && game.GetNode<Control>(content + "Settings").Visible,
            "Settings must open inside pause without resuming gameplay");
        Check(game.GetNode<CheckButton>(content + "Settings/FullscreenButton").IsVisibleInTree(),
            "Pause settings must expose the fullscreen control");
        CheckFullscreenText(game.GetNode<CheckButton>(content + "Settings/FullscreenButton"));
        Root.PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
        Check(Paused && game.GetNode<Control>(content + "Menu").Visible,
            "Escape in settings must return to the pause menu");
        Root.PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
        Check(!Paused && !game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
            "Escape in pause must resume gameplay");
        game._Process(countdown + 0.001);
        Check(game.TradeActive, "Buyer time must continue after resuming");
        int rerolls = game.RerollsRemaining;
        await PressEscapeForTest();
        Check(Paused, "Escape must open the pause menu during an active trade");
        Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
        Check(game.RerollsRemaining == rerolls, "Pause must block reroll hotkeys during an active trade");
        await ClickControl(game, content + "Menu/ResumeButton");
        Check(!Paused, "Continue must resume the game");
        await PressEscapeForTest();
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        Check(!Paused, "Removing a paused game must release its pause");
        GD.Print("Pause, zoom readability and room heading checks passed");
    }
}
