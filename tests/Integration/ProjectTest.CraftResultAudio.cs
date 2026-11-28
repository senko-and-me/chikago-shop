using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestCraftResultAudio()
    {
        const string save = "user://craft_result_audio.json";
        var effects = SoundEffects.Instance;
        var heard = new List<SoundEffects.Cue>();
        void Observe(SoundEffects.Cue cue) => heard.Add(cue);
        effects.Played += Observe;
        TradeScene game = null;
        SoundEffects.Cue Expected(string rank) => rank == "S" ? SoundEffects.Cue.CraftGreat :
            rank is "A" or "B" ? SoundEffects.Cue.CraftNormal : SoundEffects.Cue.CraftBad;
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        try
        {
            foreach (bool polishing in new[] { false, true })
            {
                int[] thresholds = polishing ? [0, 42, 63, 78, 92, 100] : [0, 40, 60, 75, 90, 100];
                for (int tier = 0; tier < 5; tier++)
                {
                    Check(Mathf.IsEqualApprox(EmbroiderySurface.QualityToRankFill(thresholds[tier], polishing), tier / 5f),
                        "Each computed rank boundary must coincide with its painted grade band");
                    int midpoint = (thresholds[tier] + thresholds[tier + 1]) / 2;
                    float fill = EmbroiderySurface.QualityToRankFill(midpoint, polishing);
                    Check(fill > tier / 5f && fill < (tier + 1) / 5f,
                        "The visible rank must agree with the rank used for collection audio");
                }
            }
            foreach (string rank in new[] { "S", "A", "B", "C", "D" })
            {
                heard.Clear();
                effects.PlayCraftResult(rank);
                var cue = Expected(rank);
                var player = effects.GetNode<AudioStreamPlayer>(cue.ToString());
                Check(heard.SequenceEqual(new[] { cue }) && player.Playing && player.Bus == SoundEffects.BusName,
                    "Every quality tier must select its own scene-owned SFX result player");
                foreach (var other in new[] { SoundEffects.Cue.CraftGreat, SoundEffects.Cue.CraftNormal, SoundEffects.Cue.CraftBad })
                    if (other != cue) Check(!effects.GetNode<AudioStreamPlayer>(other.ToString()).Playing,
                        "Different result melodies must not overlap");
                string filename = cue == SoundEffects.Cue.CraftGreat ? "great(1)" : cue == SoundEffects.Cue.CraftNormal ? "normal(1)" : "bad";
                var stream = (AudioStreamWav)player.Stream;
                Check(stream.ResourcePath == $"res://audio/sfx/{filename}.wav" && stream.GetLength() > 0 &&
                    stream.LoopMode == AudioStreamWav.LoopModeEnum.Disabled,
                    "Result sounds must be playable one-shots using the supplied assets");
            }
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
            await Frame();
            foreach (bool music in new[] { false, true })
            foreach (string outcome in music ? new[] { "S", "D", "Early" } : new[] { "S", "B", "D", "Early" })
            {
                game.NewGame();
                game.Score = 10000;
                if (music) UnlockMusicForFixture(game);
                game.EndDay();
                var mini = game.GetNode<EmbroideryGame>(music ? "PolishingGame" : "EmbroideryGame");
                mini.SetProcess(false);
                string workshop = music ? "music" : "atelier", kind = music ? "guitar" : "hat";
                game.Crafting.OpenCatalog(workshop);
                Check(game.Crafting.Select(kind), "The result fixture must prepare the selected item");
                await Frame();
                heard.Clear();
                mini.Close();
                Check(heard.Count == 0, "Cancelling before crafting must not play a result");
                game.Crafting.Catalog.Close();
                game.Crafting.OpenCatalog(workshop);
                Check(game.Crafting.Select(kind), "The item must be available after cancellation");
                mini.Activate();
                heard.Clear();
                if (outcome == "Early")
                {
                    if (!music)
                        foreach (var point in mini.Session.Points.Take(mini.Session.Points.Count - 1))
                            mini.Session.Hit(point.X, point.Y);
                    Check(mini.StartButton.IsVisibleInTree(), "An ordinary craft must offer early collection");
                }
                else if (outcome == "B")
                {
                    for (int miss = 0; miss < 3; miss++) mini.Session.Hit(-1000, -1000);
                    while (!mini.Session.Complete)
                    {
                        var point = mini.Session.Points[mini.Session.Stitches];
                        mini.Session.Hit(point.X, point.Y);
                    }
                    mini._Process(0); // Commit and present the naturally achieved 64% / B result.
                    Check(mini.ActiveSession.Rank == "B" &&
                        EmbroiderySurface.QualityToRankFill(mini.ActiveSession.Quality, false) is >= 0.4f and < 0.6f,
                        "A real B item must occupy the painted B band before collection");
                }
                else mini.FinishForTesting(outcome == "S");
                string rank = outcome == "Early" ? (music ? "D" : "B") : mini.ActiveSession.Rank;
                Check(heard.Count == 0 && mini.IsOpen, "Finishing must wait for Collect before playing the result");
                await Frame();
                heard.Clear();
                await ClickControl(game, (music ? "PolishingGame" : "EmbroideryGame") + "/Surface/Panel/Buttons/Action");
                Check(!mini.IsOpen && heard.Where(cue => cue != SoundEffects.Cue.Hover).SequenceEqual(new[] { Expected(rank) }) &&
                    !effects.GetNode<AudioStreamPlayer>("Hover").Playing,
                    $"Real Collect in {workshop} must play exactly one {rank} result, without a generic click");
                int display = Array.IndexOf(game.ShowcaseItems, WorkshopItemCatalog.IndexOf(kind));
                Check(display >= 0 && game.GetShowcaseQualityRank(display) == rank,
                    "The collected item's saved rank must match its result sound, including an automatic offer");
                heard.Clear();
                mini.Activate();
                Check(heard.Count == 0, "Repeated collection input must not replay the reward sound");
            }
        }
        finally
        {
            effects.Played -= Observe;
            game?.QueueFree();
            Paused = false;
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
            await Frame();
        }
    }
}
