using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestMenuMusic()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmokeMenuMusic"),
            "Menu music checks require isolated userdata; refusing to touch player saves");
        const int frames = 940800; // 32 quarter notes at 90 BPM, 44100 Hz.
        var stream = GD.Load<AudioStreamWav>("res://audio/music/menu_theme_loop.wav");
        Check(stream != null && stream.MixRate == 44100 && stream.Stereo &&
              stream.Format == AudioStreamWav.FormatEnum.Format16Bits &&
              stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward &&
              stream.LoopBegin == 0 && stream.LoopEnd == frames && stream.Data.Length == frames * 4,
            "The imported music must retain lossless stereo PCM and exact eight-bar native loop points");
        Check(Math.Abs(stream.GetLength() - 32 * 60.0 / 90) < 1.0 / stream.MixRate,
            "Loop duration must equal exactly eight 4/4 bars at 90 BPM");
        byte[] data = stream.Data;
        double jumpLeft = Math.Abs(BitConverter.ToInt16(data, 0) - BitConverter.ToInt16(data, data.Length - 4)) / 32768.0;
        double jumpRight = Math.Abs(BitConverter.ToInt16(data, 2) - BitConverter.ToInt16(data, data.Length - 2)) / 32768.0;
        Check(jumpLeft < 0.003 && jumpRight < 0.003,
            "The prepared loop must not retain the original abrupt waveform discontinuity");
        using var playback = stream.InstantiatePlayback();
        double loopSeconds = frames / 44100.0;
        playback.Start(loopSeconds - 0.025);
        var rendered = playback.MixAudio(1, (int)AudioServer.GetMixRate() / 10);
        GD.Print($"MENU_LOOP_DECODE frames={rendered.Length} playing={playback.IsPlaying()} loops={playback.GetLoopCount()} position={playback.GetPlaybackPosition():F9}");
        Check(rendered.Length > 0 && playback.IsPlaying() &&
              playback.GetPlaybackPosition() > 0 && playback.GetPlaybackPosition() < 0.11,
            "Native playback must wrap across the loop boundary without Finished/Play scheduling");
        for (int start = 0; start + 256 <= rendered.Length; start += 256)
            Check(rendered.Skip(start).Take(256).Sum(value => value.LengthSquared()) / 256 > 0.00001,
                "Decoded audio must remain continuous, without a silent gap around the loop seam");
        playback.Stop();

        var menuScene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
        var menu = menuScene.Instantiate<MainMenu>();
        var target = new Control { Name = "MusicTestGameplay" };
        using var targetScene = new PackedScene();
        Check(targetScene.Pack(target) == Error.Ok, "The isolated transition target must pack");
        target.Free();
        menu.GameScene = targetScene;
        Root.AddChild(menu);
        CurrentScene = menu;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            var music = menu.GetNode<AudioStreamPlayer>("MenuMusic");
            var director = menu.GetNode<IntroMusicDirector>("MusicDirector");
            director.SetProcess(false);
            Check(music.Autoplay && music.Playing && music.Stream == stream && music.VolumeDb == -6,
                "Main menu must automatically play the prepared track at the configured volume");
            ulong initialPlayback = music.GetStreamPlayback().GetInstanceId();
            menu.OnSettingsPressed();
            menu.OnBackPressed();
            menu.OnPlayPressed();
            Check(music.Playing && music.GetStreamPlayback().GetInstanceId() == initialPlayback,
                "Settings and the new/continue screen must keep the same uninterrupted player");
            var newGame = menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton");
            newGame.EmitSignal(BaseButton.SignalName.Pressed);
            var intro = director.GetNode<AudioStreamPlayer>("IntroBaseMusic");
            Check(menu.ActiveDialogue != null && intro.Playing && intro.VolumeLinear < 0.003,
                "Starting the intro must start a very quiet first-stage voice");
            director._Process(4);
            Check(!music.Playing && intro.Playing, "The outgoing menu voice must fade away during intro entry");
            await ToSignal(this, SignalName.ProcessFrame);
            menu.ActiveDialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Check(menu.ActiveDialogue == null && music.Playing,
                "Cancelling the intro must resume menu music");
            director._Process(1);
            newGame.EmitSignal(BaseButton.SignalName.Pressed);
            Check(intro.Playing, "Repeated intro entry must start its quiet voice again");
            menu.GetNode<TextureButton>("IntroSkipLayer/SkipButton").EmitSignal(BaseButton.SignalName.Pressed);
            Check(CurrentScene == menu, "Skipping must wait for music to fade before changing scenes");
            director._Process(director.ExitFadeSeconds);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(!GodotObject.IsInstanceValid(music) && CurrentScene.Name == "MusicTestGameplay" &&
                  CurrentScene.FindChildren("*", "AudioStreamPlayer", true, false).Count == 0,
                "Entering gameplay must free the menu player with no leaking or duplicate audio");
            CurrentScene.QueueFree();
            CurrentScene = null;
            await ToSignal(this, SignalName.ProcessFrame);
            menu = menuScene.Instantiate<MainMenu>();
            Root.AddChild(menu);
            CurrentScene = menu;
            Check(menu.GetNode<AudioStreamPlayer>("MenuMusic").Playing &&
                  menu.FindChildren("*", "AudioStreamPlayer", true, false).Cast<AudioStreamPlayer>().Count(p => p.Playing) == 1,
                "Returning to the menu must start exactly one fresh music player");
        }
        finally
        {
            if (CurrentScene != null) { CurrentScene.QueueFree(); CurrentScene = null; }
        }
        await ToSignal(this, SignalName.ProcessFrame);
        // AudioServer releases stopped voices on its next mixing blocks.
        await ToSignal(CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        GD.Print($"MENU_MUSIC_CHECKS_PASS: 90 BPM, 32 beats, lossless native loop; seam deltas {jumpLeft:F7}/{jumpRight:F7}; menu/settings/intro/cancel/gameplay/reentry");
    }
}
