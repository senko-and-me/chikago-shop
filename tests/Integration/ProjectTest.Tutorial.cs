using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestFirstTradeTutorial()
    {
        const string save = "user://first_trade_tutorial_test.json";
        const string progress = save + ".tutorial.cfg";
        Vector2I size = Root.Size;
        string locale = TranslationServer.GetLocale();
        // Keep this C# script alive while the fixture repeatedly destroys/recreates its pause menu.
        var retainedFullscreenScript = GD.Load<Script>("res://scripts/UI/FullscreenToggle.cs");
        TradeScene game = null;
        FirstTradeTutorial tutorial = null;
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        async Task CameraSettled()
        {
            var camera = game.GetNode<WorldCamera>("Camera2D");
            ulong deadline = Time.GetTicksMsec() + 3000;
            while (camera.IsFocusing || camera.IsReturningToOverview)
            {
                Check(Time.GetTicksMsec() < deadline, "Tutorial camera transition must complete");
                await Frame();
            }
            await Frame();
        }
        async Task<bool> CheckBuyerRequestLesson()
        {
            var body = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            TranslationServer.SetLocale("en");
            await Frame();
            bool english = body.Tr(body.Text).Contains("clothing or a musical instrument") &&
                body.Tr(body.Text).Contains("clothing from the atelier");
            TranslationServer.SetLocale("ru");
            await Frame();
            return english && body.Text.Contains("одежду или музыкальный инструмент") &&
                body.Text.Contains("Сейчас этому покупателю нужна одежда из ателье") &&
                tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done").Visible;
        }
        void CheckEmptyTutorialCounter()
        {
            int arrivals = 0;
            System.Action arrived = () => arrivals++;
            game.BuyerArrived += arrived;
            int money = game.Score, energy = game.EnergyRemaining;
            int[] stock = game.StoredItems;
            game._Process(0);
            game._Process(1);
            game.BuyerArrived -= arrived;
            Check(!game.TradeActive && !game.GetNode<Node2D>("World/TradingRoom/Customer").Visible &&
                !game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble").IsVisibleInTree() &&
                arrivals == 0 && game.Score == money && game.EnergyRemaining == energy && game.StoredItems.SequenceEqual(stock),
                "After the training sale the old customer must leave and no next customer or bell may appear during Summary");
        }
        void MouseButtonAt(Vector2 point, MouseButton button, bool pressed) => Root.PushInput(new InputEventMouseButton
        { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = pressed,
            ButtonMask = pressed && button == MouseButton.Left ? MouseButtonMask.Left : 0 }, true);
        async Task PanCamera(int segments = 24)
        {
            Vector2 point = new(30, Root.Size.Y * 0.55f);
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            await Frame();
            MouseButtonAt(point, MouseButton.Left, true);
            for (int i = 0; i < segments; i++)
            {
                Vector2 displacement = new(i % 2 == 0 ? 25 : -25, 0);
                point += displacement;
                Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                    Relative = displacement, ButtonMask = MouseButtonMask.Left }, true);
            }
            MouseButtonAt(point, MouseButton.Left, false);
            await Frame();
        }
        async Task CheckCameraLocked()
        {
            await CameraSettled();
            var camera = game.GetNode<WorldCamera>("Camera2D");
            Check(camera.NavigationLocked, "Camera must stay locked after navigation practice");
            Vector2 position = camera.Position, zoom = camera.Zoom;
            camera.SetProcess(true);
            await PanCamera();
            Vector2 point = new(30, Root.Size.Y * 0.55f);
            MouseButtonAt(point, MouseButton.WheelUp, true);
            MouseButtonAt(point, MouseButton.WheelUp, false);
            await Frame();
            Check(camera.Position.IsEqualApprox(position) && camera.Zoom.IsEqualApprox(zoom),
                "Dragging and wheel input must not change a settled tutorial checkpoint");
            camera.SetProcess(false);
        }
        void CheckFreshRun()
        {
            Check(game.DayRemainingSeconds == WorkshopItemCatalog.DayDurationSeconds,
                "Leaving the tutorial must restore the ordinary day duration");
            Check(game.CurrentDay == 1 && !game.DayStarted && !game.DayClosed &&
                game.Score == game.StartingMoney && game.EnergyRemaining == game.EnergyCapacity &&
                game.Accepted == 0 && game.Declined == 0 && game.QuotaStage == 0 &&
                game.StoredItems.Length == 0 && System.Array.TrueForAll(game.ShowcaseItems, item => item < 0) &&
                !game.TradeActive && !game.MusicWorkshopUnlocked && !game.GetNode<WorldCamera>("Camera2D").NavigationLocked,
                "Leaving tutorial must restore a fresh playable day, resources, items and free camera");
        }
        async Task Create()
        {
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            Root.AddChild(game);
            // A new panel has no cached pointer; keep the fixture independent of the OS cursor after reload.
            MoveButtonPointer(new Vector2(800, 400));
            game.SetProcess(false);
            game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
            tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            await Frame();
        }
        async Task Reload()
        {
            game.QueueFree();
            await Frame();
            await Create();
        }
        void CheckSkipPlacement()
        {
            var skip = tutorial.GetNode<Button>("Root/Skip");
            var panel = tutorial.GetNode<Control>("Root/Panel");
            Rect2 screen = Root.GetVisibleRect(), bounds = skip.GetGlobalRect();
            Check(skip.IsVisibleInTree() && !panel.IsAncestorOf(skip) &&
                !tutorial.HasNode("Root/Panel/Content/Actions/Skip") && screen.Encloses(bounds) &&
                Mathf.IsEqualApprox(screen.End.X - bounds.End.X, 20) &&
                Mathf.IsEqualApprox(screen.End.Y - bounds.End.Y, 20),
                "Skip must stay outside hint windows, twenty pixels from the bottom-right screen corner");
        }
        void Step(FirstTradeTutorial.Step expected)
        {
            Check(tutorial.IsActive && tutorial.IsShowing && tutorial.CurrentStep == expected,
                $"Tutorial must follow actual gameplay state: expected {expected}, got {tutorial.CurrentStep}");
            CheckSkipPlacement();
            if (expected is FirstTradeTutorial.Step.Craft or FirstTradeTutorial.Step.Select or
                FirstTradeTutorial.Step.Place or FirstTradeTutorial.Step.WaitBuyer)
                Check(!game.GetNode<Node2D>("World/TradingRoom/Customer").Visible &&
                    !game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble").IsVisibleInTree(),
                    "Before the buyer-request lesson there must be no customer or request bubble");
        }
        ProgressBar CameraBar() => tutorial.GetNode<ProgressBar>("Root/Panel/Content/CameraProgress");
        async Task CheckTutorialEndBlocked()
        {
            int day = game.CurrentDay, score = game.Score, energy = game.EnergyRemaining;
            double remaining = game.DayRemainingSeconds;
            bool trading = game.TradeActive;
            var step = tutorial.CurrentStep;
            game.EndDay();
            await ClickControl(game, "HUD/DayClock/Root/ClockActionButton");
            game.GetNode<Button>("HUD/Root/ProgressPanel/Content/EndDayButton").EmitSignal(Button.SignalName.Pressed);
            await Frame();
            Check(game.DayStarted && !game.DayClosed && !game.DayTransitionOpen &&
                game.CurrentDay == day && game.DayRemainingSeconds == remaining &&
                game.Score == score && game.EnergyRemaining == energy && game.TradeActive == trading &&
                tutorial.IsActive && tutorial.CurrentStep == step,
                "Direct, real clock and legacy end-day actions must preserve the active tutorial and day");
            var caption = game.GetNode<Label>("HUD/DayClock/Root/DayTimer/EndDayCaption");
            Check(!caption.Visible &&
                game.GetNode<Button>("HUD/DayClock/Root/ClockActionButton").TooltipText == "" &&
                game.GetNode<Label>("HUD/DayClock/Root/DayTimer").Text ==
                    ((int)System.Math.Ceiling(remaining)).ToString("00"),
                "Hovering or clicking the training clock must keep its countdown without a completion caption or tooltip");
            MoveButtonPointer(new Vector2(800, 400));
        }
        async Task CheckPracticeLayout(string stage)
        {
            foreach (Vector2I viewport in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = viewport;
                TranslationServer.SetLocale(language);
                await Frame();
                var panel = tutorial.GetNode<Control>("Root/Panel");
                CheckSkipPlacement();
                var title = tutorial.GetNode<Label>("Root/Panel/Content/Title");
                Check(Root.GetVisibleRect().Encloses(panel.GetGlobalRect()) &&
                    panel.GetGlobalRect().Encloses(CameraBar().GetGlobalRect()) && CameraBar().ShowPercentage &&
                    title.Tr(title.Text) == (tutorial.CurrentStep == FirstTradeTutorial.Step.Camera
                        ? language == "en" ? "Camera movement" : "Передвижение камеры"
                        : language == "en" ? "Zooming" : "Масштабирование"),
                    "Each camera stage must show its own localized title and a fitted percentage bar");
                using (await CapturePresentation($"tutorial-{stage}-{language}-{viewport.X}")) { }
            }
        }
        void Highlight()
        {
            var outline = tutorial.GetNode<Control>("Root/Highlight");
            Rect2 target = tutorial.Target.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, tutorial.Target.Size);
            Check(outline.Visible && Root.GetVisibleRect().Encloses(outline.GetGlobalRect()) &&
                outline.GetGlobalRect().Intersects(target), "The highlight must follow the actual screen-space target without capturing clicks");
            Check(outline.MouseFilter == Control.MouseFilterEnum.Ignore && tutorial.GetNode<Control>("Root").MouseFilter == Control.MouseFilterEnum.Ignore,
                "Tutorial highlights and full-screen root must leave gameplay input available");
        }
        async Task CheckYellowHighlight(string name)
        {
            var outline = tutorial.GetNode<Control>("Root/Highlight");
            using (var image = await CapturePresentation(name))
            {
                Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
                Rect2 rect = ScreenBounds(outline);
                int yellow = 0;
                for (int y = (int)(rect.Position.Y * scale.Y); y < (int)(rect.End.Y * scale.Y); y++)
                for (int x = (int)(rect.Position.X * scale.X); x < (int)(rect.End.X * scale.X); x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    if (pixel.R > 0.8f && pixel.G > 0.6f && pixel.B < 0.25f) yellow++;
                }
                Check(yellow > 10, "The tutorial frame must keep its authored yellow border");
                CheckRenderedGray(image, ScreenBounds(outline),
                    ScreenBounds(game.GetNode<Control>("HUD/Root/EnergyPanel/EnergyBulbs")));
            }
            // The clock already preserves its original color; use the pack to isolate the new frame exception.
            if (tutorial.Target.Name != "ProgressToggleButton") return;
            var probe = new ColorRect { Color = Colors.Red, Size = new Vector2(12, 12),
                Position = outline.Size / 2 - new Vector2(6, 6), MouseFilter = Control.MouseFilterEnum.Ignore };
            outline.AddChild(probe);
            using (var image = await CapturePresentation())
            {
                Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
                Vector2 center = ScreenBounds(probe).GetCenter() * scale;
                Color pixel = image.GetPixel((int)center.X, (int)center.Y);
                Check(HasChroma(pixel) && pixel.R > 0.8f && pixel.G < 0.2f,
                    "The hollow tutorial highlight must leave source colors unchanged without a screen filter");
            }
            probe.QueueFree();
            await Frame();
        }
        try
        {
            foreach (string path in new[] { save, progress }) if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
            Root.Size = new Vector2I(1280, 720);
            await Create();
            Step(FirstTradeTutorial.Step.Camera);
            var initialCamera = game.GetNode<WorldCamera>("Camera2D");
            Check(tutorial.GetNode("CameraCheckpoints").GetChildCount() == 8 && initialCamera.IsFocusing,
                "Every tutorial checkpoint must be scene-authored and entry must fly to the practice view");
            Vector2 initialPosition = initialCamera.Position;
            float initialZoom = initialCamera.Zoom.X;
            await ToSignal(CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
            Check(initialCamera.IsFocusing && (initialCamera.Position != initialPosition || initialCamera.Zoom.X != initialZoom) &&
                !tutorial.CameraPanDone && !tutorial.CameraZoomDone,
                "An intermediate camera frame must move smoothly without completing the manual practice");
            await CameraSettled();
            Check(!initialCamera.NavigationLocked, "Practice must allow manual camera movement");
            Check(CameraBar().Value == 0, "Movement must start at zero percent");
            await CheckPracticeLayout("movement-zero");
            await CameraSettled();
            Vector2 earlyWheelPoint = new(30, Root.Size.Y * 0.55f);
            MouseButtonAt(earlyWheelPoint, MouseButton.WheelUp, true);
            MouseButtonAt(earlyWheelPoint, MouseButton.WheelUp, false);
            await Frame();
            Check(tutorial.CameraZoomProgress == 0 && !tutorial.CameraZoomDone,
                "Zooming before its separate stage must not complete or accumulate zoom progress");
            using (var image = await CapturePresentation("tutorial-camera")) { }
            var panelRect = tutorial.GetNode<Control>("Root/Panel").GetGlobalRect();
            Vector2 uiPoint = panelRect.Position + new Vector2(20, 20);
            MouseButtonAt(uiPoint, MouseButton.Left, true);
            Root.PushInput(new InputEventMouseMotion { Position = uiPoint + new Vector2(90, 0),
                GlobalPosition = uiPoint + new Vector2(90, 0), Relative = new Vector2(90, 0), ButtonMask = MouseButtonMask.Left }, true);
            MouseButtonAt(uiPoint + new Vector2(90, 0), MouseButton.Left, false);
            await Frame();
            Check(!tutorial.CameraPanDone && !tutorial.CameraZoomDone, "Dragging a tutorial panel must not count as world navigation");
            await PanCamera(12);
            Check(CameraBar().Value > 0 && CameraBar().Value < 100 && !tutorial.CameraPanDone,
                "A short drag must accumulate partial progress without ending movement practice");
            Check(Mathf.IsEqualApprox(tutorial.CameraPanProgress, 10),
                "300 accepted screen pixels must fill exactly 10% of the 3000px goal");
            float partialPan = tutorial.CameraPanProgress;
            using (await CapturePresentation("tutorial-movement-partial")) { }
            await Reload();
            Check(Mathf.IsEqualApprox(tutorial.CameraPanProgress, partialPan), "Partial movement percent must survive reload");
            await CameraSettled();
            await PanCamera(107);
            Check(!tutorial.CameraPanDone && Mathf.IsEqualApprox(tutorial.CameraPanProgress, 2975f / 3000 * 100),
                "2975px must remain below completion after restoring partial movement");
            await PanCamera(1);
            Check(tutorial.CameraPanDone && !tutorial.CameraZoomDone, "Real left-button background drag must complete only movement");
            Step(FirstTradeTutorial.Step.Camera);
            Check(CameraBar().Value == 100, "The completed movement stage must display 100% before advancing");
            var movementCamera = game.GetNode<WorldCamera>("Camera2D");
            Vector2 movementPosition = movementCamera.Position, movementZoom = movementCamera.Zoom;
            Vector2 holdPoint = new(30, Root.Size.Y * 0.55f);
            MouseButtonAt(holdPoint, MouseButton.Left, true);
            await ToSignal(CreateTimer(0.7), SceneTreeTimer.SignalName.Timeout);
            Step(FirstTradeTutorial.Step.Camera);
            Check(movementCamera.IsDragging && CameraBar().Value == 100,
                "Movement at 100% must remain on its stage until the player releases the drag");
            MouseButtonAt(holdPoint, MouseButton.Left, false);
            await ToSignal(CreateTimer(0.7), SceneTreeTimer.SignalName.Timeout);
            Step(FirstTradeTutorial.Step.CameraZoom);
            Check(CameraBar().Value == 0 && !movementCamera.IsFocusing &&
                movementCamera.Position.IsEqualApprox(movementPosition) && movementCamera.Zoom.IsEqualApprox(movementZoom),
                "Zoom must start at 0% without another automatic camera flight");
            await CheckPracticeLayout("zoom-zero");
            await Reload();
            Step(FirstTradeTutorial.Step.CameraZoom);
            Check(tutorial.CameraPanDone && !tutorial.CameraZoomDone, "Partial camera practice must survive reload");
            await CameraSettled();
            Vector2 wheelPoint = new(30, Root.Size.Y * 0.55f);
            MouseButtonAt(wheelPoint, MouseButton.WheelUp, true);
            MouseButtonAt(wheelPoint, MouseButton.WheelUp, false);
            await Frame();
            Check(tutorial.CameraZoomProgress == 6.25f && CameraBar().Value == 6.25 && !tutorial.CameraZoomDone,
                "One accepted wheel step must show 6.25%, requiring sixteen steps instead of four");
            using (await CapturePresentation("tutorial-zoom-partial")) { }
            await Reload();
            Step(FirstTradeTutorial.Step.CameraZoom);
            Check(tutorial.CameraZoomProgress == 6.25f, "Partial zoom percent must survive reload");
            await CameraSettled();
            for (int i = 0; i < 15; i++)
            {
                MouseButton direction = i % 2 == 0 ? MouseButton.WheelDown : MouseButton.WheelUp;
                MouseButtonAt(wheelPoint, direction, true);
                MouseButtonAt(wheelPoint, direction, false);
                await Frame();
                Check(CameraBar().Value == (i + 2) * 6.25, "Each accepted wheel step in either direction must advance zoom by 6.25%");
                if (i < 14) Step(FirstTradeTutorial.Step.CameraZoom);
            }
            Step(FirstTradeTutorial.Step.RoomFocus);
            Check(tutorial.CameraZoomDone && !tutorial.RoomFocusDone, "Wheel practice must lead to the room focus step");
            var focusHint = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            TranslationServer.SetLocale("en");
            Check(focusHint.Tr(focusHint.Text).StartsWith("Double-click a room"), "The actual focus step must explain the gesture in English");
            TranslationServer.SetLocale("ru");
            Check(focusHint.Text.StartsWith("Дважды нажмите ЛКМ по комнате"), "The actual focus step must explain the gesture in Russian");
            var focusCamera = game.GetNode<WorldCamera>("Camera2D");
            focusCamera.ResetToOverview();
            await Frame();
            await CameraSettled();
            var corridor = game.GetNode<Control>("World/Corridor/RoomFrame");
            Rect2 visibleRoom = ScreenBounds(corridor).Intersection(Root.GetVisibleRect());
            Vector2 focusPoint = visibleRoom.Position + visibleRoom.Size * new Vector2(0.15f, 0.75f);
            MoveButtonPointer(focusPoint);
            await Frame();
            MouseButtonAt(focusPoint, MouseButton.Left, true);
            MouseButtonAt(focusPoint, MouseButton.Left, false);
            Check(!tutorial.RoomFocusDone, "A single click must not finish room focus");
            Root.PushInput(new InputEventMouseButton { Position = focusPoint, GlobalPosition = focusPoint,
                ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true, ButtonMask = MouseButtonMask.Left }, true);
            MouseButtonAt(focusPoint, MouseButton.Left, false);
            Check(tutorial.RoomFocusDone && focusCamera.IsFocusing, "A successful real double click must finish room focus");
            while (focusCamera.IsFocusing)
            {
                await Frame();
                Check(!focusCamera.IsReturningToOverview,
                    "Finishing the double-click lesson must not start an automatic overview flight");
            }
            Vector2 chosenPosition = focusCamera.Position, chosenZoom = focusCamera.Zoom;
            await ToSignal(CreateTimer(tutorial.CameraTravelSeconds + 0.2), SceneTreeTimer.SignalName.Timeout);
            await Frame();
            Step(FirstTradeTutorial.Step.Start);
            Check(!focusCamera.NavigationLocked && !focusCamera.IsReturningToOverview &&
                focusCamera.Position.IsEqualApprox(chosenPosition) && focusCamera.Zoom.IsEqualApprox(chosenZoom),
                "The Start prompt must preserve the chosen room and allow player camera control");
            focusCamera.SetProcess(true);
            Vector2 roomWheelPoint = Root.GetVisibleRect().Size / 2;
            MouseButtonAt(roomWheelPoint, MouseButton.WheelDown, true);
            MouseButtonAt(roomWheelPoint, MouseButton.WheelDown, false);
            await Frame();
            Check(focusCamera.Zoom.X < chosenZoom.X, "The player must be able to zoom out manually after room practice");
            await PanCamera();
            focusCamera.SetProcess(false);
            Check(!tutorial.HasNode("Root/Panel/Content/Navigation"), "Later steps must not repeat manual navigation instructions");
            Highlight();
            await CheckYellowHighlight("tutorial-yellow-start");
            Check(!Paused && !game.DayStarted && game.Score == game.StartingMoney && game.StoredItems.Length == 0,
                "Starting the tutorial must not pause gameplay, spend money or generate items");
            int firstBuyerArrivals = 0;
            game.BuyerArrived += () => firstBuyerArrivals++;
            await ClickControl(game, "HUD/DayClock/Root/DayTimer/StartButton");
            game._Process(0);
            Check(firstBuyerArrivals == 0, "Starting the tutorial day must not announce a customer arrival");
            await Frame();
            Step(FirstTradeTutorial.Step.Craft);
            Check(game.DayRemainingSeconds == 180, "The training day must start with three minutes");
            await CheckTutorialEndBlocked();
            Check(game.GetNode<WorldCamera>("Camera2D").IsFocusing, "Craft instructions must start a new camera flight");
            await CheckCameraLocked();
            Highlight();
            using (var image = await CapturePresentation("tutorial-craft")) { }
            await ClickControl(game, "World/Atelier/GetItemButton");
            await Frame();
            Check(game.CraftMiniGameOpen && !tutorial.IsShowing, "Tutorial must hide during the crafting modal");
            var guidedCraft = game.GetNode<EmbroideryGame>("EmbroideryGame");
            guidedCraft.SetProcess(false);
            async Task CheckEmbroideryHint(string instruction)
            {
                foreach (var craftSize in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
                foreach (string language in new[] { "en", "ru" })
                {
                    Root.Size = craftSize;
                    TranslationServer.SetLocale(language);
                    await Frame();
                    guidedCraft._Process(0);
                    await Frame();
                    var hint = guidedCraft.GetNode<Label>("Surface/Hint");
                    var workSurface = guidedCraft.GetNode<EmbroiderySurface>("Surface");
                    Check(hint.IsVisibleInTree() && hint.Text == GameLocalization.Text(instruction) &&
                        workSurface.PanelRect.Encloses(hint.GetRect()) &&
                        hint.GetThemeFont("font").GetStringSize(hint.Text, fontSize: hint.GetThemeFontSize("font_size")).X <= hint.Size.X,
                        "The in-window embroidery guide must show the current action, localized and fitted to the panel");
                    if (language == "en") Check(hint.Text != instruction, "The craft guide needs an actual English translation");
                }
            }
            await CheckEmbroideryHint("Нажмите «Начать», чтобы приступить к вышивке.");
            await ClickControl(game, "EmbroideryGame/Surface/Action");
            Check(guidedCraft.ActiveSession.State == ChikagoShop.Domain.Crafting.EmbroideryState.Playing,
                "The guided Start button must begin embroidery");
            await CheckEmbroideryHint("Нажимайте на подсвечивающиеся точки по очереди.");
            Vector2 stitchPoint = guidedCraft.GetNode<EmbroiderySurface>("Surface").ScreenPoint(0);
            MoveButtonPointer(stitchPoint);
            await Frame();
            MouseButtonAt(stitchPoint, MouseButton.Left, true);
            MouseButtonAt(stitchPoint, MouseButton.Left, false);
            Check(guidedCraft.Session.Stitches == 1, "Clicking the guided highlighted point must make a stitch");
            guidedCraft.FinishForTesting(true);
            await CheckEmbroideryHint("Нажмите «Забрать», чтобы вернуться в магазин.");
            await ClickControl(game, "EmbroideryGame/Surface/Action");
            Check(!guidedCraft.IsOpen && !Paused, "Guided Collect must return to the shop and resume the tutorial");
            guidedCraft.SetProcess(true);
            await Frame();
            Step(FirstTradeTutorial.Step.Select);
            await CheckCameraLocked();
            Highlight();
            int trainingDay = game.CurrentDay, trainingEnergy = game.EnergyRemaining, trainingCash = game.Score;
            int[] trainingStock = game.StoredItems;
            game._Process(game.DayRemainingSeconds - 0.01);
            Check(!game.DayClosed && game.DayRemainingSeconds > 0 && game.DayRemainingSeconds < 0.02,
                "The tutorial must remain open immediately before the practice clock expires");
            game._Process(0.02);
            Check(game.DayRemainingSeconds == 180 && !game.DayClosed && !game.DayTransitionOpen,
                "Expired training time must refill to180 without closing the day or showing upgrade cards");
            game._Process(180);
            game.TickCraftDay(180);
            Step(FirstTradeTutorial.Step.Select);
            Check(game.DayRemainingSeconds == 180 && !game.DayClosed && game.CurrentDay == trainingDay &&
                game.EnergyRemaining == trainingEnergy && game.Score == trainingCash && game.StoredItems.SequenceEqual(trainingStock),
                "Repeated expiry through gameplay/craft ticking must preserve the current guide step and run state");
            game._Process(7.25);
            game.PersistProgress();
            double savedTrainingTime = game.DayRemainingSeconds;
            var legacyTutorial = new ConfigFile();
            Check(legacyTutorial.Load(progress) == Error.Ok, "Tutorial fixture must be saved");
            legacyTutorial.SetValue("tutorial", "camera_handoff_done", false);
            legacyTutorial.Save(progress);
            var oldWaitingSave = System.Text.Json.Nodes.JsonNode.Parse(FileAccess.GetFileAsString(save));
            oldWaitingSave["waiting_buyer_present"] = true;
            oldWaitingSave["buyer_wait_remaining"] = 12;
            using (var oldSave = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                oldSave.StoreString(oldWaitingSave.ToJsonString());
            await Reload();
            Step(FirstTradeTutorial.Step.Select);
            Check(game.DayRemainingSeconds == savedTrainingTime && savedTrainingTime == 172.75 && !game.DayClosed,
                "Reload must retain tutorial time above the ordinary sixty-second duration");
            await CheckTutorialEndBlocked();
            Check(!tutorial.IsCameraCompletionPending,
                "A save from the removed waiting phase must hand off immediately after loading");
            legacyTutorial.EraseSectionKey("tutorial", "camera_handoff_done");
            legacyTutorial.EraseSectionKey("tutorial", "room_focus_done");
            legacyTutorial.EraseSectionKey("tutorial", "training_clock_duration");
            legacyTutorial.Save(progress);
            await Reload();
            Step(FirstTradeTutorial.Step.Select);
            Check(!tutorial.IsCameraCompletionPending, "An older save with completed navigation must not replay the new handoff delay");
            Check(game.DayRemainingSeconds == 180 && !game.DayClosed,
                "A legacy tutorial without the training-clock marker must receive180 seconds once");
            await CameraSettled();
            var slots = game.GetNode<InventoryUI>("HUD/InventoryUI").Container;
            await ClickControl(game, "HUD/InventoryUI/ScrollContainer/HBoxContainer/" + slots.GetChild(0).Name);
            await Frame();
            Step(FirstTradeTutorial.Step.Place);
            await CheckCameraLocked();
            var camera = game.GetNode<WorldCamera>("Camera2D");
            camera.Position += new Vector2(100, 50);
            camera.Zoom *= 1.1f;
            camera.ClampToBounds();
            await Frame();
            Highlight();
            foreach (Vector2I viewport in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = viewport;
                TranslationServer.SetLocale(language);
                await Frame();
                await CameraSettled();
                var panel = tutorial.GetNode<Control>("Root/Panel");
                var body = tutorial.GetNode<Label>("Root/Panel/Content/Body");
                Check(Root.GetVisibleRect().Encloses(panel.GetGlobalRect()) &&
                    body.Tr(body.Text).StartsWith(language == "en" ? "Click" : "Нажмите"),
                    "Tutorial panel and live translated instructions must fit both viewport sizes");
            }
            int requestArrivals = 0;
            game.BuyerArrived += () => requestArrivals++;
            await ClickControl(game, "World/ShowroomLeft/Showcases/Slot1");
            await Frame();
            Step(FirstTradeTutorial.Step.WaitBuyer);
            await CheckCameraLocked();
            game._Process(0);
            await Frame();
            Step(FirstTradeTutorial.Step.BuyerRequest);
            Check(game.GetNode<Node2D>("World/TradingRoom/Customer").Visible &&
                game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought/ThoughtBubble").IsVisibleInTree() &&
                game.BuyerWaitRemaining == TradeScene.BuyerWaitDuration && requestArrivals == 1,
                "The first customer and bubble must appear together with the request lesson at full patience");
            game.PersistProgress();
            await Reload();
            Step(FirstTradeTutorial.Step.BuyerRequest);
            Check(game.GetNode<Node2D>("World/TradingRoom/Customer").Visible &&
                game.BuyerWaitRemaining == TradeScene.BuyerWaitDuration,
                "Reloading the unread request lesson must keep its customer and full patience");
            await CameraSettled();
            const string thought = "World/TradingRoom/BuyerWaitingThought/ThoughtBubble";
            const string continueInstruction = "FirstTradeTutorial/Root/Panel/Content/Actions/Done";
            foreach (var viewport in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            {
                Root.Size = viewport;
                await Frame();
                await CameraSettled();
                Check(tutorial.Target == game.GetNode<Control>(thought), "Buyer request must outline the actual thought bubble");
                Highlight();
                Check(await CheckBuyerRequestLesson(), "Buyer request must explain both categories and require acknowledgment");
                using var screenshot = await CapturePresentation($"tutorial-buyer-request-{viewport.X}");
                Check(screenshot.GetSize() == viewport, "Buyer lesson must be checked at the requested native resolution");
            }
            Root.Size = new Vector2I(1280, 720);
            await Frame();
            await CameraSettled();
            double heldTime = game.BuyerWaitRemaining;
            int rerolls = game.RerollsRemaining;
            game._Process(1);
            game.AcceptTrade();
            game.RerollCustomer();
            Check(game.Accepted == 0 && game.BuyerWaitRemaining == heldTime && game.RerollsRemaining == rerolls,
                "Reading the buyer request must preserve the time, rerolls and first sale");
            int originalBuyer = game.CustomerIndex, originalItem = game.ItemIndex, originalCash = game.Score;
            const string iconPath = thought + "/Stack/Category/ThoughtIcon";
            Check(game.GetNode<TextureRect>(iconPath).Texture.ResourcePath == "res://art/ui/buyer_wish_atelier.png",
                "The first request must show the real clothing icon before the example");
            await ClickControl(game, continueInstruction);
            Step(FirstTradeTutorial.Step.MusicBuyerRequest);
            Check(game.GetNode<TextureRect>(iconPath).Texture.ResourcePath == "res://art/ui/buyer_wish_music.png" &&
                tutorial.Target == game.GetNode<Control>(thought),
                "The music example must replace the visible category icon and highlight its bubble");
            TranslationServer.SetLocale("en");
            await Frame();
            var musicMessage = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            Check(musicMessage.Tr(musicMessage.Text) == "This customer wants an instrument from the music workshop.",
                "The music icon example must have an English explanation");
            TranslationServer.SetLocale("ru");
            await Frame();
            game._Process(1); game.AcceptTrade(); game.RerollCustomer();
            Check(game.CustomerIndex == originalBuyer && game.ItemIndex == originalItem && game.Score == originalCash &&
                game.BuyerWaitRemaining == heldTime && game.RerollsRemaining == rerolls && !game.MusicWorkshopUnlocked,
                "The visual example must freeze actions/time without replacing the buyer/item or unlocking music");
            game.PersistProgress();
            await Reload();
            Step(FirstTradeTutorial.Step.MusicBuyerRequest);
            Check(game.GetNode<TextureRect>(iconPath).Texture.ResourcePath == "res://art/ui/buyer_wish_music.png",
                "Reload must preserve the musical request demonstration");
            await ClickControl(game, continueInstruction);
            Step(FirstTradeTutorial.Step.Offer);
            Check(game.GetNode<TextureRect>(iconPath).Texture.ResourcePath == "res://art/ui/buyer_wish_atelier.png" &&
                game.CustomerIndex == originalBuyer && game.ItemIndex == originalItem && game.Score == originalCash,
                "Continuing must restore the actual clothing request and the unchanged first deal");
            await Reload();
            Step(FirstTradeTutorial.Step.Offer);
            await CheckTutorialEndBlocked();
            await CheckCameraLocked();
            Highlight();
            var buyerHint = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            Check(buyerHint.Text.Contains("✓") && buyerHint.Text.Contains("×") && !buyerHint.Text.Contains("Скорость"),
                "Offer instructions must identify buttons by their visible symbols, without relying on color");
            const string tradeButtons = "World/TradingRoom/Item/TradePanel/Content/Buttons/";
            var accept = game.GetNode<Button>(tradeButtons + "AcceptButton");
            var reject = game.GetNode<Button>(tradeButtons + "DeclineButton");
            Check(accept.Disabled && reject.Disabled && tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done").Visible,
                "Offer explanation must provide Next while both transaction buttons are disabled");
            double explanationBuyerTime = game.BuyerWaitRemaining, explanationDayTime = game.DayRemainingSeconds;
            int explanationCustomer = game.CustomerIndex, explanationRerolls = game.RerollsRemaining,
                explanationCash = game.Score;
            game._Process(2);
            game.AcceptTrade(); game.RerollCustomer();
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = false }, true);
            await ClickControl(game, tradeButtons + "AcceptButton");
            await ClickControl(game, tradeButtons + "DeclineButton");
            Check(game.TradeActive && game.Accepted == 0 && game.CustomerIndex == explanationCustomer &&
                game.RerollsRemaining == explanationRerolls && game.Score == explanationCash &&
                game.BuyerWaitRemaining == explanationBuyerTime && game.DayRemainingSeconds == explanationDayTime,
                "Offer reading must freeze both clocks and block mouse, F and direct trading calls");
            await ClickControl(game, continueInstruction);
            Step(FirstTradeTutorial.Step.Offer);
            Check(!tutorial.GetNode<Control>("Root/Panel").Visible &&
                tutorial.GetNode<Control>("Root/Highlight").Visible && !accept.Disabled && !reject.Disabled,
                "Next must hide only the hint window, keep the yellow trade outline and enable trading");
            game._Process(0.5);
            Check(System.Math.Abs(game.BuyerWaitRemaining - (explanationBuyerTime - 0.5)) < 0.001 &&
                System.Math.Abs(game.DayRemainingSeconds - (explanationDayTime - 0.5)) < 0.001,
                "Buyer and day clocks must resume after acknowledging the offer explanation");
            game.PersistProgress(); await Reload();
            Step(FirstTradeTutorial.Step.Offer);
            Check(!tutorial.GetNode<Control>("Root/Panel").Visible &&
                !game.GetNode<Button>(tradeButtons + "AcceptButton").Disabled,
                "Reload must retain acknowledged trading practice without bringing back the hint");
            int offerChangesBeforePractice = game.RerollsRemaining;
            await ClickControl(game, tradeButtons + "DeclineButton");
            Check(game.TradeActive && game.RerollsRemaining == offerChangesBeforePractice - 1 &&
                !tutorial.GetNode<Control>("Root/Panel").Visible,
                "A real rejection during practice must change the offer without restoring the explanation");
            Highlight();
            int cash = game.Score, offer = game.OfferedPrice + TradeScene.SpeedBonus(game.BuyerWaitRemaining) +
                TradeScene.QualityBonus(game.ActiveTradeQualityRank);
            await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Summary);
            await CheckCameraLocked();
            Check(game.Score == cash + offer && game.Accepted == 1, "The guided sale must apply exactly the normal reward once");
            CheckEmptyTutorialCounter();
            var pack = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            var toggle = game.GetNode<Control>("HUD/Root/ProgressToggleButton");
            Root.PushInput(new InputEventMouseMotion { Position = new Vector2(800, 400), GlobalPosition = new Vector2(800, 400) }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(!pack.Expanded && tutorial.Target == toggle,
                "Summary must highlight the pack without automatically opening it");
            Check(!tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done").Visible &&
                !tutorial.GetNode<Control>("Root/Panel/Content/Actions").Visible &&
                tutorial.GetNode<Label>("Root/Panel/Content/Title").Text == "Дни и квоты" &&
                !tutorial.GetNode<Label>("Root/Panel/Content/Body").Text.Contains("улучшение"),
                "Days and quotas must wait for pack hover without Next or the card-selection sentence");
            Highlight();
            await CheckYellowHighlight("tutorial-yellow-pack");
            Vector2 packPoint = ScreenBounds(toggle).GetCenter();
            Root.PushInput(new InputEventMouseMotion { Position = packPoint, GlobalPosition = packPoint }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(pack.Expanded, "Only the player's real hover must reveal the tutorial pack information");
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Summary &&
                !game.GetNode<DayTransitionCard>("DayTransitionCard").IsOpen,
                "Hovering for less than one second must not show the introduction or open cards");
            MoveButtonPointer(new Vector2(800, 400));
            await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
            MoveButtonPointer(packPoint);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Summary,
                "Leaving the pack must reset the one-second hover delay");
            await ToSignal(CreateTimer(0.55), SceneTreeTimer.SignalName.Timeout);
            Step(FirstTradeTutorial.Step.UpgradeIntroduction);
            Check(!game.GetNode<DayTransitionCard>("DayTransitionCard").IsOpen,
                "After the hover delay only the ordinary tutorial introduction should appear");
            foreach (var introSize in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = introSize;
                TranslationServer.SetLocale(language);
                await Frame();
                var introPanel = tutorial.GetNode<Control>("Root/Panel");
                var introBody = tutorial.GetNode<Label>("Root/Panel/Content/Body");
                var openUpgrades = tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done");
                Check(introPanel.IsVisibleInTree() && Root.GetVisibleRect().Encloses(introPanel.GetGlobalRect()) &&
                    introPanel.GetGlobalRect().Encloses(openUpgrades.GetGlobalRect()) &&
                    openUpgrades.Tr(openUpgrades.Text) == (language == "en" ? "Open upgrade menu" : "Открыть окно улучшений") &&
                    introBody.Tr(introBody.Text).Contains(language == "en" ? "You can also buy" : "Также между днями"),
                    "The ordinary introduction must localize and fit with its explicit open-menu button");
            }
            await Reload();
            Step(FirstTradeTutorial.Step.UpgradeIntroduction);
            Check(!game.GetNode<DayTransitionCard>("DayTransitionCard").IsOpen,
                "Reload must retain the introduction and still wait for the explicit button");
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Done");
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.UpgradeChoice && !tutorial.IsShowing &&
                game.GetNode<DayTransitionCard>("DayTransitionCard").IsOpen,
                "Only the open-menu button must launch the cards and hide the introduction");
            await WaitForDayCardAnimation(game);
            var trainingCards = game.GetNode<DayTransitionCard>("DayTransitionCard");
            int upgradeLessonDay = game.CurrentDay, upgradeLessonCash = game.Score, upgradeLessonEnergy = game.EnergyRemaining;
            double upgradeLessonTime = game.DayRemainingSeconds;
            foreach (var upgradeSize in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = upgradeSize;
                TranslationServer.SetLocale(language);
                await Frame();
                await WaitForDayCardAnimation(game);
                var explanation = trainingCards.GetNode<Label>("Root/Center/Card/Content/TutorialExplanation");
                Check(explanation.IsVisibleInTree() && Root.GetVisibleRect().Encloses(explanation.GetGlobalRect()) &&
                    explanation.Text == GameLocalization.Text("Выберите карту или нажмите «Пропустить»."),
                    "The card menu must contain only the short choose-or-skip instruction in both languages");
                foreach (var upgrade in trainingCards.Options)
                    Check(Root.GetVisibleRect().Encloses(trainingCards.GetNode<Control>(
                        "Root/Center/Card/Content/Choices/" + upgrade).GetGlobalRect()),
                        "Tutorial upgrade cards must remain inside the viewport with the extra explanation");
            }
            game.PersistProgress();
            await Reload();
            await WaitForDayCardAnimation(game);
            trainingCards = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Check(trainingCards.IsOpen && tutorial.CurrentStep == FirstTradeTutorial.Step.UpgradeChoice,
                "Reloading a pending tutorial choice must reopen its card menu");
            var learnedUpgrade = trainingCards.Options.First(option => game.DayUpgrades.CostFor(option) <= game.Score);
            int learnedPrice = game.DayUpgrades.CostFor(learnedUpgrade);
            await ClickControl(game, "DayTransitionCard/Root/Center/Card/Content/Choices/" + learnedUpgrade + "/Content/Choose");
            await WaitForDayCardAnimation(game);
            await Frame();
            int learnedBalance = upgradeLessonCash - learnedPrice +
                (learnedUpgrade == ChikagoShop.Domain.Days.DayUpgrade.Cash ? game.DayUpgrades.CashReward : 0);
            Check(!trainingCards.IsOpen && !Paused && game.Score == learnedBalance &&
                game.DayUpgrades.Selected == learnedUpgrade && game.CurrentDay == upgradeLessonDay && !game.DayClosed &&
                game.DayRemainingSeconds == upgradeLessonTime &&
                game.EnergyRemaining == upgradeLessonEnergy + (learnedUpgrade == ChikagoShop.Domain.Days.DayUpgrade.Energy ? 1 : 0),
                "A tutorial purchase must apply the real cost/reward once without advancing the day or spending lesson time");
            trainingCards.Choose(learnedUpgrade);
            Check(game.Score == learnedBalance, "Repeated input after a tutorial purchase must not charge again");
            async Task CompleteNotebookLesson(bool reload)
            {
                Step(FirstTradeTutorial.Step.Notebook);
                MoveButtonPointer(new Vector2(20, 300));
                await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
                Check(!game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook").Expanded,
                    "The notebook lesson must not automatically open the notebook");
                foreach (var notebookSize in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
                foreach (string language in new[] { "en", "ru" })
                {
                    Root.Size = notebookSize;
                    TranslationServer.SetLocale(language);
                    await Frame();
                    Check(tutorial.Target == game.GetNode<Control>("HUD/Root/PulloutNotebook/Page") &&
                        !tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done").Visible &&
                        tutorial.GetNode<Label>("Root/Panel/Content/Body").Tr(
                            tutorial.GetNode<Label>("Root/Panel/Content/Body").Text) ==
                            (language == "en" ? "Hover over the notebook to see your current tasks and upgrades." :
                                "Наведитесь на блокнот, чтобы увидеть текущие задания и улучшения."),
                        "Notebook instruction must localize, highlight the real page and wait for hover without Next");
                    Highlight();
                }
                if (reload)
                {
                    game.PersistProgress();
                    await Reload();
                    Step(FirstTradeTutorial.Step.Notebook);
                }
                var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
                Rect2 tab = notebook.GetGlobalTransformWithCanvas() * notebook.HoverBounds;
                Vector2 point = new(tab.Position.X + 17, tab.GetCenter().Y);
                MoveButtonPointer(point);
                await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
                Check(notebook.Expanded && tutorial.CurrentStep == FirstTradeTutorial.Step.Notebook,
                    "Actual hover must open the notebook and leave a full second to read it");
                MoveButtonPointer(new Vector2(20, 300));
                await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
                MoveButtonPointer(point);
                await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
                Step(FirstTradeTutorial.Step.Notebook);
                await ToSignal(CreateTimer(0.55), SceneTreeTimer.SignalName.Timeout);
                Step(FirstTradeTutorial.Step.Summary);
                Check(tutorial.GetNode<Label>("Root/Panel/Content/Title").Text == "Музыкальная мастерская",
                    "A full uninterrupted notebook hover must continue the existing guide");
                CheckEmptyTutorialCounter();
                MoveButtonPointer(new Vector2(20, 300));
            }
            await CompleteNotebookLesson(true);
            // Exercise a pending lesson with no money: skipping must always let the guide continue.
            var skipLesson = new ConfigFile();
            skipLesson.Load(progress);
            skipLesson.SetValue("tutorial", "summary_page", 0);
            skipLesson.SetValue("tutorial", "upgrade_lesson_pending", true);
            skipLesson.Save(progress);
            game.Score = 0; game.PersistProgress();
            await Reload();
            await WaitForDayCardAnimation(game);
            trainingCards = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Check(trainingCards.IsOpen && !trainingCards.GetNode<Button>("Root/Center/Card/Content/Skip").Disabled,
                "A player without purchase funds must still be able to skip the tutorial choice");
            await ClickControl(game, "DayTransitionCard/Root/Center/Card/Content/Skip");
            await WaitForDayCardAnimation(game);
            await Frame();
            Step(FirstTradeTutorial.Step.Notebook);
            Check(!trainingCards.IsOpen && !Paused && game.Score == 0 && game.CurrentDay == upgradeLessonDay &&
                !game.DayUpgrades.Pending && game.DayUpgrades.Selected == ChikagoShop.Domain.Days.DayUpgrade.None,
                "Skipping must be free, close the menu and continue the same tutorial day");
            await CompleteNotebookLesson(false);
            Check(tutorial.GetNode<Label>("Root/Panel/Content/Title").Text == "Музыкальная мастерская" &&
                tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done").Visible && tutorial.Target == null,
                "Completing the card choice must continue to the workshop lesson and restore Next there");
            pack = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            toggle = game.GetNode<Control>("HUD/Root/ProgressToggleButton");
            Root.PushInput(new InputEventMouseMotion { Position = new Vector2(800, 400), GlobalPosition = new Vector2(800, 400) }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(!pack.Expanded, "Leaving the pack during Summary must retract it normally");
            await Reload();
            Step(FirstTradeTutorial.Step.Summary);
            await CheckTutorialEndBlocked();
            Check(!game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel").Expanded,
                "Reloading Summary must not open the pack without player input");
            const string summaryButton = "FirstTradeTutorial/Root/Panel/Content/Actions/Done";
            var oldSummarySave = System.Text.Json.Nodes.JsonNode.Parse(FileAccess.GetFileAsString(save));
            oldSummarySave["waiting_buyer_present"] = true;
            using (var oldSummary = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                oldSummary.StoreString(oldSummarySave.ToJsonString());
            await Reload();
            Step(FirstTradeTutorial.Step.Summary);
            CheckEmptyTutorialCounter();
            using (var normalizedSummary = System.Text.Json.JsonDocument.Parse(FileAccess.GetFileAsString(save)))
                Check(!normalizedSummary.RootElement.GetProperty("waiting_buyer_present").GetBoolean(),
                    "Reloading the post-sale guide must clear a saved next customer instead of restoring their wait");
            var summaryTitle = tutorial.GetNode<Label>("Root/Panel/Content/Title");
            var summaryBody = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            TranslationServer.SetLocale("en");
            await Frame();
            Check(summaryTitle.Tr(summaryTitle.Text) == "Music workshop" && summaryBody.Tr(summaryBody.Text).Contains("hold the left mouse button"),
                "Reload must resume the localized workshop page and explain polishing");
            await ClickControl(game, summaryButton);
            Check(summaryTitle.Tr(summaryTitle.Text) == "Pause and saving" && summaryBody.Tr(summaryBody.Text).Contains("return to a checkpoint"),
                "The last page must cover pause and persistence");
            TranslationServer.SetLocale("ru");
            await Frame();
            await ClickControl(game, summaryButton);
            Check(!tutorial.IsActive && !tutorial.IsShowing, "Acknowledging the summary must complete onboarding");
            CheckFreshRun();
            Check(game.GetNode<WorldCamera>("Camera2D").IsReturningToOverview, "Completing onboarding must fly back to the initial overview");
            await CameraSettled();
            Vector2 returnedPosition = game.GetNode<WorldCamera>("Camera2D").Position;
            Vector2 returnedZoom = game.GetNode<WorldCamera>("Camera2D").Zoom;
            game.GetNode<WorldCamera>("Camera2D").ResetToOverview();
            Check(returnedPosition.DistanceTo(game.GetNode<WorldCamera>("Camera2D").Position) < 0.05f &&
                returnedZoom.IsEqualApprox(game.GetNode<WorldCamera>("Camera2D").Zoom),
                "The completed tutorial must finish at the same view as a new game");
            await Reload();
            Check(!tutorial.IsActive, "Completed onboarding must not replay when loading the game");
            CheckFreshRun();
            game.EndDay();
            await ClickControl(game, "HUD/DayClock/Root/ClockActionButton");
            Check(game.DayClosed && !game.DayTransitionOpen, "Completing the tutorial must restore normal manual day closure");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            await Frame();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/NewGameButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Camera);
            Check(!Paused && !tutorial.CameraPanDone && !tutorial.CameraZoomDone,
                "The real pause-menu new-run button must offer fresh practice after a completed tutorial");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            await Frame();
            Check(!tutorial.IsShowing && Paused, "Pause must hide tutorial hints");
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/TutorialButton");
            await Frame();
            Check(tutorial.IsActive && !Paused, "The scene-owned pause button must allow replay without resetting the run");
            Step(FirstTradeTutorial.Step.Camera);
            game.GetNode<PauseMenu>("PauseMenu").Open();
            await Frame();
            Check(!tutorial.IsShowing && Paused, "An active tutorial must hide its panel and highlight while paused");
            Vector2 pausedPosition = game.GetNode<WorldCamera>("Camera2D").Position;
            await ToSignal(CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
            Check(!game.GetNode<WorldCamera>("Camera2D").IsFocusing &&
                game.GetNode<WorldCamera>("Camera2D").Position == pausedPosition,
                "Pausing onboarding must cancel its flight and hold the current camera position");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            await Frame();
            Check(tutorial.IsShowing && !Paused, "Closing pause must restore the current instruction without advancing it");
            game.NewGame();
            await Frame();
            Step(FirstTradeTutorial.Step.Camera);
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            Check(tutorial.IsActive && tutorial.IsSkipConfirmationOpen && Paused,
                "The first skip click must open confirmation and pause the world without finishing onboarding");
            var pending = new ConfigFile();
            Check(pending.Load(progress) == Error.Ok && !pending.GetValue("tutorial", "finished").AsBool(),
                "Merely opening confirmation must not persist a skipped tutorial");
            await ClickControl(game, "HUD/DayClock/Root/DayTimer/StartButton");
            Check(!game.DayStarted && tutorial.IsSkipConfirmationOpen,
                "The confirmation overlay must prevent clicks from starting the day behind it");
            foreach (Vector2I viewport in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = viewport;
                TranslationServer.SetLocale(language);
                await Frame();
                var panel = tutorial.GetNode<Control>("Root/SkipConfirmation/Center/Panel");
                var title = panel.GetNode<Label>("Content/Title");
                Check(Root.GetVisibleRect().Encloses(panel.GetGlobalRect()) &&
                    title.Tr(title.Text) == (language == "en" ? "Skip the tutorial?" : "Пропустить обучение?"),
                    "Confirmation must fit both viewports and translate while open");
            }
            using (var image = await CapturePresentation("tutorial-skip-confirmation")) { }
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel");
            await Frame();
            Step(FirstTradeTutorial.Step.Camera);
            Check(!tutorial.IsSkipConfirmationOpen && !Paused,
                "Cancel must resume the same tutorial step and release only the confirmation's pause");
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Escape }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Escape }, true);
            await Frame();
            Check(!tutorial.IsSkipConfirmationOpen && !Paused &&
                !game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
                "Escape must cancel skip without opening the pause menu or ending onboarding");
            await ClickControl(game, "HUD/DayClock/Root/DayTimer/StartButton");
            game.GetItemFromAtelier();
            FinishEmbroidery(game);
            await Frame();
            Check(game.DayStarted && game.Score < game.StartingMoney && game.StoredItems.Length > 0 &&
                game.EnergyRemaining < game.EnergyCapacity, "Skip must be tested after real paid practice has changed the run");
            int scoreBeforeCancel = game.Score;
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel");
            Check(game.Score == scoreBeforeCancel && game.DayStarted && game.StoredItems.Length > 0,
                "Cancelling skip must preserve the current practice day and its items");
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Confirm");
            Check(!tutorial.IsActive && !tutorial.IsSkipConfirmationOpen && !Paused,
                "Only explicit confirmation must finish the tutorial and unpause gameplay");
            CheckFreshRun();
            Check(game.GetNode<WorldCamera>("Camera2D").IsReturningToOverview, "Skipping must also return to the starting overview");
            await CameraSettled();
            await Reload();
            Check(!tutorial.IsActive && !tutorial.IsShowing, "Skipping must persist just like completing onboarding");
            CheckFreshRun();
            game.EndDay();
            await ClickControl(game, "HUD/DayClock/Root/ClockActionButton");
            Check(game.DayClosed && !game.DayTransitionOpen, "Skipping the tutorial must restore normal manual day closure");
            game.NewGame();
            await Frame();
            Step(FirstTradeTutorial.Step.Camera);
            Check(!tutorial.CameraPanDone && !tutorial.CameraZoomDone,
                "A new run must reset a previous skip and both camera-practice flags");
            Check(tutorial.CameraPanProgress == 0 && tutorial.CameraZoomProgress == 0 && CameraBar().Value == 0,
                "A new run must clear both fractional camera progress values and the displayed percentage");
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            game.NewGame();
            await Frame();
            Check(tutorial.IsActive && !tutorial.IsSkipConfirmationOpen && !Paused,
                "Restarting a run while confirmation is open must release its pause and reset onboarding");
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            await Reload();
            Check(tutorial.IsActive && !tutorial.IsSkipConfirmationOpen && !Paused,
                "Exiting with confirmation open must not leak its pause or persist an unconfirmed skip");
            var stale = new ConfigFile();
            stale.SetValue("tutorial", "finished", true);
            stale.SetValue("tutorial", "camera_pan_done", true);
            stale.SetValue("tutorial", "camera_zoom_done", true);
            stale.Save(progress);
            DirAccess.RemoveAbsolute(save);
            await Reload();
            Step(FirstTradeTutorial.Step.Camera);
            Check(!game.LoadedSavedGame && !tutorial.CameraPanDone && !tutorial.CameraZoomDone,
                "Fresh entry after the menu deletes the game save must ignore a stale completed tutorial sidecar");
        }
        finally
        {
            Paused = false;
            game?.QueueFree();
            Root.Size = size;
            TranslationServer.SetLocale(locale);
            foreach (string path in new[] { save, progress }) if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
        }
        await Frame();
        System.GC.KeepAlive(retainedFullscreenScript);
    }
}
