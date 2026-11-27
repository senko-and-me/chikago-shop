using System.Collections.Generic;
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
        var sounds = new List<SoundEffects.Cue>();
        var variants = new HashSet<SoundEffects.Cue>();
        void Observe(SoundEffects.Cue cue) => sounds.Add(cue);
        SoundEffects.Instance.Played += Observe;
        try
        {
            foreach (var (kind, size) in new[] { ("hat", new Vector2I(1280, 720)), ("vest", new Vector2I(1920, 1080)),
                ("gloves", new Vector2I(1280, 720)), ("matteo_hat", new Vector2I(1920, 1080)),
                ("lorgnette", new Vector2I(1280, 720)) })
            {
                Root.Size = size;
                game.NewGame();
                game.EndDay();
                var craft = game.GetNode<EmbroideryGame>("EmbroideryGame");
                game.GetItemFromAtelier();
                Check(game.Crafting.Select(kind) && craft.IsOpen, $"The real catalogue must prepare the {kind} pattern.");
                await ToSignal(this, SignalName.ProcessFrame);
                var surface = craft.GetNode<EmbroiderySurface>("Surface");
                sounds.Clear();
                PushEmbroideryClick(surface.ScreenPoint(0));
                Check(sounds.Count == 0 && craft.Session.Stitches == 0,
                    "Ready embroidery must not sound a stitch before starting");
                craft.Activate();
                craft.SetProcess(false); // Freeze the timer while sampling actual GPU frames.
                sounds.Clear();
                PushEmbroideryClick(surface.GlobalPosition + surface.FabricRect.Position + new Vector2(4, 4));
                Check(sounds.Count == 0 && craft.Session.Misses == 1,
                    "Missing the black target must not play a stitch or generic click");
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
                        image.SavePng(Path.Combine(Path.GetTempPath(), $"chikago-target-{kind}-{size.X}.png"));
                    sounds.Clear();
                    if (stitch % 2 == 0) PushEmbroideryClick(target);
                    else
                    {
                        Root.PushInput(new InputEventScreenTouch { Index = 0, Position = target, Pressed = true }, true);
                        Root.PushInput(new InputEventScreenTouch { Index = 0, Position = target, Pressed = false }, true);
                    }
                    Check(sounds.Count == 1 && sounds[0] is SoundEffects.Cue.Stitch1 or SoundEffects.Cue.Stitch2 or SoundEffects.Cue.Stitch3,
                        "Each successful mouse/touch stitch must play exactly one variant without duplicate click");
                    variants.Add(sounds[0]);
                    var player = SoundEffects.Instance.GetNode<AudioStreamPlayer>(sounds[0].ToString());
                    Check(Paused && player.Playing && player.Bus == SoundEffects.BusName,
                        "Stitch audio must play on the effects bus while the minigame pauses gameplay");
                    Check(craft.Session.Stitches == stitch + 1 && craft.Session.Misses == 1,
                        "Real clicks must advance exactly one target without a miss");
                }
                Check(craft.Session.Complete, "The target must remain usable through the whole pattern");
                sounds.Clear();
                PushEmbroideryClick(surface.ScreenPoint(0));
                Check(sounds.Count == 0, "Finished embroidery must not sound another stitch");
                craft.Activate();
            }
            Check(variants.Count > 1, "Repeated stitches must randomly vary their sound");
            GD.Print($"STITCH_AUDIO_CHECKS_PASS: mouse/touch, one cue per stitch, {variants.Count} random variants, silent miss/ready/finished, paused SFX bus");
        }
        finally
        {
            SoundEffects.Instance.Played -= Observe;
            Paused = false;
            game.QueueFree();
            Root.Size = originalSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
