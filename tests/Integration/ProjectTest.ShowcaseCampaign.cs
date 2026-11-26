using System;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestShowcaseCampaign()
    {
        const string save = "user://showcase_v20_test.json";
        const string progress = save + ".tutorial.cfg";
        const string atelier = "World/Atelier/GetItemButton";
        const string visitor = "World/TradingRoom/QuotaVisitor";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        string locale = TranslationServer.GetLocale();
        string WorkshopPanel(TradeScene shop) => shop.GetNode("World/MusicWorkshop/LockedOverlay")
            .GetChildren().First(node => node is MusicWorkshopUnlockPanel).Name.ToString();
        async Task Frame()
        {
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        async Task Reload()
        {
            if (game != null)
            {
                game.QueueFree();
                await Frame();
            }
            game = scene.Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = progress;
            Root.AddChild(game);
            game.SetProcess(false);
            await Frame();
        }
        void TutorialProgress(bool finished)
        {
            var cfg = new ConfigFile();
            cfg.SetValue("tutorial", "finished", finished);
            cfg.SetValue("tutorial", "camera_pan_done", true);
            cfg.SetValue("tutorial", "camera_zoom_done", true);
            cfg.SetValue("tutorial", "room_focus_done", true);
            cfg.SetValue("tutorial", "camera_handoff_done", true);
            cfg.SetValue("tutorial", "two_sales", true);
            cfg.Save(progress);
        }
        void Seed(ProgressData data) => ProgressSaveStore.Save(save, data);
        void Craft(string node, Action open)
        {
            open();
            var craft = game.GetNode<EmbroideryGame>(node);
            Check(craft.IsOpen, "Craft must open with enough cash and a matching free display");
            craft.Activate();
            craft.FinishForTesting(true);
            craft.Activate();
            Check(!craft.IsOpen, "Collect must close the minigame");
        }
        try
        {
            const int version = ProgressJson.CurrentVersion;
            Check(WorkshopItemCatalog.Count("atelier") == 5 && WorkshopItemCatalog.Count("music") == 5,
                "Both workshops must select from five equally likely tickets");
            foreach (string category in new[] { "atelier", "music" })
                Check(Enumerable.Range(0, 5).Select(i => WorkshopItemCatalog.Roll(category, i)).Distinct().Count() == 5,
                    "Every uniform ticket must select a different item");
            Check(Enumerable.Range(0, 10).Select(TradeScene.GetItemBasePrice)
                .SequenceEqual(new[] { 25, 45, 40, 70, 75, 115, 195, 115, 175, 295 }),
                "Catalogue prices must remain unchanged");
            var upgrades = new DayUpgradeSession();
            upgrades.Offer(2);
            Check(upgrades.Options.ToHashSet().SetEquals(new[] { DayUpgrade.Cash, DayUpgrade.AtelierPrice }),
                "The first week must deal only existing applicable cards");
            upgrades.Offer(8, musicUnlocked: true);
            Check(upgrades.Options.ToHashSet().SetEquals(new[] { DayUpgrade.Cash, DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice }),
                "The music price card must enter rotation after unlocking");
            upgrades.Offer(2, training: true);
            Check(upgrades.Options.SequenceEqual(new[] { DayUpgrade.AtelierPrice }),
                "The tutorial must work with a single applicable card");
            upgrades.Restore(false, (DayUpgrade)2);
            Check(upgrades.Selected == DayUpgrade.None, "Old removed card IDs must restore as no upgrade");

            TutorialProgress(true);
            Seed(new ProgressData { SaveVersion = version, Score = 2000, DayStarted = true });
            await Reload();
            string panelPath = "World/MusicWorkshop/LockedOverlay/" + WorkshopPanel(game);
            game.TryUnlockMusicWorkshop();
            Check(!game.MusicWorkshopUnlocked && !game.GetNode<Control>(panelPath).IsVisibleInTree() && game.Score == 2000,
                "Music must remain closed in week one regardless of cash");
            Check(!game.HasNode("HUD/Root/EnergyPanel") && !game.HasNode("HUD/InventoryUI") &&
                !game.HasNode(atelier + "/RarityHotspot"), "Removed mechanics must have no runtime controls");

            game.GetItemFromAtelier();
            var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
            embroidery.Activate();
            var slot = game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1");
            Check(slot.GetNode<TextureRect>("Icon").Texture == null, "An unfinished item must remain hidden until Collect");
            embroidery.FinishForTesting(true);
            embroidery.Activate();
            TranslationServer.SetLocale("en");
            slot.EmitSignal(Control.SignalName.MouseEntered);
            Check(game.ShowcasePopup.ItemText.Contains("Quality: S") && game.ShowcasePopup.ItemText.Contains("Quality bonus: +$5") &&
                !game.ShowcasePopup.ItemText.Contains("Rarity"), "Displayed tooltips must localize grade, bonus and price");
            slot.EmitSignal(Control.SignalName.MouseExited);
            TranslationServer.SetLocale("ru");
            Check(game.Score == 1975 && game.ShowcaseItems[0] >= 0 && slot.GetNode<TextureRect>("Icon").Texture != null,
                "First collection must place the paid item in slot one");
            Craft("EmbroideryGame", game.GetItemFromAtelier);
            Craft("EmbroideryGame", game.GetItemFromAtelier);
            Check(game.ShowcaseItems.Take(3).All(item => item >= 0) &&
                game.GetNode<Button>(atelier).Disabled, "Full displays must disable crafting");
            game.GetNode<Button>(atelier).EmitSignal(Control.SignalName.MouseEntered);
            Check(game.GetNode<Label>(atelier + "/PriceHint/Label").Text == "Нет свободных витрин",
                "A full workshop must provide a localized reason");
            game.GetItemFromAtelier();
            Check(!embroidery.IsOpen && game.Score == 1925, "Opening a full workshop must not charge");

            game.OnShowcasePressed(0);
            Check(game.TradeActive && game.ShowcaseItems[0] == game.ItemIndex, "An offer must reserve its source display");
            int firstItem = game.ItemIndex;
            game.RerollCustomer();
            int percent = game.Percentage, rerolls = game.RerollsRemaining;
            game.ReturnItemToShowcase();
            Check(!game.TradeActive && game.ShowcaseItems[0] == firstItem, "Return must restore the same item");
            game.PersistProgress();
            await Reload();
            game.OnShowcasePressed(0);
            Check(game.TradeActive && game.Percentage == percent && game.RerollsRemaining == rerolls &&
                game.ActiveTradeQualityRank == "S", "Returning and reloading must preserve offer, rerolls and quality");
            game.AcceptTrade();
            Check(game.ShowcaseItems[0] == -1 && !game.GetNode<Button>(atelier).Disabled, "A sale frees its display");

            Seed(new ProgressData { SaveVersion = 19, Score = 220, DayStarted = true, TradeActive = true,
                ItemIndex = 5, CustomerIndex = 0, ActiveShowcaseIndex = 0, RerollsRemaining = 1, ActiveTradeGrade = "S",
                ShowcaseItems = [5, -1, -1], ShowcaseGrades = ["B"] });
            await Reload();
            game.PersistProgress();
            var occupied = ProgressSaveStore.Load(save);
            Check(occupied.ShowcaseItems[0] == 5 && occupied.ShowcaseGrades[0] == "B" &&
                occupied.ActiveShowcaseIndex == 1 && occupied.ShowcaseGrades[1] == "S",
                "A legacy trade and another copy of the same item must both survive reload");

            Seed(new ProgressData { SaveVersion = 19, Score = 220, DayStarted = true, MusicWorkshopUnlocked = true,
                ShowcaseItems = [0, 1, 4, 2, -1, -1], ShowcaseGrades = ["A", "B", "C", "S"],
                StoredItems = [5, 6], StoredGrades = ["S", "B"] });
            await Reload();
            game.PersistProgress();
            var saved = ProgressSaveStore.Load(save);
            Check(!game.MusicWorkshopUnlocked && saved.DeferredDisplayItems.Length == 3 &&
                saved.DeferredDisplayItems.Any(item => item.Item == 5 && item.Grade == "S") &&
                saved.DeferredDisplayItems.Any(item => item.Item == 2 && item.Grade == "S"),
                "Full legacy stock and stock behind the first-week curtain must remain saved");
            game.OnShowcasePressed(0);
            game.AcceptTrade();
            game.PersistProgress();
            saved = ProgressSaveStore.Load(save);
            Check(saved.ShowcaseItems[0] == 5 && saved.ShowcaseGrades[0] == "S" && saved.DeferredDisplayItems.Length == 2,
                "The next migrated item must keep its grade when an appropriate display frees up");

            Seed(new ProgressData { SaveVersion = version, Score = 890, Day = 7, DayStarted = true });
            await Reload();
            game.AdvanceToNextDay();
            Check(game.Score == 140 && game.QuotaVisitPending && !game.MusicWorkshopAvailable, "Paying the quota is necessary but insufficient");
            game.AcknowledgeMafiaNotice();
            game.ResumeQuotaVisit();
            game.GetNode<TextureButton>(visitor).EmitSignal(BaseButton.SignalName.Pressed);
            await Frame();
            var dialogue = game.GetNode<DialoguePlayer>("QuotaDialogue/DialoguePlayer");
            for (int i = 0; i < 32 && game.QuotaVisitPending; i++) dialogue.Advance();
            await Frame();
            Check(!game.QuotaVisitPending && game.MusicWorkshopAvailable && game.Score == 140,
                "Completing the visitor's dialogue must unlock access without changing cash");
            TranslationServer.SetLocale("en");
            var button = game.GetNode<MusicWorkshopUnlockPanel>(panelPath).UnlockButton;
            Check(button.IsVisibleInTree() && button.Tr(button.Text) == "Open workshop", "Free unlock button must localize");
            game.TryUnlockMusicWorkshop();
            await Frame();
            var tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            Check(game.MusicWorkshopUnlocked && game.Score == 140 && tutorial.CurrentStep == FirstTradeTutorial.Step.MusicWorkshop,
                "Unlocking is free and starts the music tutorial");
            game.PersistProgress();
            await Reload();
            Check(game.MusicWorkshopUnlocked && !game.QuotaVisitPending && game.Score == 140,
                "Workshop, completed visit and cash must survive reload");
            if (!game.DayStarted) game.EndDay();
            Craft("PolishingGame", game.GetItemFromMusicWorkshop);
            Check(game.ShowcaseItems[3] >= 0 && WorkshopItemCatalog.Items[game.ShowcaseItems[3]].WorkshopId == "music" &&
                game.Score == 90, "Music crafting must place one instrument in the first music display");

            TranslationServer.SetLocale("ru");
            TutorialProgress(false);
            Seed(new ProgressData { SaveVersion = version, Score = 220, DayStarted = true });
            await Reload();
            tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Craft, "Training must point to crafting after camera practice");
            Craft("EmbroideryGame", game.GetItemFromAtelier);
            await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Select && game.ShowcaseItems[0] >= 0,
                "The first crafted item must lead to a showcase inspection");
            game.GetNode<Button>("World/ShowroomLeft/Showcases/Slot1").EmitSignal(Control.SignalName.MouseEntered);
            await Frame();
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.Place, "Hovering over the item must teach selling it");
            game.OnShowcasePressed(0);
            await Frame();
            var next = tutorial.GetNode<Button>("Root/Panel/Content/Actions/Done");
            for (int i = 0; i < 4; i++)
            {
                next.EmitSignal(Button.SignalName.Pressed);
                await Frame();
            }
            Check(tutorial.CurrentStep == FirstTradeTutorial.Step.TimedSale, "Training must lead into the timed sale");
            game._Process(100);
            Check(game.BuyerWaitRemaining == 1, "Practice preserves the last second for the speed bonus");
            game.AcceptTrade();
            Check(game.Accepted == 1 && game.ShowcaseItems[0] < 0, "The timed practice sale must clear its showcase");
            Craft("EmbroideryGame", game.GetItemFromAtelier);
            await Frame();
            game.OnShowcasePressed(0);
            game._Process(100);
            Check(game.TradeActive && game.BuyerWaitRemaining == 0, "A second sale must be available after expiry");
            game.AcceptTrade();
            await Frame();
            Check(game.Accepted == 2 && tutorial.CurrentStep == FirstTradeTutorial.Step.Summary,
                "Both practice sales must lead to the quota lesson");

            Check(DebugBalanceSettings.Fields.All(field => !field.Key.Contains("rarity") &&
                !field.Key.Contains("energy") && !field.Key.EndsWith(".unlock")), "F10 must omit retired settings");
            var panel = Root.GetNode<DebugBalancePanel>("DebugBalance");
            Root.PushInput(new InputEventKey { Keycode = Key.F10, Pressed = true }, true);
            Check(panel.IsOpen && panel.Editor("workshop.atelier.cost") != null, "F10 must still open and expose craft prices");
            panel.Close();
        }
        finally
        {
            Paused = false;
            if (game != null && IsInstanceValid(game)) game.QueueFree();
            TranslationServer.SetLocale(locale);
            foreach (string path in new[] { save, progress })
                if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
        await Frame();
    }
}
