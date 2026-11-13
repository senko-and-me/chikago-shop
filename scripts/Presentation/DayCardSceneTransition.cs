using System;
using System.Collections.Generic;
using Godot;

// Presentation only: return the world view, then hide UI until the card choice closes.
public sealed class DayCardSceneTransition(Node owner)
{
    public const double CameraDuration = 0.7;
    public const double UiFadeDuration = 0.25;
    private readonly List<(CanvasItem Item, Color Color)> _ui = new();
    private Tween _sequence;
    private readonly List<Tween> _fades = new();
    private WorldCamera _camera;

    public void Begin(Action ready)
    {
        Restore();
        var game = owner.GetParent();
        _camera = game.GetNode<WorldCamera>("Camera2D");
        void Remember(CanvasItem item)
        {
            if (item.IsVisibleInTree()) _ui.Add((item, item.Modulate));
        }
        void WorldUi(Node node)
        {
            if (node.Name.ToString() is "RoomFrame" or "RoomPartitions" or "LockedCurtain" or "Wall") return;
            if (node is Control control && node.Name != "LockedOverlay") { Remember(control); return; }
            foreach (Node child in node.GetChildren()) WorldUi(child);
        }
        WorldUi(game.GetNode("World"));
        foreach (Node child in game.GetNode("HUD").GetChildren())
            if (child is CanvasItem item) Remember(item);
        Remember(game.GetNode<InventoryUI>("HUD/InventoryUI").ItemPopup);
        Remember(game.GetNode<Control>("DayAnnouncements/Root"));
        _camera.ReturnToOverview(CameraDuration);
        _sequence = owner.CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _sequence.TweenInterval(CameraDuration);
        _sequence.TweenCallback(Callable.From(() =>
        {
            foreach (var entry in _ui)
                if (GodotObject.IsInstanceValid(entry.Item))
                {
                    var fade = owner.CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
                    fade.TweenProperty(entry.Item, "modulate:a", 0f, UiFadeDuration);
                    _fades.Add(fade);
                }
        }));
        _sequence.TweenInterval(UiFadeDuration);
        _sequence.TweenCallback(Callable.From(() =>
        {
            foreach (var entry in _ui) if (GodotObject.IsInstanceValid(entry.Item)) entry.Item.Hide();
            ready();
        }));
    }

    public void Restore()
    {
        if (_sequence?.IsValid() == true) _sequence.Kill();
        _sequence = null;
        foreach (var fade in _fades) if (fade.IsValid()) fade.Kill();
        _fades.Clear();
        if (GodotObject.IsInstanceValid(_camera)) _camera.CancelOverviewTransition();
        foreach (var entry in _ui)
            if (GodotObject.IsInstanceValid(entry.Item)) { entry.Item.Modulate = entry.Color; entry.Item.Show(); }
        _ui.Clear();
    }
}
