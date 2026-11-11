using System;
using System.Threading.Tasks;
using ChikagoShop.Domain.Days;
using Godot;

public partial class ProjectTest
{
    private async Task TestCardDealing()
    {
        const string save = "user://card_dealing_test.json";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = null;
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        TradeScene Create()
        {
            var instance = scene.Instantiate<TradeScene>();
            instance.StartingMoney = 220; instance.FirstQuota = 250; instance.SecondQuota = 450;
            instance.SavePath = save;
            Root.AddChild(instance);
            instance.SetProcess(false);
            instance.GetNode<Camera2D>("Camera2D").SetProcess(false);
            return instance;
        }
        async Task Capture(string name)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chikago-hf90-" + name + ".png"));
        }
        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            game = Create();
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "ru", "en" })
            {
                Root.Size = size;
                TranslationServer.SetLocale(language);
                game.NewGame();
                game.EndDay(); game.EndDay();
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.IsOpen && card.IsAnimating && Paused && game.DayUpgrades.Pending,
                    "Dealing must overlay the paused game while the upgrade remains pending");
                var shade = card.GetNode<ColorRect>("Root/Shade");
                Check(shade.Color.A > 0.3f && shade.Color.A < 0.9f &&
                    ((StyleBoxFlat)card.GetNode<PanelContainer>("Root/Center/Card").GetThemeStylebox("panel")).BgColor.A == 0,
                    "Translucent shade and transparent tray must preserve the gameplay background");
                card.Choose(DayUpgrade.Cash); card.Skip();
                Check(game.Score == 220 && game.DayUpgrades.Pending, "Early input must not commit a choice during dealing");
                await ToSignal(CreateTimer(0.36, true), SceneTreeTimer.SignalName.Timeout);
                var first = card.GetNode<PanelContainer>("Root/Center/Card/Content/Choices/Cash");
                Check(card.IsAnimating && first.GetNode<Control>("Back").Visible && first.Scale.X < 1 &&
                    card.GetNode<Control>("Root/Deck").IsVisibleInTree(),
                    "Cards must fly from the visible deck with backs showing before they flip");
                if (language == "ru" && size.X == 1280) await Capture("dealing");
                await WaitForDayCardAnimation(card);
                Check(Root.GuiGetFocusOwner() == null && game.DayRemainingSeconds == 90,
                    "Finishing the deal must not preselect a purchase or consume day time");
                foreach (var upgrade in card.Options)
                {
                    var face = card.GetNode<PanelContainer>("Root/Center/Card/Content/Choices/" + upgrade);
                    Check(face.Scale.IsEqualApprox(Vector2.One) && Mathf.IsZeroApprox(face.Rotation) &&
                        !face.GetNode<Control>("Back").Visible && Root.GetVisibleRect().Encloses(face.GetGlobalRect()) &&
                        face.Size.Y > face.Size.X,
                        "Settled portrait cards must fit the viewport in both languages and sizes");
                    foreach (string name in new[] { "Amount", "Title", "Description", "Duration", "Choose" })
                        Check(Root.GetVisibleRect().Encloses(face.GetNode<Control>("Content/" + name).GetGlobalRect()),
                            "Every card label and button must remain inside the screen");
                }
                await Capture("ready-" + language + "-" + size.X);
                bool buy = language == "ru";
                if (buy)
                {
                    Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Key1 }, true);
                    Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Key1 }, true);
                }
                else card.Skip();
                Check(card.IsAnimating && card.IsOpen && Paused && game.Score == 220,
                    "Game and transaction must wait for the exit animation");
                card.Choose(DayUpgrade.Energy); card.Skip();
                await WaitForDayCardAnimation(card);
                Check(!card.IsOpen && !Paused && !game.DayUpgrades.Pending && game.CurrentDay == 2 &&
                    game.Score == (buy ? 250 : 220) && game.DayUpgrades.Selected == (buy ? DayUpgrade.Cash : DayUpgrade.None),
                    "Exactly one purchase or free skip must apply after exit and resume the new day");
            }
            game.NewGame();
            game.EndDay(); game.EndDay();
            var active = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Root.Size = new Vector2I(960, 540);
            await WaitForDayCardAnimation(active);
            active.Choose(DayUpgrade.Cash);
            game.NewGame(); // Cancels an already queued exit transaction.
            await ToSignal(CreateTimer(0.7, true), SceneTreeTimer.SignalName.Timeout);
            Check(!Paused && !active.IsOpen && game.Score == 220 && game.CurrentDay == 1,
                "Reset during exit must cancel its callback and release pause without charging");
            game.EndDay(); game.EndDay();
            await WaitForDayCardAnimation(active);
            active.Choose(DayUpgrade.Cash);
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            active = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Check(active.IsOpen && game.DayUpgrades.Pending && game.Score == 220,
                "Reload during exit must restore the uncommitted choice without a duplicate reward");
            await WaitForDayCardAnimation(active);
            active.Skip();
            await WaitForDayCardAnimation(active);
            game.NewGame();
            game.EndDay(); game.EndDay();
            game.QueueFree(); // Cancel deferred layout and entrance callbacks too.
            await ToSignal(this, SignalName.ProcessFrame);
            game = null;
            await ToSignal(CreateTimer(0.2, true), SceneTreeTimer.SignalName.Timeout);
            Check(!Paused, "Removing the scene during dealing must release its owned pause");
        }
        finally
        {
            Paused = false;
            if (game != null && GodotObject.IsInstanceValid(game)) game.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(originalLocale);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
