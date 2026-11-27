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
    private async Task TestEnergyRemoval()
    {
        const string path = "user://energy_removal_test.json";
        string locale = TranslationServer.GetLocale();
        TradeScene game = null;
        IEnumerable<Node> Descendants(Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                yield return child;
                foreach (Node nested in Descendants(child)) yield return nested;
            }
        }
        void CheckNoEnergyCopy(Node node)
        {
            foreach (Node child in Descendants(node))
            {
                string text = child is Label label ? label.Text : child is Button button ? button.Text : "";
                Check(!text.Contains("энерг", StringComparison.OrdinalIgnoreCase) &&
                    !text.Contains("energy", StringComparison.OrdinalIgnoreCase),
                    $"Energy must not appear in player-facing crafting copy: {child.GetPath()}");
            }
        }
        try
        {
            ProgressSaveStore.Save(path, new ProgressData
            {
                SaveVersion = 18, Score = 10000, Day = 2, DayStarted = true,
                EnergyRemaining = 0, DayRemainingSeconds = 90,
                SelectedUpgrade = (int)DayUpgrade.Energy,
                CheckpointDay = 1, CheckpointScore = 220, CheckpointEnergy = 0
            });
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = path;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            var mini = game.GetNode<EmbroideryGame>("EmbroideryGame");
            mini.SetProcess(false);
            game.GetNode<EmbroideryGame>("PolishingGame").SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.LoadedSavedGame && game.EnergyRemaining == 0 && game.StoredItems.Length == 0,
                "A legacy zero-energy save must load with its inventory intact");
            Check(game.DayUpgrades.Selected == DayUpgrade.None,
                "A previously purchased energy card must restore as a retired upgrade");
            Check(!game.HasNode("HUD/Root/EnergyPanel"), "The energy HUD must be removed from the scene");
            game._Process(0);
            var waiting = game.GetNode<Control>("World/TradingRoom/BuyerWaitingThought");
            Check(waiting.Visible && !game.TradeActive,
                "An empty-stock buyer must wait even when the legacy energy counter is zero");

            foreach (string language in new[] { "ru", "en" })
            {
                TranslationServer.SetLocale(language);
                game.Crafting.OpenCatalog("atelier");
                Check(game.Crafting.IsCatalogOpen && !game.Crafting.Catalog.ItemButton("hat").Disabled,
                    "Funded crafting must remain selectable at zero legacy energy");
                CheckNoEnergyCopy(game.Crafting.Catalog);
                CheckNoEnergyCopy(game.GetNode("HUD"));
                game.Crafting.Catalog.Close();
                foreach (string room in new[] { "Atelier", "MusicWorkshop" })
                    CheckNoEnergyCopy(game.GetNode("World/" + room + "/GetItemButton"));
            }

            int funds = game.Score;
            double remaining = game.DayRemainingSeconds;
            for (int attempt = 0; attempt < 7; attempt++)
            {
                game.Crafting.OpenCatalog("atelier");
                Check(game.Crafting.Select("hat"), "Each funded attempt must prepare at zero energy");
                CheckNoEnergyCopy(mini);
                mini.Activate();
                Check(mini.ActiveSession.State == EmbroideryState.Playing && game.EnergyRemaining == 0 &&
                    game.StoredItems.Length == attempt + 1 && game.Score == funds - (attempt + 1) * 20,
                    "Starting must debit only money and create exactly one selected item");
                mini._Process(.25);
                mini.FinishForTesting(true);
                mini.Activate();
            }
            Check(Math.Abs(game.DayRemainingSeconds - (remaining - 1.75)) < .001,
                "Repeated crafting must still consume ordinary active day time");
            var saved = ProgressSaveStore.Load(path, 3);
            Check(saved != null && saved.EnergyRemaining == 0 && saved.StoredItems.Length == 7 &&
                saved.Score == game.Score, "Energy-free attempts must persist their paid items and cash");

            game.Score = 0;
            game.Crafting.OpenCatalog("atelier");
            Check(game.Crafting.Catalog.ItemButton("hat").Disabled && !game.Crafting.Select("hat"),
                "Removing energy must retain the money restriction");
            game.Crafting.Catalog.Close();
            game.Score = 10000;
            game.Crafting.OpenCatalog("music");
            Check(!game.Crafting.IsCatalogOpen && !game.Crafting.Select("guitar"),
                "Removing energy must retain the locked-workshop restriction");
            game.EndDay();
            game.Crafting.OpenCatalog("atelier");
            Check(!game.Crafting.IsCatalogOpen, "A closed day must still prohibit crafting");

            game.DayUpgrades.Offer(3, true);
            Check(!game.DayUpgrades.Options.Contains(DayUpgrade.Energy) &&
                !game.DayUpgrades.TryPurchase(DayUpgrade.Energy, 10000, out _),
                "The retired energy card must neither be offered nor purchasable");
            game.ReturnToCheckpoint();
            Check(!game.HasNode("HUD/Root/EnergyPanel"), "Checkpoint restoration must not restore energy UI");
            game.NewGame();
            Check(!game.HasNode("HUD/Root/EnergyPanel"), "New Game must not restore energy UI");
            game.Crafting.OpenCatalog("atelier");
            Check(!game.Crafting.IsCatalogOpen, "A new waiting day must still require starting the day");
            GD.Print("Energy removal: legacy zero saves, seven paid crafts, buyer waiting, retired cards and HUD checks passed");
        }
        finally
        {
            Paused = false;
            TranslationServer.SetLocale(locale);
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
    }
}
