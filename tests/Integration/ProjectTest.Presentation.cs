using System;
using System.IO;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task<Image> CapturePresentation(string name = null)
    {
        // Allow layout, popup placement and screen-texture sampling to settle.
        for (int frame = 0; frame < 3; frame++) await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = Root.GetTexture().GetImage();
        Check(image != null && !image.IsEmpty(), "Presentation tests require a rendered viewport (run without --headless)");
        if (name != null)
            image.SavePng(Path.Combine(Path.GetTempPath(), "chikago-hf67-" + name + ".png"));
        return image;
    }

    private static bool HasChroma(Color color) =>
        Mathf.Max(color.R, Mathf.Max(color.G, color.B)) - Mathf.Min(color.R, Mathf.Min(color.G, color.B)) > 2.1f / 255;

    private static Rect2 ScreenBounds(Control control) =>
        control.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, control.Size);

    private void CheckRenderedGray(Image image, params Rect2[] exceptions)
    {
        Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
        for (int y = 1; y < image.GetHeight(); y += 3)
        for (int x = 1; x < image.GetWidth(); x += 3)
        {
            Vector2 point = new Vector2(x, y) / scale;
            bool excepted = false;
            foreach (Rect2 exception in exceptions) excepted |= exception.Grow(1).HasPoint(point);
            if (!excepted)
                Check(!HasChroma(image.GetPixel(x, y)), $"Unexpected color outside exceptions at {x},{y}");
        }
    }

    private int CountRenderedColor(Image image, Rect2 bounds)
    {
        Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
        int count = 0;
        for (int y = Math.Max(0, (int)(bounds.Position.Y * scale.Y)); y < Math.Min(image.GetHeight(), bounds.End.Y * scale.Y); y++)
        for (int x = Math.Max(0, (int)(bounds.Position.X * scale.X)); x < Math.Min(image.GetWidth(), bounds.End.X * scale.X); x++)
            if (HasChroma(image.GetPixel(x, y))) count++;
        return count;
    }

    private async Task TestNativeColorPresentation()
    {
        Check(!ProjectSettings.HasSetting("autoload/MonochromePresentation") && !Root.HasNode("MonochromePresentation"),
            "The monochrome screen filter must not be loaded");
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        var probe = new CanvasLayer { Layer = 150 };
        var swatch = new ColorRect { Position = new Vector2(24, 24), Size = new Vector2(64, 64),
            Color = new Color(0.9f, 0.1f, 0.4f), MouseFilter = Control.MouseFilterEnum.Ignore };
        probe.AddChild(swatch);
        Root.AddChild(probe);
        Root.AddChild(game);
        game.SetProcess(false);
        async Task CheckColor(string state)
        {
            using var image = await CapturePresentation("native-color-" + state);
            Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
            Color pixel = image.GetPixelv((Vector2I)(new Vector2(56, 56) * scale));
            Check(Mathf.Abs(pixel.R - swatch.Color.R) < 0.025f && Mathf.Abs(pixel.G - swatch.Color.G) < 0.025f &&
                Mathf.Abs(pixel.B - swatch.Color.B) < 0.025f, "Source colors must survive the final render in " + state);
            CheckRenderedGray(image, ScreenBounds(swatch),
                ScreenBounds(game.GetNode<Control>("HUD/Root/EnergyPanel/EnergyBulbs")));
        }
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                Root.Size = size;
                game.NewGame();
                game.EndDay();
                game.Score = 10000;
                game.TryUnlockMusicWorkshop();
                await CheckColor("world-" + size.X);
                var table = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
                table.Show();
                await CheckColor("rarity-" + size.X);
                table.Hide();
                var bulb = game.GetNode<TextureRect>("HUD/Root/EnergyPanel/EnergyBulbs/Bulb1");
                using (var image = await CapturePresentation())
                    Check(CountRenderedColor(image, ScreenBounds(bulb)) > 15,
                        "Actual energy artwork must retain its original yellow color");
                var pause = game.GetNode<PauseMenu>("PauseMenu");
                pause.Open();
                await CheckColor("pause-" + size.X);
                pause.Close();
                foreach (string name in new[] { "EmbroideryGame", "PolishingGame" })
                {
                    var craft = game.GetNode<EmbroideryGame>(name);
                    craft.Open();
                    craft.SetProcess(false);
                    Check(craft.IsOpen, "The fixture must open both craft modals");
                    await CheckColor(name + "-" + size.X);
                    craft.Activate();
                    await CheckColor(name + "-playing-" + size.X);
                    craft.FinishForTesting(true);
                    await CheckColor(name + "-finished-" + size.X);
                    craft.Activate();
                }
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                card.Open(1, 2, new[] { DayUpgrade.Cash, DayUpgrade.Energy, DayUpgrade.Time }, game.Score);
                await WaitForDayCardAnimation(card);
                await CheckColor("cards-" + size.X);
                card.Close();
            }
        }
        finally
        {
            Paused = false;
            game.QueueFree();
            probe.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
        var menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
        Root.AddChild(menu);
        try
        {
            using (var image = await CapturePresentation("native-gray-menu")) CheckRenderedGray(image);
            menu.OnSettingsPressed();
            using (var image = await CapturePresentation("native-gray-settings")) CheckRenderedGray(image);
            ulong deadline = Time.GetTicksMsec() + 10000;
            while (!menu.IsGameScenePrepared)
            {
                Check(Time.GetTicksMsec() < deadline, "Menu warmup must finish");
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        finally { menu.QueueFree(); }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
