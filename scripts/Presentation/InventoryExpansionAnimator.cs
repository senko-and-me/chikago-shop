using System.Collections.Generic;
using Godot;

// Temporary visual copies avoid fighting the inventory Container's sorting or clipping arrivals.
public sealed class InventoryExpansionAnimator(Control owner, Control overlay)
{
    private readonly List<Tween> _tweens = new();
    private CanvasItem _background, _scroller;
    public bool IsRunning { get; private set; }

    public void Play(Rect2 before, Rect2 after, int oldCount, IReadOnlyList<SlotUI> slots,
        TextureRect background, Control scroller, float slotSize, float step)
    {
        Cancel();
        _background = background;
        _scroller = scroller;
        IsRunning = true;
        background.Modulate = scroller.Modulate = new Color(1, 1, 1, 0);
        overlay.Show();
        for (int i = 0; i < slots.Count; i++)
        {
            bool existing = i < oldCount;
            Vector2 destination = after.Position + new Vector2(i * step, 0);
            var ghost = new TextureRect {
                Name = "Arrival" + i, MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Size = new Vector2(slotSize, slotSize),
                Texture = new AtlasTexture { Atlas = background.Texture,
                    Region = new Rect2(i * step / after.Size.X * background.Texture.GetWidth(), 0,
                        slotSize / after.Size.X * background.Texture.GetWidth(), background.Texture.GetHeight()) },
                Position = existing ? before.Position + new Vector2(i * step, 0)
                    : new Vector2(destination.X, owner.GetViewportRect().Size.Y - owner.GlobalPosition.Y + 24)
            };
            overlay.AddChild(ghost);
            var face = new Panel { Name = "Face", MouseFilter = Control.MouseFilterEnum.Ignore };
            face.AddThemeStyleboxOverride("panel", slots[i].GetThemeStylebox("normal"));
            ghost.AddChild(face);
            face.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var sourceIcon = slots[i].GetNode<TextureRect>("Icon");
            var icon = new TextureRect {
                Name = "Icon", Texture = sourceIcon.Texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = sourceIcon.StretchMode,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            ghost.AddChild(icon);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var tween = owner.CreateTween();
            _tweens.Add(tween);
            if (!existing) tween.TweenInterval(0.32 + (i - oldCount) * 0.18);
            tween.TweenProperty(ghost, "position", destination, existing ? 0.45 : 0.48)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            if (i == slots.Count - 1) tween.TweenCallback(Callable.From(Cancel));
        }
    }

    public void Cancel()
    {
        foreach (var tween in _tweens) if (tween.IsValid()) tween.Kill();
        _tweens.Clear();
        if (GodotObject.IsInstanceValid(_background)) _background.Modulate = Colors.White;
        if (GodotObject.IsInstanceValid(_scroller)) _scroller.Modulate = Colors.White;
        foreach (Node child in overlay.GetChildren()) { overlay.RemoveChild(child); child.QueueFree(); }
        overlay.Hide();
        IsRunning = false;
    }
}
