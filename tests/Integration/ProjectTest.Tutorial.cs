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
            Step(FirstTradeTutorial.Step.Start);
            Highlight();
            Check(!Paused && !game.DayStarted && game.Score == game.StartingMoney && game.StoredItems.Length == 0,
                "Starting the tutorial must not pause gameplay, spend money or generate items");
            await ClickControl(game, "HUD/Root/DayTimer/StartButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Craft);
            using (var image = await CapturePresentation("tutorial-craft")) { }
            await ClickControl(game, "World/Atelier/GetItemButton");
            await Frame();
            Check(game.CraftMiniGameOpen && !tutorial.IsShowing, "Tutorial must hide during the crafting modal");
            FinishEmbroidery(game);
            await Frame();
            Step(FirstTradeTutorial.Step.Select);
            Highlight();
            await Reload();
            Step(FirstTradeTutorial.Step.Select);
            var slots = game.GetNode<InventoryUI>("HUD/InventoryUI").Container;
            await ClickControl(game, "HUD/InventoryUI/ScrollContainer/HBoxContainer/" + slots.GetChild(0).Name);
            await Frame();
            Step(FirstTradeTutorial.Step.Place);
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
                var panel = tutorial.GetNode<Control>("Root/Panel");
                var body = tutorial.GetNode<Label>("Root/Panel/Content/Body");
                Check(Root.GetVisibleRect().Encloses(panel.GetGlobalRect()) &&
                    body.Tr(body.Text).StartsWith(language == "en" ? "Click" : "Нажмите"),
                    "Tutorial panel and live translated instructions must fit both viewport sizes");
            }
            await ClickControl(game, "World/ShowroomLeft/Showcases/Slot1");
            await Frame();
            Step(FirstTradeTutorial.Step.WaitBuyer);
            game._Process(game.BuyerCountdown + 0.01);
            await Frame();
            Step(FirstTradeTutorial.Step.Offer);
            Highlight();
            var buyerHint = tutorial.GetNode<Label>("Root/Panel/Content/Body");
            Check(buyerHint.Text.Contains("✓") && buyerHint.Text.Contains("×"),
                "Offer instructions must identify buttons by their visible symbols, without relying on color");
            int cash = game.Score, offer = game.OfferedPrice;
            await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
            await Frame();
            Step(FirstTradeTutorial.Step.Summary);
            Check(game.Score == cash + offer && game.Accepted == 1, "The guided sale must apply exactly the normal reward once");
            await Reload();
            Step(FirstTradeTutorial.Step.Summary);
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Done");
            Check(!tutorial.IsActive && !tutorial.IsShowing, "Acknowledging the summary must complete onboarding");
            await Reload();
            Check(!tutorial.IsActive, "Completed onboarding must not replay when loading the game");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            await Frame();
            Check(!tutorial.IsShowing && Paused, "Pause must hide tutorial hints");
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/TutorialButton");
            await Frame();
            Check(tutorial.IsActive && !Paused, "The scene-owned pause button must allow replay without resetting the run");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            await Frame();
            Check(!tutorial.IsShowing && Paused, "An active tutorial must hide its panel and highlight while paused");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            await Frame();
            Check(tutorial.IsShowing && !Paused, "Closing pause must restore the current instruction without advancing it");
            game.NewGame();
            await Frame();
            Step(FirstTradeTutorial.Step.Start);
            await ClickControl(game, "FirstTradeTutorial/Root/Panel/Content/Actions/Skip");
            await Reload();
            Check(!tutorial.IsActive && !tutorial.IsShowing, "Skipping must persist just like completing onboarding");
            game.NewGame();
            await Frame();
            Check(!tutorial.IsActive, "NewGame must preserve the player's choice to skip onboarding");
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
