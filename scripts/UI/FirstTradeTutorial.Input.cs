using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Crafting;
using Godot;

public partial class FirstTradeTutorial
{
    private CanvasLayer _inputLayer;
    internal bool RestrictsInput => _initialized && IsInsideTree() &&
        (IsActive || IsShowing && CurrentStep == Step.MusicWorkshop) && !SceneTransition.IsActive(GetTree()) &&
        !_game.Failed && !_game.Completed;
    internal bool AllowsProgressInteraction => !RestrictsInput ||
        !IsSkipConfirmationOpen && !_pause.IsOpen && CurrentStep == Step.Summary && _summaryPage == 0;
    internal bool AllowsNotebookInteraction => !RestrictsInput ||
        !IsSkipConfirmationOpen && !_pause.IsOpen && CurrentStep == Step.Notebook;

    private void InstallInputGate()
    {
        if (_inputLayer != null) return;
        _inputLayer = new CanvasLayer { Name = "TutorialInputGate", Layer = 4096, ProcessMode = ProcessModeEnum.Always };
        GetTree().Root.AddChild(_inputLayer);
        var gate = new TutorialInputGate { Name = "Shield", Tutorial = this, MouseFilter = Control.MouseFilterEnum.Stop };
        _inputLayer.AddChild(gate);
        gate.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    private IEnumerable<Control> TutorialClickTargets()
    {
        if (IsSkipConfirmationOpen)
        {
            yield return _confirmation.GetNode<Control>("Center/Panel/Content/Actions/Confirm");
            yield return _confirmation.GetNode<Control>("Center/Panel/Content/Actions/Cancel");
            yield break;
        }
        if (_pause.IsOpen)
        {
            yield return _pause.GetNode<Control>("Root");
            yield break;
        }
        if (_skip.IsVisibleInTree()) yield return _skip;
        if (_done.IsVisibleInTree()) yield return _done;
        if (_card.IsOpen)
        {
            if (_cardLessonDone && !_card.IsAnimating)
            {
                foreach (Node card in _card.GetNode("Root/Center/Card/Content/Choices").GetChildren())
                    yield return card.GetNode<Control>("Content/Choose");
                yield return _card.GetNode<Control>("Root/Center/Card/Content/Skip");
            }
            yield break;
        }
        var mini = _embroidery.IsOpen ? _embroidery : _game.GetNode<EmbroideryGame>("PolishingGame");
        if (mini.IsOpen)
        {
            if (mini.ActiveSession.State != EmbroideryState.Playing)
                yield return mini.GetNode<Control>("Surface/Panel/Buttons/Action");
            yield break;
        }
        if (CurrentStep is Step.Start or Step.Craft or Step.CraftSelection or Step.Place or Step.Reroll or Step.CloseTrainingDay)
        {
            if (Target != null) yield return Target;
        }
        else if (CurrentStep is Step.TimedSale or Step.LateSale && !BlocksSaleActions)
            yield return _salePanel.GetNode<Control>("Content/Buttons/AcceptButton");
        else if (CurrentStep == Step.Summary && _summaryPage == 0)
            yield return _game.GetNode<Control>("HUD/Root/ProgressToggleButton");
    }

    private static bool Shown(Control control) => IsInstanceValid(control) && control.IsVisibleInTree();
    private static bool Contains(Control control, Vector2 pointer) => Shown(control) && TutorialSpotlight.TargetBounds(control).HasPoint(pointer);

    internal bool AllowsTutorialPointer(Vector2 pointer, bool action)
    {
        foreach (Control control in TutorialClickTargets())
            if (Contains(control, pointer)) return true;
        if (IsSkipConfirmationOpen || _pause.IsOpen) return false;
        var mini = _embroidery.IsOpen ? _embroidery : _game.GetNode<EmbroideryGame>("PolishingGame");
        if (mini.IsOpen && mini.ActiveSession.State == EmbroideryState.Playing)
        {
            var surface = mini.GetNode<EmbroiderySurface>("Surface");
            return (surface.GetGlobalTransformWithCanvas() * surface.FabricRect).HasPoint(pointer);
        }
        if (action || _card.IsOpen || mini.IsOpen) return false;
        if (AllowsProgressInteraction && Contains(_pack, pointer)) return true;
        if (AllowsNotebookInteraction && (_notebook.GetGlobalTransformWithCanvas() * _notebook.HoverBounds).HasPoint(pointer)) return true;
        return CurrentStep is not (Step.Camera or Step.CameraZoom or Step.RoomFocus) && Contains(Target, pointer);
    }

    internal bool AllowsTutorialFocus(Control control) => TutorialClickTargets().Any(target => Shown(target) &&
        (target == control || target.IsAncestorOf(control)));

    private IEnumerable<BaseButton> TutorialButtons(Control target)
    {
        if (!Shown(target)) yield break;
        if (target is BaseButton button && !button.Disabled && button.FocusMode != Control.FocusModeEnum.None) yield return button;
        foreach (Node child in target.GetChildren())
            if (child is Control control)
                foreach (var nested in TutorialButtons(control)) yield return nested;
    }

    internal bool FilterTutorialInput(InputEvent input)
    {
        if (input is InputEventKey key)
        {
            if (key.Keycode == Key.Escape)
                return IsSkipConfirmationOpen || _pause.IsOpen ||
                    CurrentStep == Step.Summary && _summaryPage == SummaryPages.Length - 1 || CurrentStep == Step.Start && _game.DayClosed;
            if (key.CtrlPressed || key.AltPressed || key.MetaPressed) return false;
            if (key.Keycode is Key.Tab or Key.Left or Key.Right or Key.Up or Key.Down)
            {
                if (key.Pressed)
                {
                    var buttons = TutorialClickTargets().SelectMany(TutorialButtons).Distinct().ToArray();
                    int index = System.Array.IndexOf(buttons, GetViewport().GuiGetFocusOwner());
                    bool back = key.ShiftPressed || key.Keycode is Key.Left or Key.Up;
                    if (buttons.Length > 0) buttons[index < 0 ? 0 : (index + (back ? -1 : 1) + buttons.Length) % buttons.Length].GrabFocus();
                }
                return false;
            }
            return key.Keycode is Key.Space or Key.Enter or Key.KpEnter &&
                GetViewport().GuiGetFocusOwner() is BaseButton focused && AllowsTutorialFocus(focused);
        }
        Vector2? pointer = input switch
        {
            InputEventMouse mouse => mouse.Position,
            InputEventScreenTouch touch => touch.Position,
            InputEventScreenDrag drag => drag.Position,
            _ => null
        };
        if (pointer == null) return false;
        bool action = input is InputEventMouseButton or InputEventScreenTouch;
        if (AllowsTutorialPointer(pointer.Value, action))
            return input is not InputEventMouseButton button || button.ButtonIndex == MouseButton.Left;
        // Forward just the prescribed camera gesture; GUI underneath remains shielded.
        if (!IsSkipConfirmationOpen && !_pause.IsOpen && !GetTree().Paused)
        {
            if (CurrentStep == Step.Camera && (input is InputEventMouseMotion || input is InputEventMouseButton { ButtonIndex: MouseButton.Left, DoubleClick: false }))
            {
                _camera._Input(input);
                if (input is InputEventMouseButton { Pressed: true }) _camera._UnhandledInput(input);
            }
            else if (CurrentStep == Step.CameraZoom && input is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown })
                _camera._Input(input);
            else if (CurrentStep == Step.RoomFocus && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true } && Contains(_atelierFrame, pointer.Value))
                _camera._UnhandledInput(input);
        }
        return false;
    }
}
