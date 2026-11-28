using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Configuration;
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
            game.Crafting.Catalog.SetProcess(false);
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
            int funds = game.Score;
            game.Crafting.OpenCatalog(room);
            Check(game.Crafting.IsCatalogOpen && Paused && game.Score == funds,
                "Opening the catalogue must not charge the shared craft cost");
            var button = game.Crafting.Catalog.ItemButton(kind);
            Check(!button.HasNode("Content/Choose"), "The item card itself must replace the lower Choose button");
            var scroll = game.Crafting.Catalog.GetNode<ScrollContainer>("Root/Center/Panel/Margin/Content/Scroll");
            scroll.EnsureControlVisible(button);
            await Frame();
            if (kind == "hat" || kind == "guitar")
            {
                var panel = game.Crafting.Catalog.GetNode<Control>("Root/Center/Panel");
                var content = panel.GetNode<Control>("Margin/Content");
                Rect2 paper = ScreenBounds(panel), inner = ScreenBounds(content), viewport = Root.GetVisibleRect();
                Check(viewport.Encloses(paper) && inner.Position.X - paper.Position.X >= 47 &&
                    paper.End.X - inner.End.X >= 47,
                    "The catalogue must fit the viewport with larger equal paper margins");
                var focus = (StyleBoxFlat)button.GetThemeStylebox("focus");
                var normal = (StyleBoxFlat)button.GetThemeStylebox("normal");
                var hover = (StyleBoxFlat)button.GetThemeStylebox("hover");
                var pressed = (StyleBoxFlat)button.GetThemeStylebox("pressed");
                Check(button.HasFocus() && !button.HasFocus(ignoreHiddenFocus: true) && normal.BgColor == Colors.White &&
                    focus.BorderWidthLeft == 0 && focus.BorderWidthRight == 0 &&
                    focus.BorderWidthTop == 0 && focus.BorderWidthBottom == 0 &&
                    focus.ExpandMarginLeft < -normal.BorderWidthLeft &&
                    focus.ExpandMarginRight < -normal.BorderWidthRight &&
                    focus.ExpandMarginTop < -normal.BorderWidthTop &&
                    focus.ExpandMarginBottom < -normal.BorderWidthBottom &&
                    normal.BorderColor == hover.BorderColor && normal.BorderColor == pressed.BorderColor,
                    "The first card must open with hidden focus, a white fill and the same thin border");
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
            Check(DebugBalanceSettings.Cards.SequenceEqual(DayUpgradeRules.Available) && DebugBalanceSettings.Cards.Length == 9,
                "F10 must expose all nine active cards");
            game.Crafting.OpenCatalog("music");
            Check(!game.Crafting.IsCatalogOpen, "A locked room must still reject its catalogue");
            var debug = Root.GetNode<DebugBalancePanel>("DebugBalance");
            Root.PushInput(new InputEventKey { Keycode = Key.F10, Pressed = true }, true);
            Root.PushInput(new InputEventKey { Keycode = Key.F10, Pressed = false }, true);
            await Frame();
            Check(debug.IsOpen, "F10 must open the balance panel");
            var tabs = debug.GetNode<TabContainer>("Root/Inset/Panel/Content/Tabs");
            Check(!tabs.HasNode("Редкости"), "The retired rarity settings must not remain as an empty tab");
            for (int tab = 0; tab < tabs.GetTabCount(); tab++) { tabs.CurrentTab = tab; await Frame(); }
            Check(!DebugBalanceSettings.Fields.Any(field => field.Key is "craft.atelier.cost" or "craft.music.cost") &&
                debug.Editor(DebugBalanceSettings.CraftCostKey).Text == "20",
                "Both workshops must share one editable craft cost");
            Check(debug.Editor(DebugBalanceSettings.FirstWeekDurationKey).Text == "45" &&
                debug.Editor(DebugBalanceSettings.SecondWeekDurationKey).Text == "60",
                "F10 must expose both week durations independently");
            Check(debug.Editor("buyer.wait.min").Text == "15" && debug.Editor("buyer.wait.max").Text == "20",
                "F10 must expose the shipped shared buyer range");
            Check(WorkshopItemCatalog.Items.Count == 10 && WorkshopItemCatalog.Items.All(item => item.BasePrice == 50) &&
                debug.Editor(DebugBalanceSettings.BasePriceKey).Text == "50" &&
                !DebugBalanceSettings.Fields.Any(field => field.Key.StartsWith("item.") && field.Key.EndsWith(".price")),
                "All ten catalogue and F10 base prices must be 50");
            Check(!DebugBalanceSettings.Fields.Any(field => field.Key == "items.rerolls") &&
                WorkshopItemCatalog.Items.All(item => item.Rerolls == 3) &&
                !DebugBalanceSettings.Fields.Any(field => field.Key.StartsWith("item.") && field.Key.EndsWith(".rerolls")),
                "All ten items must use exactly three rerolls");
            foreach (var upgrade in DayUpgradeRules.Available)
            {
                Check(debug.Editor($"card.{upgrade}.amount") != null &&
                    DebugBalanceSettings.Fields.Any(field => field.Key == $"card.{upgrade}.cost") &&
                    !DebugBalanceSettings.Fields.Any(field => field.Key == $"card.{upgrade}.limit"),
                    $"F10 must expose the effect and applicable price for {upgrade}");
            }
            debug.Close();
            string previousProfilePath = DebugBalanceSettings.StoragePath;
            const string profilePath = "user://merged_wait_profile.json";
            try
            {
                DebugBalanceSettings.StoragePath = profilePath;
                var profile = new ChikagoShop.Domain.Configuration.BalanceProfile
                {
                    FirstQuotaBaselineRevision = 1, EconomyBaselineRevision = 1, ItemCatalogueRevision = 1
                };
                profile.Numbers["item.hat.price"] = 70;
                profile.Numbers["item.guitar.price"] = 120;
                profile.Numbers["item.cello.price"] = 250;
                profile.Numbers["buyer.min"] = 9;
                profile.Numbers["buyer.music.max"] = 40;
                profile.Numbers["item.hat.rerolls"] = 1;
                profile.Numbers["rarity.unique.rerolls"] = 1;
                profile.Numbers["craft.atelier.seconds"] = 12;
                profile.Numbers["craft.hat.cost"] = 75;
                profile.Numbers["polish.violin.strength"] = 0.8;
                profile.Numbers["polish.violin.radius"] = 0.91;
                profile.Numbers["card.Music.weight"] = 200;
                profile.Numbers["card.Atelier.cost"] = 25;
                using (var file = FileAccess.Open(profilePath, FileAccess.ModeFlags.Write))
                    file.StoreString(profile.ToJson());
                Check(DebugBalanceSettings.Reload(out string error), $"Legacy F10 profile must reload: {error}");
                var migrated = ChikagoShop.Domain.Configuration.BalanceProfile.FromJson(FileAccess.GetFileAsString(profilePath));
                Check(migrated.Numbers.Count == 3 && migrated.PolishingStrengthRevision == 1 &&
                    migrated.Numbers["polish.violin.strength"] == 1.0 &&
                    migrated.Numbers["polish.violin.radius"] == 0.91 &&
                    DebugBalanceSettings.Reload(out _) && DebugBalanceSettings.Number("polish.violin.strength") == 1.0 &&
                    migrated.Numbers["craft.atelier.seconds"] == 12 &&
                    WorkshopItemCatalog.Items.All(item => item.BasePrice == 50 && item.Rerolls == 3) &&
                    DebugBalanceSettings.Economy.BuyerWaitMinSeconds == 15 &&
                    DebugBalanceSettings.Economy.BuyerWaitMaxSeconds == 20,
                    "Migration must remove old prices and ranges on disk while preserving other valid overrides");
                profile = DebugBalanceSettings.Profile;
                profile.Numbers[DebugBalanceSettings.BasePriceKey] = 62;
                profile.Numbers["items.rerolls"] = 2;
                profile.Numbers[DebugBalanceSettings.FirstWeekDurationKey] = 40;
                profile.Numbers[DebugBalanceSettings.SecondWeekDurationKey] = 65;
                profile.Numbers["buyer.wait.min"] = 24;
                profile.Numbers["buyer.wait.max"] = 24;
                Check(DebugBalanceSettings.TrySave(profile, out error), $"Valid F10 range must save: {error}");
                Check(WorkshopItemCatalog.Items.All(item => item.BasePrice == 62 && item.Rerolls == 3),
                    "F10 must apply shared values to all ten items");
                Check(!DebugBalanceSettings.Profile.Numbers.ContainsKey("craft.hat.cost"),
                    "An old F10 price for crafting must be discarded");
                Fresh();
                game._Process(0);
                Check(game.BuyerWaitInitial == 24 && game.DayRemainingSeconds == 40,
                    "New customers and the first week must use their F10 settings");
                game.PersistProgress();
                profile.Numbers["buyer.wait.min"] = 28;
                profile.Numbers["buyer.wait.max"] = 26;
                Check(!DebugBalanceSettings.TrySave(profile, out _), "F10 must reject a reversed range");
                profile.Numbers["buyer.wait.max"] = 28;
                Check(DebugBalanceSettings.TrySave(profile, out error), $"Changed F10 range must save: {error}");
                await Reload();
                Check(game.BuyerWaitInitial == 24 && game.BuyerWaitRemaining == 24,
                    "An existing customer must retain both saved wait values after editing F10");
                Fresh();
                game._Process(0);
                Check(game.BuyerWaitInitial == 28, "Only new customers acquire the changed F10 duration");
                await Reload(new ProgressData { SaveVersion = ProgressJson.CurrentVersion, Day = 9,
                    QuotaStage = 1, QuotaVisit = QuotaVisitStage.Rewarded, DayStarted = false, Score = 100 });
                Check(game.DayRemainingSeconds == 65,
                    "A second-week save must use that week's independent F10 duration, including while music is locked");
            }
            finally
            {
                DebugBalanceSettings.StoragePath = previousProfilePath;
                DebugBalanceSettings.Reload(out _);
                game.RefreshDebugBalance();
                if (FileAccess.FileExists(profilePath)) DirAccess.RemoveAbsolute(profilePath);
            }
            var patterns = new HashSet<string>();
            foreach (var item in WorkshopItemCatalog.Items)
            {
                Fresh(item.WorkshopId == "music");
                Root.Size = item.WorkshopId == "music" ? new Vector2I(960, 540) : new Vector2I(1280, 720);
                TranslationServer.SetLocale(item.WorkshopId == "music" ? "en" : "ru");
                bool music = item.WorkshopId == "music";
                int index = WorkshopItemCatalog.IndexOf(item.Kind), slot = music ? 3 : 0;
                if (item.Kind is "hat" or "guitar")
                {
                    string room = music ? "MusicWorkshop" : "Atelier";
                    var button = game.GetNode<Button>($"World/{room}/GetItemButton");
                    var price = button.GetNode<Label>("Price");
                    Check(price.Text == (music ? "$20" : "20$") && new Rect2(Vector2.Zero, button.Size).Encloses(price.GetRect()),
                        "The fixed price must be visible inside the localized workshop button");
                    var camera = game.GetNode<WorldCamera>("Camera2D");
                    camera.FocusOn(button.GetGlobalRect().GetCenter(), new Vector2(1500, 844), 0);
                    camera.ForceUpdateScroll();
                    var curtain = game.GetNodeOrNull<RoomCurtainPresentation>($"World/{room}/LockedOverlay");
                    ulong deadline = Time.GetTicksMsec() + 3000;
                    while (curtain?.IsRevealing == true)
                    {
                        Check(Time.GetTicksMsec() < deadline, "The music workshop must finish opening");
                        await Frame();
                    }
                    Check(price.IsVisibleInTree() && Root.GetVisibleRect().Encloses(ScreenBounds(price)),
                        "The price must fit in the visible scene");
                    using (await CapturePresentation("craft-button-price-" + item.Kind)) { }
                }
                await Select(item.Kind);
                Check(!game.Crafting.Catalog.ItemButton(item.Kind).GetNode<Label>("Content/Rarity").Visible,
                    "The catalogue must not show retired rarity metadata");
                var mini = Mini(item.WorkshopId);
                int funds = game.Score;
                mini.Close();
                Check(game.Crafting.IsCatalogOpen && game.Score == funds && game.ShowcaseItems.All(value => value < 0),
                    "Preparation cancellation must not pay or reserve a showcase");
                game.Crafting.Catalog.Close();
                Check(game.Score == funds,
                    "Leaving the catalogue must leave the balance unchanged");
                await Select(item.Kind);
                Check(ProgressSaveStore.Load(path, 3).Score == funds,
                    "Reload must preserve the balance from before preparation");
                mini.Activate();
                Check(game.Score == funds - 20 && TradeScene.GetItemBasePrice(index) == 50 &&
                    game.ShowcaseItems[slot] == index &&
                    game.StoredItems.Length == 0 && game.GetShowcaseQualityRank(slot) == "D", "Paid start must reserve the exact item with D directly on its room showcase");
                var saved = ProgressSaveStore.Load(path, 3);
                Check(saved.ShowcaseItems[slot] == index && saved.ShowcaseGrades[slot] == "D" &&
                    saved.ShowcaseRerolls[slot] == 3 && saved.StoredItems.Length == 0,
                    "The reserved item must exist on disk before playing completes");
                Check(!game.Crafting.TryStart(index), "Duplicate start must be rejected");
                game.Crafting.Complete("S");
                Check(game.GetShowcaseQualityRank(slot) == "D", "An unfinished game cannot change its saved D");
                double day = game.DayRemainingSeconds;
                game.Crafting.Catalog._Process(0.25);
                mini._Process(0.25);
                Check(Math.Abs(game.DayRemainingSeconds - day + 0.25) < 0.001, "Active craft must advance day time");
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                pause.Open();
                double craftSeconds = mini.ActiveSession.RemainingSeconds, pausedDay = game.DayRemainingSeconds;
                game.Crafting.Catalog._Process(1);
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
                double beforeResult = game.DayRemainingSeconds, beforeBuyer = game.BuyerWaitRemaining;
                game.Crafting.Catalog._Process(0.25);
                Check(game.DayRemainingSeconds == beforeResult - 0.25 &&
                    game.BuyerWaitRemaining == Math.Max(0, beforeBuyer - 0.25),
                    "The result must tick both shop clocks");
                Check(game.GetShowcaseQualityRank(slot) == "S" && game.ShowcaseItems.Count(value => value >= 0) == 1,
                    "Finishing must upgrade the same instance without issuing a second item");
                mini.Activate();
                Check(!Paused && !mini.IsOpen, "Collecting must return to gameplay");
            }
            Check(patterns.Count == 5, "Five distinct garment patterns must remain");

            foreach (string kind in new[] { "hat", "guitar" })
            {
                bool music = kind == "guitar";
                if (music)
                    await Reload(new ProgressData { SaveVersion = ProgressJson.CurrentVersion, Day = 8,
                        QuotaStage = 1, QuotaVisit = QuotaVisitStage.Rewarded, DayStarted = true,
                        DayRemainingSeconds = 60, MusicWorkshopUnlocked = true, Score = 500 });
                else Fresh();
                game._Process(0);
                await Reload(ProgressSaveStore.Load(path, 3) with
                {
                    WaitingBuyerItemKind = music ? "drum" : "vest"
                });
                int cashBeforeCraft = game.Score;
                double dayStart = game.DayRemainingSeconds, buyerStart = game.BuyerWaitRemaining;
                Check(buyerStart > 6, "A live buyer must be present before crafting");
                var catalog = game.Crafting.Catalog;
                game.Crafting.OpenCatalog(music ? "music" : "atelier");
                var mini = Mini(music ? "music" : "atelier");
                void Tick(double elapsed)
                {
                    game._Process(elapsed);
                    catalog._Process(elapsed);
                    mini._Process(elapsed);
                }
                void ExpectClocks(double elapsed) => Check(
                    Math.Abs(game.DayRemainingSeconds - (dayStart - elapsed)) < 0.001 &&
                    Math.Abs(game.BuyerWaitRemaining - (buyerStart - elapsed)) < 0.001,
                    $"Both shop clocks must tick exactly once through each craft phase ({kind}: " +
                    $"day {game.DayRemainingSeconds}/{dayStart - elapsed}, buyer {game.BuyerWaitRemaining}/{buyerStart - elapsed})");

                Tick(1);
                ExpectClocks(1);
                var buyerBadge = catalog.GetNode<Label>("Root/Center/Panel/Margin/Content/BuyerTime");
                Color badgeInk = buyerBadge.GetThemeColor("font_color");
                Check(buyerBadge.Text == GameLocalization.Format("Клиент: {0}", game.BuyerClockText) &&
                    badgeInk.R > badgeInk.G + 0.4f && buyerBadge.GetThemeFontSize("font_size") >= 24,
                    "The catalogue must highlight the running buyer timer in red");
                foreach (string language in new[] { "ru", "en" })
                {
                    TranslationServer.SetLocale(language);
                    foreach (var item in WorkshopItemCatalog.Items.Where(item => item.WorkshopId == (music ? "music" : "atelier")))
                    {
                        var card = catalog.ItemButton(item.Kind);
                        Check(!card.HasNode("Content/Price") && !card.HasNode("Content/Choose") &&
                            card.Text.Length == 0 && card.GetNode<Label>("Content/Name").Text == GameLocalization.Text(item.Name),
                            "Every localized catalogue card must consist of its illustration and title without a price");
                    }
                }
                Check(game.Crafting.Select(kind), "Live craft must select an item");
                Tick(1);
                ExpectClocks(2);
                var buyerLabel = mini.GetNode<Label>("Surface/Panel/StatTime/Value");
                Color clockInk = buyerLabel.GetThemeColor("font_color");
                Check(buyerLabel.Text == game.BuyerClockText && clockInk.R > clockInk.G + 0.4f &&
                    buyerLabel.GetThemeFontSize("font_size") >= 28,
                    "Preparation must show a prominent red buyer clock");
                var surface = mini.GetNode<EmbroiderySurface>("Surface");
                float left = surface.FabricRect.Position.X - surface.PanelRect.Position.X - 7;
                float right = surface.PanelRect.End.X - surface.QualityTrackRect.End.X;
                Check(left >= 38 && Mathf.IsEqualApprox(left, right) &&
                    Mathf.IsEqualApprox(surface.PanelRect.Position.X, surface.Size.X - surface.PanelRect.End.X),
                    "Crafting must have equal, generous left and right margins around the paper and its content");
                mini.Activate();
                double miniStart = mini.ActiveSession.RemainingSeconds;
                await PressEscapeForTest();
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                Check(pause.IsOpen, "ESC must open true pause over crafting");
                Tick(2);
                ExpectClocks(2);
                Check(mini.ActiveSession.RemainingSeconds == miniStart, "ESC must stop the mini-game clock too");
                pause.Close();
                Tick(1);
                ExpectClocks(3);
                Check(buyerLabel.Text == game.BuyerClockText && buyerLabel.Text != miniStart.ToString(),
                    "Playing must show the live buyer timer while the internal clock remains separate");
                Check(mini.ActiveSession.RemainingSeconds == miniStart - 1, "The mini-game clock must tick once");
                if (!music)
                {
                    foreach (var point in mini.Session.Points.Take(mini.Session.Points.Count - 1))
                        mini.Session.Hit(point.X, point.Y);
                }
                string expectedRank = music ? "D" : "B";
                var collect = mini.GetNode<Button>("Surface/Panel/Buttons/Action");
                Check(collect.IsVisibleInTree() && collect.Text == GameLocalization.Text("Забрать"),
                    "Collect must be available during play");
                collect.EmitSignal(Button.SignalName.Pressed);
                int itemIndex = WorkshopItemCatalog.IndexOf(kind), slot = music ? 3 : 0;
                int cashAfterCraft = game.Score;
                Check(cashAfterCraft == cashBeforeCraft - WorkshopItemCatalog.CraftCost(music ? "music" : "atelier"),
                    "Early collection must keep the one crafting debit");
                Check(!mini.IsOpen && !Paused && game.Score == cashAfterCraft &&
                    game.ShowcaseItems.Count(item => item == itemIndex) == 1 && game.GetShowcaseQualityRank(slot) == expectedRank,
                    "Early collection must keep one item with its earned rank");
                collect.EmitSignal(Button.SignalName.Pressed);
                game.PersistProgress();
                await Reload();
                ExpectClocks(3);
                Check(game.Score == cashAfterCraft && game.ShowcaseItems.Count(item => item == itemIndex) == 1 &&
                    game.GetShowcaseQualityRank(slot) == expectedRank, "Early collection and clocks must survive reload");
            }

            Fresh();
            game._Process(0);
            await Select("hat");
            var expiryMini = Mini("atelier");
            expiryMini.Activate();
            expiryMini.FinishForTesting(true);
            double dayBeforeExpiry = game.DayRemainingSeconds;
            game.Crafting.Catalog.SetProcess(true);
            await Frame();
            game.Crafting.Catalog.SetProcess(false);
            Check(game.DayRemainingSeconds < dayBeforeExpiry, "The real scene tree must tick while the result is open");
            game.Crafting.Catalog._Process(game.BuyerWaitRemaining);
            Check(game.BuyerWaitRemaining == 0 && expiryMini.IsOpen &&
                game.GetNode<Node2D>("World/TradingRoom/Customer").Visible,
                "Patience must reach zero in the result screen without dismissing the buyer");
            expiryMini.Activate();

            Fresh();
            await Reload(ProgressSaveStore.Load(path, 3) with { WaitingBuyerItemKind = "vest" });
            await Craft("hat");
            Check(!game.TradeActive && game.ShowcaseItems.Contains(WorkshopItemCatalog.IndexOf("hat")),
                "Collecting the wrong item must keep it on a showcase");
            double initialWait = game.BuyerWaitRemaining;
            await Craft("vest");
            var autoSale = ProgressSaveStore.Load(path, 3);
            var counter = game.GetNode<Node2D>("World/TradingRoom/Item");
            var displays = game.GetNode("World/ShowroomLeft/Showcases").GetChildren().OfType<Button>().ToArray();
            int nearest = Enumerable.Range(1, displays.Length - 1).MinBy(index =>
                displays[index].GetGlobalRect().GetCenter().DistanceSquaredTo(counter.GlobalPosition));
            Check(game.TradeActive && game.ItemIndex == WorkshopItemCatalog.IndexOf("vest") &&
                game.BuyerWaitRemaining == initialWait && autoSale.ActiveShowcaseIndex == nearest &&
                displays[nearest].GetNode<TextureRect>("TradeShadow").IsVisibleInTree(),
                "Collecting the exact item must open a sale and leave a shadow on the closest available display");
            await Reload();
            Check(game.TradeActive && game.ItemIndex == WorkshopItemCatalog.IndexOf("vest") &&
                ProgressSaveStore.Load(path, 3).ActiveShowcaseIndex == nearest,
                "Reload must retain the automatic offer and reserved display");

            Fresh();
            await Reload(ProgressSaveStore.Load(path, 3) with { WaitingBuyerItemKind = "lorgnette" });
            await Craft("hat"); await Craft("vest"); await Craft("gloves");
            int fullFunds = game.Score;
            game.Crafting.OpenCatalog("atelier");
            Check(!game.Crafting.IsCatalogOpen && game.Score == fullFunds,
                "A full room must reject opening the catalogue before charging");
            await Reload(ProgressSaveStore.Load(path, 3) with { WaitingBuyerItemKind = "vest" });
            game._Process(0);
            Check(!game.TradeActive, "Displaying items must wait for the player's manual selection");
            game.OnShowcasePressed(1);
            Check(game.TradeActive && game.ItemIndex == WorkshopItemCatalog.IndexOf("vest"), "The clicked display must determine the offered item");
            double patienceBeforeReroll = game.BuyerWaitRemaining;
            Check(game.RerollsRemaining == 3, "A newly crafted vest must open its first sale with three rerolls");
            game.RerollCustomer();
            Check(game.RerollsRemaining == 2 && game.BuyerWaitRemaining == patienceBeforeReroll,
                "A reroll consumes one attempt and leaves the patience timer unchanged");
            int percent = game.Percentage, rerolls = game.RerollsRemaining;
            await ClickControl(game, "World/ShowroomLeft/Showcases/Slot2");
            Check(!game.TradeActive && game.ShowcaseItems[1] == WorkshopItemCatalog.IndexOf("vest"), "Returning must preserve the occupied slot");
            game.PersistProgress(); await Reload();
            game.OnShowcasePressed(1);
            Check(game.TradeActive && game.Percentage == percent && game.RerollsRemaining == rerolls,
                "Returning, saving and selecting again must preserve the offer and remaining rerolls");
            game.RerollCustomer();
            Check(game.RerollsRemaining == 1, "The second reroll must consume precisely one attempt");
            game.RerollCustomer();
            int lastOffer = game.OfferedPrice;
            game.RerollCustomer();
            Check(game.RerollsRemaining == 0 && game.OfferedPrice == lastOffer &&
                game.BuyerWaitRemaining == patienceBeforeReroll, "An exhausted item must reject another reroll without resetting patience");
            game.PersistProgress(); await Reload();
            Check(game.TradeActive && game.RerollsRemaining == 0 && game.BuyerWaitRemaining == patienceBeforeReroll,
                "A saved exhausted item must stay exhausted across reload");
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
                WaitingBuyerPresent = true, WaitingBuyerItemKind = "hat", CustomerIndex = 0,
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
            Check(!game.CraftMiniGameOpen && game.ShowcaseItems[0] == 0 && game.GetShowcaseQualityRank(0) == "D", "Reload after starting must retain D without resuming the game");
            Fresh(); game.Score = 0; game.Crafting.OpenCatalog("atelier");
            Check(!game.Crafting.Select("hat") && game.ShowcaseItems.All(value => value < 0), "Insufficient cash must remain a gate");
            game.Crafting.Catalog.Close();

            Fresh(); await Select("hat"); Mini("atelier").Activate();
            game.Crafting.Catalog._Process(game.DayRemainingSeconds + 1);
            Check(game.DayClosed && Mini("atelier").IsOpen && game.ShowcaseItems[0] == 0,
                "Expiry must retain the reserved showcase item until its result is collected");
            Mini("atelier")._Process(Mini("atelier").ActiveSession.RemainingSeconds);
            Mini("atelier").Activate();
            game._Process(0);
            Check(game.CurrentDay == 2 && game.DayUpgrades.Pending, "Collecting the expired attempt must advance exactly one day");
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            await WaitForDayCardAnimation(game);
            Check(!game.DayStarted && game.ShowcaseItems[0] == 0, "Unsold output must survive the next waiting day");
            const string priceProfile = "user://workshop_price_test.json";
            string savedProfilePath = DebugBalanceSettings.StoragePath;
            try
            {
                DebugBalanceSettings.StoragePath = priceProfile;
                var profile = new BalanceProfile();
                profile.Numbers["item.hat.price"] = 30;
                profile.Numbers["item.guitar.price"] = 40;
                profile.Numbers["craft.atelier.cost"] = 68;
                profile.Numbers["craft.music.cost"] = 112;
                profile.Numbers[DebugBalanceSettings.BasePriceKey] = 62;
                profile.Numbers[DebugBalanceSettings.CraftCostKey] = 27;
                profile.Numbers["card.Materials.amount"] = 8;
                using (var file = FileAccess.Open(priceProfile, FileAccess.ModeFlags.Write))
                    file.StoreString(profile.ToJson());
                Check(DebugBalanceSettings.Reload(out _), "The old profile with per-item prices must still load");
                foreach (var (kind, workshop, price, upgrade) in new[]
                {
                    ("hat", "atelier", 62, DayUpgrade.AtelierPrice),
                    ("guitar", "music", 62, DayUpgrade.MusicPrice)
                })
                {
                    int index = WorkshopItemCatalog.IndexOf(kind);
                    await Reload(new ProgressData
                    {
                        SaveVersion = ProgressJson.CurrentVersion, Day = 8, QuotaStage = 1, QuotaVisit = QuotaVisitStage.Rewarded,
                        MusicWorkshopUnlocked = true, DayStarted = true, DayRemainingSeconds = 60, Score = 500,
                        TradeActive = true, ItemIndex = index, CustomerIndex = 0, Percentage = 30,
                        BuyerWaitInitial = 20, BuyerWaitRemaining = 15, SelectedUpgrade = (int)upgrade
                    });
                    var baseLabel = game.GetNode<Label>("World/TradingRoom/Item/TradePanel/Content/BasePriceLabel");
                    Check(WorkshopItemCatalog.CraftCost(workshop) == 27 && TradeScene.GetItemBasePrice(index) == price &&
                        baseLabel.Text == GameLocalization.Format("Обычно: {0}$", price),
                        "Shared price and cost must stay separate even with legacy overrides and an active card");
                    Check(game.GetItemPrice(index) == price &&
                        game.OfferedPrice == TradeScene.PriceForPercent(price, 30),
                        "An active card must leave the percentage-adjusted part unchanged");
                    int money = game.Score;
                    int expected = game.OfferedPrice + TradeScene.SpeedBonus(15, 20)
                        + TradeScene.QualityBonus(game.ActiveTradeQualityRank) + game.DayUpgrades.Rules.For(upgrade).Amount;
                    game.AcceptTrade();
                    Check(game.Score == money + expected, "The workshop card must enter the actual sale payout");
                }
                await Reload(new ProgressData {
                    SaveVersion = ProgressJson.CurrentVersion, Day = 2, DayStarted = true, DayRemainingSeconds = 35, Score = 18,
                    SelectedUpgrade = (int)DayUpgrade.Materials });
                game.Crafting.OpenCatalog("atelier");
                Check(!game.Crafting.IsCatalogOpen && game.Score == 18, "Insufficient funds must prevent crafting");
                game.Score = 19;
                game.Crafting.OpenCatalog("atelier");
                Check(game.Crafting.Select("hat") && game.Score == 19, "Preparation with a restored discount is free");
                Mini("atelier").Activate();
                Check(game.Score == 0, "The saved discount and F10 amount must affect the single Start debit");
                Mini("atelier").FinishForTesting(false);
                Mini("atelier").Activate();
                Check(game.Score == 0, "Collection must neither refund nor debit crafting");
            }
            finally
            {
                DebugBalanceSettings.StoragePath = savedProfilePath;
                DebugBalanceSettings.Reload(out _);
                if (FileAccess.FileExists(priceProfile)) DirAccess.RemoveAbsolute(priceProfile);
            }
            GD.Print("Merged crafting: selection, shared workshop prices, five patterns/masks, exact paid displays, manual sales, overflow and reload passed");
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
