using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestDayExpiry()
    {
        const string save = "user://hf157_day_expiry.json";
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
        async Task CheckTransition()
        {
            game._Process(0.01);
            Check(game.CurrentDay == 2 && game.DayUpgrades.Pending && game.DayTransitionOpen,
                "An expired first day must automatically open day two and its cards without clicking the clock");
            game._Process(1000);
            Check(game.CurrentDay == 2, "An open transition must not advance a second time");
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Skip();
            await WaitForDayCardAnimation(game);
            Check(!Paused && !game.DayStarted && !game.DayUpgrades.Pending,
                "After the cards, the second day must wait for Start with gameplay unpaused");
            double time = game.DayRemainingSeconds;
            game._Process(5);
            Check(game.DayRemainingSeconds == time && game.CurrentDay == 2,
                "The second day must retain its full time before Start");
        }
        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            await Reload();
            game.EndDay();
            game._Process(game.DayRemainingSeconds + 0.01);
            await CheckTransition();

            foreach (bool manual in new[] { false, true })
            {
                game.NewGame(); game.EndDay();
                CraftAtelier(game); SelectInventorySlot(game, 0); game.PlaceItemInShowcase(0);
                game._Process(game.BuyerCountdown + 0.01);
                Check(game.TradeActive, "The expiry fixture must have a real active buyer");
                if (manual) await ClickControl(game, "HUD/DayClock/Root/ClockActionButton");
                else game._Process(game.DayRemainingSeconds + 0.01);
                game._Process(1);
                Check(game.DayClosed && game.CurrentDay == 1 && game.TradeActive,
                    "Timeout or clock click must preserve the current buyer until the player finishes the sale");
                game.PersistProgress();
                await Reload();
                game._Process(1);
                Check(game.DayClosed && game.CurrentDay == 1 && game.TradeActive,
                    "Reloading a closed day must still preserve its active buyer");
                game.AcceptTrade();
                await CheckTransition();
            }

            game.NewGame(); game.EndDay();
            var craft = game.GetNode<EmbroideryGame>("EmbroideryGame");
            craft.Open(); craft.Activate(); craft.SetProcess(false);
            craft._Process(game.DayRemainingSeconds + 0.01);
            Check(game.DayClosed && game.CurrentDay == 1 && craft.IsOpen && Paused,
                "Expiry inside crafting must wait until the minigame closes");
            craft.Activate();
            await CheckTransition();

            game.NewGame(); game.EndDay();
            Paused = true;
            double remaining = game.DayRemainingSeconds;
            game._Process(1000);
            Check(game.CurrentDay == 1 && game.DayRemainingSeconds == remaining,
                "Pause must not expire or advance a day");
            Paused = false;
            game._Process(game.DayRemainingSeconds + 0.01);
            game.PersistProgress();
            await Reload();
            await CheckTransition();
        }
        finally
        {
            Paused = false;
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
    }
}
