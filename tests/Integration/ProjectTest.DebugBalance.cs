using System;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestDebugBalance()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmokeMenuMusic"), "Balance checks require isolated userdata");
        const string profilePath = "user://debug_balance_integration.json", save = "user://debug_balance_game.json";
        const string content = "Root/Inset/Panel/Content";
        string oldPath = DebugBalanceSettings.StoragePath;
        var size = Root.Size;
        TradeScene game = null;
        var panel = Root.GetNode<DebugBalancePanel>("DebugBalance");
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        async Task Press(Key key, bool echo = false)
        {
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = key, Echo = echo }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = key }, true);
            await Frame();
        }
        async Task Create()
        {
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await Frame();
        }
        try
        {
            foreach (string path in new[] { profilePath, save }) if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
            DebugBalanceSettings.StoragePath = profilePath;
            Check(DebugBalanceSettings.Reload(out _), "No debug profile must mean the shipped baseline");
            var menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            await Frame();
            await Press(Key.F10);
            Check(panel.IsOpen && Paused && panel.GetNode<Button>(content + "/Actions/NewGame").Disabled,
                "F10 must also expose saved starting parameters in the main menu without inventing an active run");
            await Press(Key.Escape);
            Check(!panel.IsOpen && !Paused && menu.IsInsideTree(), "Escape from menu balance must resume the menu without quitting");
            menu.QueueFree(); await Frame();
            await Create();
            await ClickControl(game, "HUD/DayClock/Root/DayTimer/StartButton");
            game._Process(10);
            double remaining = game.DayRemainingSeconds;
            int cash = game.Score, energy = game.EnergyRemaining;
            await Press(Key.F10);
            Check(panel.IsOpen && Paused, "F10 must open the balance panel and pause gameplay");
            game.SetProcess(true);
            await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
            Check(game.DayRemainingSeconds == remaining, "Editing balance must freeze the day timer");
            game.SetProcess(false);
            await Press(Key.F10, echo: true);
            Check(panel.IsOpen, "Held F10 must not flicker the panel");
            await Press(Key.F6); await Press(Key.F7);
            Check(game.Score == cash && game.DayRemainingSeconds == remaining,
                "The debug editor must block underlying resource hotkeys while it owns input");
            foreach (var viewport in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                Root.Size = viewport;
                await Frame();
                foreach (int tab in Enumerable.Range(0, 5))
                {
                    panel.GetNode<TabContainer>(content + "/Tabs").CurrentTab = tab;
                    await Frame();
                    var apply = panel.GetNode<Control>(content + "/Actions/Apply");
                    var tabs = panel.GetNode<Control>(content + "/Tabs");
                    Check(Root.GetVisibleRect().Encloses(apply.GetGlobalRect()) && tabs.Size.Y > 100,
                        "Every debug tab must leave a scrollable editor and visible action buttons at both sizes");
                    if (tab is 0 or 3 or 4)
                        using (var image = await CapturePresentation($"debug-balance-{viewport.X}-{tab}")) { }
                }
            }
            panel.GetNode<TabContainer>(content + "/Tabs").CurrentTab = 0;
            await Frame();
            panel.Editor("start.cash").GrabFocus();
            panel.Editor("start.cash").SelectAll();
            for (int digit = 0; digit < 3; digit++)
            {
                Root.PushInput(new InputEventKey { Pressed = true, Keycode = (Key)'5', Unicode = '5' }, true);
                Root.PushInput(new InputEventKey { Pressed = false, Keycode = (Key)'5' }, true);
            }
            Check(panel.Editor("start.cash").Text == "555",
                $"Keyboard entry must reach the focused starting cash field: {panel.Editor("start.cash").Text}, focus={panel.Editor("start.cash").HasFocus()}");
            panel.Editor("energy.before").Text = (4).ToString(System.Globalization.CultureInfo.InvariantCulture); panel.Editor("energy.after").Text = (9).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("craft.count").Text = (3).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("workshop.atelier.cost").Text = (7).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("workshop.music.cost").Text = (11).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("workshop.music.unlock").Text = (20).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("day.seconds").Text = (180).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("craft.atelier.seconds").Text = (25).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("craft.music.seconds").Text = (17).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor(DebugBalanceSettings.BasePriceKey).Text = (123).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("rarity.unique.rerolls").Text = (4).ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (var rarity in EconomyConfigProvider.Baseline.Rarities)
                panel.Editor($"rarity.{rarity.Id}.weight").Text = (rarity.Id == "unique" ? 100 : 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (var item in WorkshopItemCatalog.BaselineItems)
                panel.RarityEditor(item.Kind).Select(item.Kind is "hat" or "guitar" ? 4 : 0);
            panel.Editor("cards.weighted").Text = (1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (var card in DebugBalanceSettings.Cards)
                panel.Editor($"card.{card}.weight").Text = (card == DayUpgrade.Cash ? 1 : 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
            panel.Editor("card.Energy.cost").Text = (17).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var board = new PolishingSurface();
            Root.AddChild(board);
            var originalPolish = board.Begin("guitar");
            panel.Editor("polish.guitar.radius").Text = "1.11";
            panel.Editor("polish.guitar.strength").Text = "0.83";
            await ClickControl(panel, content + "/Actions/Apply");
            Check(DebugBalanceSettings.Reload(out _), "Polishing coefficients must reload from the profile");
            var revisedPolish = board.Begin("guitar");
            Check(revisedPolish.MaskArea == originalPolish.MaskArea &&
                Math.Abs(revisedPolish.Radius / originalPolish.Radius - 1.11) < .001 &&
                Math.Abs(revisedPolish.CleaningStrength / originalPolish.CleaningStrength - .83 / .67) < .001,
                "F10 edits must independently change the physical brush radius and strength on the next game");
            board.QueueFree();
            Check(FileAccess.FileExists(profilePath) && DebugBalanceSettings.Number("start.cash") == 555,
                $"Apply must commit focused typed input and persist the profile to disk: {DebugBalanceSettings.Number("start.cash")}, {panel.GetNode<Label>(content + "/Status").Text}");
            Check(game.Score == cash && game.EnergyRemaining == energy && Math.Abs(game.DayRemainingSeconds - (remaining + 180 - EconomyConfigProvider.Baseline.Days.DurationSeconds)) < 0.1,
                "Live apply must retain money/energy and elapsed time while changing the configured day duration");
            Check(TradeScene.GetItemBasePrice(0) == 123 && TradeScene.GetItemRerollLimit(0) == 4 &&
                WorkshopItemCatalog.Items[0].RarityId == "unique", $"Live apply must rebuild item prices, rerolls and rarity: {TradeScene.GetItemBasePrice(0)}/{TradeScene.GetItemRerollLimit(0)}/{WorkshopItemCatalog.Items[0].RarityId}");
            var rarityPanel = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
            rarityPanel.RefreshChances();
            Check(rarityPanel.Values.GetNode<Label>("Chance4").Text == "100%", "The real crafting tooltip must show normalized edited chances");
            using (var image = await CapturePresentation("debug-balance-party")) { }
            await Press(Key.Escape);
            Check(!panel.IsOpen && !Paused && !game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
                "Escape must close only the debug panel without leaking pause or opening PauseMenu");

            game.GetItemFromAtelier();
            var craft = game.GetNode<EmbroideryGame>("EmbroideryGame");
            Check(craft.Session.RemainingSeconds == 25, "Edited crafting duration must configure the next minigame");
            craft.Activate();
            Check(game.Score == cash - 7 && game.StoredItems.Length == 3 && game.StoredItems.All(index => index == 0),
                "Actual crafting must pay the edited cost once and use edited count and rarity selection");
            double craftTime = craft.Session.RemainingSeconds, dayTime = game.DayRemainingSeconds;
            await Press(Key.F10);
            await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
            Check(panel.IsOpen && Paused && panel.GetNode<Button>(content + "/Actions/Apply").Disabled &&
                craft.Session.RemainingSeconds == craftTime && game.DayRemainingSeconds == dayTime,
                "Debug inspection inside a craft must freeze both timers and prevent mid-transaction changes");
            await Press(Key.F10);
            Check(Paused && craft.IsOpen, "Closing debug over a craft must preserve its existing pause");
            craft.FinishForTesting(true); craft.Activate();
            Check(!Paused && !craft.IsOpen, "Collect must retain the normal craft pause lifecycle");
            game.SelectInventoryItem(0); game.PlaceItemInShowcase(0);
            game._Process(0);
            Check(game.TradeActive && game.OfferedPrice == TradeScene.PriceForPercent(123, game.Percentage),
                "A real buyer must calculate its offer from the edited item price");
            game.AcceptTrade();
            game.TryUnlockMusicWorkshop();
            game.GetItemFromMusicWorkshop();
            var polish = game.GetNode<EmbroideryGame>("PolishingGame");
            Check(polish.ActiveSession.RemainingSeconds == 17, "Polishing has its own editable duration");
            int beforeMusic = game.Score;
            polish.Activate(); polish.FinishForTesting(true); polish.Activate();
            Check(game.Score == beforeMusic - 11 && game.StoredItems.Count(index => index == 2) == 3,
                "Music crafting must use its own cost and the edited loot table");

            await Press(Key.F10);
            await ClickControl(panel, content + "/Actions/NewGame");
            Check(!panel.IsOpen && !Paused && game.Score == 555 && game.EnergyRemaining == 4 &&
                game.CurrentDay == 1 && !game.DayStarted && game.DayRemainingSeconds == 180 && game.StoredItems.Length == 0,
                "Apply and restart must begin a clean run using the saved starting parameters");
            game.EndDay(); game.EndDay(); game.EndDay();
            await WaitForDayCardAnimation(game);
            Check(game.DayUpgrades.Options.SequenceEqual(new[] { DayUpgrade.Cash }), "Edited card weights must determine the actual dealt cards");
            var choose = game.GetNode<Button>("DayTransitionCard/Root/Center/Card/Content/Choices/Cash/Content/Choose");
            Check(choose.Text == GameLocalization.Text("Получить 50$") && game.DayUpgrades.CostFor(DayUpgrade.Energy) == 17,
                "Cash must be free while other costs remain editable");
            await ClickControl(game, "DayTransitionCard/Root/Center/Card/Content/Choices/Cash/Content/Choose");
            Check(game.Score == 555 + 50, "The real card purchase must charge the same edited cost exactly once");
            Check(DebugBalanceSettings.Reload(out _), "A recreated configuration must read the saved debug profile");
            game.QueueFree(); await Frame(); await Create();
            Check(game.StartingMoney == 555 && game.Score == 605 && game.DayRemainingSeconds == 180 &&
                TradeScene.GetItemBasePrice(0) == 123, "Reload must retain both the saved profile and the current gameplay save");

            var pause = game.GetNode<PauseMenu>("PauseMenu"); pause.Open();
            await Press(Key.F10); await Press(Key.F10);
            Check(Paused && game.GetNode<Control>("PauseMenu/Root/Overlay").Visible,
                "Debug open/close over the pause menu must preserve its pause and visibility");
            pause.Close();
            await Press(Key.F10);
            string savedProfile = FileAccess.GetFileAsString(profilePath);
            foreach (var rarity in EconomyConfigProvider.Baseline.Rarities) panel.Editor($"rarity.{rarity.Id}.weight").Text = (0).ToString(System.Globalization.CultureInfo.InvariantCulture);
            await ClickControl(panel, content + "/Actions/Apply");
            Check(FileAccess.GetFileAsString(profilePath) == savedProfile && panel.GetNode<Label>(content + "/Status").Text.StartsWith("Не применено"),
                "An impossible loot distribution must leave both disk and active settings unchanged");
            await Press(Key.F10); await Press(Key.F10);
            Check(panel.Editor("rarity.unique.weight").Text == "100", "Closing without apply must discard invalid draft changes");
            await ClickControl(panel, content + "/Actions/Defaults");
            Check(DebugBalanceSettings.Number("start.cash") == 555, "Loading defaults into fields must not apply them prematurely");
            panel.Editor("start.cash").Text = "not a number";
            await ClickControl(panel, content + "/Actions/Apply");
            Check(DebugBalanceSettings.Number("start.cash") == 555 && panel.GetNode<Label>(content + "/Status").Text.StartsWith("Не применено"),
                "Invalid typed input must be reported without discarding or publishing the active profile");
            await ClickControl(panel, content + "/Actions/Defaults");
            await ClickControl(panel, content + "/Actions/Apply");
            Check(TradeScene.GetItemBasePrice(0) == 50 && WorkshopItemCatalog.Items[0].RarityId == "common" &&
                DebugBalanceSettings.CardRules.Cost(DayUpgrade.Cash) == 0 && DebugBalanceSettings.CardRules.Cost(DayUpgrade.Energy) == 10, "Applying defaults must clear item and card overrides");
            await Press(Key.F10);
            int revision = DebugBalanceSettings.Revision;
            DebugBalanceSettings.StoragePath = "user://missing_balance_folder/profile.json";
            Check(!DebugBalanceSettings.TrySave(new BalanceProfile(), out _) && DebugBalanceSettings.Revision == revision,
                "Failed disk publication must not publish a different active profile");
            DebugBalanceSettings.StoragePath = profilePath;
            using (var file = FileAccess.Open(profilePath, FileAccess.ModeFlags.Write)) file.StoreString("{broken json");
            Check(!DebugBalanceSettings.Reload(out _) && EconomyConfigProvider.Load().StartingMoney == 220 &&
                FileAccess.GetFileAsString(profilePath) == "{broken json", "Corrupt profile must fall back safely without overwriting the evidence");
        }
        finally
        {
            panel.Close(); Paused = false;
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            DebugBalanceSettings.StoragePath = oldPath; DebugBalanceSettings.Reload(out _);
            Root.Size = size;
            foreach (string path in new[] { profilePath, save }) if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
        }
        await Frame();
    }
}
