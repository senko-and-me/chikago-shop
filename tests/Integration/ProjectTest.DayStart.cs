using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestDayStart()
    {
        const string save = "user://hf106_day_start.json";
        var packed = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        Vector2I oldSize = Root.Size;
        string oldLocale = TranslationServer.GetLocale();
        async Task Reload()
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            game = packed.Instantiate<TradeScene>();
            game.SavePath = save;
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            Root.Size = new Vector2I(1280, 720);
            await Reload();
            var timer = game.GetNode<Label>("HUD/Root/DayTimer");
            var start = timer.GetNode<Button>("StartButton");
            using (var image = await CapturePresentation("day-start")) { }
            Check(Mathf.Abs(timer.Size.X - 207) < 0.1f && timer.OffsetLeft == 6 && timer.OffsetBottom == -6,
                "Clock must grow by 15% from 180 to 207 and sit six pixels from the lower-left corner");
            Check(!game.DayStarted && start.Visible && timer.Text == "", "A fresh day must display Start on the clock");
            game._Process(15);
            Check(game.DayRemainingSeconds == 90 && !game.DayStarted, "The day must not tick before Start");
            game.PersistProgress();
            await Reload();
            Check(!game.DayStarted && game.DayRemainingSeconds == 90, "Reload must preserve waiting for Start");
            TranslationServer.SetLocale("en");
            start = game.GetNode<Button>("HUD/Root/DayTimer/StartButton");
            Check(start.Tr(start.Text) == "Start", "Start must have an English translation");
            TranslationServer.SetLocale("ru");
            await ClickControl(game, "HUD/Root/DayTimer/StartButton");
            Check(game.DayStarted && !start.Visible && game.DayRemainingSeconds == 90, "A real click must start exactly once");
            start.EmitSignal(BaseButton.SignalName.Pressed);
            game._Process(1);
            Check(game.DayRemainingSeconds == 89 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "89",
                "After Start the timer must count down in seconds");
            await Reload();
            Check(game.DayStarted && game.DayRemainingSeconds == 89, "Reload of a running day must not demand another Start");
            game.EndDay();
            game.EndDay();
            await WaitForDayCardAnimation(game);
            var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
            card.Skip();
            await WaitForDayCardAnimation(game);
            Check(game.CurrentDay == 2 && !game.DayStarted && !game.DayUpgrades.Pending,
                "Choosing or skipping the upgrade must leave the next day waiting for Start");
            game._Process(5);
            Check(game.DayRemainingSeconds == 90, "The next day must not lose time before Start");
            game.EndDay(); // The existing day button is also a Start action while waiting.
            Check(game.DayStarted, "The existing day button must also start a waiting day");
            game.ReturnToCheckpoint();
            Check(!game.DayStarted, "Returning to a checkpoint must wait for Start");
            game.NewGame();
            Check(!game.DayStarted && game.GetNode<Button>("HUD/Root/DayTimer/StartButton").Visible,
                "New Game must show Start again");
            game.Score = 1000;
            game.TryUnlockMusicWorkshop();
            Root.Size = new Vector2I(1000, 800);
            using (var image = await CapturePresentation()) { }
            timer = game.GetNode<Label>("HUD/Root/DayTimer");
            var inventory = game.GetNode<Control>("HUD/InventoryUI/ScrollContainer");
            Check(timer.GetGlobalRect().End.X <= inventory.GetGlobalRect().Position.X,
                "The larger clock must not overlap the six-slot inventory on a narrow viewport");
            // Legacy saves already described a running day, including an active buyer.
            using (var file = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":13,\"day\":1,\"score\":220,\"energy_remaining\":3,\"day_remaining_seconds\":42}");
            await Reload();
            Check(game.DayStarted && game.DayRemainingSeconds == 42, "Saves without the new field must continue their running day");
            using (var file = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":13,\"day\":1,\"score\":220,\"energy_remaining\":3,\"day_remaining_seconds\":90,\"day_started\":false,\"showcase_items\":[0,-1,-1,-1,-1,-1]}");
            await Reload();
            game._Process(5);
            Check(!game.TradeActive && game.DayRemainingSeconds == 90, "Displayed goods must not attract buyers before Start");
            game.EndDay();
            game._Process(5);
            Check(game.TradeActive, "Buyers must resume normally after Start");
        }
        finally
        {
            game?.QueueFree();
            Root.Size = oldSize;
            TranslationServer.SetLocale(oldLocale);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
