using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestClosedDayRerolls()
    {
        const string path = "user://closed_reroll_test.json";
        const string actions = "World/TradingRoom/Item/TradePanel/Content/Buttons/";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        string locale = TranslationServer.GetLocale();
        async Task Reload()
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            game = scene.Instantiate<TradeScene>();
            game.SavePath = path;
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            foreach (bool manual in new[] { false, true })
            {
                using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
                    file.StoreString("{\"save_version\":13,\"day\":1,\"score\":220,\"energy_remaining\":3,\"day_remaining_seconds\":90,\"trade_active\":true,\"item_index\":0,\"customer_index\":0,\"rerolls_remaining\":3}");
                await Reload();
                var reroll = game.GetNode<Button>(actions + "DeclineButton");
                Check(!reroll.Disabled, "Reroll must be enabled during an open active trade");
                int initialCustomer = game.CustomerIndex;
                game.RerollCustomer();
                Check(game.RerollsRemaining == 2 && game.CustomerIndex != initialCustomer,
                    "The open-day reroll must still replace a buyer and spend one reroll");
                if (manual) game.EndDay();
                else
                {
                    game._Process(game.DayRemainingSeconds - 0.01);
                    Check(!game.DayClosed && !reroll.Disabled, "Reroll stays available while positive time remains");
                    game._Process(0.02);
                }
                Check(game.DayClosed && game.DayRemainingSeconds == 0 && reroll.Disabled,
                    "Expiry and manual closing must immediately disable reroll");
                int buyer = game.CustomerIndex, offer = game.OfferedPrice, remaining = game.RerollsRemaining;
                int score = game.Score, item = game.ItemIndex;
                string closedSave = FileAccess.GetFileAsString(path);
                game.RerollCustomer();
                game.DeclineTrade();
                game._Input(new InputEventKey { Pressed = true, Keycode = Key.F });
                await ClickControl(game, actions + "DeclineButton");
                Check(game.CustomerIndex == buyer && game.OfferedPrice == offer && game.RerollsRemaining == remaining &&
                    game.TradeActive && game.ItemIndex == item && FileAccess.GetFileAsString(path) == closedSave,
                    "All closed-day reroll routes must leave buyer, offer, stock, count and save unchanged");
                await Reload();
                reroll = game.GetNode<Button>(actions + "DeclineButton");
                foreach (string language in new[] { "en", "ru" })
                {
                    TranslationServer.SetLocale(language);
                    await ToSignal(this, SignalName.ProcessFrame);
                    game.RerollCustomer();
                    Check(game.DayClosed && reroll.Disabled && game.RerollsRemaining == remaining &&
                        game.CustomerIndex == buyer && game.OfferedPrice == offer,
                        "Reload and language refresh must not restore expired rerolls");
                }
                Check(!game.GetNode<Button>(actions + "AcceptButton").Disabled,
                    "The last offer remains sellable after time expires");
                game.AcceptTrade();
                game.AcceptTrade();
                Check(game.Score == score + offer && !game.TradeActive && game.DayClosed,
                    "The final sale must pay exactly once without reopening the day");
                game.NewGame();
                game.TradeActive = true; game.ItemIndex = 0; game.CustomerIndex = 0; game.RerollsRemaining = 3;
                game.ShowTrade();
                Check(!game.DayClosed && !game.GetNode<Button>(actions + "DeclineButton").Disabled,
                    "Starting a new open day must restore normal reroll availability");
                game.QueueFree(); game = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(locale);
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestDayCycle()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        Vector2I originalSize = Root.Size;
        TradeScene game = null;
        try
        {
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            game = await CreateGame(scene);
            Check(!game.DayClosed && game.DayRemainingSeconds == 120 &&
                  game.GetNode<Label>("HUD/Root/DayTimer").Text == "02:00",
                "A new day must start with a full two minutes in mm:ss format");
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            foreach (float zoom in new[] { 0.25f, 1.8f })
            {
                Root.Size = size;
                game.GetNode<Camera2D>("Camera2D").Zoom = Vector2.One * zoom;
                await ToSignal(this, SignalName.ProcessFrame);
                var timer = game.GetNode<Label>("HUD/Root/DayTimer");
                Rect2 bounds = timer.GetGlobalRect();
                Check(Mathf.IsEqualApprox(bounds.GetCenter().X, Root.GetVisibleRect().Size.X / 2) &&
                      Root.GetVisibleRect().Encloses(bounds) && timer.MouseFilter == Control.MouseFilterEnum.Ignore,
                    "The timer must stay centered at the top, in bounds, and ignore mouse input regardless of zoom");
            }
            game._Process(0.25);
            Check(game.DayRemainingSeconds == 119.75 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "02:00",
                "The timer must count partial seconds without showing zero early");
            game._Process(0.75);
            Check(game.GetNode<Label>("HUD/Root/DayTimer").Text == "01:59", "The timer must roll from minutes to seconds");

            foreach (bool manual in new[] { true, false })
            {
                game.NewGame();
                game.Score = 1000;
                CraftAtelier(game);
                SelectInventorySlot(game, 0);
                game.PlaceItemInShowcase(0);
                SelectInventorySlot(game, 0);
                game.PlaceItemInShowcase(1);
                game._Process(game.BuyerCountdown + 0.001);
                Check(game.TradeActive && game.EnergyRemaining == 1, "Closing test must have an active trade and unused energy");
                int item = game.ItemIndex;
                int[] inventory = game.StoredItems;
                int[] showcases = game.ShowcaseItems;
                double remaining = game.DayRemainingSeconds;
                game.GetNode<PauseMenu>("PauseMenu").Open();
                game._Process(120);
                Check(!game.DayClosed && game.DayRemainingSeconds == remaining && game.TradeActive,
                    "Pause must freeze the day even with an active trade");
                game.GetNode<PauseMenu>("PauseMenu").Close();
                game.CurrentDay = 4;
                game.Score = game.FirstQuota - 1;
                if (manual) await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
                else game._Process(game.DayRemainingSeconds);
                Check(game.DayClosed && game.DayRemainingSeconds == 0 && game.EnergyRemaining == 0 &&
                      game.CurrentDay == 4 && !game.Failed && game.TradeActive && game.ItemIndex == item &&
                      game.StoredItems.SequenceEqual(inventory) && game.ShowcaseItems.SequenceEqual(showcases) &&
                      game.GetNode<Label>("HUD/Root/DayTimer").Text == "00:00" && game.BuyerCountdown == 0,
                    "Manual close and timeout must discard energy, retain goods and keep the last trade before quota settlement");
                Check(game.GetNode<Button>("World/Atelier/GetItemButton").Disabled &&
                      game.GetNode<Button>("HUD/Root/ProgressPanel/Content/EndDayButton").Disabled &&
                      !game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton").Disabled &&
                      game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton").Disabled,
                    "Closed-day controls must block production, advance and reroll while retaining sale");
                int cash = game.Score;
                CraftAtelier(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game._Process(120);
                Check(game.DayClosed && game.TradeActive && game.ItemIndex == item && game.Score == cash &&
                      game.StoredItems.SequenceEqual(inventory) && game.CurrentDay == 4,
                    "Repeated closing and ticking must not advance the day, spend money or replace the active buyer");
                int rerolls = game.RerollsRemaining;
                int customer = game.CustomerIndex;
                await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton");
                Check(game.TradeActive && game.RerollsRemaining == rerolls && game.CustomerIndex == customer,
                    "A real reroll click must not replace the price or buyer after closing");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(game.DayClosed && game.DayRemainingSeconds == 0 && game.EnergyRemaining == 0 &&
                      game.TradeActive && game.ItemIndex == item && game.RerollsRemaining == rerolls &&
                      game.ShowcaseItems.SequenceEqual(showcases),
                    "Reload must retain closed day, zero energy and the unchanged last offer");
                game._Input(new InputEventKey { Keycode = Key.F, Pressed = true });
                Check(game.RerollsRemaining == rerolls, "The reroll hotkey must remain blocked after closing and reload");
                for (int count = game.RerollsRemaining; count > 0; count--) game.RerollCustomer();
                int offer = game.OfferedPrice;
                await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
                game.AcceptTrade();
                Check(!game.TradeActive && game.DayClosed && game.Score == cash + offer && game.EnergyRemaining == 0 &&
                      game.ShowcaseItems.SequenceEqual(showcases) && game.BuyerCountdown == 0,
                    "The final sale must credit money once and keep the shop closed with displayed goods intact");
                game._Process(120);
                Check(!game.TradeActive && game.BuyerCountdown == 0, "No further buyers may arrive after the last sale");
                await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
                Check(!game.DayClosed && game.CurrentDay == 5 && game.QuotaStage == 1 &&
                      game.Score == cash + offer - game.FirstQuota && game.EnergyRemaining == 2 && game.DayRemainingSeconds == 120 &&
                      game.BuyerCountdown is >= 0.5 and <= 2,
                    "The next day must settle the quota using the last sale, restore energy/time and schedule a fresh buyer delay");
                await ClickControl(game, "DayTransitionCard/Root/Center/Card/Content/Choices/Atelier/Content/Choose");
                game._Process(game.BuyerCountdown + 0.001);
                Check(game.TradeActive, "Buyers must return when the next day starts");
                game.ReturnToCheckpoint();
                Check(!game.TradeActive && !game.DayClosed && game.DayRemainingSeconds == 120 && game.EnergyRemaining == 2,
                    "Checkpoint restore must start a fresh open day");
            }

            game.NewGame();
            CraftAtelier(game);
            SelectInventorySlot(game, 0);
            game.PlaceItemInShowcase(0);
            int displayedItem = game.ShowcaseItems[0];
            game._Process(120);
            Check(game.DayClosed && !game.TradeActive && game.EnergyRemaining == 0 && game.ShowcaseItems[0] == displayedItem,
                "Expiry must cancel a pending buyer in the same frame instead of creating a new last trade");
            game._Process(120);
            Check(!game.TradeActive, "An expired day with no active deal must stay closed");
            game.NewGame();
            game._Process(10.25);
            game.GetNode<PauseMenu>("PauseMenu").Open();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(scene);
            Check(!Paused && !game.DayClosed && game.DayRemainingSeconds == 109.75 && game.EnergyRemaining == 2,
                "Pause/menu save must restore the exact remaining fraction without charging offline time");
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            using (var legacy = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                legacy.StoreString("{\"save_version\":7,\"energy_remaining\":1}");
            game = await CreateGame(scene);
            Check(!game.DayClosed && game.DayRemainingSeconds == 120 && game.EnergyRemaining == 1,
                "Pre-timer saves must gain two full minutes while preserving their spent energy");
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Two-minute timer, manual/timeout closure, last sale, quota, pause and save migration checks passed");
    }

    private async Task TestDayTransitionCards()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        Vector2I originalSize = Root.Size;
        string language = GameLocalization.Language;
        TradeScene game = null;
        const string content = "DayTransitionCard/Root/Center/Card/Content/";
        try
        {
            game = await CreateGame(scene);
            foreach (string locale in new[] { "ru", "en" })
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                TranslationServer.SetLocale(locale);
                Root.Size = size;
                game.NewGame();
                game.Score = 2000;
                for (int day = 1; day <= 6; day++)
                {
                    game.EndDay();
                    await WaitForDayCardAnimation(game);
                    Check(game.DayClosed && !game.DayTransitionOpen, "Closing alone must not show a new-day card");
                    game.EndDay();
                    await WaitForDayCardAnimation(game);
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                    Check(game.DayTransitionOpen && Paused && game.CurrentDay == day + 1 && game.DayRemainingSeconds == 120 &&
                          game.GetNode<Label>(content + "Route").Text.StartsWith($"{day:00}  →  {day + 1:00}"),
                        "Each of the six transitions must show its own card with the new day frozen at two minutes");
                    Check(card.Options.Count == 3 && card.Options.Distinct().Count() == 3 &&
                          game.GetNode(content + "Choices").GetChildCount() == 3, "Each transition must offer exactly three different upgrades");
                    if (day == 6 && locale == "ru" && size.X == 1280 && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                    {
                        await ToSignal(this, SignalName.ProcessFrame);
                        await ToSignal(this, SignalName.ProcessFrame);
                        Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-day-card-preview.png"));
                    }
                    foreach (var option in card.Options)
                    {
                        foreach (string node in new[] { "Amount", "Title", "Description", "Duration", "Choose" })
                        {
                            var control = game.GetNode<Control>(content + $"Choices/{option}/Content/{node}");
                            Check(Root.GetVisibleRect().Encloses(control.GetGlobalRect()), "Upgrade card content must fit both sizes and languages");
                        }
                        var button = game.GetNode<Button>(content + $"Choices/{option}/Content/Choose");
                        Check(button.Text == GameLocalization.Format("Купить за {0}$", DayUpgradeSession.Cost(option)), "Upgrade prices must translate");
                    }
                    double remaining = game.DayRemainingSeconds;
                    int cash = game.Score;
                    var camera = game.GetNode<Camera2D>("Camera2D");
                    Vector2 position = camera.Position;
                    game.EndDay();
                    await WaitForDayCardAnimation(game);
                    game._Process(60);
                    game.TickCraftDay(60);
                    game.GetNode<PauseMenu>("PauseMenu").Open();
                    game.GetItemFromAtelier();
                    Check(game.DayRemainingSeconds == remaining && game.Score == cash && game.CurrentDay == day + 1 &&
                          !game.CraftMiniGameOpen && camera.Position == position &&
                          !game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
                        "Repeated advance, pause and crafting must not act underneath a day card");
                    if (day == 4) Check(game.QuotaStage == 1, "The 4-to-5 choice must follow ordinary quota settlement");
                    DayUpgrade choice = card.Options[0];
                    card._Input(new InputEventKey { Pressed = true, Echo = true, Keycode = Key.Key1 });
                    Check(card.IsOpen, "Held keys must not purchase an upgrade");
                    if (day % 2 == 0) await ClickControl(game, content + $"Choices/{choice}/Content/Choose");
                    else card._Input(new InputEventKey { Pressed = true, Keycode = Key.Key1 });
                    card.Choose(card.Options[1]);
                    await WaitForDayCardAnimation(game);
                    Check(!Paused && !game.DayTransitionOpen && game.CurrentDay == day + 1,
                        "Mouse/keyboard selection must apply only one upgrade without skipping a day");
                    Check(game.DayUpgrades.Selected == choice && game.Score == cash - DayUpgradeSession.Cost(choice) + (choice == DayUpgrade.Cash ? 50 : 0) &&
                          game.EnergyRemaining == (choice == DayUpgrade.Energy ? 3 : 2) &&
                          game.DayRemainingSeconds == (choice == DayUpgrade.Time ? 150 : 120), "The chosen upgrade must change only its intended stat");
                }
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                Check(game.Completed && !game.DayTransitionOpen && !Paused, "Winning after day seven must not show a day-eight card");
            }
            game.NewGame();
            game.CurrentDay = 4;
            game.Score = 0;
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.EndDay();
            await WaitForDayCardAnimation(game);
            Check(game.Failed && !game.DayTransitionOpen && !Paused, "An unpaid quota must not show a successful transition");
            game.NewGame();
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.ReturnToCheckpoint();
            Check(!Paused && !game.DayTransitionOpen && game.CurrentDay == 1, "Checkpoint restore must release the card pause");
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.NewGame();
            Check(!Paused && !game.DayTransitionOpen && game.CurrentDay == 1, "New game must release the card pause");
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            Check(!Paused, "Freeing an open card must release its pause");
            game = await CreateGame(scene);
            Check(game.CurrentDay == 2 && Paused && game.DayTransitionOpen && game.DayRemainingSeconds == 120,
                "Reload before selection must reopen the three choices without repeating the day transition");
            foreach (DayUpgrade choice in new[] { DayUpgrade.Cash, DayUpgrade.Energy, DayUpgrade.Time, DayUpgrade.Atelier, DayUpgrade.Music })
            {
                game.NewGame();
                game.Score = 1000;
                game.CurrentDay = choice is DayUpgrade.Atelier or DayUpgrade.Music ? 3 : 1;
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                card.Choose(choice);
                await WaitForDayCardAnimation(game);
                int cash = game.Score;
                int energy = game.EnergyRemaining;
                double time = game.DayRemainingSeconds;
                int atelierLevel = game.DayUpgrades.AtelierLevel;
                int musicLevel = game.DayUpgrades.MusicLevel;
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(!Paused && !game.DayTransitionOpen && game.Score == cash && game.EnergyRemaining == energy &&
                      game.DayRemainingSeconds == time && game.DayUpgrades.AtelierLevel == atelierLevel && game.DayUpgrades.MusicLevel == musicLevel,
                    "Every upgrade must survive reload with no repeated reward, lost energy or clamped bonus time");
                game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(choice);
                await WaitForDayCardAnimation(game);
                Check(game.Score == cash && game.EnergyRemaining == energy && game.DayRemainingSeconds == time, "Reload must not reapply a reward");
                if (choice is DayUpgrade.Atelier or DayUpgrade.Music)
                {
                    foreach (string room in new[] { "Atelier", "MusicWorkshop" })
                    {
                        var panel = game.GetNode<RarityPanel>($"World/{room}/RarityPanel");
                        panel.RefreshChances();
                        bool boosted = (room == "Atelier") == (choice == DayUpgrade.Atelier);
                        Check(panel.Values.GetNode<Label>("Chance0").Text == (boosted ? "45%" : "60%") &&
                              panel.Values.GetNode<Label>("Chance2").Text == (boosted ? "18%" : "10%"), "Category chances must match the actual boosted table");
                        Check(panel.Values.GetNode<Label>("Chance0").GetThemeColor("font_color") == (boosted ? new Color("bf3232") : Colors.Black) &&
                              panel.Values.GetNode<Label>("Chance2").GetThemeColor("font_color") == (boosted ? new Color("208447") : Colors.Black),
                            "Chances below baseline must be red and above baseline green; the other category stays neutral");
                    }
                    int[] counts = new int[10];
                    string workshop = choice == DayUpgrade.Atelier ? "atelier" : "music";
                    for (int ticket = 0; ticket < 100; ticket++) counts[WorkshopItemCatalog.Roll(workshop, ticket, game.DayUpgrades)]++;
                    foreach (var item in WorkshopItemCatalog.Items.Select((item, index) => (item, index)).Where(x => x.item.WorkshopId == workshop))
                        Check(counts[item.index] == WorkshopItemCatalog.RarityWeight(workshop, item.item.RarityId, game.DayUpgrades), "All 100 tickets must match the displayed category chances");
                }
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                Check(game.DayUpgrades.AtelierLevel == atelierLevel && game.DayUpgrades.MusicLevel == musicLevel &&
                      game.DayRemainingSeconds == 120 && game.EnergyRemaining == 2, "Category upgrades must persist across days while daily energy/time expire");
                game.ReturnToCheckpoint();
                Check(game.DayUpgrades.AtelierLevel == (game.CheckpointDay == 5 ? atelierLevel : 0) &&
                      game.DayUpgrades.MusicLevel == (game.CheckpointDay == 5 ? musicLevel : 0), "Checkpoint restore must retain exactly its captured category upgrades");
            }
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(language);
            Root.Size = originalSize;
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("All six day transition cards, localization, layout, input, quotas and reload checks passed");
    }

    private async Task TestClockHud()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        Vector2I originalSize = Root.Size;
        try
        {
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            game = await CreateGame(scene);
            game.NewGame();
            var timer = game.GetNode<Label>("HUD/Root/DayTimer");
            var clock = timer.GetNode<TextureRect>("Clock");
            Check(timer.Text == "90" && clock.Texture.ResourcePath == "res://art/ui/day_timer_empty.png",
                "The existing one-room duration must display plain seconds on the empty clock");
            foreach (bool music in new[] { false, true })
            {
                if (music) { game.Score = 1000; game.TryUnlockMusicWorkshop(); }
                foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
                foreach (float zoom in new[] { 0.25f, 1.8f })
                {
                    Root.Size = size;
                    game.GetNode<Camera2D>("Camera2D").Zoom = Vector2.One * zoom;
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    game._Process(0);
                    Rect2 bounds = timer.GetGlobalRect();
                    Rect2 inventory = game.GetNode<Control>("HUD/InventoryUI/ScrollContainer").GetGlobalRect();
                    Check(Root.GetVisibleRect().Encloses(bounds) && bounds.Position.X == 12 &&
                          Mathf.IsEqualApprox(bounds.End.Y, Root.GetVisibleRect().Size.Y - 12) && !bounds.Intersects(inventory),
                        "The clock must remain bottom-left without overlapping three or six inventory slots at either size/zoom");
                    Check(timer.MouseFilter == Control.MouseFilterEnum.Ignore && clock.MouseFilter == Control.MouseFilterEnum.Ignore &&
                          clock.ShowBehindParent && timer.GetThemeFontSize("font_size") >= 18,
                        "Clock art must sit behind readable digits and let mouse input reach the world");
                }
            }
            Check(timer.Text == "90" && game.DayRemainingSeconds == 90, "Unlocking the music room must retain the existing ninety-second day");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            game._Process(10);
            Check(timer.Text == "90" && game.DayRemainingSeconds == 90, "Pause must freeze the seconds and clock state");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            if (OS.GetCmdlineUserArgs().Contains("--clock-preview"))
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-clock-active.png"));
            }
            game._Process(0.25);
            Check(timer.Text == "90", "Fractional seconds must round up rather than show zero early");
            game._Process(0.75);
            Check(timer.Text == "89", "Seconds must count down without a minutes separator");
            game._Process(88.99);
            Check(timer.Text == "01" && clock.Texture.ResourcePath.EndsWith("day_timer_empty.png"), "The last partial second must show 01 on the empty face");
            game._Process(0.02);
            Check(timer.Text == "00" && game.DayClosed && clock.Texture.ResourcePath.EndsWith("day_timer_expired.png"),
                "At timeout the clock must show 00 and switch to the supplied filled red face");
            if (OS.GetCmdlineUserArgs().Contains("--clock-preview"))
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-clock-expired.png"));
            }
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            await WaitForDayCardAnimation(game);
            Check(timer.Text == "90" && clock.Texture.ResourcePath.EndsWith("day_timer_empty.png"), "The next day must restore the empty clock");
            game._Process(2.25);
            double remaining = game.DayRemainingSeconds;
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(scene);
            timer = game.GetNode<Label>("HUD/Root/DayTimer");
            clock = timer.GetNode<TextureRect>("Clock");
            Check(game.DayRemainingSeconds == remaining && timer.Text == "88" && clock.Texture.ResourcePath.EndsWith("day_timer_empty.png"),
                "Reload must restore fractional time with the matching seconds and art");
            game.EndDay();
            await WaitForDayCardAnimation(game);
            Check(timer.Text == "00" && clock.Texture.ResourcePath.EndsWith("day_timer_expired.png"), "Manual closure must also show the filled clock");
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(DayUpgrade.Time);
            await WaitForDayCardAnimation(game);
            Check(timer.Text == "120" && game.DayRemainingSeconds == 120, "The timer must show all remaining seconds including a purchased time bonus");
            game.GetItemFromAtelier();
            var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
            Check(embroidery.IsOpen && embroidery.GetNode<Label>("Surface/DayTimer").Text == timer.Text,
                "The minigame clock copy must keep working with the new seconds format");
            embroidery.GetNode<Button>("Surface/Cancel").EmitSignal(Button.SignalName.Pressed);
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            Root.Size = originalSize;
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestDayCardPurchases()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        string language = GameLocalization.Language;
        Vector2I originalSize = Root.Size;
        const string content = "DayTransitionCard/Root/Center/Card/Content/";
        void Seed(int day, int cash)
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            file.StoreString(System.Text.Json.JsonSerializer.Serialize(new
            {
                save_version = 10, day, score = cash, quota_stage = day >= 5 ? 1 : 0,
                energy_remaining = 2, day_remaining_seconds = 60
            }));
        }
        async Task Reload()
        {
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = await CreateGame(scene);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            foreach (string locale in new[] { "ru", "en" })
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                TranslationServer.SetLocale(locale);
                Root.Size = size;
                Seed(1, 0);
                game = await CreateGame(scene);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.IsOpen && Paused, "A zero balance must still allow viewing and skipping the shop");
                foreach (var choice in card.Options)
                {
                    var button = game.GetNode<Button>(content + $"Choices/{choice}/Content/Choose");
                    Check(button.Disabled && button.Text == GameLocalization.Format("Купить за {0}$", DayUpgradeSession.Cost(choice)),
                        "Unaffordable cards must show their price and disable purchase in both languages");
                    foreach (string name in new[] { "Amount", "Title", "Description", "Duration", "Choose" })
                        Check(Root.GetVisibleRect().Encloses(game.GetNode<Control>(content + $"Choices/{choice}/Content/{name}").GetGlobalRect()),
                            "All paid card content must fit the viewport");
                    card.Choose(choice);
                    await WaitForDayCardAnimation(game);
                }
                card._Input(new InputEventKey { Pressed = true, Keycode = Key.Key1 });
                Check(card.IsOpen && game.Score == 0 && game.DayUpgrades.Selected == DayUpgrade.None,
                    "Direct requests and hotkeys must not purchase cards without funds");
                var skip = game.GetNode<Button>(content + "Skip");
                Check(!skip.Disabled && Root.GetVisibleRect().Encloses(skip.GetGlobalRect()) &&
                      skip.Tr(skip.Text) == (locale == "en" ? "Skip" : "Пропустить"), "Free skip must stay visible and localized");
                if (size.X == 960) await ClickControl(game, content + "Skip");
                else card._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
                card.Skip();
                await WaitForDayCardAnimation(game);
                Check(!Paused && !card.IsOpen && game.Score == 0 && !game.DayUpgrades.Pending &&
                      game.DayUpgrades.Selected == DayUpgrade.None, "Skipping must resume with no payment or effect");
                await Reload();
                Check(!Paused && !game.DayTransitionOpen && !game.DayUpgrades.Pending && game.Score == 0 && game.CurrentDay == 2,
                    "Reload must not reopen or charge a skipped offer");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = null;
            }
            Root.Size = new Vector2I(1280, 720);
            foreach (var choice in Enum.GetValues<DayUpgrade>().Where(value => value != DayUpgrade.None))
            {
                int day = choice is DayUpgrade.AtelierPrice or DayUpgrade.MusicPrice ? 5 : choice is DayUpgrade.Atelier or DayUpgrade.Music ? 3 : 1;
                Seed(day, DayUpgradeSession.Cost(choice));
                game = await CreateGame(scene);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                await Reload();
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.IsOpen && game.DayUpgrades.Pending, "An unfinished purchase must reopen after reload");
                int balance = game.Score;
                double remaining = game.DayRemainingSeconds;
                int energy = game.EnergyRemaining;
                var button = game.GetNode<Button>(content + $"Choices/{choice}/Content/Choose");
                Check(!button.Disabled, "Exact funds must allow buying the card");
                if (choice == DayUpgrade.AtelierPrice && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                {
                    await ToSignal(this, SignalName.ProcessFrame);
                    Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-paid-cards-preview.png"));
                }
                await ClickControl(game, content + $"Choices/{choice}/Content/Choose");
                int expected = balance - DayUpgradeSession.Cost(choice) + (choice == DayUpgrade.Cash ? game.DayUpgrades.CashReward : 0);
                card.Choose(choice);
                await WaitForDayCardAnimation(game);
                card.Skip();
                await WaitForDayCardAnimation(game);
                Check(game.Score == expected && !Paused && !card.IsOpen && game.DayUpgrades.Selected == choice,
                    "A card must charge once, apply once and dismiss the shop");
                Check(game.EnergyRemaining == energy + (choice == DayUpgrade.Energy ? 1 : 0) &&
                      game.DayRemainingSeconds == remaining + (choice == DayUpgrade.Time ? 30 : 0), $"Paid energy/time effects must match their descriptions: {choice}, energy={game.EnergyRemaining}/{energy}, time={game.DayRemainingSeconds}/{remaining}");
                int atelier = game.DayUpgrades.AtelierLevel, music = game.DayUpgrades.MusicLevel;
                int atelierPrice = game.DayUpgrades.AtelierPriceLevel, musicPrice = game.DayUpgrades.MusicPriceLevel;
                Check(atelier == (choice == DayUpgrade.Atelier ? 1 : 0) && music == (choice == DayUpgrade.Music ? 1 : 0) &&
                      atelierPrice == (choice == DayUpgrade.AtelierPrice ? 1 : 0) && musicPrice == (choice == DayUpgrade.MusicPrice ? 1 : 0),
                    "Paid category upgrades must apply exactly one level to the correct category");
                await Reload();
                Check(game.Score == expected && !Paused && !game.DayTransitionOpen && game.DayUpgrades.Selected == choice &&
                      game.DayUpgrades.AtelierLevel == atelier && game.DayUpgrades.MusicLevel == music &&
                      game.DayUpgrades.AtelierPriceLevel == atelierPrice && game.DayUpgrades.MusicPriceLevel == musicPrice,
                    "Reload must preserve the payment and upgrade without charging or rewarding twice");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = null;
            }
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            Root.Size = originalSize;
            TranslationServer.SetLocale(language);
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestCategoryPriceUpgrades()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        string language = GameLocalization.Language;
        Vector2I size = Root.Size;
        try
        {
            TranslationServer.SetLocale("ru");
            Root.Size = new Vector2I(1280, 720);
            foreach (DayUpgrade choice in new[] { DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice })
            {
                bool clothing = choice == DayUpgrade.AtelierPrice;
                using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                    file.StoreString(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        save_version = 10, day = clothing ? 4 : 5, score = 1000, energy_remaining = 2,
                        quota_stage = clothing ? 0 : 1, checkpoint_day = clothing ? 1 : 5,
                        day_remaining_seconds = 120, music_workshop_unlocked = true,
                        stored_items = Enumerable.Range(0, 10).ToArray()
                    }));
                game = await CreateGame(scene);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.Options.Contains(choice) && card.Options.Count == 3,
                    $"Price card {choice} must be reachable: day={game.CurrentDay}, failed={game.Failed}, pending={game.DayUpgrades.Pending}, options={string.Join(',', card.Options)}");
                if (clothing && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                    Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-price-upgrade-preview.png"));
                await ClickControl(game, $"DayTransitionCard/Root/Center/Card/Content/Choices/{choice}/Content/Choose");
                Check(!Paused && !game.DayTransitionOpen, "Selecting a price card must apply it and resume the day");
                foreach (int index in Enumerable.Range(0, 10))
                {
                    bool boosted = WorkshopItemCatalog.Items[index].WorkshopId == (clothing ? "atelier" : "music");
                    int expected = boosted ? (int)Math.Round(TradeScene.GetItemBasePrice(index) * 1.2m, MidpointRounding.AwayFromZero) : TradeScene.GetItemBasePrice(index);
                    Check(game.GetItemPrice(index) == expected, "A price card must affect all rarities of only its category, including existing goods");
                    var slot = (SlotUI)game.GetNode<InventoryUI>("HUD/InventoryUI").Container.GetChild(index);
                    Check(slot.ItemInfo.EndsWith($"Стоимость: {expected}$"), "Existing inventory tooltips must refresh to the improved price");
                }
                foreach (string room in new[] { "Atelier", "MusicWorkshop" })
                {
                    var panel = game.GetNode<RarityPanel>($"World/{room}/RarityPanel");
                    panel.RefreshChances();
                    int[] baseline = room == "Atelier" ? new[] { 30, 50, 80, 120, 200 } : new[] { 45, 75, 120, 180, 300 };
                    bool boosted = (room == "Atelier") == clothing;
                    for (int rarity = 0; rarity < 5; rarity++)
                    {
                        var label = panel.Values.GetNode<Label>($"Price{rarity}");
                        int expected = boosted ? (int)(baseline[rarity] * 1.2m) : baseline[rarity];
                        Check(label.Text == $"{expected}$" && label.GetThemeColor("font_color") == (boosted ? new Color("208447") : Colors.Black),
                            "Craft tables must show boosted prices in green and the other category in neutral color");
                    }
                    if (clothing && room == "Atelier" && OS.GetCmdlineUserArgs().Contains("--day-card-preview"))
                    {
                        panel.Show();
                        await ToSignal(this, SignalName.ProcessFrame);
                        await ToSignal(this, SignalName.ProcessFrame);
                        Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-price-table-preview.png"));
                        panel.Hide();
                    }
                }
                int item = clothing ? 0 : 2;
                SelectInventorySlot(game, item);
                game.PlaceItemInShowcase(0);
                game._Process(game.BuyerCountdown + 0.001);
                Check(game.TradeActive && game.ItemIndex == item && game.OfferedPrice == TradeScene.PriceForPercent(game.GetItemPrice(item), game.Percentage),
                    "The buyer offer must use the improved category price");
                int offer = game.OfferedPrice;
                int cash = game.Score;
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(game.TradeActive && game.OfferedPrice == offer, "Reload must retain an active trade with the improved price");
                game.AcceptTrade();
                game.AcceptTrade();
                Check(game.Score == cash + offer, "Selling an improved item must credit its offer exactly once");
                int beforeCraft = game.Score;
                if (clothing) CraftAtelier(game); else CraftMusic(game);
                Check(game.Score == beforeCraft - (clothing ? 90 : 140), "Price upgrades must leave production costs unchanged");
                foreach (int index in game.StoredItems.TakeLast(3))
                    Check(game.GetItemPrice(index) == (int)(TradeScene.GetItemBasePrice(index) * 1.2m), "Newly crafted goods must receive the category price too");
                game.EndDay();
                await WaitForDayCardAnimation(game);
                game.EndDay();
                await WaitForDayCardAnimation(game);
                card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                card.Choose(choice);
                await WaitForDayCardAnimation(game);
                Check(game.GetItemPrice(item) == (int)(TradeScene.GetItemBasePrice(item) * 1.4m), "Repeated price cards must add twenty percentage points rather than compound");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(game.GetItemPrice(item) == (int)(TradeScene.GetItemBasePrice(item) * 1.4m), "Stacked price bonuses must survive reload");
                game.ReturnToCheckpoint();
                Check(game.GetItemPrice(item) == TradeScene.GetItemBasePrice(item), "Checkpoint rollback must remove price cards selected after that checkpoint");
                game.NewGame();
                Check(game.DayUpgrades.AtelierPriceLevel == 0 && game.DayUpgrades.MusicPriceLevel == 0, "New game must clear both category price bonuses");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = null;
            }
            using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":10,\"day\":4,\"score\":1000,\"atelier_price_level\":1,\"music_price_level\":1}");
            game = await CreateGame(scene);
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.EndDay();
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(DayUpgrade.AtelierPrice);
            await WaitForDayCardAnimation(game);
            game.ReturnToCheckpoint();
            Check(game.DayUpgrades.AtelierPriceLevel == 1 && game.DayUpgrades.MusicPriceLevel == 1,
                "The day-five checkpoint must capture both price bonus levels before the next selection");
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(language);
            Root.Size = size;
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("Category price cards, tables, inventory, sale, stacking, save and checkpoint checks passed");
    }
}
