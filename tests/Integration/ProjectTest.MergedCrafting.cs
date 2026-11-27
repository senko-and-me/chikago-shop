using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestMergedCrafting()
    {
        const string path = "user://merged_crafting.json";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        Vector2I originalSize = Root.Size;
        string locale = TranslationServer.GetLocale();
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        async Task Reload(ProgressData data = null)
        {
            if (game != null) { game.QueueFree(); await Frame(); }
            if (data != null) ProgressSaveStore.Save(path, data);
            game = scene.Instantiate<TradeScene>();
            game.SavePath = path;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
            game.GetNode<EmbroideryGame>("EmbroideryGame").SetProcess(false);
            game.GetNode<EmbroideryGame>("PolishingGame").SetProcess(false);
            await Frame();
        }
        void Fresh(bool music = false)
        {
            game.NewGame();
            if (music) UnlockMusicForFixture(game);
            game.Score = 10000;
            game.EnergyRemaining = 0;
            game.EndDay();
        }
        EmbroideryGame Mini(string room) => game.GetNode<EmbroideryGame>(room == "music" ? "PolishingGame" : "EmbroideryGame");
        async Task Select(string kind)
        {
            int index = WorkshopItemCatalog.IndexOf(kind);
            string room = WorkshopItemCatalog.Items[index].WorkshopId;
            game.Crafting.OpenCatalog(room);
            Check(game.Crafting.IsCatalogOpen && Paused, "The item catalog must pause the day");
            var button = game.Crafting.Catalog.ItemButton(kind);
            var scroll = game.Crafting.Catalog.GetNode<ScrollContainer>("Root/Center/Panel/Margin/Content/Scroll");
            scroll.EnsureControlVisible(button);
            await Frame();
            if (kind == "hat" || kind == "guitar")
            {
                using var capture = await CapturePresentation("m223-catalog-" + kind);
            }
            await ClickControl(game, button.GetPath().ToString().Replace(game.GetPath() + "/", ""));
            Check(Mini(room).IsOpen && Mini(room).PreparedItemIndex == index, "The selected kind must open its own preparation");
        }
        async Task Craft(string kind, bool success = true)
        {
            await Select(kind);
            var mini = Mini(WorkshopItemCatalog.Items[WorkshopItemCatalog.IndexOf(kind)].WorkshopId);
            mini.Activate();
            mini.FinishForTesting(success);
            mini.Activate();
            await Frame();
        }
        try
        {
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
            await Reload();
            Fresh();
            Check(!game.GetNode<InventoryUI>("HUD/InventoryUI").IsVisibleInTree(), "The old inventory must not appear");
            Check(DebugBalanceSettings.Cards.SequenceEqual(new[] { DayUpgrade.Cash, DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice }),
                "Only cash and sale-price cards remain active");
            game.Crafting.OpenCatalog("music");
            Check(!game.Crafting.IsCatalogOpen, "A locked room must still reject its catalogue");
            var debug = Root.GetNode<DebugBalancePanel>("DebugBalance");
            debug.Open();
            var tabs = debug.GetNode<TabContainer>("Root/Inset/Panel/Content/Tabs");
            Check(!tabs.HasNode("Редкости"), "The retired rarity settings must not remain as an empty tab");
            for (int tab = 0; tab < tabs.GetTabCount(); tab++) { tabs.CurrentTab = tab; await Frame(); }
            Check(debug.Editor("craft.hat.cost") != null, "Per-item prices must remain editable");
            debug.Close();
            var patterns = new HashSet<string>();
            foreach (var item in WorkshopItemCatalog.Items)
            {
                Fresh(item.WorkshopId == "music");
                Root.Size = item.WorkshopId == "music" ? new Vector2I(960, 540) : new Vector2I(1280, 720);
                TranslationServer.SetLocale(item.WorkshopId == "music" ? "en" : "ru");
                int index = WorkshopItemCatalog.IndexOf(item.Kind), slot = item.WorkshopId == "music" ? 3 : 0;
                await Select(item.Kind);
                Check(!game.Crafting.Catalog.ItemButton(item.Kind).GetParent().GetNode<Label>("Rarity").Visible,
                    "The catalogue must not show retired rarity metadata");
                var mini = Mini(item.WorkshopId);
                int funds = game.Score;
                mini.Close();
                Check(game.Crafting.IsCatalogOpen && game.Score == funds && game.ShowcaseItems.All(value => value < 0),
                    "Preparation cancellation must not pay or reserve a showcase");
                game.Crafting.Catalog.Close();
                await Select(item.Kind);
                mini.Activate();
                Check(game.Score == funds - WorkshopItemCatalog.ItemCraftCost(index) && game.ShowcaseItems[slot] == index &&
                    game.StoredItems.Length == 0 && game.GetShowcaseQualityRank(slot) == "D", "Paid start must reserve the exact item with D directly on its room showcase");
                var saved = ProgressSaveStore.Load(path, 3);
                Check(saved.ShowcaseItems[slot] == index && saved.ShowcaseGrades[slot] == "D" && saved.StoredItems.Length == 0,
                    "The paid item must exist on disk before playing completes");
                Check(!game.Crafting.TryStart(index), "Duplicate start must be rejected");
                game.Crafting.Complete("S");
                Check(game.GetShowcaseQualityRank(slot) == "D", "An unfinished game cannot change its saved D");
                double day = game.DayRemainingSeconds;
                mini._Process(0.25);
                Check(Math.Abs(game.DayRemainingSeconds - day + 0.25) < 0.001, "Active craft must advance day time");
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                pause.Open();
                double craftSeconds = mini.ActiveSession.RemainingSeconds, pausedDay = game.DayRemainingSeconds;
                mini._Process(1);
                Check(mini.ActiveSession.RemainingSeconds == craftSeconds && game.DayRemainingSeconds == pausedDay, "Pause must suspend both clocks");
                pause.Close();
                if (item.WorkshopId == "atelier")
                {
                    patterns.Add(string.Join(";", mini.Session.Points.Select(point => $"{point.X},{point.Y}")));
                    var surface = mini.GetNode<EmbroiderySurface>("Surface");
                    for (int point = 0; point < mini.Session.Points.Count; point++) PushEmbroideryClick(surface.ScreenPoint(point));
                    mini._Process(0);
                    Check(mini.Session.Complete, "Every selected garment pattern must be playable through actual clicks");
                }
                else
                {
                    var board = mini.GetNode<PolishingSurface>("Surface/Polishing");
                    Check(board.InstrumentKind == item.Kind, "Polishing must use the selected instrument's mask");
                    var target = mini.Polishing.Cells.First(cell => cell.Inside && mini.Polishing.Inside(cell.X + 14, cell.Y));
                    Vector2 left = board.LogicalToScreen(new Vector2((float)target.X, (float)target.Y));
                    Vector2 right = board.LogicalToScreen(new Vector2((float)target.X + 14, (float)target.Y));
                    Root.PushInput(new InputEventMouseMotion { Position = left, GlobalPosition = left }, true);
                    Root.PushInput(new InputEventMouseButton { Position = left, GlobalPosition = left, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
                    for (int stroke = 0; stroke < 30; stroke++)
                    {
                        Vector2 point = stroke % 2 == 0 ? right : left;
                        Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point, ButtonMask = MouseButtonMask.Left }, true);
                    }
                    Root.PushInput(new InputEventMouseButton { Position = left, GlobalPosition = left, ButtonIndex = MouseButton.Left, Pressed = false }, true);
                    Check(mini.Polishing.Progress > 0, "The selected instrument must respond to actual rubbing");
                    mini.FinishForTesting(true);
                }
                Check(game.GetShowcaseQualityRank(slot) == "S" && game.ShowcaseItems.Count(value => value >= 0) == 1,
                    "Finishing must upgrade the same instance without issuing a second item");
                mini.Activate();
                Check(!Paused && !mini.IsOpen, "Collecting must return to gameplay");
            }
            Check(patterns.Count == 5, "Five distinct garment patterns must remain");
            Fresh();
            await Craft("hat"); await Craft("vest"); await Craft("gloves");
            int fullFunds = game.Score;
            game.Crafting.OpenCatalog("atelier");
            Check(game.Crafting.IsCatalogOpen && !game.Crafting.Select("lorgnette") && game.Score == fullFunds,
                "A full room must disable selection without charging");
            game.Crafting.Catalog.Close();
            game._Process(0);
            Check(!game.TradeActive, "Displaying items must wait for the player's manual selection");
            game.OnShowcasePressed(1);
            Check(game.TradeActive && game.ItemIndex == WorkshopItemCatalog.IndexOf("vest"), "The clicked display must determine the offered item");
            game.RerollCustomer();
            int percent = game.Percentage, rerolls = game.RerollsRemaining;
            await ClickControl(game, "World/ShowroomLeft/Showcases/Slot2");
            Check(!game.TradeActive && game.ShowcaseItems[1] == WorkshopItemCatalog.IndexOf("vest"), "Returning must preserve the occupied slot");
            game.PersistProgress(); await Reload();
            game.OnShowcasePressed(1);
            Check(game.TradeActive && game.Percentage == percent && game.RerollsRemaining == rerolls,
                "Returning, saving and selecting again must preserve the offer and remaining rerolls");
            int payout = ChikagoShop.Domain.Trading.BuyerPatienceRules.TotalPayout(game.OfferedPrice, game.BuyerWaitRemaining,
                TradeScene.SpeedBonus(game.BuyerWaitRemaining, ProgressSaveStore.Load(path, 3).BuyerWaitInitial ?? TradeScene.BuyerWaitDuration),
                TradeScene.QualityBonus(game.ActiveTradeQualityRank)), cash = game.Score;
            game.AcceptTrade();
            Check(game.Score == cash + payout && game.ShowcaseItems[1] < 0, "Sale must pay once and release its exact showcase");
            game.AcceptTrade(); Check(game.Score == cash + payout, "Duplicate accept must not pay again");

            await Reload(new ProgressData { SaveVersion = 19, Score = 220, DayStarted = true, TradeActive = true,
                ItemIndex = 5, CustomerIndex = 0, ActiveShowcaseIndex = 0, RerollsRemaining = 1, ActiveTradeGrade = "S",
                ShowcaseItems = new[] { 5, -1, -1 }, ShowcaseGrades = new[] { "B" } });
            game.PersistProgress();
            var occupied = ProgressSaveStore.Load(path, 3);
            Check(occupied.ShowcaseItems[0] == 5 && occupied.ShowcaseGrades[0] == "B" && occupied.ActiveShowcaseIndex == 1 &&
                occupied.ShowcaseItems[1] == 5 && occupied.ShowcaseGrades[1] == "S",
                "A v19 active trade and a distinct displayed copy of the same kind must retain both qualities");
            await Reload();
            Check(game.TradeActive && game.ShowcaseItems.Count(value => value == 5) == 2,
                "A v20 reload must recognize the already reserved copy without creating a third one");

            // Older inventory becomes category-matched displays; overflow and grades must survive.
            await Reload(new ProgressData { SaveVersion = 19, Day = 2, DayStarted = true, Score = 1000,
                StoredItems = new[] { 0, 1, 4, 5, 6, 2 }, StoredGrades = new[] { "S", "A", "B", "C", "D", "S" },
                CheckpointDay = 1, CheckpointItems = new[] { 0, 2 }, CheckpointGrades = new[] { "A", "S" } });
            Check(game.ShowcaseItems.Take(3).SequenceEqual(new[] { 0, 1, 4 }) && game.DeferredItems.Length == 3 && game.StoredItems.Length == 0,
                "Legacy overflow and locked music stock must be retained outside the retired inventory");
            Check(game.GetShowcaseQualityRank(0) == "S" && game.DeferredItems.Single(stock => stock.Item == 2).Grade == "S",
                "Legacy per-instance quality must survive migration");
            game.PersistProgress(); await Reload();
            Check(game.DeferredItems.Length == 3 && game.ShowcaseItems.Count(value => value >= 0) == 3, "Migrated stock must not duplicate on reload");
            game._Process(0); game.OnShowcasePressed(0); game.AcceptTrade();
            Check(game.ShowcaseItems[0] == 5 && game.GetShowcaseQualityRank(0) == "C" && game.DeferredItems.Length == 2,
                "Selling must fill the freed display from deferred stock with its original grade");
            UnlockMusicForFixture(game);
            Check(game.ShowcaseItems[3] == 2 && game.GetShowcaseQualityRank(3) == "S", "Opening music must place deferred instruments on music displays");
            game.ReturnToCheckpoint();
            Check(game.ShowcaseItems.Contains(0) && game.GetShowcaseQualityRank(0) == "A", "Checkpoint must restore its own stock and quality");

            Fresh(); await Select("hat"); Mini("atelier").Activate(); await Reload();
            Check(!game.CraftMiniGameOpen && game.ShowcaseItems[0] == 0 && game.GetShowcaseQualityRank(0) == "D", "Reload after paid start must retain D without resuming the game");
            Fresh(); game.Score = 0; game.Crafting.OpenCatalog("atelier");
            Check(!game.Crafting.Select("hat") && game.ShowcaseItems.All(value => value < 0), "Insufficient cash must remain a gate");
            game.Crafting.Catalog.Close();
            Fresh(); await Select("hat"); game.Score = 0; Mini("atelier").Activate();
            Check(Mini("atelier").ActiveSession.State == EmbroideryState.Ready, "Paid start must revalidate funds");
            Mini("atelier").Close(); game.Crafting.Catalog.Close();
            Fresh(); await Select("hat"); Mini("atelier").Activate();
            Mini("atelier")._Process(game.DayRemainingSeconds + 1);
            Check(game.DayClosed && Mini("atelier").IsOpen && game.ShowcaseItems[0] == 0,
                "Expiry must retain the paid showcase item until its result is collected");
            Mini("atelier").Activate();
            game._Process(0);
            Check(game.CurrentDay == 2 && game.DayUpgrades.Pending, "Collecting the expired attempt must advance exactly one day");
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            await WaitForDayCardAnimation(game);
            Check(!game.DayStarted && game.ShowcaseItems[0] == 0, "Unsold output must survive the next waiting day");
            GD.Print("Merged crafting: all ten real selections, five patterns/masks, exact paid displays, manual sales, overflow and reload passed");
        }
        finally
        {
            Paused = false;
            game?.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(locale);
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
        }
        await Frame();
    }
}
