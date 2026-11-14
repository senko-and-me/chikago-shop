using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestGameMusic()
    {
        const string path = "user://game_music_lifecycle_test.json";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        GameplayMusicDirector director = null;
        AudioStreamPlayer day = null, upgrades = null;
        DayTransitionCard card = null;

        void Create()
        {
            game = scene.Instantiate<TradeScene>();
            game.SavePath = path;
            Root.AddChild(game);
            game.SetProcess(false);
            director = game.GetNode<GameplayMusicDirector>("GameplayMusicDirector");
            director.SetProcess(false);
            day = director.GetNode<AudioStreamPlayer>("DayMusic");
            upgrades = director.GetNode<AudioStreamPlayer>("UpgradeMusic");
            card = game.GetNode<DayTransitionCard>("DayTransitionCard");
        }
        async Task FreeGame()
        {
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            game = null;
            await ToSignal(this, SignalName.ProcessFrame);
        }
        void Settle()
        {
            director._Process(director.CrossfadeSeconds + director.FadeInSeconds);
        }
        void CheckDay()
        {
            Check(director.CurrentCue == "day" && day.Playing && !upgrades.Playing &&
                  Math.Abs(day.VolumeDb - (-12.0206)) < 0.001,
                "Gameplay must use the main loop at the first intro stage's quiet level");
        }

        try
        {
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            Create();
            director._Process(0);
            Check(day.Playing && day.VolumeLinear == 0 && !upgrades.Playing,
                "A fresh game must start its main music softly, without the card track");
            Settle();
            CheckDay();
            Check(((AudioStreamWav)day.Stream).LoopEnd == 940800 &&
                  ((AudioStreamWav)upgrades.Stream).LoopEnd == 470400 &&
                  ((AudioStreamWav)day.Stream).LoopMode == AudioStreamWav.LoopModeEnum.Forward &&
                  ((AudioStreamWav)upgrades.Stream).LoopMode == AudioStreamWav.LoopModeEnum.Forward,
                "Gameplay must reuse the exact musical loops already checked for the intro");
            double dayLevel = MusicRms((AudioStreamWav)day.Stream) * day.VolumeLinear;
            ulong playback = day.GetStreamPlayback().GetInstanceId();
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            director._Process(1);
            Check(Paused && director.ProcessMode == Node.ProcessModeEnum.Always &&
                  director.CurrentCue == "day" && day.GetStreamPlayback().GetInstanceId() == playback,
                "Ordinary pause must keep the same day track without restarting it");
            pause.Close();

            foreach (bool buy in new[] { false, true })
            {
                game.Score = 1000;
                game.EndDay(); game.EndDay(); game.EndDay(); // Start the waiting day, close it, then advance.
                Check(card.IsOpen && card.IsAnimating && Paused, "The real day flow must open paused upgrade dealing");
                director._Process(0);
                Check(director.CurrentCue == "upgrades" && upgrades.Playing && day.Playing,
                    "Opening the card screen must start a crossfade, not abruptly stop the day track");
                director._Process(director.CrossfadeSeconds / 2);
                double normalizedPower = day.VolumeLinear * day.VolumeLinear +
                    Math.Pow(upgrades.VolumeLinear / director.UpgradeCompensation, 2);
                Check(day.VolumeLinear > 0 && upgrades.VolumeLinear > 0 &&
                      Math.Abs(normalizedPower - Math.Pow(Mathf.DbToLinear(director.VolumeDb), 2)) < 0.0001,
                    "The equal-power midpoint must avoid a loudness bump or dip between the matched tracks");
                float before = upgrades.VolumeLinear;
                director.SetProcess(true);
                await ToSignal(CreateTimer(0.08, true), SceneTreeTimer.SignalName.Timeout);
                director.SetProcess(false);
                Check(Paused && upgrades.VolumeLinear > before,
                    "The crossfade must actually advance while the card screen pauses gameplay");
                Settle();
                double cardLevel = MusicRms((AudioStreamWav)upgrades.Stream) * upgrades.VolumeLinear;
                Check(!day.Playing && upgrades.Playing && Math.Abs(cardLevel / dayLevel - 1) < 0.003,
                    "Upgrade music must have the same steady perceived level as the quiet day theme");
                await WaitForDayCardAnimation(game);
                if (buy) card.Choose(card.Options[0]);
                else card.Skip();
                director._Process(0.1);
                Check(card.IsOpen && card.IsAnimating && director.CurrentCue == "upgrades",
                    "Thoughtful music must continue through the cards' leaving animation");
                await WaitForDayCardAnimation(game);
                Check(!card.IsOpen && !Paused && !game.DayUpgrades.Pending,
                    "Purchase or skip must complete the existing day transition");
                director._Process(0);
                Check(director.CurrentCue == "day" && day.Playing && upgrades.Playing,
                    "The actual start of the day must crossfade back to the main theme");
                Settle();
                CheckDay();
            }

            game.NewGame();
            game.EndDay(); game.EndDay(); game.EndDay();
            director._Process(0.15);
            float[] gains = { day.VolumeLinear, upgrades.VolumeLinear };
            game.NewGame();
            director._Process(0);
            Check(!card.IsOpen && !Paused && director.CurrentCue == "day" &&
                  Math.Abs(day.VolumeLinear - gains[0]) < 0.00001 &&
                  Math.Abs(upgrades.VolumeLinear - gains[1]) < 0.00001,
                "Reset during a crossfade must retarget current gains without an audible jump");
            Settle(); CheckDay();

            game.EndDay(); game.EndDay(); game.EndDay();
            game.PersistProgress();
            var oldDay = day;
            await FreeGame();
            Check(!GodotObject.IsInstanceValid(oldDay), "Leaving gameplay must free the music players");
            Create();
            Check(card.IsOpen && game.DayUpgrades.Pending, "The fixture must restore a pending upgrade screen");
            director._Process(0);
            Check(director.CurrentCue == "upgrades" && upgrades.Playing && !day.Playing,
                "A pending-choice reload must start thoughtful music without briefly playing the day theme");
            Settle();
            Check(game.FindChildren("*", "AudioStreamPlayer", true, false).Cast<AudioStreamPlayer>().Count(p => p.Playing) == 1,
                "Reload must leave exactly one music voice after the fade");
            await WaitForDayCardAnimation(game);
            card.Skip();
            await WaitForDayCardAnimation(game);
            Settle(); CheckDay();
        }
        finally
        {
            await FreeGame();
            Paused = false;
            if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        }
        await ToSignal(CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        GD.Print("GAME_MUSIC_CHECKS_PASS: quiet day loop, matched thoughtful cards, equal-power fade, paused deal/exit, purchase/skip/reset/reload, cleanup");
    }
}
