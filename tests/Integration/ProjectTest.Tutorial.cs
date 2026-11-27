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
            bool english = body.Tr(body.Text).Contains("clothing from the atelier");
            TranslationServer.SetLocale("ru");
            await Frame();
            return english && body.Text.Contains("ищет одежду из ателье") &&
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
        const string skipPath = "Root/Skip";
        void CheckSkipPlacement()
        {
            var skip = tutorial.GetNode<Button>(skipPath);
            string language = GameLocalization.Language == "en" ? "en" : "ru";
            Rect2 screen = Root.GetVisibleRect(), bounds = skip.GetGlobalRect();
            Check(skip.GetThemeStylebox("normal") is StyleBoxTexture { Texture.ResourcePath: "res://art/ui/panel_small.png" } &&
                skip.Tr(skip.Text) == (language == "en" ? "Skip tutorial" : "Пропустить обучение") &&
                screen.Encloses(bounds) && Mathf.IsEqualApprox(screen.End.X - bounds.End.X, 20) &&
                Mathf.IsEqualApprox(screen.End.Y - bounds.End.Y, 20),
                "The handwritten tutorial skip button must stay at the bottom right in both languages");
        }
        bool HasHalo(Control control)
        {
            var material = (ShaderMaterial)tutorial.GetNode<TutorialSpotlight>("Spotlight").Material;
            Vector2 expected = ScreenBounds(control).GetCenter();
            int count = material.GetShaderParameter("accessory_count").AsInt32();
            return material.GetShaderParameter("accessories").AsVector4Array().Take(count)
                .Any(area => new Vector2(area.X, area.Y).DistanceTo(expected) < 8);
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
            var spotlight = tutorial.GetNode<TutorialSpotlight>("Spotlight");
            Check(tutorial.Target != null && spotlight.MouseFilter == Control.MouseFilterEnum.Ignore &&
                !tutorial.HasNode("Root/Highlight"), "Soft focus must follow the current target and leave input available");
        }
        async Task CheckGlow(string name)
        {
            await ToSignal(CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);
            using var image = await CapturePresentation(name);
            var glow = tutorial.GetNode<TutorialSpotlight>("Spotlight");
            Check(glow.Visible && ((ShaderMaterial)glow.Material).GetShaderParameter("darkness").AsSingle() > 0,
                "The rendered tutorial must use the feathered spotlight on this step");
        }
        try
        {
            foreach (string path in new[] { save, progress }) if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
            Root.Size = new Vector2I(1280, 720);
            await Create();
            Step(FirstTradeTutorial.Step.Camera);
            var initialCamera = game.GetNode<WorldCamera>("Camera2D");
            Check(tutorial.GetNode("CameraCheckpoints").GetChildCount() == 9 && initialCamera.IsFocusing,
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
            var startCaption = game.GetNode<Button>("HUD/DayClock/Root/DayTimer/StartButton");
            foreach (string language in new[] { "en", "ru" })
            {
                TranslationServer.SetLocale(language);
                await Frame();
                Check(startCaption.Tr(startCaption.Text) == (language == "en" ? "Start" : "Начать"),
                    "The start clock caption must be readable in both locales");
            }
            await CheckGlow("tutorial-glow-start");
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
            await CameraSettled();
            var rarity = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
            MoveButtonPointer(ScreenBounds(game.GetNode<Control>("World/Atelier/GetItemButton/RarityHotspot")).GetCenter());
            await Frame();
            Check(rarity.Popup.IsVisibleInTree() && rarity.Popup.GetParent<CanvasLayer>().Layer > tutorial.Layer &&
                HasHalo(rarity.Popup),
                "The rarity popup must glow and render above the dimmer while hovering the question mark");
            MoveButtonPointer(new Vector2(6, 350));
            await Frame();
            Check(!rarity.Popup.IsVisibleInTree(), "The rarity popup must still close on pointer exit");
            await ClickControl(game, "World/Atelier/GetItemButton");
            await Frame();
            Step(FirstTradeTutorial.Step.CraftSelection);
            Check(game.Crafting.IsCatalogOpen && tutorial.Target == game.Crafting.Catalog.ItemButton("hat") &&
                tutorial.Layer > game.Crafting.Catalog.Layer, "Tutorial must highlight the hat in the actual catalog");
            await ClickControl(game, game.GetPathTo(game.Crafting.Catalog.ItemButton("hat")).ToString());
            await Frame();
            Step(FirstTradeTutorial.Step.EmbroideryStart);
            var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
            var spotlight = tutorial.GetNode<TutorialSpotlight>("Spotlight");
            Check(game.CraftMiniGameOpen && tutorial.Target == embroidery.StartButton &&
                tutorial.Layer > embroidery.Layer && spotlight.MouseFilter == Control.MouseFilterEnum.Ignore,
                "The ready lesson must highlight only the actual Start button above the modal");
            Check(embroidery.GetNode<Label>("Surface/Panel/Hint").Text.Length == 0,
                "The regular ready screen must not repeat the tutorial instruction");
            TranslationServer.SetLocale("en");
            await Frame();
            var embroideryBody = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            Check(embroideryBody.Tr(embroideryBody.Text) == "Click Start to begin embroidering.",
                "The embroidery lesson must be translated");
            TranslationServer.SetLocale("ru");
            foreach (Vector2I viewport in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            {
                Root.Size = viewport;
                await ToSignal(CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
                using var image = await CapturePresentation($"tutorial-embroidery-spotlight-{viewport.X}");
                Rect2 button = ScreenBounds(embroidery.StartButton);
                var material = (ShaderMaterial)spotlight.Material;
                Vector2 center = material.GetShaderParameter("focus_center").AsVector2();
                float darkness = material.GetShaderParameter("darkness").AsSingle();
                Check(image.GetSize() == viewport && center.DistanceTo(button.GetCenter()) < 1 &&
                    Mathf.IsEqualApprox(darkness, 0.50f), "The visible spotlight must settle over Start at both resolutions");
            }
            Root.Size = new Vector2I(1280, 720);
            await ClickControl(game, "EmbroideryGame/Surface/Panel/Buttons/Action");
            await Frame();
            var craftHint = embroidery.GetNode<Label>("Surface/Panel/Hint");
            Check(!tutorial.IsShowing && craftHint.Text == GameLocalization.Text("Нажимайте на подсвечивающиеся точки по очереди."),
                "Clicking Start must finish the spotlight lesson and reveal the stitch instruction");
            var sewingSurface = embroidery.GetNode<EmbroiderySurface>("Surface");
            for (int stitch = 0; stitch < 2; stitch++)
            {
                Vector2 point = sewingSurface.ScreenPoint(stitch);
                MouseButtonAt(point, MouseButton.Left, true);
                MouseButtonAt(point, MouseButton.Left, false);
            }
            Check(embroidery.Session.Stitches == 2, "Two real clicks must sew the first textured stitch");
            using (var image = await CapturePresentation("tutorial-stitch")) { }
            embroidery.FinishForTesting(true);
            Check(string.IsNullOrEmpty(craftHint.Text),
                "The craft result must only show the Collect button");
            await ClickControl(game, "EmbroideryGame/Surface/Panel/Buttons/Action");
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
            var inventory = game.GetNode<InventoryUI>("HUD/InventoryUI");
            MoveButtonPointer(ScreenBounds(inventory.Container.GetChild<Control>(0)).GetCenter());
            await Frame();
            Check(inventory.ItemPopup.IsVisibleInTree() && inventory.ItemPopup.GetParent<CanvasLayer>().Layer > tutorial.Layer &&
                HasHalo(inventory.ItemPopup),
                "The complete item tooltip must glow and render above the dimmer");
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
                game.BuyerWaitRemaining == game.BuyerWaitInitial && requestArrivals == 1,
                "The first customer and bubble must appear together with the request lesson at full patience");
            game.PersistProgress();
            await Reload();
            Step(FirstTradeTutorial.Step.BuyerRequest);
            Check(game.GetNode<Node2D>("World/TradingRoom/Customer").Visible &&
                game.BuyerWaitRemaining == game.BuyerWaitInitial,
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
            await Frame();
            Step(FirstTradeTutorial.Step.Offer);
            Check(game.GetNode<TextureRect>(iconPath).Texture.ResourcePath == "res://art/ui/buyer_wish_atelier.png" &&
                game.CustomerIndex == originalBuyer && game.ItemIndex == originalItem && game.Score == originalCash,
                "The introductory tutorial must progress directly to the offer with the real clothing request");
            await Reload();
            Step(FirstTradeTutorial.Step.Offer);
            await CheckTutorialEndBlocked();
            await CheckCameraLocked();
            Highlight();
            var buyerHint = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            Check(buyerHint.Text.Contains("✓") && !buyerHint.Text.Contains("×"),
                "The first offer instruction must describe only the sale");
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
            await Frame();
            Check(tutorial.Target == reject && buyerHint.Text.Contains("×") && !buyerHint.Text.Contains(" F"),
                "The reroll lesson must highlight the button and omit the keyboard shortcut");
            game.PersistProgress();
            await Reload();
            Check(tutorial.Target == game.GetNode<Button>(tradeButtons + "DeclineButton"),
                "Reloading must restore the current offer instruction");
            await ClickControl(game, continueInstruction);
            await Frame();
            Check(tutorial.Target.Name == "OfferLabel", "The quality lesson must highlight the offer");
            await ClickControl(game, continueInstruction);
            await Frame();
            Step(FirstTradeTutorial.Step.TimedSale);
            Check(!game.GetNode<Button>(tradeButtons + "AcceptButton").Disabled &&
                !game.GetNode<Button>(tradeButtons + "DeclineButton").Disabled, "Acknowledging the offer must enable the timed sale");
            game._Process(game.BuyerWaitRemaining + 1);
            Check(game.BuyerWaitRemaining == 1 && TradeScene.SpeedBonus(game.BuyerWaitRemaining, game.BuyerWaitInitial) > 0,
                "The first lesson must reserve one second and a real speed bonus");
            game.PersistProgress(); await Reload();
            Step(FirstTradeTutorial.Step.TimedSale);
            async Task CheckSaleGlow()
            {
                await CameraSettled();
                await CheckGlow("tutorial-glow-" + tutorial.CurrentStep);
                Check(tutorial.Target.Name == "ThoughtBubble" &&
                    HasHalo(game.GetNode<Control>("World/TradingRoom/Item/TradePanel")),
                    "Both timed sales must illuminate the thought bubble and the sale panel");
            }
            await CheckSaleGlow();
            int offerChangesBeforePractice = game.RerollsRemaining;
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = true }, true);
            Root.PushInput(new InputEventKey { Keycode = Key.F, Pressed = false }, true);
            Check(game.RerollsRemaining == offerChangesBeforePractice - 1 && game.BuyerWaitRemaining == 1,
                "Reroll may change the offer but not the remaining time");
            int cash = game.Score, payout = game.OfferedPrice + TradeScene.SpeedBonus(game.BuyerWaitRemaining, game.BuyerWaitInitial) +
                TradeScene.QualityBonus(game.ActiveTradeQualityRank);
            await ClickControl(game, tradeButtons + "AcceptButton");
            Check(game.Score == cash + payout && game.Accepted == 1, "The first sale must pay its real bonus");
            if (game.StoredItems.Length == 0)
            {
                game.GetItemFromAtelier();
                FinishEmbroidery(game);
            }
            game.SelectInventoryItem(0);
            game.PlaceItemInShowcase(0);
            game._Process(0);
            await Frame();
            Step(FirstTradeTutorial.Step.LateSale);
            await CheckSaleGlow();
            Check(game.GetNode<Button>(tradeButtons + "AcceptButton").Disabled, "The second sale must wait for zero");
            game._Process(game.BuyerWaitRemaining + 1);
            await Frame();
            Check(game.TradeActive && game.BuyerWaitRemaining == 0 && !game.GetNode<Button>(tradeButtons + "AcceptButton").Disabled &&
                TradeScene.SpeedBonus(game.BuyerWaitRemaining, game.BuyerWaitInitial) == 0, "The expired buyer must remain able to buy with no speed bonus");
            cash = game.Score;
            payout = game.OfferedPrice;
            await ClickControl(game, tradeButtons + "AcceptButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Summary);
            await CheckCameraLocked();
            Check(game.Score == cash + payout && game.Accepted == 2, "The second sale must pay exactly its late offer");
            CheckEmptyTutorialCounter();
            var pack = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            var toggle = game.GetNode<Control>("HUD/Root/ProgressToggleButton");
            Root.PushInput(new InputEventMouseMotion { Position = new Vector2(800, 400), GlobalPosition = new Vector2(800, 400) }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(!pack.Expanded && tutorial.Target == toggle,
                "Summary must highlight the pack without automatically opening it");
            Check(!tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done").Visible &&
                tutorial.GetNode<Button>(skipPath).Visible &&
                tutorial.GetNode<Label>("Root/Panel/Content/Title").Text == "Дни и квоты" &&
                !tutorial.GetNode<Label>("Root/Panel/Content/Body").Text.Contains("улучшение"),
                "Days and quotas must wait for pack hover without Next or the card-selection sentence");
            Highlight();
            await CheckGlow("tutorial-glow-pack");
            Vector2 packPoint = ScreenBounds(toggle).GetCenter();
            Root.PushInput(new InputEventMouseMotion { Position = packPoint, GlobalPosition = packPoint }, true);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(pack.Expanded, "Only the player's real hover must reveal the tutorial pack information");
            var packShader = (ShaderMaterial)tutorial.GetNode<TutorialSpotlight>("Spotlight").Material;
            await CheckGlow("tutorial-glow-cigarettes");
            Check(packShader.GetShaderParameter("focus_half_size").AsVector2().X * 2 > pack.CigaretteBounds().Size.X - 20,
                "The spotlight must follow the entire revealed cigarette strip");
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Summary &&
                !game.GetNode<DayTransitionCard>("DayTransitionCard").IsOpen,
                "Hovering for less than one second must not show the introduction or open cards");
            MoveButtonPointer(new Vector2(800, 400));
            await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
            MoveButtonPointer(packPoint);
            await ToSignal(CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Summary,
                "Leaving the pack must reset the one-second hover delay");
            await ToSignal(CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
            await WaitForDayCardAnimation(game);
            var trainingCards = game.GetNode<DayTransitionCard>("DayTransitionCard");
            int upgradeLessonDay = game.CurrentDay, upgradeLessonCash = game.Score;
            foreach (var upgradeSize in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = upgradeSize;
                TranslationServer.SetLocale(language);
                await Frame();
                await WaitForDayCardAnimation(game);
                Step(FirstTradeTutorial.Step.CardExplanation);
                var explanation = tutorial.GetNode<Label>("Root/Panel/Content/Body");
                Check(explanation.IsVisibleInTree() && Root.GetVisibleRect().Encloses(explanation.GetGlobalRect()) &&
                    explanation.Tr(explanation.Text).Contains(language == "en" ? "Some upgrades cost money" : "Некоторые улучшения стоят денег") &&
                    !trainingCards.Options.Contains(ChikagoShop.Domain.Days.DayUpgrade.Cash),
                    "The training hand must localize and contain only next-day cards");
                foreach (var upgrade in trainingCards.Options)
                    Check(Root.GetVisibleRect().Encloses(trainingCards.GetNode<Control>(
                        "Root/Center/Card/Content/Choices/" + upgrade).GetGlobalRect()),
                        "Tutorial upgrade cards must remain inside the viewport with the extra explanation");
            }
            game.PersistProgress();
            await Reload();
            await WaitForDayCardAnimation(game);
            trainingCards = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Check(trainingCards.IsOpen,
                "Reloading a pending tutorial choice must reopen its card menu");
            await Frame();
            Step(FirstTradeTutorial.Step.CardExplanation);
            await ClickControl(game, continueInstruction);
            await ClickControl(game, continueInstruction);
            var learnedUpgrade = trainingCards.Options.First(option => game.DayUpgrades.CostFor(option) <= game.Score);
            int learnedPrice = game.DayUpgrades.CostFor(learnedUpgrade);
            await ClickControl(game, "DayTransitionCard/Root/Center/Card/Content/Choices/" + learnedUpgrade + "/Content/Choose");
            await WaitForDayCardAnimation(game);
            await Frame();
            int learnedBalance = upgradeLessonCash - learnedPrice;
            Check(!trainingCards.IsOpen && !Paused && game.Score == learnedBalance &&
                game.DayUpgrades.Selected == learnedUpgrade && game.CurrentDay == upgradeLessonDay + 1 &&
                !game.DayStarted && !game.DayClosed && game.DayRemainingSeconds == DayCycleController.TutorialDuration,
                "A tutorial purchase must pay the real cost and prepare the following day");
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
                            (language == "en" ? "Hover over the notebook on the right to open it." :
                                "Наведите курсор на блокнот справа, чтобы открыть его."),
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
                Step(FirstTradeTutorial.Step.NotebookContents);
                Check(notebook.Expanded &&
                    (notebook.GetNode<Label>("Page/Notes/BuffDetails").Text != "Нет") ==
                        (game.DayUpgrades.Selected != ChikagoShop.Domain.Days.DayUpgrade.None),
                    "The open notebook must display the actual selected upgrade");
                await CheckGlow("tutorial-glow-notebook");
                await ClickControl(game, continueInstruction);
                await Frame();
                Step(FirstTradeTutorial.Step.Summary);
                Check(tutorial.GetNode<Label>("Root/Panel/Content/Title").Text == "Пауза и сохранение",
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
            skipLesson.SetValue("tutorial", "upgrade_lesson_day", game.CurrentDay);
            skipLesson.SetValue("tutorial", "notebook_lesson_done", false);
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
            Check(!trainingCards.IsOpen && !Paused && game.Score == 0 && game.CurrentDay == upgradeLessonDay + 2 &&
                !game.DayUpgrades.Pending && game.DayUpgrades.Selected == ChikagoShop.Domain.Days.DayUpgrade.None,
                "Skipping must be free, close the menu and continue to the next tutorial day");
            await CompleteNotebookLesson(false);
            Check(tutorial.GetNode<Label>("Root/Panel/Content/Title").Text == "Пауза и сохранение" &&
                tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done").Visible && tutorial.Target == null,
                "Completing the card choice must continue to pause and saving");
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
            Check(summaryTitle.Tr(summaryTitle.Text) == "Pause and saving" && summaryBody.Tr(summaryBody.Text).Contains("checkpoint"),
                "The last page must cover pause and persistence");
            TranslationServer.SetLocale("ru");
            await Frame();
            int completionEvents = 0;
            tutorial.Finished += () => completionEvents++;
            await ClickControl(game, summaryButton);
            Check(!tutorial.IsActive && !tutorial.IsShowing, "Acknowledging the summary must complete onboarding");
            Check(completionEvents == 1, "Natural tutorial completion must notify the startup sequence exactly once");
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
            var skipButton = tutorial.GetNode<Button>(skipPath);
            Vector2 skipPoint = skipButton.GetGlobalRect().GetCenter();
            Vector2 outsideSkip = new(6, Root.GetVisibleRect().Size.Y - 6);
            Rect2 inputRect = skipButton.GetGlobalRect();
            MoveButtonPointer(skipPoint);
            await ToSignal(CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
            Check(ButtonFeedbackController.Instance.FaceScale(skipButton).X > 1 && skipButton.GetGlobalRect() == inputRect,
                "Hover animation must only change drawing, not the hitbox");
            MouseButtonAt(skipPoint, MouseButton.Left, true);
            await ToSignal(CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
            Check(ButtonFeedbackController.Instance.FaceScale(skipButton).Y < 1, "Press must compress the art vertically");
            MoveButtonPointer(outsideSkip);
            MouseButtonAt(outsideSkip, MouseButton.Left, false);
            await ToSignal(CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
            Check(ButtonFeedbackController.Instance.FaceScale(skipButton).IsEqualApprox(Vector2.One) && !tutorial.IsSkipConfirmationOpen,
                "Pointer exit must restore the neutral face and cancel the press");
            await ClickControl(game, "FirstTradeTutorial/" + skipPath);
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
            await ClickControl(game, "FirstTradeTutorial/" + skipPath);
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
            Check(game.DayStarted && game.Score < game.StartingMoney && game.StoredItems.Length > 0,
                "Skip must be tested after real paid practice has changed the run");
            int scoreBeforeCancel = game.Score;
            await ClickControl(game, "FirstTradeTutorial/" + skipPath);
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel");
            Check(game.Score == scoreBeforeCancel && game.DayStarted && game.StoredItems.Length > 0,
                "Cancelling skip must preserve the current practice day and its items");
            await ClickControl(game, "FirstTradeTutorial/" + skipPath);
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
            await ClickControl(game, "FirstTradeTutorial/" + skipPath);
            game.NewGame();
            await Frame();
            Check(tutorial.IsActive && !tutorial.IsSkipConfirmationOpen && !Paused,
                "Restarting a run while confirmation is open must release its pause and reset onboarding");
            await ClickControl(game, "FirstTradeTutorial/" + skipPath);
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
    private async Task TestWorkshopTutorial()
    {
        const string save = "user://workshop_tutorial_test.json";
        const string profile = save + ".tutorial.cfg";
        const string next = "FirstTradeTutorial/Root/Panel/Content/Actions/Done";
        TradeScene game = null;
        async Task<FirstTradeTutorial> ReloadWorkshop()
        {
            if (game != null)
            {
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            return game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
        }
        try
        {
            var config = new ConfigFile();
            config.SetValue("tutorial", "finished", true);
            Check(config.Save(profile) == Error.Ok, "Workshop test profile must save");
            ProgressSaveStore.Save(save, new ChikagoShop.Domain.Persistence.ProgressData
            {
                SaveVersion = ChikagoShop.Domain.Persistence.ProgressJson.CurrentVersion,
                Day = 8, QuotaStage = 1, Score = 1000, DayStarted = false, EnergyRemaining = 2,
                QuotaVisit = ChikagoShop.Domain.Persistence.QuotaVisitStage.Rewarded,
                CheckpointQuotaVisit = ChikagoShop.Domain.Persistence.QuotaVisitStage.Rewarded,
                CheckpointDay = 8
            });
            var tutorial = await ReloadWorkshop();
            Check(game.MusicWorkshopAvailable && !game.MusicWorkshopUnlocked && !tutorial.IsShowing,
                "Finishing the story exposes a separate unlock action without starting the music lesson");
            game.TryUnlockMusicWorkshop();
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(tutorial.IsShowing && tutorial.CurrentStep == FirstTradeTutorial.Step.MusicWorkshop &&
                game.MusicWorkshopUnlocked && game.Score == 1000 && !game.DayStarted &&
                !game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible,
                "Opening the workshop must start its lesson, keep the balance and leave an empty counter");
            await ClickControl(game, next);
            Check(tutorial.Target == null, "The polishing explanation has no fixed target");
            await ClickControl(game, next);
            Check(tutorial.Target == game.GetNode<Control>("World/Showroom/Showcases/Slot4"),
                "The third workshop explanation must point at a music display");
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            Check(tutorial.IsSkipConfirmationOpen, "Workshop instruction must offer skipping");
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Confirm");
            tutorial = await ReloadWorkshop();
            Check(!tutorial.IsShowing && !tutorial.IsActive && game.MusicWorkshopUnlocked && game.CurrentDay == 8 &&
                !game.DayStarted && game.Score == 1000 && !game.GetNode<WorldCamera>("Camera2D").NavigationLocked,
                "Skipping the workshop lesson must persist without resetting or locking the campaign");
            game.EndDay();
            Check(game.DayStarted && game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible,
                "The regular buyer can arrive after Start");
        }
        finally
        {
            game?.QueueFree();
            Paused = false;
            foreach (string file in new[] { save, profile })
                if (FileAccess.FileExists(file)) DirAccess.RemoveAbsolute(file);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

}
