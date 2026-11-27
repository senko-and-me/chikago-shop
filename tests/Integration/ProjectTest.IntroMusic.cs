using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private static void CheckPreparedMusicSeam(AudioStreamWav stream)
    {
        byte[] data = stream.Data;
        double energy = 0;
        int count = (int)(stream.MixRate * 0.02);
        for (int channel = 0; channel < 2; channel++)
        {
            double first = BitConverter.ToInt16(data, channel * 2) / 32768.0;
            double last = BitConverter.ToInt16(data, data.Length - 4 + channel * 2) / 32768.0;
            Check(Math.Abs(first - last) < 0.001,
                "The prepared cyclic seam must not introduce an impulsive sample jump in either channel");
            for (int i = 0; i < count; i++)
            {
                double before = BitConverter.ToInt16(data, data.Length - (i + 1) * 4 + channel * 2) / 32768.0;
                double after = BitConverter.ToInt16(data, i * 4 + channel * 2) / 32768.0;
                energy += before * before + after * after;
            }
        }
        double seamRms = Math.Sqrt(energy / (count * 4));
        double bodyRms = MusicRms(stream);
        Check(seamRms > bodyRms * 0.5 && seamRms < bodyRms * 1.5,
            "The seam must carry continuous music without a fade to silence or a loudness spike");
        using var playback = stream.InstantiatePlayback();
        playback.Start(stream.GetLength() - 0.12);
        int rate = (int)AudioServer.GetMixRate();
        var mixed = playback.MixAudio(1, (int)(rate * 0.24));
        Check(playback.IsPlaying() && playback.GetPlaybackPosition() < 0.25,
            "The audio probe must actually wrap from the recording's end back to its beginning");
        int window = rate / 50;
        for (int offset = 0; offset + window <= mixed.Length; offset += window)
        {
            double power = 0;
            for (int i = offset; i < offset + window; i++) power += mixed[i].LengthSquared() / 2;
            double rms = Math.Sqrt(power / window);
            Check(rms > bodyRms * 0.4 && rms < bodyRms * 3,
                "Every decoded 20ms block around the loop must retain music without a micro-pause or spike");
        }
        playback.Stop();
    }

    private static double MusicRms(AudioStreamWav stream)
    {
        byte[] data = stream.Data;
        double power = 0;
        for (int i = 0; i < data.Length; i += 2)
        {
            double value = BitConverter.ToInt16(data, i) / 32768.0;
            power += value * value;
        }
        return Math.Sqrt(power / (data.Length / 2));
    }

    private static void AdvanceMusicLine(DialoguePlayer dialogue)
    {
        int index = dialogue.Session.Index, steps = 0;
        while (!dialogue.Session.IsComplete && dialogue.Session.Index == index && steps++ < 100)
            dialogue.Advance();
        Check(steps < 100, "A dialogue line must finish while testing music cues");
    }

    private async Task TestIntroMusic()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmokeMenuMusic"),
            "Intro music checks require isolated userdata");
        foreach (string id in new[] { "thoughtful", "sinister" })
        {
            var stream = GD.Load<AudioStreamWav>($"res://audio/music/{id}_loop.wav");
            int frames = id == "thoughtful" ? 2419200 : 1411200;
            if (id == "sinister")
            {
                Check(Math.Abs(stream.GetLength() - 48 * 60.0 / 90) < 1.0 / stream.MixRate,
                    "The updated sinister recording must loop exactly twelve bars at 90 BPM");
                CheckPreparedMusicSeam(stream);
                GD.Print($"SINISTER_LOOP_CHECK: twelve bars, 90 BPM, length={stream.GetLength():F6}s; continuous decoded seam");
            }
            if (id == "thoughtful")
            {
                Check(Math.Abs(stream.GetLength() - 64 * 60.0 / 70) < 1.0 / stream.MixRate,
                    "The thoughtful loop must align with its 70 BPM period rather than the main theme's 90 BPM");
                CheckPreparedMusicSeam(stream);
                using var ending = stream.InstantiatePlayback();
                ending.Start(53.333333333333336);
                var lastPhrase = ending.MixAudio(1, (int)AudioServer.GetMixRate());
                Check(ending.IsPlaying() && ending.GetLoopCount() == 0 && ending.GetPlaybackPosition() > 54 &&
                      lastPhrase.Any(sample => sample.LengthSquared() > 0.00001),
                    "The last phrase must still play after the former premature 53.333-second boundary");
                GD.Print($"THOUGHTFUL_ENDING_CHECK: length={stream.GetLength():F6}s, last phrase position={ending.GetPlaybackPosition():F6}s, loops={ending.GetLoopCount()}");
                ending.Stop();
            }
            Check(stream.MixRate == 44100 && stream.Stereo &&
                  stream.Format == AudioStreamWav.FormatEnum.Format16Bits &&
                  stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == 0 &&
                  stream.LoopEnd == frames && stream.Data.Length == frames * 4,
                "Each track must loop its full recording through Godot's decoder");
            Check(Math.Abs(stream.GetLength() - frames / 44100.0) < 1.0 / 44100,
                "The prepared musical period must retain its exact duration");
            using var playback = stream.InstantiatePlayback();
            playback.Start(stream.GetLength() - 0.025);
            var mixed = playback.MixAudio(1, (int)AudioServer.GetMixRate() / 10);
            Check(playback.IsPlaying() && playback.GetPlaybackPosition() < 0.11 && mixed.Length > 0,
                "Music must wrap inside the decoder without scheduling a restart");
            playback.Stop();
        }

        var menuScene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
        using var targetScene = new PackedScene();
        var target = new Control { Name = "IntroMusicGameplay" };
        Check(targetScene.Pack(target) == Error.Ok, "Music transition fixture must pack");
        target.Free();

        async Task<MainMenu> OpenMenu()
        {
            var menu = menuScene.Instantiate<MainMenu>();
            menu.GameScene = targetScene;
            Root.AddChild(menu);
            CurrentScene = menu;
            menu.GetNode<IntroMusicDirector>("MusicDirector").SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            return menu;
        }
        void Begin(MainMenu menu)
        {
            // This suite isolates the prologue music; startup order is exercised by Menu checks.
            menu.StartIntroDialogue();
        }
        async Task ClearScene()
        {
            if (CurrentScene != null) { CurrentScene.QueueFree(); CurrentScene = null; }
            await ToSignal(this, SignalName.ProcessFrame);
        }

        try
        {
            var menu = await OpenMenu();
            var director = menu.GetNode<IntroMusicDirector>("MusicDirector");
            var menuVoice = menu.GetNode<AudioStreamPlayer>("MenuMusic");
            var first = director.GetNode<AudioStreamPlayer>("IntroBaseMusic");
            var thoughtful = director.GetNode<AudioStreamPlayer>("ThoughtfulMusic");
            var sinister = director.GetNode<AudioStreamPlayer>("SinisterMusic");
            Check(thoughtful.Stream != null && sinister.Stream != null &&
                  director.ThoughtfulCompensation > 0 && director.SinisterCompensation > 0 &&
                  director.IntroFadeSeconds > 0 && director.CrossfadeSeconds > 0 && director.ExitFadeSeconds > 0,
                "Scene overrides must preserve music streams, positive compensation and fade durations");
            float menuGain = Mathf.DbToLinear(director.MenuVolumeDb);
            float introGain = Mathf.DbToLinear(director.IntroVolumeDb);
            Check(director.MenuVolumeDb == director.IntroVolumeDb &&
                  director.ThoughtfulCompensation == 1 && director.SinisterCompensation == 1,
                "Menu and prologue must share the same gain without legacy source boosts");
            director._Process(1);
            Check(Math.Abs(menuVoice.VolumeLinear - menuGain) < 0.0001,
                "Idle music processing must preserve the menu level");
            Begin(menu);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            var dialogue = menu.ActiveDialogue;
            var cues = new List<string>();
            dialogue.MusicCueRequested += cues.Add;
            float quiet = first.VolumeLinear;
            Check(first.Playing && quiet < 0.003, "The first intro track must start very quietly");
            director._Process(1);
            Check(first.VolumeLinear > quiet && first.VolumeLinear < menuGain / 2 && menuVoice.Playing == false,
                "Intro music must rise gradually while the outgoing menu voice fades out");
            director._Process(3);
            Check(Math.Abs(first.VolumeLinear - introGain) < 0.0001,
                "The first intro ceiling must follow the configured level");
            while (dialogue.Session.Index < 6) AdvanceMusicLine(dialogue);
            Check(director.CurrentCue == "thoughtful" && dialogue.Session.Current.Text.Contains("я к тебе по делу") &&
                  cues.SequenceEqual(new[] { "thoughtful" }), "The business speech must trigger thoughtful music once");
            float outgoing = first.VolumeLinear;
            director._Process(director.CrossfadeSeconds / 2);
            Check(first.VolumeLinear > 0 && first.VolumeLinear < outgoing && thoughtful.Playing && thoughtful.VolumeLinear > 0,
                "The first crossfade must overlap continuous falling and rising gains");
            float halfway = thoughtful.VolumeLinear;
            director.ApplyIntroCue("thoughtful");
            Check(thoughtful.VolumeLinear == halfway, "Repeated cue must not restart its fade or track");
            director._Process(director.CrossfadeSeconds / 2);
            Check(!first.Playing && thoughtful.Playing, "Only thoughtful music must remain after the first transition");
            Check(Math.Abs(thoughtful.VolumeLinear - introGain) < 0.0001 &&
                  director.ThoughtfulVolumeDb == director.IntroVolumeDb,
                "Thoughtful music must use the same gain as the untouched main recording");
            while (dialogue.Session.Index < 15) AdvanceMusicLine(dialogue);
            Check(director.CurrentCue == "sinister" && dialogue.Session.Current.Text.StartsWith("— Либо мы подписываем договор") &&
                  cues.SequenceEqual(new[] { "thoughtful", "sinister" }),
                "The refusal speech must trigger sinister music exactly once, including paginated speeches");
            director._Process(director.CrossfadeSeconds);
            Check(!thoughtful.Playing && sinister.Playing && Math.Abs(sinister.VolumeLinear - introGain) < 0.0001 &&
                  director.SinisterVolumeDb == director.IntroVolumeDb,
                "Sinister music must use the same configured gain as the other stages");

            director.ApplyIntroCue("thoughtful");
            director._Process(0.1);
            float[] before = new[] { first.VolumeLinear, thoughtful.VolumeLinear, sinister.VolumeLinear };
            director.ApplyIntroCue("sinister");
            Check(before.SequenceEqual(new[] { first.VolumeLinear, thoughtful.VolumeLinear, sinister.VolumeLinear }),
                "Rapid cue changes must retarget current gains without an abrupt jump");
            director._Process(3);
            while (!dialogue.Session.IsComplete) AdvanceMusicLine(dialogue);
            Check(CurrentScene == menu && !director.IsSilent, "Natural dialogue end must wait for the final music fade");
            director._Process(SceneTransition.FadeOutSeconds / 2);
            Check(CurrentScene == menu && sinister.VolumeLinear > 0, "Gameplay must not open halfway through fade-out");
            director._Process(SceneTransition.FadeOutSeconds / 2);
            Check(director.IsSilent, "Natural prologue exit must finish audio within the shared visual fade-out, without an extra black hold");
            await WaitForSceneFade();
            Check(CurrentScene.Name == "IntroMusicGameplay" && !GodotObject.IsInstanceValid(director),
                "Natural completion must start gameplay only after all voices stop");
            await ClearScene();

            foreach (string stage in new[] { "base", "thoughtful", "sinister" })
            {
                menu = await OpenMenu(); director = menu.GetNode<IntroMusicDirector>("MusicDirector");
                Begin(menu);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                director._Process(1);
                if (stage != "base") director.ApplyIntroCue(stage);
                director._Process(0.2);
                var skip = menu.GetNode<TextureButton>("IntroSkipLayer/SkipButton");
                float[] gains = menu.FindChildren("*", "AudioStreamPlayer", true, false)
                    .Cast<AudioStreamPlayer>().Select(player => player.VolumeLinear).ToArray();
                Check(director.SkipFadeSeconds > 0 && director.SkipFadeSeconds <= 0.25 &&
                    director.SkipFadeSeconds < director.ExitFadeSeconds, "Skip must use its own short, smooth fade");
                skip.EmitSignal(BaseButton.SignalName.Pressed);
                skip.EmitSignal(BaseButton.SignalName.Pressed);
                menu.OnPlayPressed();
                Check(CurrentScene == menu && director.CurrentCue == "silence",
                    "Skip, including repeated input during a crossfade, must wait for silence");
                Check(gains.SequenceEqual(menu.FindChildren("*", "AudioStreamPlayer", true, false)
                    .Cast<AudioStreamPlayer>().Select(player => player.VolumeLinear)),
                    "Skip must ramp from current gains without abruptly cutting or resetting a crossfade");
                director._Process(SceneTransition.FadeOutSeconds / 2);
                Check(CurrentScene == menu && !director.IsSilent, "The first half of a fast fade must still be smooth");
                director._Process(SceneTransition.FadeOutSeconds / 2);
                Check(director.IsSilent, "Skipped prologue must use the same audio/visual fade window as natural completion");
                await WaitForSceneFade();
                Check(CurrentScene.Name == "IntroMusicGameplay", "Skip must enter gameplay once after the fade");
                await ClearScene();
            }

            menu = await OpenMenu(); director = menu.GetNode<IntroMusicDirector>("MusicDirector");
            Begin(menu);
            await ToSignal(this, SignalName.ProcessFrame);
            director.ApplyIntroCue("thoughtful"); director._Process(0.4);
            menu.ActiveDialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            await WaitForSceneFade();
            director._Process(1);
            Check(menu.ActiveDialogue == null && director.CurrentCue == "menu" &&
                  menu.FindChildren("*", "AudioStreamPlayer", true, false).Cast<AudioStreamPlayer>().Count(p => p.Playing) == 1,
                "Cancel during a crossfade must restore exactly one menu voice");
            await ClearScene();

            menu = await OpenMenu(); director = menu.GetNode<IntroMusicDirector>("MusicDirector");
            Begin(menu);
            menu.GetNode<TextureButton>("IntroSkipLayer/SkipButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ClearScene();
            await WaitForSceneFade();
            Check(CurrentScene == null, "Removing a menu during fade-out must cancel its pending transition");
        }
        finally { await ClearScene(); }
        await ToSignal(CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        GD.Print("INTRO_MUSIC_CHECKS_PASS: seamless musical loops, quiet entry, equal gains, cues, smooth/rapid crossfades, full/skip/cancel/exit");
    }
}
