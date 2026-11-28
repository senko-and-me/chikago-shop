using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestPolishingAudio()
    {
        var effects = SoundEffects.Instance;
        var audio = effects.Polishing;
        float volumeDb = audio.VolumeDb;
        float pitchScale = audio.PitchScale;
        double busVolume = effects.Volume;
        var heard = new List<int>();
        void Observe(int sample) => heard.Add(sample);
        audio.SamplePlayed += Observe;
        audio.SetProcess(false);
        effects.PreviewVolume(0); // Exercise real playback without blasting rapid sampler checks.
        var owner = new Node();
        Root.AddChild(owner);
        TradeScene game = null;
        const string save = "user://polishing_audio_test.json";
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        void Advance(double seconds)
        {
            while (seconds > 0.00001) { double step = Math.Min(0.01, seconds); audio._Process(step); seconds -= step; }
        }
        void Mouse(Vector2 point, bool down) => Root.PushInput(new InputEventMouseButton
        {
            Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = down,
            ButtonMask = down ? MouseButtonMask.Left : 0
        }, true);
        void Motion(Vector2 point) => Root.PushInput(new InputEventMouseMotion
        { Position = point, GlobalPosition = point, Relative = new Vector2(10, 0), ButtonMask = MouseButtonMask.Left }, true);
        try
        {
            Check(pitchScale == 1 && volumeDb == 0 && audio.MinRate == 2 && audio.MaxRate == 4,
                "The loaded scene must use natural pitch and gain while retaining the chosen cadence");
            foreach (int i in new[] { 1, 3, 4 })
            {
                var sample = (AudioStreamWav)audio.GetNode<AudioStreamPlayer>("Scrape" + i).Stream;
                Check(sample.ResourcePath == $"res://audio/sfx/stitch_{i}.wav" &&
                    sample == effects.GetNode<AudioStreamPlayer>("Stitch" + i).Stream && sample.GetLength() > 0 &&
                    sample.LoopMode == AudioStreamWav.LoopModeEnum.Disabled,
                    "The instrument minigame must use only stitch recordings one, three and four");
            }
            audio.BeginContact(owner, Vector2.Zero);
            Advance(0.2);
            for (int i = 0; i < 30; i++)
            {
                audio.MoveContact(owner, new Vector2(i % 2 == 0 ? 1 : -1, 0), 0.01);
                Advance(0.01);
            }
            Check(heard.Count == 0, "Holding still and subthreshold pointer jitter must remain silent");
            audio.MoveContact(owner, new Vector2(10, 0), 0.02);
            Check(heard.Count == 1 && audio.ActiveVoiceCount == 1, "The first meaningful motion must trigger immediately");
            Advance(0.02);
            Check(audio.GetNode<AudioStreamPlayer>("Voice1").VolumeLinear > 0,
                "The short attack must produce actual audio-player gain");
            Advance(audio.IdleSeconds + audio.FadeSeconds + 0.02);
            Check(audio.ActiveVoiceCount == 0, "Stopping motion must fade out without waiting for mouse release");
            int stopped = heard.Count;
            Advance(1);
            Check(heard.Count == stopped, "Idle time must never queue or retrigger a scrape");

            foreach (var (speed, expectedRate) in new[] { (30f, 2f), (60f, 2f), (330f, 3f), (1200f, 4f) })
            {
                audio.EndContact(owner); Advance(0.1); heard.Clear();
                audio.BeginContact(owner, Vector2.Zero);
                Vector2 point = Vector2.Zero;
                for (int i = 0; i < 200; i++)
                {
                    point.X += speed * 0.01f;
                    audio.MoveContact(owner, point, 0.01);
                    Advance(0.01);
                    Check(audio.ActiveVoiceCount <= 2, "Polishing must never use more than two voices");
                    float power = 0;
                    for (int voice = 1; voice <= 2; voice++)
                    {
                        var player = audio.GetNode<AudioStreamPlayer>("Voice" + voice);
                        if (player.Playing) power += player.VolumeLinear * player.VolumeLinear;
                    }
                    Check(power <= Mathf.Pow(Mathf.DbToLinear(audio.VolumeDb), 2) + 0.0001f,
                        "Overlapping scrapes must keep a bounded total playback power");
                }
                Check(Math.Abs(audio.CurrentRate - expectedRate) < 0.05 && Math.Abs(heard.Count - expectedRate * 2) <= 1,
                    $"Speed {speed} must produce about {expectedRate} scrapes per second");
            }
            audio.EndContact(owner); Advance(1); heard.Clear();
            for (int i = 0; i < 200; i++)
            {
                audio.BeginContact(owner, Vector2.Zero);
                audio.MoveContact(owner, new Vector2(12, 0), 0.01);
                Advance(0.01);
                audio.EndContact(owner);
            }
            Check(heard.Count is >= 7 and <= 9,
                "Rapid contour reentry must still be limited to about four scrapes per second");
            var variants = new HashSet<int>();
            int previous = audio.LastSample;
            for (int i = 0; i < 512; i++)
            {
                audio.EndContact(owner); Advance(1);
                audio.BeginContact(owner, Vector2.Zero);
                audio.MoveContact(owner, new Vector2(10, 0), 0.02);
                Check(audio.LastSample is 1 or 3 or 4, "Instrument polishing must never select stitch two or five");
                Check(audio.LastSample != previous, "Consecutive samples must never repeat");
                variants.Add(audio.LastSample); previous = audio.LastSample;
            }
            Check(variants.Count == 3, "All three selected instrument sounds must be reachable");
            audio.EndContact(owner); Advance(1);
            audio.VolumeDb = -18;
            audio.PitchScale = 2f / 3;
            audio.BeginContact(owner, Vector2.Zero);
            audio.MoveContact(owner, new Vector2(10, 0), 0.02); Advance(0.02);
            var audible = audio.GetNode<AudioStreamPlayer>("Voice1");
            Check(audible.Bus == SoundEffects.BusName && Math.Abs(audible.VolumeDb + 18) < 0.01,
                "Editor gain must feed the existing SFX volume bus");
            Check(Math.Abs(audible.PitchScale - 2f / 3) < 0.0001,
                "The editor pitch must be applied to the actual playback voice");
            Vector2 sustained = new(10, 0);
            for (int step = 0; step < 40; step++)
            {
                sustained.X += 5;
                audio.MoveContact(owner, sustained, 0.01);
                Advance(0.01);
            }
            Check(audible.Playing && audio.ActiveVoiceCount <= 2,
                "Lower-pitched scrapes must retain their longer tail within the two-voice limit");
            audio.EndContact(owner); Advance(0.1); audio.VolumeDb = volumeDb; audio.PitchScale = pitchScale;

            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game); game.SetProcess(false);
            game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
            await Frame();
            game.NewGame(); game.Score = 10000; UnlockMusicForFixture(game); game.EndDay();
            game.Crafting.OpenCatalog("music");
            Check(game.Crafting.Select("guitar"), "The audio fixture must prepare a real instrument");
            var mini = game.GetNode<EmbroideryGame>("PolishingGame");
            mini.SetProcess(false);
            var surface = mini.GetNode<PolishingSurface>("Surface/Polishing");
            await Frame();
            var cell = surface.Session.Cells.First(c => c.Inside && surface.Session.Inside(c.X + 16, c.Y));
            Vector2 a = surface.LogicalToScreen(new Vector2((float)cell.X, (float)cell.Y));
            Vector2 b = surface.LogicalToScreen(new Vector2((float)cell.X + 16, (float)cell.Y));
            heard.Clear(); Mouse(a, true); Motion(b); Mouse(b, false);
            Check(heard.Count == 0, "Preparation must not sound polishing input");
            mini.Activate(); await Frame();
            Advance(1);
            heard.Clear(); Mouse(a, true);
            Check(heard.Count == 0, "Pressing without movement must remain silent");
            Motion(b); Advance(0.02);
            Check(heard.Count == 1 && audio.ActiveVoiceCount > 0, "A real mouse rub over the silhouette must play a scrape");
            var realVoice = new[] { audio.GetNode<AudioStreamPlayer>("Voice1"), audio.GetNode<AudioStreamPlayer>("Voice2") }
                .Single(player => player.Playing);
            Check(Math.Abs(realVoice.PitchScale - 1) < 0.0001,
                "The actual second minigame must play the stitch recording at its original pitch");
            GD.Print($"POLISHING_PITCH_CHECK: configured={pitchScale}, actual={realVoice.PitchScale}, maxRate={audio.MaxRate}");
            Motion(surface.LogicalToScreen(Vector2.Zero)); Advance(0.08);
            Check(audio.ActiveVoiceCount == 0, "Moving off the silhouette must fade out");
            Advance(1);
            Motion(a); Motion(b); Advance(0.02);
            Check(audio.ActiveVoiceCount > 0, "Returning and moving on the instrument must resume sound");
            Mouse(b, false); Advance(0.08);
            Check(audio.ActiveVoiceCount == 0, "Releasing the mouse must fade out");
            Advance(1);
            Mouse(a, true); Motion(b); Advance(0.02);
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open(); Advance(0.08);
            Check(audio.ActiveVoiceCount == 0, "True pause must stop polishing despite always-processing audio");
            Mouse(b, false); pause.Close();
            Advance(1);
            heard.Clear();
            Root.PushInput(new InputEventScreenTouch { Index = 0, Position = a, Pressed = true }, true);
            Root.PushInput(new InputEventScreenDrag { Index = 0, Position = b, Relative = b - a }, true);
            Advance(0.02);
            Check(heard.Count == 1, "A real touch drag must trigger exactly one polishing sound");
            mini.FinishForTesting(true); await Frame(); Advance(0.08);
            Check(audio.ActiveVoiceCount == 0, "Finishing the session must fade out all scraping voices");
            Root.PushInput(new InputEventScreenTouch { Index = 0, Position = b, Pressed = false }, true);
            mini.Activate(); Advance(0.1);
            Check(!mini.IsOpen && audio.ActiveVoiceCount == 0, "Collecting must not leave a scraping tail or queue");

            game.Crafting.OpenCatalog("music");
            Check(game.Crafting.Select("guitar"), "A second instrument must allow an early collection check");
            mini.Activate(); await Frame();
            Advance(1);
            Mouse(a, true); Motion(b); Advance(0.02);
            Check(audio.ActiveVoiceCount > 0, "A scrape should be active before early collection");
            mini.Activate(); Advance(0.08);
            Check(!mini.IsOpen && audio.ActiveVoiceCount == 0 && game.GetShowcaseQualityRank(4) == "D",
                "Early collection during active rubbing must fade the scrape and save partial quality");
            Mouse(b, false);
        }
        finally
        {
            audio.SamplePlayed -= Observe;
            audio.EndContact(owner);
            if (game != null) { game.GetNode<EmbroideryGame>("PolishingGame").SuspendPolishingInput(); game.QueueFree(); }
            Advance(0.1);
            audio.VolumeDb = volumeDb;
            audio.PitchScale = pitchScale;
            audio.SetProcess(true);
            effects.PreviewVolume(busVolume);
            owner.QueueFree(); Paused = false;
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
            await Frame();
        }
    }
}
