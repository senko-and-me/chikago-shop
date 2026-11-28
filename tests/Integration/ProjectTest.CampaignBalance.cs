using System.Linq;
using System.Threading.Tasks;
using ChikagoShop.Domain.Persistence;
using Godot;

public partial class ProjectTest
{
    private async Task TestCampaignBalance()
    {
        const string save = "user://campaign_balance_test.json";
        var config = EconomyConfigProvider.Load();
        Check(config.Days.TotalDays == 14 && config.Quotas[0].DeadlineDay == 7 && config.Quotas[0].Amount == 750 &&
            config.Quotas[1].DeadlineDay == 14 && config.Quotas[1].Amount == 1500, "Keep the imported fourteen-day balance exactly");
        TradeScene game = null;
        async Task Reload()
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        async Task WaitForEnding()
        {
            ulong deadline = Time.GetTicksMsec() + 5000;
            var notice = game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice");
            while (!notice.IsOpen || SceneTransition.IsActive(this))
            {
                Check(Time.GetTicksMsec() < deadline, "The final quota notice must finish opening after its fade");
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        try
        {
            foreach (bool enough in new[] { true, false })
            {
                ProgressSaveStore.Save(save, new ProgressData
                {
                    SaveVersion = ProgressJson.CurrentVersion, Day = 14, DayStarted = true,
                    Score = enough ? 1500 : 1499, QuotaStage = 1, MusicWorkshopUnlocked = true,
                    QuotaVisit = QuotaVisitStage.Rewarded, CheckpointQuotaVisit = QuotaVisitStage.Rewarded,
                    CheckpointDay = 8, CheckpointScore = 1000, EnergyRemaining = 6
                });
                await Reload();
                Check(game.CurrentDay == 14 && game.DayRemainingSeconds == 90, "The last musical day must restore with ninety seconds");
                game.EndDay();
                game._Process(0);
                await WaitForEnding();
                Check(game.Completed == enough && game.Failed != enough && game.MafiaNoticePending &&
                    game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice").IsOpen && !game.GetNode<DayTransitionCard>("DayTransitionCard").IsOpen,
                    $"Day14 result: enough={enough}, completed={game.Completed}, failed={game.Failed}, pending={game.MafiaNoticePending}, notice={game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice").IsOpen}, card={game.GetNode<DayTransitionCard>("DayTransitionCard").IsOpen}, closed={game.DayClosed}, paused={Paused}");
                int cash = enough ? 0 : 1499;
                Check(game.Score == cash, "The final payment must charge exactly $1500 only when affordable");
                game.EndDay();
                Check(game.Score == cash && game.CurrentDay == 14, "Repeated end input must not charge again");
                await Reload();
                await WaitForEnding();
                Check(game.Score == cash && game.Completed == enough && game.Failed != enough &&
                    game.GetNode<MafiaQuotaNotice>("MafiaQuotaNotice").IsOpen, "Continue must preserve both terminal outcomes without charging again");
                game.NewGame();
                Check(game.CurrentDay == 1 && game.Score == 220 && !game.Failed && !game.Completed &&
                    !game.MusicWorkshopUnlocked && !game.QuotaVisitPending && game.DayRemainingSeconds == 60 &&
                    game.ShowcaseItems.All(item => item == -1), "New Game must restore the imported initial balance and clear the campaign");
            }
        }
        finally
        {
            game?.QueueFree();
            Paused = false;
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(save);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
