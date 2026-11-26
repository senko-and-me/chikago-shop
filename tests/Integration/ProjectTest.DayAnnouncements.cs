using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestDayAnnouncements()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            var overlay = game.GetNode<DayAnnouncementOverlay>("DayAnnouncements");
            game.EndDay();
            Check(game.DayStarted && !game.DayClosed && !overlay.IsShowing &&
                game.GetNode<Node2D>("World/TradingRoom/Customer").Visible,
                "The day and first customer must start without an announcement");
            game._Process(game.DayRemainingSeconds / 2 + .1);
            Check(!overlay.IsShowing && !game.DayClosed, "Midday must not show or dim the game");
            game._Process(game.DayRemainingSeconds);
            Check(game.DayClosed && !overlay.IsShowing && !game.DayTransitionOpen,
                "Expiry must close the shop without an announcement");
            game._Process(0);
            Check(game.CurrentDay == 2 && game.DayTransitionOpen && !overlay.IsShowing,
                "The ordinary card transition must still follow closing");
            game.NewGame();
            game.EndDay();
            game.EndDay();
            Check(game.DayClosed && !overlay.IsShowing, "Manual closure must also stay silent");

            int rewardCallbacks = 0;
            overlay.ShowReward("Получено 1000$", () => rewardCallbacks++);
            overlay.SetProcess(false);
            Check(overlay.IsShowing && overlay.Opacity == 0, "Gift begins with a transparent fade");
            overlay._Process(.225);
            Check(overlay.Opacity > 0 && overlay.Opacity < 1, "Gift still fades in");
            overlay._Process(5);
            Check(!overlay.IsShowing && rewardCallbacks == 1, "Gift still dismisses and resumes the flow exactly once");
        }
        finally
        {
            game.QueueFree();
            Paused = false;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
