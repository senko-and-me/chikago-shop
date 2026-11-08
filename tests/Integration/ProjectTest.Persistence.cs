using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestNewGameFunds()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        var game = scene.Instantiate<TradeScene>();
        try
        {
            Check(game.StartingMoney == 220 && game.FirstQuota == 250 && game.SecondQuota == 450 &&
                game.SavePath == "user://trade_progress.json",
                "Packed scene must preserve configured starting funds, quotas and save path instead of overriding them with null");
            game.SavePath = "user://new_game_funds_test.json";
            if (FileAccess.FileExists(game.SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(game.SavePath));
            Root.AddChild(game);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.Score == 220 && game.CheckpointScore == 220 && game.CurrentDay == 1,
                "A fresh game must start with exactly220 dollars and a matching checkpoint");
            var rarity = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
            Check(new[] { rarity.Price0, rarity.Price1, rarity.Price2, rarity.Price3, rarity.Price4 }
                .SequenceEqual(new[] { 30, 50, 80, 120, 200 }),
                "Scene null overrides must not zero the atelier price table");
            game.Score = 0;
            game.NewGame();
            Check(game.Score == 220 && game.CheckpointScore == 220 && game.CurrentDay == 1 &&
                !game.GetNode<Button>("World/Atelier/GetItemButton").Disabled,
                "New Game must restore220 and enable affordable crafting after a depleted balance");
            var saved = ProgressSaveStore.Load(game.SavePath, 3);
            Check(saved != null && saved.Score == 220 && saved.CheckpointScore == 220,
                "New Game must persist220 for both current funds and the checkpoint");
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = scene.Instantiate<TradeScene>();
            game.SavePath = "user://new_game_funds_test.json";
            Root.AddChild(game);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.Score == 220 && game.CheckpointScore == 220,
                "Reloading the new game must retain220 dollars");
        }
        finally
        {
            const string path = "user://new_game_funds_test.json";
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            game.QueueFree();
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestSaveCompatibility()
    {
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        try
        {
            foreach (var sample in new (int Version, bool Music, int SavedEnergy, double Seconds, bool Bonus, int Energy, double Remaining)[]
            {
                (1, false, 0, 60d, false, 3, 90d),
                (5, false, 0, 60d, false, 3, 90d),
                (6, true, 0, 60d, false, 2, 90d),
                (7, true, 1, 60d, false, 5, 90d),
                (8, true, 1, 60d, false, 5, 45d),
                (9, true, 1, 75d, true, 5, 60d),
                (11, false, 0, 30d, false, 2, 45d),
                (12, false, 1, 30d, false, 1, 45d),
                (13, false, 1, 30d, false, 1, 30d)
            })
            {
                ProgressSaveStore.Save(SavePath, new ProgressData
                {
                    SaveVersion = sample.Version, Score = 333, EnergyRemaining = sample.SavedEnergy,
                    DayRemainingSeconds = sample.Seconds, MusicWorkshopUnlocked = sample.Music,
                    SelectedUpgrade = sample.Bonus ? (int)DayUpgrade.Time : 0,
                    StoredItems = [-1, 0, 999], TradeActive = true, ItemIndex = 2,
                    CustomerIndex = 1, RerollsRemaining = 1
                });
                game = await CreateGame(scene);
                Check(game.Score == 333 && game.EnergyRemaining == sample.Energy &&
                      Math.Abs(game.DayRemainingSeconds - sample.Remaining) < 1 &&
                      game.StoredItems.SequenceEqual(new[] { 0 }) && game.TradeActive &&
                      game.CustomerIndex == (sample.Version < 3 ? 3 : 1) &&
                      game.RerollsRemaining == (sample.Version < 2 ? TradeScene.GetItemRerollLimit(2) : 1),
                    $"Version {sample.Version} must preserve legacy energy, timer, inventory and trade interpretation");
                double seconds = game.DayRemainingSeconds;
                game.PersistProgress();
                var saved = ProgressSaveStore.Load(SavePath, 3);
                Check(saved.SaveVersion == 13 && saved.Score == 333 && saved.TradeActive &&
                      saved.StoredItems.SequenceEqual(new[] { 0 }) && saved.DayRemainingSeconds == seconds,
                    "Migrated state must write the existing version13 schema without losing the active trade");
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
                game = await CreateGame(scene);
                Check(game.TradeActive && Math.Abs(game.DayRemainingSeconds - seconds) < 1,
                    "A second reload must not apply legacy time conversion again");
                game.QueueFree();
                game = null;
                await ToSignal(this, SignalName.ProcessFrame);
            }

            ProgressSaveStore.Save(SavePath, new ProgressData
            {
                SaveVersion = 13, Day = 6, Score = 333, EnergyRemaining = 2,
                MusicWorkshopUnlocked = true, QuotaStage = 1,
                CheckpointDay = 5, CheckpointScore = 111, CheckpointEnergy = 5,
                CheckpointMusicWorkshopUnlocked = true, CheckpointItems = [2, 5],
                CheckpointShowcaseItems = [-1, 4, -1, -1, -1, -1],
                CheckpointAtelierPriceLevel = 2, CheckpointMusicPriceLevel = 3,
                DayRemainingSeconds = 25
            });
            game = await CreateGame(scene);
            game.ReturnToCheckpoint();
            Check(game.CurrentDay == 5 && game.Score == 111 && game.EnergyRemaining == 5 &&
                  game.StoredItems.SequenceEqual(new[] { 2, 5 }) && game.ShowcaseItems[1] == 4 &&
                  game.DayUpgrades.AtelierPriceLevel == 2 && game.DayUpgrades.MusicPriceLevel == 3,
                "Checkpoint cash, energy, stock and price upgrades must restore through the extracted adapter");
            game.QueueFree();
            game = null;
            await ToSignal(this, SignalName.ProcessFrame);
            using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write)) file.StoreString("{broken");
            game = await CreateGame(scene);
            Check(game.Score == game.StartingMoney && game.CurrentDay == 1 && game.StoredItems.Length == 0,
                "Malformed JSON must fall back to a new game");
            Check(ProgressSaveStore.Load("", 3) == null, "An empty path must disable save I/O");
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestProgress()
    {
        string absolutePath = ProjectSettings.GlobalizePath(SavePath);
        if (FileAccess.FileExists(SavePath)) DirAccess.RemoveAbsolute(absolutePath);
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn")
            ?? throw new Exception("The trade scene must load");
        var economy = EconomyConfigProvider.Load();
        int initialEnergy = economy.Days.EnergyBeforeMusicUnlock;
        int musicEnergy = economy.Days.EnergyAfterMusicUnlock;
        int craftSpending = WorkshopItemCatalog.CraftCost("atelier") + WorkshopItemCatalog.CraftCost("music");
        int unlockCost = economy.Workshops.Single(workshop => workshop.Id == "music").UnlockCost;
        TradeScene game = await CreateGame(scene);
		game.Score = 500;
		game.RefreshItemButtons();
		game.TryUnlockMusicWorkshop();
        CraftAtelier(game);
        CraftMusic(game);
        int[] itemsBeforePlacement = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(3);
        Check(!game.TradeActive && game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(1)) &&
              game.ShowcaseItems[3] == itemsBeforePlacement[0],
            "Placing must move the selected item into a showcase without starting a trade");
        double savedDelay = game.BuyerCountdown;
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(!game.TradeActive && Math.Abs(game.BuyerCountdown - savedDelay) < 0.00001 &&
              game.ShowcaseItems[3] == itemsBeforePlacement[0],
            "Reload must restore the pending buyer delay without starting an immediate trade");
        CheckInventoryTooltips(game);
        game._Process(game.BuyerCountdown + 0.001);
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0] && game.ShowcaseItems[3] == -1 &&
              game.StoredItems[0] == itemsBeforePlacement[1] && game.EnergyRemaining == musicEnergy - 2 &&
		      game.Score == 500 - unlockCost - craftSpending && game.MusicWorkshopUnlocked,
            "The delayed trade, inventory, unlocked room, cash and spent energy must survive a reload");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0] && game.ShowcaseItems[3] == -1 &&
              game.GetNode<Button>("World/Showroom/Showcases/Slot4").Disabled &&
              game.GetNode<Label>("World/Showroom/Showcases/Slot4/ItemLabel").Text == "СВОБОДНО",
            "An active showcase trade must survive a reload");
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(5);
        Check(game.TradeActive && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.ShowcaseItems[3] == -1,
            "Other showcases must remain available during a restored trade");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.TradeActive && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.GetNode<Button>("World/Showroom/Showcases/Slot4").Disabled,
            "Stocking during a trade must survive another reload");
        game.DeclineTrade();
        Check(game.TradeActive && game.ItemIndex == itemsBeforePlacement[0] &&
              game.ShowcaseItems[3] == -1 && game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(2)),
            "The red button must reroll a restored trade without returning or moving goods");

        game.CurrentDay = 4;
        game.AcceptTrade();
        game.Score = game.FirstQuota - 1;
        AdvanceDayForTest(game);
        Check(game.Failed && !game.TradeActive && game.ShowcaseItems[3] == -1 &&
              game.ShowcaseItems[5] == itemsBeforePlacement[1] &&
              game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(2)),
            "Failure at the deadline must preserve unsold goods and keep the sold item removed");
        CraftAtelier(game);
        game._Input(new InputEventKey { Keycode = Key.R, Pressed = true });
        Check(game.StoredItems.SequenceEqual(itemsBeforePlacement.Skip(2)),
            "Failure and R must not create items");
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 1 && game.Score == game.StartingMoney && game.EnergyRemaining == initialEnergy &&
		      !game.MusicWorkshopUnlocked && game.StoredItems.Length == 0 &&
		      game.ShowcaseItems.All(item => item == -1),
            "First failure must restore the empty day-one checkpoint");

		game.Score = 500;
		game.RefreshItemButtons();
		game.TryUnlockMusicWorkshop();
        CraftAtelier(game);
        CraftMusic(game);
        int[] checkpointItems = game.StoredItems;
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(2);
        game.CurrentDay = 4;
        game.Score = game.FirstQuota + 12;
        AdvanceDayForTest(game);
        Check(game.QuotaStage == 1 && game.CurrentDay == 5 && game.Score == 12 &&
		      game.EnergyRemaining == musicEnergy && game.MusicWorkshopUnlocked,
            "Paying the first quota must open day five with the remaining cash");
        Check(game.StoredItems.SequenceEqual(checkpointItems.Skip(1)) &&
              game.ShowcaseItems[2] == checkpointItems[0] && !game.TradeActive,
            "The checkpoint must retain unsold showroom items");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(!game.TradeActive && game.BuyerCountdown is > 0 and <= 2,
            "Reloaded checkpoint goods must wait for a buyer");
        game._Process(game.BuyerCountdown + 0.001);
        Check(game.CurrentDay == 5 && game.Score == 12 && game.EnergyRemaining == musicEnergy &&
		      game.MusicWorkshopUnlocked &&
              game.TradeActive && game.ItemIndex == checkpointItems[0] && game.ShowcaseItems[2] == -1 &&
              game.StoredItems.SequenceEqual(checkpointItems.Skip(1)),
            "The delayed showroom trade, inventory, cash and restored energy must survive a scene reload");
        game.AcceptTrade();
        Check(!game.TradeActive && game.ShowcaseItems[2] == -1,
            "The selected showroom item must leave through a completed sale");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.ShowcaseItems[2] == -1 && game.StoredItems.SequenceEqual(checkpointItems.Skip(1)),
            "A sold showroom item must stay sold after reload");
        game.ReturnToCheckpoint();
        Check(game.ShowcaseItems[2] == checkpointItems[0],
            "Returning to the checkpoint must restore its showroom contents");
        SelectInventorySlot(game, 0);
        game.PlaceItemInShowcase(4);
        game.CurrentDay = 6;
        game.Score = 1000;
        game.ReturnToCheckpoint();
        Check(game.CurrentDay == 5 && game.Score == 12 && game.EnergyRemaining == musicEnergy &&
		      game.MusicWorkshopUnlocked && !game.TradeActive &&
              game.StoredItems.SequenceEqual(checkpointItems.Skip(1)) &&
              game.ShowcaseItems[2] == checkpointItems[0] && game.ShowcaseItems[4] == -1,
            "Checkpoint must restore cash, inventory and showroom items");

        game.CurrentDay = 7;
        game.Score = game.SecondQuota;
        AdvanceDayForTest(game);
        Check(game.Completed && game.QuotaStage == 2 && game.Score == 0,
            "Paying the second quota must finish the game");
        game.NewGame();
        Check(game.CurrentDay == 1 && game.Score == game.StartingMoney && game.EnergyRemaining == initialEnergy &&
		      !game.MusicWorkshopUnlocked &&
              game.StoredItems.Length == 0 && !game.TradeActive &&
              game.ShowcaseItems.All(item => item == -1),
            "New game must reset progress without giving a free item");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);

        foreach (double savedCountdown in new[] { 0d, 0.125d })
        {
            using (var waitingSave = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
                waitingSave.StoreString(savedCountdown == 0
                    ? "{\"save_version\":7,\"showcase_items\":[0],\"buyer_countdown\":0}"
                    : "{\"save_version\":7,\"showcase_items\":[0],\"buyer_countdown\":0.125}");
            game = await CreateGame(scene);
            Check(!game.TradeActive && (savedCountdown == 0
                    ? game.BuyerCountdown is >= 0.5 and <= 2
                    : game.BuyerCountdown == savedCountdown),
                "Legacy zero timers must gain a delay; a positive remainder below 0.5 must survive reload");
            game._Process(game.BuyerCountdown - 0.001);
            Check(!game.TradeActive, "Reload must not bypass the remaining delay");
            game._Process(0.002);
            Check(game.TradeActive && game.ItemIndex == 0, "Reloaded waiting goods must reach a buyer after the delay");
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
        }

        using (var oldSave = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write))
            oldSave.StoreString("{\"trade_active\":true,\"item_index\":2,\"customer_index\":1,\"percentage\":0,\"rerolls_remaining\":1}");
        game = await CreateGame(scene);
        Check(game.TradeActive && game.StoredItems.Length == 0, "An older active trade must be restored without duplicating its item");
        Check(game.RerollsRemaining == 3 && game.CustomerIndex == 3,
            "Old reroll limits and customer appearance must migrate");
        game.RerollCustomer();
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        game = await CreateGame(scene);
        Check(game.RerollsRemaining == 2, "A current save must preserve spent rerolls");
        game.QueueFree();
        await ToSignal(this, SignalName.ProcessFrame);
        DirAccess.RemoveAbsolute(absolutePath);
        GD.Print("Inventory, save and quota checks passed");
    }
}
