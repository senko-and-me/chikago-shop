using System;
using System.Linq;
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
            instance.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
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
        string previousDebugPath = DebugBalanceSettings.StoragePath;
        const string debugPath = "user://card_dealing_balance.json";
        try
        {
            DebugBalanceSettings.StoragePath = debugPath;
            var profile = new ChikagoShop.Domain.Configuration.BalanceProfile();
            int seed = Enumerable.Range(1, 100).First(value => new DayUpgradeRules { Seed = value }.Options(2)[0] == DayUpgrade.Cash);
            profile.Numbers["cards.seed"] = seed;
            profile.Numbers["start.cash"] = 220;
            Check(DebugBalanceSettings.TrySave(profile, out string error), error);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            game = Create();
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "ru", "en" })
            {
                Root.Size = size;
                TranslationServer.SetLocale(language);
                game.NewGame();
                var camera = game.GetNode<WorldCamera>("Camera2D");
                Vector2 initialPosition = camera.Position;
                Vector2 initialZoom = camera.Zoom;
                camera.Position += new Vector2(250, 120);
                camera.Zoom = initialZoom * 1.6f;
                camera.ClampToBounds();
                Vector2 closeupPosition = camera.Position;
                game.EndDay(); game.EndDay(); game.EndDay();
                var card = game.GetNode<DayTransitionCard>("DayTransitionCard");
                Check(card.IsOpen && card.IsAnimating && Paused && game.DayUpgrades.Pending,
                    "Dealing must overlay the paused game while the upgrade remains pending");
                var shade = card.GetNode<ColorRect>("Root/Shade");
                Check(shade.Color.A > 0.3f && shade.Color.A < 0.9f &&
                    ((StyleBoxFlat)card.GetNode<PanelContainer>("Root/Center/Card").GetThemeStylebox("panel")).BgColor.A == 0,
                    "Translucent shade and transparent tray must preserve the gameplay background");
                card.Choose(DayUpgrade.Cash); card.Skip();
                Check(game.Score == 220 && game.DayUpgrades.Pending, "Early input must not commit a choice during dealing");
                var hud = game.GetNode<Control>("HUD/Root");
                var clock = game.GetNode<Control>("HUD/DayClock/Root");
                var deck = card.GetNode<Control>("Root/Deck");
                Check(camera.IsReturningToOverview && camera.Position == closeupPosition && hud.Visible && deck.Modulate.A == 0,
                    "The camera return must begin before hiding UI or revealing the deck, with no initial jump");
                await ToSignal(CreateTimer(0.28, true), SceneTreeTimer.SignalName.Timeout);
                Check(camera.Position.DistanceTo(initialPosition) < closeupPosition.DistanceTo(initialPosition) &&
                    camera.Position.DistanceTo(initialPosition) > 0.1f && hud.Visible && deck.Modulate.A == 0,
                    "The camera must travel smoothly while the UI and hidden deck retain their first-stage states");
                await ToSignal(CreateTimer(0.5, true), SceneTreeTimer.SignalName.Timeout);
                Check(camera.Zoom.DistanceTo(initialZoom) < 0.001f && camera.Position.DistanceTo(initialPosition) < 0.05f &&
                    hud.Modulate.A < 1 && hud.Modulate.A > 0 && deck.Modulate.A == 0,
                    "Only after reaching the NewGame overview may UI begin fading out");
                await ToSignal(CreateTimer(0.3, true), SceneTreeTimer.SignalName.Timeout);
                Check(!hud.Visible && !clock.Visible && !game.GetNode<Control>("HUD/InventoryUI").Visible &&
                    !game.GetNode<Control>("World/Atelier/GetItemButton").Visible &&
                    game.GetNode<Control>("World/MusicWorkshop/LockedOverlay/LockedCurtain").IsVisibleInTree() &&
                    deck.Position.Y > Root.GetVisibleRect().Size.Y - deck.Size.Y - 28,
                    "All gameplay UI must be hidden before the deck rises from below, while background curtains remain visible");
                await ToSignal(CreateTimer(0.55, true), SceneTreeTimer.SignalName.Timeout);
                var first = card.GetNode<PanelContainer>("Root/Center/Card/Content/Choices/" + card.Options[0]);
                Check(card.IsAnimating && first.GetNode<Control>("Back").Visible && first.Scale.X < 1 &&
                    card.GetNode<Control>("Root/Deck").IsVisibleInTree(),
                    "Cards must fly from the visible deck with backs showing before they flip");
                using (var backArt = GD.Load<Texture2D>("res://art/ui/card_back.png").GetImage())
                    Check(backArt.GetSize() == new Vector2I(1391, 1991) && backArt.GetPixel(695, 995).R < 0.2f &&
                        backArt.GetPixel(0, 0).A == 0,
                        "The deck and flying backs must load the original textured, transparent card art");
                if (language == "ru" && size.X == 1280) await Capture("dealing");
                await WaitForDayCardAnimation(card);
                Check(Root.GuiGetFocusOwner() == null && game.DayRemainingSeconds == 45,
                    "Finishing the deal must not preselect a purchase or consume day time");
                foreach (var upgrade in card.Options)
                {
                    var face = card.GetNode<PanelContainer>("Root/Center/Card/Content/Choices/" + upgrade);
                    Check(face.Scale.IsEqualApprox(Vector2.One) && Mathf.IsZeroApprox(face.Rotation) &&
                        !face.GetNode<Control>("Back").Visible && Root.GetVisibleRect().Encloses(face.GetGlobalRect()) &&
                        face.Size.Y > face.Size.X,
                        "Settled portrait cards must fit the viewport in both languages and sizes");
                    int cardCost = game.DayUpgrades.CostFor(upgrade);
                    const string expectedArt = "res://art/ui/card_face.png";
                    Check(face.GetThemeStylebox("panel") is StyleBoxTexture faceArt &&
                        faceArt.Texture.ResourcePath == expectedArt,
                        "Card artwork must match the configured price tier");
                    foreach (string name in new[] { "Amount", "Title", "Description", "Duration", "Choose" })
                    {
                        var entry = face.GetNode<Control>("Content/" + name);
                        Check(Root.GetVisibleRect().Encloses(entry.GetGlobalRect()) &&
                            face.GetGlobalRect().GrowIndividual(-face.Size.X * 0.07f, -face.Size.Y * 0.04f,
                                -face.Size.X * 0.07f, -face.Size.Y * 0.04f).Encloses(entry.GetGlobalRect()),
                            "Every card label and button must remain within the artwork's white interior");
                    }
                    var purchase = face.GetNode<Button>("Content/Choose");
                    foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
                        Check(purchase.GetThemeStylebox(state) is StyleBoxTexture paper &&
                            paper.Texture.ResourcePath == "res://art/ui/panel_small.png",
                            "Purchase states must retain the existing drawn paper border");
                    Check(purchase.GetThemeColor("font_color").R < 0.2f &&
                        purchase.GetThemeColor("font_pressed_color").R > 0.9f,
                        "Purchase text must contrast with light paper and the dark pressed state");
                }
                await Capture("ready-" + language + "-" + size.X);
                if (language == "ru" && size.X == 1280)
                {
                    var purchase = first.GetNode<Button>("Content/Choose");
                    Vector2 point = purchase.GetGlobalRect().GetCenter();
                    MoveButtonPointer(point);
                    await SettleButtonFeedback();
                    Check(purchase.IsHovered(), "The drawn purchase must remain mouse-accessible");
                    await Capture("purchase-hover");
                    PressButtonPointer(point, true);
                    await Capture("purchase-pressed");
                    Check(game.Score == 220 && card.IsOpen && !card.IsAnimating,
                        "Pressing the purchase must wait for release before charging");
                    // Release outside: inspect pressed art without committing a transaction.
                    MoveButtonPointer(Vector2.Zero);
                    PressButtonPointer(Vector2.Zero, false);
                    purchase.ReleaseFocus();
                    purchase.Disabled = true;
                    await Capture("purchase-disabled");
                    PressButtonPointer(point, true);
                    PressButtonPointer(point, false);
                    Check(game.Score == 220 && !card.IsAnimating,
                        "Disabled drawn purchases must reject clicks without charging");
                    purchase.Disabled = false;
                    MoveButtonPointer(Vector2.Zero);
                }
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
                    game.Score == (buy ? 229 : 220) && game.DayUpgrades.Selected == (buy ? DayUpgrade.Cash : DayUpgrade.None),
                    "Exactly one purchase or free skip must apply after exit and resume the new day");
                Check(hud.Visible && clock.Visible && clock.Modulate == Colors.White && hud.Modulate == Colors.White && !game.GetNode<Control>("HUD/InventoryUI").Visible &&
                    game.GetNode<Control>("World/Atelier/GetItemButton").Visible,
                    "Leaving the cards must restore gameplay UI and its full opacity");
            }
            foreach (double delay in new[] { 0.1, 0.8 })
            {
                game.NewGame();
                game.EndDay(); game.EndDay(); game.EndDay();
                await ToSignal(CreateTimer(delay, true), SceneTreeTimer.SignalName.Timeout);
                game.NewGame();
                await ToSignal(CreateTimer(0.4, true), SceneTreeTimer.SignalName.Timeout);
                Check(!Paused && game.GetNode<Control>("HUD/Root").Visible &&
                    game.GetNode<Control>("HUD/Root").Modulate == Colors.White,
                    "Reset during camera travel or UI fade must cancel late callbacks and restore UI opacity");
            }
            game.NewGame();
            game.EndDay(); game.EndDay(); game.EndDay();
            var active = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Root.Size = new Vector2I(960, 540);
            await WaitForDayCardAnimation(active);
            active.Choose(DayUpgrade.Cash);
            game.NewGame(); // Cancels an already queued exit transaction.
            await ToSignal(CreateTimer(0.7, true), SceneTreeTimer.SignalName.Timeout);
            Check(!Paused && !active.IsOpen && game.Score == 220 && game.CurrentDay == 1,
                "Reset during exit must cancel its callback and release pause without charging");
            game.EndDay(); game.EndDay(); game.EndDay();
            await WaitForDayCardAnimation(active);
            active.Choose(DayUpgrade.Cash);
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            active = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Check(active.IsOpen && game.DayUpgrades.Pending && game.Score == 220,
                "Reload during exit must restore the uncommitted choice without a duplicate reward");
            await WaitForDayCardAnimation(active);
            game.Score = 0;
            game.PersistProgress();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            active = game.GetNode<DayTransitionCard>("DayTransitionCard");
            await WaitForDayCardAnimation(active);
            var free = active.GetNode<Button>("Root/Center/Card/Content/Choices/Cash/Content/Choose");
            Check(free.Disabled && free.Text == GameLocalization.Format("Купить за {0}$", 6), "The cash card must cost money");
            active.Choose(DayUpgrade.Cash);
            Check(game.Score == 0 && game.DayUpgrades.Pending, "A player with zero balance must not get a paid card");
            game.Score = 6;
            game.PersistProgress();
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            active = game.GetNode<DayTransitionCard>("DayTransitionCard");
            await WaitForDayCardAnimation(active);
            active.Choose(DayUpgrade.Cash);
            active.Choose(DayUpgrade.Cash);
            await WaitForDayCardAnimation(active);
            Check(game.Score == 15, "The paid cash card must award the amount once");
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = Create();
            Check(game.Score == 15 && !game.DayUpgrades.Pending, "Reloading must preserve the committed reward once");
            game.NewGame();
            game.EndDay(); game.EndDay(); game.EndDay();
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
            DebugBalanceSettings.StoragePath = previousDebugPath;
            DebugBalanceSettings.Reload(out _);
            if (FileAccess.FileExists(debugPath)) DirAccess.RemoveAbsolute(debugPath);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
