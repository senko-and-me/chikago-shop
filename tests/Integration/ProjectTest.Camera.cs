using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestPosterNavigation()
    {
        // Exercise real viewport dispatch and Area2D picking with the authored poster.
        var authored = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate();
        var poster = authored.GetNode<TradingRoomPosterHotspot>("World/TradingRoom/ShowPoster");
        poster.GetParent().RemoveChild(poster);
        authored.Free();
        var scene = new Node2D();
        var bounds = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://art/rooms/room_layout.jpg"),
            Centered = false, Scale = Vector2.One * 4
        };
        scene.AddChild(bounds);
        poster.Position = bounds.GetRect().Size * bounds.Scale / 2 - new Vector2(55.5f, 83);
        scene.AddChild(poster);
        var camera = new WorldCamera { BoundsSource = bounds };
        scene.AddChild(camera);
        Root.AddChild(scene);
        camera.SetProcess(false);
        camera.Zoom = Vector2.One * 0.7f;
        camera.ClampToBounds();
        camera.ForceUpdateScroll();
        var overlay = poster.GetNode<Control>("PosterOverlay/Root");
        async Task Pick()
        {
            await ToSignal(this, SignalName.PhysicsFrame);
            await ToSignal(this, SignalName.PhysicsFrame);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        void Move(Vector2 point, Vector2 relative, MouseButtonMask mask = 0) =>
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                Relative = relative, ButtonMask = mask }, true);
        void Left(Vector2 point, bool pressed) => Root.PushInput(new InputEventMouseButton
        {
            Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
            Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0
        }, true);
        Vector2 PosterPoint() => poster.GetGlobalTransformWithCanvas() * new Vector2(55.5f, 83);
        try
        {
            await Pick();
            foreach (bool returnToStart in new[] { false, true })
            {
                Vector2 point = PosterPoint();
                Move(point, Vector2.Zero);
                await Pick();
                Vector2 before = camera.Position;
                Left(point, true);
                await Pick();
                Check(!overlay.Visible && !Paused, "Pressing the poster must leave LMB camera navigation available");
                Vector2 offset = new(40, 20);
                Move(point + offset, offset, MouseButtonMask.Left);
                await Pick();
                Check(camera.Position.DistanceTo(before) > 1 && !overlay.Visible && !Paused,
                    "Dragging from the poster must move the real camera without opening or pausing");
                if (returnToStart)
                {
                    Move(point, -offset, MouseButtonMask.Left);
                    await Pick();
                }
                Left(returnToStart ? point : point + offset, false);
                await Pick();
                Check(!overlay.Visible && !Paused, "Releasing a drag must not open the poster, even after returning to the press point");
            }
            Vector2 click = PosterPoint();
            Move(click, Vector2.Zero);
            await Pick();
            Left(click, true);
            await Pick();
            Left(click + new Vector2(100, 0), false);
            await Pick();
            Check(!overlay.Visible && !Paused, "A distant release must cancel the click even without a motion event");
            Left(click, false);
            await Pick();
            Check(!overlay.Visible && !Paused, "Releasing over the poster without a press must not open it");
            Left(click, true);
            await Pick();
            Paused = true;
            await Pick();
            Paused = false;
            Left(click, false);
            await Pick();
            Check(!overlay.Visible && !Paused, "Pause must cancel a pending poster click");

            Left(click, true);
            await Pick();
            Check(!overlay.Visible && !Paused, "A simple press must wait for release");
            Move(click + new Vector2(2, 0), new Vector2(2, 0), MouseButtonMask.Left);
            Left(click + new Vector2(2, 0), false);
            await Pick();
            Check(overlay.Visible && Paused, "A simple LMB click must open and pause on release");
            Root.PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
            await ToSignal(CreateTimer(0.25, true), SceneTreeTimer.SignalName.Timeout);
            Check(!overlay.Visible && !Paused, "Escape must still close the poster and resume navigation");
        }
        finally
        {
            Paused = false;
            scene.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }

    private async Task TestRoomTitles()
    {
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Check(game.GetNode<Label>("World/MusicWorkshop/NameLabel").Text == "МАСТЕРСКАЯ",
            "The designer's new second-room name must be stored in the scene");
        foreach (var workshop in EconomyConfigProvider.Load().Workshops)
        {
            string room = workshop.Id == "atelier" ? "Atelier" : "MusicWorkshop";
            string preview = game.GetNode<Label>($"World/{room}/GetItemButton/PriceHint/Label").Text;
            Check(preview == $"1 предмет  •  {workshop.SpinCost}$ + 1 энергия",
                "The authored price preview must match the default single-item economy before Ready");
        }
        var tradePanel = game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
        Check(tradePanel.ZIndex == 125 && !tradePanel.ZAsRelative, "The trade panel's initial drawing layer must be authored in the scene");
        tradePanel.ZIndex = 127;
        var unlockPanel = game.GetNode<MusicWorkshopUnlockPanel>("World/MusicWorkshop/LockedOverlay/UnlockPanel");
        var authoredPaper = (StyleBoxTexture)unlockPanel.GetThemeStylebox("panel").Duplicate();
        authoredPaper.ContentMarginLeft = 47;
        unlockPanel.AddThemeStyleboxOverride("panel", authoredPaper);
        var authoredButton = (StyleBoxTexture)unlockPanel.GetNode<Button>("Content/UnlockButton").GetThemeStylebox("normal").Duplicate();
        authoredButton.ModulateColor = new Color(0.81f, 0.82f, 0.83f);
        unlockPanel.GetNode<Button>("Content/UnlockButton").AddThemeStyleboxOverride("normal", authoredButton);
        var glass = game.GetNode<Node2D>("World/ShowroomLeft/Showcases").GetChild(0).GetNode<ColorRect>("Glass");
        var authoredGlass = new Color(0.2f, 0.3f, 0.4f, 0.5f);
        glass.Color = authoredGlass;
        game.GetNode<Control>("World/ShowroomLeft/ShowroomHeader").Hide();
        var customTitle = game.GetNode<Label>("World/ShowroomLeft/ShowroomLabel");
        customTitle.Text = "ПОЛКИ";
        customTitle.Position += new Vector2(7, 3);
        var entries = new[] { "Atelier", "MusicWorkshop", "ShowroomLeft", "Showroom" }.Select(room =>
        {
            var node = game.GetNode("World/" + room);
            var plate = node.GetNodeOrNull<Control>("Header") ?? node.GetNode<Control>("ShowroomHeader");
            var title = node.GetNodeOrNull<Label>("NameLabel") ?? node.GetNode<Label>("ShowroomLabel");
            Check(!node.HasNode("HintLabel") && !node.HasNode("ShowroomHint"),
                "Obsolete room descriptions must be absent from the authored scene, not hidden at runtime");
            return (Room: room, Plate: plate, Title: title, Text: title.Text,
                PlateVisible: plate.Visible,
                PlatePosition: plate.Position, PlateSize: plate.Size, PlateScale: plate.Scale,
                TitlePosition: title.Position, TitleSize: title.Size, TitleScale: title.Scale);
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
                Check(entries.All(entry => !entry.Plate.Visible && !entry.Title.Visible),
                    "The real maximum overview must hide authored room headings and plates in every viewport");
                if (size.X == 1280 && language == "ru") using (await CapturePresentation("room-titles-overview")) { }
                camera.Zoom = Vector2.One * camera.OverviewZoom * 1.2f;
                ui.UpdateForZoom(camera.Zoom.X);
                Check(entries.All(entry => !entry.Title.Visible && Mathf.IsZeroApprox(entry.Title.Modulate.A)),
                    "Titles must stay hidden through twenty percent zoom above overview");
                camera.Zoom = Vector2.One * camera.OverviewZoom * 1.4f;
                ui.UpdateForZoom(camera.Zoom.X);
                Check(entries.All(entry => entry.Title.Visible && entry.Title.Modulate.A > 0 && entry.Title.Modulate.A < 1),
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
                    Check(entry.Title.Visible && Mathf.IsEqualApprox(entry.Title.Modulate.A, 1) &&
                        entry.Plate.Visible == entry.PlateVisible &&
                        entry.Plate.Position == entry.PlatePosition && entry.Plate.Size == entry.PlateSize && entry.Plate.Scale == entry.PlateScale &&
                        entry.Title.Position == entry.TitlePosition && entry.Title.Size == entry.TitleSize && entry.Title.Scale == entry.TitleScale &&
                        entry.Title.Text == entry.Text && plateRect.Encloses(titleRect) && textWidth <= entry.Title.Size.X,
                        $"The authored {entry.Room} geometry and name must survive startup, zoom, {language} and {size}");
                    if (size.X == 1280)
                        using (await CapturePresentation($"room-title-{entry.Room}-{language}")) { }
                }
                game.RefreshDayPresentation();
                await Frame();
                Check(ReferenceEquals(unlockPanel.GetThemeStylebox("panel"), authoredPaper) &&
                    ReferenceEquals(unlockPanel.UnlockButton.GetThemeStylebox("normal"), authoredButton) && glass.Color == authoredGlass &&
                    tradePanel.ZIndex == 127,
                    "Startup and game/locale refresh must preserve designer-authored paper, button styles and glass tint");
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
        GD.Print("ROOM_TITLE_CHECKS_PASS: authored titles/geometry/styles/tint, no description nodes, zoom, RU/EN and three viewports");
    }

    private async Task TestRoomDoubleClickFocus()
    {
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var camera = game.GetNode<WorldCamera>("Camera2D");
        int navigationEvents = 0;
        camera.UserPanned += _ => navigationEvents++;
        camera.UserZoomed += () => navigationEvents++;
        void Click(Vector2 point, bool twice = true)
        {
            MoveButtonPointer(point);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = twice, ButtonMask = MouseButtonMask.Left }, true);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = false }, true);
        }
        async Task Settle()
        {
            ulong deadline = Time.GetTicksMsec() + 2000;
            while (camera.IsFocusing)
            {
                Check(Time.GetTicksMsec() < deadline, "Room focus must finish within its bounded transition");
                await ToSignal(this, SignalName.ProcessFrame);
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = size;
                await ToSignal(this, SignalName.ProcessFrame);
                foreach (string room in new[] { "Atelier", "Corridor", "TradingRoom", "ShowroomLeft", "MusicWorkshop", "Showroom" })
                {
                    camera.ResetToOverview();
                    await ToSignal(this, SignalName.ProcessFrame);
                    var frame = game.GetNode<Control>($"World/{room}/RoomFrame");
                    Rect2 bounds = frame.GetGlobalTransform() * new Rect2(Vector2.Zero, frame.Size);
                    Vector2 worldPoint = bounds.Position + bounds.Size * new Vector2(0.1f, 0.4f);
                    Vector2 point = Root.GetCanvasTransform() * worldPoint;
                    Vector2 startPosition = camera.Position;
                    float startZoom = camera.Zoom.X;
                    Click(point, twice: false);
                    Check(!camera.IsFocusing && camera.Position == startPosition && camera.Zoom.X == startZoom,
                        "A single room click must retain the current view");
                    Click(point);
                    Check(camera.IsFocusing && !camera.IsDragging && camera.Position == startPosition,
                        $"Double-clicking {room} must begin a smooth focus, without a drag or an instant jump");
                    if (room == "TradingRoom" && size.X == 1280)
                    {
                        game.GetNode<PauseMenu>("PauseMenu").Open();
                        Vector2 pausedPosition = camera.Position;
                        Vector2 pausedZoom = camera.Zoom;
                        await ToSignal(CreateTimer(0.12, true), SceneTreeTimer.SignalName.Timeout);
                        Check(camera.Position == pausedPosition && camera.Zoom == pausedZoom, "Pause must freeze a room focus flight");
                        game.GetNode<PauseMenu>("PauseMenu").Close();
                    }
                    await Settle();
                    Check(camera.Position.DistanceTo(bounds.GetCenter()) < 0.1f && camera.Zoom.X > startZoom,
                        $"Double click must center and enlarge the actual {room} geometry at {size}");
                    if (room == "TradingRoom") using (await CapturePresentation($"room-double-click-{size.X}")) { }
                }
            }
            camera.ResetToOverview();
            await ToSignal(this, SignalName.ProcessFrame);
            Vector2 originalPosition = camera.Position;
            float originalZoom = camera.Zoom.X;
            var pack = game.GetNode<Control>("HUD/Root/ProgressToggleButton");
            Click(ScreenBounds(pack).GetCenter());
            Check(!camera.IsFocusing && camera.Position == originalPosition && camera.Zoom.X == originalZoom,
                "A double click on HUD controls must never focus a room behind them");
            var slot = game.GetNode<Control>("World/ShowroomLeft/Showcases/Slot1");
            Click(ScreenBounds(slot).GetCenter());
            Check(!camera.IsFocusing, "Interactive showcase slots must consume double clicks instead of focusing their room");
            var targetFrame = game.GetNode<Control>("World/Corridor/RoomFrame");
            Vector2 targetPoint = ScreenBounds(targetFrame).GetCenter();
            camera.NavigationLocked = true;
            Click(targetPoint);
            Check(!camera.IsFocusing, "Tutorial navigation lock must block double-click focus");
            camera.NavigationLocked = false;
            game.GetNode<PauseMenu>("PauseMenu").Open();
            Click(targetPoint);
            Check(!camera.IsFocusing, "Pause must block a new double-click focus");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            if (!game.DayStarted) game.EndDay();
            game.GetNode<EmbroideryGame>("EmbroideryGame").Open();
            Check(game.CraftMiniGameOpen,
                $"Modal fixture must open after Start: started={game.DayStarted}, closed={game.DayClosed}, paused={Paused}, money={game.Score}, energy={game.EnergyRemaining}, failed={game.Failed}");
            Click(targetPoint);
            Check(!camera.IsFocusing, "A crafting modal must consume double clicks before world navigation");
            await PressEscapeForTest();
            Check(navigationEvents == 0, "Room flights must not count as manual tutorial pan or wheel input");
        }
        finally
        {
            Paused = false;
            Root.Size = originalSize;
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
        }
        GD.Print("ROOM_FOCUS_CHECKS_PASS: six rooms, both sizes, smooth center/zoom, single click, GUI/modal/lock/pause");
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

            var rarity = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
            rarity.Show();
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(rarity.Popup.IsVisibleInTree() && Root.GetVisibleRect().Encloses(ScreenBounds(rarity.Popup)),
                "Rarity popup placement must work after removing the pause-button obstacle");
            rarity.Hide();
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

    private async Task TestPauseAndZoomUi()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        var ui = game.GetNode<ZoomResponsiveUi>("ZoomResponsiveUi");
        var panel = game.GetNode<Control>("World/TradingRoom/Item/TradePanel");
        var hud = game.GetNode<Control>("HUD/Root/ProgressPanel");
        var headers = new[] { "Atelier/Header", "Atelier/NameLabel",
            "MusicWorkshop/Header", "MusicWorkshop/NameLabel",
            "Showroom/ShowroomHeader", "Showroom/ShowroomLabel",
            "ShowroomLeft/ShowroomHeader", "ShowroomLeft/ShowroomLabel"
            }.Select(path => game.GetNode<CanvasItem>("World/" + path)).ToArray();
        Check(!game.HasNode("World/TradingRoom/SalesRoomLabel"), "The trading room heading must be removed");
        float overviewZoom = game.GetNode<WorldCamera>("Camera2D").OverviewZoom;
        ui.UpdateForZoom(overviewZoom * 1.2f);
        Check(headers.All(header => !header.Visible && Mathf.IsZeroApprox(header.Modulate.A)),
            "All room titles and backplates must disappear near the current overview");
        Check(panel.Scale.X > 1 && panel.Scale.X <= 1.2f && hud.Scale == Vector2.One,
            "Small world UI must grow moderately on zoom-out while HUD stays fixed");
        ui.UpdateForZoom(overviewZoom * 1.4f);
        Check(headers.All(header => header.Visible && header.Modulate.A > 0 && header.Modulate.A < 1),
            "Room headings must fade in gradually above overview");
        ui.UpdateForZoom(overviewZoom * 1.6f);
        Check(headers.All(header => header.Visible && Mathf.IsEqualApprox(header.Modulate.A, 1)),
            "Room headings must reach full opacity after zooming in sixty percent from overview");
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
        double countdown = game.BuyerWaitRemaining;
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
        Check(Paused && !game.TradeActive && game.BuyerWaitRemaining == countdown && game.Score == score && game.CurrentDay == day &&
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
