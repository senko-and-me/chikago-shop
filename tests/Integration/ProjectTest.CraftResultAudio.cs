using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;
using ChikagoShop.Domain.Persistence;

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
            var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
            Scrub[] polishingRoute = null;
            foreach (bool music in new[] { false, true })
            foreach (string outcome in new[] { "S", "A", "B", "C", "D", "Early" })
            {
                string workshop = music ? "music" : "atelier", kind = music ? "guitar" : "hat";
                if (game != null) { game.QueueFree(); await Frame(); }
                ProgressSaveStore.Save(save, new ProgressData
                {
                    SaveVersion = ProgressJson.CurrentVersion, Score = 10000, Day = 8, QuotaStage = 1,
                    DayStarted = true, DayRemainingSeconds = 59, QuotaVisit = QuotaVisitStage.Rewarded,
                    MusicWorkshopUnlocked = music, WaitingBuyerPresent = true, WaitingBuyerItemKind = kind,
                    WaitingBuyerMusic = music, CustomerIndex = 0, BuyerWaitInitial = 20, BuyerWaitRemaining = 20
                });
                game = scene.Instantiate<TradeScene>();
                game.SavePath = save;
                game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
                Root.AddChild(game);
                game.SetProcess(false);
                game.Crafting.Catalog.SetProcess(false);
                game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
                var mini = game.GetNode<EmbroideryGame>(music ? "PolishingGame" : "EmbroideryGame");
                mini.SetProcess(false);
                await Frame();
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
                else if (outcome != "D")
                {
                    if (music)
                    {
                        var session = mini.Polishing;
                        int misses = outcome switch { "S" => 3, "A" => 5, "B" => 10, _ => 14 };
                        for (int miss = 0; miss < misses; miss++)
                        {
                            session.BeginRub(0, 0);
                            session.EndRub();
                            session.Tick(0.25);
                        }
                        polishingRoute ??= BuildScrubRoute(session, false);
                        SimulatePolishing(session, polishingRoute, 3000);
                        Check(session.Complete, "The polishing route must finish naturally after the simulated misses");
                    }
                    else
                    {
                        int misses = outcome switch { "S" => 1, "A" => 2, "B" => 4, _ => 6 };
                        var surface = mini.GetNode<EmbroiderySurface>("Surface");
                        var percentage = surface.GetNode<Label>("Panel/Quality");
                        Check(percentage.Text == "100%" && surface.MeterFill == 1f,
                            "Full embroidery quality must be shown as a full meter");
                        for (int miss = 0; miss < misses; miss++)
                        {
                            mini.Session.Hit(-1000, -1000);
                            mini._Process(0);
                            int quality = 100 - (miss + 1) * 12;
                            Check(percentage.Text == $"{quality}%" && Mathf.IsEqualApprox(surface.MeterFill, quality / 100f),
                                "Each embroidery miss must lower the displayed percentage and meter by the same 12 points");
                        }
                        foreach (var point in mini.Session.Points) mini.Session.Hit(point.X, point.Y);
                    }
                    mini._Process(0);
                }
                else mini.FinishForTesting(false);
                string rank = outcome == "Early" ? (music ? "D" : "B") : outcome;
                if (outcome != "Early")
                    Check(mini.ActiveSession.Rank == rank,
                        $"{workshop} fixture must finish with {rank}, got {mini.ActiveSession.Rank}");
                if (outcome == "S")
                    Check(mini.ActiveSession.Quality is >= 80 and < 90,
                        "An actual score just above the painted S divider must earn S");
                Check(heard.Count == 0 && mini.IsOpen, "Finishing must wait for Collect before playing the result");
                int moneyBeforeCollect = game.Score;
                Check(moneyBeforeCollect == 9980, "Starting either workshop must charge the configured $20 just once");
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
                Check(game.Score == moneyBeforeCollect && game.TradeActive &&
                    game.ActiveTradeQualityRank == rank && game.BuyerWaitRemaining == 20,
                    "Collect must preserve cash, opening a matching buyer offer with the item's grade");
                int adjustment = rank switch { "S" => 20, "A" => 10, "B" => 0, "C" => -10, _ => -20 };
                int offer = game.OfferedPrice;
                const int speedAtFullPatience = 5;
                var accept = game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
                Check(!accept.Disabled && accept.IsVisibleInTree(), "The normal accept button must be available");
                accept.EmitSignal(Button.SignalName.Pressed);
                int payout = game.Score - moneyBeforeCollect;
                Check(!game.TradeActive && payout == offer + speedAtFullPatience + adjustment,
                    $"{workshop} {rank}: expected {offer} + 5 + ({adjustment}), got {payout}");
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
