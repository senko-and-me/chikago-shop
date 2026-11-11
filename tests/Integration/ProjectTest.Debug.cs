using System;
using System.Threading.Tasks;
using ChikagoShop.Domain.Crafting;
using Godot;

public partial class ProjectTest
{
    private async Task TestDebugHotkeys()
    {
        Check(OS.IsDebugBuild(), "Manual test shortcuts require a debug Godot build");
        const string save = "user://debug_hotkeys_test.json";
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        // Keep this fixture independent of unrelated editor overrides in the scene.
        game.StartingMoney = 220; game.FirstQuota = 250; game.SecondQuota = 450; game.SavePath = save;
        async Task Press(Key key, bool echo = false, bool shift = false)
        {
            Root.PushInput(new InputEventKey { Keycode = key, Pressed = true, Echo = echo, ShiftPressed = shift }, true);
            Root.PushInput(new InputEventKey { Keycode = key, Pressed = false, ShiftPressed = shift }, true);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            Root.AddChild(game);
            game.SetProcess(false);
            game.NewGame();
            Check(game.HasNode("DebugHotkeys"), "Debug scene must attach the test input controller");
            await Press(Key.F6);
            Check(game.Score == 320 && game.GetNode<Label>("HUD/Root/ScorePanel/ScoreLabel").Text.Contains("320"),
                "F6 must grant exactly100 and refresh the visible balance");
            Check(ProgressSaveStore.Load(save, 3).Score == 320, "Granted funds must be saved through normal persistence");
            await Press(Key.F6, echo: true);
            await Press(Key.F6, shift: true);
            Check(game.Score == 320, "Repeat and modified keys must not grant money");
            await Press(Key.F7);
            await Press(Key.F7);
            Check(game.DayRemainingSeconds == 150 && game.GetNode<Label>("HUD/Root/DayTimer").Text == "150",
                "Time grants must stack and update the HUD immediately");
            game._Process(150);
            Check(game.DayClosed, "Timer fixture must expire");
            await Press(Key.F7);
            Check(!game.DayClosed && game.CurrentDay == 1 && game.DayRemainingSeconds == 30,
                "F7 must reopen the same expired day with30 seconds");
            Check(game.GetNode<TextureRect>("HUD/Root/DayTimer/Clock").Texture.ResourcePath.EndsWith("day_timer_empty.png"),
                "Reopening the day must restore the active clock sprite");
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            await Press(Key.F6);
            await Press(Key.F7);
            Check(game.Score == 320 && game.DayRemainingSeconds == 30, "Pause settings/menu must block debug grants");
            pause.Close();
            game.DayUpgrades.Offer(2);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Open(1, 2, game.DayUpgrades.Options, game.Score);
            await Press(Key.F6);
            await Press(Key.F7);
            Check(game.Score == 320 && game.DayRemainingSeconds == 30, "Day-card selection must block debug grants");
            game.NewGame();

            foreach (bool music in new[] { false, true })
            foreach (bool good in new[] { false, true })
            {
                game.NewGame();
                if (music) game.TryUnlockMusicWorkshop();
                var craft = game.GetNode<EmbroideryGame>(music ? "PolishingGame" : "EmbroideryGame");
                craft.Open();
                craft.SetProcess(false);
                int balance = game.Score, energy = game.EnergyRemaining, count = game.StoredItems.Length;
                if (good) craft.Activate(); // Cover Ready and already Playing shortcuts.
                await Press(good ? Key.F9 : Key.F8);
                Check(craft.IsOpen && craft.ActiveSession.State == EmbroideryState.Finished &&
                    craft.ActiveSession.Quality == (good ? 100 : 0) && craft.ActiveSession.Complete == good,
                    "F8/F9 must finish either minigame with the chosen visible result");
                Check(craft.GetNode<Label>("Surface/Quality").Text == (good ? "100%" : "0%") &&
                    game.Score == balance - (music ? 50 : 25) && game.EnergyRemaining == energy - 1 &&
                    game.StoredItems.Length == count + 1,
                    "Debug completion must charge and acquire exactly once using normal craft behavior");
                await Press(good ? Key.F8 : Key.F9);
                Check(craft.ActiveSession.Quality == (good ? 100 : 0) && game.StoredItems.Length == count + 1,
                    "Repeated skips must not change a finished result or create another item");
                double seconds = game.DayRemainingSeconds;
                await Press(Key.F6);
                await Press(Key.F7);
                Check(game.Score == balance - (music ? 50 : 25) + 100 && game.DayRemainingSeconds == seconds + 30,
                    "Resource hotkeys must remain usable inside a paused crafting overlay");
                craft.Activate();
                Check(!craft.IsOpen && !Paused, "The normal collect action must close the skipped craft");
                await Press(Key.F8);
                await Press(Key.F9);
                Check(game.StoredItems.Length == count + 1, "Skip keys outside minigames must not create items");
            }
            game.Score = int.MaxValue - 10;
            await Press(Key.F6);
            Check(game.Score == int.MaxValue, "Debug funds must not overflow to a negative balance");
        }
        finally
        {
            Paused = false;
            game.QueueFree();
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
