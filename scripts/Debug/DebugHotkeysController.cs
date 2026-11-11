using System;
using Godot;

public partial class DebugHotkeysController : Node
{
    private TradeScene _game = null!;
    private DayCycleController _days = null!;

    public static void Attach(TradeScene game, DayCycleController days)
    {
        if (!OS.IsDebugBuild()) return;
        game.AddChild(new DebugHotkeysController
        {
            Name = "DebugHotkeys", ProcessMode = ProcessModeEnum.Always,
            _game = game, _days = days
        });
    }

    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (!OS.IsDebugBuild() || input is not InputEventKey { Pressed: true, Echo: false } key ||
            key.CtrlPressed || key.AltPressed || key.ShiftPressed || key.MetaPressed ||
            _game.Failed || _game.Completed || (GetTree().Paused && !_game.CraftMiniGameOpen)) return;

        switch (key.Keycode)
        {
            case Key.F6:
                _game.Score = (int)Math.Min(int.MaxValue, (long)_game.Score + 100);
                _game.RefreshDayPresentation();
                break;
            case Key.F7:
                _days.AddDebugTime();
                break;
            case Key.F8:
            case Key.F9:
                foreach (string name in new[] { "EmbroideryGame", "PolishingGame" })
                {
                    var craft = _game.GetNode<EmbroideryGame>(name);
                    if (craft.IsOpen) craft.FinishForTesting(key.Keycode == Key.F9);
                }
                break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }
}
