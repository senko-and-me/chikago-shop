using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestDayStartGate()
    {
        const string save = "user://hf176_start_gate.json";
        Vector2I originalSize = Root.Size;
        string originalLocale = TranslationServer.GetLocale();
        TradeScene game = null;
        async Task Reload()
        {
            if (game != null) { game.QueueFree(); await ToSignal(this, SignalName.ProcessFrame); }
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            Root.AddChild(game);
            game.SetProcess(false);
            MoveButtonPointer(new Vector2(800, 400));
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            using (var file = FileAccess.Open(save, FileAccess.ModeFlags.Write))
                file.StoreString("{\"save_version\":13,\"day\":1,\"score\":1000,\"energy_remaining\":3,\"day_remaining_seconds\":90,\"day_started\":false,\"stored_items\":[0],\"showcase_items\":[-1,-1,-1,-1,-1,-1]}");
            await Reload();
            Check(game.StoredItems.Length == 1 && !game.DayStarted, "The waiting-day fixture must restore an inventory item");
            var gate = game.GetNode<DayStartGate>("DayStartGate");
            var notice = gate.GetNode<Control>("Root/Notice");
            var message = notice.GetNode<Label>("Message");
            var inventory = game.GetNode<InventoryUI>("HUD/InventoryUI");
            int money = game.Score, energy = game.EnergyRemaining;
            game.GetItemFromAtelier();
            game.GetItemFromMusicWorkshop();
            game.GetNode<EmbroideryGame>("EmbroideryGame").Open();
            game.GetNode<EmbroideryGame>("PolishingGame").Open();
            game.TryUnlockMusicWorkshop();
            game.SelectInventoryItem(0);
            game.SelectedInventoryIndex = 0; // Even a stale selection must not bypass the action adapter.
            game.PlaceItemInShowcase(0);
            game.RerollCustomer();
            game.AcceptTrade();
            Check(!game.CraftMiniGameOpen && !Paused && !game.MusicWorkshopUnlocked && !game.TradeActive &&
                game.Score == money && game.EnergyRemaining == energy && game.StoredItems.SequenceEqual(new[] { 0 }) &&
                game.ShowcaseItems.All(item => item == -1) && game.DayRemainingSeconds == 90,
                "Every direct gameplay action must be inert before Start");
            game.SelectedInventoryIndex = -1;
            Check(notice.Visible && message.Text.Contains("Начать"), "Blocked actions must show a visible Start instruction");
            foreach (Vector2I size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string locale in new[] { "ru", "en" })
            {
                Root.Size = size;
                TranslationServer.SetLocale(locale);
                gate.Dismiss();
                await ClickControl(game, "World/Atelier/GetItemButton");
                Check(notice.Visible && !game.CraftMiniGameOpen &&
                    message.Text.Contains(locale == "en" ? "Start" : "Начать"),
                    "A real craft click must explain the waiting-day rule in the selected language");
                using (await CapturePresentation($"start-gate-{locale}-{size.X}")) { }
                Check(Root.GetVisibleRect().Encloses(ScreenBounds(notice)) && message.GetLineCount() > 0 &&
                    message.Size.Y >= message.GetLineHeight() * message.GetLineCount(),
                    "The localized notice must fit the viewport and all wrapped lines");
                var tutorialPanel = game.GetNode<Control>("FirstTradeTutorial/Root/Panel");
                Check(!tutorialPanel.IsVisibleInTree() || !ScreenBounds(notice).Intersects(ScreenBounds(tutorialPanel)),
                    "The Start instruction must remain readable beside the existing tutorial panel");
            }
            gate.Dismiss();
            await ClickControl(game, "World/MusicWorkshop/LockedOverlay/UnlockButton");
            Check(notice.Visible && !game.MusicWorkshopUnlocked && game.Score == money, "Unlock clicks must show the same notice without spending money");
            gate.Dismiss();
            await ClickControl(game, "World/ShowroomLeft/Showcases/Slot1");
            Check(notice.Visible && game.StoredItems.Length == 1, "Even a disabled display must explain why a pre-start click cannot act");
            gate.Dismiss();
            var item = inventory.Container.GetChild<SlotUI>(0);
            await ClickControl(game, item.GetPath().ToString().Replace(game.GetPath() + "/", ""));
            Check(notice.Visible && inventory.SelectedIndex == -1 && game.SelectedInventoryIndex == -1 &&
                item._GetDragData(Vector2.Zero).VariantType == Variant.Type.Nil,
                "Click, selection and drag must not transfer an inventory item before Start");
            gate.Dismiss();
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.F }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.F }, true);
            Check(notice.Visible, "The reroll hotkey must explain the same waiting-day rule");
            gate.Dismiss();
            game.GetNode<Button>("World/Atelier/GetItemButton").GrabFocus();
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Space }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Space }, true);
            Check(notice.Visible && !game.CraftMiniGameOpen, "Keyboard activation must be blocked with feedback");
            game.GetNode<Button>("World/Atelier/GetItemButton").ReleaseFocus();
            var craftButton = game.GetNode<Control>("World/Atelier/GetItemButton");
            var uiOverlay = new Button { Name = "GateUiProbe", ZIndex = 200,
                Position = ScreenBounds(craftButton).Position, Size = ScreenBounds(craftButton).Size, Text = "UI" };
            game.GetNode<Control>("HUD/Root").AddChild(uiOverlay);
            int uiClicks = 0;
            uiOverlay.Pressed += () => uiClicks++;
            gate.Dismiss();
            await ClickControl(game, "HUD/Root/GateUiProbe");
            Check(uiClicks == 1 && !notice.Visible,
                "A foreground UI control must remain usable even when a blocked world button is behind it");
            uiOverlay.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            var camera = game.GetNode<WorldCamera>("Camera2D");
            camera.ResetToOverview();
            await ToSignal(this, SignalName.ProcessFrame);
            var room = game.GetNode<Control>("World/Corridor/RoomFrame");
            Vector2 point = room.GetGlobalTransformWithCanvas() * (room.Size * new Vector2(0.1f, 0.4f));
            MoveButtonPointer(point);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true }, true);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = false }, true);
            Check(camera.IsFocusing && !game.DayStarted, "Double-click camera navigation must remain available before Start");
            camera.CancelFocusTransition();
            game.GetNode<PauseMenu>("PauseMenu").Open();
            Check(Paused, "Pause and settings must remain accessible before Start");
            game.GetNode<PauseMenu>("PauseMenu").Close();
            gate._Process(4);
            Check(!notice.Visible, "The notice must disappear after a short interval");
            game.PersistProgress();
            await Reload();
            game.GetItemFromAtelier();
            Check(!game.DayStarted && !game.CraftMiniGameOpen, "Reload must preserve the action lock for a waiting day");
            gate = game.GetNode<DayStartGate>("DayStartGate");
            await ClickControl(game, "HUD/DayClock/Root/DayTimer/StartButton");
            Check(game.DayStarted && !gate.GetNode<Control>("Root/Notice").Visible, "Start must immediately dismiss the notice and lift the gate");
            game.SelectInventoryItem(0);
            game.PlaceItemInShowcase(0);
            Check(game.StoredItems.Length == 0 && game.ShowcaseItems[0] == 0, "Placing inventory items must work normally after Start");
            game.TryUnlockMusicWorkshop();
            Check(game.MusicWorkshopUnlocked, "Unlocking must work normally after Start");
            game.PersistProgress();
            await Reload();
            Check(game.DayStarted, "Reload of a running day must not reapply the waiting-day gate");
            game.GetItemFromAtelier();
            Check(game.CraftMiniGameOpen, "Crafting must open normally after Start");
            await PressEscapeForTest();
            AdvanceDayForTest(game);
            Check(!game.DayStarted, "The next day must require Start again");
            game.GetItemFromAtelier();
            Check(!game.CraftMiniGameOpen, "The next waiting day must block actions again");
            game.ReturnToCheckpoint();
            game.GetItemFromAtelier();
            Check(!game.DayStarted && !game.CraftMiniGameOpen, "Checkpoint recovery must retain the pre-start action lock");
        }
        finally
        {
            Paused = false;
            game?.QueueFree();
            Root.Size = originalSize;
            TranslationServer.SetLocale(originalLocale);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            await ToSignal(this, SignalName.ProcessFrame);
        }
        GD.Print("DAY_START_GATE_CHECKS_PASS: direct/UI/keyboard/drag blocking, localized notice, navigation, Start/reload/new day/checkpoint");
    }
}
