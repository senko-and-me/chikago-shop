using System;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task BlockedTutorialClick(Button button)
    {
        int presses = 0;
        void Count() => presses++;
        button.Pressed += Count;
        try
        {
            Rect2 bounds = ScreenBounds(button).Intersection(Root.GetVisibleRect());
            Check(bounds.HasArea(), "Blocked UI must be visible for a real input test: " + button.Name);
            Vector2 point = bounds.GetCenter();
            MoveButtonPointer(point);
            await ToSignal(this, SignalName.ProcessFrame);
            PressButtonPointer(point, true);
            PressButtonPointer(point, false);
            // Keyboard activation must not bypass the same restriction.
            button.GrabFocus();
            Root.PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true }, true);
            Root.PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = false }, true);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(presses == 0, "Only the current instruction may activate UI: " + button.Name);
        }
        finally { button.Pressed -= Count; }
    }

    private async Task TestTutorialInput()
    {
        Check(OS.GetUserDataDir().Contains("SC218"), "Tutorial input checks require isolated saves");
        Vector2I oldSize = Root.Size;
        TradeScene game = null;
        async Task Frame() { await ToSignal(this, SignalName.ProcessFrame); await ToSignal(this, SignalName.ProcessFrame); }
        try
        {
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = "";
            var tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            tutorial.ProgressPath = "";
            tutorial.CameraTravelSeconds = 0;
            Root.AddChild(game);
            game.SetProcess(false);
            await Frame();
            tutorial.Restart();
            var pack = game.GetNode<ProgressPanel>("HUD/Root/ProgressPanel");
            var notebook = game.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook");
            var toggle = game.GetNode<Button>("HUD/Root/ProgressToggleButton");
            var camera = game.GetNode<WorldCamera>("Camera2D");
            foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                Root.Size = size;
                await Frame();
                await BlockedTutorialClick(toggle);
                Rect2 tab = notebook.GetGlobalTransformWithCanvas() * notebook.HoverBounds;
                Vector2 pointer = new(tab.End.X - 2, tab.GetCenter().Y);
                MoveButtonPointer(pointer);
                notebook._Process(1);
                pack._Process(1);
                await Frame();
                Check(!pack.Expanded && !pack.Pinned && !notebook.Expanded,
                    "Neither hover, click nor forced focus may reveal unintroduced HUD widgets");
                float zoom = camera.Zoom.X;
                Root.PushInput(new InputEventMouseButton { Position = Root.Size / 2, ButtonIndex = MouseButton.WheelUp, Pressed = true }, true);
                await Frame();
                Check(camera.Zoom.X == zoom, "Panning instruction must not allow zooming early");
                foreach (Key key in new[] { Key.Escape, Key.F10, Key.F6, Key.F7, Key.F })
                {
                    Root.PushInput(new InputEventKey { Keycode = key, Pressed = true }, true);
                    Root.PushInput(new InputEventKey { Keycode = key, Pressed = false }, true);
                }
                await Frame();
                Check(!game.GetNode<PauseMenu>("PauseMenu").IsOpen && !Root.GetNode<DebugBalancePanel>("DebugBalance").IsOpen &&
                    !game.DayStarted && !game.Crafting.IsCatalogOpen,
                    "Unintroduced shortcuts must not open menus or change tutorial progress");
            }
            // Real prescribed panning still reaches the next practice instruction.
            camera.FocusOn(camera.Position, new Vector2(1800, 1000), 0);
            Vector2 start = Root.Size * new Vector2(0.5f, 0.65f);
            MoveButtonPointer(start);
            PressButtonPointer(start, true);
            Root.PushInput(new InputEventMouseMotion { Position = start + new Vector2(400, 0), Relative = new Vector2(400, 0), ButtonMask = MouseButtonMask.Left }, true);
            PressButtonPointer(start + new Vector2(400, 0), false);
            await Frame();
            Check(tutorial.CameraPanProgress > 0, "The prescribed camera gesture must remain usable through the shield");

            // Skip confirmation owns all input, then cancellation returns to the same step.
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            Check(tutorial.IsSkipConfirmationOpen && Paused, "Skip must open its own working modal");
            await BlockedTutorialClick(toggle);
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Cancel");
            Check(tutorial.IsActive && !tutorial.IsSkipConfirmationOpen && !Paused, "Cancel skip must keep training active");
            await ClickControl(game, "FirstTradeTutorial/Root/Skip");
            await ClickControl(game, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Confirm");
            await Frame();
            Check(!tutorial.IsActive && !tutorial.RestrictsInput, "Finishing or skipping must remove all restrictions");
            MoveButtonPointer(ScreenBounds(toggle).GetCenter());
            pack._Process(1);
            await Frame();
            Check(pack.Expanded, "Normal cigarette hover must return after training");
            Rect2 normalTab = notebook.GetGlobalTransformWithCanvas() * notebook.HoverBounds;
            MoveButtonPointer(new Vector2(normalTab.End.X - 2, normalTab.GetCenter().Y));
            notebook._Process(1);
            Check(notebook.Expanded, "Normal notebook hover must return after training");
            game.QueueFree(); game = null;
            await Frame();
            Check(Root.GetNodeOrNull("TutorialInputGate") == null, "Unloading the scene must remove its viewport shield");
        }
        finally
        {
            Paused = false;
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            Root.Size = oldSize;
            await Frame();
        }
        await TestMergedTutorial();
    }
}
