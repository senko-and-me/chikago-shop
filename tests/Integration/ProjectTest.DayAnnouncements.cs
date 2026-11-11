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
            game._Process(0);
            var overlay = game.GetNode<DayAnnouncementOverlay>("DayAnnouncements");
            var title = overlay.GetNode<Label>("Root/Title");
            overlay.SetProcess(false);
            Check(overlay.IsShowing && title.Text == "Магазин открыт" && overlay.Opacity == 0 && !Paused,
                "New day must start a nonblocking opening announcement with a transparent first frame");
            foreach (string path in new[] { "Root", "Root/Shade", "Root/Title" })
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
            game.EndDay();
            game.EndDay();
            game._Process(0);
            Check(game.DayTransitionOpen && !overlay.IsShowing,
                "Next-day announcement must wait until the upgrade card closes");
            game.GetNode<DayTransitionCard>("DayTransitionCard").Choose(DayUpgrade.Time);
            game._Process(0);
            overlay.Dismiss();
            Check(game.DayRemainingSeconds == 120, "Time-upgrade fixture must have120 seconds");
            game._Process(59.9);
            Check(!overlay.IsShowing, "Extended days must not announce midday at the old45-second threshold");
            game._Process(0.2);
            Check(overlay.IsShowing, "Extended-day midday must follow the actual120-second duration");
            game._Process(1000);
            Check(!overlay.IsShowing && game.DayClosed, "Day expiry must cancel the announcement");
            game.ReturnToCheckpoint();
            game._Process(0);
            Check(overlay.IsShowing && overlay.GetNode<Label>("Root/Title").Text == "Магазин открыт",
                "Restarting a checkpoint must reset opening and midday milestones");
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
