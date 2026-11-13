using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestEmbroideryTarget()
    {
        Check(DisplayServer.GetName() != "headless", "Target contrast requires a rendered viewport");
        Vector2I originalSize = Root.Size;
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = size;
                game.NewGame();
                var craft = game.GetNode<EmbroideryGame>("EmbroideryGame");
                craft.Open();
                craft.Activate();
                craft.SetProcess(false); // Freeze the timer while sampling actual GPU frames.
                var surface = craft.GetNode<EmbroiderySurface>("Surface");
                for (int stitch = 0; stitch < craft.Session.Points.Count; stitch++)
                {
                    using var image = await CapturePresentation();
                    Vector2 target = surface.ScreenPoint(stitch);
                    Vector2 scale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
                    Color Pixel(Vector2 offset)
                    {
                        Vector2 p = (target + offset) * scale;
                        return image.GetPixel(Mathf.RoundToInt(p.X), Mathf.RoundToInt(p.Y));
                    }
                    Check(Pixel(Vector2.Zero).R > 0.85f && Pixel(new Vector2(6, 0)).R < 0.15f &&
                          Pixel(new Vector2(0, 11)).R > 0.85f,
                        "Every active target must retain a white bullseye, dark body and white separator in monochrome");
                    int darkRing = 0;
                    for (int x = 15; x <= 24; x++)
                        if (Pixel(new Vector2(x, 0)).R < 0.25f) darkRing++;
                    Check(darkRing >= 1, $"The pulsing outer ring must remain dark on white fabric ({size}, stitch {stitch})");
                    if (stitch == 4)
                        image.SavePng(Path.Combine(Path.GetTempPath(), $"chikago-hf93-target-{size.X}.png"));
                    PushEmbroideryClick(target);
                    Check(craft.Session.Stitches == stitch + 1 && craft.Session.Misses == 0,
                        "Real clicks must advance exactly one target without a miss");
                }
                Check(craft.Session.Complete, "The target must remain usable through the whole pattern");
                craft.Activate();
            }
        }
        finally
        {
            Paused = false;
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
