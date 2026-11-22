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
        void Step(FirstTradeTutorial.Step expected) => Check(tutorial.IsActive && tutorial.IsShowing && tutorial.CurrentStep == expected,
            $"Tutorial must follow actual gameplay state: expected {expected}, got {tutorial.CurrentStep}");
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
            Check(caption.Visible && caption.Text == GameLocalization.Text("Завершите обучение").Replace(" ", "\n") &&
                game.GetNode<Button>("HUD/DayClock/Root/ClockActionButton").TooltipText ==
                    GameLocalization.Text("Сначала завершите или пропустите обучение."),
                "Blocked end-day attempts must show a localized explanation on the clock");
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
            await CameraSettled();
            await Frame();
            Step(FirstTradeTutorial.Step.Start);
            await CheckCameraLocked();
            Check(!tutorial.HasNode("Root/Panel/Content/Navigation"), "Later steps must not repeat manual navigation instructions");
            Highlight();
            await CheckYellowHighlight("tutorial-yellow-start");
            Check(!Paused && !game.DayStarted && game.Score == game.StartingMoney && game.StoredItems.Length == 0,
                "Starting the tutorial must not pause gameplay, spend money or generate items");
            await ClickControl(game, "HUD/DayClock/Root/DayTimer/StartButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Craft);
            await CheckTutorialEndBlocked();
            Check(game.GetNode<WorldCamera>("Camera2D").IsFocusing, "Craft instructions must start a new camera flight");
            await CheckCameraLocked();
            Highlight();
            using (var image = await CapturePresentation("tutorial-craft")) { }
            await ClickControl(game, "World/Atelier/GetItemButton");
            await Frame();
            Check(game.CraftMiniGameOpen && !tutorial.IsShowing, "Tutorial must hide during the crafting modal");
            FinishEmbroidery(game);
            await Frame();
            Step(FirstTradeTutorial.Step.Select);
            await CheckCameraLocked();
            Highlight();
            var legacyTutorial = new ConfigFile();
            Check(legacyTutorial.Load(progress) == Error.Ok, "Tutorial fixture must be saved");
            legacyTutorial.SetValue("tutorial", "camera_handoff_done", false);
            legacyTutorial.Save(progress);
            await Reload();
            Step(FirstTradeTutorial.Step.Select);
            await CheckTutorialEndBlocked();
            Check(!tutorial.IsCameraCompletionPending,
                "A save from the removed waiting phase must hand off immediately after loading");
            legacyTutorial.EraseSectionKey("tutorial", "camera_handoff_done");
            legacyTutorial.EraseSectionKey("tutorial", "room_focus_done");
            legacyTutorial.Save(progress);
            await Reload();
            Step(FirstTradeTutorial.Step.Select);
            Check(!tutorial.IsCameraCompletionPending, "An older save with completed navigation must not replay the new handoff delay");
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
            await ClickControl(game, "World/ShowroomLeft/Showcases/Slot1");
            await Frame();
            Step(FirstTradeTutorial.Step.WaitBuyer);
            await CheckCameraLocked();
            game._Process(0);
            await Frame();
            Step(FirstTradeTutorial.Step.Offer);
            await CheckTutorialEndBlocked();
            await CheckCameraLocked();
            Highlight();
            var buyerHint = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            Check(buyerHint.Text.Contains("✓") && buyerHint.Text.Contains("×"),
                "Offer instructions must identify buttons by their visible symbols, without relying on color");
            int cash = game.Score, offer = game.OfferedPrice + TradeScene.SpeedBonus(game.BuyerWaitRemaining) +
                TradeScene.QualityBonus(game.ActiveTradeQualityRank);
            await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Summary);
            await CheckCameraLocked();
            Check(game.Score == cash + offer && game.Accepted == 1, "The guided sale must apply exactly the normal reward once");
            var pack = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            var toggle = game.GetNode<Control>("HUD/Root/ProgressToggleButton");
            Root.PushInput(new InputEventMouseMotion { Position = new Vector2(800, 400), GlobalPosition = new Vector2(800, 400) }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(!pack.Expanded && tutorial.Target == toggle,
                "Summary must highlight the pack without automatically opening it");
            Highlight();
            await CheckYellowHighlight("tutorial-yellow-pack");
            Vector2 packPoint = ScreenBounds(toggle).GetCenter();
            Root.PushInput(new InputEventMouseMotion { Position = packPoint, GlobalPosition = packPoint }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(pack.Expanded, "Only the player's real hover must reveal the tutorial pack information");
            Root.PushInput(new InputEventMouseMotion { Position = new Vector2(800, 400), GlobalPosition = new Vector2(800, 400) }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(!pack.Expanded, "Leaving the pack during Summary must retract it normally");
            await Reload();
            Step(FirstTradeTutorial.Step.Summary);
            await CheckTutorialEndBlocked();
            Check(!game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel").Expanded,
                "Reloading Summary must not open the pack without player input");
            const string summaryButton = "FirstTradeTutorial/Root/Panel/Content/Actions/Done";
            await ClickControl(game, summaryButton);
            await Reload();
            Step(FirstTradeTutorial.Step.Summary);
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
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Skip");
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
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Skip");
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
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Skip");
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel");
            Check(game.Score == scoreBeforeCancel && game.DayStarted && game.StoredItems.Length > 0,
                "Cancelling skip must preserve the current practice day and its items");
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Skip");
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
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Skip");
            game.NewGame();
            await Frame();
            Check(tutorial.IsActive && !tutorial.IsSkipConfirmationOpen && !Paused,
                "Restarting a run while confirmation is open must release its pause and reset onboarding");
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Skip");
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
    }
}
