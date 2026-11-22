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
        try
        {
            int[] expectedPrices = [25, 45, 40, 70, 75, 115, 195, 115, 175, 295];
            Check(WorkshopItemCatalog.Items.Select(item => item.BasePrice).SequenceEqual(expectedPrices) && price == 1000,
                "All catalogue items must use the new base prices and the music workshop must cost $1000");
            SetLastDay(4000);
            await Reload();
            var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
            Check(Mathf.IsEqualApprox(notebook.HoverBounds.Size.X, 67.2f) &&
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
                Check(page.Size == pageSize && notes.Modulate.A == 1 && Mathf.IsEqualApprox(notebook.HoverBounds.Size.X, pageSize.X * 1.2f),
                    "The open notebook must keep its texture size and get the same 20% hover expansion");
            }
            game.EndDay();
            game._Process(0);
            Check(game.QuotaStage == 1 && game.MafiaNoticePending && !game.GetNode<TextureButton>(visitorPath).Visible,
                "The visitor must wait until the existing successful quota scene ends");
            int cashAfterQuota = game.Score;
            game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice").Close();
            Check(game.GetNode<TextureButton>(visitorPath).Visible && game.QuotaVisitPending && !game.DayStarted,
                "The visitor must replace the regular customer before the next day");
            game.EndDay();
            Check(!game.DayStarted, "The next day cannot start until the scene ends");
            await Reload();
            Check(game.GetNode<TextureButton>(visitorPath).Visible && !Paused, "Reload must restore the waiting visitor");
            await ToSignal(CreateTimer(0.7), SceneTreeTimer.SignalName.Timeout);
            if (DisplayServer.GetName() == "headless") game.GetNode<TextureButton>(visitorPath).EmitSignal(BaseButton.SignalName.Pressed);
            else await ClickControl(game, visitorPath);
            Check(Paused && Dialogue().Session.Current.Text.Contains("1000"), "Clicking the visitor must open the existing dialogue UI");
            await ToSignal(this, SignalName.ProcessFrame);
            Check(!game.GetNode<TextureButton>(visitorPath).Visible &&
                Dialogue().GetNode<TextureRect>("Characters/Son").Visible,
                "The son must use the full portrait while the world visitor is hidden during dialogue");
            Dialogue()._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Check(!Paused && game.GetNode<TextureButton>(visitorPath).Visible && game.QuotaVisitPending,
                "Escape must close the story layout and restore the clickable visitor");
            await ToSignal(this, SignalName.ProcessFrame);
            game.GetNode<TextureButton>(visitorPath).EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(this, SignalName.ProcessFrame);
            Dialogue().RestorePage(0, true);
            Check(ProgressSaveStore.Load(save, 3).QuotaVisit == QuotaVisitStage.Dialogue,
                "Starting the conversation must save the dialogue phase");
            await Reload();
            Check(Paused && game.GetNodeOrNull<DialoguePlayer>("QuotaDialogue/DialoguePlayer") is { SavedPageRevealed: true } &&
                game.Score == cashAfterQuota,
                $"Reload must resume the revealed dialogue without granting the gift. Paused={Paused}, pending={game.QuotaVisitPending}, saved={ProgressSaveStore.Load(save, 3).QuotaVisit}");
            TranslationServer.SetLocale("en");
            Check(Dialogue().GetNode<Label>("Panel/Content/Speaker").Text == "Son", "The story dialogue must localize in English");
            for (int i = 0; i < 8 && game.QuotaVisitPending; i++) Dialogue().Advance();
            Check(!game.QuotaVisitPending && game.Score == cashAfterQuota + 1000 && !game.MusicWorkshopUnlocked,
                "Finishing must add the gift exactly once and leave the workshop locked");
            Check(game.GetNode<DayAnnouncementOverlay>("DayAnnouncements").IsShowing &&
                game.GetNode<Label>("DayAnnouncements/Root/Title").Text == GameLocalization.Format("Получено {0}$", TradeScene.QuotaGift), "The standard overlay must announce the gift");
            var customer = game.GetNode<ModularCustomer>("World/TradingRoom/Customer");
            game._Process(0.1);
            Check(!customer.Visible && !game.DayStarted, "The counter must remain empty after the dialogue");
            await Reload();
            Check(!game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible,
                "Restoring a claimed reward before Start must leave the counter empty");
            Check(!game.GetNode<TextureButton>(visitorPath).Visible && game.Score == cashAfterQuota + 1000,
                "Reload must retain the reward and the completed event");
            game.ReturnToCheckpoint();
            Check(!game.QuotaVisitPending && game.Score == cashAfterQuota + 1000, "Checkpoint must restore both the gift and the claimed flag");
            game.EndDay();
            Check(game.GetNode<ModularCustomer>("World/TradingRoom/Customer").Visible, "Start must resume ordinary arrivals");
            game.TryUnlockMusicWorkshop();
            Check(game.MusicWorkshopUnlocked && game.Score == cashAfterQuota, "Buying the workshop must charge the configured $1000");
            game.NewGame();
            Check(ProgressSaveStore.Load(save, 3).QuotaVisit == QuotaVisitStage.Unavailable, "A new run must reset the visitor state");

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
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
