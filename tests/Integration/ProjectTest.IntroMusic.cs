using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
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
            Check(stream.MixRate == 44100 && stream.Stereo &&
                  stream.Format == AudioStreamWav.FormatEnum.Format16Bits &&
                  stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == 0 &&
                  stream.LoopEnd == 470400 && stream.Data.Length == 470400 * 4,
                "Each new track must be lossless PCM, natively looped at four complete 90 BPM bars");
            Check(Math.Abs(stream.GetLength() - 16 * 60.0 / 90) < 1.0 / 44100,
                "MP3 padding and export tails must not change the musical loop duration");
            byte[] pcm = stream.Data;
            for (int channel = 0; channel < 2; channel++)
                Check(Math.Abs(BitConverter.ToInt16(pcm, channel * 2) -
                      BitConverter.ToInt16(pcm, pcm.Length - 4 + channel * 2)) / 32768.0 < 0.003,
                    "Prepared MP3-derived loops must have a smooth waveform join");
            using var playback = stream.InstantiatePlayback();
            playback.Start(stream.GetLength() - 0.025);
            var mixed = playback.MixAudio(1, (int)AudioServer.GetMixRate() / 10);
            Check(playback.IsPlaying() && playback.GetPlaybackPosition() < 0.11 &&
                  mixed.Any(frame => frame.LengthSquared() > 0.00001),
                "New music must wrap inside the decoder without a restart or silent gap");
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
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
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
            float menuGain = Mathf.DbToLinear(director.MenuVolumeDb);
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
            Check(Math.Abs(first.VolumeLinear / menuGain - 0.5) < 0.0001,
                "The first intro ceiling must be half the menu amplitude");
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
            double firstLevel = MusicRms((AudioStreamWav)first.Stream) * menuGain / 2;
            double secondLevel = MusicRms((AudioStreamWav)thoughtful.Stream) * thoughtful.VolumeLinear;
            Check(secondLevel > firstLevel && secondLevel < firstLevel * 1.4,
                "Source loudness compensation must make thoughtful music slightly louder than the first stage");
            while (dialogue.Session.Index < 14) AdvanceMusicLine(dialogue);
            Check(director.CurrentCue == "sinister" && dialogue.Session.Current.Text.StartsWith("Поверь, я и слышать не хочу") &&
                  cues.SequenceEqual(new[] { "thoughtful", "sinister" }),
                "The refusal speech must trigger sinister music exactly once, including paginated speeches");
            director._Process(director.CrossfadeSeconds);
            double thirdLevel = MusicRms((AudioStreamWav)sinister.Stream) * sinister.VolumeLinear;
            Check(!thoughtful.Playing && sinister.Playing && thirdLevel > secondLevel && thirdLevel < secondLevel * 1.4,
                "Sinister music must complete the gradual loudness increase");

            director.ApplyIntroCue("thoughtful");
            director._Process(0.1);
            float[] before = new[] { first.VolumeLinear, thoughtful.VolumeLinear, sinister.VolumeLinear };
            director.ApplyIntroCue("sinister");
            Check(before.SequenceEqual(new[] { first.VolumeLinear, thoughtful.VolumeLinear, sinister.VolumeLinear }),
                "Rapid cue changes must retarget current gains without an abrupt jump");
            director._Process(3);
            while (!dialogue.Session.IsComplete) AdvanceMusicLine(dialogue);
            Check(CurrentScene == menu && !director.IsSilent, "Natural dialogue end must wait for the final music fade");
            director._Process(director.ExitFadeSeconds / 2);
            Check(CurrentScene == menu && sinister.VolumeLinear > 0, "Gameplay must not open halfway through fade-out");
            director._Process(director.ExitFadeSeconds / 2);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
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
                skip.EmitSignal(BaseButton.SignalName.Pressed);
                skip.EmitSignal(BaseButton.SignalName.Pressed);
                menu.OnPlayPressed();
                Check(CurrentScene == menu && director.CurrentCue == "silence",
                    "Skip, including repeated input during a crossfade, must wait for silence");
                director._Process(director.ExitFadeSeconds);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(CurrentScene.Name == "IntroMusicGameplay", "Skip must enter gameplay once after the fade");
                await ClearScene();
            }

            menu = await OpenMenu(); director = menu.GetNode<IntroMusicDirector>("MusicDirector");
            Begin(menu);
            await ToSignal(this, SignalName.ProcessFrame);
            director.ApplyIntroCue("thoughtful"); director._Process(0.4);
            menu.ActiveDialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            director._Process(1);
            Check(menu.ActiveDialogue == null && director.CurrentCue == "menu" &&
                  menu.FindChildren("*", "AudioStreamPlayer", true, false).Cast<AudioStreamPlayer>().Count(p => p.Playing) == 1,
                "Cancel during a crossfade must restore exactly one menu voice");
            await ClearScene();

            menu = await OpenMenu(); director = menu.GetNode<IntroMusicDirector>("MusicDirector");
            Begin(menu);
            menu.GetNode<TextureButton>("IntroSkipLayer/SkipButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ClearScene();
            Check(CurrentScene == null, "Removing a menu during fade-out must cancel its pending transition");
        }
        finally { await ClearScene(); }
        await ToSignal(CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        GD.Print("INTRO_MUSIC_CHECKS_PASS: 90 BPM loops, quiet entry, normalized crescendo, sentence cues, smooth/rapid crossfades, full/skip/cancel/exit");
    }
}
