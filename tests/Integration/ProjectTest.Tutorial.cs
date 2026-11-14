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
        async Task PanCamera()
        {
            Vector2 point = new(30, Root.Size.Y * 0.55f);
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            await Frame();
            MouseButtonAt(point, MouseButton.Left, true);
            for (int i = 0; i < 4; i++)
            {
                point += new Vector2(25, 0);
                Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                    Relative = new Vector2(25, 0), ButtonMask = MouseButtonMask.Left }, true);
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
        void Highlight()
        {
            var outline = tutorial.GetNode<Control>("Root/Highlight");
            Rect2 target = tutorial.Target.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, tutorial.Target.Size);
            Check(outline.Visible && Root.GetVisibleRect().Encloses(outline.GetGlobalRect()) &&
                outline.GetGlobalRect().Intersects(target), "The highlight must follow the actual screen-space target without capturing clicks");
            Check(outline.MouseFilter == Control.MouseFilterEnum.Ignore && tutorial.GetNode<Control>("Root").MouseFilter == Control.MouseFilterEnum.Ignore,
                "Tutorial highlights and full-screen root must leave gameplay input available");
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
            using (var image = await CapturePresentation("tutorial-camera")) { }
            var panelRect = tutorial.GetNode<Control>("Root/Panel").GetGlobalRect();
            Vector2 uiPoint = panelRect.Position + new Vector2(20, 20);
            MouseButtonAt(uiPoint, MouseButton.Left, true);
            Root.PushInput(new InputEventMouseMotion { Position = uiPoint + new Vector2(90, 0),
                GlobalPosition = uiPoint + new Vector2(90, 0), Relative = new Vector2(90, 0), ButtonMask = MouseButtonMask.Left }, true);
            MouseButtonAt(uiPoint + new Vector2(90, 0), MouseButton.Left, false);
            await Frame();
            Check(!tutorial.CameraPanDone && !tutorial.CameraZoomDone, "Dragging a tutorial panel must not count as world navigation");
            await PanCamera();
            Check(tutorial.CameraPanDone && !tutorial.CameraZoomDone, "Real left-button background drag must complete only movement");
            await Reload();
            Step(FirstTradeTutorial.Step.Camera);
            Check(tutorial.CameraPanDone && !tutorial.CameraZoomDone, "Partial camera practice must survive reload");
            await CameraSettled();
            Vector2 wheelPoint = new(30, Root.Size.Y * 0.55f);
            MouseButtonAt(wheelPoint, MouseButton.WheelUp, true);
            MouseButtonAt(wheelPoint, MouseButton.WheelUp, false);
            await Frame();
            Check(tutorial.CameraZoomDone, "A real wheel input must complete zoom practice");
            Step(FirstTradeTutorial.Step.Start);
            await CheckCameraLocked();
            Check(!tutorial.HasNode("Root/Panel/Content/Navigation"), "Later steps must not repeat manual navigation instructions");
            Highlight();
            Check(!Paused && !game.DayStarted && game.Score == game.StartingMoney && game.StoredItems.Length == 0,
                "Starting the tutorial must not pause gameplay, spend money or generate items");
            await ClickControl(game, "HUD/Root/DayTimer/StartButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Craft);
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
            await Reload();
            Step(FirstTradeTutorial.Step.Select);
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
            game._Process(game.BuyerCountdown + 0.01);
            await Frame();
            Step(FirstTradeTutorial.Step.Offer);
            await CheckCameraLocked();
            Highlight();
            var buyerHint = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            Check(buyerHint.Text.Contains("✓") && buyerHint.Text.Contains("×"),
                "Offer instructions must identify buttons by their visible symbols, without relying on color");
            int cash = game.Score, offer = game.OfferedPrice;
            await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Summary);
            await CheckCameraLocked();
            Check(game.Score == cash + offer && game.Accepted == 1, "The guided sale must apply exactly the normal reward once");
            await Reload();
            Step(FirstTradeTutorial.Step.Summary);
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Done");
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
            await ClickControl(game, "HUD/Root/DayTimer/StartButton");
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
            await ClickControl(game, "HUD/Root/DayTimer/StartButton");
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
            game.NewGame();
            await Frame();
            Step(FirstTradeTutorial.Step.Camera);
            Check(!tutorial.CameraPanDone && !tutorial.CameraZoomDone,
                "A new run must reset a previous skip and both camera-practice flags");
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
