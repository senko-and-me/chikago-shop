using System;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestEnergyBulbs()
    {
        string locale = TranslationServer.GetLocale();
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var bulbs = game.GetNode<EnergyBulbsPresentation>("HUD/Root/EnergyPanel/EnergyBulbs");
        var label = game.GetNode<Label>("HUD/Root/EnergyPanel/EnergyLabel");
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720), new Vector2I(1000, 800) })
            foreach (string language in new[] { "ru", "en" })
            foreach (int configuration in new[] { 0, 1, 2 })
            {
                Root.Size = size;
                TranslationServer.SetLocale(language);
                game.NewGame();
                game.Score = 10000;
                game.EndDay();
                if (configuration > 0) game.TryUnlockMusicWorkshop();
                if (configuration == 2)
                {
                    game.DayUpgrades.Offer(2);
                    Check(game.DayUpgrades.TryPurchase(DayUpgrade.Energy, game.Score, out int balance),
                        "The fixture must buy the actual energy upgrade before checking expanded capacity");
                    game.Score = balance;
                }
                int capacity = game.EnergyCapacity;
                Check(capacity == (configuration == 0 ? 3 : configuration == 1 ? 6 : 7),
                    "Exercise the real initial, unlocked and energy-upgraded HUD capacities");
                foreach (int remaining in new[] { capacity, capacity / 2, 0 })
                {
                    game.EnergyRemaining = remaining;
                    game.RefreshDayPresentation();
                    Paused = remaining == 0;
                    using var image = await CapturePresentation();
                    Check(bulbs.BulbCount == capacity && bulbs.LitCount == remaining,
                        "Bulbs must follow current available energy, workshop unlock and energy upgrade, also while paused");
                    Check(!label.Visible && bulbs.TooltipText == label.Text &&
                          label.Text.Contains($"{remaining} / {capacity}") &&
                          label.Text.Contains(language == "ru" ? "Энергия" : "Energy"),
                        "Replace the visible energy text while keeping a localized numeric tooltip");
                    Rect2 area = new(Vector2.Zero, bulbs.Size);
                    Vector2 pixels = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
                    for (int index = 0; index < capacity; index++)
                    {
                        Rect2 rect = bulbs.BulbRect(index);
                        Check(area.Encloses(rect) && rect.Size.Y >= 24 && rect.Size.X >= 14,
                            "Every bulb must fit the HUD panel with readable proportions, including seven energy");
                        if (index > 0) Check(rect.Position.X - bulbs.BulbRect(index - 1).End.X >= 4,
                            "Energy bulbs must retain distinct spacing");
                        Vector2 sample = bulbs.GetGlobalTransformWithCanvas() * (rect.Position + rect.Size * new Vector2(0.5f, 0.25f));
                        Color color = image.GetPixelv(new Vector2I((int)(sample.X * pixels.X), (int)(sample.Y * pixels.Y)));
                        Check(!HasChroma(color), "Energy artwork must preserve the game's monochrome presentation");
                        Check(index < remaining ? color.R > 0.65f : color.R < 0.4f,
                            "Remaining bulbs must be visibly bright and spent bulbs visibly dark");
                    }
                    if (size.X == 1280 && language == "ru" && configuration == 2 && remaining == capacity / 2)
                        image.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-hf190-energy-bulbs.png"));
                }
                Paused = false;
            }
            game.NewGame();
            game.Score = 10000;
            game.EndDay();
            var craft = game.GetNode<EmbroideryGame>("EmbroideryGame");
            craft.Open();
            craft.SetProcess(false);
            int before = game.EnergyRemaining;
            craft.Activate();
            using (await CapturePresentation())
                Check(bulbs.LitCount == before - 1 && game.EnergyRemaining == before - 1,
                    "Starting a real craft must darken exactly one bulb immediately while the modal pauses the game");
            craft.FinishForTesting(true);
            craft.Activate();
        }
        finally
        {
            Paused = false;
            Root.Size = originalSize;
            TranslationServer.SetLocale(locale);
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
        }
    }
}
