using System;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestRarityColors()
    {
        const string save = "user://rarity_colors_test.json";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        async Task Reload()
        {
            if (game != null)
            {
                game.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            game = scene.Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }

        Color ColorFor(double difference) => difference > 0 ? new Color("268d48") :
            difference < 0 ? new Color("c32b38") : Colors.Black;

        void CheckTables(string locale, int expectedShift = 0)
        {
            foreach (var (room, workshop) in new[] { ("Atelier", "atelier"), ("MusicWorkshop", "music") })
            {
                var panel = game.GetNode<RarityPanel>($"World/{room}/RarityPanel");
                panel.Show();
                var catalog = WorkshopItemCatalog.Items.Select((item, index) => (item, index))
                    .Where(entry => entry.index < WorkshopItemCatalog.ActiveItemCount && entry.item.WorkshopId == workshop).ToArray();
                int[] Tickets(DayUpgradeSession upgrades, int shift) =>
                    Enumerable.Range(0, WorkshopItemCatalog.TotalWeight(workshop, upgrades, shift))
                        .Select(ticket => WorkshopItemCatalog.Roll(workshop, ticket, upgrades, shift)).ToArray();
                var baseline = Tickets(null, 0);
                var current = Tickets(game.DayUpgrades, expectedShift);
                string[] tiers = ["common", "ordinary", "first_class", "exclusive", "unique"];
                for (int i = 0; i < tiers.Length; i++)
                {
                    var ids = catalog.Where(entry => entry.item.RarityId == tiers[i]).Select(entry => entry.index).ToHashSet();
                    double chanceBase = baseline.Count(ids.Contains) * 100.0 / baseline.Length;
                    double chanceNow = current.Count(ids.Contains) * 100.0 / current.Length;
                    var chance = panel.Values.GetNode<Label>($"Chance{i}");
                    Check(chance.Text == $"{chanceNow:0.#}%" && chance.GetThemeColor("font_color") == ColorFor(chanceNow - chanceBase) &&
                        chance.TooltipText.StartsWith(locale == "en" ? "Base chance:" : "Базовый шанс:"),
                        $"{workshop}/{tiers[i]} must display the live odds, their change and a localized baseline");
                    int priceBase = (int)Math.Round(ids.Average(TradeScene.GetItemBasePrice));
                    int priceNow = (int)Math.Round(ids.Average(game.GetItemPrice));
                    var price = panel.Values.GetNode<Label>($"Price{i}");
                    Check(price.Text == $"{priceNow}$" && price.GetThemeColor("font_color") == ColorFor(priceNow - priceBase),
                        $"{workshop}/{tiers[i]} must display the actual average item price and its change");
                }
                panel.Hide();
            }
        }

        try
        {
            using (var file = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                file.StoreString("""{"save_version":17,"day_started":false,"score":220,"checkpoint_score":220,"music_workshop_unlocked":true}""");
            await Reload();
            foreach (var choice in new[] { DayUpgrade.None, DayUpgrade.Atelier, DayUpgrade.Music, DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice })
            {
                game.DayUpgrades.Restore(false, choice, musicUnlocked: true);
                foreach (string locale in new[] { "ru", "en" })
                {
                    TranslationServer.SetLocale(locale);
                    CheckTables(locale);
                }
            }
            game.PersistProgress();
            await Reload();
            Check(game.DayUpgrades.Selected == DayUpgrade.MusicPrice, "Reload must restore the selected card");
            CheckTables("en");
            game.DayUpgrades.Reset();
            CheckTables("en");

            using (var file = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                file.StoreString("""{"save_version":17,"day_started":false,"score":220,"checkpoint_score":220,"invisible_hand_pressure":1,"music_workshop_unlocked":true}""");
            await Reload();
            CheckTables("en", expectedShift: 4);
            CheckTables("en", expectedShift: 4);
            game.PersistProgress();
            Check(ProgressSaveStore.Load(save, 3).InvisibleHandPressure == 1, "Previewing the next draw must never mutate live odds");
        }
        finally
        {
            game?.QueueFree();
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
