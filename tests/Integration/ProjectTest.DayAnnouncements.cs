using System;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestDayAnnouncements()
    {
        const string save = "user://day_announcements_test.json";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        string locale = TranslationServer.GetLocale();
        Vector2I originalSize = Root.Size;
        TradeScene Create()
        {
            var instance = scene.Instantiate<TradeScene>();
            instance.StartingMoney = 220; instance.FirstQuota = 250; instance.SecondQuota = 450;
            instance.SavePath = save; // Independent of existing editor null overrides.
            Root.AddChild(instance);
            instance.SetProcess(false);
            return instance;
        }
        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            Root.Size = new Vector2I(1280, 720);
            TranslationServer.SetLocale("ru");
            game = Create();
            game.NewGame();
            game.EndDay(); // Explicit Start introduced in HF-106.
            game._Process(0);
            var overlay = game.GetNode<DayAnnouncementOverlay>("DayAnnouncements");
            var title = overlay.GetNode<Label>("Root/Title");
            var details = overlay.GetNode<Label>("Root/Details");
            overlay.SetProcess(false);
            Check(overlay.IsShowing && title.Text == "Магазин открыт" && overlay.Opacity == 0 && !Paused,
                "New day must start a nonblocking opening announcement with a transparent first frame");
            Check(details.Visible && details.Text == "Осталось 7 дней\nДо промежуточной квоты: 4 дня",
                "Opening must directly show remaining days and the interim quota deadline, including today");
            float groupCenter = (title.GetGlobalRect().Position.Y + details.GetGlobalRect().End.Y) / 2;
            Check(Mathf.Abs(groupCenter - Root.GetVisibleRect().Size.Y * 0.6f) < 1,
                "The announcement block must sit slightly below center at sixty percent of viewport height");
            foreach (string path in new[] { "Root", "Root/Shade", "Root/Title", "Root/Details" })
                Check(overlay.GetNode<Control>(path).MouseFilter == Control.MouseFilterEnum.Ignore,
                    "All full-screen announcement controls must pass pointer input through");
            overlay._Process(0.225);
            Check(overlay.Opacity > 0 && overlay.Opacity < 1, "Opening must fade in smoothly");
            overlay._Process(0.3);
            Check(overlay.Opacity == 1, "Opening must briefly hold at full opacity");
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-hf87-opening.png"));
            float alpha = overlay.Opacity;
            await PressEscapeForTest();
            Check(Paused && !overlay.CanProcess() && overlay.Opacity == alpha,
                "Escape must still open pause and freeze announcement animation");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            overlay._Process(2.5);
            Check(overlay.IsShowing && overlay.Opacity > 0 && overlay.Opacity < 1,
                "The announcement must fade slowly over several seconds");
            overlay._Process(2.1);
            Check(!overlay.IsShowing && !overlay.IsProcessing(), "Finished animation must hide and stop processing");
            game._Process(44.9);
            Check(!overlay.IsShowing, "Midday must not appear before the halfway point");
            game._Process(0.2);
            overlay.SetProcess(false);
            Check(overlay.IsShowing && title.Text == "Середина дня", "Crossing halfway must show midday");
            Check(!details.Visible, "Midday must not retain the new-day countdown");
            overlay._Process(0.6);
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "en", "ru" })
            {
                Root.Size = size;
                TranslationServer.SetLocale(language);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(title.Tr(title.Text) == (language == "en" ? "Midday" : "Середина дня") &&
                    Root.GetVisibleRect().Encloses(title.GetGlobalRect()),
                    "Announcement must translate live and fit both viewport sizes");
            }
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-hf87-midday.png"));
            overlay.Dismiss();
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.F7 }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.F7 }, true);
            game._Process(30.1);
            Check(!overlay.IsShowing, "Adding debug time and recrossing halfway must not repeat midday");
            game.PersistProgress();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            game._Process(0);
            overlay = game.GetNode<DayAnnouncementOverlay>("DayAnnouncements");
            Check(!overlay.IsShowing, "Continuing after halfway must not replay opening or midday");
            game.NewGame();
            game.EndDay(); // Start before testing manual closure.
            game.EndDay();
            Check(overlay.IsShowing && overlay.GetNode<Label>("Root/Title").Text == "Магазин закрыт",
                "Manual day closure must show the shop-closed announcement");
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.F7 }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.F7 }, true);
            Check(!game.DayClosed && !overlay.IsShowing,
                "Debug reopening must immediately clear the stale shop-closed notice");
            game.EndDay();
            game.EndDay();
            game._Process(0);
            Check(game.DayTransitionOpen && !overlay.IsShowing,
                "Next-day announcement must wait until the upgrade card closes");
            await WaitForDayCardAnimation(game);
            game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(DayUpgrade.Time);
            await WaitForDayCardAnimation(game);
            game.EndDay(); // Start day two after the upgrade choice.
            game._Process(0);
            overlay.Dismiss();
            Check(game.DayRemainingSeconds == 120, "Time-upgrade fixture must have120 seconds");
            game._Process(59.9);
            Check(!overlay.IsShowing, "Extended days must not announce midday at the old45-second threshold");
            game._Process(0.2);
            Check(overlay.IsShowing, "Extended-day midday must follow the actual120-second duration");
            game._Process(1000);
            title = overlay.GetNode<Label>("Root/Title");
            overlay.SetProcess(false);
            Check(overlay.IsShowing && game.DayClosed && title.Text == "Магазин закрыт",
                "Day expiry must replace midday with the shop-closed announcement");
            TranslationServer.SetLocale("en");
            Check(title.Tr(title.Text) == "Shop is closed", "Closing announcement must support English");
            overlay._Process(0.6);
            game._Process(1);
            Check(overlay.Opacity == 1, "Repeated closed-day ticks must not restart the fade");
            overlay._Process(5);
            game._Process(1);
            Check(!overlay.IsShowing, "Closing announcement must fade away and stay dismissed on closed ticks");
            game.PersistProgress();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            game._Process(0);
            overlay = game.GetNode<DayAnnouncementOverlay>("DayAnnouncements");
            Check(game.DayClosed && !overlay.IsShowing, "Continuing a closed save must not replay the closing notice");
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.F7 }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.F7 }, true);
            game._Process(30);
            Check(overlay.IsShowing && overlay.GetNode<Label>("Root/Title").Text == "Магазин закрыт",
                "A debug-reopened day must allow a fresh closing announcement");
            game.ReturnToCheckpoint();
            game.EndDay(); // Checkpoints also wait for explicit Start.
            game._Process(0);
            Check(overlay.IsShowing && overlay.GetNode<Label>("Root/Title").Text == "Магазин открыт",
                "Restarting a checkpoint must reset opening and midday milestones");
            overlay.SetProcess(false);
            foreach (int day in new[] { 1, 3, 4, 5, 6, 7 })
            foreach (string language in new[] { "ru", "en" })
            {
                overlay.BeginDay();
                overlay.Observe(90, 90, false, day, 7, 4);
                TranslationServer.SetLocale(language);
                overlay.SetProcess(false);
                overlay._Process(0.6);
                details = overlay.GetNode<Label>("Root/Details");
                int left = 8 - day;
                string ru = left == 1 ? "Остался 1 день"
                    : left is >= 2 and <= 4 ? $"Осталось {left} дня" : $"Осталось {left} дней";
                string expected = language == "en" ? $"{left} {(left == 1 ? "day" : "days")} left" : ru;
                if (day <= 4)
                {
                    int quotaLeft = 5 - day;
                    expected += language == "en" ? $"\nInterim quota: {quotaLeft} {(quotaLeft == 1 ? "day" : "days")} left"
                        : $"\nДо промежуточной квоты: {quotaLeft} {(quotaLeft == 1 ? "день" : "дня")}";
                }
                Check(details.Text == expected, "Direct countdown must handle quota deadline, first/last day, Russian/English plurals and live locale changes");
                Check(Root.GetVisibleRect().Encloses(details.GetGlobalRect()), "Day countdown must fit inside the viewport");
                if (day == 3 && language == "ru")
                {
                    var hud = game.GetNode<CanvasLayer>("HUD");
                    hud.Hide();
                    using (var image = await CapturePresentation("quota-countdown")) { }
                    hud.Show();
                }
            }
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-hf116-last-day-en.png"));
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(locale);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
