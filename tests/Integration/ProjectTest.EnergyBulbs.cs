using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestEnergyBulbs()
    {
        string locale = TranslationServer.GetLocale();
        Vector2I size = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        var panel = game.GetNode<PanelContainer>("HUD/Root/EnergyPanel");
        var bulbs = panel.GetNode<EnergyBulbsPresentation>("EnergyBulbs");
        Check(panel.SceneFilePath == "res://scenes/UI/EnergyPanel.tscn" && bulbs.GetChildCount() == 7 && panel.GetThemeStylebox("panel") is StyleBoxTexture,
            "The editor must contain the reusable seven-slot paper energy panel");
        for (int i = 0; i < 7; i++)
        {
            var bulb = bulbs.GetChild<TextureRect>(i);
            Check(bulb.Visible == (i < 3) && bulb.Texture is AtlasTexture art && art.Region == new Rect2(478, 168, 1057, 1682),
                "Editor preview must show three cropped bulbs and hide unused capacity");
        }
        game.SavePath = "";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        try
        {
            foreach (var window in new[] { new Vector2I(960, 540), new Vector2I(1280, 720), new Vector2I(1000, 800) })
            foreach (string language in new[] { "ru", "en" })
            foreach (int configuration in new[] { 0, 1, 2 })
            {
                Root.Size = window;
                TranslationServer.SetLocale(language);
                game.NewGame();
                game.Score = 10000;
                game.EndDay();
                if (configuration > 0) game.TryUnlockMusicWorkshop();
                if (configuration == 2)
                {
                    game.DayUpgrades.Offer(2, true);
                    Check(game.DayUpgrades.TryPurchase(DayUpgrade.Energy, game.Score, out int cash), "Buy the real energy card");
                    game.Score = cash;
                    game.EnergyRemaining = game.EnergyCapacity;
                }
                int capacity = configuration == 0 ? 3 : configuration == 1 ? 6 : 7;
                Check(game.EnergyCapacity == capacity, "Initial, unlocked and upgraded capacity must remain 3/6/7");
                foreach (int remaining in new[] { capacity, capacity - 1, 0 })
                {
                    game.EnergyRemaining = remaining;
                    game.RefreshDayPresentation();
                    Paused = remaining == 0;
                    using (await CapturePresentation()) { }
                    Check(bulbs.BulbCount == capacity && bulbs.LitCount == remaining && Root.GetVisibleRect().Encloses(panel.GetGlobalRect()),
                        "Energy must update while paused and fit every supported window");
                    Check(!panel.GetNode<Label>("EnergyLabel").Visible && bulbs.TooltipText.Contains($"{remaining} / {capacity}") &&
                        bulbs.TooltipText.Contains(language == "ru" ? "Энергия" : "Energy"), "Numeric energy must remain localized in the tooltip");
                    for (int i = 0; i < 7; i++)
                    {
                        var bulb = bulbs.GetChild<TextureRect>(i);
                        Check(bulb.Visible == (i < capacity), "Only capacity slots must be visible");
                        if (!bulb.Visible) continue;
                        var art = (AtlasTexture)bulb.Texture;
                        Check(art.Atlas.ResourcePath == (i < remaining ? "res://art/ui/energy_bulb.png" : "res://art/ui/energy_bulb_empty.png"),
                            "Spending energy must replace full bulbs with empty artwork from right to left");
                        Check(panel.GetGlobalRect().Encloses(bulb.GetGlobalRect()) && bulb.Size.X > 0 && bulb.Size.Y > 0,
                            "All seven full or empty bulbs must fit inside their paper panel");
                    }
                    Paused = false;
                }
            }
        }
        finally
        {
            Paused = false;
            game.QueueFree();
            Root.Size = size;
            TranslationServer.SetLocale(locale);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}