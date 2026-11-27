using Godot;

// A viewport-level shield runs before scene/autoload input and blocks GUI hit testing.
// Polling HUD widgets also consult the same tutorial policy.
public partial class TutorialInputGate : Control
{
    internal FirstTradeTutorial Tutorial { get; set; }

    public override bool _HasPoint(Vector2 point) => Tutorial.RestrictsInput && !Tutorial.AllowsTutorialPointer(point, false);

    public override void _Input(InputEvent input)
    {
        if (Tutorial.RestrictsInput && !Tutorial.FilterTutorialInput(input)) GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!Tutorial.RestrictsInput) return;
        Control focus = GetViewport().GuiGetFocusOwner();
        if (focus != null && !Tutorial.AllowsTutorialFocus(focus)) focus.ReleaseFocus();
    }
}
