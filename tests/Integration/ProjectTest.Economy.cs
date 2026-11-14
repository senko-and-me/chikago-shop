using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestPlaceholderItems()
    {
        const string path = "user://placeholder_items_test.json";
        TradeScene game = null;
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        async Task<TradeScene> Load()
        {
            var loaded = scene.Instantiate<TradeScene>();
            loaded.SavePath = path;
            Root.AddChild(loaded);
            loaded.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            return loaded;
        }
        try
        {
            var config = EconomyConfigProvider.Load();
            foreach (string workshop in new[] { "atelier", "music" })
            {
                int[] counts = new int[WorkshopItemCatalog.Items.Count];
                int multiplier = WorkshopItemCatalog.TotalWeight(workshop) / config.Rarities.Sum(rarity => rarity.Weight);
                Check(multiplier > 0, "Crafting must use a positive integer ticket multiplier");
                for (int ticket = 0; ticket < WorkshopItemCatalog.TotalWeight(workshop); ticket++)
                    counts[WorkshopItemCatalog.Roll(workshop, ticket)]++;
                foreach (var rarity in config.Rarities)
                {
                    var entries = WorkshopItemCatalog.Items.Select((item,index)=>(item,index))
                        .Where(entry => entry.item.WorkshopId == workshop && entry.item.RarityId == rarity.Id).ToArray();
                    Check(entries.Sum(entry => counts[entry.index]) == rarity.Weight * multiplier &&
                        entries.All(entry => counts[entry.index] == rarity.Weight * multiplier / entries.Length &&
                            TradeScene.GetItemBasePrice(entry.index) == rarity.BasePrices[workshop] &&
                            TradeScene.GetItemRerollLimit(entry.index) == rarity.Rerolls),
                        "Rarity odds must stay configured and split equally between its items, with matching prices and rerolls");
                }
                for (int level = 1; level <= 6; level++)
                {
                    var upgrades = new DayUpgradeSession();
                    upgrades.Restore(false, DayUpgrade.None, atelierLevel: level, musicLevel: level);
                    var boosted = new int[WorkshopItemCatalog.Items.Count];
                    for (int ticket = 0; ticket < WorkshopItemCatalog.TotalWeight(workshop); ticket++)
                        boosted[WorkshopItemCatalog.Roll(workshop, ticket, upgrades)]++;
                    foreach (var rarity in config.Rarities)
                    {
                        var entries = WorkshopItemCatalog.Items.Select((item, index) => (item, index))
                            .Where(entry => entry.item.WorkshopId == workshop && entry.item.RarityId == rarity.Id).ToArray();
                        int expected = WorkshopItemCatalog.RarityWeight(workshop, rarity.Id, upgrades) * multiplier;
                        Check(entries.Sum(entry => boosted[entry.index]) == expected &&
                            entries.All(entry => boosted[entry.index] == expected / entries.Length),
                            "All six upgrade levels must preserve displayed rarity odds and equal item shares");
                    }
                }
            }
            Check(TradeScene.GetItemKind(0) == "hat" && TradeScene.GetItemKind(1) == "gloves" &&
                  TradeScene.GetItemKind(2) == "guitar" && TradeScene.GetItemKind(3) == "trumpet",
                "Legacy save indices must keep their original item identity");
            for (int index=0;index<WorkshopItemCatalog.Items.Count;index++)
            {
                var texture = GD.Load<Texture2D>(TradeScene.GetItemArtPath(index));
                var image = texture.GetImage();
                Check(image.GetPixel(0,0).A == 0, "Item artwork must retain transparent padding");
                Check(!TradeScene.GetItemArtPath(index).Contains("placeholder_"), "Every item must use the new artist artwork");
            }
            using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":8,\"day_remaining_seconds\":120,\"score\":1000,\"energy_remaining\":2,\"stored_items\":[" +
                    string.Join(",", Enumerable.Range(0, WorkshopItemCatalog.Items.Count)) + "]}");
            game = await Load();
            for(int index=0;index<WorkshopItemCatalog.Items.Count;index++)
            {
                var inventory = game.GetNode<HBoxContainer>("HUD/InventoryUI/ScrollContainer/HBoxContainer");
                var slot = inventory.GetChild<SlotUI>(0);
                string art = TradeScene.GetItemArtPath(index);
                Check(game.StoredItems[0] == index && slot.GetNode<TextureRect>("Icon").Texture.ResourcePath == art &&
                      slot.ItemInfo == ExpectedItemTooltip(index),
                    "New placeholder identity, art and tooltip must survive inventory reload");
                SelectInventorySlot(game,0);
                game.PlaceItemInShowcase(0);
                Check(game.GetNode<TextureRect>("World/ShowroomLeft/Showcases/Slot1/Icon").Texture.ResourcePath == art,
                    "Showcases must display matching catalogue artwork");
                game._Process(game.BuyerCountdown+0.001);
                var visible = game.GetNode<Node2D>("World/TradingRoom/Item").GetChildren().OfType<Sprite2D>()
                    .Where(sprite => sprite.Visible).ToArray();
                Check(game.ItemIndex == index && visible.Length == 1 && visible[0].Texture.ResourcePath == art &&
                      game.RerollsRemaining == TradeScene.GetItemRerollLimit(index),
                    "Each high-rarity item must have its own visible trading sprite and rarity rerolls");
                game.RerollCustomer();
                int remaining = game.RerollsRemaining;
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await Load();
                Check(game.TradeActive && game.ItemIndex == index && game.RerollsRemaining == remaining,
                    "Active placeholder trades must preserve identity and spent rerolls across reload");
                int cash = game.Score, offer = game.OfferedPrice;
                Check(offer == TradeScene.PriceForPercent(TradeScene.GetItemBasePrice(index), game.Percentage),
                    "High-rarity offers must use the correct workshop base price");
                game.AcceptTrade();
                Check(game.Score == cash+offer && !game.TradeActive,
                    "Placeholder sales must credit their offer and finish the trade");
            }
        }
        finally
        {
            if(game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if(FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
        await ToSignal(this, SignalName.ProcessFrame);
        GD.Print("All 16 items, configured rarity odds, equal per-rarity shares, trading and reload checks passed");
    }

    private static void TestEconomyConfig()
    {
        var config = EconomyConfigProvider.Load();
        Check(config.SchemaVersion == 1 && config.IsProvisional,
            "The game must load the versioned provisional economy config");
        Check(config.Workshops.Count == 2 && config.Rarities.Count == 5 && config.OfferRanges.Count == 3,
            "The game and domain tests must share the complete economy v1 source");
        Check(config.StartingMoney == 220 && config.Workshops[0].SpinCost == 25 &&
              config.Workshops[1].SpinCost == 50 && config.Workshops[1].UnlockCost == 100 &&
		      config.Quotas[0].Amount == 250 &&
              config.Quotas[1].Amount == 450,
            "The runtime must load the provisional paid-spin and raised-quota balance");
    }

    private async Task TestLiveEconomyBalance()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        var config = EconomyConfigProvider.Load();
        TradeScene game = null;
        string locale = GameLocalization.Language;
        try
        {
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            game = await CreateGame(scene);
            Check(game.StartingMoney == config.StartingMoney && game.FirstQuota == 250 && game.SecondQuota == 450,
                "The live game must read the same starting money and quotas as the simulator");
            foreach (string language in new[] { "ru", "en" })
            {
                TranslationServer.SetLocale(language);
                game.NewGame();
                Check(game.DayRemainingSeconds == 90 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "90",
                    "The first day must start at ninety seconds before unlocking music");
                foreach (string workshop in new[] { "atelier", "music" })
                {
                    string room = workshop == "atelier" ? "Atelier" : "MusicWorkshop";
                    int cost = WorkshopItemCatalog.CraftCost(workshop);
                    if (workshop == "music")
                    {
                        game.NewGame(); game.Score = 1000; game.TryUnlockMusicWorkshop();
                        Check(game.DayRemainingSeconds == 90, "Unlocking music must not change the ninety-second baseline");
                    }
                    Check(game.GetNode<Label>($"World/{room}/HintLabel").Text.Contains(cost.ToString()) &&
                          game.GetNode<Label>($"World/{room}/GetItemButton/PriceHint/Label").Text.Contains(cost.ToString()),
                        "RU/EN crafting labels must show the configured single-item cost");
                    game.Score = cost - 1; game.RefreshItemButtons();
                    var button = game.GetNode<Button>($"World/{room}/GetItemButton");
                    Check(button.Text == string.Empty,
                        "Illustrated craft buttons must not duplicate their image lettering after RU/EN changes");
                    Check(button.Disabled, "Crafting must be unavailable one dollar below the real cost");
                    if (workshop == "atelier") game.GetItemFromAtelier(); else game.GetItemFromMusicWorkshop();
                    var mini = game.GetNode<EmbroideryGame>(workshop == "atelier" ? "EmbroideryGame" : "PolishingGame");
                    Check(!mini.IsOpen, "The minigame must use the new affordability threshold too");
                    game.Score = cost; game.RefreshItemButtons();
                    Check(!button.Disabled, "Exact funds must enable single-item crafting");
                    button.EmitSignal(Button.SignalName.Pressed);
                    Check(mini.IsOpen, "The real craft button must open the minigame at the configured cost");
                    int items = game.StoredItems.Length, energy = game.EnergyRemaining;
                    mini.Activate(); mini.Activate();
                    Check(game.Score == 0 && game.StoredItems.Length == items + 1 && game.EnergyRemaining == energy - 1,
                        "Starting a craft must charge once for exactly one item and one energy");
                    mini._Process(10); mini.Activate();
                    Check(!Paused && !mini.IsOpen && game.DayRemainingSeconds == 80,
                        "Crafting must consume its actual ten seconds within the ninety-second day");
                }
            }
            game.NewGame(); game.CurrentDay = 4; game.Score = 250;
            game.EndDay(); game.EndDay();
            Check(game.CurrentDay == 5 && game.QuotaStage == 1 && game.Score == 0 && game.CheckpointDay == 5,
                "The first quota must be exactly250 at the end of day four");
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            await WaitForDayCardAnimation(game);
            game.CurrentDay = 7; game.Score = 450; game.EndDay(); game.EndDay();
            Check(game.Completed && game.Score == 0 && !game.DayTransitionOpen,
                "The final quota must be exactly450 at the end of day seven");
            game.NewGame(); game.CurrentDay = 4; game.Score = 249; game.EndDay(); game.EndDay();
            Check(game.Failed && !game.DayTransitionOpen, "One dollar below the first quota must remain a real loss");
            game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); game = null;
            using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":12,\"day\":1,\"score\":220,\"energy_remaining\":3,\"day_remaining_seconds\":30}");
            game = await CreateGame(scene);
            Check(game.DayRemainingSeconds == 45 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "45",
                "Half of a legacy sixty-second first day must restore as half of ninety seconds");
        }
        finally
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            TranslationServer.SetLocale(locale);
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
