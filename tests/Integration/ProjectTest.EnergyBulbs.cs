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
        var authoredPanel = game.GetNode<PanelContainer>("HUD/Root/EnergyPanel");
        var authoredBulbs = authoredPanel.GetNode<EnergyBulbsPresentation>("EnergyBulbs");
        Check(authoredPanel.SceneFilePath == "res://scenes/UI/EnergyPanel.tscn" &&
            authoredBulbs.GetChildCount() == 7 && !authoredPanel.GetNode<Label>("EnergyLabel").Visible,
            "The packed trade scene must already contain the reusable energy panel and seven authored slots before Ready");
        for (int index = 0; index < 7; index++)
        {
            var slot = authoredBulbs.GetChild<TextureRect>(index);
            Check(slot.Visible == (index < 3) && slot.SceneFilePath == "res://scenes/UI/EnergyBulb.tscn" &&
                slot.Texture is AtlasTexture art && art.Region == new Rect2(526, 213, 984, 1612) &&
                slot.CustomMinimumSize == new Vector2(40, 64),
                "Editor preview must have three visible bulbs with scene-authored texture crop and sizing");
        }
        Check(authoredBulbs.GetThemeConstant("separation") == 5 &&
            authoredPanel.GetThemeStylebox("panel") is StyleBoxEmpty &&
            authoredPanel.GetThemeStylebox("panel").ContentMarginLeft == 10,
            "Spacing and panel style must be authored in the scene rather than changed by runtime code");
        // Preview the reusable scene on its own, without TradeScene or its startup hooks.
        var preview = new SubViewport { Size = new Vector2I(240, 100), TransparentBg = true,
            World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        Root.AddChild(preview);
        try
        {
            var panel = GD.Load<PackedScene>("res://scenes/UI/EnergyPanel.tscn").Instantiate<PanelContainer>();
            preview.AddChild(panel);
            panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
            panel.Position = new Vector2(30, 18);
            panel.Size = new Vector2(180, 80);
            using (await CapturePresentation()) { }
            using var pixels = preview.GetTexture().GetImage();
            pixels.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-hf197-preview.png"));
            foreach (Vector2I point in new[] { new Vector2I(31, 19), new Vector2I(120, 19),
                new Vector2I(31, 50), new Vector2I(208, 50), new Vector2I(120, 99) })
                Check(pixels.GetPixelv(point).A < 0.01f,
                    $"Only bulb artwork may be drawn: the panel background and border must stay transparent at {point}");
            foreach (var child in panel.GetNode("EnergyBulbs").GetChildren())
            {
                var bulb = (TextureRect)child;
                if (!bulb.Visible) continue;
                Color pixel = pixels.GetPixelv((Vector2I)(bulb.GlobalPosition + bulb.Size * new Vector2(0.5f, 0.25f)));
                Check(pixel.A > 0.9f && pixel.R > 0.5f,
                    "The standalone scene must render visible bulbs without the game's runtime bootstrap");
            }
            pixels.SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-hf192-editor-preview.png"));
        }
        finally { preview.QueueFree(); }
        Rect2 ArtworkRect(int index)
        {
            var slot = authoredBulbs.GetChild<TextureRect>(index);
            Vector2 textureSize = slot.Texture.GetSize();
            Vector2 drawn = textureSize * Mathf.Min(slot.Size.X / textureSize.X, slot.Size.Y / textureSize.Y);
            return new Rect2(slot.Position + (slot.Size - drawn) / 2, drawn);
        }
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
                        Rect2 rect = ArtworkRect(index);
                        Check(area.Encloses(rect) && rect.Size.Y >= 63 && rect.Size.X >= 38,
                            "Enlarged bulbs must keep their full height, including seven energy");
                        Check(Root.GetVisibleRect().Encloses(authoredPanel.GetGlobalRect()) &&
                            authoredPanel.GetGlobalRect().Position.Y >= 100,
                            "Expanded energy must fit the viewport below the balance without moving its right edge");
                        if (index > 0) Check(rect.Position.X - ArtworkRect(index - 1).End.X >= 4,
                            "Energy bulbs must retain distinct spacing");
                        Vector2 sample = bulbs.GetGlobalTransformWithCanvas() * (rect.Position + rect.Size * new Vector2(0.5f, 0.25f));
                        Color color = image.GetPixelv(new Vector2I((int)(sample.X * pixels.X), (int)(sample.Y * pixels.Y)));
                        var displayedBulb = bulbs.GetChild<TextureRect>(index);
                        string artPath = index < remaining
                            ? "res://art/ui/energy_bulb.png"
                            : "res://art/ui/energy_bulb_empty.png";
                        Check(displayedBulb.Texture is AtlasTexture frame && frame.Atlas.ResourcePath == artPath,
                            "Energy must swap between the provided lit and empty textures from right to left");
                        Check(displayedBulb.SelfModulate == Colors.White,
                            "Energy texture colors must be preserved rather than darkened with a tint");
                        if (index < remaining)
                            Check(HasChroma(color) && color.R > 0.65f,
                                "A lit bulb must retain its native yellow color");
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
