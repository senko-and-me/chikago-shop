using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestCardFlightAudio()
    {
        var originalSize = Root.Size;
        double volume = SoundEffects.Instance.Volume;
        DayTransitionCard cards = null;
        TradeScene game = null;
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        async Task Wait(Func<bool> ready)
        {
            ulong start = Time.GetTicksMsec();
            while (!ready()) { Check(Time.GetTicksMsec() - start < 7000, "Card animation/audio callback must arrive"); await Frame(); }
        }
        try
        {
            SoundEffects.Instance.PreviewVolume(0);
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = "";
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await Frame();
            cards = game.GetNode<DayTransitionCard>("DayTransitionCard");
            var player = cards.GetNode<AudioStreamPlayer>("CardFlySound");
            Check(player.Stream.ResourcePath == "res://audio/sfx/card_fly.mp3" && player.Stream.GetLength() > 0 &&
                player.Bus == SoundEffects.BusName && player.MaxPolyphony == 3, "Card flights must use the supplied clip on the SFX bus with bounded overlapping voices");
            Check(AudioServer.IsBusMute(AudioServer.GetBusIndex(player.Bus)), "Existing sound mute must apply to card flights");
            var heard = new List<PanelContainer>();
            cards.FlightSoundPlayed += card =>
            {
                Check(player.Playing && card.IsVisibleInTree(), "Sound must start with the actual visible card flight");
                heard.Add(card);
            };
            var options = DayUpgradeRules.Available.Take(3).ToArray();
            cards.Open(1, 2, options, 10000);
            Check(heard.Count == 0 && !player.Playing, "Opening must not play whoosh before a card launches");
            await Wait(() => heard.Count > 0);
            Check(Paused && heard.Count == 1, "Staggered card flight must sound while the day-choice pause is held");
            // Resize in the middle of dealing: already-launched cards must remain silent on reflow.
            Root.Size = new Vector2I(960, 540);
            await WaitForDayCardAnimation(cards);
            await Frame();
            Check(heard.Count == options.Length && heard.Distinct().Count() == options.Length,
                "Each logical card must sound once despite a mid-deal resize");
            Root.Size = new Vector2I(1280, 720);
            await Frame();
            Check(heard.Count == options.Length, "Choosing-phase layout must not replay flight audio");
            cards.Close();
            Check(!player.Playing && !Paused, "Closing must stop card flight audio and release the pause");
            int before = heard.Count;
            cards.Open(1, 2, options.Take(1).ToArray(), 10000);
            cards.Close();
            await ToSignal(CreateTimer(1.2, true), SceneTreeTimer.SignalName.Timeout);
            Check(heard.Count == before && !player.Playing, "Closing before launch must cancel delayed sound callbacks");
            cards.Open(1, 2, options.Take(1).ToArray(), 10000);
            await WaitForDayCardAnimation(cards);
            Check(heard.Count == before + 1, "A new opening must re-arm the single card's flight sound");
            cards.Close();
            Check(!player.Playing, "The completed flight must leave no sound after closing");
        }
        finally
        {
            cards?.Close();
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            Paused = false;
            Root.Size = originalSize;
            SoundEffects.Instance.PreviewVolume(volume);
            await Frame();
        }
    }
}
