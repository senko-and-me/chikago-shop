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

    private async Task TestMonochromePresentation()
    {
        Check(DisplayServer.GetName() != "headless", "Use the GPU renderer for presentation pixel checks");
        Vector2I originalSize = Root.Size;
        var presentation = Root.GetNode<MonochromePresentation>("MonochromePresentation");
        var filter = presentation.GetNode<ColorRect>("Filter");
        Check(filter.MouseFilter == Control.MouseFilterEnum.Ignore && filter.FocusMode == Control.FocusModeEnum.None,
            "The final presentation pass must not intercept mouse or keyboard input");
        MainMenu menu = null;
        TradeScene game = null;
        try
        {
            Root.Size = new Vector2I(1280, 720);
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            // A deterministic color probe proves that rendered pixels are actually transformed,
            // even when the menu artwork itself is already monochrome.
            var probe = new CanvasLayer { Layer = 150 };
            probe.AddChild(new ColorRect
            {
                Position = new Vector2(24, 24), Size = new Vector2(64, 64), Color = new Color(0.9f, 0.1f, 0.4f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            menu.AddChild(probe);
            filter.Hide();
            using (var original = await CapturePresentation("probe-original"))
                Check(CountRenderedColor(original, Root.GetVisibleRect()) > 100, "The test must start with actual colored source pixels");
            filter.Show();
            using (var image = await CapturePresentation()) CheckRenderedGray(image);
            probe.QueueFree();
            using (var image = await CapturePresentation("menu")) CheckRenderedGray(image);
            menu.OnSettingsPressed();
            using (var image = await CapturePresentation()) CheckRenderedGray(image);
            menu.OnBackPressed();
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(menu.ActiveDialogue != null, "The intro must open for its presentation check");
            using (var image = await CapturePresentation("dialogue")) CheckRenderedGray(image);
            menu.QueueFree();
            menu = null;
            await ToSignal(this, SignalName.ProcessFrame);

            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = ""; // Never load or write player progress.
            Root.AddChild(game);
            game.SetProcess(false);
            game.GetNode<Camera2D>("Camera2D").SetProcess(false);
            game.NewGame();
            game.Score = 1000;
            game.TryUnlockMusicWorkshop();
            game.DayUpgrades.Restore(false, DayUpgrade.None, 2, 1, 1, 1, 1);
            var clock = game.GetNode<TextureRect>("HUD/Root/DayTimer/Clock");
            game._Process(91);
            Check(clock.Texture.ResourcePath.EndsWith("day_timer_expired.png"), "Expired clock must use its special red sprite");
            using (var image = await CapturePresentation("world"))
            {
                CheckRenderedGray(image, ScreenBounds(clock));
                Check(CountRenderedColor(image, ScreenBounds(clock)) > 20, "The actual red clock sprite must retain color");
            }

            // A color patch behind the sprite catches accidental rectangular holes around its alpha.
            var timer = clock.GetParent<Control>();
            var patch = new ColorRect
            {
                Position = timer.Position, Size = timer.Size, Color = new Color(0.9f, 0.1f, 0.4f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            timer.GetParent().AddChild(patch);
            timer.GetParent().MoveChild(patch, timer.GetIndex());
            filter.Hide();
            using (var original = await CapturePresentation())
            {
                Vector2 corner = ScreenBounds(clock).Position + new Vector2(4, 4);
                Vector2 scale = new Vector2(original.GetWidth(), original.GetHeight()) / Root.GetVisibleRect().Size;
                Check(HasChroma(original.GetPixel((int)(corner.X * scale.X), (int)(corner.Y * scale.Y))),
                    "The transparent clock corner must reveal the test patch before filtering");
            }
            filter.Show();
            using (var image = await CapturePresentation())
            {
                Vector2 corner = ScreenBounds(clock).Position + new Vector2(4, 4);
                Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
                Check(!HasChroma(image.GetPixel((int)(corner.X * scale.X), (int)(corner.Y * scale.Y))),
                    "Transparent clock corners must not preserve colored world pixels");
                Check(CountRenderedColor(image, ScreenBounds(clock)) > 20, "Opaque clock pixels must stay colored");
            }
            patch.QueueFree();

            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string room in new[] { "Atelier", "MusicWorkshop" })
            {
                Root.Size = size;
                game.GetNode<Camera2D>("Camera2D").Zoom = Vector2.One * (size.X == 960 ? 0.25f : 1.8f);
                var popup = game.GetNode<RarityPanel>($"World/{room}/RarityPanel");
                popup.Show();
                using (var image = await CapturePresentation(size.X == 1280 ? room.ToLowerInvariant() : null))
                {
                    CheckRenderedGray(image, ScreenBounds(clock), ScreenBounds(popup.Popup));
                    Check(CountRenderedColor(image, ScreenBounds(popup.Popup)) > 20,
                        "Both chance/category price tables must preserve their colored values");
                }
                popup.Hide();
            }

            var table = game.GetNode<RarityPanel>("World/Atelier/RarityPanel");
            table.Show();
            game.GetNode<PauseMenu>("PauseMenu").Open();
            using (var image = await CapturePresentation("pause")) CheckRenderedGray(image);
            game.GetNode<PauseMenu>("PauseMenu").Close();
            table.Hide();
            game.NewGame();
            game.Score = 1000;
            game.TryUnlockMusicWorkshop();
            foreach (string crafting in new[] { "EmbroideryGame", "PolishingGame" })
            {
                var miniGame = game.GetNode<EmbroideryGame>(crafting);
                miniGame.Open();
                Check(miniGame.IsOpen, "The real crafting overlay must open");
                using (var image = await CapturePresentation(crafting.ToLowerInvariant())) CheckRenderedGray(image);
                miniGame._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            }
            var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
            card.Open(1, 2, new[] { DayUpgrade.Cash, DayUpgrade.Energy, DayUpgrade.Time }, 1000);
            using (var image = await CapturePresentation("day-card")) CheckRenderedGray(image);
            card.Skip();
            game.QueueFree();
            game = null;
            await ToSignal(this, SignalName.ProcessFrame);
            // Freed scene exceptions must not leave holes in the next menu.
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            using (var image = await CapturePresentation()) CheckRenderedGray(image);
        }
        finally
        {
            filter.Show();
            Paused = false;
            if (menu != null && GodotObject.IsInstanceValid(menu)) menu.QueueFree();
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
