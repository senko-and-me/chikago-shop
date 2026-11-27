using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestRoomCurtains()
    {
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var covers = new[] {
            game.GetNode<RoomCurtainPresentation>("World/MusicWorkshop/LockedOverlay"),
            game.GetNode<RoomCurtainPresentation>("World/Showroom/LockedOverlay") };
        Vector2 start = covers[0].GetNode<Control>("LockedCurtain").Position;
        async Task Wait(double time) => await ToSignal(CreateTimer(time, true), SceneTreeTimer.SignalName.Timeout);
        async Task PayFirstQuota()
        {
            game.CurrentDay = EconomyConfigProvider.Load().Quotas.Single(quota => quota.Id == "first").DeadlineDay;
            game.Score = game.FirstQuota;
            game.AdvanceToNextDay();
            game.AcknowledgeMafiaNotice();
            game.ResumeQuotaVisit();
            game.GetNode<TextureButton>("World/TradingRoom/QuotaVisitor").EmitSignal(BaseButton.SignalName.Pressed);
            var dialogue = game.GetNode<DialoguePlayer>("QuotaDialogue/DialoguePlayer");
            await ToSignal(this, SignalName.ProcessFrame);
            for (int i = 0; i < 32 && game.QuotaVisitPending; i++)
            {
                dialogue.Advance();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Check(game.MusicWorkshopAvailable && !game.MusicWorkshopUnlocked, "The curtain remains locked after the story grants access.");
        }
        try
        {
            game.NewGame();
            var navigation = game.GetNode<WorldCamera>("Camera2D");
            navigation.SetProcess(false);
            void Press(Vector2 point, bool pressed) => Root.PushInput(new InputEventMouseButton {
                Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
                Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0 }, true);
            void Move(Vector2 point, Vector2 relative) => Root.PushInput(new InputEventMouseMotion {
                Position = point, GlobalPosition = point, Relative = relative,
                ButtonMask = MouseButtonMask.Left }, true);
            foreach (var cover in covers)
            {
                navigation.Zoom = Vector2.One * 0.8f;
                navigation.Position = cover.GlobalPosition + cover.Size / 2;
                navigation.ForceUpdateScroll();
                await ToSignal(this, SignalName.ProcessFrame);
                Vector2 point = cover.GetGlobalTransformWithCanvas() * (cover.Size * new Vector2(0.35f, 0.4f));
                MoveButtonPointer(point);
                int money = game.Score;
                Vector2 before = navigation.Position;
                Press(point, true);
                Check(navigation.IsDragging, $"LMB on {cover.GetParent().Name} curtain must start camera drag: point={point}, hover={Root.GuiGetHoveredControl()?.GetPath()}, filter={cover.MouseFilter}, locked={navigation.NavigationLocked}, paused={Paused}, focus={navigation.IsFocusing}");
                Vector2 delta = new Vector2(35, 20);
                Move(point + delta, delta);
                Check(navigation.Position.DistanceTo(before) > 1 && game.Score == money && cover.Visible,
                    "Curtain drag must move camera without buying or opening the room");
                Press(point + delta, false);
                before = navigation.Position;
                Move(point + delta * 2, delta);
                Check(!navigation.IsDragging && navigation.Position == before, "Release must stop curtain drag");
                navigation.NavigationLocked = true;
                Press(point, true);
                Check(!navigation.IsDragging, "Tutorial lock must block curtain navigation");
                Press(point, false);
                navigation.NavigationLocked = false;
                Paused = true;
                Press(point, true);
                Check(!navigation.IsDragging, "Pause must block curtain navigation");
                Press(point, false);
                Paused = false;
            }
            var purchase = covers[0].GetParent().GetNode<Button>("UnlockPanel/Content/UnlockButton");
            await PayFirstQuota();
            navigation.CancelFocusTransition();
            navigation.Position = covers[0].GlobalPosition + covers[0].Size / 2;
            navigation.ForceUpdateScroll();
            game.RefreshItemButtons();
            await ToSignal(this, SignalName.ProcessFrame);
            Vector2 purchasePoint = ScreenBounds(purchase).GetCenter();
            MoveButtonPointer(purchasePoint);
            Press(purchasePoint, true);
            Check(!navigation.IsDragging && purchase.ButtonPressed,
                "Unlock button must retain GUI input without starting camera drag");
            Press(purchasePoint, false);
            Check(game.MusicWorkshopUnlocked, "A real button click must open the workshop at zero cash");
            game.NewGame();
            foreach (var cover in covers)
            {
                var room = cover.GetParent();
                var art = room.GetNode<Sprite2D>(room.Name == "Showroom" ? "ShowroomBackground" : "Background");
                Rect2 opening = art.Transform * art.GetRect();
                Check(cover.GetRect().Encloses(opening.Grow(4)),
                    "Locked cover must overdraw every edge of the room art to prevent clipped seams");
                Check(cover.GetNode<Control>("LockedCurtain").GetRect().IsEqualApprox(new Rect2(Vector2.Zero, cover.Size)),
                    "Curtain artwork must fill its cover in the authored scene without shifted offsets");
            }
            foreach (float zoom in new[] { 0.39583334f, 0.8f, 1.2f })
            {
                var camera = game.GetNode<Camera2D>("Camera2D");
                camera.SetProcess(false);
                camera.Zoom = Vector2.One * zoom;
                camera.Position = new Vector2(3500, 1650);
                foreach (var cover in covers)
                {
                    Rect2 edges = cover.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, cover.Size);
                    var room = cover.GetParent();
                    var art = room.GetNode<Sprite2D>(room.Name == "Showroom" ? "ShowroomBackground" : "Background");
                    Check(edges.Encloses(art.GetGlobalTransformWithCanvas() * art.GetRect()),
                        "Camera movement and zoom must retain complete locked-room coverage");
                }
                using (var image = await CapturePresentation("curtain-seams-" + zoom))
                {
                    var upper = covers[0].GetParent().GetNode<Sprite2D>("Background");
                    var lower = covers[1].GetParent().GetNode<Sprite2D>("ShowroomBackground");
                    Rect2 top = upper.GetGlobalTransformWithCanvas() * upper.GetRect();
                    Rect2 bottom = lower.GetGlobalTransformWithCanvas() * lower.GetRect();
                    foreach (float fraction in new[] { 0.15f, 0.5f, 0.85f })
                    for (int y = Mathf.CeilToInt(top.End.Y); y < bottom.Position.Y; y++)
                    {
                        int x = Mathf.RoundToInt(Mathf.Lerp(top.Position.X, top.End.X, fraction));
                        if (x < 0 || x >= image.GetWidth() || y < 0 || y >= image.GetHeight()) continue;
                        Check(image.GetPixel(x, y).R < 0.7f,
                            "The rendered floor seam between locked rooms must not expose a bright strip");
                    }
                }
            }
            game.EndDay();
            game.Score = 0;
            game.TryUnlockMusicWorkshop();
            foreach (var cover in covers) Check(cover.Visible && !cover.IsRevealing,
                "A first-week action must leave the curtains closed without animation");
            await PayFirstQuota();
            game.TryUnlockMusicWorkshop();
            foreach (var cover in covers) Check(cover.Visible && cover.IsRevealing &&
                cover.MouseFilter == Control.MouseFilterEnum.Ignore && cover.ClipContents,
                "Unlock must reveal both rooms within their clipped openings without blocking input");
            Check(!covers[0].GetParent().GetNode<Control>("UnlockPanel").Visible,
                "The purchase button must disappear immediately on successful unlock");
            await Wait(0.35);
            foreach (var cover in covers)
            {
                var curtain = cover.GetNode<Control>("LockedCurtain");
                Check(cover.IsRevealing && curtain.Scale.Y > 0.06f && curtain.Scale.Y < 0.95f &&
                    curtain.Position.Y < 0 && cover.Modulate.A > 0 && cover.Modulate.A < 1,
                    "Mid-reveal curtains must gather upward while gradually fading");
            }
            using (await CapturePresentation("curtain-midpoint")) { }
            for (int i = 0; i < 12; i++) game.RefreshItemButtons();
            Paused = true;
            await Wait(0.65);
            foreach (var cover in covers) Check(!cover.Visible && !cover.IsRevealing,
                "Repeated refresh must not restart the tween and pause must not strand a curtain");
            Paused = false;
            game.NewGame();
            await PayFirstQuota(); game.TryUnlockMusicWorkshop();
            await Wait(0.15);
            game.NewGame();
            await Wait(1);
            foreach (var cover in covers) Check(cover.Visible && !cover.IsRevealing &&
                cover.Modulate == Colors.White && cover.GetNode<Control>("LockedCurtain").Scale == Vector2.One,
                "NewGame during reveal must cancel callbacks and restore the complete curtain");
            Check(covers[0].GetNode<Control>("LockedCurtain").Position == start &&
                !covers[0].GetParent().GetNode<Control>("UnlockPanel").Visible,
                "Reset must restore the authored curtain and hide the unlock button");
            var loadedScene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            var loaded = loadedScene.GetNode<RoomCurtainPresentation>("World/MusicWorkshop/LockedOverlay");
            // A standalone initial unlocked state models loading an already-owned room.
            loaded.GetParent().RemoveChild(loaded);
            loadedScene.Free();
            Root.AddChild(loaded);
            loaded.SetLocked(false);
            Check(!loaded.Visible && !loaded.IsRevealing,
                "The first unlocked refresh must show an owned room immediately without replay");
            loaded.QueueFree();
            await PayFirstQuota(); game.TryUnlockMusicWorkshop();
        }
        finally
        {
            Paused = false;
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            await Wait(1);
        }
    }
}
