using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestQuotaCountdown()
    {
        const string save = "user://hf178_quota_countdown.json";
        string locale = TranslationServer.GetLocale();
        TradeScene game = null;
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        Label Caption() => game.GetNode<Label>("HUD/Root/ProgressPanel/Visual/Deadline/Caption");
        async Task Create()
        {
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            Root.AddChild(game);
            game.SetProcess(false);
            MoveButtonPointer(new Vector2(800, 400));
            await Frame();
        }
        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            await Create();
            int firstDeadline = EconomyConfigProvider.Load().Quotas.Single(quota => quota.Id == "first").DeadlineDay;
            int finalDeadline = EconomyConfigProvider.Load().Days.TotalDays;
            for (int stage = 0; stage < 2; stage++)
            {
                game.NewGame();
                game.FirstQuota = 1000; // Reproduce the user's custom quota without changing its rules.
                if (stage == 1)
                {
                    game.CurrentDay = firstDeadline;
                    game.Score = 10000;
                    game.AdvanceToNextDay();
                }
                int deadline = stage == 0 ? firstDeadline : finalDeadline;
                int target = stage == 0 ? game.FirstQuota : game.SecondQuota;
                game.CurrentDay = deadline - 1;
                game.Score = target - 1;
                TranslationServer.SetLocale("en");
                game.RefreshDayPresentation();
                await Frame();
                Check(Caption().Text == "Quota due in 2 days", "An available penultimate day must still count today and the deadline day");
                game.EndDay();
                game._Process(game.DayRemainingSeconds + 0.1);
                await Frame();
                Check(game.DayClosed && !game.Failed && Caption().Text == "Quota due in 1 day",
                    "A closed penultimate day must no longer count as an available day");
                AdvanceDayForTest(game);
                await Frame();
                Check(game.CurrentDay == deadline && !game.DayStarted && !game.Failed && Caption().Text == "Quota due today",
                    "The last waiting day must say today, without promising another full day after it");
                game.EndDay();
                game._Process(game.DayRemainingSeconds - 0.01);
                await Frame();
                Check(!game.DayClosed && !game.Failed && Caption().Text == "Quota due today",
                    "The deadline remains today until the last playable second finishes");
                game._Process(0.02);
                await Frame();
                Check(game.DayClosed && !game.Failed && Caption().Text == "Quota due now",
                    "At 00:00 the deadline must be due now, with no extra day displayed");
                game._Process(0.01);
                await Frame();
                Check(game.Failed && game.CurrentDay == deadline && game.Score == target - 1 &&
                    Caption().Text == "Quota deadline passed", "Failing either real quota deadline must explicitly show an expired deadline");
                await ToSignal(CreateTimer(0.65), SceneTreeTimer.SignalName.Timeout);
                foreach (string language in new[] { "ru", "en" })
                {
                    TranslationServer.SetLocale(language);
                    await Frame();
                    Check(Caption().Text == (language == "ru" ? "Срок квоты истёк" : "Quota deadline passed"),
                        "The expired deadline must be localized and never claim another day remains");
                    using (await CapturePresentation($"quota-expired-{stage}-{language}")) { }
                }
                game.PersistProgress();
                game.QueueFree();
                await Frame();
                await Create();
                Check(game.Failed && Caption().Text == "Quota deadline passed", "Reloading a failed run must retain the expired deadline caption");
                game.ReturnToCheckpoint();
                await Frame();
                Check(!game.Failed && Caption().Text != "Quota deadline passed", "Checkpoint recovery must clear the expired caption");
            }
            game.NewGame();
            game.Score = 10000;
            game.CurrentDay = firstDeadline;
            game.AdvanceToNextDay();
            game.CurrentDay = finalDeadline;
            game.AdvanceToNextDay();
            await Frame();
            Check(game.Completed && Caption().Text == "Debt repaid", "Paying the final quota must keep the existing victory caption");
            game.NewGame();
            await Frame();
            Check(!game.Failed && !game.Completed && Caption().Text.Contains(firstDeadline.ToString()),
                "A new run must restore the initial countdown instead of keeping a terminal caption");
        }
        finally
        {
            Paused = false;
            game?.QueueFree();
            TranslationServer.SetLocale(locale);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            await Frame();
        }
        GD.Print("QUOTA_COUNTDOWN_CHECKS_PASS: both deadlines, closed-day exclusion, today/00:00/failure, RU/EN, reload/checkpoint/victory/new run");
    }

    private static void TestPopupPlacement()
    {
        Rect2 viewport = new(0, 0, 1000, 700);
        Rect2 anchor = new(400, 300, 100, 80);
        Vector2 size = new(200, 120);
        Vector2 right = PopupPlacement.FindPosition(viewport, anchor, size, ReadOnlySpan<Rect2>.Empty);
        Check(right == new Vector2(512, 280), "Unobstructed popups must keep the existing preferred right-hand placement");
        ReadOnlySpan<Rect2> obstacles = stackalloc Rect2[] { new(right, size) };
        Vector2 left = PopupPlacement.FindPosition(viewport, anchor, size, obstacles);
        Check(left == new Vector2(188, 280), "Popups must move to the left when the preferred side is occupied");
        for (int i = 0; i < 32; i++) PopupPlacement.FindPosition(viewport, anchor, size, obstacles);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) PopupPlacement.FindPosition(viewport, anchor, size, obstacles);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Popup geometry must allocate no managed memory in steady state (actual {allocated} bytes)");
        GD.Print("Popup placement: 1000 calculations, 0 managed bytes allocated");
    }


    private async Task TestCollapsibleProgress()
    {
        Vector2I originalSize = Root.Size;
        string locale = TranslationServer.GetLocale();
        MoveButtonPointer(new Vector2(600, 350));
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
        var panel = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
        var content = panel.GetNode<Control>("Content");
        var toggle = game.GetNode<Button>("HUD/Root/ProgressToggleButton");
        var sticks = new[] { "Day", "Quota", "Deadline" }.Select(name => panel.GetNode<Control>("Visual/" + name)).ToArray();
        Label Caption(int index) => sticks[index].GetNode<Label>("Caption");
        Vector2[] captionOffsets = null;
        void CheckAttachedCaptions()
        {
            for (int i = 0; i < sticks.Length; i++)
                Check(Caption(i).Visible && Caption(i).GetParent() == sticks[i] &&
                    Caption(i).Position.IsEqualApprox(captionOffsets[i]) && Mathf.IsEqualApprox(Caption(i).Modulate.A, 1),
                    "Each caption must stay visible and fixed to its cigarette throughout opening and retraction");
        }
        async Task Settle() => await ToSignal(CreateTimer(0.75, true), SceneTreeTimer.SignalName.Timeout);
        try
        {
            await Settle();
            captionOffsets = Enumerable.Range(0, sticks.Length).Select(i => Caption(i).Position).ToArray();
            CheckAttachedCaptions();
            Check(!panel.Expanded && !content.Visible && toggle.Text == "" && panel.Visible,
                "The old rectangle and arrow must be replaced by the collapsed pack");
            Check(!content.GetNode<Label>("DayLabel").Visible && panel.GetNode<Control>("Visual/Pack").GetGlobalRect().Position.X < 0,
                "Legacy bindings stay hidden and only the edge of the scene-authored pack remains on screen");
            var visual = panel.GetNode<Control>("Visual");
            var front = visual.GetNode<TextureRect>("Pack/Front");
            var back = visual.GetNode<TextureRect>("Back");
            Rect2 frontBounds = ScreenBounds(front);
            Rect2 backBounds = ScreenBounds(back);
            Check(back.GetIndex() < sticks[0].GetIndex() &&
                sticks.All(stick => stick.GetIndex() < visual.GetNode<Control>("Pack").GetIndex()),
                "The back, cigarettes and front must draw in that order to form a pocket");
            var frontAtlas = (AtlasTexture)front.Texture;
            var backAtlas = (AtlasTexture)back.Texture;
            Vector2 frontScale = front.Size / frontAtlas.Region.Size;
            Vector2 backScale = back.Size / backAtlas.Region.Size;
            Check(Mathf.IsZeroApprox(front.Rotation) && Mathf.IsZeroApprox(back.Rotation) &&
                Mathf.IsEqualApprox(frontScale.X, frontScale.Y) && frontScale.IsEqualApprox(backScale) &&
                (frontBounds.Position - frontAtlas.Region.Position * frontScale).IsEqualApprox(
                    backBounds.Position - backAtlas.Region.Position * backScale),
                "Both pack layers must keep the original landscape orientation, aspect ratio and shared source canvas alignment");
            Check(backBounds.End.X > frontBounds.End.X + 3 &&
                Mathf.IsEqualApprox(backBounds.Position.Y, frontBounds.Position.Y),
                "The black back must form the right side behind the unrotated front");
            foreach (var stick in sticks)
            {
                Rect2 bounds = ScreenBounds(stick);
                Check(Mathf.IsEqualApprox(stick.Position.X, panel.ClosedX) &&
                    bounds.Position.X < frontBounds.End.X && bounds.End.X > backBounds.End.X + 12 &&
                    bounds.End.X < backBounds.End.X + 28 && Caption(Array.IndexOf(sticks, stick)).Visible &&
                    bounds.Position.Y > backBounds.Position.Y && bounds.End.Y < backBounds.End.Y,
                    "A resting cigarette must overlap the pack, protrude slightly beyond its black side and retain its caption");
            }
            using (var image = await CapturePresentation("cigarette-collapsed"))
            {
                Vector2 pixelScale = new Vector2(image.GetWidth(), image.GetHeight()) / Root.GetVisibleRect().Size;
                foreach (var stick in sticks)
                {
                    Rect2 bounds = ScreenBounds(stick);
                    float middle = bounds.GetCenter().Y;
                    Color recess = image.GetPixel((int)((backBounds.End.X - 3) * pixelScale.X),
                        (int)((bounds.Position.Y - 4) * pixelScale.Y));
                    bool visibleTip = false;
                    for (float x = backBounds.End.X + 2; x < bounds.End.X - 2; x++)
                    {
                        Color tip = image.GetPixel((int)(x * pixelScale.X), (int)(middle * pixelScale.Y));
                        visibleTip |= tip.R > recess.R + 0.08f;
                    }
                    Check(recess.R < 0.1f && visibleTip,
                        "Each resting cigarette tip must visibly protrude past the black side of the pack");
                }
            }
            Vector2 authoredVisualPosition = visual.Position;
            try
            {
                // Bring the off-screen body into view for comparison with the source artwork.
                visual.Position += new Vector2(300, 240);
                using (await CapturePresentation("cigarette-pack-reference")) { }
            }
            finally { visual.Position = authoredVisualPosition; }
            Vector2 camera = game.GetNode<Camera2D>("Camera2D").Position;
            int score = game.Score;
            Vector2 restingTip = new(ScreenBounds(sticks[0]).End.X - 4, ScreenBounds(sticks[0]).GetCenter().Y);
            Check(ScreenBounds(toggle).HasPoint(restingTip), "The pack hover target must include protruding tips");
            MoveButtonPointer(restingTip);
            await ToSignal(CreateTimer(0.14, true), SceneTreeTimer.SignalName.Timeout);
            Check(panel.Expanded && panel.IsAnimating && sticks[0].Position.X > sticks[2].Position.X &&
                sticks[0].Position.X > panel.ClosedX && sticks[0].Position.X < panel.OpenX,
                $"Real hover must animate cigarettes one at a time (expanded={panel.Expanded}, animating={panel.IsAnimating}, x={sticks[0].Position.X}/{sticks[2].Position.X}, pointer={panel.GetLocalMousePosition()}, toggle={ScreenBounds(toggle)})");
            CheckAttachedCaptions();
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            float pausedX = sticks[0].Position.X;
            await ToSignal(CreateTimer(0.16, true), SceneTreeTimer.SignalName.Timeout);
            Check(Mathf.IsEqualApprox(sticks[0].Position.X, pausedX), "Pause must freeze a slide in progress");
            pause.Close();
            await Settle();
            Check(!content.Visible && sticks.All(stick => Mathf.IsEqualApprox(stick.Position.X, panel.OpenX)) &&
                Enumerable.Range(0, sticks.Length).All(index => Caption(index).Visible),
                "Only the three information cigarettes appear; legacy actions and status stay hidden");
            Check(sticks.All(stick => ScreenBounds(stick).Position.X < frontBounds.End.X &&
                ScreenBounds(stick).End.X > backBounds.End.X),
                "Extended cigarettes must emerge through the opening while their bases remain behind the front");
            Vector2 cigarettePoint = ScreenBounds(sticks[0]).GetCenter();
            MoveButtonPointer(cigarettePoint);
            await ToSignal(this, SignalName.ProcessFrame);
            PressButtonPointer(cigarettePoint, true);
            Root.PushInput(new InputEventMouseMotion
            {
                Position = cigarettePoint + new Vector2(50, 0), GlobalPosition = cigarettePoint + new Vector2(50, 0),
                Relative = new Vector2(50, 0), ButtonMask = MouseButtonMask.Left
            }, true);
            PressButtonPointer(cigarettePoint + new Vector2(50, 0), false);
            Check(game.GetNode<Camera2D>("Camera2D").Position == camera, "Dragging the information must not pan the camera");
            MoveButtonPointer(ScreenBounds(sticks[2]).GetCenter());
            await Settle();
            Check(panel.Expanded && game.Score == score && game.GetNode<Camera2D>("Camera2D").Position == camera,
                "Moving from pack to cigarettes keeps them open without changing gameplay or camera");
            foreach (string language in new[] { "ru", "en" })
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            {
                Root.Size = size;
                GameLocalization.SelectLanguage(language);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                Check(Caption(0).Text.Contains("1") && Caption(1).Text.Contains("750") && Caption(2).Text.Contains("7"),
                    "Current day, upcoming quota and inclusive countdown must be localized from live state");
                foreach (var stick in sticks)
                {
                    var caption = stick.GetNode<Label>("Caption");
                    Check(Root.GetVisibleRect().Encloses(ScreenBounds(stick)) &&
                        caption.GetThemeFont("font").GetStringSize(caption.Text, fontSize: caption.GetThemeFontSize("font_size")).X <= caption.Size.X,
                        "Expanded cigarettes and their RU/EN captions must fit both viewports");
                }
                using (await CapturePresentation($"cigarette-{language}-{size.X}")) { }
            }
            await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
            Check(game.DayStarted, "The existing Start action remains wired to the real day cycle");
            await ClickControl(game, "HUD/Root/ProgressPanel/Content/EndDayButton");
            Check(game.DayClosed, "The existing End Day action still closes the shop");
            await ClickControl(game, "HUD/DayClock/Root/ClockActionButton");
            await WaitForDayCardAnimation(game);
            var dayCard = game.GetNode<DayTransitionCard>("DayTransitionCard");
            Check(game.CurrentDay == 2 && dayCard.IsOpen, "Clicking the closed clock must open the next day and its cards");
            dayCard.Skip();
            await WaitForDayCardAnimation(game);
            await Settle();
            Check(!content.Visible && !game.DayStarted, "Returning from cards keeps legacy controls hidden and waits for Start");
            MoveButtonPointer(ScreenBounds(toggle).GetCenter());
            await Settle();
            MoveButtonPointer(new Vector2(600, 350));
            ulong retractDeadline = Time.GetTicksMsec() + 1500;
            while (panel.Expanded || !panel.IsAnimating || sticks[2].Position.X >= panel.OpenX)
            {
                Check(Time.GetTicksMsec() < retractDeadline, "Leaving the reopened pack must begin retraction");
                CheckAttachedCaptions();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Check(!panel.Expanded && panel.IsAnimating && sticks[2].Position.X < panel.OpenX &&
                sticks[2].Position.X > panel.ClosedX, "The caption regression must sample a real retraction in progress");
            CheckAttachedCaptions();
            using (await CapturePresentation("cigarette-caption-retract")) { }
            await Settle();
            Check(!panel.Expanded && !content.Visible && sticks.All(stick => Mathf.IsEqualApprox(stick.Position.X, panel.ClosedX)),
                "Leaving retracts cigarettes and mouse actions do not leave sticky keyboard focus");
            CheckAttachedCaptions();
            toggle.GrabFocus();
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Space }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Space }, true);
            await Settle();
            toggle.ReleaseFocus();
            Check(panel.Pinned && panel.Expanded, "Keyboard activation can pin the pack open");
            await ClickControl(game, "HUD/Root/ProgressToggleButton");
            MoveButtonPointer(new Vector2(600, 350));
            await Settle();
            Check(!panel.Pinned && !panel.Expanded, $"Clicking again unpins and restores hover behavior: pinned={panel.Pinned}, expanded={panel.Expanded}, focus={toggle.HasFocus()}, failed={game.Failed}");
            game.FirstQuota = 999;
            game.CurrentDay = 7;
            game.RefreshDayPresentation();
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(Caption(1).Text.Contains("999") && Caption(2).Text == "Quota due in 1 day",
                $"Live balance and deadline refresh while closed: {Caption(1).Text}; {Caption(2).Text}");
            game.Score = 2000;
            game.AdvanceToNextDay();
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(game.QuotaStage == 1 && Caption(0).Text.Contains("8") && Caption(1).Text.Contains("1500") && Caption(2).Text.Contains("7 days"),
                "Paying the first quota switches both the target and deadline to the final quota");
            game.AcknowledgeMafiaNotice();
            game.ResumeQuotaVisit();
            game.GetNode<TextureButton>("World/TradingRoom/QuotaVisitor").EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(this, SignalName.ProcessFrame);
            var visitorDialogue = game.GetNode<DialoguePlayer>("QuotaDialogue/DialoguePlayer");
            for (int page = 0; page < 32 && game.QuotaVisitPending; page++) visitorDialogue.Advance();
            game.Score = game.SecondQuota;
            game.CurrentDay = 14;
            game.AdvanceToNextDay();
            await Settle();
            Check(game.Completed && !content.Visible && Caption(1).Text == "All quotas paid" && Caption(2).Text == "Debt repaid",
                "Victory replaces obsolete countdowns without adding any extra information");
            game.NewGame();
            game.CurrentDay = 7;
            game.Score = 0;
            game.AdvanceToNextDay();
            await Settle();
            Check(game.Failed && !content.Visible && Caption(1).Text.Contains("999"),
                "Failure must not expose the old status or buttons in the pack");
            game.GetNode<PauseMenu>("PauseMenu").Open();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/CheckpointButton");
            Check(!game.Failed && game.CurrentDay == 1, "Checkpoint recovery remains wired");
            game.CurrentDay = 7;
            game.Score = 0;
            game.AdvanceToNextDay();
            await Settle();
            game.GetNode<PauseMenu>("PauseMenu").Open();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/NewGameButton");
            Check(!game.Failed && game.CurrentDay == 1 && game.Score == game.StartingMoney, "New game remains wired");
            const string reloadPath = "user://hf150_pack_reload.json";
            game.SavePath = reloadPath;
            game.CurrentDay = 3;
            game.Score = 888;
            game.RefreshDayPresentation();
            game.PersistProgress();
            game.SavePath = "";
            var restored = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            restored.SavePath = reloadPath;
            try
            {
                Root.AddChild(restored);
                restored.SetProcess(false);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                var restoredPanel = restored.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
                Check(restored.CurrentDay == 3 && restored.Score == 888 &&
                    restoredPanel.GetNode<Label>("Visual/Day/Caption").Text.Contains("3") &&
                    restoredPanel.GetNode<Label>("Visual/Deadline/Caption").Text.Contains("5 days") &&
                    !restoredPanel.GetNode<Control>("Content").Visible,
                    "Reload must populate the new cigarettes without showing legacy controls");
            }
            finally
            {
                restored.QueueFree();
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(reloadPath));
            }
        }
        finally
        {
            Paused = false;
            Root.Size = originalSize;
            TranslationServer.SetLocale(locale);
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
        }
        GD.Print("Cigarette pack: hover, staggered slides, localized live quotas, keyboard and recovery passed");
    }

}
