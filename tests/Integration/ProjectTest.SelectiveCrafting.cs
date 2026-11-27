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
    private async Task TestSelectiveCrafting()
    {
        const string path = "user://selective_crafting_test.json";
        Vector2I sizeBefore = Root.Size;
        string localeBefore = TranslationServer.GetLocale();
        TradeScene game = null;
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        async Task Reload()
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            game = scene.Instantiate<TradeScene>();
            game.SavePath = path;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
            // Explicit ticks below make clock and timeout assertions independent of rendering speed.
            game.GetNode<EmbroideryGame>("EmbroideryGame").SetProcess(false);
            game.GetNode<EmbroideryGame>("PolishingGame").SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        void Fresh(bool music = false)
        {
            game.NewGame();
            game.EndDay();
            game.Score = 10000;
            if (music) UnlockMusicForFixture(game);
            game.RefreshItemButtons();
        }
        async Task SelectThroughCatalog(string kind)
        {
            int index = WorkshopItemCatalog.IndexOf(kind);
            var item = WorkshopItemCatalog.Items[index];
            game.Crafting.OpenCatalog(item.WorkshopId);
            Check(game.Crafting.IsCatalogOpen && Paused, "Catalog must open as a paused modal");
            var button = game.Crafting.Catalog.ItemButton(kind);
            var scroll = game.Crafting.Catalog.GetNode<ScrollContainer>(
                "Root/Center/Panel/Margin/Content/Scroll");
            await ToSignal(this, SignalName.ProcessFrame);
            scroll.EnsureControlVisible(button);
            await ToSignal(this, SignalName.ProcessFrame);
            await ClickControl(game, game.GetPathTo(button).ToString());
        }
        EmbroideryGame Mini(string workshop) => game.GetNode<EmbroideryGame>(
            workshop == "music" ? "PolishingGame" : "EmbroideryGame");
        string MiniPath(string workshop, string button) =>
            (workshop == "music" ? "PolishingGame" : "EmbroideryGame") + "/Surface/Panel/Buttons/" + button;
        try
        {
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            Root.Size = new Vector2I(1280, 720);
            TranslationServer.SetLocale("ru");
            await Reload();
            Check(WorkshopItemCatalog.Items.Count == 10 &&
                WorkshopItemCatalog.Items.Count(item => item.WorkshopId == "atelier") == 5 &&
                WorkshopItemCatalog.Items.Count(item => item.WorkshopId == "music") == 5,
                "Selection must expose exactly five canonical items per room");
            var prices = new Dictionary<string, int> { ["hat"] = 20, ["vest"] = 30, ["gloves"] = 55,
                ["matteo_hat"] = 80, ["lorgnette"] = 135, ["guitar"] = 30, ["drum"] = 50,
                ["banjo"] = 80, ["violin"] = 125, ["tuba"] = 205 };
            var patterns = new HashSet<string>();
            game.NewGame();
            game.Crafting.OpenCatalog("atelier");
            Check(!game.Crafting.IsCatalogOpen, "A waiting day must reject the catalog");
            game.EndDay();
            game.Crafting.OpenCatalog("music");
            Check(!game.Crafting.IsCatalogOpen && !game.Crafting.Select("guitar"), "The locked workshop must reject music selection");
            foreach (var item in WorkshopItemCatalog.Items)
            {
                Fresh(item.WorkshopId == "music");
                int index = WorkshopItemCatalog.IndexOf(item.Kind);
                Check(WorkshopItemCatalog.ItemCraftCost(index) == prices[item.Kind], "Keep approved per-item costs");
                int funds = game.Score, energy = game.EnergyRemaining;
                double dayTime = game.DayRemainingSeconds;
                await SelectThroughCatalog(item.Kind);
                var mini = Mini(item.WorkshopId);
                Check(mini.IsOpen && mini.PreparedItemIndex == index &&
                    mini.ActiveSession.State == EmbroideryState.Ready && !game.Crafting.IsCatalogOpen && Paused,
                    "Real catalog click must prepare the selected canonical item");
                mini._Process(1);
                Check(game.Score == funds && game.EnergyRemaining == energy && game.StoredItems.Length == 0 &&
                    game.DayRemainingSeconds == dayTime && mini.ActiveSession.RemainingSeconds == 10,
                    "Preparation must freeze the day and leave resources and inventory unchanged");
                if (item.WorkshopId == "atelier")
                {
                    string signature = string.Join(";", mini.Session.Points.Select(p => $"{p.X},{p.Y}"));
                    Check(patterns.Add(signature), "Each garment must have its own fixed stitch geometry");
                }
                else Check(mini.Polishing.Cells.Any(cell => cell.Inside),
                    "Each selected instrument must provide a nonempty polishing mask");

                await ClickControl(game, MiniPath(item.WorkshopId, "Cancel"));
                Check(game.Crafting.IsCatalogOpen && !mini.IsOpen && Paused && game.Score == funds &&
                    game.EnergyRemaining == energy && game.StoredItems.Length == 0,
                    "Back from preparation must return freely to the catalog");
                game.Crafting.Catalog.Close();
                Check(!Paused && !game.Crafting.IsCatalogOpen, "Leaving the catalog must resume the day");
                await SelectThroughCatalog(item.Kind);
                await ClickControl(game, MiniPath(item.WorkshopId, "Action"));
                Check(mini.ActiveSession.State == EmbroideryState.Playing &&
                    game.Score == funds - prices[item.Kind] && game.EnergyRemaining == energy &&
                    game.StoredItems.SequenceEqual(new[] { index }) && game.GetStoredItemQualityRank(0) == "D",
                    "Start must debit once and immediately grant exactly the selected D item");
                game.Crafting.Complete("S");
                Check(game.GetStoredItemQualityRank(0) == "D", "A result cannot be committed before the session finishes");
                var saved = ProgressSaveStore.Load(path, 3);
                Check(saved != null && saved.Score == game.Score && saved.EnergyRemaining == game.EnergyRemaining &&
                    saved.StoredItems.SequenceEqual(new[] { index }), "Paid D item and debit must be saved at start");
                mini.Activate();
                Check(!game.Crafting.TryStart(index) && !game.Crafting.Select("hat") &&
                    game.Score == funds - prices[item.Kind] && game.StoredItems.Length == 1,
                    "Repeated start and selection input must never create or charge another item");
                mini.Close();
                Check(mini.IsOpen, "An active paid mini-game cannot be dismissed normally");
                double playingTime = game.DayRemainingSeconds;
                mini._Process(0.25);
                Check(Math.Abs(game.DayRemainingSeconds - (playingTime - 0.25)) < 0.001,
                    "Only active play must advance the day clock");
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                pause.Open();
                double seconds = mini.ActiveSession.RemainingSeconds;
                double pausedDay = game.DayRemainingSeconds;
                mini._Process(1);
                Check(mini.ActiveSession.RemainingSeconds == seconds && game.DayRemainingSeconds == pausedDay,
                    "Pause must suspend both active craft and day clocks");
                pause.Close();
                if (item.WorkshopId == "atelier")
                {
                    var surface = mini.GetNode<EmbroiderySurface>("Surface");
                    for (int point = 0; point < mini.Session.Points.Count; point++)
                        PushEmbroideryClick(surface.ScreenPoint(point));
                    mini._Process(0);
                    Check(mini.Session.Complete, "Every fixed garment pattern must be completable through actual clicks");
                }
                else
                {
                    var board = mini.GetNode<PolishingSurface>("Surface/Polishing");
                    var target = mini.Polishing.Cells.First(cell => cell.Inside &&
                        mini.Polishing.Inside(cell.X + 14, cell.Y));
                    Vector2 left = board.LogicalToScreen(new Vector2((float)target.X, (float)target.Y));
                    Vector2 right = board.LogicalToScreen(new Vector2((float)target.X + 14, (float)target.Y));
                    Root.PushInput(new InputEventMouseMotion { Position = left, GlobalPosition = left }, true);
                    Root.PushInput(new InputEventMouseButton { Position = left, GlobalPosition = left,
                        ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
                    for (int stroke = 0; stroke < 30; stroke++)
                    {
                        Vector2 point = stroke % 2 == 0 ? right : left;
                        Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                            ButtonMask = MouseButtonMask.Left }, true);
                    }
                    Root.PushInput(new InputEventMouseButton { Position = left, GlobalPosition = left,
                        ButtonIndex = MouseButton.Left, Pressed = false }, true);
                    Check(mini.Polishing.Progress > 0 && mini.Polishing.DirtProgress > 0,
                        "Each selected instrument must respond to actual held mouse strokes on its own mask");
                    mini.FinishForTesting(true);
                }
                Check(game.StoredItems.SequenceEqual(new[] { index }) &&
                    game.GetStoredItemQualityRank(0) == mini.ActiveSession.Rank && mini.ActiveSession.Rank != "D",
                    "Finishing must upgrade the same selected inventory instance without replacing its kind");
                game.Crafting.Complete("D");
                Check(game.GetStoredItemQualityRank(0) == mini.ActiveSession.Rank,
                    "A late duplicate completion must not overwrite the committed quality");
                mini.Activate();
                Check(!mini.IsOpen && !Paused && game.StoredItems.Length == 1,
                    "Collect must close without granting the already-paid item again");
            }

            // Paid attempts are deliberately not restored; the committed D item is.
            Fresh();
            await SelectThroughCatalog("lorgnette");
            await ClickControl(game, MiniPath("atelier", "Action"));
            int paidFunds = game.Score, paidEnergy = game.EnergyRemaining;
            await Reload();
            Check(!game.Crafting.IsCatalogOpen && !Mini("atelier").IsOpen && !Mini("music").IsOpen &&
                game.StoredItems.SequenceEqual(new[] { WorkshopItemCatalog.IndexOf("lorgnette") }) &&
                game.GetStoredItemQualityRank(0) == "D" && game.Score == paidFunds && game.EnergyRemaining == paidEnergy,
                "Reload during a paid attempt must retain its D item and debit with no resumed modal");

            Fresh();
            game.Crafting.OpenCatalog("music");
            Check(!game.Crafting.IsCatalogOpen, "Locked workshop must not expose the music catalog");
            game.Score = 0;
            game.Crafting.OpenCatalog("atelier");
            Check(game.Crafting.Catalog.ItemButton("hat").Disabled && !game.Crafting.Select("hat"),
                "Unaffordable items must be disabled and reject direct selection");
            game.Crafting.Catalog.Close();
            Fresh();
            await SelectThroughCatalog("hat");
            game.Score = 0;
            Mini("atelier").Activate();
            Check(Mini("atelier").ActiveSession.State == EmbroideryState.Ready && game.StoredItems.Length == 0,
                "Start must revalidate funds after selection");
            Mini("atelier").Close();
            game.Crafting.Catalog.Close();

            Fresh();
            game.EnergyRemaining = 0;
            const int attempts = 6;
            int repeatedFunds = game.Score;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                await SelectThroughCatalog("hat");
                Mini("atelier").Activate();
                Mini("atelier")._Process(10);
                Check(Mini("atelier").ActiveSession.State == EmbroideryState.Finished &&
                    game.GetStoredItemQualityRank(attempt) == "D", "An untouched expired pattern must keep the paid D reward");
                Mini("atelier").Activate();
            }
            game.Crafting.OpenCatalog("atelier");
            Check(!game.Crafting.Catalog.ItemButton("hat").Disabled && game.EnergyRemaining == 0 &&
                game.StoredItems.Length == attempts && game.Score == repeatedFunds - attempts * prices["hat"],
                "Six funded attempts must remain available at zero legacy energy and debit only money");
            game.Crafting.Catalog.Close();

            Fresh();
            game._Process(game.DayRemainingSeconds - 0.25);
            await SelectThroughCatalog("hat");
            Mini("atelier").Activate();
            Mini("atelier")._Process(1);
            Check(game.DayClosed && Mini("atelier").IsOpen && game.StoredItems.Length == 1,
                "Day expiry during active play must preserve the paid attempt and defer navigation");
            Mini("atelier").FinishForTesting(true);
            Mini("atelier").Activate();

            foreach (int version in new[] { 13, 18 })
            {
                ProgressSaveStore.Save(path, new ProgressData
                {
                    SaveVersion = version, Score = 500, Day = 2, DayStarted = true,
                    EnergyRemaining = 3, MusicWorkshopUnlocked = true, DayRemainingSeconds = 45,
                    StoredItems = new[] { 0, 4, 9 },
                    StoredGrades = version == 18 ? new[] { "S", "B", "A" } : Array.Empty<string>(),
                    SelectedUpgrade = version == 18 ? (int)DayUpgrade.Music : 0
                });
                await Reload();
                Check(game.StoredItems.SequenceEqual(new[] { 0, 4, 9 }) &&
                    game.GetStoredItemQualityRank(0) == (version == 18 ? "S" : "D") &&
                    game.GetStoredItemQualityRank(1) == (version == 18 ? "B" : "D") &&
                    game.GetStoredItemQualityRank(2) == (version == 18 ? "A" : "D") &&
                    !game.Crafting.IsCatalogOpen && !Mini("atelier").IsOpen && !Mini("music").IsOpen,
                    "Historical saves must retain canonical indices and existing grades without opening a craft");
                if (version == 18) Check(game.Crafting.Duration("music") == 12,
                    "A saved category upgrade must receive its new duration bonus");
                game.PersistProgress();
                await Reload();
                Check(game.StoredItems.SequenceEqual(new[] { 0, 4, 9 }) &&
                    game.GetStoredItemQualityRank(0) == (version == 18 ? "S" : "D"),
                    "Saving a migrated inventory must preserve item identity and quality on another load");
            }
            Fresh();
            await SelectThroughCatalog("hat");
            Mini("atelier").FinishForTesting(true);
            Mini("atelier").Activate();
            await SelectThroughCatalog("hat");
            Mini("atelier").FinishForTesting(false);
            Mini("atelier").Activate();
            Check(game.StoredItems.SequenceEqual(new[] { 0, 0 }) && game.GetStoredItemQualityRank(0) == "S" &&
                game.GetStoredItemQualityRank(1) == "D", "Two identical items must keep separate attempt qualities");
            game.PersistProgress();
            await Reload();
            Check(game.StoredItems.SequenceEqual(new[] { 0, 0 }) && game.GetStoredItemQualityRank(0) == "S" &&
                game.GetStoredItemQualityRank(1) == "D", "Loading identical copies must preserve their distinct qualities");

            foreach (var sample in new[] { (DayUpgrade.Atelier, "atelier"), (DayUpgrade.Music, "music") })
            {
                Fresh(true);
                game.DayUpgrades.Restore(false, sample.Item1, musicUnlocked: true);
                Check(game.Crafting.Duration(sample.Item2) == 12 &&
                    game.Crafting.Duration(sample.Item2 == "atelier" ? "music" : "atelier") == 10,
                    "Restored legacy category identifiers must give only their room twenty percent more time");
            }
            Fresh();
            game.Crafting.OpenCatalog("atelier");
            var catalogPause = game.GetNode<PauseMenu>("PauseMenu");
            catalogPause.Open();
            Check(catalogPause.Layer > game.Crafting.Catalog.Layer, "Pause must cover the catalog");
            game.Crafting.Catalog.Close();
            Check(game.Crafting.IsCatalogOpen && Paused, "Catalog must not close through an active pause menu");
            catalogPause.Close();
            Check(game.Crafting.IsCatalogOpen && Paused, "Resume must restore the catalog pause");
            TranslationServer.SetLocale("en");
            Check(game.Crafting.Catalog.ItemButton("hat").Text == "Choose", "Open catalog must switch to English");
            TranslationServer.SetLocale("ru");
            Check(game.Crafting.Catalog.ItemButton("hat").Text == "Выбрать", "Open catalog must switch back to Russian");
            catalogPause.Open();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/CheckpointButton");
            Check(!game.CraftMiniGameOpen && !game.Crafting.IsCatalogOpen && !Paused,
                "Checkpoint from pause must dismiss the catalog and release its pause");
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string locale in new[] { "ru", "en" })
            foreach (string workshop in new[] { "atelier", "music" })
            {
                Fresh(true);
                Root.Size = size;
                TranslationServer.SetLocale(locale);
                game.Crafting.OpenCatalog(workshop);
                using (var image = await CapturePresentation($"selective-catalog-{workshop}-{locale}-{size.X}")) { }
                var catalogPanel = game.Crafting.Catalog.GetNode<Control>("Root/Center/Panel");
                var catalogCards = game.Crafting.Catalog.GetNode<Control>("Root/Center/Panel/Margin/Content/Scroll/Cards");
                Check(Root.GetVisibleRect().Encloses(ScreenBounds(catalogPanel)) &&
                    ScreenBounds(catalogPanel).Size.Y < Root.GetVisibleRect().Size.Y - 48,
                    "Catalog must be a compact centered window with visible outer margins");
                Check(ScreenBounds(catalogPanel).End.Y - ScreenBounds(catalogCards).End.Y < 140,
                    "Catalog must not leave a large empty area below its item cards");
                float? alignedButtonY = null;
                foreach (Node card in catalogCards.GetChildren())
                {
                    var choose = card.GetNode<Button>("Content/Choose");
                    Check(choose.GetThemeStylebox("normal") is StyleBoxFlat enabled && enabled.BgColor.R < 0.15f &&
                        choose.GetThemeStylebox("disabled") is StyleBoxFlat disabled && disabled.BgColor.R > 0.8f &&
                        choose.GetThemeColor("font_color") == Colors.White,
                        "Enabled choices need dark fill/white text, with clearly distinct disabled styling");
                    Check(!card.GetNode<Label>("Content/Reason").Visible,
                        "Affordable cards must not reserve an empty explanation row");
                    if (catalogCards is GridContainer { Columns: 5 })
                    {
                        var scrollWindow = catalogCards.GetParent<ScrollContainer>();
                        Check(ScreenBounds(scrollWindow).Grow(1).Encloses(ScreenBounds((Control)card)),
                            "A single row of cards must remain fully visible without clipping or scroll offset");
                        float buttonY = ScreenBounds(choose).Position.Y;
                        if (alignedButtonY.HasValue) Check(Math.Abs(buttonY - alignedButtonY.Value) < 1,
                            "All five choice buttons must share the same baseline");
                        alignedButtonY = buttonY;
                    }
                }
                var first = WorkshopItemCatalog.Items.First(item => item.WorkshopId == workshop);
                Check(game.Crafting.Select(first.Kind), "Catalog selection must work after resizing and language change");
                var mini = Mini(workshop);
                mini._Process(0);
                using (var image = await CapturePresentation($"selective-ready-{workshop}-{locale}-{size.X}")) { }
                var surface = mini.GetNode<EmbroiderySurface>("Surface");
                Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(surface.PanelRect),
                    "Both prepared mini-game scenes must fit RU/EN at 960 and 1280 widths");
                foreach (string button in new[] { "Action", "Cancel" })
                    Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(
                        ScreenBounds(mini.GetNode<Control>("Surface/Panel/Buttons/" + button))),
                        "Start and back buttons must remain inside the visible viewport");
                mini.Close();
                game.Crafting.Catalog.Close();
            }
            GD.Print("Selective crafting: all ten GUI selections, five playable patterns, atomic rewards, clocks, reload and responsive scenes passed");
            Fresh();
            game.Score = 0;
            game.Crafting.OpenCatalog("atelier");
            using (var image = await CapturePresentation("selective-catalog-disabled")) { }
            foreach (Node card in game.Crafting.Catalog.GetNode("Root/Center/Panel/Margin/Content/Scroll/Cards").GetChildren())
                Check(card.GetNode<Button>("Content/Choose").Disabled && card.GetNode<Label>("Content/Reason").IsVisibleInTree() &&
                    !string.IsNullOrWhiteSpace(card.GetNode<Label>("Content/Reason").Text),
                    "Unavailable cards must keep a visible explanation with their disabled choice");
            game.Crafting.Catalog.Close();
            string previousProfilePath = DebugBalanceSettings.StoragePath;
            const string profilePath = "user://selective_craft_profile_test.json";
            try
            {
                DebugBalanceSettings.StoragePath = profilePath;
                var profile = new ChikagoShop.Domain.Configuration.BalanceProfile();
                profile.Numbers["craft.hat.cost"] = 21;
                profile.Numbers["workshop.atelier.cost"] = 999;
                profile.Numbers["craft.count"] = 5;
                profile.Numbers["rarity.common.weight"] = 0;
                profile.Numbers["hand.rarity"] = 5;
                Check(DebugBalanceSettings.TrySave(profile, out string profileError),
                    "Per-item cost profiles must save while retiring random-craft settings: " + profileError);
                Check(DebugBalanceSettings.Reload(out profileError) &&
                    WorkshopItemCatalog.ItemCraftCost(WorkshopItemCatalog.IndexOf("hat")) == 21,
                    "Saved per-item costs must survive profile reload: " + profileError);
                Fresh();
                await SelectThroughCatalog("hat");
                int beforeDebit = game.Score;
                Mini("atelier").Activate();
                Check(game.Score == beforeDebit - 21 && game.StoredItems.SequenceEqual(new[] { 0 }),
                    "Live per-item debug cost must affect the actual single-item debit");
                Mini("atelier").FinishForTesting(true);
                Mini("atelier").Activate();
            }
            finally
            {
                DebugBalanceSettings.StoragePath = previousProfilePath;
                DebugBalanceSettings.Reload(out _);
                if (FileAccess.FileExists(profilePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(profilePath));
            }
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            Root.Size = sizeBefore;
            TranslationServer.SetLocale(localeBefore);
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
    }
}
