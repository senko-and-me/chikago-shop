using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestQuotaVisit()
    {
        const string save = "user://quota_visit_test.json";
        const string visitorPath = "World/TradingRoom/QuotaVisitor";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        var config = EconomyConfigProvider.Load();
        int deadline = config.Quotas.Single(quota => quota.Id == "first").DeadlineDay;
        int price = config.Workshops.Single(workshop => workshop.Id == "music").UnlockCost;
        async Task Reload()
        {
            if (game != null)
            {
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            game = scene.Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        void SetLastDay(int score) => ProgressSaveStore.Save(save, new ProgressData
        {
            SaveVersion = ProgressJson.CurrentVersion, Day = deadline, Score = score, DayStarted = true, EnergyRemaining = 3
        });
        DialoguePlayer Dialogue() => game.GetNode<DialoguePlayer>("QuotaDialogue/DialoguePlayer");
        async Task WaitForQuotaFade()
        {
            for (int frame = 0; frame < 240 && SceneTransition.IsActive(this); frame++)
                await ToSignal(this, SignalName.ProcessFrame);
            Check(!SceneTransition.IsActive(this), "The quota fade must finish without leaving a black/input-blocking overlay");
        }
        void CheckCigarettes(int day, int daysLeft, string russianUnit, int amount)
        {
            var cigarettes = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            foreach (string language in new[] { "ru", "en" })
            {
                TranslationServer.SetLocale(language);
                cigarettes._Process(0);
                string dayText = cigarettes.GetNode<Label>("Visual/Day/Caption").Text;
                string quotaText = cigarettes.GetNode<Label>("Visual/Quota/Caption").Text;
                string timeText = cigarettes.GetNode<Label>("Visual/Deadline/Caption").Text;
                Check(dayText == (language == "ru" ? $"День {day}" : $"Day {day}") &&
                    quotaText.Contains(amount.ToString()) &&
                    timeText.Contains(language == "ru" ? $"{daysLeft} {russianUnit}" : $"{daysLeft} day"),
                    $"Incorrect cigarettes at {language} day {day}: {dayText} / {quotaText} / {timeText}");
            }
        }
        string originalLanguage = TranslationServer.GetLocale();
        try
        {
            var familyDialogue = ChikagoShop.Domain.Dialogue.DialogueSession.FromJson(
                FileAccess.GetFileAsString("res://dialogue/quota_visitor.ru.json"));
            Check(familyDialogue.Count == 6, "The workshop visit must remain a short conversation");
            TranslationServer.SetLocale("en");
            while (!familyDialogue.IsComplete)
            {
                var line = familyDialogue.Current;
                Check(line.Speaker == (familyDialogue.Index % 2 == 0 ? "Сын" : "Френки"),
                    "Both son and father must answer each other");
                Check(GameLocalization.Text(line.Text) != line.Text,
                    "Every revised family line must have an English translation");
                familyDialogue.Advance();
            }
            TranslationServer.SetLocale("ru");
            int[] expectedPrices = [25, 45, 40, 70, 75, 115, 195, 115, 175, 295];
            Check(WorkshopItemCatalog.Items.Select(item => item.BasePrice).SequenceEqual(expectedPrices) && price == 0,
                "Catalogue prices remain unchanged and the story workshop has no purchase cost");
            int[] remaining = [7, 6, 5, 4, 3, 2, 1, 7, 6, 5, 4, 3, 2, 1];
            string[] units = ["дней", "день", "дня", "дня", "дня", "дней", "дней", "дней"];
            for (int i = 0; i < remaining.Length; i++)
            {
                int stage = i < deadline ? 0 : 1;
                ProgressSaveStore.Save(save, new ProgressData
                {
                    SaveVersion = ProgressJson.CurrentVersion, Day = i + 1, QuotaStage = stage,
                    Score = 4000, EnergyRemaining = 3
                });
                await Reload();
                CheckCigarettes(i + 1, remaining[i], units[remaining[i]], stage == 0 ? 750 : 1500);
            }
            SetLastDay(4000);
            await Reload();
            game.TryUnlockMusicWorkshop();
            Check(!game.MusicWorkshopUnlocked && game.Score == 4000,
                "Money and a direct action must not unlock the workshop during week one");
            var lockedNotice = game.GetNode<MusicWorkshopUnlockPanel>("World/MusicWorkshop/LockedOverlay/UnlockPanel");
            Check(!lockedNotice.IsVisibleInTree() && !lockedNotice.UnlockButton.IsVisibleInTree(),
                "The first-week curtain must hide the panel and its button");
            lockedNotice.UnlockButton.EmitSignal(BaseButton.SignalName.Pressed);
            Check(!game.GetNode<Control>("DayStartGate/Root/Notice").Visible,
                "A locked-room action must not display a notice");
            ProgressSaveStore.Save(save, new ProgressData { SaveVersion = ProgressJson.CurrentVersion, Day = deadline, Score = 4000, DayStarted = false });
            await Reload();
            Check(!game.GetNode<MusicWorkshopUnlockPanel>("World/MusicWorkshop/LockedOverlay/UnlockPanel").IsVisibleInTree(),
                "The locked panel must remain hidden before day Start on reload");
            SetLastDay(4000);
            await Reload();
            lockedNotice = game.GetNode<MusicWorkshopUnlockPanel>("World/MusicWorkshop/LockedOverlay/UnlockPanel");
            TranslationServer.SetLocale("ru");
            if (DisplayServer.GetName() != "headless")
            {
                var camera = game.GetNode<WorldCamera>("Camera2D");
                Vector2 position = camera.Position, zoom = camera.Zoom;
                camera.Position = lockedNotice.GetGlobalRect().GetCenter();
                camera.Zoom = Vector2.One * 0.8f;
                camera.ForceUpdateScroll();
                using (await CapturePresentation("story-workshop-locked-ru")) { }
                camera.Position = position;
                camera.Zoom = zoom;
                camera.ForceUpdateScroll();
            }
            var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
            // Keep the desktop pointer from opening this unrelated hover widget.
            notebook.Hide();
            notebook._Process(0);
            notebook.Show();
            notebook.SetProcess(false);
            Check(Mathf.IsEqualApprox(notebook.HoverBounds.Position.X, notebook.ClosedX - 11.2f) &&
                !notebook.HasNode("Page/Notes/Footer") && !notebook.GetNode<Label>("Page/Notes/DebuffsTitle").Visible,
                "The notebook tab must have 20% extra hover area and only real content");
            if (DisplayServer.GetName() != "headless")
            {
                var page = notebook.GetNode<TextureRect>("Page");
                var notes = page.GetNode<Control>("Notes");
                Vector2 pageSize = page.Size;
                var before = GetProcessedTweens().ToHashSet();
                page.EmitSignal(Control.SignalName.GuiInput,
                    new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
                Check(notebook.Expanded, "Pinning the notebook must begin its expansion");
                var slide = GetProcessedTweens().Single(tween => !before.Contains(tween));
                slide.Pause();
                slide.CustomStep(notebook.SlideSeconds / 2);
                Check(notebook.Expanded && notes.Modulate.A is > 0 and < 1, "The notebook writing must fade during the slide");
                slide.CustomStep(notebook.SlideSeconds);
                Check(page.Size == pageSize && notes.Modulate.A == 1 &&
                    Mathf.IsEqualApprox((notebook.GetGlobalTransformWithCanvas() * notebook.HoverBounds).End.X, Root.GetVisibleRect().End.X),
                    "The open notebook must keep its texture size and extend its hover area to the window edge");
            }
            game.EndDay();
            game._Process(0);
            Check(game.QuotaStage == 1 && game.MafiaNoticePending && !game.GetNode<TextureButton>(visitorPath).Visible,
                "The visitor must wait until the existing successful quota scene ends");
            int cashAfterQuota = game.Score;
            CheckCigarettes(8, 7, "дней", 1500);
            var mafia = game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice");
            Check(SceneTransition.IsActive(this) && Paused && !mafia.IsOpen,
                "The world must fade first, with the clock paused, before Castello appears");
            for (int frame = 0; frame < 120 && !mafia.IsOpen; frame++)
                await ToSignal(this, SignalName.ProcessFrame);
            Check(mafia.IsOpen && Root.GetNode<SceneTransition>("SceneTransition").Opacity >= 0.99f,
                "The dialogue must appear behind a fully black cover");
            await WaitForQuotaFade();
            Check(Paused && mafia.IsOpen, "Fade-in must retain the dialogue pause");
            game.ResumeQuotaVisit();
            Check(!game.GetNode<TextureButton>(visitorPath).Visible, "The son must wait until Castello finishes");
            await ClickControl(game, "MafiaQuotaNotice/StoryRoot/SkipDialogue");
            Check(SceneTransition.IsActive(this) && mafia.IsOpen && game.MafiaNoticePending,
                "Continue must fade out before closing or acknowledging Castello");
            mafia._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            await WaitForQuotaFade();
            Check(!mafia.IsOpen && Paused && !game.MafiaNoticePending && game.Score == cashAfterQuota &&
                game.QuotaVisitPending && Dialogue().Session.Index == 0,
                "Skipping Castello must automatically start only the son's dialogue, without skipping its first line");
            Check(game.GetNode<TextureButton>(visitorPath).Visible && !game.DayStarted,
                "No visitor click or playable gap may appear between the two conversations");
            game.EndDay();
            Check(!game.DayStarted, "The next day cannot start until both conversations end");
            await Reload();
            Check(Paused && Dialogue().Session.Current.Text.Contains("Пап") && !game.MusicWorkshopUnlocked,
                "The family conversation must open in the existing dialogue UI without unlocking prematurely");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.GetNode<TextureButton>(visitorPath).Visible && Dialogue().UseWorldScene &&
                !Dialogue().GetNode<Control>("Background").Visible &&
                !Dialogue().GetNode<Control>("Characters").Visible &&
                game.GetNode<Node2D>("World/TradingRoom").IsAncestorOf(game.GetNode(visitorPath)),
                "The son must stand in the real trading room; the overlay must not replace its background or father");
            mafia = game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice");
            Check(!mafia.HasNode("StoryRoot/Speech/Content/Actions/Continue") &&
                mafia.GetNode<Control>("StoryRoot/SkipDialogue").AnchorRight == 1,
                "Castello must use a top-right prologue skip button without Continue");
            var storyCamera = game.GetNode<WorldCamera>("Camera2D");
            var storyRoom = game.GetNode<Control>("World/TradingRoom/RoomFrame").GetGlobalRect();
            Vector2 storyCenter = storyRoom.GetCenter() + storyRoom.Size * new Vector2(0.08f, 0.04f);
            Check(storyCamera.Position.DistanceTo(storyCenter) < 0.1f &&
                !game.GetNode<CanvasLayer>("HUD").Visible && !game.GetNode<CanvasLayer>("HUD/DayClock").Visible,
                "The camera must frame the actual trading room and hide the gameplay HUD during the story");
            if (DisplayServer.GetName() != "headless")
            {
                Vector2I storySize = Root.Size;
                foreach (string language in new[] { "ru", "en" })
                {
                    Root.Size = language == "ru" ? new Vector2I(1280, 720) : new Vector2I(1920, 1080);
                    TranslationServer.SetLocale(language);
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    Dialogue().RestorePage(0, true);
                    Check(storyCamera.Position.DistanceTo(storyCenter) < 0.1f &&
                        Root.GetVisibleRect().Encloses(Dialogue().GetNode<Control>("Panel/Content").GetGlobalRect()),
                        "Resizing the story must preserve the real room framing and readable dialogue");
                    using (await CapturePresentation("story-workshop-dialogue-" + language)) { }
                }
                Root.Size = storySize;
                TranslationServer.SetLocale("ru");
            }
            Dialogue()._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Check(Paused && game.QuotaVisitPending && Dialogue().Session.Index == 0,
                "A repeated Escape from Castello must not also skip the automatically opened family dialogue");
            game.TryUnlockMusicWorkshop();
            Check(!game.MusicWorkshopUnlocked && game.Score == cashAfterQuota,
                "The unfinished family dialogue must not grant access or money");
            Dialogue().RestorePage(0, true);
            Check(ProgressSaveStore.Load(save, 3).QuotaVisit == QuotaVisitStage.Dialogue,
                "Starting the conversation must save the dialogue phase");
            await Reload();
            Check(Paused && game.GetNodeOrNull<DialoguePlayer>("QuotaDialogue/DialoguePlayer") is { SavedPageRevealed: true } &&
                game.Score == cashAfterQuota && !game.MusicWorkshopUnlocked,
                $"Reload must resume the revealed dialogue without granting the gift. Paused={Paused}, pending={game.QuotaVisitPending}, saved={ProgressSaveStore.Load(save, 3).QuotaVisit}");
            TranslationServer.SetLocale("en");
            Check(Dialogue().GetNode<Label>("Panel/Content/Speaker").Text == "Son", "The story dialogue must localize in English");
            Check(GameLocalization.Text(Dialogue().Session.Current.Text).Contains("Dad"),
                "The son must address his father naturally in English too");
            var cameraAfterStory = game.GetNode<WorldCamera>("Camera2D");
            cameraAfterStory.ResetToOverview();
            Vector2 overviewPosition = cameraAfterStory.Position, overviewZoom = cameraAfterStory.Zoom;
            cameraAfterStory.FocusOn(game.GetNode<Control>("World/TradingRoom/RoomFrame").GetGlobalRect().GetCenter(),
                cameraAfterStory.CloseupSize, 0);
            Vector2 lockedCamera = cameraAfterStory.Position;
            cameraAfterStory._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new Vector2(400, 200) });
            cameraAfterStory._Input(new InputEventMouseMotion { Relative = new Vector2(80, 40), ButtonMask = MouseButtonMask.Left });
            Check(!cameraAfterStory.IsDragging && cameraAfterStory.Position == lockedCamera,
                "The camera must stay locked while the son is speaking");
            for (int i = 0; i < 24 && game.QuotaVisitPending; i++) Dialogue().Advance();
            await WaitForQuotaFade();
            if (game.DayTransitionOpen)
            {
                await WaitForDayCardAnimation(game);
                game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
                await WaitForDayCardAnimation(game);
            }
            Check(!Paused && game.GetNode<CanvasLayer>("HUD").Visible && cameraAfterStory.Position.DistanceTo(overviewPosition) < 0.05f &&
                cameraAfterStory.Zoom.DistanceTo(overviewZoom) < 0.001f,
                "Completing both dialogues must restore the initial overview and camera control");
            cameraAfterStory._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new Vector2(400, 200) });
            Check(cameraAfterStory.IsDragging, "Camera drag must be available again after the story sequence");
            cameraAfterStory._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
            Check(!game.QuotaVisitPending && game.Score == cashAfterQuota && game.MusicWorkshopAvailable &&
                !game.MusicWorkshopUnlocked && !game.CheckpointMusicWorkshopUnlocked,
                "The dialogue must grant only permission, leaving the balance and curtain untouched");
            Check(!game.GetNode<DayAnnouncementOverlay>("DayAnnouncements").IsShowing,
                "The finished conversation must not announce cash");
            var customer = game.GetNode<ModularCustomer>("World/TradingRoom/Customer");
            game._Process(0.1);
            Check(!customer.Visible && !game.DayStarted, "The counter must remain empty after the dialogue");
            await Reload();
            Check(!game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible,
                "Restoring a completed visit before Start must leave the counter empty");
            Check(!game.GetNode<TextureButton>(visitorPath).Visible && game.Score == cashAfterQuota,
                "Reload must retain the balance and the completed event");
            game.ReturnToCheckpoint();
            Check(!game.QuotaVisitPending && game.Score == cashAfterQuota, "Checkpoint must restore completed access and the original cash");
            CheckCigarettes(8, 7, "дней", 1500);
            var unlockPanel = game.GetNode<MusicWorkshopUnlockPanel>("World/MusicWorkshop/LockedOverlay/UnlockPanel");
            Check(game.MusicWorkshopAvailable && unlockPanel.IsVisibleInTree() && !unlockPanel.UnlockButton.Disabled &&
                GameLocalization.Text(unlockPanel.UnlockButton.Text) == "Open workshop",
                "The localized button must appear after the restored completed visit");
            unlockPanel.UnlockButton.EmitSignal(BaseButton.SignalName.Pressed);
            Check(game.MusicWorkshopUnlocked && game.Score == cashAfterQuota &&
                !game.DayStarted && !game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible,
                "Repeated legacy unlock actions must neither charge cash nor start the day");
            await Reload();
            Check(game.MusicWorkshopUnlocked && !game.DayStarted && game.Score == cashAfterQuota &&
                !game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible,
                "Reload must keep the workshop open and the store waiting for Start");
            game.EndDay();
            Check(game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible, "Start must resume ordinary arrivals");
            game.NewGame();
            CheckCigarettes(1, 7, "дней", 750);
            Check(ProgressSaveStore.Load(save, 3).QuotaVisit == QuotaVisitStage.Unavailable, "A new run must reset the visitor state");

            // A previously claimed gift could leave the old paid workshop locked.
            ProgressSaveStore.Save(save, new ProgressData
            {
                SaveVersion = 19, Day = 8, QuotaStage = 1, Score = 0, DayStarted = false,
                QuotaVisit = QuotaVisitStage.Rewarded, CheckpointQuotaVisit = QuotaVisitStage.Rewarded,
                CheckpointDay = 8, CheckpointScore = 0
            });
            await Reload();
            Check(game.MusicWorkshopAvailable && !game.MusicWorkshopUnlocked && game.Score == 0 &&
                !game.QuotaVisitPending && !game.DayStarted,
                "An old rewarded but locked save must regain permission without opening or transferring cash");
            game.ReturnToCheckpoint();
            await Reload();
            Check(game.MusicWorkshopAvailable && !game.MusicWorkshopUnlocked && game.Score == 0,
                "Migrated permission must survive saving and checkpoint rollback even with no money");

            // A player who only just pays the quota must receive the same free access.
            SetLastDay(game.FirstQuota);
            await Reload();
            game.EndDay();
            game._Process(0);
            await WaitForQuotaFade();
            var zeroCashNotice = game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice");
            zeroCashNotice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
            Check(zeroCashNotice.IsOpen && zeroCashNotice.GetNode<Label>("StoryRoot/Speech/Content/Message").VisibleCharacters == -1,
                "The first prologue-style input must reveal Castello's line without closing it");
            zeroCashNotice._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
            await WaitForQuotaFade();
            Check(!zeroCashNotice.IsOpen, "The next input must finish Castello before the automatic son dialogue");
            Check(game.Score == 0 && !game.MusicWorkshopUnlocked, "Exact quota payment must leave a pending story and zero cash");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(Paused && Dialogue().Session.Index == 0, "The son must open automatically even with zero cash");
            await ClickControl(game, "QuotaDialogue/DialoguePlayer/SkipDialogue");
            await WaitForQuotaFade();
            Check(game.MusicWorkshopAvailable && !game.MusicWorkshopUnlocked && game.Score == 0,
                "Completing the conversation with zero cash must make opening available");
            if (game.DayTransitionOpen)
            {
                await WaitForDayCardAnimation(game);
                game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
                await WaitForDayCardAnimation(game);
            }
            game.TryUnlockMusicWorkshop();
            Check(game.MusicWorkshopUnlocked && game.Score == 0 && !game.DayStarted,
                "Opening must succeed for a player with no cash without starting the day");

            SetLastDay(0);
            await Reload();
            game.EndDay();
            game._Process(0);
            Check(game.Failed && !game.QuotaVisitPending && !game.GetNode<TextureButton>(visitorPath).Visible && game.Score == 0,
                "Failure to pay must not show the visitor or grant a gift");
        }
        finally
        {
            game?.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(originalLanguage);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
