using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestCanonicalMigration()
    {
        const string save = "user://canonical_migration_test.json";
        TradeScene game = null;
        async Task Reload()
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            string[] kinds = ["hat", "vest", "guitar", "drum", "gloves", "matteo_hat", "lorgnette", "banjo", "violin", "tuba"];
            Check(WorkshopItemCatalog.Items.Select(item => item.Kind).SequenceEqual(kinds), "The canonical catalogue must contain exactly ten stable kinds");
            Check(WorkshopItemCatalog.Items.All(item => ResourceLoader.Exists(item.ArtPath)), "Every canonical item must resolve its renamed artwork");
            ProgressSaveStore.Save(save, new ProgressData
            {
                SaveVersion = 18, Day = 8, Score = 2333, QuotaStage = 1, MusicWorkshopUnlocked = true,
                QuotaVisit = QuotaVisitStage.Rewarded, CheckpointQuotaVisit = QuotaVisitStage.Rewarded,
                DayStarted = true, DayRemainingSeconds = 42, EnergyRemaining = 4,
                StoredItems = [10, 11, 12, 13, 14, 15], StoredGrades = ["S", "A", "B", "C", "D", "S"],
                ShowcaseItems = [-1, 10, 11, 12, 13, 14], ItemIndex = 15, ActiveShowcaseIndex = 0,
                TradeActive = true, CustomerIndex = 0, RerollsRemaining = 1, ActiveTradeGrade = "A", BuyerWaitRemaining = 12,
                CheckpointDay = 8, CheckpointScore = 1000, CheckpointMusicWorkshopUnlocked = true,
                CheckpointEnergy = 6, CheckpointItems = [15, 0, 11], CheckpointShowcaseItems = [10, 12, -1],
                CheckpointGrades = ["A", "B", "C"]
            });
            await Reload();
            Check(game.StoredItems.SequenceEqual(new[] { 1, 4, 5, 2, 8, 9 }) && game.TradeActive && game.ItemIndex == 9 &&
                game.Score == 2333 && game.EnergyRemaining == 4 && game.CurrentDay == 8 && game.ActiveTradeQualityRank == "A",
                "Loading version18 must preserve every item copy, active trade, cash, energy and day while resolving legacy aliases");
            game.PersistProgress();
            var mapped = ProgressSaveStore.Load(save, 3);
            Check(mapped.SaveVersion == 19 && mapped.StoredGrades.SequenceEqual(new[] { "S", "A", "B", "C", "D", "S" }) &&
                mapped.CheckpointItems.SequenceEqual(new[] { 9, 0, 4 }) && mapped.CheckpointGrades.SequenceEqual(new[] { "A", "B", "C" }),
                "The new schema must preserve per-copy quality and checkpoint stock");
            await Reload();
            Check(game.StoredItems.SequenceEqual(mapped.StoredItems) && game.ItemIndex == 9 && game.Score == 2333,
                "Reloading version19 must never apply the index mapping twice");

            string fixture = OS.GetEnvironment("CHIKAGO_SAVE_FIXTURE");
            if (!string.IsNullOrEmpty(fixture) && FileAccess.FileExists(fixture))
            {
                var original = ProgressJson.Deserialize(FileAccess.GetFileAsString(fixture), 3);
                Check(original != null && original.SaveVersion <= 19, "The backed-up player fixture must have a supported schema");
                ProgressSaveStore.Save(save, original);
                await Reload();
                game.PersistProgress();
                var restored = ProgressSaveStore.Load(save, 3);
                Check(restored.Score == original.Score && restored.Accepted == original.Accepted && restored.Declined == original.Declined &&
                    restored.StoredItems.Length == original.StoredItems.Length && restored.CheckpointItems.Length == original.CheckpointItems.Length &&
                    restored.Failed == original.Failed && restored.Completed == original.Completed,
                    "A copy of the real player save must retain money, statistics, inventory counts, checkpoint and terminal state");
                Check(restored.StoredItems.All(index => index is >= 0 and < 10), "Migrated real inventory must use canonical catalogue indices");
            }
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
