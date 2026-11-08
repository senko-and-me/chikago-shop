using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

// Observes existing input signals; never triggers buttons, changes their styles or moves controls.
public partial class ButtonFeedbackController : Node
{
    public static ButtonFeedbackController Instance { get; private set; }
    public readonly record struct VisualFrame(Rect2 Bounds, Color Accent, float Strength, float Pressed, float Flash);
    private const float FlashSeconds = 0.18f;
    private readonly Dictionary<BaseButton, State> _states = new();
    private State _active;
    private float _strength;
    private long _sequence;
    private bool _preferPointer = true;
    private readonly ConditionalWeakTable<Texture2D, TextureInk> _textureInk = new();
    private sealed class TextureInk { public Rect2 Bounds; }

    private sealed class State
    {
        public BaseButton Button;
        public bool Hovered, Held;
        public float Flash;
        public long Sequence;
        public Control.CursorShape OriginalCursor;
        public Action Enter, Exit, Down, Up, Activate;
    }

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        GetTree().NodeAdded += OnNodeAdded;
        GetTree().NodeRemoved += OnNodeRemoved;
        AttachExisting(GetTree().Root);
    }

    private void AttachExisting(Node node)
    {
        OnNodeAdded(node);
        foreach (Node child in node.GetChildren()) AttachExisting(child);
    }

    private void OnNodeAdded(Node node)
    {
        if (node is not BaseButton button || _states.ContainsKey(button)) return;
        var state = new State { Button = button, OriginalCursor = button.MouseDefaultCursorShape };
        state.Enter = () => { state.Hovered = true; state.Sequence = ++_sequence; };
        state.Exit = () => { state.Hovered = false; state.Held = false; };
        state.Down = () =>
        {
            if (!Interactive(button)) return;
            state.Held = true;
            state.Flash = FlashSeconds;
            state.Sequence = ++_sequence;
        };
        state.Up = () => state.Held = false;
        state.Activate = () =>
        {
            if (!Interactive(button)) return;
            state.Flash = FlashSeconds;
            state.Sequence = ++_sequence;
        };
        button.MouseEntered += state.Enter;
        button.MouseExited += state.Exit;
        button.ButtonDown += state.Down;
        button.ButtonUp += state.Up;
        button.Pressed += state.Activate;
        _states.Add(button, state);
    }

    private void OnNodeRemoved(Node node)
    {
        if (node is not BaseButton button || !_states.Remove(button, out var state)) return;
        Disconnect(state);
        if (_active == state) { _active = null; _strength = 0; }
    }

    private static void Disconnect(State state)
    {
        if (!GodotObject.IsInstanceValid(state.Button)) return;
        state.Button.MouseEntered -= state.Enter;
        state.Button.MouseExited -= state.Exit;
        state.Button.ButtonDown -= state.Down;
        state.Button.ButtonUp -= state.Up;
        state.Button.Pressed -= state.Activate;
        state.Button.MouseDefaultCursorShape = state.OriginalCursor;
    }

    private static bool Interactive(BaseButton button) => GodotObject.IsInstanceValid(button) &&
        !button.IsQueuedForDeletion() && button.IsInsideTree() && button.IsVisibleInTree() &&
        !button.Disabled && button.CanProcess();

    // Modal navigation can consume a key before this autoload's _Input observer receives it.
    public void UseKeyboardNavigation() => _preferPointer = false;

    public override void _Input(InputEvent input)
    {
        // Observe the navigation method without consuming or forwarding input.
        if (input is InputEventMouseMotion or InputEventMouseButton) _preferPointer = true;
        else if (input is InputEventKey { Pressed: true, Echo: false }) _preferPointer = false;
    }

    public override void _Process(double delta)
    {
        State flashing = null, hovered = null, focused = null;
        var pointerControl = GetViewport().GuiGetHoveredControl();
        foreach (var state in _states.Values)
        {
            var button = state.Button;
            button.MouseDefaultCursorShape = button.Disabled ? Control.CursorShape.Forbidden : Control.CursorShape.PointingHand;
            if (!Interactive(button)) { state.Hovered = false; state.Held = false; state.Flash = 0; continue; }
            // Reconcile hover after hide/show, re-enable, scrolling and modal transitions.
            state.Hovered = pointerControl == button || (pointerControl != null && button.IsAncestorOf(pointerControl));
            state.Flash = Mathf.Max(0, state.Flash - (float)delta);
            if (state.Flash > 0 && (flashing == null || state.Sequence > flashing.Sequence)) flashing = state;
            if (state.Hovered && (hovered == null || state.Sequence > hovered.Sequence)) hovered = state;
            if (button.HasFocus()) focused = state;
        }
        var next = flashing ?? (_preferPointer ? hovered ?? focused : focused ?? hovered);
        if (next != null && next != _active) { _active = next; _strength = 0; }
        if (_active == null || !Interactive(_active.Button)) { _active = null; _strength = 0; return; }
        _strength = Mathf.MoveToward(_strength, next != null ? 1 : 0, (float)delta * 12);
        if (_strength <= 0) _active = null;
    }

    public VisualFrame GetVisualFrame()
    {
        if (_active == null || !Interactive(_active.Button)) return default;
        var button = _active.Button;
        Rect2 localBounds = button is TextureButton textureButton
            ? TextureBounds(textureButton) : new Rect2(Vector2.Zero, button.Size);
        if (button.HasMeta("feedback_bounds"))
            localBounds = button.GetMeta("feedback_bounds").AsRect2().Intersection(localBounds);
        Rect2 bounds = button.GetGlobalTransformWithCanvas() * localBounds;
        for (Node parent = button.GetParent(); parent != null && parent is not CanvasLayer; parent = parent.GetParent())
            if (parent is Control { ClipContents: true } clip)
                bounds = bounds.Intersection(clip.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, clip.Size));
        bounds = bounds.Intersection(button.GetViewportRect());
        return new VisualFrame(bounds, AccentFor(button), _strength,
            _active.Held && button.IsPressed() ? 1 : 0, _active.Flash / FlashSeconds);
    }

    private Rect2 TextureBounds(TextureButton button)
    {
        Texture2D texture = button.TextureNormal;
        if (button.IsPressed() && button.TexturePressed != null) texture = button.TexturePressed;
        else if (button.IsHovered() && button.TextureHover != null) texture = button.TextureHover;
        Rect2 control = new(Vector2.Zero, button.Size);
        if (texture == null || button.StretchMode == TextureButton.StretchModeEnum.Tile) return control;
        Vector2 size = texture.GetSize();
        if (size.X <= 0 || size.Y <= 0) return control;
        Rect2 ink = _textureInk.GetValue(texture, source =>
        {
            var cached = new TextureInk { Bounds = new Rect2(Vector2.Zero, source.GetSize()) };
            using var image = source.GetImage();
            if (image != null && (!image.IsCompressed() || image.Decompress() == Error.Ok))
                cached.Bounds = image.GetUsedRect();
            return cached;
        }).Bounds;
        Vector2 drawnSize = button.Size;
        Vector2 offset = Vector2.Zero;
        switch (button.StretchMode)
        {
            case TextureButton.StretchModeEnum.Keep:
                drawnSize = size;
                break;
            case TextureButton.StretchModeEnum.KeepCentered:
                drawnSize = size;
                offset = (button.Size - drawnSize) / 2;
                break;
            case TextureButton.StretchModeEnum.KeepAspect:
            case TextureButton.StretchModeEnum.KeepAspectCentered:
                drawnSize = size * Mathf.Min(button.Size.X / size.X, button.Size.Y / size.Y);
                if (button.StretchMode == TextureButton.StretchModeEnum.KeepAspectCentered)
                    offset = (button.Size - drawnSize) / 2;
                break;
            case TextureButton.StretchModeEnum.KeepAspectCovered:
                drawnSize = size * Mathf.Max(button.Size.X / size.X, button.Size.Y / size.Y);
                offset = (button.Size - drawnSize) / 2;
                break;
        }
        if (button.FlipH) ink.Position = new Vector2(size.X - ink.End.X, ink.Position.Y);
        if (button.FlipV) ink.Position = new Vector2(ink.Position.X, size.Y - ink.End.Y);
        Vector2 scale = drawnSize / size;
        return new Rect2(offset + ink.Position * scale, ink.Size * scale).Intersection(control);
    }

    private static Color AccentFor(BaseButton button) => button.Name.ToString() switch
    {
        "AcceptButton" or "PlayButton" or "ContinueButton" or "ResumeButton" or
        "GetItemButton" or "UnlockButton" or "Choose" or "Action" or "EndDayButton" => new Color("55c78b"),
        "DeclineButton" or "QuitButton" or "Cancel" or "Skip" or "SkipButton" or
        "CheckpointButton" or "NewGameButton" => new Color("ed7474"),
        _ => new Color("d7d7d7")
    };

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= OnNodeAdded;
        GetTree().NodeRemoved -= OnNodeRemoved;
        foreach (var state in _states.Values) Disconnect(state);
        _states.Clear();
        _active = null;
        if (Instance == this) Instance = null;
    }
}
